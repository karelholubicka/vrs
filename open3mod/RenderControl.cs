///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [RenderControl.cs]
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace open3mod
{
    /// <summary>
    /// Derivative of GLControl to be able to specify constructor
    /// parameters while still being usable with the WinForms designer.
    /// 
    /// The RenderControl always requests a stencil buffer and a 24 bit depth
    /// buffer, which should be natively supported by most hardware in use today.
    /// 
    /// Sets up two contexts, one Compatible and one Core, both sharing one pair of Renderbuffers
    /// Render should go always to that Framebuffer(s) and before Swapbuffers we copy everything to onscreen Framebuffer
    /// RenderControl also sets separate pair of Framebuffers for offscreen rendering of video.
    /// 
    /// MultiSampling is requested according to the current value of
    /// GraphicsSettings.Default.UseMultiSampling.
    /// 
    /// </summary>
    public class RenderControl : GLControl
    {
        static int numBuffers = 3;
        int[] renderBuffer = new int[numBuffers];
        int[] depthBuffer = new int[numBuffers];
        int[,] frameBuffer = new int[2,numBuffers];
        public bool initialized = false;
        Size _videoSize; //size of Video buffers
        public const byte bytePerPixel = 4;
        GLControl glScreenCore;
        // GLControl glScreenCompat; this = this. is base context
        // IGraphicsContext screenCompatContext;
        GLControl glVideoCore;
        GLControl glVideoCompat;
        int samples = 1; //1 no MSAA, 8 max
        int isScreenCore = 0;//0 = compat,1= core;
        int isVideoCore = 0;//0 = compat,1= core;
        IGraphicsContext videoCompatContext;
        IGraphicsContext videoCoreContext;
        IGraphicsContext screenCoreContext;

        public RenderControl()
            : base(new GraphicsMode(new ColorFormat(32), 24, 8, GetSampleCount(GraphicsSettings.Default.MultiSampling)))
        {
            samples = GetSampleCount(GraphicsSettings.Default.MultiSampling);
            Thread.Sleep(10);
            glScreenCore = new OpenTK.GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, samples), 4, 5, 0);
          //  glScreenCompat = new OpenTK.GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, samples));
            glVideoCore = new OpenTK.GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, samples), 4, 5, 0);
            glVideoCompat = new OpenTK.GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, samples));
            //glCompatible GL = 2.1 (2.1.0)
        }

        /// <summary>
        /// Enumerates available rendering targets.
        /// </summary>
        /// <returns></returns>
        public enum RenderTarget
        {
            ScreenDirect = 0,
            ScreenCompat = 1,
            ScreenCore = 2,
            VideoCompat = 3,
            VideoCore = 4,
            VideoSSCompat = 5,
            VideoSSCore = 6,
            VideoNone = 7,
        }

        public void ReBindFrameBuffer(int i)
        {
            int isCore =isScreenCore;
            if (i > 0) isCore = isVideoCore;
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, frameBuffer[isCore, i]);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frameBuffer[isCore, i]);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isCore, i]);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderBuffer[i]);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthBuffer[i]);
                FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (status != FramebufferErrorCode.FramebufferComplete)
                {
                    Console.WriteLine("Error creating framebuffer: {0}", status);
                }
        }


        /// <summary>
        /// Sets up initiate values and buffers.
        /// </summary>
        /// <returns></returns>
        public void InitGlControl(int videoSizeX, int videoSizeY)
        {
            this.MakeCurrent();
            ErrorCode err = GL.GetError();//nVidia does not like something at real beginning
            RenderControl.GLError("InitializeGLControl");

            _videoSize.Width = videoSizeX;
            _videoSize.Height = videoSizeY;
            GL.GenRenderbuffers(numBuffers, renderBuffer);
            GL.GenRenderbuffers(numBuffers, depthBuffer);

            GL.Enable(EnableCap.Multisample);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[0]);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Rgba8, Width, Height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[0]);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Depth24Stencil8, Width, Height);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[1]);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Rgba8, videoSizeX, videoSizeY);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[1]);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Depth24Stencil8, videoSizeX, videoSizeY);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[2]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,  RenderbufferStorage.Rgba8, videoSizeX, videoSizeY);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[2]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,  RenderbufferStorage.Depth24Stencil8, videoSizeX, videoSizeY);

            int[] frameBufferCompat = new int[numBuffers];
            GL.GenFramebuffers(numBuffers, frameBufferCompat);

            this.MakeCurrent();
           // screenCompatContext = GraphicsContext.CurrentContext; 
           // GraphicsContext.ShareContexts = true;
            VSync = false;
            isScreenCore = 0;
            frameBuffer[isScreenCore, 0] = frameBufferCompat[0];
            ReBindFrameBuffer(0);

            glVideoCompat.MakeCurrent();
            videoCompatContext = GraphicsContext.CurrentContext;
           // GraphicsContext.ShareContexts = true;
            VSync = false;
            isVideoCore = 0;
            frameBuffer[isVideoCore, 1] = frameBufferCompat[1];
            frameBuffer[isVideoCore, 2] = frameBufferCompat[2];
            ReBindFrameBuffer(1);
            ReBindFrameBuffer(2);

            glScreenCore.MakeCurrent();
            screenCoreContext = GraphicsContext.CurrentContext;
           // GraphicsContext.ShareContexts = true;
            glScreenCore.VSync = false;
            isScreenCore = 1;
            int[] frameBufferCore = new int[numBuffers];
            GL.GenFramebuffers(numBuffers, frameBufferCore);
            frameBuffer[isScreenCore, 0] = frameBufferCore[0];
            ReBindFrameBuffer(0);

            glVideoCore.MakeCurrent();
            videoCoreContext = GraphicsContext.CurrentContext;
           // GraphicsContext.ShareContexts = true;
            glVideoCore.VSync = false;
            isVideoCore = 1;
            frameBuffer[isVideoCore, 1] = frameBufferCore[1];
            frameBuffer[isVideoCore, 2] = frameBufferCore[2];
            ReBindFrameBuffer(1);
            ReBindFrameBuffer(2);

            videoCoreContext.MakeCurrent(null);
            this.MakeCurrent();
            isScreenCore = 0;
            //detach video, set screen to compat

            initialized = true;
            RenderControl.GLError("AfterInit");

        }

        /// <summary>
        /// Changes buffer sizes in case of resizing.
        /// Video buffers remain unchanged.
        /// RenderTarget.ScreenCore setting is needed here to keep Core working.
        /// </summary>
        /// <returns></returns>
        public void ResizeGlControl(int _width, int _height, int _left, int _top)
        {
            SetRenderTarget(RenderTarget.ScreenCompat);
            Width = _width;
            Height = _height;
            Left = _left;
            Top = _top;
            GL.Viewport(Left, Top, Width, Height);
            SetRenderTarget(RenderTarget.ScreenCore);
            glScreenCore.Width = _width;
            glScreenCore.Height = _height;
            glScreenCore.Left = _left;
            glScreenCore.Top = _top;
            GL.Viewport(Left, Top, Width, Height);
            if (!initialized) return;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[0]);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Rgba8, Width, Height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[0]);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Depth24Stencil8, Width, Height);

            SetRenderTarget(RenderTarget.ScreenCompat);
            RenderControl.GLError("After Resizing");
        }

        /// <summary>
        /// Switches between all available targets.
        /// </summary>
        /// <returns></returns>
        public void SetRenderTarget(RenderTarget rt)
        {
            IGraphicsContext currentContext = GraphicsContext.CurrentContext;
            int[] CurrentViewport = new int[4];
            bool validVp = false;
            if (currentContext != null)
            {
                RenderControl.GLError("BeforeTargetChange");
                GL.GetInteger(GetPName.Viewport, CurrentViewport);
                validVp = true;
               // GL.Finish();
            }
            switch (rt)
            {
                case RenderTarget.ScreenDirect:
                    this.MakeCurrent();
                    isScreenCore = 0;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    break;
                case RenderTarget.ScreenCompat:
                    this.MakeCurrent();
                    isScreenCore = 0;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isScreenCore, 0]);
                    break;
                case RenderTarget.ScreenCore:
                    glScreenCore.MakeCurrent();
                    isScreenCore = 1;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isScreenCore, 0]);
                    break;
                case RenderTarget.VideoCompat:
                    glVideoCompat.MakeCurrent();
                    isVideoCore = 0;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isVideoCore,1]);
                    break;
                case RenderTarget.VideoCore:
                    glVideoCore.MakeCurrent();
                    isVideoCore = 1;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isVideoCore, 1]);
                    break;
                case RenderTarget.VideoSSCompat:
                    glVideoCompat.MakeCurrent();
                    isVideoCore = 0;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isVideoCore, 2]);
                    break;
                case RenderTarget.VideoSSCore:
                    glVideoCore.MakeCurrent();
                    isVideoCore = 1;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[isVideoCore, 2]);
                    break;
                case RenderTarget.VideoNone:
                    if (currentContext != null)
                    {
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                        currentContext.MakeCurrent(null);
                    }
                    isVideoCore = -1;
                    validVp = false;
                    // now is no context current, co calling GLError does not make sense
                    break;
            }
            if (validVp)
            {
                GL.Viewport(CurrentViewport[0], CurrentViewport[1], CurrentViewport[2], CurrentViewport[3]);//We copy viewport to the context we switched to..
                RenderControl.GLError("AfterTargetChange");
            }
        }

        /// <summary>
        /// Copy from shared Renderbuffer to onscreen Renderbuffer.
        /// </summary>
        /// <returns></returns>
        public void CopyToOnScreenFramebuffer()
        {
            if ((Width != 0) && (Height != 0))
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, frameBuffer[isScreenCore,0]);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.DrawBuffer(DrawBufferMode.Back);
                GL.BlitFramebuffer(Left, Top, Width, Height, Left, Top, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            }
        }

        /// <summary>
        /// Copy between video Renderbuffers.
        /// </summary>
        /// <returns></returns>
        public void CopyVideoFramebuffers(int src, int dest)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, frameBuffer[isVideoCore, src]);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frameBuffer[isVideoCore, dest]);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            GL.BlitFramebuffer(0, 0, _videoSize.Width - 1, _videoSize.Height - 1, 0, 0, _videoSize.Width - 1, _videoSize.Height - 1, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        }

        public Bitmap ReadFramebufferTest() //missing check if core/compat is to be read
        {
            Size bmpSize = new Size(Width, Height); ;
            if (GL.IsFramebuffer(frameBuffer[isVideoCore, 1])) bmpSize = _videoSize;
            if ((bmpSize.Height <= 0) || (bmpSize.Width <= 0)) return null;
            Bitmap testBmp = new Bitmap(bmpSize.Width,bmpSize.Height);
            //Graphics gr = Graphics.FromImage(testBmp);
            //gr.Clear(Color.Red);
            var testData = testBmp.LockBits(
                new Rectangle(0, 0, testBmp.Width, testBmp.Height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            RenderControl.GLError("A");

            GL.ReadPixels(0, 0, testBmp.Width, testBmp.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, testData.Scan0);
            RenderControl.GLInfo("B");
            // SetRenderTarget(RenderControl.RenderTarget.ScreenDirect);
            // GL.DrawPixels(Width, Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, testData.Scan0);
            testBmp.UnlockBits(testData);
            // testBmp.Dispose();
            return testBmp;
        }

        public Bitmap ReadVideoTextureTest()
        {
            Bitmap testBmp = new Bitmap(NDISender.videoSizeX, NDISender.videoSizeY);
            Graphics gr = Graphics.FromImage(testBmp);
            gr.Clear(Color.Beige);
            var testData = testBmp.LockBits(new Rectangle(0, 0, testBmp.Width, testBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, testData.Scan0);
            testBmp.UnlockBits(testData);
            gr.Dispose();
            return testBmp;
        }

        /// <summary>
        /// Converts a value for GraphicsSettings.Default.MultiSampling into a device-specific
        /// sample count.
        /// </summary>
        /// <param name="multiSampling">Device-independent quality level in [0,3]</param>
        /// <returns>Sample count for device</returns>
        private static int GetSampleCount(int multiSampling)
        {
            // UI names:
            /*  None
                Slight
                Normal
                Maximum
            */
            //int multiSampling = 2;
            switch (multiSampling)
            {
                case 0:
                    return 0;
                case 1:
                    return 2;
                case 2:
                    return 4;
                case 3:
                    return MaximumSampleCount();

            }
            Debug.Assert(false);
            return 0;
        }

        /// <summary>
        /// Determines the maximum number of FSAA samples supported by the hardware.
        /// </summary>
        /// <returns></returns>
        private static int MaximumSampleCount()
        {
            // http://www.opentk.com/node/2355 modified to actually work
            var highest = 0;
            var aa = 0;
            do
            {
                var mode = new GraphicsMode(32, 0, 0, aa);
                if (mode.Samples == aa && mode.Samples > highest)
                {
                    highest = mode.Samples;
                }
                aa += 2;
            } while (aa <= 8);//32 too much. looks GL tells always OK, but framebuffer cannnot be created then
            return highest;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        /// <returns></returns>
        public void Exit()
        {
            int[] frameBufferDel0 = new int[numBuffers];
            int[] frameBufferDel1 = new int[numBuffers];
            for (int i = 0; i < numBuffers; i++)
            {
                frameBufferDel0[i] = frameBuffer[0, i];
                frameBufferDel1[i] = frameBuffer[1, i];
            }
            GL.DeleteFramebuffers(1, frameBufferDel0);
            GL.DeleteFramebuffers(1, frameBufferDel1);
            GL.DeleteRenderbuffers(numBuffers, renderBuffer);
            GL.DeleteRenderbuffers(numBuffers, depthBuffer);
        }

        public static void GLInfo(string ID)
        {
            Console.WriteLine("Info ID: " + ID);
            GLError(ID);
            string ver = GL.GetString(StringName.Version);
            Console.WriteLine(ver);
            ver = GL.GetString(StringName.Renderer);
            Console.WriteLine(ver);
            ver = GL.GetString(StringName.Vendor);
            Console.WriteLine(ver);
            ver = GL.GetString(StringName.ShadingLanguageVersion);
            Console.WriteLine(ver);
        }

        public static void GLError(string where)
        {
            ErrorCode err = GL.GetError();
            if (err != ErrorCode.NoError) Console.WriteLine("Error reported at " + where + ":  " + err.ToString());
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */
