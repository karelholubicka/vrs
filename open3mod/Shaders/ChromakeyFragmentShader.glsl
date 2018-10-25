// #version 450 - line is added by compiler
//https://github.com/Blackhart/GLSL-Shaders/tree/master/glsl/Chromakey
//https://www.shadertoy.com/view/4dX3WN
// dodělat spill..?? viz
//https://github.com/keijiro/ProcAmp

out vec4 fragColor;
in vec4 vs_color;
//in vec4 vs_position;

uniform sampler2D iMask;          // input channel Mask
uniform sampler2D iYUYVtex;            // input channel Camera YUYV macropixel texture passed as RGBA format
uniform sampler2D iYUYVtex2;            // input channel second Camera YUYV macropixel texture passed as RGBA format
uniform sampler2D iForeground;          // input channel Foreground
uniform vec2      iTextureResolution;    // RGBA texture resolution (in pixels)
uniform vec2      iViewportSize;         // image resolution (output width in pixels)
uniform vec2      iViewportStart;         // left bottom in pixels)
uniform vec3      iBackgroundColorHSV;      //keying color
uniform vec3      iWeightsKeying;          //keying distance
uniform float     iTresholdKeying;         // treshold for keying
uniform float     iPowerCanceling;         // treshold for keying
uniform bool      iWellDone;     //canceling process
uniform int       iMode;     //canceling process 0:normal composition 1: camera+key (no fgd+mask) 2:camera+cancel color (no fgd+mask) 3: camera natural

vec4 rec709YCbCr2rgba(float Y, float Cb, float Cr, float a) 
{ 
	float r, g, b; 
// Y: Undo 1/256 texture value scaling and scale [16..235] to [0..1] range
// C: Undo 1/256 texture value scaling and scale [16..240] to [-0.5 .. + 0.5] range
	Y = (Y * 256.0 - 16.0) / 219.0; 
	Cb = (Cb * 256.0 - 16.0) / 224.0 - 0.5; 
	Cr = (Cr * 256.0 - 16.0) / 224.0 - 0.5; 
// Convert to RGB using Rec.709 conversion matrix (see eq 26.7 in Poynton 2003)
	r = Y + 1.5748 * Cr; 
	g = Y - 0.1873 * Cb - 0.4681 * Cr; 
	b = Y + 1.8556 * Cb; 
	return vec4(r, g, b, a); 
}

vec4 rec709NormYCbCr2rgba(float Y, float Cb, float Cr, float a) 
{ 
	float r, g, b; 
	r = Y + 1.5748 * Cr; 
	g = Y - 0.1873 * Cb - 0.4681 * Cr; 
	b = Y + 1.8556 * Cb; 
	return vec4(r, g, b, a); 
}

vec4 rec709NormRgba2YCbCr(float r, float g, float b, float a) 
{ 
	float Y, Cb, Cr; 
    Y = 0.2215 * r + 0.7154 * g + 0.0721 * b;
    Cb = -0.1145 * r - 0.3855 * g + 0.5000 * b;
    Cr = 0.5016 * r - 0.4556 * g - 0.0459 * b;
	return vec4(Y, Cb, Cr, a); 
}

vec4 spillWellDoneRec709YCbCr(float Y, float Cb, float Cr, float a) 
{ 
	if (Cb+Cr<0.)
	{
		if ((Cr<0.) &&(Cb<0.))
		{
			Cr = 0.;
			Cb=0.;
		}
	    if (Cr>0.) Cb = - Cr;
	    if (Cb>0.) Cr = - Cb;
	}
	return vec4(Y, Cb, Cr, a); 
}

vec4 spillRareRec709YCbCr(float Y, float Cb, float Cr, float a) 
{ 
	if (Cb<0.)
	{
    	if (Cr<0.)
		{
	    	if (Cr>Cb)
			{
				Cb = Cb*(Cb-Cr)/Cb;
  				Cr = 0.;
			}
				else
			{
				Cr = Cr*(Cr-Cb)/Cr;
  				Cb = 0.;
			}
		}
	}
	return vec4(Y, Cb, Cr, a); 
}

