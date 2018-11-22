///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [RenderMesh.cs]
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

using Assimp;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace open3mod
{
    public enum VertexVector
    {
        Position, Normal, Color, TextureCoordinate, Tangent, Bitangent
    }


    public struct FullVertex //NormalColorTexturedTangentBitangentVertex
    {
        public const int Size = (4 + 4 + 4 + 4 + 4 + 4) * 4; // size of struct in bytes
        private Vector4 _position;
        private Vector4 _normal;
        private Color4D _color;
        private Vector4 _textureCoordinate;  //vector2 is enough, but for simplicity i keep it vec4
        private Vector4 _tangent;
        private Vector4 _bitangent;

        public FullVertex(Vector4 position)
        {
            _position = new Vector4(0,0,0,0);
            _normal = new Vector4(0, 0, 0, 0);
            _color = new Color4D(1, 1, 1, 1);
            _textureCoordinate = new Vector4(0, 0,0,0);
            _tangent = new Vector4(0, 0, 0, 0);
            _bitangent = new Vector4(0, 0, 0, 0);
        }
        public void SetTextureCoordinate(Vector2 textureCoordinate)
        {
            _textureCoordinate.X = textureCoordinate.X;
            _textureCoordinate.Y = textureCoordinate.Y;
            _textureCoordinate.Z = 0;
            _textureCoordinate.W = 0;
        }

        public void SetColor(Color4D color)
        {
            _color = color;
        }

        public void SetVector4(VertexVector bufferId, Vector4 data)
        {
            switch(bufferId)
            {
                case VertexVector.Position: _position = data; break;
                case VertexVector.Normal: _normal = data; break;
                case VertexVector.TextureCoordinate: _textureCoordinate = data; break;
                case VertexVector.Tangent: _tangent = data; break;
                case VertexVector.Bitangent: _bitangent = data; break;
                default:Debug.Assert(false);break;
            }
        }
    }

    /// <summary>
    /// Mesh rendering using VBOs.
    /// 
    /// Based on http://www.opentk.com/files/T08_VBO.cs
    /// </summary>
    public class RenderMesh
    {
        private readonly Mesh _mesh;
        struct Vbo
        {
            public int VertexArray;
            public int VertexBufferId;
            //            public int ColorBufferId;
            //            public int TexCoordBufferId;
            //            public int NormalBufferId;
            //            public int TangentBufferId;
            //            public int BitangentBufferId;
            public int ElementBufferId;
            public int NumIndices;
            public bool Is32BitIndices;
            public int VerticeCount;
        }

        private readonly Vbo _vbo;

        /// <summary>
        /// Constructs a RenderMesh for a given assimp mesh and uploads the
        /// required data to the GPU.
        /// </summary>
        /// <param name="mesh"></param>
        /// <exception cref="Exception">When any Gl errors occur during uploading</exception>
        public RenderMesh(Mesh mesh)
        {
            Debug.Assert(mesh != null);
            _mesh = mesh;
            Upload(out _vbo);
        }

        /// <summary>
        /// Draws the mesh geometry given the current pipeline state. 
        /// 
        /// The pipeline is restored afterwards.
        /// </summary>
        /// <param name="flags">Rendering mode</param>
        public void Render(RenderFlags flags)
        {
            Debug.Assert(_vbo.VertexBufferId != 0);
            Debug.Assert(_vbo.ElementBufferId != 0);
            GL.BindVertexArray(_vbo.VertexArray); ///
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo.VertexBufferId);

            // primitives
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _vbo.ElementBufferId);
            GL.DrawElements(OpenTK.Graphics.OpenGL.PrimitiveType.Triangles, _vbo.NumIndices,
                _vbo.Is32BitIndices ? DrawElementsType.UnsignedInt : DrawElementsType.UnsignedShort, IntPtr.Zero);
            RenderControl.GLError("EndModernRender");
        }

        /// <summary>
        /// Currently only called during construction, this method uploads the input mesh (
        /// the RenderMesh instance is bound to) to a VBO.
        /// </summary>
        /// <param name="vboToFill"></param>
        private void Upload(out Vbo vboToFill)
        {
            vboToFill = new Vbo();
            vboToFill.VerticeCount = _mesh.VertexCount;
            FullVertex[] tempVertexBuffer = new FullVertex[vboToFill.VerticeCount];

            FillBufferV3D(ref tempVertexBuffer, VertexVector.Position, _mesh.Vertices); 
            if (_mesh.HasNormals) 
            {
                Debug.Assert(_mesh.HasNormals);
                FillBufferV3D(ref tempVertexBuffer, VertexVector.Normal, _mesh.Normals);
            }
            if (_mesh.HasVertexColors(0))
            {
                Debug.Assert(_mesh.HasVertexColors(0));
                var colors = _mesh.VertexColorChannels[0];
                FillBufferC4D(ref tempVertexBuffer, colors);
            }
            if (_mesh.HasTextureCoords(0))
            {
                Debug.Assert(_mesh.HasTextureCoords(0));
                var uvs = _mesh.TextureCoordinateChannels[0];
                FillBufferV2D(ref tempVertexBuffer, VertexVector.TextureCoordinate, uvs);
            }
            if (_mesh.HasTangentBasis)
            {
                Debug.Assert(_mesh.HasTangentBasis);
                var tangents = _mesh.Tangents;
                FillBufferV3D(ref tempVertexBuffer, VertexVector.Tangent, tangents);
                var bitangents = _mesh.BiTangents;
                Debug.Assert(bitangents.Count == tangents.Count);
                FillBufferV3D(ref tempVertexBuffer, VertexVector.Bitangent, bitangents);
            }
            UploadPrimitives(out vboToFill.ElementBufferId, out vboToFill.NumIndices, out vboToFill.Is32BitIndices);
            // TODO: upload bone weights

            vboToFill.VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(vboToFill.VertexArray);

            GL.GenBuffers(1, out vboToFill.VertexBufferId);
            int byteCount = vboToFill.VerticeCount * FullVertex.Size;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboToFill.VertexBufferId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(byteCount), tempVertexBuffer, BufferUsageHint.StaticDraw);
            VerifyBufferSize(byteCount);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboToFill.VertexBufferId);

            GL.VertexArrayAttribBinding(vboToFill.VertexArray, 0, 0);
            GL.EnableVertexArrayAttrib(vboToFill.VertexArray, 0);
            GL.VertexArrayAttribFormat(
                vboToFill.VertexArray,
                0,                      // attribute index, from the shader location = 0 : Vector4 _position;
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                0);                     // relative offsetm first item

            GL.VertexArrayAttribBinding(vboToFill.VertexArray, 1, 0);
            GL.EnableVertexArrayAttrib(vboToFill.VertexArray, 1);
            GL.VertexArrayAttribFormat(
                vboToFill.VertexArray,
                1,                      // attribute index, from the shader location = 1 : Vector4 _normal
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                16);                     // relative offset after a vec4

            GL.VertexArrayAttribBinding(vboToFill.VertexArray, 2, 0);
            GL.EnableVertexArrayAttrib(vboToFill.VertexArray, 2);
            GL.VertexArrayAttribFormat(
                vboToFill.VertexArray,
                2,                      // attribute index, from the shader location = 2 : Color4D _color;
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                32);                     // relative offset after a vec4

            GL.VertexArrayAttribBinding(vboToFill.VertexArray, 3, 0);
            GL.EnableVertexArrayAttrib(vboToFill.VertexArray, 3);
            GL.VertexArrayAttribFormat(
                vboToFill.VertexArray,
                3,                      // attribute index, from the shader location = 3 : Vector4 _textureCoordinate;
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                48);                     // relative offset after a vec4

            GL.VertexArrayAttribBinding(vboToFill.VertexArray, 4, 0);
            GL.EnableVertexArrayAttrib(vboToFill.VertexArray, 4);
            GL.VertexArrayAttribFormat(
                vboToFill.VertexArray,
                4,                      // attribute index, from the shader location = 4 : Vector4 _tangent;
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                64);                     // relative offset after a vec4

            GL.VertexArrayAttribBinding(vboToFill.VertexArray, 5, 0);
            GL.EnableVertexArrayAttrib(vboToFill.VertexArray, 5);
            GL.VertexArrayAttribFormat(
                vboToFill.VertexArray,
                5,                      // attribute index, from the shader location = 4 : Vector4 _bitangent;
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                80);                     // relative offset after a vec4

            GL.VertexArrayVertexBuffer(vboToFill.VertexArray, 0, vboToFill.VertexBufferId, IntPtr.Zero, FullVertex.Size);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

        }


        /// <summary>
        /// Generates and populates an vertex temp buffer given 3D vectors as source data
        /// </summary>
        private void FillBufferV3D(ref FullVertex[] vertexBuffer, VertexVector vertexBufferId, List<Vector3D> dataBuffer) 
        {
            Vector4 temp = new Vector4(0, 0, 0, 1);
            var i = 0;
            foreach(var v in dataBuffer)
            {
                temp.X = v.X;
                temp.Y = v.Y;
                temp.Z = v.Z;
                vertexBuffer[i].SetVector4(vertexBufferId, temp);
                i++;
            }
        }

        /// <summary>
        /// Generates and populates an vertex temp buffer given 2D vectors as source data (given as 3D)
        /// </summary>
        private void FillBufferV2D(ref FullVertex[] vertexBuffer, VertexVector vertexBufferId, List<Vector3D> dataBuffer)
        {
            Vector4 temp = new Vector4(0, 0, 0, 0);
            var i = 0;
            foreach (var v in dataBuffer)
            {
                temp.X = v.X;
                temp.Y = v.Y;
                vertexBuffer[i].SetVector4(vertexBufferId, temp);
                i++;
            }
        }

        /// <summary>
        /// Verifies that the size of the currently bound vertex array buffer matches
        /// a given parameter and throws if it doesn't.
        /// </summary>
