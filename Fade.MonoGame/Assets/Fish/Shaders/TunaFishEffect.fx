#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Draws the fish that fill out the QR code, when TUNA is typed. See TUNA in fish_headers.
//
// The fish of the board go to their places in the code first. Then the rest of the fish come out of
// them: each one starts on top of a fish of the board, where it cannot be seen, and swims from there
// to a place of its own.
//
// A sprite sits where its fish ends up, and never moves. This draws the fish away from there, on the
// fish that it comes out of, and brings it over as Progress goes up. So the game only has to set
// Progress, and not move hundreds of fish on every frame.
//
// The fish are drawn in their own colors. The screen effect makes them dark.
//
// What is different from one fish to the next comes in on the two extra texcoords.
//
//   texcoord1.xy where the fish starts, in pixels, from where it ends up
//   texcoord1.z  how late it sets off, from 0 to 1. the ones that have further to go set off later.
//
//   texcoord2.x  where in its bobbing up and down the fish is, in radians, so that they are not in step

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// how far along the code is, from 0 to 1. the fish of the board take the first half of that to get to
// their places. these fish take the second half: at a half, every one of them is still on the fish
// that it comes out of, and at 1 every one is in its place.
float Progress;

// in seconds
float Time;

// 1 while the fish are on their way out. they turn around, so that they do not swim backwards.
float Leaving;

// how much of their half the last fish to set off has left to go in. 1 is half of it.
#define SPREAD 1.0

// how far a fish bobs up and down while it swims, in pixels, and how quickly
#define BOB 1.0
#define BOB_SPEED 7.0

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

	// which way the fish is swimming: 1 to the right, and -1 to the left
	float Heading : TEXCOORD1;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1,
								float4 custom2  : TEXCOORD2)
{
	VertexShaderOutput output;

	// how far along this fish is. it sets off late, starts and stops gently, and is there before the end.
	float half = saturate(Progress * 2.0 - 1.0);
	float along = saturate(half * (1.0 + SPREAD) - custom.z * SPREAD);
	float away = 1.0 - along * along * (3.0 - 2.0 * along);

	// it bobs while it swims, and settles as it gets there
	float bob = sin(Time * BOB_SPEED + custom2.x) * BOB * saturate(away * 6.0);
	position.xy += floor(custom.xy * away + float2(0.0, bob) + .5);

	// on the way out, it swims away from where it started. on the way back, toward it.
	output.Heading = sign(custom.x) * (Leaving * 2.0 - 1.0);

	// it cannot be seen until it has set off, so that it comes out of the other fish, and is not
	// just a second fish on top of it
	color.a *= saturate(along * 8.0);

	output.TextureCoordinates = texCoord;
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// the pictures of the fish all face left
	float2 uv = input.TextureCoordinates;
	uv.x = input.Heading > 0.0 ? 1.0 - uv.x : uv.x;

	return tex2D(SpriteTextureSampler, uv) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