vec4 bilinear(vec4 W, vec4 X, vec4 Y, vec4 Z, vec2 weight) 
{
	vec4 m0 = mix(W, Z, weight.x);
	vec4 m1 = mix(X, Y, weight.x);
	return mix(m0, m1, weight.y); 
}

		// Gather neighboring YUV macropixels from the given texture coordinate
void textureGatherYUV(sampler2D YUYVsampler, vec2 tc, out vec4 W, out vec4 X, out vec4 Y, out vec4 Z) 
{
    ivec2 tx = ivec2(tc);
	ivec2 iYUYVTextureResolution = textureSize(YUYVsampler, 0);
	ivec2 tmin = ivec2(0,0);
	ivec2 tmax = iYUYVTextureResolution - ivec2(1,1);
	W = texelFetch(YUYVsampler, clamp(tx + ivec2(0,0), tmin, tmax), 0); 
	X = texelFetch(YUYVsampler, clamp(tx + ivec2(0,1), tmin, tmax), 0);
	Y = texelFetch(YUYVsampler, clamp(tx + ivec2(1,1), tmin, tmax), 0);
	Z = texelFetch(YUYVsampler, clamp(tx + ivec2(1,0), tmin, tmax), 0); 
}

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
	vec3 hsv = rgb2hsv(color);
	vec3 target = iBackgroundColorHSV;
	float dist = length(iWeightsKeying * (target - hsv));
	return 1. - clamp(3. * dist - iTresholdKeying, 0., 1.0);
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
	//scale, straight, values 0 - iTextureResolution
	ivec2 texPos  = ivec2((gl_FragCoord.x - 0.5-iViewportStart.x)/scale.x, (gl_FragCoord.y - 0.5-iViewportStart.y)/scale.y); //texture input, halfpixels
	//noscale, upside down
    ivec2 texPos2 = ivec2((gl_FragCoord.x - 0.5),  iTextureResolution.y-1-(gl_FragCoord.y - 0.5));

    vec2 tc = vec2((gl_FragCoord.x - 0.5)-iViewportStart.x,              iViewportStart.y+iViewportSize.y-(gl_FragCoord.y - 0.5)); 
    tc.x=tc.x/2;
	tc = tc/scale;
	float alpha = 1; 

//	if (tx.y<iViewportSize.y) alpha = 0.0;

	vec4 macro, macro_u, macro_r, macro_ur;
	vec4 pixel, pixel_r, pixel_u, pixel_ur; 

	textureGatherYUV(iYUYVtex, tc, macro, macro_u, macro_ur, macro_r);
	vec2 off = fract(tc); 
	//off = vec2(0,0); 

	if (off.x > 0.5) { 			// right half of macropixel
		pixel    = rec709YCbCr2rgba(macro.a, (macro.b + macro_r.b)/2, (macro.r+macro_r.r)/2, alpha) ; 
		pixel_r  = rec709YCbCr2rgba(macro_r.g, macro_r.b, macro_r.r, alpha); 
		pixel_u  = rec709YCbCr2rgba(macro_u.a, (macro_u.b + macro_ur.b)/2, (macro_u.r+macro_ur.r)/2, alpha) ; 
		pixel_ur = rec709YCbCr2rgba(macro_ur.g, macro_ur.b, macro_ur.r, alpha); 
	} else { 					// left half & center of macropixel
		pixel = rec709YCbCr2rgba(macro.g, macro.b, macro.r, alpha); 
		pixel_r = rec709YCbCr2rgba(macro.a, (macro.b+macro_r.b)/2, (macro.r+macro_r.r)/2, alpha); 
		pixel_u = rec709YCbCr2rgba(macro_u.g, macro_u.b, macro_u.r, alpha); 
		pixel_ur = rec709YCbCr2rgba(macro_u.a, (macro_u.b+macro_ur.b)/2, (macro_u.r+macro_ur.r)/2, alpha); 
	}
	vec4 camera = bilinear(pixel, pixel_u, pixel_ur, pixel_r, off);
	//since weighting somehow does not work properly (rounding errors?pixel centers?)
	if ((iTextureResolution.x == iViewportSize.x)&&(iTextureResolution.y == iViewportSize.y)) camera = pixel;

