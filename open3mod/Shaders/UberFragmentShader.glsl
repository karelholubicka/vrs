// #version 450 - line is generated in ShaderGen

///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v0.1)
// [UberFragmentShader.glsl]
// (c) 2012-2013, Open3Mod Contributors
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

struct materialTexs {
    sampler2D diffuse;
    sampler2D specular;
    sampler2D ambient;
    sampler2D emissive;
    sampler2D height;
    sampler2D normal;
}; 

struct lightStruct {
    int lightType;
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
  
    float constant;
    float linear;
    float quadratic;
  
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;       
};

// #define NR_LIGHTS 1 //line is generated in ShaderGen

//variables for whole material
uniform vec4 MaterialDiffuse_Alpha;
uniform vec4 MaterialSpecular;
uniform vec4 MaterialAmbient;
uniform vec4 MaterialEmissive;
uniform float MaterialShininess;

uniform float SceneBrightness;
uniform lightStruct Lights[NR_LIGHTS]; // modelview space, norm.
uniform materialTexs material;
uniform vec3 CameraPosition;
uniform mat4 View;

in vec3 position;
in vec3 normal; 

out vec4 fragColor;
in vec4 vs_color;
in vec2 texCoord; 

#ifdef HAS_VERTEX_COLOR
in vec3 vertexColor; 
#endif

// Light function prototypes
vec3 CalcDirLight(lightStruct light, vec3 normal, vec3 viewDir, vec2 TexCoords);
vec3 CalcPointLight(lightStruct light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 TexCoords);
vec3 CalcSpotLight(lightStruct light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 TexCoords);

//Global variables - we need them in all light functions
  vec4 materialDiffuseAndAlpha = MaterialAmbient*MaterialSpecular*MaterialEmissive*MaterialShininess*Lights[0].lightType; //to keep variables active
  vec4 materialSpecular;
  vec4 materialAmbient;
  vec4 materialEmissive;

void main(void) 
{
  vec2 texCoordFlipped = texCoord;
  texCoordFlipped.y = 1.-texCoord.y;

  float alpha = MaterialDiffuse_Alpha.a * texture2D(material.diffuse, texCoordFlipped.xy).a; // alpha channel
  materialDiffuseAndAlpha = MaterialDiffuse_Alpha;
  materialSpecular = MaterialSpecular;
  materialAmbient = MaterialAmbient;
  materialEmissive = MaterialEmissive;

  vec3 norm = normalize(normal);
  vec3 dir = normalize(Lights[0].direction);
  #ifdef HAS_TWOSIDE
    if (!gl_FrontFacing) norm = -norm;
  #endif
  #ifdef HAS_SKINNING
  #endif
  
#ifdef HAS_VERTEX_COLOR
  materialDiffuseAndAlpha.rgb *= vertexColor;
  materialSpecular.rgb *= vertexColor;
  materialAmbient.rgb *= vertexColor;
  materialEmissive.rgb *= vertexColor;
#endif

#ifndef HAS_LIGHTING
  materialDiffuseAndAlpha *= texture2D(material.diffuse, texCoordFlipped.xy); //texture or owntexture apply
  fragColor = materialDiffuseAndAlpha;
#endif

#ifdef HAS_LIGHTING
	vec4 emission = texture2D(material.diffuse, texCoordFlipped.xy)*materialEmissive + texture2D(material.emissive, texCoordFlipped.xy); 
    vec3 viewDir = normalize(CameraPosition - position);
	vec3 result = vec3(0.0, 0.0, 0.0);
    for(int i = 0; i < NR_LIGHTS; i++)
	{
	lightStruct currLight = Lights[i];
    if  (currLight.lightType == 1)  result += CalcDirLight(currLight, norm, viewDir, texCoordFlipped);
    if  (currLight.lightType == 2)  result += CalcPointLight(currLight, norm, position, viewDir, texCoordFlipped);    
    if  (currLight.lightType == 3)  result += CalcSpotLight(currLight, norm, position, viewDir, texCoordFlipped);    
	}
    fragColor = vec4(result * SceneBrightness + emission.rgb, alpha) ;
#endif

#ifndef HAS_PHONG_SPECULAR_SHADING //= control test with one directional light
//  vec3 specular = vec3(0.0, 0.0, 0.0);
//  materialDiffuseAndAlpha = MaterialDiffuse_Alpha.rgba;
//  materialDiffuseAndAlpha *= texture2D(material.diffuse, texCoordsFlipped.xy); //texture or owntexture apply
//  float diff = 1.0;
//  diff = max(dot(norm, dir),0.0);
//  materialDiffuseAndAlpha.rgb = diff * materialDiffuseAndAlpha.rgb * Lights[0].diffuse + MaterialAmbient.rgb * Lights[0].ambient;
//  vec3 eyeDir = normalize(CameraPosition-position);
//  vec3 reflectDir = normalize(reflect(-dir, norm));
//  specular = Lights[0].specular * pow(max(dot(reflectDir, eyeDir), 0.0), MaterialShininess);	
//  vec3 totalColor = (materialDiffuseAndAlpha.rgb + specular) * SceneBrightness + MaterialEmissive.rgb*materialDiffuseAndAlpha.rgb;//when scene brightness drives lights only
//  fragColor = vec4(totalColor.rgb, materialDiffuseAndAlpha.a);
#endif

//  fragColor = vs_color;
//  fragColor = vec4(1.0, 0.0, 0.0, 1.0);
//  fragColor = texelFetch(Texture0, texPos, 0); 
//  fragColor = vec4(viewDir, 1.0);
//  fragColor = vec4((position/8+0.5),1.0);
//  fragColor  = vec4((normal/2+0.5),1.0); 

}




