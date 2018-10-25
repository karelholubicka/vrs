using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using OpenTK.Input;
using System.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using System.Windows.Forms;


namespace WindowsFormsApp1
{
    public class MainWindow : Form
    {
        private int _programExample;
        private int _programSimple;
        private int _programChromakey;
        private int _mVertexArray;
        private double _time;
        private RenderControl renderControl;
        private Color4 _backColor = new Color4(0.1f, 0.1f, 0.3f, 1.0f);
        int videoSizeX = 1920;
        int videoSizeY = 1080;
        private System.Windows.Forms.Timer timer1;
        private System.ComponentModel.IContainer components;
        private int count = 0;
        Matrix4 _projectionMatrix = Matrix4.Identity;
        private List<RenderObject> _renderObjects = new List<RenderObject>();
        int texF;
        int texB;
        Bitmap bmpF;
        Bitmap bmpB;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnResize(EventArgs e)
        {
            //oveřit toto nastavení i pro jiná okna!!!
            int BorderY = 38;
            int BorderX = 16;
            renderControl.ResizeGlControl(Width - BorderX, Height-BorderY, 0, 0);
            CreateProjection();
        }

        protected override void OnLoad(EventArgs e)
        {
            renderControl.InitGlControl(videoSizeX, videoSizeY);
            Closed += OnClosed;
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
            _programExample = CreateProgram("example");
            _programSimple = CreateProgram("simple");
            _programChromakey = CreateProgram("chromakey");
            GL.GenVertexArrays(1, out _mVertexArray);
            GL.BindVertexArray(_mVertexArray);
            float W = 1.6f;
            float H = 0.9f;
            float D = 0f; ;
            Vertex[] vertices1 =
                  {
  new Vertex(new Vector4(-W,  H, D, 1f), Color4.Yellow),
  new Vertex(new Vector4(-W, -H, D, 1f), Color4.Yellow),
  new Vertex(new Vector4( W,  H, D, 1f), Color4.Yellow),
 };
            _renderObjects.Add(new RenderObject(vertices1));
            Vertex[] vertices2 =
                  {
  new Vertex(new Vector4( W, -H, D, 1f), Color4.White),
  new Vertex(new Vector4(-W, -H, D, 1f), Color4.White),
  new Vertex(new Vector4( W,  H, D, 1f), Color4.White),
 };
            _renderObjects.Add(new RenderObject(vertices2));

            Vertex[] verticesB =
            {
  new Vertex(new Vector4(-0.25f, 0.25f, 0.5f, 1-0f), Color4.HotPink),
  new Vertex(new Vector4( 0.0f, -0.25f, 0.5f, 1-0f), Color4.HotPink),
  new Vertex(new Vector4( 0.25f, 0.25f, 0.5f, 1-0f), Color4.HotPink),
 };
            _renderObjects.Add(new RenderObject(verticesB));
            float side = 0.2f;
            _renderObjects.Add(new RenderObject(ObjectFactory.CreateSolidCube(side, Color4.HotPink)));
            _renderObjects.Add(new RenderObject(ObjectFactory.CreateSolidCube(side, Color4.BlueViolet)));
            _renderObjects.Add(new RenderObject(ObjectFactory.CreateSolidCube(side, Color4.Red)));
            _renderObjects.Add(new RenderObject(ObjectFactory.CreateSolidCube(side, Color4.LimeGreen)));

            GL.CreateTextures(TextureTarget.Texture2D, 1, out texF);
            GL.CreateTextures(TextureTarget.Texture2D, 1, out texB);

            //loading test bitmaps
            bmpF = (Bitmap)Bitmap.FromFile("e:\\vr-software\\Color Bkgd + bars in HD.bmp");
            bmpB = (Bitmap)Bitmap.FromFile("e:\\vr-software\\PruhyTV 00-255.bmp");
            var dataF = bmpF.LockBits(
                new Rectangle(0, 0, bmpF.Width, bmpF.Height),
                    ImageLockMode.ReadWrite,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TextureStorage2D(texF, 1, SizedInternalFormat.Rgba8, bmpF.Width, bmpF.Height);
            GL.BindTexture(TextureTarget.Texture2D, texF);
            // GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, bmpF.Width, bmpF.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, dataF.Scan0);
            //Invalid operation - dunno why. Inverse GetTexImage works. Iverye loading works both:
            //GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, dataF.Scan0);
            //GL.GetTextureImage(texF, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpF.Width * bmpF.Height * RenderControl.bytePerPixel, dataF.Scan0);

            GL.TextureSubImage2D(texF, 0, 0, 0, bmpF.Width, bmpF.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, dataF.Scan0);
            bmpF.UnlockBits(dataF);
            RenderControl.GLError("BmpFLoaded");

            var dataB = bmpB.LockBits(
                new Rectangle(0, 0, bmpF.Width, bmpF.Height),
                    ImageLockMode.ReadWrite,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.BindTexture(TextureTarget.Texture2D, texB);
            GL.TextureStorage2D(texB, 1, SizedInternalFormat.Rgba8, bmpB.Width, bmpB.Height);
            GL.TextureSubImage2D(texB, 0, 0, 0, bmpB.Width, bmpB.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, dataB.Scan0);
            bmpB.UnlockBits(dataB);
            RenderControl.GLError("BmpBLoaded");

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texB);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, texF);
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            Exit();
        }

