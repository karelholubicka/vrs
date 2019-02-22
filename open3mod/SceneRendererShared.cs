///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [SceneRendererShared.cs]
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

namespace open3mod
{
    /// <summary>
    /// Shared utility class for SceneRendererClassicGl and SceneRendererModernGl
    /// that encapsulates logic to traverse the scene graph, determine rendering
    /// passes and finally produce draw calls which are then dispatched to the
    /// (deriving) renderer implementation.
    /// </summary>
    public abstract class SceneRendererShared
    {
        protected readonly Scene Owner;
        protected readonly Vector3 InitposeMin;
        protected readonly Vector3 InitposeMax;
        protected readonly CpuSkinningEvaluator Skinner;
        protected readonly bool[] IsAlphaMaterial;


        /// <summary>
        /// Constructs an instance given a scene with its bounds (AABB)
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="initposeMin"></param>
        /// <param name="initposeMax"></param>
        protected SceneRendererShared(Scene owner, Vector3 initposeMin, Vector3 initposeMax)
        {
            Owner = owner;
            InitposeMin = initposeMin;
            InitposeMax = initposeMax;

            Debug.Assert(Owner.Raw != null);    
            Skinner = new CpuSkinningEvaluator(owner);

            IsAlphaMaterial = new bool[owner.Raw.MaterialCount];
            for (int i = 0; i < IsAlphaMaterial.Length; ++i)
            {
                IsAlphaMaterial[i] = Owner.MaterialMapper.IsAlphaMaterial(owner.Raw.Materials[i]);
            }
        }


        /// <summary>
        /// Make sure all textures required for the materials in the scene are uploaded to VRAM.
        /// </summary>
        protected void UploadTextures()
        {
            if (Owner.Raw.Materials == null)
            {
                return;
            }
       //     Owner.Renderer.MainWindow.renderControl.SetRenderTarget(RenderControl.RenderTarget.ScreenCompat); this is stupid
            var i = 0;
            foreach (var mat in Owner.Raw.Materials)
            {
                if (Owner.MaterialMapper.UploadTextures(mat))
                {
                    IsAlphaMaterial[i] = Owner.MaterialMapper.IsAlphaMaterial(mat);
                }
                ++i;
            }
        }
        /// <summary>

        /// </summary>
        protected void UploadDynamicTextures()
        {
            if (Owner.Raw.Materials == null)
            {
                return;
            }
            var i = 0;
            foreach (var mat in Owner.Raw.Materials)
            {
                if (Owner.MaterialMapper.UploadDynamicTextures(mat))
                {
                    IsAlphaMaterial[i] = Owner.MaterialMapper.IsAlphaMaterial(mat);
                }
                ++i;
            }
        }

        /* display lists:
        0: Background;
        1: Else (always visible);
        2: Foreground;
        3: GreenScreen;
        4: BackgroundAnimated;
        5: Else (always visible) Animated;
        6: ForegroundAnimated;
        7: GreenScreenAnimated;
        Animated - list is refreshed each frame
        */
        /// <summary>
        /// <sorts geometry into Displists based on its name>
        /// </summary>   
        protected int GetDispList(string name)
        {
            switch (name.Substring(0, 1))
            {
                case "B":
                    return 0;
                case "X":
                    return 1;
                case "F":
                    return 2;
                case "G":
                    return 3;
                case "b":
                    return 4;
                case "a":
                    return 5; //animated others
                case "x":
                    return 5; //oher animated others
                case "f":
                    return 6;
                case "g":
                    return 7;
                default:
                    return 0;//everything else/unnamed goes automatically to BKGD
            }
        }