//	if (texPos.y < 150)  camera =  macro;
//	if (texPos.y < 100)  camera =  pixel;
//	if (texPos.y < 50)  camera =  pixel_r;

	textureGatherYUV(iYUYVtex2, tc, macro, macro_u, macro_ur, macro_r);
	if (off.x > 0.5) { 			// right half of macropixel
		pixel    = rec709YCbCr2rgba(macro.a, (macro.b + macro_r.b)/2, (macro.r+macro_r.r)/2, alpha) ; 
		pixel_r  = rec709YCbCr2rgba(macro_r.g, macro_r.b, macro_r.r, alpha); 
		pixel_u  = rec709YCbCr2rgba(macro_u.a, (macro_u.b + macro_ur.b)/2, (macro_u.r+macro_ur.r)/2, alpha) ; 
		pixel_ur = rec709YCbCr2rgba(macro_ur.g, macro_ur.b, macro_ur.r, alpha); 
	} else { 					// left half & center of macropixel
		pixel = rec709YCbCr2rgba(macro.g, macro.b, macro.r, alpha); 
		pixel_r = rec709YCbCr2rgba(macro.a, (macro.b+macro_r.b)/2, (macro.r+macro_r.r)/2, alpha); 
		pixel_u = rec709YCbCr2rgba(macro_u.g, macro_u.b, macro_u.r, alpha); 
		pixel_ur = rec709YCbCr2rgba(macro_u.a, (macro_u.b+macro_ur.b)/2, (macro_u.r+macro_ur.r)/2, alpha); 
	}
	vec4 camera2 = bilinear(pixel, pixel_u, pixel_ur, pixel_r, off);
	vec3 color = camera.rgb;
	vec4 mask = texelFetch(iMask, texPos, 0);
	vec4 foreground = texelFetch(iForeground, texPos, 0);
    //	mask = vec4(0.0,0.0,0.0,1.0);

	fragColor = vec4(0.0,0.0,0.0,0.0);
	//color = changeSaturation(color, 0.5);
    fragColor = vec4(color,alpha);
	if (iMode == 3) return;

	if (iMode < 2)
	{
      float incrustation = chromaKey(color);
      fragColor = vec4(color, (1-incrustation)); //colorKey
	}

	if ((mask.a > 0)||(iMode > 0))
	{
	  vec4 cancelledColor = rec709NormRgba2YCbCr(fragColor.r,fragColor.g,fragColor.b,fragColor.a);
	  if (iWellDone)
  	  cancelledColor = spillWellDoneRec709YCbCr(cancelledColor.r,cancelledColor.g,cancelledColor.b,cancelledColor.a);
	  else
  	  cancelledColor = spillRareRec709YCbCr(cancelledColor.r,cancelledColor.g,cancelledColor.b,cancelledColor.a);

	  cancelledColor = rec709NormYCbCr2rgba(cancelledColor.r,cancelledColor.g,cancelledColor.b,cancelledColor.a);
      
	  fragColor = mix(fragColor, cancelledColor, iPowerCanceling);//cancel color
	}


    if (iMode == 0) 
	{
        fragColor = vec4(fragColor.rgb * fragColor.a*mask.a, fragColor.a*mask.a); // masking + alpha multiply
     	vec4 foregroundOpaque = vec4(foreground.rgb, 1.0);
	    fragColor = mix(fragColor, foregroundOpaque, foreground.a);//foreground add
        if (fragColor.a != 0.0f) fragColor = vec4(fragColor.rgb / fragColor.a, sqrt(fragColor.a));//divide alpha
	}

    if (iMode == 1) 
	{
        vec4 black = vec4(0.0,0.0,0.0,1.0);
	    fragColor = mix(black,fragColor, fragColor.a);//key over black
		fragColor = vec4(fragColor.rgb,1.0);
	}


   // fragColor = texelFetch(iForeground, texPos2, 0); test foreground bitmap over
	//fragColor = vec4(gl_FragCoord.x/1000, gl_FragCoord.y/1000, 0, 1);
    //fragColor = vs_color;
    //fragColor = texelFetch(iYUYVtex, texPos, 0); //direct YUYV camera source
	//fragColor = vec4(color, 1.0);
//    if (texPos.x < 800)  fragColor = camera2;
//    if (texPos.x > 900)  fragColor = camera;


	}

