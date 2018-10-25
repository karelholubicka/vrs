///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [MaterialMapperClassicGl.cs]
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

using Assimp;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace open3mod
{
    public sealed class MaterialMapperClassicGl : MaterialMapper
    {

        internal MaterialMapperClassicGl(Scene scene)
            : base(scene)
        { }

        public override void SetMatrices(Matrix4 world, Matrix4 view, Matrix4 perspective)
        {
        }
        public override void SetWorld(Matrix4 world)
        {
        }


        public override void Dispose()
        {
            // no dispose semantics in this implementation
        }



        public override void ApplyMaterial(Mesh mesh, Material mat, bool textured, bool shaded, bool twoSided)
        {
            ApplyFixedFunctionMaterial(mesh, mat, textured, shaded);
        }


        public override void ApplyGhostMaterial(Mesh mesh, Material material, bool shaded, bool twoSided)
        {
            ApplyFixedFunctionGhostMaterial(mesh, material, shaded);
        }

        private float[] Colo3DToFloat(Color3D col)
        {
            float[] floatType = { col.B, col.G, col.R, 1};
            return floatType;
        }
        private float[] Vector3ToFloat(Vector3 col)
        {
            float[] floatType = { col.X, col.Y, col.Z, 1 };
            return floatType;
        }
        private float[] Vector3DToFloat(Vector3D col)
        {
            float[] floatType = { col.X, col.Y, col.Z, 1 };
            return floatType;
        }

        public override void BeginScene(Renderer renderer, bool useSceneLights = true)
        {
            /*   GL.GetInteger(GL_MAX_LIGHTS)
               glGetIntegerv along with GL_MAX_LIGHTS.*/
            int GLMaxLights = 8;
            for (var i = 0; i < GLMaxLights; ++i)
            {
                GL.Disable(EnableCap.Light0 + i);
            }
            if (useSceneLights)
            {
                GL.ShadeModel(ShadingModel.Smooth);
                GL.LightModel(LightModelParameter.LightModelAmbient, new[] { 0.03f, 0.03f, 0.03f, 1 }); //nastaví základní osvìtlení
                GL.LightModel(LightModelParameter.LightModelLocalViewer, 1.0f);
                GL.LightModel(LightModelParameter.LightModelTwoSide, 1.0f);
                GL.Enable(EnableCap.Lighting);
                var lightNodes = GenerateLightNodes();
                Light[] lights = GenerateLights();
                for (var i = 0; i < LightCount(); ++i)
                {
                    if (i < 8)// GL.Light.)
                        {
                            Node node = lightNodes[i];
                        if ((node != null) && ((node == _scene.ActiveLight) || (null == _scene.ActiveLight)))
                        {
                            //                        var matrix4X4 = node.Transform;
                            var mat1 = Matrix4x4.Identity;
                            var cur = node;
                            while (cur != null)
                            {
                                var trafo = cur.Transform;
                                trafo.Transpose();
                                mat1 = trafo * mat1;
                                cur = cur.Parent;
                            }
                            mat1.Transpose();

                            var mat = renderer.LightRotation;
                            var light_type = lights[i].LightType;
                            Vector3 lposTemp = new Vector3(mat1.A4, mat1.B4, mat1.C4);
                            Vector3 ldirTemp = new Vector3(mat1.B1, -mat1.B2, mat1.B3); //partially a guess, needed verification
                            float[] light_position = { -0f, 0f, -2f, 1.0f }; //X doprava, Y nahoru, - Z vpøed
                            light_position = Vector3ToFloat(lposTemp);
                            if (light_type == LightSourceType.Directional)
                            {
                               // Vector3.TransformNormal(ref ldirTemp, ref mat, out ldirTemp); //only if we want to change dir lights
                                light_position = Vector3ToFloat(-ldirTemp);
                                light_position[3] = 0.0f;
                                //If the w component of the position is 0, the light is treated as a directional source with no actual position
                            }

                            float[] light_ambient = { 0.0f, 0.0f, 0.0f, 1.0f }; // na tohle v zasade reaguji nektere predmety, rozsah 0-1
                            light_ambient = Colo3DToFloat(lights[i].ColorAmbient);
                            float[] light_diffuse = { 0.2f, 0.2f, 0.2f, 1.0f };//jak moc svítí
                            light_diffuse = Colo3DToFloat(lights[i].ColorDiffuse); // 100); //v Assimp je v %
                            float[] light_specular = { 1f, 1f, 1f, 1.0f };//not saved in dae, we assume 1
                            light_specular = Colo3DToFloat(lights[i].ColorSpecular / 100*2);// / 100);
                            //diffuse+specular are just like two lights

                            var coltmp = new Vector3D(light_diffuse[0], light_diffuse[1], light_diffuse[2]);
                            coltmp *= (0.0f + 2f * GraphicsSettings.Default.OutputBrightness / 100.0f) * 2f;
                            if (light_type != LightSourceType.Point) coltmp *= 0.01f;
                            light_diffuse = Vector3DToFloat(coltmp);
    
                            float light_aic = lights[i].AngleInnerCone;
                            float light_aoc = lights[i].AngleOuterCone;
                            float light_ac = lights[i].AttenuationConstant * _scene.Scale;
                            float light_al = lights[i].AttenuationLinear * _scene.Scale;
                            float light_aq = lights[i].AttenuationQuadratic * _scene.Scale *_scene.Scale;

                            //float[] spot_direction = { 0, 0, -1 };//(0,-1,0)directly down
                            float[] spot_direction = Vector3DToFloat(lights[i].Direction); //makes sense only when cutoff is set
                            float light_cutoff = 180;
                            if (light_type == LightSourceType.Spot)
                            {
                                //Vector3.TransformNormal(ref ldirTemp, ref mat, out ldirTemp); //only if we want to change spot lights direction
                                spot_direction = Vector3ToFloat(ldirTemp);
                                light_cutoff = light_aic * 180 / MathHelper.Pi;
                            }
                            float lessAttenuation =  _scene.Scale;
                            light_ac = light_ac / lessAttenuation;    //lesser attenuation for testing
                            light_al = light_al / lessAttenuation;
                            light_aq = light_aq / lessAttenuation;
                            float spot_exp = 0.5f; //genarate from light_aoc?,1f = uniform, 

                            GL.Light(LightName.Light0 + i, LightParameter.Position, light_position);
                            GL.Light(LightName.Light0 + i, LightParameter.Ambient, light_ambient);
                            GL.Light(LightName.Light0 + i, LightParameter.Diffuse, light_diffuse);
                            GL.Light(LightName.Light0 + i, LightParameter.Specular, light_specular);

                            GL.Light(LightName.Light0 + i, LightParameter.ConstantAttenuation, light_ac); //intenzita odlesku a osvícení
                            GL.Light(LightName.Light0 + i, LightParameter.QuadraticAttenuation, light_aq);
                            GL.Light(LightName.Light0 + i, LightParameter.LinearAttenuation, light_al);

                           GL.Light(LightName.Light0 + i, LightParameter.SpotCutoff, light_cutoff); //180-unidirectional, 0-90 - directional
                           GL.Light(LightName.Light0 + i, LightParameter.SpotDirection, spot_direction); //direction if not infinity
                           GL.Light(LightName.Light0 + i, LightParameter.SpotExponent, spot_exp);

                            GL.Enable(EnableCap.Light0 + i);
                        }
                    }
                }
            }
            else
            {
                // set fixed-function lighting parameters
                GL.ShadeModel(ShadingModel.Smooth);
                GL.LightModel(LightModelParameter.LightModelAmbient, new[] { 0.53f, 0.03f, 0.03f, 1 }); //základní osvìtlení
                GL.LightModel(LightModelParameter.LightModelLocalViewer, 1.0f);
                GL.LightModel(LightModelParameter.LightModelTwoSide, 1.0f);
                GL.Enable(EnableCap.Lighting);

                // light direction
                var dir = new Vector3(1, 0, 1);
                var mat = renderer.LightRotation;
                Vector3.TransformNormal(ref dir, ref mat, out dir);
                GL.Light(LightName.Light0, LightParameter.Position, new float[] { dir.X, dir.Y, dir.Z, 0 });

                // light color
                var col = new Vector3(1, 1, 1);
                col *= (0.25f + 1.5f * GraphicsSettings.Default.OutputBrightness / 100.0f) * 1.5f;
                GL.Light(LightName.Light0, LightParameter.Diffuse, new float[] { col.X, col.Y, col.Z, 1 });
                GL.Light(LightName.Light0, LightParameter.Specular, new float[] { col.X, col.Y, col.Z, 1 });
                GL.Enable(EnableCap.Light0);
            }
        }

        public override void EndScene(Renderer renderer)
        {
            GL.Disable(EnableCap.Lighting);
        }


        private void ApplyFixedFunctionMaterial(Mesh mesh, Material mat, bool textured, bool shaded)
        {
            shaded = shaded && (mesh == null || mesh.HasNormals);
            if (shaded)
            {
                GL.Enable(EnableCap.Lighting);
            }
            else
            {
                GL.Disable(EnableCap.Lighting);
            }

            var hasColors = mesh != null && mesh.HasVertexColors(0);
            if (hasColors)
            {
                GL.Enable(EnableCap.ColorMaterial);
                GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);
            }
            else
            {
                GL.Disable(EnableCap.ColorMaterial);
            }

            // note: keep semantics of hasAlpha consistent with IsAlphaMaterial()
            var hasAlpha = false;
            var hasTexture = false;

            // note: keep this up-to-date with the code in UploadTextures()
            for (int i = 0; i < Renderer.classicGLUsedTextureTypeCount; i++)
            {
                TextureType currTextureType = (TextureType)((int)TextureType.Diffuse + i);
                if (textured && mat.GetMaterialTextureCount(currTextureType) > 0)
                {
                    hasTexture = true;
                    TextureSlot tex;
                    mat.GetMaterialTexture(currTextureType, 0, out tex);
                    var gtex = _scene.TextureSet.GetOriginalOrReplacement(tex.FilePath);
                    hasAlpha = hasAlpha || gtex.HasAlpha == Texture.AlphaState.HasAlpha;

                    if (gtex.State == Texture.TextureState.GlTextureCreated)
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        gtex.BindGlTexture();

                        GL.Enable(EnableCap.Texture2D);
                    }
                }
                else
                {
                    GL.Disable(EnableCap.Texture2D);
                }
            }
            GL.Enable(EnableCap.Normalize);

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
                    GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, Color4.White);
                }
                else
                {
                    GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, color);
                }

                color = new Color4(0, 0, 0, 1.0f);
                if (mat.HasColorSpecular)
                {
                    color = AssimpToOpenTk.FromColor(mat.ColorSpecular);              
                }
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, color);

                color = new Color4(.2f, .2f, .2f, 1.0f);
                if (mat.HasColorAmbient)
                {
                    color = AssimpToOpenTk.FromColor(mat.ColorAmbient);
                }
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, color);

                color = new Color4(0, 0, 0, 1.0f);
                if (mat.HasColorEmissive)
                {
                    color = AssimpToOpenTk.FromColor(mat.ColorEmissive);
                }
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, color);

                float shininess = 1;
                float strength = 1;
                if (mat.HasShininess)
                {
                    shininess = mat.Shininess;

                }
                // todo: I don't even remember how shininess strength was supposed to be handled in assimp
                if (mat.HasShininessStrength)
                {
                    strength = mat.ShininessStrength;
                }

                var exp = shininess;
                if (exp >= 128.0f) // 128 is the maximum exponent as per the Gl spec
                {
                    exp = 128.0f;
                }

                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, exp);
            }
            else if (!hasColors)
            {
                GL.Color3(color.R, color.G, color.B);
            }

            if (hasAlpha)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                GL.DepthMask(false);
            }
            else
            {
                GL.Disable(EnableCap.Blend);
                GL.DepthMask(true);
            }
        }


        private void ApplyFixedFunctionGhostMaterial(Mesh mesh, Material mat, bool shaded)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.DepthMask(false);

            var color = new Color4(.6f, .6f, .9f, 0.15f);           

            shaded = shaded && (mesh == null || mesh.HasNormals);
            if (shaded)
            {
                GL.Enable(EnableCap.Lighting);

                
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, color);

                color = new Color4(1, 1, 1, 0.4f);
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, color);

                color = new Color4(.2f, .2f, .2f, 0.1f);
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, color);

                color = new Color4(0, 0, 0, 0.0f);       
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, color);

                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, 16.0f);
            }
            else
            {
                GL.Disable(EnableCap.Lighting);
                GL.Color3(color.R, color.G, color.B);
            }

            GL.Disable(EnableCap.ColorMaterial);
            GL.Disable(EnableCap.Texture2D);
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 