using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace WindowsFormsApp1
{
    class Drawing
    {
        private static Color4 StartColor = new Color4(0.0f, 1.0f, 0.3f, 1.0f);
        private static Color4 BorderColor = new Color4(1.0f, 0.1f, 0.3f, 1.0f);
        static ErrorCode err;
        static Matrix4 projection;

        public static void DrawViewportPost(double xs, double ys, double xe,
                                  double ye, int width, int height)
        {
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, width / (float)height, 1.0f, 64.0f);
            // update viewport 
            var w = (double)width;
            var h = (double)height;
            var vw = (int)((xe - xs) * w);
            var vh = (int)((ye - ys) * h);
            int[] oldViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, oldViewport);
            GL.Viewport((int)(xs * w), (int)(ys * h), (int)((xe - xs) * w), (int)((ye - ys) * h));
            DrawViewportColorsPost(vw, vh);
            GL.Viewport(0,0,width, height);
        }


        public static void Clean(Color4 _backColor)
        {
            GL.ClearColor(_backColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(0);
        }

    public static void DrawViewportPre()
        {

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Color4(StartColor);
            GL.Color4(Color4.Red);
            GL.Rect(-1, -1, 1, 1);
            GL.Color4(Color4.White);
            GL.Rect(-0.95, -0.95, 0.95, 0.95);
            GL.Color4(Color4.Black);
            GL.Rect(-0.9, -0.9, 0.9, 0.9);
            GL.Color4(StartColor);
            GL.Rect(-0.8, -0.89, 0.5, 0.0);
        }

        public static void DrawArrays()
        {
            //  GL.MatrixMode(MatrixMode.Projection);
            //  GL.LoadMatrix(ref projection);
            //   Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            //   GL.MatrixMode(MatrixMode.Modelview);
            //    GL.LoadMatrix(ref modelview);
            // GL.Color4(Color4.Azure);
            GL.PointSize(100);
            TestError("point");
            GL.DrawArrays(PrimitiveType.Points, 0, 1);
            TestError("drawArr");

        }

        private static void DrawViewportColorsPost(int width, int height)
        {
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);

            var texW = 1.0 / width;
            var texH = 1.0 / height;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            TestError("loadId");
            var lineWidth = 4;
            // draw contour line
            GL.LineWidth(lineWidth);
            GL.Color4(BorderColor);
            TestError("color0");

            var xofs = lineWidth * 0.5 * texW;
            var yofs = lineWidth * 0.5 * texH;

            GL.Begin(BeginMode.LineStrip);
            GL.Vertex2(-1.0 + xofs, -1.0 + yofs);
            GL.Vertex2(1.0 - xofs, -1.0 + yofs);
            GL.Vertex2(1.0 - xofs, 1.0 - yofs);
            GL.Vertex2(-1.0 + xofs, 1.0 - yofs);
            GL.Vertex2(-1.0 + xofs, -1.0 + yofs);
            GL.End();
            TestError("bg+end");
            GL.LineWidth(1);
            GL.MatrixMode(MatrixMode.Modelview);

            GL.Begin(BeginMode.Triangles);

            GL.Color3(1.0f, 1.0f, 0.0f); GL.Vertex3(-1.0f, -1.0f, 4.0f);
            GL.Color3(1.0f, 0.0f, 0.0f); GL.Vertex3(1.0f, -1.0f, 4.0f);
            GL.Color3(0.2f, 0.9f, 1.0f); GL.Vertex3(0.0f, 1.0f, 4.0f);

            GL.End();
            TestError("bgend2");
        }
        public static void TestError(string where)
        {
            ErrorCode err = GL.GetError();
            if (err != ErrorCode.NoError) Console.WriteLine(where + "  " + err.ToString());
        }
    }
}
