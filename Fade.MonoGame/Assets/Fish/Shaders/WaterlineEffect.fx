#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// a thing floating in the sea of the main menu: the boat, and Ava's bottle (see THE BOTTLE in
// fish_headers_ava). it sits in the water, not on it: under a line of the water that stays level however it
// rocks, it is only seen faintly, through the water, in the water's color. right on that line the water is
// lit, where it laps at it. the line laps up and down a little across it.
//
//   texcoord1.x  how far down the screen the water is, in pixels of the world
//   texcoord1.y  the time, in seconds
//   texcoord1.z  how much of the bottle under the water can be seen, from 0 to 1
//   texcoord2.rgb  the color of the water, and .a how much of it the glass takes under the water

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
	float4 Custom : TEXCOORD1;
	float4 Water : TEXCOORD2;
	float2 World : TEXCOORD3;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1,
								float4 water    : TEXCOORD2)
{
	VertexShaderOutput output;
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	output.Custom = custom;
	output.Water = water;
	output.World = position.xy;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;

	// a whole pixel of the world at a time, so the line of the water is as blocky as the bottle
	float2 px = floor(input.World);
	float t = input.Custom.y;
	float waterY = input.Custom.x + sin(px.x * 0.7 + t * 3.1) * 0.6 + sin(px.x * 0.31 - t * 1.7) * 0.5;
	float under = px.y - floor(waterY);

	// under the water: dim, in the water's color, and fading out the deeper it goes
	float3 sunk = lerp(color.rgb, input.Water.rgb, input.Water.a);
	float deep = saturate(1.0 - under / 6.0) * input.Custom.z;
	float isUnder = step(0.5, under);
	color.rgb = lerp(color.rgb, sunk, isUnder);
	color.a *= lerp(1.0, deep, isUnder);

	// and lit where the water laps at it
	float lap = step(abs(under), 0.5) * step(0.05, color.a);
	color.rgb = lerp(color.rgb, float3(1.0, 1.0, 1.0), lap * 0.55);

	return color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
