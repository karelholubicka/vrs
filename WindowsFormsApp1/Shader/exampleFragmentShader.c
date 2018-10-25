#version 450
out vec4 color;
in vec4 vs_color;

void main(void)
{
//	color = vec4(1.0, 0.0, 0.0, 1.0);
	color = vs_color;
}

