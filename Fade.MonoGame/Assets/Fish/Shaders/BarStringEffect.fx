#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE STRING. the bar of the round, which was drawn to a render target of its own, drawn over the whole
// screen and bent by waves, as if it were a string. each wave is a bump that runs out both ways from
// where it started, bounces off the two ends of the bar, and dies away. it moves in whole pixels, so
// that it stays as sharp as the rest of the game. see THE STRING in fish_headers_charge.
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

// x is the time, in seconds
float2 Time;
// how big the screen is, in pixels, which is how big the render target is
float2 Screen;
// the left and right ends of the bar, in pixels
float2 Span;
// the waves: where each started, across the screen, when, in seconds, and how far it bends the bar, in
// pixels. one that bends it by 0 is not there.
float4 Pulse0;
float4 Pulse1;
float4 Pulse2;
float4 Pulse3;
float4 Pulse4;
float4 Pulse5;

#define SPEED 300.0  // how fast a wave runs along the bar, in pixels a second
#define WIDE  10.0   // how wide its bump is, in pixels
#define FADE  2.6    // how quickly it dies away
#define MOST  6.0    // the most that the bar bends, in pixels

// where a wave that has run this far from its start is, bounced back and forth between the ends
float fold(float p)
{
	float w = max(Span.y - Span.x, 1.0);
	float q = fmod(p - Span.x, 2.0 * w);
	q = q < 0.0 ? q + 2.0 * w : q;
	return Span.x + (q < w ? q : 2.0 * w - q);
}

float bendBy(float4 pulse, float x)
{
	float age = Time.x - pulse.y;
	if (pulse.z <= 0.0 || age < 0.0)
		return 0.0;
	float strength = pulse.z * exp(-age * FADE);
	float run = SPEED * age;
	float d1 = x - fold(pulse.x + run);
	float d2 = x - fold(pulse.x - run);
	// it starts as one bump, which splits into two, half as big, that run apart
	float half1 = exp(-(d1 * d1) / (2.0 * WIDE * WIDE));
	float half2 = exp(-(d2 * d2) / (2.0 * WIDE * WIDE));
	return strength * 0.5 * (half1 + half2);
}

// the bar, as it was before it was blended onto the see-through target. see RainbowTextEffect.
float4 barAt(float2 uv)
{
	float4 bar = tex2D(SpriteTextureSampler, uv);
	float alpha = sqrt(bar.a);
	return float4(bar.rgb / max(alpha, 0.001), alpha);
}

float4 MainPS(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	if (Screen.y < 1.0)
		return barAt(uv) * tint;
	float x = uv.x * Screen.x;
	float bend = 0.0;
	if (x >= Span.x && x <= Span.y)
	{
		bend += bendBy(Pulse0, x);
		bend += bendBy(Pulse1, x);
		bend += bendBy(Pulse2, x);
		bend += bendBy(Pulse3, x);
		bend += bendBy(Pulse4, x);
		bend += bendBy(Pulse5, x);
		// it is pinned at the two ends, like a string
		float toEnd = min(x - Span.x, Span.y - x);
		bend *= saturate(toEnd / 12.0);
		bend = clamp(round(bend), -MOST, MOST);
	}
	// up is a smaller y: what is drawn here was `bend` pixels lower in the target
	return barAt(uv + float2(0.0, bend / Screen.y)) * tint;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
