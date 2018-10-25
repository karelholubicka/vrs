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
using DeckLinkAPI;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace open3mod
{
    /// <summary>
    /// Dummy derivative of GLControl to be able to specify constructor
    /// parameters while still being usable with the WinForms designer.
    /// 
    /// The RenderControl always requests a stencil buffer and a 24 bit depth
    /// buffer, which should be natively supported by most hardware in use today.
    /// 
    /// MultiSampling is requested according to the current value of
    /// GraphicsSettings.Default.UseMultiSampling.
    /// 
    /// </summary>
    class GLWindow : GLControl, IDeckLinkScreenPreviewCallback
    {
        public GLWindow()
            : base(new GraphicsMode(new ColorFormat(32), 24, 8, 0))
        {   }
        private IDeckLinkGLScreenPreviewHelper m_previewHelper;
        private string m_timeCodeString;
        private bool glIsInitialized = false;
        private long capturedFrames = 0;
        GraphicsContext thisContext;
        GraphicsMode gm;
        private Color4 _backColor = new Color4(0.1f, 0.1f, 0.3f, 1.0f);

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
            } while (aa <= 32);
            return highest;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // RenderControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Name = "RenderControl";
            this.Size = new System.Drawing.Size(170, 162);
            this.ResumeLayout(false);
        }


        public void InitGL(CapturePreview owner)
        {
            gm = new GraphicsMode(new ColorFormat(32), 24, 8, 0);
            thisContext = new GraphicsContext(gm, WindowInfo);
            this.MakeCurrent();
            this.VSync = false;
            GL.Viewport(0, 0, Width, Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, -1.0, 1.0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();// Reset The View
            GL.ClearColor(Color.DimGray);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            m_previewHelper = new CDeckLinkGLScreenPreviewHelper();
            m_previewHelper.InitializeGL();
            glIsInitialized = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var cgx = GraphicsContext.CurrentContext;
            if (cgx != thisContext) this.MakeCurrent();
            GLDrawFrame();
            SwapBuffers();
        }

        void SetTimecode(IDeckLinkVideoFrame videoFrame)
        {
            IDeckLinkTimecode timecode;
            m_timeCodeString = "--:--:--:--";
            videoFrame.GetTimecode(_BMDTimecodeFormat.bmdTimecodeRP188Any, out timecode);
            videoFrame.GetTimecode(_BMDTimecodeFormat.bmdTimecodeVITC, out timecode);
            if (timecode != null)
            {
                timecode.GetString(out m_timeCodeString);
            }
            else
            {
                m_timeCodeString = capturedFrames.ToString();
            }
        }

        public void GLDrawFrame()
        {
            if (!glIsInitialized) return;
            m_previewHelper.PaintGL();
        }

        void IDeckLinkScreenPreviewCallback.DrawFrame(IDeckLinkVideoFrame theFrame)
        {
            if (!glIsInitialized) return;
            // First, pass the frame to the DeckLink screen preview helper
            SetTimecode(theFrame);
            m_previewHelper.SetFrame(theFrame);
            capturedFrames++;
            Invalidate();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theFrame);
        }
    }
}


