#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE TEMPLE COMES THROUGH. every piece of the temple, the steps, the columns and the roof, comes out of
// the sky that the way in opens, a few pixels at a time, in no order, with starlight where it is still
// coming. see THE SUMMONING in fish_headers_neptune.
// the game says how far along a piece is in the red of its color, from 0 to 255, and how big its picture
// is, in pairs of pixels, in the green and the blue, because a parameter is the same for every sprite
// that has the effect, and these are different for each of them.

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// x is the time, in seconds, and y how much it has faded into the water, from 0 to 1
float4 Temple;

// it has been under the sea a long time, and it looks it: the color of the water is in it, it has less
// of a difference between its light and its dark than it would in the air, and the light that comes
// down through the waves runs over it
#define WATER float3(0.30, 0.52, 0.64)
#define FADE 0.48
#define CAUSTIC 0.10

// how wide the band of starlight is, at the edge of what has come through
#define BAND 0.12
#define STARLIGHT float3(1.00, 0.92, 0.66)
#define LIGHT_BLUE float3(0.62, 0.86, 1.00)

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
	float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	VertexShaderOutput output;
	position.xy = floor(position.xy + .5);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	return output;
}

float hash(float2 p)
{
	return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 texel = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	float progress = input.Color.r;
	float2 pairs = max(input.Color.gb * 255.0, 1.0);

	// every pair of pixels of the picture comes through at a moment of its own, a little more of it
	// from the bottom up, and starlight sits on it for a moment as it does
	float2 block = floor(input.TextureCoordinates * pairs);
	float when = hash(block) * .75 + (1.0 - input.TextureCoordinates.y) * .25;
	float reach = progress * (1.0 + BAND);
	float shown = step(when, reach);
	float edge = shown * step(reach - BAND, when);

	float twinkle = .5 + .5 * sin(Temple.x * 12.0 + hash(block + 3.1) * 30.0);
	float3 glow = lerp(LIGHT_BLUE, STARLIGHT, twinkle);
	float lum = dot(texel.rgb, float3(.3, .55, .15));
	float3 sunk = lerp(texel.rgb, WATER * (.35 + .85 * lum), FADE * saturate(Temple.y));
	float2 uv = input.TextureCoordinates * pairs;
	float caustic = sin(uv.x * .9 + uv.y * .35 + Temple.x * 1.7) * sin(uv.x * .4 - uv.y * .8 - Temple.x * 1.1);
	sunk += WATER * saturate(caustic) * CAUSTIC * saturate(Temple.y);
	float3 color = lerp(sunk, glow, edge * .85);
	float alpha = texel.a * shown * input.Color.a;
	return float4(color, alpha);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
