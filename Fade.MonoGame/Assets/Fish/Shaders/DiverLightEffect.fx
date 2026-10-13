#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the light of the diver's flashlight, at night: a pool of pale light on the water around the diver,
// with a brighter beam the way that the diver faces. the sprite is a plain square with the diver in
// the middle of it, under the fish. see THE FLASHLIGHT in fish_headers_diver, and diverStep.

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// the game sets these on every frame. a parameter that the game never sets does not keep a value that
// it is given here, so the things that never change are #defines instead.
//   Light.x  how much of the light there is, from 0 to 1. it comes and goes with the night.
//   Light.y  the time, in seconds
//   Light.z  1 when the diver faces right, and -1 when it faces left
//   Light.w  how big a cell of the board is, in pixels
//   Lens     where the bright end of the flashlight is, from the middle of the sprite, in pixels
float4 Light;
float2 Lens;

// how far the pool reaches, in cells. the fish in the four cells that touch the diver are in the light,
// and the ones at the corners are not, so the pool is somewhere between a circle and a diamond: the
// middle of a cell at a corner is past the edge of it.
#define REACH 1.62

// how far the beam goes from the flashlight, in cells, and how fast it opens out
#define BEAM_LONG 1.25
#define BEAM_OPEN 0.55

// how far the beam sways, in radians, and how fast
#define BEAM_SWAY 0.22
#define BEAM_SWAY_SPEED 0.9

// how bright the pool is at the diver, and how much the beam adds, and how many steps the light comes in
#define POOL_BRIGHT 0.46
#define BEAM_BRIGHT 0.85
#define STEPS 7.0

// the color of the light, and how much of it is drawn at its brightest
#define WARM float3(1.00, 0.86, 0.48)
#define STRENGTH 0.58

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
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

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// where this pixel is from the middle of the sprite, in pixels
	float2 uv = input.TextureCoordinates;
	float2 perPixel = max(fwidth(uv), .0001);
	float2 pixel = floor(uv / perPixel);
	float2 size = 1 / perPixel;
	float2 from = pixel + .5 - size * .5;
	float cell = max(Light.w, 1.0);

	// the pool. it is brightest at the diver.
	float2 d = from / cell;
	float far = .5 * (length(d) + abs(d.x) + abs(d.y));
	float pool = saturate(1.0 - far / REACH);

	// the beam. it starts at the flashlight, opens out the way that the diver faces, and sways a
	// little, as the hand that holds it does. it does not reach past the pool.
	float2 v = (from - Lens) / cell;
	v.x *= Light.z;
	float sway = sin(Light.y * BEAM_SWAY_SPEED) * BEAM_SWAY;
	float along = v.x * cos(sway) + v.y * sin(sway);
	float across = v.y * cos(sway) - v.x * sin(sway);
	float wide = .07 + max(along, 0.0) * BEAM_OPEN;
	float beam = saturate(1.0 - abs(across) / wide) * step(0.0, along) * saturate(1.0 - along / BEAM_LONG);
	beam *= saturate(pool * 5.0);

	// it comes in steps, with a checkerboard between one step and the next, like the halo of a fish
	// does at night, so that it has no hard edge
	float bright = pool * POOL_BRIGHT + beam * BEAM_BRIGHT;
	float checker = frac((pixel.x + pixel.y) * .5) * 2.0;
	float level = saturate(floor(bright * STEPS + checker * .5) / STEPS);

	return float4(WARM, level * STRENGTH * saturate(Light.x)) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
