// #version 450 - line is added by compiler

out vec4 color;
in vec4 frag_color;

void main(void)
{
	color = vec4(1.0, 0.0, 0.0, 1.0);
	//color = frag_color;
}