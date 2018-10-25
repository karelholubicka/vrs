// #version 450 - line is added by compiler
//https://github.com/Blackhart/GLSL-Shaders/tree/master/glsl/Chromakey
//https://www.shadertoy.com/view/4dX3WN
// dodělat spill..?? viz
//https://github.com/keijiro/ProcAmp

out vec4 fragColor;
in vec4 vs_color;
//in vec4 vs_position;

uniform sampler2D iChannel0;          // input channel Mask
uniform sampler2D iChannel1;          // input channel Camera
uniform sampler2D iChannel2;          // input channel Foreground
uniform vec2      iTextureResolution;        // image /texture resolution (in pixels)
uniform vec2      iViewportSize;        // image /texture resolution (in pixels)


vec3 rgb2hsv(vec3 rgb)
{
	float Cmax = max(rgb.r, max(rgb.g, rgb.b));
	float Cmin = min(rgb.r, min(rgb.g, rgb.b));
	float delta = Cmax - Cmin;

	vec3 hsv = vec3(0., 0., Cmax);

	if (Cmax > Cmin)
	{
		hsv.y = delta / Cmax;

		if (rgb.r == Cmax)
			hsv.x = (rgb.g - rgb.b) / delta;
		else
		{
			if (rgb.g == Cmax)
				hsv.x = 2. + (rgb.b - rgb.r) / delta;
			else
				hsv.x = 4. + (rgb.r - rgb.g) / delta;
		}
		hsv.x = fract(hsv.x / 6.);
	}
	return hsv;
}

float chromaKey(vec3 color)
{
	vec3 backgroundColor = vec3(0.157, 0.576, 0.129);
	vec3 weights = vec3(4.0, 1.0, 2.0);

	vec3 hsv = rgb2hsv(color);
	vec3 target = rgb2hsv(backgroundColor);
	float dist = length(weights * (target - hsv));
	return 1. - clamp(3. * dist - 1.5, 0., 1.0);
}

vec3 changeSaturation(vec3 color, float saturation)
{
	float luma = dot(vec3(0.213, 0.715, 0.072) * color, vec3(1.0));
	return mix(vec3(luma), color, saturation);
}

void main(void)
{
	vec2 scale = iViewportSize / iTextureResolution;
	vec2 uv = gl_FragCoord.xy / iTextureResolution.xy;
	//uv - at the border of image/texture value is 1 - but is positioned where rectangle fits in image;
//	ivec2 texPos = ivec2((vs_position.x + 0.5) * iTextureResolution.x, (-vs_position.y + 0.5) * iTextureResolution.y);
 //   ivec2 texPos = ivec2((gl_FragCoord.x - 0.5)/scale.x, iTextureResolution.y-(gl_FragCoord.y - 0.5)/scale.y); //bitmap input
	ivec2 texPos = ivec2((gl_FragCoord.x - 0.5)/scale.x, (gl_FragCoord.y - 0.5)/scale.y); //texture input
	//	vec3 color = texture(iChannel0, uv).rgb;
	vec3 color = texelFetch(iChannel1, texPos, 0).rgb;
	//vec3 bg = texelFetch(iChannel0, texPos, 0).rgb;
	vec4 mask = texelFetch(iChannel0, texPos, 0);
	vec4 foreground = texelFetch(iChannel2, texPos, 0);

	float incrustation = chromaKey(color);
	//color = changeSaturation(color, 0.5);
	//color = mix(color, bg, incrustation);

	//fragColor = vec4(gl_FragCoord.x/1000, gl_FragCoord.y/1000, 0, 1);
    //fragColor = vs_color;
	//fragColor = texture(iChannel0, -uv);
	//fragColor = texelFetch(iChannel0, texPos, 0); 
    //	if (texPos.x > 960)  	fragColor = texelFetch(iChannel1, texPos, 0); 
	//fragColor = vec4(color, 1.0);

	fragColor = vec4(color, (1-incrustation)*mask.a); //colorKey
    //	if (texPos.x > 960)  	fragColor = vs_color;

	fragColor = mix(fragColor, foreground/foreground.a, foreground.a);

	fragColor = texelFetch(iChannel1, texPos, 0); 
}