// ReSharper disable UnusedParameter.Local
        private void VerifyBufferSize(int byteCount)
// ReSharper restore UnusedParameter.Local
        {
            int bufferSize;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
            if (byteCount != bufferSize)
            {
                throw new Exception("Vertex data array not uploaded correctly - buffer size does not match upload size");
            }
        }


        /// <summary>
        /// Uploads vertex indices to a newly generated Gl vertex array
        /// </summary>
        private void UploadPrimitives(out int elementBufferId, out int indicesCount, out bool is32Bit)
        {
          //  Debug.Assert(_mesh.HasTextureCoords(0));

            GL.GenBuffers(1, out elementBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferId);

            var faces = _mesh.Faces;

            // TODO account for other primitives than triangles
            var triCount = 0;
            int byteCount;
            is32Bit = false;
            foreach(var face in faces)
            {
                if (face.IndexCount != 3)
                {
                    continue;
                }
                ++triCount;
                if (face.Indices.Any(idx => idx > 0xffff))
                {
                    is32Bit = true;
                }
            }

            var intCount = triCount * 3;
            if (is32Bit)
            {
                var temp = new uint[intCount];
                byteCount = intCount * sizeof(uint);
                var n = 0;
                foreach (var idx in faces.Where(face => face.IndexCount == 3).SelectMany(face => face.Indices))
                {
                    temp[n++] = (uint)idx;
                }

                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)byteCount,
                    temp, BufferUsageHint.StaticDraw);
            }
            else
            {
                var temp = new ushort[intCount];
                byteCount = intCount * sizeof(ushort);
                var n = 0;
                foreach (var idx in faces.Where(face => face.IndexCount == 3).SelectMany(face => face.Indices))
                {
                    Debug.Assert(idx <= 0xffff);
                    temp[n++] = (ushort)idx;
                }

                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)byteCount, 
                    temp, BufferUsageHint.StaticDraw);
            }

            int bufferSize;
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
            if (byteCount != bufferSize)
            {
                throw new Exception("Index data array not uploaded correctly - buffer size does not match upload size");
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            indicesCount = triCount * 3;
        }


        /// <summary>
        /// Uploads vertex colors to a vertex temp buffer
        /// </summary>
        /// <param name="colorBufferId"></param>
        private void FillBufferC4D(ref FullVertex[] vertexBuffer, List<Color4D> dataBuffer)
        {
            var i = 0;
            foreach (var v in dataBuffer)
            {
                /*   // convert to 32Bit RGBA - skipped
                       var byteCount = dataBuffer.Count*4;
                       var byteColors = new byte[byteCount];
                       var n = 0;
                       foreach(var c in colors)
                       {
                           byteColors[n++] = (byte)(c.R * 255);
                           byteColors[n++] = (byte)(c.G * 255);
                           byteColors[n++] = (byte)(c.B * 255);
                           byteColors[n++] = (byte)(c.A * 255);*/
                vertexBuffer[i].SetColor(v);
                i++;
            }
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 