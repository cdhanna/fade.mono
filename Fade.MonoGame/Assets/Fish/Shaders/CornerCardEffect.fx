#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// A picture stretched onto any four corners on the screen, so that pictures that share corners meet
// with no seam. The game itself does not use it: the Steam art does, to lay the squid's ink onto the
// tilted board of the key art, one cell at a time (see capDraftInkCard in fish_tests_capsule).
//
//   texcoord1.xy  where the top left corner goes, in pixels, and .zw the top right
//   texcoord2.xy  where the bottom left corner goes, and .zw the bottom right
//
// where the sprite itself is does not matter.

float4x4 MatrixTransform;
Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 Quad : TEXCOORD0;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 top      : TEXCOORD1,
								float4 bottom   : TEXCOORD2)
{
	VertexShaderOutput output;
	float2 corner = step(.5, texCoord);
	float2 upper = lerp(top.xy, top.zw, corner.x);
	float2 lower = lerp(bottom.xy, bottom.zw, corner.x);
	position.xy = lerp(upper, lower, corner.y);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.Quad = corner;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	return tex2D(SpriteTextureSampler, input.Quad) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
