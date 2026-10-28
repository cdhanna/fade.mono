#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE FIZZ. the ink of one cell going, once it has been washed off, popped with its constellation, lost
// at nightfall, or broken up by coral: the blot of the ink goops up, pinches into bubbles, and they
// break away from it and rise, pale as they fill with water, and pop, until there is nothing left. a
// clean cell of the pot that pays poofs away instead, in the look that InkEffect.fx gives clean cells: it
// swells and flashes, and breaks up into white sparkles from the edge in. see THE FIZZ in fish_headers_squid.
// every sprite is one of them, a few cells across, so that the bubbles have room to rise, and the game
// tells each one how it is doing in its texcoord2:
//   x  how far along it is, from 0 to 1
//   y  a number of its own, so that no two go the same way
//   z  what it is, times 10 (0 the ink, 1 a clean cell that poofs), and how much ink it is, from 0 to 1
//      of a cell's worth, added to that
//   w  how much it is the dried ink of the day, from 0 to 1, which has its colors
// and on every frame, for all of them:
//   Size  how big the sprite is, in pixels

float4x4 MatrixTransform;
float Size;

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

// the sprite is this many cells across, and the blot this big in it, in cells, before it goops up. it
// has to be the same as INK_FIZZ_SPAN# in fish_headers_squid.
#define SPAN 3.0
#define BLOT 0.47

// how many bubbles it breaks into, and how much they run together at the start
#define BUBBLES 7
#define GOO 0.16

// the colors: the ink of the night and its edge, the ink of the day and its edge, and the water that the
// bubbles fill with
#define NIGHT_EDGE float3(0.45, 0.92, 1.00)
#define DAY_EDGE   float3(0.19, 0.29, 0.58)
#define WATER      float3(0.70, 0.88, 1.00)

// a clean cell, as InkEffect.fx draws it: its light, how far in from the side of its cell it stops, how
// round its corners are, and how much of its inside, its edge and its glow there is
#define CLEAN_LIGHT float3(0.72, 0.95, 1.00)
#define CLEAN_HALF  0.45
#define CLEAN_ROUND 0.42
#define CLEAN_FILL  0.025
#define CLEAN_RIM   0.16
#define CLEAN_SPOT  0.22

// the cloud of the ink, which is the same as InkEffect.fx's, so that the goo is the same ink
#define CLOUD_BIG    11.0
#define CLOUD_SMALL  5.0
#define CLOUD_DRIFT  1.4
#define CLOUD_DARK   float3(0.012, 0.018, 0.070)
#define CLOUD_BLUE   float3(0.040, 0.090, 0.290)
#define CLOUD_PURPLE float3(0.190, 0.060, 0.300)
#define CLOUD_STEPS  6.0
#define DEEP         float3(0.035, 0.025, 0.095)
#define CLOUD_NIGHT  0.55

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 Quad : TEXCOORD0;
	float4 Fizz : TEXCOORD1;
	float2 World : TEXCOORD2;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1,
								float4 custom2  : TEXCOORD2)
{
	VertexShaderOutput output;
	position.xy = floor(position.xy + .5);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.Quad = texCoord;
	output.Fizz = custom2;
	output.World = position.xy;
	return output;
}

