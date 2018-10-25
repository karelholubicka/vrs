#version 450

layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in float time;

out vec4 vs_color;
//out vec4 vs_position;

layout(location = 20) uniform  mat4 projection;
layout(location = 21) uniform  mat4 modelView;

void main(void)
{
	vs_color = color;
	gl_Position = projection * modelView * position;
	//vs_position = projection * modelView * position;

//	gl_Position = vec4(time, -0.25, 0.5, 1.0);
//	vs_position = vec4(time/10, -0.25, 1, 1.0);

	//vs_color = vec4(sin(time * 5) * 0.5 + 0.5, 0, cos(time * 5) * 0.5 + 0.5, 1.0);
}







//}