// Calculates the color when using a directional light.
vec3 CalcDirLight(lightStruct light, vec3 normal, vec3 viewDir, vec2 TexCoords)
{
    vec3 lightDir = normalize(light.direction);
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular shading
    vec3 reflectDir = normalize(reflect(-lightDir, normal));
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), MaterialShininess);
    // Combine results
    vec3 ambient = light.ambient *          vec3(texture(material.ambient, TexCoords))*materialAmbient.rgb;
    vec3 diffuse = light.diffuse * diff *   vec3(texture(material.diffuse, TexCoords))*materialDiffuseAndAlpha.rgb;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords))*materialSpecular.rgb;
    return (ambient + diffuse + specular);
}

// Calculates the color when using a point light.
vec3 CalcPointLight(lightStruct light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 TexCoords)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), MaterialShininess);
    // Attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0f / (light.constant + light.linear * distance + light.quadratic * (distance * distance));    
    // Combine results
    vec3 ambient = light.ambient *          vec3(texture(material.ambient, TexCoords))*materialAmbient.rgb;
    vec3 diffuse = light.diffuse * diff *   vec3(texture(material.diffuse, TexCoords))*materialDiffuseAndAlpha.rgb;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords))*materialSpecular.rgb;
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

// Calculates the color when using a spot light.
vec3 CalcSpotLight(lightStruct light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 TexCoords)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), MaterialShininess);
    // Attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0f / (light.constant + light.linear * distance + light.quadratic * (distance * distance));    
    // Spotlight intensity
    float theta = dot(lightDir, normalize(light.direction)); //values 0.0f (at 180degrees spot size, outerCutOff=PI) - 1.0f (at 0 degrees,outerCutoff=0 )
	float epsilon = cos(light.cutOff/2) - cos(light.outerCutOff/2); 
	if (epsilon == 0.0f) epsilon = 0.001f;
    float intensity = clamp((theta - cos(light.outerCutOff/2)) / epsilon, 0.0, 1.0);
    // Combine results
    vec3 ambient = light.ambient *          vec3(texture(material.ambient, TexCoords))*materialAmbient.rgb;
    vec3 diffuse = light.diffuse * diff *   vec3(texture(material.diffuse, TexCoords))*materialDiffuseAndAlpha.rgb;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords))*materialSpecular.rgb;
    ambient *= attenuation * intensity;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular);
}