        public void Exit()
        {
            Debug.WriteLine("Exit called");
            foreach (var obj in _renderObjects)
                obj.Dispose();
            GL.DeleteVertexArrays(1, ref _mVertexArray);
            GL.DeleteProgram(_programExample);
            GL.DeleteProgram(_programSimple);
            GL.DeleteProgram(_programChromakey);
            this.renderControl.Exit();
        }

        private int CreateProgram(string shadersPrefix)
        {
            try
            {
                var program = GL.CreateProgram();
                var shaders = new List<int>();
                string sh = "Shader\\" + shadersPrefix+"VertexShader.c";
                shaders.Add(CompileShader(ShaderType.VertexShader, @sh));
                sh = "Shader\\" + shadersPrefix + "FragmentShader.c";
                shaders.Add(CompileShader(ShaderType.FragmentShader, @sh));

                foreach (var shader in shaders)
                    GL.AttachShader(program, shader);
                GL.LinkProgram(program);
                var info = GL.GetProgramInfoLog(program);
                if (!string.IsNullOrWhiteSpace(info))
                    throw new Exception($"CompileShaders ProgramLinking had errors: {info}");

                foreach (var shader in shaders)
                {
                    GL.DetachShader(program, shader);
                    GL.DeleteShader(shader);
                }
                return program;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private int CompileShader(ShaderType type, string path)
        {
            var shader = GL.CreateShader(type);
            var src = File.ReadAllText(path);
            GL.ShaderSource(shader, src);
            GL.CompileShader(shader);
            var info = GL.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(info))
                throw new Exception($"CompileShader {type} had errors: {info}");
            return shader;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.renderControl = new WindowsFormsApp1.RenderControl();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 20;
            this.timer1.Tick += new System.EventHandler(this.OnTimerTick);
            // 
            // renderControl
            // 
            this.renderControl.BackColor = System.Drawing.Color.Black;
            this.renderControl.Location = new System.Drawing.Point(24, 22);
            this.renderControl.Name = "renderControl";
            this.renderControl.Size = new System.Drawing.Size(300, 400);
            this.renderControl.TabIndex = 1;
            this.renderControl.VSync = false;
            // 
            // MainWindow
            // 
            this.ClientSize = new System.Drawing.Size(924, 481);
            this.Controls.Add(this.renderControl);
            this.Name = "MainWindow";
            this.ResumeLayout(false);

        }

        protected void OnPaintTestin(PaintEventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            Drawing.Clean(_backColor);
            Drawing.DrawViewportPre();
            Drawing.DrawViewportPost(0.2f, 0.2f, 0.8f, 0.8f, renderControl.Width, renderControl.Height);
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
            GL.UseProgram(_programSimple);
            Drawing.DrawArrays();
            GL.UseProgram(0);
            renderControl.CopyToScreenFramebuffer();
            renderControl.SwapBuffers();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            RenderControl.GLError("Paint");

            if (!renderControl.initialized) return;
            _time = (double)DateTime.Now.Millisecond / 660;

            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            Drawing.Clean(_backColor);
            Drawing.DrawViewportPre();
            RenderControl.GLError("Compatible");

            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCore);
            Matrix4 identity = Matrix4.Identity;
            var modelView = identity;
            GL.UseProgram(_programExample);
            GL.UniformMatrix4(20, false, ref _projectionMatrix);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            //    RenderControl.GLInfo("I");
            float c = 0f;
            foreach (var renderObject in _renderObjects)
            {
                renderObject.Bind();
                for (int i = 0; i < 5; i++)
                {
                    var k = i + (float)(_time * (0.05f + (0.1 * c)));
                    var t2 = Matrix4.CreateTranslation(
                        (float)(Math.Sin(k * 5f) * (c + 0.5f)),
                        (float)(Math.Cos(k * 5f) * (c + 0.5f)),
                        -3.5f);
                    var r1 = Matrix4.CreateRotationX(k * 13.0f + i);
                    var r2 = Matrix4.CreateRotationY(k * 13.0f + i);
                    var r3 = Matrix4.CreateRotationZ(k * 3.0f + i);
                    modelView = r1 * r2 * r3 * t2;
                     GL.UniformMatrix4(21, false, ref modelView);
                    renderObject.Render();
                }
                c += 0.3f;
            }

            GL.UniformMatrix4(21, false, ref identity);
            GL.UniformMatrix4(20, false, ref identity);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            foreach (var renderObject in _renderObjects)
            {
                if (_renderObjects.IndexOf(renderObject) < 2)
                {
                    renderObject.Bind();
                  //  renderObject.Render();
                }
            }

            RenderControl.GLError("Check");
            GL.UseProgram(_programChromakey);
            modelView = Matrix4.CreateTranslation(0, 0, -2);
            modelView = modelView * Matrix4.CreateRotationY((float)_time / 3);
            GL.UniformMatrix4(21, false, ref modelView);
            GL.UniformMatrix4(20, false, ref _projectionMatrix);
            //    GL.UniformMatrix4(21, false, ref identity);
            //   GL.UniformMatrix4(20, false, ref identity);
            Vector2 res = new Vector2(bmpB.Width, bmpB.Height);
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            Vector2 vp = new Vector2(viewport[2]-viewport[0], viewport[3]-viewport[1]);

            int ch0Loc = GL.GetUniformLocation(_programChromakey, "iChannel0");
            int ch1Loc = GL.GetUniformLocation(_programChromakey, "iChannel1");
            int resLoc = GL.GetUniformLocation(_programChromakey, "iTextureResolution");
            int  vpLoc = GL.GetUniformLocation(_programChromakey, "iViewportSize");
            int timeLoc = GL.GetUniformLocation(_programChromakey, "time");
            GL.Uniform1(ch0Loc, 0);
            GL.Uniform1(ch1Loc, 1);
            GL.Uniform2(resLoc, ref res);
            GL.Uniform2(vpLoc, ref vp);
            GL.VertexAttrib1(2, (float)_time);

            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.DepthMask(false);


            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            foreach (var renderObject in _renderObjects)
            {
                if (_renderObjects.IndexOf(renderObject) < 2)
                {
                    renderObject.Bind();
                    renderObject.Render();
                }
            }
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
            RenderControl.GLError("EndChromakey");

            GL.UseProgram(_programSimple);
            //   GL.VertexAttrib4(0, 1d, 0.5d, 0.5d, 1d);
            //  GL.VertexAttrib4(1, 0.5d, 0.5d, 1d, 1d);
            GL.VertexAttrib1(2, (float)_time);
            GL.PointSize(20);
            GL.DrawArrays(PrimitiveType.Points, 0, 1);
                
            renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            Drawing.DrawViewportPost(0.2f, 0.2f, 0.2f + 0.5f * _time/5, 0.8f, renderControl.Width, renderControl.Height);

            renderControl.CopyToScreenFramebuffer();
            renderControl.SwapBuffers();
            count++;
            GL.UseProgram(0);

            //Console.WriteLine(count.ToString()+"  -  "+ _time.ToString());
        }

