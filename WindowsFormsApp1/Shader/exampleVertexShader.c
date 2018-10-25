#version 450

layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in float time;

out vec4 vs_color;

layout(location = 20) uniform  mat4 projection;
layout(location = 21) uniform  mat4 modelView;

void main(void)
{
	gl_Position = projection * modelView * position;
	vs_color = color;
	//vs_color = vec4(sin(time * 5) * 0.5 + 0.5, 0, cos(time * 5) * 0.5 + 0.5, 1.0);
//	gl_Position = vec4(0.25, -0.25, 0.5, 1.0);
}










//}
