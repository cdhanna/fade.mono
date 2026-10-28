#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE WAY IN. when Neptune raises his trident, a blot of the ink of the night opens in the water behind
// him, and it is not ink: it is the sky, deep and full of stars, the way that it was over the moon. the
// temple comes through it, and then it closes. see THE SUMMONING in fish_headers_neptune.
// it is drawn on one plain sprite, with no texture. the edge of it is the edge of the squid's ink, with
// its light and its glow (see InkEffect.fx), so that it reads as the same stuff.

float4x4 MatrixTransform;

// the game sets these on every frame.
//   Portal  x the time, in seconds. y how far it is open, from 0 to 1. z and w how big the sprite is, in pixels.
float4 Portal;

// how wide the blot is when it is all the way open, next to the sprite, and how much its edge wobbles
#define REACH 0.38
#define WOBBLE 0.035

#define RIM 2.0
#define GLOW_REACH 0.09
#define GLOW_STEPS 6.0

#define DEEP      float3(0.020, 0.012, 0.060)
#define NEBULA_A  float3(0.22, 0.08, 0.38)
#define NEBULA_B  float3(0.06, 0.18, 0.40)
#define STAR      float3(1.00, 0.96, 0.86)
#define EDGE_A    float3(0.45, 0.92, 1.00)
#define EDGE_B    float3(0.78, 0.52, 1.00)
#define HALO_A    float3(0.16, 0.48, 1.00)
#define HALO_B    float3(0.50, 0.26, 1.00)

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 Quad : TEXCOORD0;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	VertexShaderOutput output;
	position.xy = floor(position.xy + .5);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.Quad = texCoord;
	return output;
}

float hash(float2 p)
{
	return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float time = Portal.x;
	float open = saturate(Portal.y);
	float2 size = max(Portal.zw, 1.0);

	// where the pixel is, a whole pixel of the world at a time, from the middle of the sprite
	float2 px = floor(input.Quad * size);
	float2 from = (px + .5 - size * .5) / size;
	from.y *= size.y / size.x;

	// the blot: it opens out from nothing, and its edge wobbles the way that ink does
	float angle = atan2(from.y, from.x);
	float grown = open * (2.0 - open);
	float wobble = WOBBLE * sin(angle * 3.0 + time * 1.3) + WOBBLE * .6 * sin(angle * 5.0 - time * .9);
	float tall = size.y / size.x;
	float2 squash = float2(from.x, from.y / max(tall, .01));
	float d = (length(squash) - (REACH + wobble) * grown) * size.x;

	// inside: the deep, a slow cloud of color drifting through it, and the stars, which twinkle
	float cloud = .5 + .5 * sin(from.x * 9.0 + time * .4 + sin(from.y * 11.0 - time * .3) * 1.7);
	float3 color = DEEP + lerp(NEBULA_A, NEBULA_B, cloud) * cloud * .55;
	float starSeed = hash(px);
	float twinkle = .6 + .4 * sin(time * 3.0 + starSeed * 40.0);
	float star = step(.985, starSeed) * twinkle;
	float big = step(.9985, hash(floor(px / 3.0)));
	float2 inBig = px - floor(px / 3.0) * 3.0;
	float cross = big * step(abs(inBig.x - 1.0) + abs(inBig.y - 1.0), 1.0);
	color = lerp(color, STAR, saturate(star + cross));
	float alpha = .96;

	// the bright edge, the colors of the edge of the ink
	float swing = .5 + .5 * sin(angle * 2.0 + time * 1.6);
	float runner = pow(.5 + .5 * sin(angle * 2.0 - time * 3.0), 10.0);
	float onEdge = step(-RIM, d) * step(d, 0.0);
	color = lerp(color, lerp(EDGE_A, EDGE_B, swing) * (1.0 + runner * .8), onEdge);

	// outside: the glow, in steps, with a checkerboard between them
	float glow = exp(-max(d, 0.0) * GLOW_REACH) * .62;
	float checker = frac((px.x + px.y) * .5) * 2.0;
	float glowStep = saturate(floor(glow * GLOW_STEPS + checker * .5) / GLOW_STEPS);
	float outside = step(0.0, d);
	color = lerp(color, lerp(HALO_A, HALO_B, swing), outside);
	alpha = lerp(alpha, glowStep, outside);

	alpha *= step(.001, open);
	return float4(color, alpha) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
