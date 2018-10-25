// #version 450 - line is added by compiler

layout(location = 0) in vec4 inposition;
layout(location = 1) in vec4 innormal;
layout(location = 2) in vec4 color;
layout(location = 3) in vec4 textureCoordinate;
layout(location = 4) in vec4 tangent;
layout(location = 5) in vec4 bitangent;
layout(location = 6) in float time;

uniform mat4x4 WorldViewProjection;
uniform mat4x4 WorldView;

out vec4 frag_color;

void main(void) 
{
	//gl_Position = position;
	frag_color = vec4(sin(time * 5) * 0.5 + 0.5, 0, cos(time * 5) * 0.5 + 0.5, 1.0);
	mat4x4 test = WorldView * 2;
	gl_Position = vec4(0.25, -0.25, 0.5, 1.0);
	gl_Position = vec4(-1, -1, 0, 1.0);
	gl_Position = vec4(0, 0, 0, 1.0);
}