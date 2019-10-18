///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [SceneRendererClassicGl.cs]
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
using System.IO;
using System.Linq;
using System.Text;

using Assimp;
using OpenTK;

using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace open3mod
{
    /// <summary>
    /// Render a Scene using old-school OpenGl, that is, display lists,
    /// matrix stacks and glVertexN-family calls.
    /// </summary>
    public sealed class SceneRendererClassicGl : SceneRendererShared, ISceneRenderer
    {
        private const int _displayListCount = 8;
        private int[,] _displayList = new int[_displayListCount,2];
        private RenderFlags _lastFlags;
        private bool wasAnimated;


        internal SceneRendererClassicGl(Scene owner, Vector3 initposeMin, Vector3 initposeMax)
            : base(owner, initposeMin, initposeMax)
        {

        }


        public void Dispose()
        {
            for (var i = 0; i < _displayListCount;i++)
            {
                if (_displayList[i,0] != 0)
                {
                    GL.DeleteLists(_displayList[i, 0], 1);
                    _displayList[i, 0] = 0;
                }
                if (_displayList[i, 1] != 0)
                {
                    GL.DeleteLists(_displayList[i, 1], 1);
                    _displayList[i,1] = 0;
                }
            }
            GC.SuppressFinalize(this);
        }


#if DEBUG
        ~SceneRendererClassicGl()
        {
            // OpenTk is unsafe from here, explicit Dispose() is required.
            Debug.Assert(false);
        }
#endif


        /// <summary>
        /// <see cref="ISceneRenderer.Update"/>
        /// </summary>   
        public void Update(double delta)
        {
            Skinner.Update();
        }


        /// <summary>
        /// <see cref="ISceneRenderer.Render"/>
        /// </summary>   
        public void Render(ICameraController cam, Dictionary<Node, List<Mesh>> visibleMeshesByNode,
            bool visibleSetChanged,
            bool texturesChanged,
            RenderFlags flags,
            Renderer renderer)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Enable(EnableCap.DepthTest);

            if (flags.HasFlag(RenderFlags.Wireframe))
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }

            int[] CurrentViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, CurrentViewport);
            var aspectRatio = (float)CurrentViewport[2] / CurrentViewport[3];
            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(cam.GetFOV(), aspectRatio, renderer.zNear, renderer.zFar);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perspective);

            var tmp = InitposeMax.X - InitposeMin.X;
            tmp = Math.Max(InitposeMax.Y - InitposeMin.Y, tmp);
            tmp = Math.Max(InitposeMax.Z - InitposeMin.Z, tmp);
            int logScale = (int)Math.Truncate(Math.Log10(tmp*10/50)); //  Up to 50units max size = 50m: keep scale (for smaller scenes).
            float scale = 1;
           // for (int i = 0; i < logScale;i++) scale = scale / 10; keep 1x
            Owner.Scale = scale;
            if (cam != null)
            {
            //       cam.SetPivot(Owner.Pivot * (float)scale); this does nothing (?) only makes controller dirty
            }
            var view = cam == null ? Matrix4.LookAt(0, 10, 5, 0, 0, 0, 0, 1, 0) : cam.GetView();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref view);
            GL.Scale(scale, scale, scale);
            Owner.MaterialMapper.BeginScene(renderer, flags.HasFlag(RenderFlags.UseSceneLights)); //here we switch on lights

            if (flags.HasFlag(RenderFlags.ShowLightDirection)) //switch off for video??
            {
                var dir = new Vector3(1, 1, 0);
                var mat = renderer.LightRotation;
                Vector3.TransformNormal(ref dir, ref mat, out dir);
                OverlayLightSource.DrawLightSource(dir);
            }

            // If textures changed, we may need to upload some of them to VRAM.
            // it is important this happens here and not accidentially while
            // compiling a displist.
            if (texturesChanged)
            {
                UploadTextures();
            }
            UploadDynamicTextures();

            GL.PushMatrix();

            // Build and cache Gl displaylists and update only when the scene changes.
            // when the scene is being animated, this changes each frame
            var animated = Owner.SceneAnimator.IsAnimationActive;
            if (visibleSetChanged || texturesChanged || flags != _lastFlags || (animated && (Owner.NewFrame) || wasAnimated ))
            {
                int startList = 4; //we update only 4 animation displists
                if (visibleSetChanged || texturesChanged || flags != _lastFlags) startList = 0;
                _lastFlags = flags;

                // handle opaque geometry
                for (int currDispList = startList; currDispList < _displayListCount; currDispList++)
                {
                    if (_displayList[currDispList, 0] == 0) _displayList[currDispList, 0] = GL.GenLists(1);
                    GL.NewList(_displayList[currDispList, 0], ListMode.Compile);
                    var needAlpha = RecursiveRender(Owner.Raw.RootNode, visibleMeshesByNode, flags, animated, currDispList);
                    if (flags.HasFlag(RenderFlags.ShowSkeleton))
                    {
                        RecursiveRenderNoScale(Owner.Raw.RootNode, visibleMeshesByNode, flags, 1.0f / scale, animated, currDispList);
                    }
                    if (flags.HasFlag(RenderFlags.ShowNormals))
                    {
                        RecursiveRenderNormals(Owner.Raw.RootNode, visibleMeshesByNode, flags, 1.0f / scale, animated, Matrix4.Identity, currDispList);
                    }
                    GL.EndList();
                    if (needAlpha)
                    {
                        // handle semi-transparent geometry
                        if (_displayList[currDispList, 1] == 0)
                        {
                            _displayList[currDispList, 1] = GL.GenLists(1);
                        }
                        GL.NewList(_displayList[currDispList, 1], ListMode.Compile);
                        for (int order = 0; order < MaxOrder; order++)
                        {
                            RecursiveRenderWithAlpha(Owner.Raw.RootNode, visibleMeshesByNode, flags, animated, currDispList, order);
                        }
                        GL.EndList();
                    }
                    else if (_displayList[currDispList, 1] != 0)
                    {
                        GL.DeleteLists(_displayList[currDispList, 1], 1);
                        _displayList[currDispList, 1] = 0;
                    }
                }
            }
            
            Owner.NewFrame = false;
            wasAnimated = animated;

            switch (cam.GetScenePartMode())
            {
                case ScenePartMode.Others:
                    if (_displayList[0, 0] != 0) GL.CallList(_displayList[0, 0]);
                    if (_displayList[4, 0] != 0) GL.CallList(_displayList[4, 0]);
                    if (_displayList[0, 1] != 0) GL.CallList(_displayList[0, 1]);
                    if (_displayList[4, 1] != 0) GL.CallList(_displayList[4, 1]);
                    break;
                case ScenePartMode.Background:
                    if (_displayList[1, 0] != 0) GL.CallList(_displayList[1, 0]);
                    if (_displayList[5, 0] != 0) GL.CallList(_displayList[5, 0]);
                    if (_displayList[1, 1] != 0) GL.CallList(_displayList[1, 1]);
                    if (_displayList[5, 1] != 0) GL.CallList(_displayList[5, 1]);
                    break;
                case ScenePartMode.Foreground:
                    if (_displayList[2, 0] != 0) GL.CallList(_displayList[2, 0]);
                    if (_displayList[6, 0] != 0) GL.CallList(_displayList[6, 0]);
                    if (_displayList[2, 1] != 0) GL.CallList(_displayList[2, 1]);
                    if (_displayList[6, 1] != 0) GL.CallList(_displayList[6, 1]);
                    break;
                case ScenePartMode.GreenScreen:
                    if (_displayList[3, 0] != 0) GL.CallList(_displayList[3, 0]);
                    if (_displayList[7, 0] != 0) GL.CallList(_displayList[7, 0]);
                    if (_displayList[3, 1] != 0) GL.CallList(_displayList[3, 1]);
                    if (_displayList[7, 1] != 0) GL.CallList(_displayList[7, 1]);
                    break;
                case ScenePartMode.All:
                    for (int currDispList = 0; currDispList < _displayListCount; currDispList++)
                    {
                        if (_displayList[currDispList, 0] != 0) GL.CallList(_displayList[currDispList, 0]);
                        if (_displayList[currDispList, 1] != 0) GL.CallList(_displayList[currDispList, 1]);
                    }
                    break;
                case ScenePartMode.Visible:
                     if (_displayList[1, 0] != 0) GL.CallList(_displayList[1, 0]);
                     if (_displayList[5, 0] != 0) GL.CallList(_displayList[5, 0]);
                     if (_displayList[1, 1] != 0) GL.CallList(_displayList[1, 1]);
                     if (_displayList[5, 1] != 0) GL.CallList(_displayList[5, 1]);
                     if (_displayList[2, 0] != 0) GL.CallList(_displayList[2, 0]);
                     if (_displayList[6, 0] != 0) GL.CallList(_displayList[6, 0]);
                     if (_displayList[2, 1] != 0) GL.CallList(_displayList[2, 1]);
                     if (_displayList[6, 1] != 0) GL.CallList(_displayList[6, 1]);
                     break;
                default: break;//at other modes we do not render anything
            }
            if (flags.HasFlag(RenderFlags.ShowNormals)) { ErrorCode err = GL.GetError(); } //catch some error from normals rendering
            GL.PopMatrix();
            // always switch back to FILL
            Owner.MaterialMapper.EndScene(renderer);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.DepthTest);

