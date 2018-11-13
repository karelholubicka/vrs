///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [Renderer.cs]
// (c) 2012-2015, Open3Mod Contributors
//
// Licensed under the terms and conditions of the 3-clause BSD license. See
// the LICENSE file in the root folder of the repository for the details.
//
// HIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND 
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
///////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Valve.VR;
using DeckLinkAPI;
using System.IO;
using Assimp;


namespace open3mod
{
    public class Renderer : IDisposable
    {
        public bool showTimeTrack = true;//false;
        public string timeTracking = "";
        public bool renderIO = true;

        private readonly MainWindow _mainWindow;
        private readonly TextOverlay _textOverlay;

        private Image[,] _hudImages;
        private bool _hudDirty = true;

        private double _accTime;
        private readonly float[] _lastActiveVp = new float[4];
        private Point _mousePos;
        private Rectangle _hoverRegion;
        private double _displayFps;
        private Vector4 _hoverViewport;
        private Point _mouseClickPos;
        private bool _processHudClick;
        private Tab.ViewIndex _hoverViewIndex;
        private float _hoverFadeInTime;
        private NDISender _NDISender = new NDISender();
        private long _NDITimeCode;
        static int numBuffers = NDISender.NDIchannels * 2; //4 = 0-3; ale index 0 není buffer[0], pozor
        int[] pixelPackBuffer = new int[numBuffers];
        int[] pixelUnpackBuffer = new int[numBuffers + 2];//2 na dynamic in loop
        int shift = 0; //0 - first buffer, 1-second buffer
        public Stopwatch _runsw = new Stopwatch();
        public Stopwatch _renderClock = new Stopwatch();
        public double lastTime;
        public static string[] streamName = { "Decklink", "Composite", "OFF", "OFF" };
        private GraphicsContext _rendererContext;
        private Scene _controller;
        private Scene _lighthouse;
        private Scene _hmd;
        private Scene _camera;
        private VRModelCameraController _tracker;
        static private int _maxCameras = 2;//0 = NoCamera/HMD, 1+2 Cameras with Controllers 1+2;
        static private int _maxCamArray = _maxCameras + 1;
        private int[] _cameraTexture = new int[_maxCamArray];
        public PickingCameraController[] _cameraController = new PickingCameraController[_maxCamArray];
        private int _canvasTexture;
        private int _foregroundTexture;
        private int _compositeTexture;
        private List<RenderObject> _renderObjects = new List<RenderObject>();
        private bool buffersInitialized = false;
        private long totalRenderedFrames = 0;
        public int activeCamera = 1;
        public int syncCamera = 1;
        public IDeckLinkMutableVideoFrame uploadFrame1 = null;//keep as separate objects to allow locking
        public IDeckLinkMutableVideoFrame uploadFrame2 = null;
        NDIReceiver _NDIReceiver;
        int dynTexture;
        Size dynTextureSize = new Size(0, 0);
        // private float _zNear = 0.01f;
        //  private float _zFar = 100.0f;
        public float zNear = 0.1f;
        public float zFar = 1000.0f;

        public const int classicGLUsedTextureTypeCount = 1;
        public const int modernGLUsedTextureTypeCount = 6;  //TextureType 1..6
        public const int usedModernGLTextureTypeCount = 6;  //TextureType 1..6
        public static int[] modernGLTextureType = new int[usedModernGLTextureTypeCount];
        private const int _ModernGLTextureSize = 16;

        private Matrix4 _lightRotation = Matrix4.Identity;
        private Shader _shaderChromakey;

        public delegate void GlExtraDrawJobDelegate(object sender);

        /// <summary>
        /// This event is fired every draw frame to allow other editor
        /// components to access the Gl subsystem in a safe manner.
        /// 
        /// This event is always invoked on the single thread that is responsible
        /// for interacting with Gl.
        /// </summary>
        public event GlExtraDrawJobDelegate GlExtraDrawJob;

        private void OnGlExtraDrawJob()
        {
            var handler = GlExtraDrawJob;
            if (handler != null)
            {
                handler(this);
                // reset all event handlers - extra draw job get executed only once
                // TODO: what if handlers re-register themselves?
                GlExtraDrawJob = null;
            }
        }

        // Some colors and other tweakables
        protected Color HudColor
        {
            get { return Color.FromArgb(100, 80, 80, 80); }
        }

        protected Color BorderColor
        {
            get { return Color.DimGray; }
        }

        protected Color ActiveBorderColor
        {
            get { return Color.GreenYellow; }
        }

        protected Color BackgroundColor
        {
            get
            {
                var color = CoreSettings.CoreSettings.Default.BackgroundColor;
                var alpha = CoreSettings.CoreSettings.Default.BackgroundAlpha;
                int intAlpha = alpha.G;
                return Color.FromArgb(intAlpha, color);
            }
        }

        protected Color ActiveViewColor
        {
            // Make ActiveView slightly brighter than the default background color.
            get
            {
                var color = CoreSettings.CoreSettings.Default.BackgroundColor;
                var alpha = CoreSettings.CoreSettings.Default.BackgroundAlpha;
                int intAlpha = alpha.G;
                var offset = 10;
                return Color.FromArgb(intAlpha, Math.Min(color.R + offset, 0xff), Math.Min(color.G + offset, 0xff), Math.Min(color.B + offset, 0xff));
            }
        }

        protected float HudHoverTime
        {
            get { return 0.2f; }
        }

        /// <summary>
        /// The gl context which is being rendered to
        /// </summary>
        public RenderControl renderControl
        {
            get { return _mainWindow.renderControl; }
        }

        /// <summary>
        /// Host window
        /// </summary>
        public MainWindow MainWindow
        {
            get { return _mainWindow; }
        }

        /// <summary>
        /// Utility object in charge of maintaining all text overlays
        /// </summary>
        public TextOverlay TextOverlay
        {
            get { return _textOverlay; }
        }

        /// <summary>
        /// Obtain actual rendering resolution in pixels
        /// </summary>
        public Size RenderResolution
        {
            get { return renderControl.ClientSize; }
        }

        public Matrix4 LightRotation
        {
            get { return _lightRotation; }
        }

        public ICameraController renderingController
        {
            get { return _cameraController[activeCamera]; }
        }

