#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the specks that drift down in front of the fish. there are only a few, and they are faint, so that
// they do not get in the way of the board. the ones behind the board are part of SeabedEffect.fx, and
// these move the same way that those do, only faster, because they are closer.
// it is drawn onto one plain sprite that covers the whole screen, so there is no texture.

// the game sets all of these on every frame
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Clock;     // the time that the specks move by, in seconds. it runs faster when the mood is high.
float Mood;      // how long the run of pops that is going on is, from 0 to 1. there are more specks, and they are bigger.
float Rainbow;   // 0 to 1. the specks turn the colors of the rainbow in a long run of pops, every time more fish pop.

#define SnowColor   float3(0.620, 0.780, 0.820)

// a number from 0 to 1 that is different for every pixel
float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
}

// a color of the rainbow. 0 and 1 are both red. it is pale, so that it still looks like a speck of light.
float3 rainbow(float hue)
{
	float3 color = saturate(abs(frac(hue + float3(0.0, 0.6667, 0.3333)) * 6.0 - 3.0) - 1.0);
	return lerp(float3(1.0, 1.0, 1.0), color, 0.7);
}

// the screen is cut into squares, and there is at most one speck in each square. this is how bright
// the speck on this pixel is, or 0 if there is none.
// share is how many of the squares have a speck, from 0 to 1, and size is how big a speck is, in pixels.
float snow(float2 px, float square, float2 speed, float seed, float share, float size)
{
	// the specks do not move in whole pixels. one that is part of the way between two pixels lights
	// up both of them, each by its share, and that is what makes it glide. the current pushes each
	// row a little to one side and back, and further when the mood is high.
	float2 q = px + 0.5 - Clock * speed;
	q.x += sin(Clock * 0.45 + px.y * 0.03 + seed) * (2.5 + 4.0 * Mood);

	// the speck stays clear of the edges of its square, so that none of it gets cut off
	float2 cell = floor(q / square);
	float2 spot = size + float2(hash(cell + seed), hash(cell + seed + 17.0)) * (square - 2.0 * size);
	float2 away = abs(q - cell * square - spot);
	float hit = saturate(size - away.x) * saturate(size - away.y) * step(hash(cell + seed + 5.0), share);

	float bright = 0.35 + 0.65 * hash(cell + seed + 9.0);
	float twinkle = 0.75 + 0.25 * sin(Time * 1.1 + hash(cell + seed + 3.0) * 40.0);
	return hit * bright * twinkle;
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	float2 px = floor(uv * Size);

	float speck = snow(px, 47.0, float2(5.0, 10.0), 120.0, 0.55 + 0.45 * Mood, 1.0 + 0.5 * Mood);

	// the same bands of color that the specks behind the board have
	float3 color = lerp(SnowColor, rainbow(px.x * 0.0045 + px.y * 0.0030 - Time * 0.12), Rainbow);
	float alpha = speck * saturate(0.45 + 0.25 * Mood + 0.3 * Rainbow);

	return float4(color * alpha, alpha);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
