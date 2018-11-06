///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [MaterialMapperModernGl.cs]
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Assimp;
using OpenTK;
using System.Diagnostics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace open3mod
{
    public struct GLLight
    {
        public int lightType;
        public Vector3 position;
        public Vector3 direction;
        public float cutOff;
        public float outerCutOff;

        public float constant;
        public float linear;
        public float quadratic;

        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 specular;
      //  public const size = 21 *4 byte = (1 + 3 + 3 + 1 + 1 + 1 + 1 + 1 + 3 + 3 + 3 + 1)*4;
    };
    
    public sealed class MaterialMapperModernGl : MaterialMapper
    {

        internal MaterialMapperModernGl(Scene scene)
            : base(scene)
        {
        }

        private readonly ShaderGen _shaderGen = new ShaderGen();
        private Matrix4 _World;
        private Matrix4 _View;
        private Matrix4 _Perspective;
        private int _LightCount;
        private GLLight[] _GLLights;
        private float _SceneBrightness = 1.0f;
        private bool _UseSceneLights = false;
        private Node[] lightNodes;

#if DEBUG
        ~MaterialMapperModernGl()
        {
            Debug.Assert(false);
        }
#endif

        public override void Dispose()
        {
            _shaderGen.Dispose();
            GC.SuppressFinalize(this);
            RenderControl.GLError("MaterialNewDispose");

        }

        public override void SetMatrices(Matrix4 world, Matrix4 view, Matrix4 perspective)
        {
            _World = world;
            _View = view;
            _Perspective = perspective;

        }

        public override void SetWorld(Matrix4 world)
        {
            _World = world;
        }


        public override void ApplyMaterial(Mesh mesh, Material mat, bool textured, bool shaded, bool twoSided)
        {
            RenderControl.GLError("StartMaterialMapper");
            ShaderGen.GenFlags flags = 0;
            var hasAlpha = false;
            var hasTexture = false;
            var file = _scene.Renderer.MainWindow.UiState.ActiveTab.File;

            // note: keep this up-to-date with the code in MaterialMapper.UploadTextures()
            for (int i = 0; i < Renderer.modernGLUsedTextureTypeCount; i++)
            {
                TextureType currTextureType = (TextureType)((int)TextureType.Diffuse + i);
                GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + i));
                GL.BindTexture(TextureTarget.Texture2D, Renderer.modernGLTextureType[i]); //we use own texture always, even when textures are off, to supply preset values
                if (textured && mat.GetMaterialTextureCount(currTextureType) > 0)
                {
                    hasTexture = true;
                    //flags |= ShaderGen.GenFlags.Texture; flag not used, we always have some texture
                    TextureSlot tex;
                    mat.GetMaterialTexture(currTextureType, 0, out tex);
                    var gtex = _scene.TextureSet.GetOriginalOrReplacement(tex.FilePath);
                    hasAlpha = hasAlpha || gtex.HasAlpha == Texture.AlphaState.HasAlpha;
                    if (gtex.State == Texture.TextureState.GlTextureCreated)
                    {
                        gtex.BindGlTexture();
                    }
                }
            }
            GL.ActiveTexture(TextureUnit.Texture0);
            RenderControl.GLError("EndTextureSettings");
            if (shaded)
            {
                flags |= ShaderGen.GenFlags.Lighting;
            }
            var hasColors = mesh != null && mesh.HasVertexColors(0);
            if (hasColors)
            {
                flags |= ShaderGen.GenFlags.VertexColor;
            }
            if (_UseSceneLights)
            {
                flags |= ShaderGen.GenFlags.PhongSpecularShading;
            }
            if ((mat.IsTwoSided)||(twoSided))
            {
                flags |= ShaderGen.GenFlags.TwoSide;
            }
            Shader shader = _shaderGen.GenerateOrGetFromCache(flags, _LightCount > 0 ? _LightCount : 1 );
            shader.BindIfNecessary();
            Matrix4 curView = Matrix4.CreateScale(_scene.Scale) * _View;
            shader.SetMat4("WorldViewProjection", _World * curView * _Perspective);
            Matrix4 cameraPos = _View.ClearRotation();
            Matrix4 cameraRotation = _View.ClearTranslation();
            cameraRotation.Transpose();
            Matrix4 cam = Matrix4.Identity;
            cam =  cameraPos * cameraRotation;
            Vector3 cameraPosition = cam.ExtractTranslation()/ _scene.Scale;//does not work for orbitcontroller
            cameraPosition.Z = - cameraPosition.Z;

            //            cameraPosition = new Vector3(200,100,-100); //1m = 100units and positive 
            cameraPosition.Z = -cameraPosition.Z;
            shader.SetVec3("CameraPosition", -cameraPosition);
            shader.SetMat4("World", _World); 
            shader.SetMat4("WorldView", _World * curView); //_world* curView keeps light source at "fixed" position during rotating of the model
            shader.SetFloat("SceneBrightness", _SceneBrightness);
            RenderControl.GLError("UniformSettings");
            shader.SetInt("material.diffuse", 0);
            shader.SetInt("material.specular", 1);
            shader.SetInt("material.ambient", 2);
            shader.SetInt("material.emissive", 3);
            shader.SetInt("material.height", 4);
            shader.SetInt("material.normal", 5);
            shader.SetLights(_GLLights, _LightCount);
            RenderControl.GLError("UniformSettings");

            // note: keep semantics of hasAlpha consistent with IsAlphaMaterial()
            var alpha = 1.0f;
            if (mat.HasOpacity)
            {
                alpha = mat.Opacity;
                if (alpha < AlphaSuppressionThreshold) // suppress zero opacity, this is likely wrong input data
                {
                    alpha = 1.0f;
                }
            }
            var color = new Color4(.8f, .8f, .8f, 1.0f);
            if (mat.HasColorDiffuse)
            {
                color = AssimpToOpenTk.FromColor(mat.ColorDiffuse);
                if (color.A < AlphaSuppressionThreshold) // s.a.
                {
                    color.A = 1.0f;
                }
            }
            color.A *= alpha;
            hasAlpha = hasAlpha || color.A < 1.0f;

            if (shaded)
            {
                // if the material has a texture but the diffuse color texture is all black,
                // then heuristically assume that this is an import/export flaw and substitute
                // white.
                if (hasTexture && color.R < 1e-3f && color.G < 1e-3f && color.B < 1e-3f)
                {
                    color = Color4.White;
                }
                shader.SetCol4("MaterialDiffuse_Alpha", color);

                float strength = 1;
                if (mat.HasShininessStrength)
                {
                    strength = mat.ShininessStrength;
                    if (file.EndsWith("fbx")) strength = strength * 2.0f; // fbx has basic value 0.5

                }
                color = new Color4(0, 0, 0, 1.0f);
                if (mat.HasColorSpecular)
                {
                    color = AssimpToOpenTk.FromColor(mat.ColorSpecular);
                    color = new Color4(color.R * strength, color.G * strength, color.B* strength, color.A);
                }
                shader.SetCol4("MaterialSpecular", color);

                color = new Color4(.2f, .2f, .2f, 1.0f);
                if (mat.HasColorAmbient)
                {
                    color = AssimpToOpenTk.FromColor(mat.ColorAmbient);
                }
                shader.SetCol4("MaterialAmbient", color);

                color = new Color4(0, 0, 0, 1.0f);
                if (mat.HasColorEmissive)
                {
                    color = AssimpToOpenTk.FromColor(mat.ColorEmissive);
                }
                shader.SetCol4("MaterialEmissive", color);

                float shininess = 1;
                if (mat.HasShininess)
                {
                    shininess = mat.Shininess;
                }
                // todo: I don't even remember how shininess strength was supposed to be handled in assimp .. Scales the specular color of the material.
                var exp = shininess;
                if (file.EndsWith("blend")) exp = exp / 5.11f; // 511 blender value = 100 fbx value //skip this, if is desired to match FBX view in Fusion
                if (exp >= 128.0f) // 128 is the maximum exponent as per the Gl spec
                {
                    exp = 128.0f;
                }
                shader.SetFloat("MaterialShininess", exp);

                
                //Shininess may be at mat.ColorSpecular.a too..?? but in FBX is 
            }
            else if (!hasColors)
            {
                shader.SetCol4("MaterialDiffuse_Alpha", color);
            }

            if (hasAlpha)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                GL.DepthMask(false);
            }
            else
            {
                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);
            }
            RenderControl.GLError("EndMaterialMapper");

        }
        //public override void UnapplyMaterial(Mesh, Material, Tex)

        public override void ApplyGhostMaterial(Mesh mesh, Material material, bool shaded, bool twoSided)
        {
            var color = new Color4D(.6f, .6f, .9f, 0.15f);
            var oldColor = material.ColorDiffuse;
            material.ColorDiffuse = color;
            ApplyMaterial(mesh, material, false, shaded, twoSided);
            material.ColorDiffuse = oldColor;
        }

        private Vector3 Colo3DToVector3(Color3D col)
        {
            return new Vector3(col.R, col.G, col.B);
        }

        private Vector3 Vector3DToVector3(Assimp.Vector3D vec)
        {
            return new Vector3(vec.X,vec.Y,vec.Z);
        }

        public override void BeginScene(Renderer renderer, bool useSceneLights = true)
        {
            _UseSceneLights = useSceneLights;
            if (useSceneLights)
            {
                if (lightNodes == null) lightNodes = GenerateLightNodes(); //may be slow, need to do it only once, if possible
                var lights = GenerateLights();
                _LightCount = LightCount();
                _GLLights = new GLLight[_LightCount];
                for (var i = 0; i < _LightCount; i++)
                {
                    Node node = lightNodes[i];
                    if ((node != null) && ((node == _scene.ActiveLight) || (null == _scene.ActiveLight)))
                    {
                        var mat1 = Matrix4.Identity;
                        var cur = node;


                    /*    if (animated)
                        {
                            
                        }
                        else
                        {
                            m = AssimpToOpenTk.FromMatrix(node.Transform);
                        }*/

                        while (cur != null)
                        {
                            var trafo = AssimpToOpenTk.FromMatrix(cur.Transform);
                            //_scene.SceneAnimator.GetLocalTransform(node.Name, out trafo);
                            trafo.Transpose();
                            mat1 = trafo * mat1;
                            cur = cur.Parent;
                        }
                        /*   while (cur != null)
                           {
                               var trafo = cur.Transform;
                               trafo.Transpose();
                               mat1 = trafo * mat1;
                               cur = cur.Parent;
                           }*/

                        mat1.Transpose();
                        _scene.SceneAnimator.GetGlobalTransform(node.Name, out mat1); //well identical result :-)
                        _GLLights[i].lightType = (int)lights[i].LightType;
                        //Directional = 0x1,, Point = 0x2, Spot = 0x3,
                        float lightScale = _GLLights[i].lightType == 2 ? 0.01f : 1f;

                        //here move position info into lights[]
                        //   Vector3 lposTemp = new Vector3(mat1.A4, mat1.B4, mat1.C4);
                        Vector3 lposTemp = new Vector3(mat1.M14, mat1.M24, mat1.M34);
                        mat1.Transpose();//yes, again the transpose thing...
                        Vector3 lightStandardDir = new Vector3(-lights[i].Direction.X, -lights[i].Direction.Y, -lights[i].Direction.Z);
                       // lightStandardDir = new Vector3(0,1,0);
                        Vector3.TransformVector(ref lightStandardDir, ref mat1, out Vector3 ldirTemp);
                        //TransformNormal did no work, produced identical results for different light directions

                        _GLLights[i].position = lposTemp;
                        _GLLights[i].direction = ldirTemp;

                        float baseBrightness = _scene.Scale * 100;
                        float baseDirBrightness = 10;
                        //lightScale = _scene.Scale;
                       // lightScale = 1f;
                        _GLLights[i].ambient = Colo3DToVector3(lights[i].ColorAmbient) * lightScale;
                        _GLLights[i].specular = Colo3DToVector3(lights[i].ColorSpecular) * lightScale;
                        _GLLights[i].diffuse = Colo3DToVector3(lights[i].ColorDiffuse) * lightScale;

                        _GLLights[i].constant = lights[i].AttenuationConstant;
                        _GLLights[i].linear = lights[i].AttenuationLinear * _scene.Scale;
                        _GLLights[i].quadratic = lights[i].AttenuationQuadratic * _scene.Scale * _scene.Scale;
                        _GLLights[i].outerCutOff = lights[i].AngleOuterCone;
                        _GLLights[i].cutOff = lights[i].AngleInnerCone;
                    }
                }
                _SceneBrightness = 0.1f + GraphicsSettings.Default.OutputBrightness / 100.0f;
                int neededUniformComponents = 21 * 4 * _LightCount + 15 * 4 * 4;//approximately
                if (neededUniformComponents > GL.GetInteger(GetPName.MaxFragmentUniformComponents))
                {
                    throw new Exception("Too many lights");
                }
            }
            else
            {
                _LightCount = 1;

                // light direction
                var dir = new Vector3(1, 0, 1); //001 from Z, 100 from X, 010 fromY side
                var mat = renderer.LightRotation;
                Vector3.TransformNormal(ref dir, ref mat, out dir);
                // light color
                var col = new Vector3(1, 1, 1);
                _SceneBrightness = (0.00f + 5f * GraphicsSettings.Default.OutputBrightness / 100.0f);
                _GLLights = new GLLight[_LightCount];
                _GLLights[0].lightType = 1;
                _GLLights[0].direction = dir;
                _GLLights[0].specular = col;
                _GLLights[0].diffuse = col;
            }
            RenderControl.GLError("BeginSceneEnd");
        }
        public override void EndScene(Renderer renderer)
        {
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 