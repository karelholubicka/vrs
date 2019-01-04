///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [SphereGeometry.cs]
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
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace open3mod
{
    /// <summary>
    /// Utility class that generates vertices and indices for drawing spheres.
    /// 
    /// TODO add tangents and bitangents 
    /// 
    /// This code is almost completely taken from http://www.opentk.com/node/1800
    /// </summary>
    public static class SphereGeometry
    {
        public struct Vertex
        { // mimic InterleavedArrayFormat.T2fN3fV3f
            public Vector2 TexCoord;
            public Vector3 Normal;
            public Vector3 Position;
        }

        /// <summary>
        /// Draws a sphere using Gl immediate mode.
        /// 
        /// Makes no changes to Gl draw/material/matrix/lighting/.. states.
        /// </summary>
        /// <param name="sphereVertices">Array of vertices previously obtained
        ///    from a call to CalculateVertices()</param>
        /// <param name="sphereElements">Array of indices previous obtained
        ///    from a call to CalculateElements()</param>
        public static void DrawClassicGL(Vertex[] sphereVertices, ushort[] sphereElements)
        {
            GL.Begin(PrimitiveType.Triangles);
            foreach (var element in sphereElements)
            {
                var vertex = sphereVertices[element];
                GL.TexCoord2(vertex.TexCoord);
                GL.Normal3(vertex.Normal);
                GL.Vertex3(vertex.Position);
            }
            GL.End();
        }

        public static void SetFloatVec4VertexArrayAttr(int vertexArray, int attrIndex)
        {
            int attrSize = 4; //vec4
            int attrOffset = attrIndex * attrSize * sizeof(float); //attribute contains 4 float numbers

            GL.VertexArrayAttribBinding(vertexArray, attrIndex, 0);
            GL.EnableVertexArrayAttrib(vertexArray, attrIndex);
            GL.VertexArrayAttribFormat(
                vertexArray,
                attrIndex,                      // attribute index, from the shader location 
                attrSize,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                attrOffset);                     // relative offset after a vec4
        }

        public static void DrawModernGL(FullVertex[] fullSphereVertices, ushort[] sphereElements)
        {
            RenderControl.GLError("StartSphereRender");
            GL.GenBuffers(1, out int ElementBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferId);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sphereElements.Length*sizeof(ushort)), sphereElements, BufferUsageHint.StaticDraw);
            int bufferSize;
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
            if (sphereElements.Length * sizeof(ushort) != bufferSize)
            {
                throw new Exception("Index data array not uploaded correctly - buffer size does not match upload size");
            }

            int VertexArray;
            int VertexBufferId;
            VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(VertexArray);
            GL.GenBuffers(1, out VertexBufferId);
            int byteCount = fullSphereVertices.Length * FullVertex.Size;
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(byteCount), fullSphereVertices, BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
            if (byteCount != bufferSize)
            {
                throw new Exception("Vertex data array not uploaded correctly - buffer size does not match upload size");
            }

            SetFloatVec4VertexArrayAttr(VertexArray, 0); //location = 0 : Vector4 _position;
            SetFloatVec4VertexArrayAttr(VertexArray, 1); //location = 1 : Vector4 _normal
            SetFloatVec4VertexArrayAttr(VertexArray, 2); //location = 2 : Color4D _color;
            SetFloatVec4VertexArrayAttr(VertexArray, 3); //location = 3 : Vector4 _textureCoordinate;
            SetFloatVec4VertexArrayAttr(VertexArray, 4); //location = 4 : Vector4 _tangent;
            SetFloatVec4VertexArrayAttr(VertexArray, 5); //location = 5 : Vector4 _bitangent;

            GL.VertexArrayVertexBuffer(VertexArray, 0, VertexBufferId, IntPtr.Zero, FullVertex.Size);

            /*  GL.BindVertexArray(0);
              GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
              GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
              GL.BindVertexArray(VertexArray); 
              GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferId);*/
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferId); //must be re-bound here...
            // GL.DrawArrays(OpenTK.Graphics.OpenGL.PrimitiveType.Triangles, 0, fullSphereVertices.Length); //to check vertices separately
            GL.DrawElements(OpenTK.Graphics.OpenGL.PrimitiveType.Triangles, sphereElements.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
            RenderControl.GLError("EndSphereRender");
        }

        public static void CalculateVertices(float radius, float height, byte segments, byte rings, out Vertex[] data, out FullVertex[] fullData)
        {
            data = new Vertex[segments * rings];
            fullData = new FullVertex[segments * rings];
            var i = 0;

            for (double y = 0; y < rings; y++)
            {
                var phi = (y / (rings - 1)) * Math.PI; 
                for (double x = 0; x < segments; x++)
                {
                    var theta = (x / (segments - 1)) * 2 * Math.PI;

                    var v = new Vector3()
                    {
                        X = (float)(radius * Math.Sin(phi) * Math.Cos(theta)),
                        Y = (float)(height * Math.Cos(phi)),
                        Z = (float)(radius * Math.Sin(phi) * Math.Sin(theta)),
                    };
                    var n = Vector3.Normalize(v);
                    var uv = new Vector2()
                    {
                        X = (float)(x / (segments - 1)),
                        Y = (float)(y / (rings - 1))
                    };
                    // Using data[i++] causes i to be incremented multiple times in Mono 2.2 (bug #479506).
                     data[i] = new Vertex() { Position = v, Normal = n, TexCoord = uv };
                    fullData[i] = new FullVertex();
                    fullData[i].SetPosition(new Vector3(data[i].Position.X, data[i].Position.Z, -data[i].Position.Y));
                    fullData[i].SetNormal(n);
                    fullData[i].SetTextureCoordinate(new Vector2(-data[i].TexCoord.X, data[i].TexCoord.Y));
                    i++;
                }
            }
        }

        public static ushort[] CalculateElements(byte segments, byte rings)
        {
            var numVertices = segments * rings;
            var data = new ushort[numVertices * 6];
            ushort i = 0;
            for (byte y = 0; y < rings - 1; y++)
            {
                for (byte x = 0; x < segments - 1; x++)
                {
                    data[i++] = (ushort)((y + 0) * segments + x);
                    data[i++] = (ushort)((y + 1) * segments + x);
                    data[i++] = (ushort)((y + 1) * segments + x + 1);

                    data[i++] = (ushort)((y + 1) * segments + x + 1);
                    data[i++] = (ushort)((y + 0) * segments + x + 1);
                    data[i++] = (ushort)((y + 0) * segments + x);
                }
            }
            return data;
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 