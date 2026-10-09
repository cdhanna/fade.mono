#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float4x4 MatrixTransform;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;

	// x is the size of the bubble in pixels, so that the rim can be measured in pixels.
	// y is 1 to draw a solid circle instead, which is what goes into the bubble mask.
	// z is 1 to draw a star with four points instead.
	// w is 1 to draw a heart instead, and 2 to draw an eye instead.
	float4 Custom : TEXCOORD1;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1)
{
	VertexShaderOutput output;
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	output.Custom = custom;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// 0 in the middle of the bubble, 1 at the edge
	float2 p = (input.TextureCoordinates - .5) * 2;
	float d = length(p);

	// how big a pixel is, measured the same way
	float pixel = 2.0 / max(input.Custom.x, 1.0);

	float inside = step(d, 1.0);
	float rim = step(1.0 - pixel * 1.5, d);
	float shine = step(length(p - float2(-.4, -.4)), max(.18, pixel));

	// a bright rim, a bright glint, and a faint fill
	float alpha = max(max(rim * .9, shine * .75), .12);
	alpha = lerp(alpha, 1.0, input.Custom.y) * inside;

	// when z is 1 this is not a bubble at all. it is a little star with four points, for the
	// sparkles that are used in place of bubbles above the water.
	float2 a = abs(p);
	float star = step(sqrt(a.x) + sqrt(a.y), 1.0);
	alpha = lerp(alpha, star, input.Custom.z);

	// and when w is 1 it is a heart, for the fish that are happy to be sitting on coral. this is the
	// usual curve for a heart, turned the right way up, because y goes down the screen here.
	float2 h = float2(p.x * 1.22, 0.12 - p.y * 1.22);
	float around = h.x * h.x + h.y * h.y - 1.0;
	float heart = step(around * around * around - h.x * h.x * h.y * h.y * h.y, 0.0);
	alpha = lerp(alpha, heart, step(0.5, input.Custom.w) * step(input.Custom.w, 1.5));

	// and when w is 2 it is an eye, for the wave that brings the main menu back after the ancient one
	// has been summoned. it is drawn the way that the pictures of the fish are: out of a few big square
	// pixels, in flat colors, with nothing that fades into anything else. a big eye has more pixels
	// than a small one, but not as many more as it is bigger, so the pixels of a big eye are big.
	// it is the white of an eye, with a few red veins at the edge of it, a yellow iris with a green
	// ring around it, and a slit for a pupil. where it looks comes from how big it is.
	float isEye = step(1.5, input.Custom.w);
	float blocks = floor(clamp(input.Custom.x / 4.5, 7.0, 19.0));
	float2 e = ((floor(input.TextureCoordinates * blocks) + 0.5) / blocks - 0.5) * 2.0;
	float block = 2.0 / blocks;
	float eyeDist = length(e);
	float2 looks = floor(float2(sin(input.Custom.x * 1.7), cos(input.Custom.x * 2.3)) * 0.24 / block + 0.5) * block;
	float2 fromIris = e - looks;
	float veinRoll = frac(sin(dot(floor(input.TextureCoordinates * blocks), float2(12.9898, 78.233)) + input.Custom.x) * 43758.5453);
	float3 eye = float3(0.930, 0.890, 0.790);
	eye = lerp(eye, float3(0.780, 0.250, 0.220), step(0.62, eyeDist) * step(0.70, veinRoll));
	eye = lerp(eye, float3(0.370, 0.470, 0.090), step(length(fromIris), 0.56));
	eye = lerp(eye, float3(0.820, 0.760, 0.160), step(length(fromIris), 0.56 - block));
	eye = lerp(eye, float3(0.030, 0.015, 0.040), step(abs(fromIris.x), block * 0.75) * step(abs(fromIris.y), 0.38));
	eye = lerp(eye, float3(1.000, 1.000, 1.000), step(abs(fromIris.x + block * 1.5), block * 0.6) * step(abs(fromIris.y + block * 1.5), block * 0.6));
	eye = lerp(eye, float3(0.110, 0.030, 0.060), step(1.0 - block * 1.1, eyeDist));
	float eyeHere = step(eyeDist, 1.0);
	float3 color = lerp(input.Color.rgb, eye, isEye);
	alpha = lerp(alpha, eyeHere, isEye);
	float see = lerp(input.Color.a, 1.0, isEye);

	return float4(color, see * alpha);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
