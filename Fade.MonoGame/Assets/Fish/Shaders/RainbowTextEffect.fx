#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE RAINBOW SHARES. the numbers that float up off of the rainbow fish, which were drawn, in white, to a
// render target of their own. this is drawn over the whole screen, like ScoreTextEffect, and turns them to
// the colors of the rainbow, from where they are on the screen, in the same waves that go through the
// rainbow fish (THE WAVES OF A RAINBOW FISH in FishEffect.fx), with a glint that runs along them and a
// glow of the rainbow around them. see fish_headers_rainbow.
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

// the same as FishEffect's: angle, how long a wave is, how many steps, and the swirl. see RainbowWave there.
float4 RainbowWave;
// the same as FishEffect's: y is how many waves go by in a second, and w how pale the colors are.
float4 RainbowRim;
// x is the time, in seconds, the way that FishEffect's Night.y is. y is how many pixels the glow reaches.
float2 Time;
// how big the screen is, in pixels, which is how big the render target is
float2 Screen;

// how bright the glint is, how wide it is along the wave, and how many times a second one goes by
#define GLINT_LIGHT 0.85
#define GLINT_WIDE  0.10
#define GLINT_SPEED 0.9
// how bright the glow is, at the edge of the text
#define GLOW_LIGHT  0.55
// how much of the way to white the colors of the letters are, so that they read on the dark water
#define LETTER_PALE 0.45

float3 rainbowAt(float2 screenPx, out float along)
{
	float2 way = float2(cos(RainbowWave.x), sin(RainbowWave.x));
	float2 side = float2(-way.y, way.x);
	float wave = max(RainbowWave.y, 1.0);
	along = dot(screenPx, way) / wave;
	along += RainbowWave.w * 0.25 * sin(dot(screenPx, side) / (wave * 0.6) * 6.2832 + Time.x * 0.9);
	along -= Time.x * RainbowRim.y;
	float3 hue = 0.5 + 0.5 * cos(6.2832 * (along + float3(0.0, 0.33, 0.67)));
	return lerp(hue, float3(1.0, 1.0, 1.0), RainbowRim.w * 0.3);
}

// the text, as it was before it was blended onto the see-through target. see ScoreTextEffect.
float4 textAt(float2 uv)
{
	float4 texts = tex2D(SpriteTextureSampler, uv);
	float alpha = sqrt(texts.a);
	return float4(texts.rgb / max(alpha, 0.001), alpha);
}

float4 MainPS(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float4 text = textAt(uv);
	float2 screenPx = uv * Screen;
	float along;
	float3 rainbow = rainbowAt(screenPx, along);

	// the letters are white and the shadow is dark: the rainbow goes on the letters, and the shadow stays
	float light = dot(text.rgb, float3(0.333, 0.333, 0.334));
	float glintAt = frac(along * 0.5 - Time.x * GLINT_SPEED);
	float glint = saturate(1.0 - abs(glintAt - 0.5) / GLINT_WIDE) * GLINT_LIGHT;
	float3 letters = lerp(rainbow, float3(1.0, 1.0, 1.0), max(glint, LETTER_PALE));
	float3 color = lerp(text.rgb, letters, light);

	// a glow of the rainbow around the letters, from how much of the text is close by
	float2 reach = Time.y / Screen;
	float near = 0.0;
	near += textAt(uv + float2(reach.x, 0.0)).a;
	near += textAt(uv - float2(reach.x, 0.0)).a;
	near += textAt(uv + float2(0.0, reach.y)).a;
	near += textAt(uv - float2(0.0, reach.y)).a;
	near += textAt(uv + reach * 0.7).a;
	near += textAt(uv - reach * 0.7).a;
	near += textAt(uv + float2(reach.x, -reach.y) * 0.7).a;
	near += textAt(uv + float2(-reach.x, reach.y) * 0.7).a;
	float glow = saturate(near / 8.0) * GLOW_LIGHT * (1.0 - text.a);

	float alpha = saturate(text.a + glow);
	float3 outColor = (color * text.a + rainbow * 0.7 * glow) / max(alpha, 0.001);
	return float4(outColor, alpha) * tint;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