        /// <summary>
        /// Recursive rendering function
        /// </summary>
        /// <param name="node">Current node</param>
        /// <param name="visibleMeshesByNode"> </param>
        /// <param name="flags">Rendering flags</param>
        /// <param name="animated">Play animation?</param>
        /// <returns>whether there is any need to do a second render pass with alpha blending enabled</returns>
        protected bool RecursiveRender(Node node,
            Dictionary<Node, List<Mesh>> visibleMeshesByNode,
            RenderFlags flags, bool animated, int currDispList)
        {
            var needAlpha = false;

            Matrix4 m;
            if (animated)
            {
                Owner.SceneAnimator.GetLocalTransform(node, out m);
            }
            else
            {
                m = AssimpToOpenTk.FromMatrix(node.Transform);
            }
            //ASSIMP410 hack to fix FBX animation import
            // bool replaceTrafo = node.Parent != null && animated && node.Parent.Name.StartsWith(Scene.NodeNameGenericPrefix) && (m!= Matrix4.Identity);
            // if (replaceTrafo) PopWorld();

            m.Transpose();
            // TODO for some reason, all OpenTk matrices need a ^T - we should clarify our conventions somewhere
            PushWorld(ref m);

            if ((node.HasMeshes) && (currDispList == GetDispList(node.Name)))
            {
                needAlpha = DrawOpaqueMeshes(node, visibleMeshesByNode, flags, animated);
            }

            for (var i = 0; i < node.ChildCount; i++)
            {
                needAlpha = RecursiveRender(node.Children[i], visibleMeshesByNode, flags, animated, currDispList) || needAlpha;
            }
            //ASSIMP410 hack to fix FBX animation import - add to alpha render too
            // if (!replaceTrafo)
            PopWorld();
            return needAlpha;
        }


        /// <summary>
        /// Recursive rendering function for semi-transparent (i.e. alpha-blended) meshes.
        /// 
        /// Alpha blending is not globally on, meshes need to do that on their own. 
        /// 
        /// This render function is called _after_ solid geometry has been drawn, so the 
        /// relative order between transparent and opaque geometry is maintained. There
        /// is no further ordering within the alpha rendering pass.
        /// </summary>
        /// <param name="node">Current node</param>
        /// <param name="visibleNodes">Set of visible meshes</param>
        /// <param name="flags">Rendering flags</param>
        /// <param name="animated">Play animation?</param>
        protected void RecursiveRenderWithAlpha(Node node, Dictionary<Node, List<Mesh>> visibleNodes,
            RenderFlags flags,
            bool animated, int currDispList)
        {
            Matrix4 m;
            if (animated)
            {
                Owner.SceneAnimator.GetLocalTransform(node, out m);
            }
            else
            {
                m = AssimpToOpenTk.FromMatrix(node.Transform);
            }
            m.Transpose();
            // TODO for some reason, all OpenTk matrices need a ^T - we should clarify our conventions somewhere
            PushWorld(ref m);

            // the following permutations could be compacted into one big loop with lots of
            // condition magic, but at the cost of readability and also performance.
            // we therefore keep it redundant and stupid.
            if ((node.HasMeshes) && (currDispList == GetDispList(node.Name)))
                {
                DrawAlphaMeshes(node, visibleNodes, flags, animated);
            }

            for (var i = 0; i < node.ChildCount; i++)
            {
                RecursiveRenderWithAlpha(node.Children[i], visibleNodes, flags, animated, currDispList);
            }
            PopWorld();
        }


        /// <summary>
        /// Draw the opaque (i.e. not semi-transparent) meshes pertaining to a node
        /// and checks whether the node has any semi-transparent meshes to be drawn later.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="visibleMeshesByNode"></param>
        /// <param name="flags"></param>
        /// <param name="animated"></param>
        /// <returns>Whether there were any meshes skipped because they are not opaque</returns>
        protected bool DrawOpaqueMeshes(Node node, Dictionary<Node, List<Mesh>> visibleMeshesByNode, 
            RenderFlags flags, 
            bool animated)
        {
            // the following permutations could be compacted into one big loop with lots of
            // condition magic, but at the cost of readability and also performance.
            // we therefore keep it redundant and stupid.
            var needAlpha = false;
            if (visibleMeshesByNode == null)
            {
                // everything is visible. alpha-blended materials are delayed for 2nd pass
                foreach (var index in node.MeshIndices)
                {
                    var mesh = Owner.Raw.Meshes[index];
                    if (IsAlphaMaterial[mesh.MaterialIndex])
                    {
                        needAlpha = true;
                        continue;
                    }

                    var skinning = DrawMesh(node, animated, false, index, mesh, flags);
                    if (flags.HasFlag(RenderFlags.ShowBoundingBoxes))
                    {
                        OverlayBoundingBox.DrawBoundingBox(node, index, mesh, skinning ? Skinner : null);
                    }
                }
            }
            else
            {
                List<Mesh> meshList;
                if (visibleMeshesByNode.TryGetValue(node, out meshList))
                {
                    // some meshes of this node are visible. alpha-blended materials are delayed for 2nd pass
                    foreach (var index in node.MeshIndices)
                    {
                        var mesh = Owner.Raw.Meshes[index];

                        if (IsAlphaMaterial[mesh.MaterialIndex] || (meshList != null && !meshList.Contains(mesh)))
                        {
                            needAlpha = true;
                            continue;
                        }

                        var skinning = DrawMesh(node, animated, false, index, mesh, flags);
                        if (flags.HasFlag(RenderFlags.ShowBoundingBoxes))
                        {
                            OverlayBoundingBox.DrawBoundingBox(node, index, mesh, skinning ? Skinner : null);
                        }
                    }
                }
                else
                {
                    // node not visible, draw ghosts in 2nd pass
                    needAlpha = true;
                }
            }
            return needAlpha;
        }



