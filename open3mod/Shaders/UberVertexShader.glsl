// #version 450 - line is added by compiler

///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v0.1)
// [UberVertexShader.glsl]
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
layout(location = 0) in vec4 inposition;
layout(location = 1) in vec4 innormal;
layout(location = 2) in vec4 color;
layout(location = 3) in vec4 textureCoordinate;
layout(location = 4) in vec4 tangent;
layout(location = 5) in vec4 bitangent;

uniform mat4 WorldViewProjection;
uniform mat4 WorldView;
uniform mat4 World;

out vec3 position;
out vec3 normal; 
out vec4 vs_color;

// use custom varyings to pass to the fragment shader to simplify porting to HLSL
out vec2 texCoord; 
 
#ifdef HAS_VERTEX_COLOR
out vec3 vertexColor; 
#endif
 
void main(void) 
{
  //in2pos = vec4(0.5, 0.5, 0.0, 1.0);
  gl_Position = WorldViewProjection * inposition;
  position = vec3(World * inposition); //fragment position
  texCoord = textureCoordinate.xy; 
 
 vs_color = vec4(inposition/2+0.5); //zelena-zluta-modra-fialova

#ifdef HAS_VERTEX_COLOR
  vertexColor = color.rgb;
#endif


  // Scale is always uniform so the 3x3 part of World is the same as the WorldViewTranspose.
  normal = normalize(vec3(World * vec4(innormal.xyz, 0.0))); //OK for diffuse dir light 

//  vs_color = vec4(0.25, 0.25, 0.5, 1.0);
//  vs_color = vec4(inposition/8+0.5); //zelena-zluta-modra-fialova
    vs_color = vec4((normal/2+0.5),1.0); //svetle fialovomodra;
//  vs_color = vec4(color); // cerna
//  vs_color = vec4(textureCoordinate/2+0.5); //zelena-zluta-bila-cervena
//  vs_color = vec4(tangent/2+0.5); //slabe cervena zesvetlava doprava dolu
//  vs_color = vec4((bitangent)/2+0.5); //fialova

}

