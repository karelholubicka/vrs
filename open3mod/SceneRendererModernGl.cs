///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [SceneRendererModernGl.cs]
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

namespace open3mod
{
    /// <summary>
    /// "Modern" OpenGl-based renderer using VBOs and shaders.
    /// </summary>
    public class SceneRendererModernGl : SceneRendererShared, ISceneRenderer
    {
        private RenderMesh[] _meshesVideo;
        private RenderMesh[] _meshesScreen;

        internal SceneRendererModernGl(Scene owner, Vector3 initposeMin, Vector3 initposeMax) 
            : base(owner, initposeMin, initposeMax)
        {
            _meshesVideo = new RenderMesh[owner.Raw.MeshCount];
            _meshesScreen = new RenderMesh[owner.Raw.MeshCount];
        }

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
        /// </summary>   
        public void Render(ICameraController cam, Dictionary<Node, List<Mesh>> visibleMeshesByNode,
            bool visibleSetChanged,
            bool texturesChanged,
            RenderFlags flags, 
            Renderer renderer)
        {
            RenderControl.GLError("ModernRenderStart");

            GL.Enable(EnableCap.DepthTest);
            GL.FrontFace(FrontFaceDirection.Ccw);
            if (flags.HasFlag(RenderFlags.Wireframe))
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }

            var view = cam == null ? Matrix4.LookAt(0, 10, 5, 0, 0, 0, 0, 1, 0) : cam.GetView();

            var tmp = InitposeMax.X - InitposeMin.X;
            tmp = Math.Max(InitposeMax.Y - InitposeMin.Y, tmp);
            tmp = Math.Max(InitposeMax.Z - InitposeMin.Z, tmp);
            int logScale = (int)Math.Truncate(Math.Log10(tmp * 10 / 50)); //  Up to 50units max size = 50m: keep scale (for smaller scenes).
            float scale = 1;
            for (int i = 0; i < logScale; i++) scale = scale / 10;
            Owner.Scale = scale;
            Matrix4 world = Matrix4.Identity;//want to keep unity in our world


            // set a proper perspective matrix for rendering
            int[] CurrentViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, CurrentViewport);
            var aspectRatio = (float)CurrentViewport[2]/CurrentViewport[3];
            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(cam.GetFOV(), aspectRatio, renderer.zNear, renderer.zFar);
            Owner.MaterialMapper.SetMatrices(world, view, perspective);
            PushWorld(ref world);
            Owner.MaterialMapper.BeginScene(renderer, flags.HasFlag(RenderFlags.UseSceneLights)); //here we switch on lights

            // If textures changed, we may need to upload some of them to VRAM.
            if (texturesChanged)
            {
                UploadTextures();
            }
            var animated = Owner.SceneAnimator.IsAnimationActive;
            int currDispList=0;
            int count = 1;
            switch (cam.GetScenePartMode())
            {
                case ScenePartMode.Background: currDispList = 0;break;
                case ScenePartMode.Foreground: currDispList = 2; break;
                case ScenePartMode.Others: currDispList = 1; break;
                case ScenePartMode.GreenScreen: currDispList = 3; break;
                case ScenePartMode.All: currDispList = 0;count = 4; break;
                default: break;//at other modes we do not render anything
            }

            for (int countDispList = 0; countDispList < count; countDispList++)
            {

                var needAlpha = RecursiveRender(Owner.Raw.RootNode, visibleMeshesByNode, flags, animated, currDispList);
                var needAlphaAnim = RecursiveRender(Owner.Raw.RootNode, visibleMeshesByNode, flags, animated, currDispList + 4);
                if (flags.HasFlag(RenderFlags.ShowSkeleton) || flags.HasFlag(RenderFlags.ShowNormals))
                {
                    //RecursiveRenderNoScale(Owner.Raw.RootNode, visibleMeshesByNode, flags, 1.0f / tmp, animated);
                }
                if (needAlpha)
                {
                    // handle semi-transparent geometry              
                    RecursiveRenderWithAlpha(Owner.Raw.RootNode, visibleMeshesByNode, flags, animated, currDispList);
                }
                if (needAlphaAnim)
                {
                    // handle semi-transparent geometry              
                    RecursiveRenderWithAlpha(Owner.Raw.RootNode, visibleMeshesByNode, flags, animated, currDispList + 4);
                }
                currDispList++;

                /* RenderFlags application:
                Wireframe = 0x1, - Scene renderer,OK
        Shaded = 0x2, - MaterialMapper.ApplyMaterial, OK
        ShowBoundingBoxes = 0x4,
        ShowNormals = 0x8, - Scene renderer, unused in GL4
        ShowSkeleton = 0x10,  - Scene renderer, unused in GL4
        Textured = 0x20, - MaterialMapper.ApplyMaterial, OK
        ShowGhosts = 0x40, unused, always ON, InternDrawMesh applies own showGhost to MaterialMapper.Apply(Ghost)Material,
        UseSceneLights = 0x80, - MaterialMapper.BeginScene, OK
        */

            }
            PopWorld();
            // always switch back to FILL
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.DepthTest);
            RenderControl.GLError("SceneRendererModernGLEnd");
        }

        private Stack<Matrix4> worlds = new Stack<Matrix4>();
        protected override void PushWorld(ref Matrix4 world)
        {
            Matrix4 newWorld = world * (worlds.Count > 0 ? worlds.Peek() : Matrix4.Identity);
            Owner.MaterialMapper.SetWorld(newWorld);
            worlds.Push(newWorld);
        }

        protected override void PopWorld()
        {
            worlds.Pop();
        }


        protected override bool InternDrawMesh(Node node, bool animated, bool showGhost, int index, Mesh mesh, RenderFlags flags)
        {
            RenderMesh[] _meshes = _meshesScreen;
            if (flags.HasFlag(RenderFlags.ToVideo)) _meshes = _meshesVideo;
            if (_meshes[index] == null) 
            {
                _meshes[index] = new RenderMesh(mesh);
            }

            if (showGhost)
            {
                Owner.MaterialMapper.ApplyGhostMaterial(mesh, Owner.Raw.Materials[mesh.MaterialIndex],
                    flags.HasFlag(RenderFlags.Shaded), flags.HasFlag(RenderFlags.ForceTwoSidedLighting));
            }
            else
            {
                Owner.MaterialMapper.ApplyMaterial(mesh, Owner.Raw.Materials[mesh.MaterialIndex],
                    flags.HasFlag(RenderFlags.Textured),
                    flags.HasFlag(RenderFlags.Shaded), flags.HasFlag(RenderFlags.ForceTwoSidedLighting));
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

            _meshes[index].Render(flags);
            return true;
        }


        public void Dispose()
        {          
            GC.SuppressFinalize(this);
        }


#if DEBUG
        ~SceneRendererModernGl()
        {
            // OpenTk is unsafe from here, explicit Dispose() is required.
            Debug.Assert(false);
        }
#endif
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 