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

struct Material {
    sampler2D diffuse;
    sampler2D ambient;
    sampler2D specular;
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

// note: all lighting calculations done in modelview space

//variables for whole material
uniform vec4 MaterialDiffuse_Alpha;
uniform vec4 MaterialSpecular;
uniform vec4 MaterialAmbient;
uniform vec4 MaterialEmissive;
uniform float MaterialShininess;

uniform float SceneBrightness;
uniform lightStruct Lights[NR_LIGHTS]; // modelview space, norm.
uniform Material material;
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

// Function prototypes
vec3 CalcDirLight(lightStruct light, vec3 normal, vec3 viewDir, vec2 TexCoords);
vec3 CalcPointLight(lightStruct light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 TexCoords);
vec3 CalcSpotLight(lightStruct light, vec3 normal, vec3 fragPos, vec3 viewDir, vec2 TexCoords);


void main(void) 
{
  vec4 diffuseAndAlpha = MaterialAmbient*MaterialSpecular*MaterialEmissive*MaterialShininess*Lights[0].lightType; //to keep variables active
  vec2 texCoordsFlipped = texCoord;
  texCoordsFlipped.y = 1.-texCoord.y;

vec3 norm = normalize(normal);
vec3 dir = normalize(Lights[0].direction);
#ifdef HAS_TWOSIDE
  if (!gl_FrontFacing) norm = -norm;
#endif
#ifdef HAS_SKINNING
#endif

  diffuseAndAlpha = MaterialDiffuse_Alpha.rgba;
#ifdef HAS_VERTEX_COLOR
  diffuseAndAlpha.rgb *= vertexColor;
#endif
#ifndef HAS_LIGHTING
  diffuseAndAlpha *= texture2D(material.diffuse, texCoordsFlipped.xy); //texture or owntexture apply
  fragColor = diffuseAndAlpha;
#endif
#ifdef HAS_LIGHTING
    diffuseAndAlpha.a *= texture2D(material.diffuse, texCoordsFlipped.xy).a; 
    vec3 viewDir = normalize(CameraPosition - position);
	vec3 result = vec3(0.0, 0.0, 0.0);
    for(int i = 0; i < NR_LIGHTS; i++)
	{
	lightStruct currLight = Lights[i];
    if  (currLight.lightType == 1)  result += CalcDirLight(currLight, norm, viewDir, texCoordsFlipped);
    if  (currLight.lightType == 2)  result += CalcPointLight(currLight, norm, position, viewDir, texCoordsFlipped);    
    if  (currLight.lightType == 3)  result += CalcSpotLight(currLight, norm, position, viewDir, texCoordsFlipped);    
	}
    fragColor = vec4(result * SceneBrightness, diffuseAndAlpha.a) ;
#endif

#ifndef HAS_PHONG_SPECULAR_SHADING //= control test with one directional light
//  vec3 specular = vec3(0.0, 0.0, 0.0);
//  diffuseAndAlpha = MaterialDiffuse_Alpha.rgba;
//  diffuseAndAlpha *= texture2D(material.diffuse, texCoordsFlipped.xy); //texture or owntexture apply
//  float diff = 1.0;
//  diff = max(dot(norm, dir),0.0);
//  diffuseAndAlpha.rgb = diff * diffuseAndAlpha.rgb * Lights[0].diffuse + MaterialAmbient.rgb * Lights[0].ambient;
//  vec3 eyeDir = normalize(CameraPosition-position);
//  vec3 reflectDir = normalize(reflect(-dir, norm));
//  specular = Lights[0].specular * pow(max(dot(reflectDir, eyeDir), 0.0), MaterialShininess);	
//  vec3 totalColor = (diffuseAndAlpha.rgb + specular + MaterialEmissive.rgb) * SceneBrightness;
//  fragColor = vec4(totalColor.rgb, diffuseAndAlpha.a);
#endif

  //  fragColor = vs_color;
//  fragColor = vec4(1.0, 0.0, 0.0, 1.0);
//  fragColor = texelFetch(Texture0, texPos, 0); 
 // fragColor = vec4(normal, 1.0);
}





// Calculates the color when using a directional light.
vec3 CalcDirLight(lightStruct light, vec3 normal, vec3 viewDir, vec2 TexCoords)
{
    vec3 lightDir = normalize(light.direction);
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // Specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), MaterialShininess);
    // Combine results
    vec3 ambient = light.ambient *          vec3(texture(material.ambient, TexCoords))*MaterialAmbient.rgb;
    vec3 diffuse = light.diffuse * diff *   vec3(texture(material.diffuse, TexCoords))*MaterialDiffuse_Alpha.rgb;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords))*MaterialSpecular.rgb;
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
    vec3 ambient = light.ambient *          vec3(texture(material.ambient, TexCoords))*MaterialAmbient.rgb;
    vec3 diffuse = light.diffuse * diff *   vec3(texture(material.diffuse, TexCoords))*MaterialDiffuse_Alpha.rgb;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords))*MaterialSpecular.rgb;
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
	vec3 shift = vec3(0.0, 0.2, 0.0);
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
    float theta = dot(lightDir, normalize(-light.direction)); 
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
    // Combine results
    vec3 ambient = light.ambient *          vec3(texture(material.ambient, TexCoords))*MaterialAmbient.rgb;
    vec3 diffuse = light.diffuse * diff *   vec3(texture(material.diffuse, TexCoords))*MaterialDiffuse_Alpha.rgb;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords))*MaterialSpecular.rgb;
    ambient *= attenuation * intensity;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular);
}


