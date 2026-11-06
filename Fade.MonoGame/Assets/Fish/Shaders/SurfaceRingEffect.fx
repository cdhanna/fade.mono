#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// A ring going out on the surface of the sea, seen from the side, so it is a flat ellipse: the one
// that fills the sprite. it is drawn a whole pixel of the world at a time, one pixel thick, and the
// half of it that is nearer (the bottom) is brighter than the far half. The game itself does not use
// it: the Steam art and the trailer's title do, where the words come up out of the sea (see
// capSurfRing in fish_tests_capsule).
//
//   texcoord1.xy  how big the sprite is, in pixels of the world
//   texcoord1.z   how thick the ring is, in pixels

float4x4 MatrixTransform;

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 Quad : TEXCOORD0;
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
	output.Quad = texCoord;
	output.Custom = custom;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 size = max(input.Custom.xy, float2(1.0, 1.0));
	// the middle of the pixel of the world that this is in, from -1 to 1 across the ellipse
	float2 p = ((floor(input.Quad * size) + .5) / size - .5) * 2.0;
	float d = length(p);
	// how far that is from the ellipse, in pixels: how far off 1 it is, over how fast it changes per pixel
	float2 half = size * .5;
	float grow = length(p / half) / max(d, .001);
	float off = abs(d - 1.0) / max(grow, .0001);
	float rim = step(off, max(input.Custom.z, 1.0) * .5);
	// the near half is the bright one
	float near = lerp(.55, 1.0, step(0.0, p.y));
	return float4(input.Color.rgb, input.Color.a * rim * near);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