        /// <summary>
        /// Construct a renderer given a valid and fully loaded MainWindow
        /// </summary>
        /// <param name="window">Main window, Load event of the GlContext
        ///    needs to be fired already.</param>
        internal Renderer(MainWindow mainWindow)
        {
            renderIO = MainWindow.useIO || renderIO;
            _mainWindow = mainWindow;
            _rendererContext = (GraphicsContext)GraphicsContext.CurrentContext;
            LoadHudImages();
            _textOverlay = new TextOverlay(this);

            renderControl.InitGlControl(NDISender.videoSizeX, NDISender.videoSizeY);
            InitBuffers();
            if (CoreSettings.CoreSettings.Default.UseTracking)
            {
                //This is stupid,TODO - resize bitmaps and move textures AND models to stream
                string modelPath = MainWindow.exePath + "\\Models\\";
                string filePath = modelPath + "vr_controller_vive_1_5\\vr_controller_vive_1_5.obj";
                _controller = File.Exists(filePath) ? new Scene(filePath, this) : null;
                filePath = modelPath + "lh_basestation_vive\\lh_basestation_vive.obj";
                filePath = modelPath + "lighthouse_ufo\\lighthouse_ufo.obj";
                _lighthouse = File.Exists(filePath) ? new Scene(filePath, this) : null;
                filePath = modelPath + "generic_hmd\\generic_hmd.obj";
                _hmd = File.Exists(filePath) ? new Scene(filePath, this) : null;
                filePath = modelPath + "standard_camera\\standard_camera.obj";
                _camera = File.Exists(filePath) ? new Scene(filePath, this) : null;
                _tracker = new VRModelCameraController(CameraMode.HMD, MathHelper.PiOver4, ScenePartMode.All);
            }
            _renderClock.Reset();
            _renderClock.Start();

            if (MainWindow.useIO)
            {
                MainWindow.capturePreview1.Show();
                MainWindow.capturePreview2.Show();
                MainWindow.outputGenerator.Show();
            }
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            RenderControl.GLInfo("ScreenCompat");

            Bitmap testBmp = new Bitmap(NDISender.videoSizeX, NDISender.videoSizeY);
            Graphics gr = Graphics.FromImage(testBmp);
            gr.Clear(Color.Beige);
            var testData = testBmp.LockBits(new Rectangle(0, 0, testBmp.Width, testBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.GenTextures(_maxCamArray, _cameraTexture);
            GL.GenTextures(1, out _canvasTexture);
            GL.GenTextures(1, out _foregroundTexture);
            GL.GenTextures(1, out _compositeTexture);

            InitializeVideoTexture(_cameraTexture[0], testData.Scan0);
            InitializeVideoTexture(_cameraTexture[1], testData.Scan0);
            InitializeVideoTexture(_cameraTexture[2], testData.Scan0);
            InitializeVideoTexture(_canvasTexture, testData.Scan0);
            InitializeVideoTexture(_foregroundTexture, testData.Scan0);
            InitializeVideoTexture(_compositeTexture, testData.Scan0);
            //check
            GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, testData.Scan0);
            testBmp.UnlockBits(testData);
            gr.Dispose();
            testBmp.Dispose();

            GL.GenTextures(modernGLUsedTextureTypeCount, modernGLTextureType);
            UploadModernGLTextures();

            _cameraController[0] = new PickingCameraController(CameraMode.HMD, MathHelper.PiOver4 * 80 / 90, ScenePartMode.Composite);
            float Fov = CoreSettings.CoreSettings.Default.FovCam1;
            if (Fov > MathHelper.PiOver4 * 150 / 90) Fov = MathHelper.PiOver4 * 80 / 90;
            if (Fov < MathHelper.PiOver4 * 30 / 90) Fov = MathHelper.PiOver4 * 80 / 90;
            _cameraController[1] = new PickingCameraController(CameraMode.Cont1, Fov, ScenePartMode.Composite);
            Fov = CoreSettings.CoreSettings.Default.FovCam2;
            if (Fov > MathHelper.PiOver4 * 150 / 90) Fov = MathHelper.PiOver4 * 80 / 90;
            if (Fov < MathHelper.PiOver4 * 30 / 90) Fov = MathHelper.PiOver4 * 80 / 90;
            _cameraController[2] = new PickingCameraController(CameraMode.Cont2, Fov, ScenePartMode.Composite);

            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
            RenderControl.GLInfo("ScreenCore");
            if (renderIO)
            {
                _shaderChromakey = Shader.FromResource("open3mod.Shaders.ChromakeyVertexShader.glsl", "open3mod.Shaders.ChromakeyFragmentShader.glsl", "");
                //   _shaderChromakey = Shader.FromResource("open3mod.Shaders.ChromakeyVertexShader.glsl", "open3mod.Shaders.EmptyFragmentShader.glsl", "");
            }
            float W = 1f;
            float H = 1f;
            float D = 0f;
            Vertex[] vertices1 =
                  {
  new Vertex(new Vector4(-W,  H, D, 1f), Color4.Yellow),
  new Vertex(new Vector4(-W, -H, D, 1f), Color4.Yellow),
  new Vertex(new Vector4( W,  H, D, 1f), Color4.Yellow),
 };
            _renderObjects.Add(new RenderObject(vertices1));
            Vertex[] vertices2 =
                  {
  new Vertex(new Vector4(-W, -H, D, 1f), Color4.White),
  new Vertex(new Vector4( W, -H, D, 1f), Color4.White),
  new Vertex(new Vector4( W,  H, D, 1f), Color4.White),//correct order so both triangles are front faced
 };
            _renderObjects.Add(new RenderObject(vertices2));
            renderControl.ReleaseRenderTargets();
        }

        private void UploadModernGLTexture(TextureType currType, Color4 c)
        {
            int index = currType - TextureType.Diffuse;
            GL.ActiveTexture(TextureUnit.Texture0); //we will bind textures to their respective units later in MaterialMapper
            Bitmap _OwnTextureBitmap = new Bitmap(_ModernGLTextureSize, _ModernGLTextureSize);
            Color color = Color.FromArgb((byte)(c.A * 255), (byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255));
            Graphics gr = Graphics.FromImage(_OwnTextureBitmap);
            gr.Clear(color);
            GL.BindTexture(TextureTarget.Texture2D, modernGLTextureType[index]);
            // _OwnTextureBitmap.SetPixel(0, 0, color);
            var ownData = _OwnTextureBitmap.LockBits(new Rectangle(0, 0, _OwnTextureBitmap.Width, _OwnTextureBitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, _OwnTextureBitmap.Width, _OwnTextureBitmap.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, ownData.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);these textures have fixed values, do not need mipmaps
            _OwnTextureBitmap.UnlockBits(ownData);
            gr.Dispose();
            _OwnTextureBitmap.Dispose();
        }

        private void UploadModernGLTextures()
        {
            Color4 color;
            color = Color4.White;
            UploadModernGLTexture(TextureType.Diffuse, color);
            color = Color4.White;
            UploadModernGLTexture(TextureType.Specular, color);
            color = Color4.White;
            UploadModernGLTexture(TextureType.Ambient, color);
            color = Color4.Black;
            UploadModernGLTexture(TextureType.Emissive, color);
            color = Color4.Gray;
            UploadModernGLTexture(TextureType.Height, color);
            color = Color4.Gray;
            UploadModernGLTexture(TextureType.Normals, color);
        }

        /// <summary>
        /// Creates and uploads texture
        /// </summary>
        private void InitializeVideoTexture(int Texture, IntPtr data)
        {
            GL.BindTexture(TextureTarget.Texture2D, Texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, NDISender.videoSizeX, NDISender.videoSizeY, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear); //Does not allow LinearMipmapLinear
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); Video textures are not moved at all, except composite, do not need MipMaps
        }

        /// <summary>
        /// Perform any non-drawing operations that need to be executed
        /// once per frame and whose implementation resides in Renderer.
        /// </summary>
        public void Update(double delta)
        {
            if (_hoverFadeInTime > 0)
            {
                _hoverFadeInTime -= (float)delta;
                _hudDirty = true;
            }
        }

        public void InitBuffers()
        {
            if (MainWindow.useIO)
            {
                _NDITimeCode = 0;
                for (int i = 0; i < NDISender.NDIchannels; i++)
                {
                    _NDISender.InitSender(streamName[i]); //must be one after another, otherwise share buffer, we do not want this
                }
                _NDISender.PrepareFrame(streamName[0], _NDITimeCode);
                _NDISender.PrepareFrame(streamName[1], _NDITimeCode);
                _NDISender.PrepareFrame(streamName[2], _NDITimeCode);
                _NDISender.PrepareFrame(streamName[3], _NDITimeCode);
                _NDISender.FlushSenders();
            }
            else
            {
                string[] inactiveStreamName = { "", "", "", "" };
                streamName = inactiveStreamName;
            }


            GL.GenBuffers(numBuffers, pixelPackBuffer);
            for (int i = 0; i < numBuffers; i++)
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[i]);
                GL.BufferData(BufferTarget.PixelPackBuffer, (IntPtr)(NDISender.videoSizeY * NDISender.stride), (IntPtr)0, BufferUsageHint.StreamRead);
            }
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            GL.GenBuffers(numBuffers, pixelUnpackBuffer);
            for (int i = 0; i < numBuffers; i++)
            {
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[i]);
                GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(NDISender.videoSizeY * NDISender.stride), (IntPtr)0, BufferUsageHint.StreamDraw);
            }
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            buffersInitialized = true;

            if (MainWindow.useIO)_NDIReceiver = new NDIReceiver();


        }

        public void DisposeBuffers()
        {
            buffersInitialized = false;
            for (int i = 0; i < modernGLUsedTextureTypeCount; i++)
            {
                GL.DeleteTexture(modernGLTextureType[i]);
            }
            if (renderIO)
            {
                _shaderChromakey.Dispose();
            }
          
            _rendererContext.MakeCurrent(renderControl.WindowInfo);
            if (MainWindow.useIO)
            {
                for (int i = 0; i < NDISender.NDIchannels; i++)
                {
                    _NDISender.DisposeSender(streamName[i]);
                }
            }
            GL.DeleteBuffers(numBuffers, pixelPackBuffer);
            GL.DeleteBuffers(numBuffers, pixelUnpackBuffer);
        }

        public void ConnectNDI(NewTek.NDI.Source newSource)
        {
            if (MainWindow.useIO) _NDIReceiver.Connect(newSource);
        }


        public void timeTrack(string ID)
        {
            double actTime = _runsw.Elapsed.TotalMilliseconds;
            timeTracking = timeTracking + "\n" + ID + " " + actTime;
            if (showTimeTrack) Console.WriteLine(ID + " " + actTime);
            if (timeTracking.Length > 1000) timeTracking = "";
            //     if (timeTrack) Console.Clear();
            RenderControl.GLError(ID);
        }
        public void timeTrack2(string ID)
        {
            double actTime = _runsw.Elapsed.TotalMilliseconds;
            timeTracking = timeTracking + "\n" + ID + " " + actTime;
            Console.WriteLine(ID + " " + actTime);
            if (timeTracking.Length > 1000) timeTracking = "";
            //     if (timeTrack) Console.Clear();
        }

        public void uploadCameraBuffer(int cam, IntPtr videoData)
        {
            if (!buffersInitialized) return;
            if (cam == 2)
            {
                if (uploadFrame2 == null) return;
                lock (uploadFrame2)
                {
                    uploadFrame2.GetBytes(out IntPtr dest);
                    uint size = (uint)(uploadFrame2.GetRowBytes() * uploadFrame2.GetHeight());
                    MainWindow.CopyMemory(dest, videoData, size);
                }
            }
            else
            {
                if (uploadFrame1 == null) return;
                lock (uploadFrame1)
                {
                    uploadFrame1.GetBytes(out IntPtr dest);
                    uint size = (uint)(uploadFrame1.GetRowBytes() * uploadFrame1.GetHeight());
                    MainWindow.CopyMemory(dest, videoData, size);
                }
            }
        }

        /// <summary>
        /// Draw the contents of a given tab to screen. If the tab contains a scene,
        /// this scene is drawn. If the tab is in loading or failed state,
        /// the corresponding info screen will be drawn.
        /// </summary>
        /// <param name="activeTab">Tab containing the scene to be drawn</param>
        public void DrawScreen(Tab activeTab)
        {
            if (activeTab.ActiveScene != null)
            {
                DrawScreenViewports(activeTab);
            }
            else
            {
                SetFullViewport();
                if (activeTab.State == Tab.TabState.Failed)
                {
                    DrawFailureSplash(activeTab.ErrorMessage);
                }
                else if (activeTab.State == Tab.TabState.Loading)
                {
                    DrawLoadingSplash();
                }
                else
                {
                    Debug.Assert(activeTab.State == Tab.TabState.Empty);
                    DrawNoSceneSplash();
                }
            }
            timeTrack("22 - BfDrawOverlay");
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            _textOverlay.Draw();
            timeTrack("23 - OverlayDrawed");

            // draw viewport finishing (i.e. contours)
            if (activeTab.ActiveScene != null && activeTab.ActiveViewMode != Tab.ViewMode.Single)
            {
                var index = Tab.ViewIndex.Index0;
                foreach (var viewport in activeTab.ActiveViews)
                {
                    // always draw the active viewport last 
                    if (viewport == null || activeTab.ActiveViewIndex == index)
                    {
                        ++index;
                        continue;
                    }

                    var view = viewport.Bounds;
                    DrawViewportPost(view.X, view.Y, view.Z, view.W, false);
                    ++index;
                }

                var activeVp = activeTab.ActiveViews[(int)activeTab.ActiveViewIndex];
                Debug.Assert(activeVp != null);
                var activeVpBounds = activeVp.Bounds;
                DrawViewportPost(activeVpBounds.X, activeVpBounds.Y, activeVpBounds.Z, activeVpBounds.W, true);
            }

            // handle other Gl jobs such as drawing preview images - components to separate FBO
            // use this event to register their jobs.
            timeTrack("24 - BfExtraDrawJob");
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            OnGlExtraDrawJob(); //render previews to separate FBO
            renderControl.CopyToOnScreenFramebuffer();
            renderControl.SwapBuffers();
            timeTrack("25 - RenderFinished");
        }

        /// <summary>
        /// Draw the contents of a given tab to video framebuffer. Also scans positions and reset counters, must be called as first from all frame renderings
        /// </summary>
        /// <param name="activeTab">Tab containing the scene to be drawn</param>
        public void DrawVideo(Tab activeTab)
        {
            if ((uploadFrame1 == null) && (MainWindow.useIO)) uploadFrame1 = MainWindow.outputGenerator.CreateUploadVideoFrame();
            if ((uploadFrame2 == null) && (MainWindow.useIO)) uploadFrame2 = MainWindow.outputGenerator.CreateUploadVideoFrame();

            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoCompat);
            timeTracking = "";
            timeTrack("0 - Draw back");
            int loopDelay = (int)_runsw.Elapsed.TotalMilliseconds;
            double timeOffset = (_renderClock.Elapsed.TotalMilliseconds + MainWindow.timeOffset) % MainWindow.mainTiming;
            int waitTime = (int)(MainWindow.mainTiming - timeOffset);
            if (loopDelay + waitTime - 10 > MainWindow.mainTiming) waitTime = 0;
            if ((waitTime > 2) && (waitTime < MainWindow.mainTiming) && CoreSettings.CoreSettings.Default.KeepTimingRight) Thread.Sleep((int)waitTime);
            _runsw.Reset();
            _runsw.Start();
            int sendDelay = (int)_runsw.Elapsed.TotalMilliseconds;
            totalRenderedFrames++;
            if (activeTab.ActiveScene != null)
            {
                OpenVRInterface.ScanPositions();
                shift = 1 - shift;
                activeTab.ActiveScene.NewFrame = true;//needed for GL2.0 animations

                //upload video - BMD and NDI to GL
                if (MainWindow.useIO) UploadVideoTextures(activeTab.ActiveScene);
                if (renderIO)  RenderVideoComposite(activeTab);
            }
        }

        /// <summary>
        /// Helper to draw sync lines into output
        /// </summary>
        private void DrawSync(IntPtr videoData, long sentFrames)
        {
            Bitmap bmp = new Bitmap(NDISender.videoSizeX, NDISender.videoSizeY, NDISender.stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, videoData);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Pen lineOutlinePen = new Pen(Color.White, 10.0f);
            Pen transparentlineOutlinePen = new Pen(Color.Transparent, 100.0f);
            int exists = 2;
            switch (exists)
            {
                case 0:
                    lineOutlinePen = new Pen(Color.Red, 10.0f);
                    break;
                case 1:
                    lineOutlinePen = new Pen(Color.Blue, 10.0f);
                    break;
                case 2:
                    lineOutlinePen = new Pen(Color.Green, 10.0f);
                    break;
                case 3:
                    lineOutlinePen = new Pen(Color.Yellow, 10.0f);
                    break;
            };
            int lineHeight = NDISender.videoSizeY / 4;
            int a = (int)(sentFrames * 20) % NDISender.videoSizeX;
            Point pt1 = new Point(a, (NDISender.videoSizeY - (lineHeight * (exists - 1))));
            Point pt2 = new Point(a, NDISender.videoSizeY - lineHeight * (exists));
            Point pt3 = new Point(a, 0);
            Point pt4 = new Point(a, NDISender.videoSizeY);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawLine(transparentlineOutlinePen, pt3, pt4);
            graphics.DrawLine(lineOutlinePen, pt1, pt2);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.Dispose();
            bmp.Dispose();
        }

        /// <summary>
        /// Moves texture sources to buffers and previous buffers to GL textures
        /// </summary>
        private void UploadVideoTextures(Scene scene)
        {
            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoCompat);//need to make context current
            int YUVwidth = NDISender.videoSizeX / 2;
            uint YUVstride = (uint)NDISender.stride / 2;

            //upload camera 1 from buffer to GL
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[1 - shift]);
            GL.BindTexture(TextureTarget.Texture2D, _cameraTexture[1]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, YUVwidth, NDISender.videoSizeY, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, (IntPtr)0);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

            //upload camera 2 from buffer to GL
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[3 - shift]);
            GL.BindTexture(TextureTarget.Texture2D, _cameraTexture[2]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, YUVwidth, NDISender.videoSizeY, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, (IntPtr)0);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

            //upload dynamic texture from buffer to GL
            if ((scene.dynamicTexture != null) && (_NDIReceiver.ConnectedSource != null) && (dynTextureSize.Width != 0))
            {
                dynTexture = scene.dynamicTexture.Gl;//upload from buffer to texture
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[5 - shift]);
                GL.BindTexture(TextureTarget.Texture2D, dynTexture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, dynTextureSize.Width, dynTextureSize.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)0);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
            //upload camera 1 from videoframe to buffer
            lock (uploadFrame1)
            {
                uploadFrame1.GetBytes(out IntPtr dest);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[shift]);
                GL.BufferData(BufferTarget.PixelUnpackBuffer, (int)YUVstride * NDISender.videoSizeY, dest, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }
            //upload camera 2 from videoframe to buffer
            lock (uploadFrame2)
            {
                uploadFrame2.GetBytes(out IntPtr dest);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[2 + shift]);
                GL.BufferData(BufferTarget.PixelUnpackBuffer, (int)YUVstride * NDISender.videoSizeY, dest, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }

            //upload dynamic texture from receiver bitmap to buffer
            if ((scene.dynamicTexture != null) && (_NDIReceiver.ConnectedSource != null))
            {
                if (_NDIReceiver.Received != null)
                {
                    lock (_NDIReceiver.ReceiverLock)
                    {
                        var recdData = _NDIReceiver.Received.LockBits(new Rectangle(0, 0, _NDIReceiver.Received.Width, _NDIReceiver.Received.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[4 + shift]);
                        GL.BufferData(BufferTarget.PixelUnpackBuffer, _NDIReceiver.Received.Width * _NDIReceiver.Received.Height * 4, recdData.Scan0, BufferUsageHint.DynamicDraw);
                        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                        _NDIReceiver.Received.UnlockBits(recdData);
                        dynTextureSize.Width = _NDIReceiver.Received.Width;
                        dynTextureSize.Height = _NDIReceiver.Received.Height;
                    }
                }
            }
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

        }

        /// <summary>
        /// Renders composite video to Framebuffer
        /// </summary>
        private void RenderVideoComposite(Tab activeTab)
        {
            GL.Viewport(0, 0, NDISender.videoSizeX, NDISender.videoSizeY);
            //render canvas to FBO #2, move to SS#3 and move to texture
            renderingController.SetScenePartMode(ScenePartMode.GreenScreen);
            DrawVideoViewport(renderingController, activeTab);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoSSCompat);
            GL.BindTexture(TextureTarget.Texture2D, _canvasTexture);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _canvasTexture, 0);
            renderControl.CopyVideoFramebuffers(1, 2);//from MS to SS
            renderControl.ReBindBuffer(2);
            // Bitmap testBmp = renderControl.ReadVideoTextureTest();
            // testBmp.Dispose();

            //render frgd to FBO #2, move to SS#3 and move to texture
            renderingController.SetScenePartMode(ScenePartMode.Foreground);
            DrawVideoViewport(renderingController, activeTab);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoSSCompat);
            GL.BindTexture(TextureTarget.Texture2D, _foregroundTexture);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _foregroundTexture, 0);
            renderControl.CopyVideoFramebuffers(1, 2);//from MS to SS
            renderControl.ReBindBuffer(2);

            /*                  Bitmap compare = (Bitmap)Image.FromFile("e:\\vr-software\\REC\\10bit.bmp");
                              //  Bitmap compare = (Bitmap)Image.FromFile("e:\\vr-software\\REC\\VertBars.bmp");
                              // Bitmap compare = (Bitmap)Image.FromFile("e:\\vr-software\\REC\\SiemensStar.bmp");
                                var recdcData = compare.LockBits(new Rectangle(0, 0, compare.Width, compare.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[4 + shift]);
                                GL.BufferData(BufferTarget.PixelUnpackBuffer, compare.Width * compare.Height * 4, recdcData.Scan0, BufferUsageHint.DynamicDraw);
                                GL.BindTexture(TextureTarget.Texture2D, _foregroundTexture);
                                compare.UnlockBits(recdcData);
                                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, NDISender.videoSizeX, NDISender.videoSizeY, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
                                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                                compare.Dispose();
                */
            //render bkgd to #2
            RenderControl.GLError("BgStart");
            renderingController.SetScenePartMode(ScenePartMode.Background);
            renderControl.SetRenderTarget((RenderControl.RenderTarget)((int)(RenderControl.RenderTarget.VideoCompat) + (int)GraphicsSettings.Default.RenderingBackend));
            DrawVideoViewport(renderingController, activeTab);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoSSCompat);
            renderControl.CopyVideoFramebuffers(1, 2);//from MS to SS

            // and chromakey+fgd over
            renderingController.SetScenePartMode(ScenePartMode.Composite);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoSSCore);
            GL.Viewport(0, 0, NDISender.videoSizeX, NDISender.videoSizeY);
            DrawChromakey(renderingController, false);

            //testBmp = renderControl.ReadFramebufferTest();
            //testBmp.Dispose();

            //and immediatelly start transfer to CPU memory / buffer
            renderControl.SetRenderTarget((RenderControl.RenderTarget)((int)(RenderControl.RenderTarget.VideoSSCompat) + (int)GraphicsSettings.Default.RenderingBackend));
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[2 * 0 + 1 - shift]);
            GL.ReadPixels(0, 0, NDISender.videoSizeX, NDISender.videoSizeY, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)0);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            if (CoreSettings.CoreSettings.Default.SendNDI)
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[2 * 1 + 1 - shift]);
                GL.ReadPixels(0, 0, NDISender.videoSizeX, NDISender.videoSizeY, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)0);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }

            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoSSCore);
            renderControl.CopyVideoFramebuffers(2, 1);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoSSCore);
            GL.BindTexture(TextureTarget.Texture2D, _compositeTexture);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _compositeTexture, 0);
            renderControl.CopyVideoFramebuffers(1, 2);
            renderControl.ReBindBuffer(2);
            timeTrack("16 - Composite reread");
        }


        /// <summary>
        /// Outputs buffer 0 to BMD and 0+1 to NDI, if ON
        /// </summary>
        public void OutputVideo(int waitTime)
        {
            IntPtr src = (IntPtr)0;
            IntPtr[] srcs = new IntPtr[] { src, src, src, src };
            int lastBuffer = NDISender.NDIchannels;
            lastBuffer = 1;//only for BMD out

            if (CoreSettings.CoreSettings.Default.SendNDI) lastBuffer = 2;
            for (int i = 0; i < lastBuffer; i++)// sadly cannot be parallelized due to binding to virtually one PixelPackBuffer
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[2 * i + shift]);
                srcs[i] = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
                for (int j = 0; j < NDISender.videoSizeY; j++)
                {
                    MainWindow.CopyMemory(_NDISender.senderVideoFrameList[2 * i + shift].BufferPtr + ((NDISender.videoSizeY - j - 1) * NDISender.stride), srcs[i] + (j * NDISender.stride), (uint)NDISender.stride);
                }
                if ((i == 0) && MainWindow.outputGenerator.IsRunning())
                {
                    if (CoreSettings.CoreSettings.Default.CheckDroppedFrames) DrawSync(srcs[i], totalRenderedFrames);
                    IntPtr dest = MainWindow.outputGenerator.videoFrameBuffer();
                    for (int j = 0; j < NDISender.videoSizeY; j++)
                    {
                        MainWindow.CopyMemory(dest + ((NDISender.videoSizeY - j - 1) * NDISender.stride) + 1, srcs[i] + (j * NDISender.stride), (uint)NDISender.stride - 1);//RGBA do ARGB = 1 pixel/byte shift
                    }
                    MainWindow.outputGenerator.ScheduleNextFrame(false);
                }
                GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            }
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0); //better to have parallelized in case we mark all streams for testing, this takes 9ms each
            if (CoreSettings.CoreSettings.Default.SendNDI)
            {
                Parallel.Invoke(() =>
                {
                    _NDISender.PrepareFrame(streamName[0], _NDITimeCode, shift, waitTime * 250 / 40);// sendDelay*250 /40);//250=40ms prázná, 0=nic = plná, 
                },  //close first Action
                              () =>
                              {
                                  _NDISender.PrepareFrame(streamName[1], _NDITimeCode, shift);
                              }, //close second Action
                              () =>
                              {
                                  _NDISender.PrepareFrame(streamName[2], _NDITimeCode, shift);
                              }, //close third Action
                              () =>
                              {
                                  _NDISender.PrepareFrame(streamName[3], _NDITimeCode, shift);
                              } //close fourth Action
                          ); //close parallel.invoke

                _NDISender.senderVideoFrameList[2 * 0 + shift].TimeCode = _NDITimeCode; //frame of stream 0 is already prepared, we just update TC
                _NDITimeCode++;
                _NDISender.AllSend(shift);
            }
        }

        /// <summary>
        /// Draws 1-4 viewports to screen, active as last
        /// </summary>
        private void DrawScreenViewports(Tab activeTab)
        {
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);//core should work too, we are just cleaning here
            var cs = RenderResolution;
            if ((cs.Width != 0) && (cs.Height != 0)) //are we not minimized?
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.DepthMask(true);
                GL.ClearColor(BackgroundColor);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // draw viewport 3D contents
                var index = Tab.ViewIndex.Index0;
                foreach (var viewport in activeTab.ActiveViews)
                {
                    // always draw the active viewport last 
                    if (viewport == null || activeTab.ActiveViewIndex == index)
                    {
                        ++index;
                        continue;
                    }
                    var view = viewport.Bounds;
                    var cam = viewport.ActiveCameraControllerForView();
                    if (activeTab.ActiveViewMode != Tab.ViewMode.Single)
                    {
                        DrawViewport(cam, activeTab, view.X, view.Y, view.Z, view.W, index, false);
                    }
                    else
                    {
                        if ((int)index == 4) DrawViewport(cam, activeTab, view.X, view.Y, view.Z, view.W, index, false);
                    }
                    ++index;
                }
                var activeVp = activeTab.ActiveViews[(int)activeTab.ActiveViewIndex];
                Debug.Assert(activeVp != null);
                var activeVpBounds = activeVp.Bounds;
                if (activeTab.ActiveViewMode != Tab.ViewMode.Single)
                {
                    DrawViewport(activeVp.ActiveCameraControllerForView(),
                             activeTab,
                             activeVpBounds.X,
                             activeVpBounds.Y,
                             activeVpBounds.Z,
                             activeVpBounds.W, activeTab.ActiveViewIndex, true);
                }
                else
                {
                    if ((int)activeTab.ActiveViewIndex == 4) DrawViewport(activeVp.ActiveCameraControllerForView(),
                            activeTab,
                            activeVpBounds.X,
                            activeVpBounds.Y,
                            activeVpBounds.Z,
                            activeVpBounds.W, activeTab.ActiveViewIndex, true);
                }
                SetFullViewport();
                if (MainWindow.UiState.ShowFps)
                {
                    DrawFpsFov();
                }
                if (!_mainWindow.IsDraggingViewportSeparator)
                {
                    if (!_hudHidden)
                    {
                        DrawHud();
                    }
                }
                else
                {
                    _textOverlay.WantRedrawNextFrame = true;
                    _hudHidden = true;
                }
            }
        }

        /// <summary>
        /// Draw a string with four shifted copies of itself (shadow)
        /// </summary>
        /// <param name="graphics">graphics context</param>
        /// <param name="s">string to be drawn</param>
        /// <param name="font"></param>
        /// <param name="rect">graphics.DrawString() draw rectangle</param>
        /// <param name="main">text color</param>
        /// <param name="shadow">color of the text's shadow (make partially transparent at your leisure)</param>
        /// <param name="format">graphics.DrawString() formatting info</param>
        private void DrawShadowedString(Graphics graphics, String s, Font font, RectangleF rect, Color main, Color shadow,
                        StringFormat format)
        {
            using (var sb = new SolidBrush(main))
            {
                using (var sb2 = new SolidBrush(shadow))
                {
                    for (int xd = -1; xd <= 1; xd += 2)
                    {
                        for (int yd = -1; yd <= 1; yd += 2)
                        {
                            var rect2 = new RectangleF(rect.Left + xd,
                               rect.Top + yd,
                               rect.Width,
                               rect.Height);

                            graphics.DrawString(s, font, sb2, rect2, format);
                        }
                    }
                    graphics.DrawString(s, font, sb, rect, format);
                }
            }
        }

        private bool _hudViewWasActive = false;

        /// <summary>
        /// Draw HUD (camera panel) at the viewport that the mouse is currently hovering over
        /// </summary>
        private void DrawHud()
        {
            // sanity check whether the _hudViewIndex is ok 
            var ui = MainWindow.UiState.ActiveTab;
            if (ui.ActiveViews[(int)_hoverViewIndex] == null)
            {
                return;
            }

            var x1 = _hoverViewport.X;
            var y1 = _hoverViewport.Y;
            var x2 = _hoverViewport.Z;
            var y2 = _hoverViewport.W;

            if (!_hudDirty)
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                _hudDirty = x1 != _lastActiveVp[0] || y1 != _lastActiveVp[1] || x2 != _lastActiveVp[2] ||
                            y2 != _lastActiveVp[3];
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }

            // hack to make sure the HUD is redrawn if the active camera mode changed without the
            // user hovering over the HUD area. This can happen when auto-changing from one of the
            // axis-lock-in modes to orbit mode.
            var newMode = ui.ActiveCameraControllerForView(_hoverViewIndex).GetCameraMode();
            if (newMode != _lastHoverViewCameraMode)
            {
                _hudDirty = true;
            }
            if (ui.ActiveCameraControllerForView(_hoverViewIndex).GetScenePartMode() > ScenePartMode.All) return; //Camera/Keying/Composite mode - fixed controller


                bool hudViewIsActive = _hoverViewIndex == ui.ActiveViewIndex;
            if (hudViewIsActive != _hudViewWasActive)
            {
                _hudDirty = true;
            }
            _hudViewWasActive = hudViewIsActive;

            _lastHoverViewCameraMode = newMode;

            _lastActiveVp[0] = x1;
            _lastActiveVp[1] = y1;
            _lastActiveVp[2] = x2;
            _lastActiveVp[3] = y2;

            if (!_textOverlay.WantRedraw)
            {
                if (_hudDirty)
                {
                    _textOverlay.WantRedrawNextFrame = true;
                }
                return;
            }

            _hudDirty = false;

            LoadHudImages();
            Debug.Assert(_hudImages != null);

            var graphics = _textOverlay.GetDrawableGraphicsContext();
            if (graphics == null)
            {
                return;
            }

            var xPoint = 3 + (int)(x2 * (double)RenderResolution.Width);
            var yPoint = (int)((1.0f - y2) * (double)RenderResolution.Height); // note: y is flipped

            //padding if bounds are drawn
            if (ui.ActiveViewMode != Tab.ViewMode.Single)
            {
                xPoint -= 3;
                yPoint += 3;
            }

            const int xSpacing = 4;

            var imageWidth = _hudImages[0, 0].Width;
            var imageHeight = _hudImages[0, 0].Height;

            var regionWidth = imageWidth * _hudImages.GetLength(0) + xSpacing * (_hudImages.GetLength(0) - 1);
            const int regionHeight = 27;

            if (regionWidth > (x2 - x1) * RenderResolution.Width || regionHeight > (y2 - y1) * RenderResolution.Height)
            {
                return;
            }

            xPoint -= regionWidth;
            _hoverRegion = new Rectangle(xPoint, yPoint, regionWidth - 2, regionHeight);
            if (_hoverFadeInTime > 0.0f)
            {
                var cm = new ColorMatrix();
                var ia = new ImageAttributes();
                cm.Matrix33 = 1.0f - _hoverFadeInTime / HudHoverTime;
                ia.SetColorMatrix(cm);

                graphics.DrawImage(_hudBar, _hoverRegion, 0, 0, _hudBar.Width, _hudBar.Height, GraphicsUnit.Pixel, ia);
            }
            else
            {
                graphics.DrawImage(_hudBar, _hoverRegion);
            }

            // draw reset info stringin the upper left corner
            if (_hoverViewIndex == ui.ActiveViewIndex)
            {
                var format = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Far };
                var rect = new RectangleF(x1 * RenderResolution.Width - 150,
                    (1 - y2) * RenderResolution.Height + 35,
                    (x2 - x1) * RenderResolution.Width,
                    (y2 - y1) * RenderResolution.Height);

                DrawShadowedString(graphics, " Press [R] to reset the view", MainWindow.UiState.DefaultFont10,
                    rect, Color.Black, Color.FromArgb(50, Color.White), format);
            }

            // draw all the buttons on the HUD
            xPoint += _hudImages.GetLength(0) / 2;
            for (var i = 0; i < _hudImages.GetLength(0); ++i)
            {
                var x = xPoint;
                var y = yPoint + 4;
                var w = (int)(imageWidth * 2.0 / 3);
                var h = (int)(imageHeight * 2.0 / 3);

                if (_processHudClick &&
                    _mouseClickPos.X > x && _mouseClickPos.X <= x + w &&
                    _mouseClickPos.Y > y && _mouseClickPos.Y <= y + h)
                {
                    _processHudClick = false;

                    ui.ChangeCameraModeForView(_hoverViewIndex, (CameraMode)i);
                    //Debug.Assert(ui.ActiveCameraControllerForView(_hudViewIndex).GetCameraMode() == (CameraMode)i);
                }

                // normal image
                var imageIndex = 0;
                bool inside = _mousePos.X > x && _mousePos.X <= x + w && _mousePos.Y > y && _mousePos.Y <= y + h;

                if (ui.ActiveCameraControllerForView(_hoverViewIndex).GetCameraMode() == (CameraMode)i)
                {
                    // selected image
                    imageIndex = 2;
                }
                else if (inside)
                {
                    // hover image
                    imageIndex = 1;
                }
                if (inside)
                {
                    // draw tooltip
                    Debug.Assert(i < DescTable.Length);

                    var format = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Far };
                    var rect = new RectangleF(x1 * RenderResolution.Width,
                        (1 - y2) * RenderResolution.Height + 35,
                        (x2 - x1) * RenderResolution.Width - 10,
                        (y2 - y1) * RenderResolution.Height - 2);

                    DrawShadowedString(graphics, DescTable[i], MainWindow.UiState.DefaultFont10,
                        rect, Color.Black, Color.FromArgb(50, Color.White), format);
                }
                var img = _hudImages[i, imageIndex];
                graphics.DrawImage(img, new Rectangle(x, y, w, h), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, null);
                xPoint += imageWidth;
            }
        }

        public void OnMouseMove(MouseEventArgs mouseEventArgs, Vector4 viewport, Tab.ViewIndex viewIndex)
        {
            _mousePos = mouseEventArgs.Location;
            if (_mousePos.X > _hoverRegion.Left && _mousePos.X <= _hoverRegion.Right &&
                _mousePos.Y > _hoverRegion.Top && _mousePos.Y <= _hoverRegion.Bottom)
            {
                _hudDirty = true;
            }
            if (viewport == _hoverViewport)
            {
                return;
            }
            _hudDirty = true;
            _hoverViewport = viewport;
            _hoverViewIndex = viewIndex;
            _hoverFadeInTime = HudHoverTime;
            _hudHidden = false;
        }


        public void OnMouseClick(MouseEventArgs mouseEventArgs, Vector4 viewport, Tab.ViewIndex viewIndex)
        {
            // a bit hacky - the click is processed by the render routine. But this is by
            // far the simplest way to get this done without duplicating code.
            if (_mousePos.X > _hoverRegion.Left && _mousePos.X <= _hoverRegion.Right &&
                _mousePos.Y > _hoverRegion.Top && _mousePos.Y <= _hoverRegion.Bottom)
            {
                _mouseClickPos = mouseEventArgs.Location;
                _processHudClick = true;
                _hudDirty = true;
            }
        }


        private static readonly string[] DescTable = new[]
        {
            "Lock on X axis",
            "Lock on Y axis",
            "Lock on Z axis",
            "Orbit view",
//            "First-person view - use WASD or arrows to move",
            "First-person view",
            // Picking view is not implemented yet
            "HMD",
            "Controller 1",
            "Controller 2"
                    };


        private static readonly string[] PrefixTable = new[]
        {
            "open3mod.Images.HUD_X",
            "open3mod.Images.HUD_Y",
            "open3mod.Images.HUD_Z",
            "open3mod.Images.HUD_Orbit",
            "open3mod.Images.HUD_FPS",
            "open3mod.Images.HUD_HMD",
            "open3mod.Images.HUD_Cont1",
            "open3mod.Images.HUD_Cont2"
        };


        private static readonly string[] PostFixTable = new[]
        {
            "_Normal",
            "_Hover",
            "_Selected"
        };

        private bool _hudHidden;
        private CameraMode _lastHoverViewCameraMode;
        private Image _hudBar;


        /// <summary>
        /// Populate _hudImages
        /// </summary>
        private void LoadHudImages()
        {
            if (_hudImages != null)
            {
                return;
            }

            _hudImages = new Image[PrefixTable.Length, 3];
            for (var i = 0; i < _hudImages.GetLength(0); ++i)
            {
                for (var j = 0; j < _hudImages.GetLength(1); ++j)
                {
                    _hudImages[i, j] = ImageFromResource.Get(PrefixTable[i] + PostFixTable[j] + ".png");
                }
            }
            _hudBar = ImageFromResource.Get("open3mod.Images.HUDBar.png");
        }


        public void Dispose()
        {
            CoreSettings.CoreSettings.Default.FovCam1 = _cameraController[1].GetFOV();
            CoreSettings.CoreSettings.Default.FovCam2 = _cameraController[2].GetFOV();
            CoreSettings.CoreSettings.Default.Save();
            if (_camera != null) _camera.Dispose();
            if (_lighthouse != null) _lighthouse.Dispose();
            if (_controller != null) _controller.Dispose();
            if (_hmd != null) _hmd.Dispose();
            DisposeBuffers();
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            _textOverlay.Dispose();
        }


        /// <summary>
        /// Respond to window changes. Users normally do not need to call this.
        /// </summary>
        public void Resize()
        {
            renderControl.ResizeGlControl(RenderResolution.Width, RenderResolution.Height, 0, 0);
            _textOverlay.Resize();
        }


        /// <summary>
        /// Draw a scene to a viewport using an ICameraController to specify the camera.
        /// </summary>
        /// <param name="view">Active cam controller for this viewport</param>
        /// <param name="activeTab">Scene to be drawn</param>
        /// <param name="xs">X-axis starting point of the viewport in range [0,1]</param>
        /// <param name="ys">Y-axis starting point of the viewport in range [0,1]</param>
        /// <param name="xe">X-axis end point of the viewport in range [0,1]</param>
        /// <param name="ye">X-axis end point of the viewport in range [0,1]</param>
        /// <param name="active"></param>
        private void DrawViewport(ICameraController view, Tab activeTab, double xs, double ys, double xe,
                                  double ye, Tab.ViewIndex index, bool active)
        {
            // update viewport 
            var w = (double)RenderResolution.Width;
            var h = (double)RenderResolution.Height;
            var vw = (int)((xe - xs) * w);
            var vh = (int)((ye - ys) * h);
            if ((vw != 0) && (vh != 0)) //do not bother with "invisible" viewports
            {
                //renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);//returning from extra draw job may cause problem
                GL.Viewport((int)(xs * w), (int)(ys * h), vw, vh);
                if ((view.GetScenePartMode() > ScenePartMode.All)&& renderIO)
                {
                    renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
                    DrawChromakey(view, true);
                    if ((OpenVRInterface.EVRerror == EVRInitError.None) && (MainWindow.UiState.ShowVRModels)) DrawVRModels(_cameraController[activeCamera]);
                }
                else
                {
                    DrawViewportColorsPre(active);
                    if (activeTab.ActiveScene != null)
                    {
                        DrawScene(activeTab.ActiveScene, view, 0);//contains Video/Screen/Core/Compat switch and back
                    }
                    if ((OpenVRInterface.EVRerror == EVRInitError.None) && (MainWindow.UiState.ShowVRModels)) DrawVRModels(view);
                }
                RenderControl.GLError("ViewportEnd");
            }
        }

        /// <summary>
        /// Draw chromakeyed composition of camera and foreground over already rendered background
        /// </summary>

        public void DrawChromakey(ICameraController view, bool useCompositeTexture)
        {
            //we expect viewport size, view part+mode & video/screen context already being set
            int md = 0;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _canvasTexture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _cameraTexture[activeCamera]);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, _cameraTexture[3 - activeCamera]);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, _foregroundTexture);
            switch (view.GetScenePartMode())
            {
                case ScenePartMode.Camera: md = 3; break;
                case ScenePartMode.CameraCancelColor: md = 2; break;
                case ScenePartMode.Keying: md = 1; break; 
                case ScenePartMode.Composite:
                    md = 0;
                    if (useCompositeTexture)
                    { //replace foreground with composite - it overlays full viewport
                      GL.ActiveTexture(TextureUnit.Texture3);
                      GL.BindTexture(TextureTarget.Texture2D, _compositeTexture);
                    }
                    break;
                default: break;
            }
            int _programChromakey = _shaderChromakey.Program;
            GL.UseProgram(_programChromakey);
            Matrix4 identity = Matrix4.Identity;
            GL.UniformMatrix4(20, false, ref identity);
            GL.UniformMatrix4(21, false, ref identity);

            int[] CurrentViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, CurrentViewport);
            var cs = new Size(CurrentViewport[2], CurrentViewport[3]);
            if ((float)cs.Width / cs.Height > (float)16 / 9)
            {
                cs.Width = (int)((float)cs.Height * 16 / 9);
            }
            else
            {
                cs.Height = (int)((float)cs.Width * 9 / 16);
            }
            int dw = (CurrentViewport[2] - cs.Width) / 2 + CurrentViewport[0];
            int dh = (CurrentViewport[3] - cs.Height) / 2 + CurrentViewport[1];
            Vector3 bgc = new Vector3(((float)GraphicsSettings.Default.KeyingColorH) / 360, ((float)GraphicsSettings.Default.KeyingColorS) / 100, ((float)GraphicsSettings.Default.KeyingColorV / 100)); //90,150,80 - 165,105,175
            Vector3 wk = new Vector3(8.0f, 2.0f, (float)GraphicsSettings.Default.KeyingVSensitivity / 50);
            float tk = (100 - (float)GraphicsSettings.Default.KeyingSoftness) / 50;
            float sensit = ((float)GraphicsSettings.Default.KeyingTreshold) / 50 + tk/3;
            wk = wk * sensit;
            int blur = GraphicsSettings.Default.KeyingMatteBlur / 20;
            float pc = ((float)GraphicsSettings.Default.CancelColorPower) / 100;

            _shaderChromakey.SetInt("iMask", 0);
            _shaderChromakey.SetInt("iYUYVtex", 1);
            _shaderChromakey.SetInt("iYUYVtex2", 2);
            _shaderChromakey.SetInt("iForeground", 3);
            _shaderChromakey.SetInt2("iViewportSize", cs.Width, cs.Height);
            _shaderChromakey.SetInt2("iViewportStart", dw, dh);
            _shaderChromakey.SetVec3("iBackgroundColorHSV", bgc);
            _shaderChromakey.SetVec3("iWeightsKeying", wk);
            _shaderChromakey.SetFloat("iTresholdKeying", tk);
            _shaderChromakey.SetFloat("iPowerCanceling", pc);
            _shaderChromakey.SetInt("iMode", md);
            _shaderChromakey.SetInt("iMatteBlur", blur);
            if (GraphicsSettings.Default.CancelColorRange)
                _shaderChromakey.SetInt("iWellDone", 1);
            else
                _shaderChromakey.SetInt("iWellDone", 0);

            //GL.VertexAttrib1(2, (float)_time);
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.DepthMask(false);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            foreach (var renderObject in _renderObjects)
            {
                renderObject.Bind();
                renderObject.Render();
            }
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
            GL.UseProgram(0);
        }

        public void RecreateRenderingBackend()
        {
            if (_camera !=null) _camera.RecreateRenderingBackend();
            if (_lighthouse != null) _lighthouse.RecreateRenderingBackend();
            if (_controller != null) _controller.RecreateRenderingBackend();
            if (_hmd != null) _hmd.RecreateRenderingBackend();
        }

        /// <summary>
        /// Draw a scene to a virtual full HD viewport using an ICameraController to specify the camera.
        /// </summary>
        /// <param name="view">Active cam controller for this viewport</param>
        /// <param name="activeTab">Scene to be drawn</param>
        /// <param name="active"></param>
        private void DrawVideoViewport(ICameraController view, Tab activeTab)
        {
            if (activeTab.ActiveScene != null)
            {
                DrawScene(activeTab.ActiveScene, view, 1);
            }
        }

        private void DrawVRModels(ICameraController view)
        {
            {
                if (_tracker == null) return;
                if  (OpenVR.System == null) return;
                var modelView = new Matrix4();
                var modelPosition = new Matrix4();
                int toVideo = 0;
                for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
                {

                    modelPosition = OpenVRInterface.trackedPositions[i];
                    modelView = modelPosition * view.GetViewNoOffset();
                    _tracker.SetView(modelView);
                    _tracker.SetParam(view.GetFOV(), ScenePartMode.All, CameraMode.HMD);
                    switch (OpenVR.System.GetTrackedDeviceClass(i))
                    //  switch ((ETrackedDeviceClass)i) //just for testing when VR not available
                    {
                        case ETrackedDeviceClass.HMD:
                            if ((view.GetCameraMode() != CameraMode.HMD) && (_hmd != null)) DrawScene(_hmd, _tracker, toVideo, true); //Do not draw when you see it in front of view
                            break;
                        case ETrackedDeviceClass.Controller:
                            if ((OpenVRInterface.displayOrder[1] == i) && (view.GetCameraMode() == CameraMode.Cont1)) break;//Do not draw when you see it in front of view
                            if ((OpenVRInterface.displayOrder[2] == i) && (view.GetCameraMode() == CameraMode.Cont2)) break;//Do not draw when you see it in front of view
                            if (_controller != null) DrawScene(_controller, _tracker, toVideo, true);
                            modelPosition = OpenVRInterface.trackerToCamera[i] * OpenVRInterface.trackedPositions[i];
                            modelView = modelPosition * view.GetViewNoOffset();
                            _tracker.SetView(modelView);
                            if (_camera != null) DrawScene(_camera, _tracker, toVideo, true);
                            break;
                        case ETrackedDeviceClass.GenericTracker:
                            if (_lighthouse != null) DrawScene(_lighthouse, _tracker, toVideo, true);
                            break;
                        case ETrackedDeviceClass.TrackingReference:
                            if (_lighthouse != null) DrawScene(_lighthouse, _tracker, toVideo, true);
                            break;
                        default:
                            //Debug.Assert(false);
                            break;
                    }
                }
            }
        }
        private void DrawViewportPost(double xs, double ys, double xe,
                                  double ye, bool active = false)
        {
            // update viewport 
            var w = (double)RenderResolution.Width;
            var h = (double)RenderResolution.Height;
            var vw = (int)((xe - xs) * w);
            var vh = (int)((ye - ys) * h);
            GL.Viewport((int)(xs * w), (int)(ys * h), (int)((xe - xs) * w), (int)((ye - ys) * h));
            if ((w != 0) && (h != 0)) DrawViewportColorsPost(active, vw, vh);
        }

        private void SetFullViewport()
        {
            GL.Viewport(0, 0, RenderResolution.Width, RenderResolution.Height);
        }

        private void DrawViewportColorsPre(bool active)
        {
            if (!active)
            {
                return;
            }
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            // paint the active viewport in a slightly different shade of gray,
            // overwriting the initial background color.
            GL.Color4(ActiveViewColor);
            GL.Rect(-1, -1, 1, 1);
        }

        private void DrawViewportColorsPost(bool active, int width, int height)
        {
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);

            var texW = 1.0 / width;
            var texH = 1.0 / height;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            var lineWidth = active ? 4 : 3;
            GL.Disable(EnableCap.Lighting);

            // draw contour line
            GL.LineWidth(lineWidth);
            GL.Color4(active ? ActiveBorderColor : BorderColor);

            var xofs = lineWidth * 0.5 * texW;
            var yofs = lineWidth * 0.5 * texH;

            GL.Begin(OpenTK.Graphics.OpenGL.PrimitiveType.LineStrip);
            GL.Vertex2(-1.0 + xofs, -1.0 + yofs);
            GL.Vertex2(1.0 - xofs, -1.0 + yofs);
            GL.Vertex2(1.0 - xofs, 1.0 - yofs);
            GL.Vertex2(-1.0 + xofs, 1.0 - yofs);
            GL.Vertex2(-1.0 + xofs, -1.0 + yofs);
            GL.End();

            GL.LineWidth(1);
            GL.MatrixMode(MatrixMode.Modelview);

        }


        private void DrawScene(Scene scene, ICameraController view, int toVideo = 0, bool VRModel = false)
        {
            int sw = (int)GraphicsSettings.Default.RenderingBackend + 2 * (int)toVideo;
            switch (sw)
            {
                case 0:
                    renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
                    break;
                case 1:
                    renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
                    break;
                case 2:
                    renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoCompat);
                    break;
                case 3:
                    renderControl.SetRenderTarget(RenderControl.RenderTarget.VideoCore);
                    break;
            }
            if (toVideo == 1)
            {
                GL.DepthMask(true);
                GL.Viewport(0, 0, NDISender.videoSizeX, NDISender.videoSizeY);
     /*slow??*/           GL.ClearColor(BackgroundColor);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            }
            Debug.Assert(scene != null);
            if (GraphicsSettings.Default.RenderingBackend == 0) toVideo = 0;//for GL2 we cannot use toVideoFlag, too slow then
            scene.Render(MainWindow.UiState, view, this, toVideo, VRModel);
            GL.UseProgram(0);
        }


        private void DrawNoSceneSplash()
        {
            var graphics = _textOverlay.GetDrawableGraphicsContext();
            if (graphics == null)
            {
                return;
            }

            var format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            graphics.DrawString("Open file\n(Dragging is off)", MainWindow.UiState.DefaultFont16,
                                new SolidBrush(Color.Black),
                                new RectangleF(0, 0, renderControl.Width, renderControl.Height),
                                format);
        }


        private void DrawLoadingSplash()
        {
            var graphics = _textOverlay.GetDrawableGraphicsContext();
            if (graphics == null)
            {
                return;
            }

            var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

            graphics.DrawString("Loading ...", MainWindow.UiState.DefaultFont16,
                                new SolidBrush(Color.Black),
                                new RectangleF(0, 0, renderControl.Width, renderControl.Height),
                                format);
        }


        private void DrawFailureSplash(string message)
        {
            var graphics = _textOverlay.GetDrawableGraphicsContext();
            if (graphics == null)
            {
                return;
            }

            var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

            // hack: re-use the image we use for failed texture imports :-)
            var img = TextureThumbnailControl.GetLoadErrorImage();

            graphics.DrawImage(img, renderControl.Width / 2 - img.Width / 2, renderControl.Height / 2 - img.Height - 30, img.Width,
                               img.Height);
            graphics.DrawString("Sorry, this scene failed to load.", MainWindow.UiState.DefaultFont16,
                                new SolidBrush(Color.Red),
                                new RectangleF(0, 0, renderControl.Width, renderControl.Height),
                                format);

            graphics.DrawString("What the importer said went wrong: " + message, MainWindow.UiState.DefaultFont12,
                                new SolidBrush(Color.Black),
                                new RectangleF(0, 100, renderControl.Width, renderControl.Height),
                                format);
        }


        private void DrawFpsFov()
        {
            float DeltaUpdate = 0.3333f;
            DeltaUpdate = 0.10f;
            // only update every 1/3rd of a second
            _accTime += MainWindow.Fps.LastFrameDelta;
            if (_accTime < DeltaUpdate && !_textOverlay.WantRedraw)
            {
                if (_accTime >= DeltaUpdate)
                {
                    _textOverlay.WantRedrawNextFrame = true;
                }
                return;
            }

            if (_accTime >= DeltaUpdate)
            {
                _displayFps = MainWindow.Fps.LastFps;
                _accTime = 0.0;
            }

            var graphics = _textOverlay.GetDrawableGraphicsContext();
            if (graphics == null)
            {
                return;
            }
            int advanceMs = (int)(OpenVRInterface.fPredictedSecondsToPhotonsFromNow * 1000);
            string rb = GraphicsSettings.Default.RenderingBackend == 0 ? rb = " / GL2.0" : " / GL4.5";
            string cs = MainWindow.useIO ? " / Camera #" + activeCamera.ToString() : "";
            graphics.DrawString("FPS: " + _displayFps.ToString("000.0") + rb + cs +" / Advance: " + advanceMs + " ms ", MainWindow.UiState.DefaultFont16,
                                new SolidBrush(Color.Red), 5, 5);
            ICameraController cam;
            var indexFOV = Tab.ViewIndex.Index0;
            foreach (var viewport in MainWindow.UiState.ActiveTab.ActiveViews)
            {
                    if (viewport !=null)
                {
                var x1 = viewport.Bounds.X;
                var y1 = viewport.Bounds.Y;
                var x2 = viewport.Bounds.Z;
                var y2 = viewport.Bounds.W;
                cam = MainWindow.UiState.ActiveTab.ActiveCameraControllerForView(indexFOV);
                    var rectFOV = new RectangleF(x1 * RenderResolution.Width + 10,
                        (1 - y2) * RenderResolution.Height,
                        (x2 - x1) * RenderResolution.Width + 5,
                        (y2 - y1) * RenderResolution.Height - 7);
                var formatFOV = new StringFormat { LineAlignment = StringAlignment.Far, Alignment = StringAlignment.Near };
                int valueFOV = (int)(cam.GetFOV() * 360 / Math.PI);
                    if ((cam.GetScenePartMode() == ScenePartMode.Camera)|| (cam.GetScenePartMode() == ScenePartMode.Composite))
                    {
                        valueFOV = (int)(renderingController.GetFOV() * 360 / Math.PI);
                    }
                string currStreamName = "";
                    if ((int)indexFOV < streamName.Count()) { currStreamName = streamName[((int)indexFOV)]; } else currStreamName = "";
                 string strScenePartMode = cam.GetScenePartMode().ToString();
                    //Tab.ViewIndex index
                    switch (cam.GetCameraMode())
                    {
                        case CameraMode.Fps:
                        case CameraMode.X:
                        case CameraMode.Y:
                        case CameraMode.Z:
                        case CameraMode.Orbit:
                            DrawShadowedString(graphics, currStreamName + " | FOV: " + valueFOV.ToString() + " | "+strScenePartMode, MainWindow.UiState.DefaultFont16, rectFOV, Color.Black, Color.FromArgb(10, Color.White), formatFOV);
                            break;
                        case CameraMode.HMD:
                        case CameraMode.Cont1:
                        case CameraMode.Cont2:
                            if (OpenVRInterface.EVRerror == EVRInitError.None)
                            {
                                DrawShadowedString(graphics, currStreamName + " | FOV: " + valueFOV.ToString() + " | " + strScenePartMode, MainWindow.UiState.DefaultFont16, rectFOV, Color.Black, Color.FromArgb(10, Color.White), formatFOV);
                            }
                            else
                            {
                                DrawShadowedString(graphics, currStreamName + " | Tracking error  | " +  "FOV: " + valueFOV.ToString() + " | " + strScenePartMode, MainWindow.UiState.DefaultFont16, rectFOV, Color.Red, Color.FromArgb(10, Color.White), formatFOV); 
                            }


                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }


                }
                ++indexFOV;
            }
        }

        public void HandleLightRotationOnMouseMove(int mouseDeltaX, int mouseDeltaY, ref Matrix4 view)
        {
            _lightRotation = _lightRotation * Matrix4.CreateFromAxisAngle(view.Column1.Xyz, mouseDeltaX * 0.005f);
            _lightRotation = _lightRotation * Matrix4.CreateFromAxisAngle(view.Column0.Xyz, mouseDeltaY * 0.005f);
        }

        public void FlushNDI()
        {
            _NDISender.FlushSenders();
            _NDITimeCode = 0;
        }

    }
    public struct Vertex
    {
        public const int Size = (4 + 4) * 4; // size of struct in bytes

        private readonly Vector4 _position;
        private readonly Color4 _color;

        public Vertex(Vector4 position, Color4 color)
        {
            _position = position;
            _color = color;
        }
    }
    public class RenderObject : IDisposable
    {
        private bool _initialized;
        private readonly int _vertexArray;
        private readonly int _buffer;
        private readonly int _verticeCount;
        public RenderObject(Vertex[] vertices)
        {
            _verticeCount = vertices.Length;
            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            _buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _buffer);
            GL.NamedBufferStorage(
                   _buffer,
                   Vertex.Size * vertices.Length,        // the size needed by this buffer
                   vertices,                           // data to initialize with
                   BufferStorageFlags.MapWriteBit);    // at this point we will only write to the buffer

            GL.VertexArrayAttribBinding(_vertexArray, 0, 0);
            GL.EnableVertexArrayAttrib(_vertexArray, 0);
            GL.VertexArrayAttribFormat(
                _vertexArray,
                0,                      // attribute index, from the shader location = 0
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                0);                     // relative offsetm first item
            GL.VertexArrayAttribBinding(_vertexArray, 1, 0);
            GL.EnableVertexArrayAttrib(_vertexArray, 1);
            GL.VertexArrayAttribFormat(
                _vertexArray,
                1,                      // attribute index, from the shader location = 1
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                16);                     // relative offset after a vec4
            GL.VertexArrayVertexBuffer(_vertexArray, 0, _buffer, IntPtr.Zero, Vertex.Size);
            _initialized = true;
            RenderControl.GLError("RenderObject finish");

        }
        public void Bind()
        {
            GL.BindVertexArray(_vertexArray);
            RenderControl.GLError("RenderObject Bind");

        }
        public void Render()
        {
            GL.DrawArrays(OpenTK.Graphics.OpenGL.PrimitiveType.Triangles, 0, _verticeCount);
            RenderControl.GLError("RenderObject Render");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_initialized)
                {
                    GL.DeleteVertexArray(_vertexArray);
                    GL.DeleteBuffer(_buffer);
                    _initialized = false;
                }
            }
        }
    }

}

/* vi: set shiftwidth=4 tabstop=4: */