        private void CreateProjection()
        {

            var aspectRatio = (float)renderControl.Width / renderControl.Height;
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                60 * ((float)Math.PI / 180f), // field of view angle, in radians
                aspectRatio,                // current window aspect ratio
                0.1f,                       // near plane
                4000f);                     // far plane
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            OnPaint(null);
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
    public class ObjectFactory
    {
        public static Vertex[] CreateSolidCube(float side, Color4 color)
        {
            side = side / 2f; // half side - and other half
            Vertex[] vertices =
            {
   new Vertex(new Vector4(-side, -side, -side, 1.0f),   color),
   new Vertex(new Vector4(-side, -side, side, 1.0f),    color),
   new Vertex(new Vector4(-side, side, -side, 1.0f),    color),
   new Vertex(new Vector4(-side, side, -side, 1.0f),    color),
   new Vertex(new Vector4(-side, -side, side, 1.0f),    color),
   new Vertex(new Vector4(-side, side, side, 1.0f),     color),

   new Vertex(new Vector4(side, -side, -side, 1.0f),    color),
   new Vertex(new Vector4(side, side, -side, 1.0f),     color),
   new Vertex(new Vector4(side, -side, side, 1.0f),     color),
   new Vertex(new Vector4(side, -side, side, 1.0f),     color),
   new Vertex(new Vector4(side, side, -side, 1.0f),     color),
   new Vertex(new Vector4(side, side, side, 1.0f),      color),

   new Vertex(new Vector4(-side, -side, -side, 1.0f),   color),
   new Vertex(new Vector4(side, -side, -side, 1.0f),    color),
   new Vertex(new Vector4(-side, -side, side, 1.0f),    color),
   new Vertex(new Vector4(-side, -side, side, 1.0f),    color),
   new Vertex(new Vector4(side, -side, -side, 1.0f),    color),
   new Vertex(new Vector4(side, -side, side, 1.0f),     color),

   new Vertex(new Vector4(-side, side, -side, 1.0f),    color),
   new Vertex(new Vector4(-side, side, side, 1.0f),     color),
   new Vertex(new Vector4(side, side, -side, 1.0f),     color),
   new Vertex(new Vector4(side, side, -side, 1.0f),     color),
   new Vertex(new Vector4(-side, side, side, 1.0f),     color),
   new Vertex(new Vector4(side, side, side, 1.0f),      color),

   new Vertex(new Vector4(-side, -side, -side, 1.0f),   color),
   new Vertex(new Vector4(-side, side, -side, 1.0f),    color),
   new Vertex(new Vector4(side, -side, -side, 1.0f),    color),
   new Vertex(new Vector4(side, -side, -side, 1.0f),    color),
   new Vertex(new Vector4(-side, side, -side, 1.0f),    color),
   new Vertex(new Vector4(side, side, -side, 1.0f),     color),

   new Vertex(new Vector4(-side, -side, side, 1.0f),    color),
   new Vertex(new Vector4(side, -side, side, 1.0f),     color),
   new Vertex(new Vector4(-side, side, side, 1.0f),     color),
   new Vertex(new Vector4(-side, side, side, 1.0f),     color),
   new Vertex(new Vector4(side, -side, side, 1.0f),     color),
   new Vertex(new Vector4(side, side, side, 1.0f),      color),
  };
            return vertices;
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
        }
        public void Bind()
        {
            GL.BindVertexArray(_vertexArray);
        }
        public void Render()
        {
            GL.DrawArrays(PrimitiveType.Triangles, 0, _verticeCount);
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