        /// <summary>
        /// Draw the semi-transparent meshes pertaining to a node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="visibleNodes"></param>
        /// <param name="flags"></param>
        /// <param name="animated"></param>
        protected void DrawAlphaMeshes(Node node, Dictionary<Node, List<Mesh>> visibleNodes, 
            RenderFlags flags, 
            bool animated)
        {
            // the following permutations could be compacted into one big loop with lots of
            // condition magic, but at the cost of readability and also performance.
            // we therefore keep it redundant and stupid.
            List<Mesh> meshList;
            if (visibleNodes == null)
            {
                // render everything with alpha materials
                foreach (var index in node.MeshIndices)
                {
                    var mesh = Owner.Raw.Meshes[index];
                    if (IsAlphaMaterial[mesh.MaterialIndex])
                    {
                        DrawMesh(node, animated, false, index, mesh, flags);
                    }
                }
            }
            else if (visibleNodes.TryGetValue(node, out meshList))
            {
                if (meshList == null)
                {
                    // render everything with alpha materials 
                    foreach (var index in node.MeshIndices)
                    {
                        var mesh = Owner.Raw.Meshes[index];
                        if (IsAlphaMaterial[mesh.MaterialIndex])
                        {
                            DrawMesh(node, animated, false, index, mesh, flags);
                        }
                    }
                }
                else
                {
                    // render everything that has either alpha materials or is not in the
                    // list of visible meshes for this node.
                    foreach (var index in node.MeshIndices)
                    {
                        var mesh = Owner.Raw.Meshes[index];
                        if (!meshList.Contains(mesh))
                        {
                            DrawMesh(node, animated, true, index, mesh, flags);
                            continue;
                        }
                        if (IsAlphaMaterial[mesh.MaterialIndex])
                        {
                            DrawMesh(node, animated, false, index, mesh, flags);
                        }
                    }
                }
            }
            else
            {
                // node not visible, render only ghosts
                foreach (var index in node.MeshIndices)
                {
                    var mesh = Owner.Raw.Meshes[index];
                    DrawMesh(node, animated, true, index, mesh, flags);
                }
            }
        }

        protected abstract void PushWorld(ref Matrix4 world);
        protected abstract void PopWorld();

        /// <summary>
        /// Abstract method to draw a mesh attached to a node. This is to be implemented
        /// by whichever rendering method should be used.
        /// 
        /// The thread is guaranteed to own the monitor for the |mesh|, making read
        /// access to the mesh safe.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="animated"></param>
        /// <param name="showGhost"></param>
        /// <param name="index"></param>
        /// <param name="mesh"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        protected abstract bool InternDrawMesh(Node node, bool animated, 
            bool showGhost, 
            int index, 
            Mesh mesh,
            RenderFlags flags);


        /// <summary>
        /// Obtains a lock on a mesh and draws it. Also handles mesh overrides.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="animated"></param>
        /// <param name="showGhost"></param>
        /// <param name="index"></param>
        /// <param name="mesh"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        protected bool DrawMesh(Node node, bool animated,
            bool showGhost,
            int index,
            Mesh mesh,
            RenderFlags flags)
        {
            mesh = Owner.GetOverrideMesh(mesh) ?? mesh;
            lock (mesh)
            {
                return InternDrawMesh(node, animated, showGhost, index, mesh, flags);
            }
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 