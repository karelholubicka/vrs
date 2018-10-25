#version 450

layout(location = 2) in float time;

out vec4 vs_color;

void main(void)
{
	vs_color = vec4(sin(time * 5) * 0.5 + 0.5, 0, cos(time * 5) * 0.5 + 0.5, 1.0);
//	gl_Position = vec4(0.25, -0.25, 0.5, 1.0);
}










//}
