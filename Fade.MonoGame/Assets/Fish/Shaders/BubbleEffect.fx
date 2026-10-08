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

	return float4(input.Color.rgb, input.Color.a * alpha);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