#if TEST
                                    GL.Enable(EnableCap.ColorMaterial);


                                    // TEST CODE to visualize mid point (pivot) and origin
                                    GL.LoadMatrix(ref view);
                                    GL.Begin(BeginMode.Lines);

                                    GL.Vertex3((InitposeMin + InitposeMax) * 0.5f * (float)scale);
                                    GL.Color3(0.0f, 1.0f, 0.0f);
                                    GL.Vertex3(0,0,0);
                                    GL.Color3(0.0f, 1.0f, 0.0f);
                                    GL.Vertex3((InitposeMin + InitposeMax) * 0.5f * (float)scale);
                                    GL.Color3(0.0f, 1.0f, 0.0f);

                                    GL.Vertex3(10, 10, 10);
                                    GL.Color3(0.0f, 1.0f, 0.0f);
                                    GL.End();
#endif
            GL.Disable(EnableCap.Texture2D);

        }

        protected override void PushWorld(ref Matrix4 world)
        {
            GL.PushMatrix();
            GL.MultMatrix(ref world);
        }

        protected override void PopWorld()
        {
            GL.PopMatrix();
        }

        /// <summary>
        /// Draw a mesh using either its given material or a transparent "ghost" material.
        /// </summary>
        /// <param name="node">Current node</param>
        /// <param name="animated">Specifies whether animations should be played</param>
        /// <param name="showGhost">Indicates whether to substitute the mesh' material with a
        /// "ghost" surrogate material that allows looking through the geometry.</param>
        /// <param name="index">Mesh index in the scene</param>
        /// <param name="mesh">Mesh instance</param>
        /// <param name="flags"> </param>
        /// <returns></returns>
        protected override bool InternDrawMesh(Node node, bool animated, bool showGhost, int index, Mesh mesh, RenderFlags flags)
        {
            if (showGhost)
            {
                Owner.MaterialMapper.ApplyGhostMaterial(mesh, Owner.Raw.Materials[mesh.MaterialIndex],
                    flags.HasFlag(RenderFlags.Shaded), flags.HasFlag(RenderFlags.ForceTwoSidedLighting));
            }
            else
            {
                Owner.MaterialMapper.ApplyMaterial(mesh, Owner.Raw.Materials[mesh.MaterialIndex],
                    flags.HasFlag(RenderFlags.Textured),
                    flags.HasFlag(RenderFlags.Shaded),flags.HasFlag(RenderFlags.ForceTwoSidedLighting));
            }

            if (GraphicsSettings.Default.BackFaceCulling)
            {
                GL.FrontFace(FrontFaceDirection.Ccw);
                GL.CullFace(CullFaceMode.Back);
                GL.Enable(EnableCap.CullFace);
            }
            else
            {
                GL.Disable(EnableCap.CullFace);
            }

            var hasColors = mesh.HasVertexColors(0);
            var hasTexCoords = mesh.HasTextureCoords(0);

            var skinning = mesh.HasBones && animated;

            foreach (var face in mesh.Faces)
            {
                OpenTK.Graphics.OpenGL.PrimitiveType faceMode;
                switch (face.IndexCount)
                {
                    case 1:
                        faceMode = OpenTK.Graphics.OpenGL.PrimitiveType.Points;
                        break;
                    case 2:
                        faceMode = OpenTK.Graphics.OpenGL.PrimitiveType.Lines;
                        break;
                    case 3:
                        faceMode = OpenTK.Graphics.OpenGL.PrimitiveType.Triangles;
                        break;
                    default:
                        faceMode = OpenTK.Graphics.OpenGL.PrimitiveType.Polygon;
                        break;
                }

                GL.Begin(faceMode);
                for (var i = 0; i < face.IndexCount; i++)
                {
                    var indice = face.Indices[i];
                    if (hasColors)
                    {
                        var vertColor = AssimpToOpenTk.FromColor(mesh.VertexColorChannels[0][indice]);
                        GL.Color4(vertColor);
                    }
                    if (mesh.HasNormals)
                    {
                        Vector3 normal;
                        if (skinning)
                        {
                            Skinner.GetTransformedVertexNormal(node, mesh, (uint)indice, out normal);
                        }
                        else
                        {
                            normal = AssimpToOpenTk.FromVector(mesh.Normals[indice]);
                        }

                        GL.Normal3(normal);
                    }
                    if (hasTexCoords)
                    {
                        var uvw = AssimpToOpenTk.FromVector(mesh.TextureCoordinateChannels[0][indice]);
                        GL.TexCoord2(uvw.X, 1 - uvw.Y);
                    }

                    Vector3 pos;
                    if (skinning)
                    {
                        Skinner.GetTransformedVertexPosition(node, mesh, (uint)indice, out pos);
                    }
                    else
                    {
                        pos = AssimpToOpenTk.FromVector(mesh.Vertices[indice]);
                    }
                    GL.Vertex3(pos);
                }
                GL.End();
            }
            GL.Disable(EnableCap.CullFace);
            return skinning;
        }


        /// <summary>
        /// Recursive render function for drawing opaque geometry with no scaling 
        /// in the transformation chain. This is used for overlays, such as drawing
        /// the skeleton.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="visibleMeshesByNode"></param>
        /// <param name="flags"></param>
        /// <param name="invGlobalScale"></param>
        /// <param name="animated"></param>
        private void RecursiveRenderNoScale(Node node, Dictionary<Node, List<Mesh>> visibleMeshesByNode, RenderFlags flags,
            float invGlobalScale,
            bool animated, int currDispList)
        {
            // TODO unify our use of OpenTK and Assimp matrices 
            Matrix4x4 m;
            Matrix4 mConv;
            if (animated)
            {
                Owner.SceneAnimator.GetLocalTransform(node, out mConv);
                OpenTkToAssimp.FromMatrix(ref mConv, out m);
            }
            else
            {
                m = node.Transform;
            }

            // get rid of the scaling part of the matrix
            // TODO this can be done faster and Decompose() doesn't handle
            // non positively semi-definite matrices correctly anyway.

            Vector3D scaling;
            Assimp.Quaternion rotation;
            Vector3D translation;
            m.Decompose(out scaling, out rotation, out translation);

            rotation.Normalize();

            m = new Matrix4x4(rotation.GetMatrix()) * Matrix4x4.FromTranslation(translation);
            mConv = AssimpToOpenTk.FromMatrix(ref m);
            mConv.Transpose();

            if (flags.HasFlag(RenderFlags.ShowSkeleton))
            {
                var highlight = false;
                if (visibleMeshesByNode != null)
                {
                    List<Mesh> meshList;
                    if (visibleMeshesByNode.TryGetValue(node, out meshList) && meshList == null)
                    {
                        // If the user hovers over a node in the tab view, all of its descendants
                        // are added to the visible set as well. This is not the intended 
                        // behavior for skeleton joints, though! Here we only want to show the
                        // joint corresponding to the node being hovered over.

                        // Therefore, only highlight nodes whose parents either don't exist
                        // or are not in the visible set.
                        if (node.Parent == null || !visibleMeshesByNode.TryGetValue(node.Parent, out meshList) || meshList != null)
                        {
                            highlight = true;
                        }
                    }
                }
                OverlaySkeleton.DrawSkeletonBone(node, invGlobalScale, highlight);
            }

            GL.PushMatrix();
            GL.MultMatrix(ref mConv);
            for (int i = 0; i < node.ChildCount; i++)
            {
                RecursiveRenderNoScale(node.Children[i], visibleMeshesByNode, flags, invGlobalScale, animated, currDispList);
            }
            GL.PopMatrix();
        }

        /// <summary>
        /// Recursive render function for drawing normals with a constant size.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="visibleMeshesByNode"></param>
        /// <param name="flags"></param>
        /// <param name="invGlobalScale"></param>
        /// <param name="animated"></param>
        /// <param name="transform"></param>
        private void RecursiveRenderNormals(Node node, Dictionary<Node, List<Mesh>> visibleMeshesByNode, RenderFlags flags,
            float invGlobalScale,
            bool animated,
            Matrix4 transform, int currDispList)
        {
            // TODO unify our use of OpenTK and Assimp matrices
            Matrix4 mConv;
            if (animated)
            {
                Owner.SceneAnimator.GetLocalTransform(node, out mConv);
            }
            else
            {
                Matrix4x4 m = node.Transform;
                mConv = AssimpToOpenTk.FromMatrix(ref m);
            }

            mConv.Transpose();

            // The normal's position and direction are transformed differently, so we manually track the transform.
            transform = mConv * transform;

            if (flags.HasFlag(RenderFlags.ShowNormals))
            {
                List<Mesh> meshList = null;
                if (node.HasMeshes &&
                    (visibleMeshesByNode == null || visibleMeshesByNode.TryGetValue(node, out meshList)))
                {
                    foreach (var index in node.MeshIndices)
                    {
                        var mesh = Owner.Raw.Meshes[index];
                        if (meshList != null && !meshList.Contains(mesh))
                        {
                            continue;
                        }
                        if (currDispList == GetDispList(node.Name))
                        OverlayNormals.DrawNormals(node, index, mesh, mesh.HasBones && animated ? Skinner : null, invGlobalScale, transform);
                    }
                }
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                RecursiveRenderNormals(node.Children[i], visibleMeshesByNode, flags, invGlobalScale, animated, transform, currDispList);
            }
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */
