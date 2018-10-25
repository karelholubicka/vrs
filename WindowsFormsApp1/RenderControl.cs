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


using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace WindowsFormsApp1
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
    /// MultiSampling is requested according to the current value of
    /// GraphicsSettings.Default.UseMultiSampling.
    /// 
    /// </summary>
    class RenderControl : GLControl
    {
        static int numBuffers = 2;
        int[] renderBuffer = new int[numBuffers];
        int[] depthBuffer = new int[numBuffers];
        int[] frameBuffer = new int[numBuffers*2];
        int[] pixelPackBuffer = new int[numBuffers];
        int[] pixelUnpackBuffer = new int[numBuffers];
        public bool initialized = false;
        //buffers size: 0 - screen, 1 - video
        public const byte bytePerPixel = 4;
        GLControl glCore;

        public RenderControl()
            : base(new GraphicsMode(new ColorFormat(32), 24, 8, GetSampleCount()))
//            : base(new GraphicsMode(new ColorFormat(32), 24, 8, GetSampleCount(GraphicsSettings.Default.MultiSampling)))
        {
           glCore = new OpenTK.GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, 0), 4, 5, 0);
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
        }

        /// <summary>
        /// Binds Renderbuffers to Framebuffer.
        /// </summary>
        /// <returns></returns>
        public void BindBuffers(int frameIndex, int renderIndex)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[frameIndex]);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderBuffer[renderIndex]);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthBuffer[renderIndex]);
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete &&
                status != FramebufferErrorCode.FramebufferCompleteExt)
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
            GL.GenFramebuffers(numBuffers * 2, frameBuffer);
            GL.GenRenderbuffers(numBuffers, renderBuffer);
            GL.GenRenderbuffers(numBuffers, depthBuffer);
            GL.GenBuffers(numBuffers, pixelPackBuffer);
            GL.GenBuffers(numBuffers, pixelUnpackBuffer);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[0]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, Width, Height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[0]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, Width, Height);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[1]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, videoSizeX, videoSizeY);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[1]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, videoSizeX, videoSizeY);
            this.MakeCurrent();
            BindBuffers(0, 0);
            BindBuffers(2, 1);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[0]);
            GL.BufferData(BufferTarget.PixelPackBuffer, (IntPtr)(Width * Height * bytePerPixel), (IntPtr)0, BufferUsageHint.DynamicCopy);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[0]);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(Width * Height * bytePerPixel), (IntPtr)0, BufferUsageHint.DynamicCopy);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

            glCore.MakeCurrent();
            BindBuffers(1, 0);
            BindBuffers(3, 1);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[1]);
            GL.BufferData(BufferTarget.PixelPackBuffer, (IntPtr)(videoSizeX * videoSizeY * bytePerPixel), (IntPtr)0, BufferUsageHint.DynamicRead);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[1]);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(videoSizeX * videoSizeY * bytePerPixel), (IntPtr)0, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            initialized = true;
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
            SetRenderTarget(RenderTarget.ScreenCore);
            glCore.Width = _width;
            glCore.Height = _height;
            glCore.Left = _left;
            glCore.Top = _top;
            GL.Viewport(Left, Top, Width, Height);
            if (!initialized) return;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer[0]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, Width, Height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer[0]);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, Width, Height);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[0]);
            GL.BufferData(BufferTarget.PixelPackBuffer, (IntPtr)(Width * Height * bytePerPixel), (IntPtr)0, BufferUsageHint.DynamicCopy);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[0]);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(Width * Height * bytePerPixel), (IntPtr)0, BufferUsageHint.DynamicCopy);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

        }

        /// <summary>
        /// Switches between all available targets.
        /// </summary>
        /// <returns></returns>
        public void SetRenderTarget(RenderTarget rt)
        {
            switch (rt)
            {
                case RenderTarget.ScreenDirect:
                    this.MakeCurrent();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    break;
                case RenderTarget.ScreenCompat:
                    this.MakeCurrent();
                    ErrorCode err = GL.GetError();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[0]);
                    break;
                case RenderTarget.ScreenCore:
                    glCore.MakeCurrent();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[1]);
                    break;
                case RenderTarget.VideoCompat:
                    this.MakeCurrent();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[2]);
                    break;
                case RenderTarget.VideoCore:
                    glCore.MakeCurrent();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer[3]);
                    break;
            }
        }

        /// <summary>
        /// Fast copy from shared Renderbuffer to onscreen Renderbuffer.
        /// </summary>
        /// <returns></returns>
        public void CopyToScreenFramebuffer()
        {
            SetRenderTarget(RenderControl.RenderTarget.ScreenCompat);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelPackBuffer[0]);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pixelUnpackBuffer[0]);

            GL.ReadPixels(0, 0, Width, Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)0);
            GL.CopyBufferSubData(BufferTarget.PixelPackBuffer, BufferTarget.PixelUnpackBuffer, (IntPtr)0, (IntPtr)0, (IntPtr)(Width * Height * bytePerPixel));
            SetRenderTarget(RenderControl.RenderTarget.ScreenDirect);
            GL.DrawPixels(Width, Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)0);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }

        /// <summary>
        /// Converts a value for GraphicsSettings.Default.MultiSampling into a device-specific
        /// sample count.
        /// </summary>
        /// <param name="multiSampling">Device-independent quality level in [0,3]</param>
        /// <returns>Sample count for device</returns>
        private static int GetSampleCount()
        {
            // UI names:
            /*  None
                Slight
                Normal
                Maximum
            */
//            int multiSampling = GraphicsSettings.Default.MultiSampling;
            int multiSampling = 2;
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
                if(mode.Samples == aa && mode.Samples > highest)
                {
                    highest = mode.Samples;
                }
                aa += 2;
            } while (aa <= 32);
            return highest;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        /// <returns></returns>
        public void Exit()
        {
            GL.DeleteFramebuffers(numBuffers*2, frameBuffer);
            GL.DeleteRenderbuffers(numBuffers, renderBuffer);
            GL.DeleteRenderbuffers(numBuffers, depthBuffer);
            GL.DeleteBuffers(numBuffers, pixelPackBuffer);
            GL.DeleteBuffers(numBuffers, pixelUnpackBuffer);
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
            if (err != ErrorCode.NoError) Console.WriteLine("Error reported at "+ where + ":  " + err.ToString());
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 