// a number from 0 to 1, which looks random
float hash21(float2 p)
{
	return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float vnoise(float2 p)
{
	float2 i = floor(p);
	float2 f = frac(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash21(i);
	float b = hash21(i + float2(1.0, 0.0));
	float c = hash21(i + float2(0.0, 1.0));
	float e = hash21(i + float2(1.0, 1.0));
	return lerp(lerp(a, b, f.x), lerp(c, e, f.x), f.y);
}

float3 cloudAt(float2 world, float time, float checker)
{
	float drift = time * CLOUD_DRIFT;
	float n = vnoise((world + float2(drift, drift * .4)) / CLOUD_BIG) * .62
	        + vnoise((world + float2(-drift * .7, drift * .8)) / CLOUD_SMALL) * .38;
	n = saturate(floor(n * CLOUD_STEPS + checker * .5) / CLOUD_STEPS);
	float3 tint = lerp(CLOUD_BLUE, CLOUD_PURPLE, saturate(n * 2.2 - .7));
	return lerp(CLOUD_DARK, tint, saturate(n * 1.8 - .1));
}

// a smooth minimum, so that shapes that meet run together, by `k`
float melt(float a, float b, float k)
{
	float h = saturate(.5 + .5 * (b - a) / max(k, .0001));
	return lerp(b, a, h) - k * h * (1.0 - h);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float t = saturate(input.Fizz.x);
	float seed = input.Fizz.y;
	float mode = floor(input.Fizz.z / 10.0 + .001);
	float amount = saturate(input.Fizz.z - mode * 10.0);
	float day = saturate(input.Fizz.w);

	// the pixel, a whole pixel of the world at a time, and where it is from the middle, in cells
	float2 px = floor(input.Quad * Size);
	float2 p = ((px + .5) / max(Size, 1.0) - .5) * SPAN;
	float cellPx = Size / SPAN;
	float checker = frac((px.x + px.y) * .5) * 2.0;

	// the blot goops up: it wobbles hard, heaves upward, and shrinks away as the bubbles take it
	float gather = saturate((t - .12) / .5);
	float2 heave = float2(0.0, -.14 * saturate(t / .3)) * amount;
	float2 fromBlot = p - heave;
	float angle = atan2(fromBlot.y, fromBlot.x);
	float wobble = (.06 * sin(angle * 3.0 + seed + t * 14.0) + .04 * sin(angle * 5.0 - seed * 1.7 + t * 9.0)) * saturate(t * 6.0);
	float blotR = (BLOT + wobble) * amount * (1.0 + .1 * sin(saturate(t / .3) * 3.14159)) * (1.0 - gather * gather);
	float d = length(fromBlot) - blotR;
	float goo = GOO * (1.0 - saturate((t - .2) / .4));

	// the bubbles: each starts in the blot, swells out of it, rises and drifts, fills with water, and pops
	float hollow = 10.0;
	float shine = 0.0;
	float burst = 0.0;
	[unroll] for (int i = 0; i < BUBBLES; i++)
	{
		float fi = i;
		float h1 = hash21(float2(seed, fi * 7.1));
		float h2 = hash21(float2(fi * 3.3, seed + 1.7));
		float h3 = hash21(float2(seed * .37 + fi, 9.1));
		float side = (h1 - .5) * 2.0;
		float2 from = float2(side * .3, -.05 - h2 * .25) * amount;
		float rise = saturate((t - .2 - h3 * .15) / .75);
		rise = rise * (2.0 - rise);
		float2 at = from * (1.0 + rise * 1.1) + float2(sin(rise * 5.0 + h1 * 6.28) * .1, -rise * (.55 + h2 * .7));
		float popAt = .55 + h3 * .4;
		float popped = step(popAt, t);
		float swell = saturate((t - .04 - h1 * .08) / .25);
		float r = (.12 + h2 * .13) * (.45 + amount * .55) * swell * (1.0 - .3 * rise) * (1.0 - popped);
		float b = length(p - at) - r;
		d = melt(d, b + popped * 10.0, goo);
		hollow = min(hollow, b + popped * 10.0);

		// a spot of light on its top left, once it is a bubble of its own
		float2 lit = at + float2(-.4, -.45) * r;
		shine = max(shine, step(length(p - lit), r * .3) * (1.0 - popped) * step(.2, t));

		// and a ring, as it pops
		float since = (t - popAt) / .08;
		float ring = abs(length(p - at) - (.12 + h2 * .13) * (.45 + amount * .55) * (1.0 + since * .8)) * cellPx;
		burst = max(burst, step(ring, .75) * step(0.0, since) * step(since, 1.0));
	}

	// the colors: the ink, with the cloud through it, and its edge. the bubbles pale as they fill with
	// water, from the outside in, and what is left of the blot stays ink.
	float3 cloud = cloudAt(input.World, t * .9 + seed, checker);
	float3 ink = lerp(lerp(DEEP, cloud, CLOUD_NIGHT), cloud, day);
	float3 edge = lerp(NIGHT_EDGE, DAY_EDGE, day);
	float insidePx = -d * cellPx;
	float inside = step(0.0, insidePx);
	float rim = step(insidePx, 1.2);
	float fill = saturate((t - .3) / .45);
	float bubbleOf = step(hollow, d + .02);
	float3 body = lerp(ink, WATER, fill * bubbleOf * .55);
	float bodyAlpha = lerp(.92, .35, fill * bubbleOf);
	float3 color = lerp(body, edge, rim);
	float alpha = inside * lerp(bodyAlpha, .85, rim);
	color = lerp(color, WATER, shine);
	alpha = max(alpha, shine * .9);
	color = lerp(color, WATER, burst);
	alpha = max(alpha, burst * .7);

	// A POOF. the clean cell swells and flashes, and breaks up into white sparkles from the edge in
	float2 box = abs(p) - (CLEAN_HALF - CLEAN_ROUND);
	float boxD = length(max(box, 0.0)) + min(max(box.x, box.y), 0.0) - CLEAN_ROUND;
	float swellBy = sin(saturate(t * 2.5) * 3.14159) * .1;
	float poofPx = -(boxD - swellBy) * cellPx;
	float poofIn = step(0.0, poofPx);
	float flash = sin(saturate(t * 3.0) * 3.14159);
	float spot = exp(-dot(p, p) * 9.0);
	float poofA = (CLEAN_FILL + step(poofPx, 1.5) * CLEAN_RIM + spot * CLEAN_SPOT) * (1.0 + flash * 1.2);
	float breakUp = saturate((t - .2) / .75);
	float left = step(hash21(px + floor(t * 14.0)) * .7 + saturate(1.0 - poofPx / 4.0) * .3, 1.0 - breakUp);
	poofA = lerp(poofA, max(poofA, .5) * left * (1.0 - breakUp), step(.001, breakUp)) * poofIn;
	float3 poofColor = lerp(CLEAN_LIGHT, float3(1.0, 1.0, 1.0), saturate(flash + left * breakUp));
	float poofing = step(.5, mode);
	color = lerp(color, poofColor, poofing);
	alpha = lerp(alpha, saturate(poofA), poofing);

	alpha *= 1.0 - smoothstep(.85, 1.0, t);
	alpha *= tex2D(SpriteTextureSampler, input.Quad).a;
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
