#version 120

uniform sampler2D Texture0;

// Set to 0.0, 1.0 for vertical, 1.0, 0.0 for horizontal
uniform vec2 blurMultiplyVec      = vec2(0.0f, 1.0f);
uniform float blurSigma = 0.0f;    // The sigma value for the gaussian function: higher value means more blur
							// A good value for 9x9 is around 3 to 5
							// A good value for 7x7 is around 2.5 to 4
							// A good value for 5x5 is around 2 to 3.5
							// ... play around with this based on what you need

// Confstant for a 9x9 kernel (4.0), use 3.0 for 7x7, 2.0 for 5x5
const float numBlurPixelsPerSide = 4.0f;

// constant for pi
const float pi = 3.1415926535f;

varying vec4 tex; 

void main (void) 
{	
	vec3 incrementalGaussian;
	incrementalGaussian.x = 1.0f / (sqrt(2.0f * pi) * blurSigma);
	incrementalGaussian.y = exp(-0.5f / (blurSigma * blurSigma));
	incrementalGaussian.z = incrementalGaussian.y * incrementalGaussian.y;
			
	vec4 avgValue = vec4(0.0f, 0.0f, 0.0f, 0.0f);
	float coefficientSum = 0.0f;
	
	// Take the central sample first...
	avgValue +=  texture2D( Texture0, tex.st ) * incrementalGaussian.x;
	coefficientSum += incrementalGaussian.x;
	incrementalGaussian.xy *= incrementalGaussian.yz;
	
	// Go through the remaining 8 vertical samples (4 on each side of the center)
	for (float i = 1.0f; i <= numBlurPixelsPerSide; i = i + 1.0f ) 
	{ 
		avgValue += texture2D( Texture0, tex.st - i * blurMultiplyVec ) * incrementalGaussian.x;
		avgValue += texture2D( Texture0, tex.st + i * blurMultiplyVec ) * incrementalGaussian.x;
		
		coefficientSum += 2 * incrementalGaussian.x;
		incrementalGaussian.xy *= incrementalGaussian.yz;
	}
		
	gl_FragColor = avgValue / coefficientSum;
}

