#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the sea floor, behind the board: the water getting darker on the way down, shafts of light from the
// surface, three ridges of rock one behind the other, and specks drifting down through all of it.
// it is drawn onto one plain sprite that covers the whole screen, so there is no texture.
// the specks that drift in front of the fish are a sprite of their own, with SnowEffect.fx.

// the game sets all of these on every frame. a parameter that the game never sets does not keep
// a value that it is given here, so the things that never change are #defines instead.
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Clock;     // the time that the specks move by, in seconds. it runs faster when the mood is high, so they speed up without jumping.
float Rainbow;   // 0 to 1. the specks turn the colors of the rainbow in a long run of pops, every time more fish pop.
float Mood;      // how long the run of pops that is going on is, from 0 to 1. the water is brighter, the shafts of light are stronger, and there are more specks.

// the water, at the top of the screen and at the bottom of it
#define WaterTop    float3(0.090, 0.290, 0.410)
#define WaterLow    float3(0.055, 0.115, 0.250)

#define RayColor    float3(0.450, 0.780, 0.800)
#define SnowColor   float3(0.620, 0.780, 0.820)

// the light comes from the left, where the sun is on the main menu. this is how far a shaft goes
// to the right for every pixel that it goes down.
#define RaySlant 0.55

// where the top of each ridge is, as a share of the way down the screen, and how many pixels tall its bumps are
#define FarBase   0.640
#define FarRough  46.0
#define MidBase   0.800
#define MidRough  40.0
#define NearBase  0.945
#define NearRough 26.0

// the camera drifts from side to side a little. the near ridge moves by this many pixels, and the
// ones behind it by less.
#define CamSway 16.0

// a number from 0 to 1 that is different for every pixel
float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
}

// cut a smooth 0 to 1 into steps, with a checkerboard between one step and the next
float bands(float v, float count, float2 px)
{
	float checker = frac((px.x + px.y) * 0.5) * 2.0;
	return floor(v * count + checker * 0.5) / count;
}

// a smooth line of bumps
float noise1(float x, float seed)
{
	float i = floor(x);
	float f = frac(x);
	f = f * f * (3.0 - 2.0 * f);
	return lerp(hash(float2(i, seed)), hash(float2(i + 1.0, seed)), f);
}

// how far down the screen the top of a ridge is
float ground(float x, float base, float rough, float seed)
{
	float n = noise1(x * 0.011, seed) * 0.6 + noise1(x * 0.034, seed + 3.0) * 0.28 + noise1(x * 0.097, seed + 7.0) * 0.12;
	return floor(base * Size.y + (0.5 - n) * rough);
}

// the water behind everything
float3 water(float2 px)
{
	float v = bands(saturate(px.y / Size.y), 18.0, px);
	return lerp(WaterTop, WaterLow, smoothstep(0.0, 0.9, v));
}

// a color of the rainbow. 0 and 1 are both red. it is pale, so that it still looks like a speck of light.
float3 rainbow(float hue)
{
	float3 color = saturate(abs(frac(hue + float3(0.0, 0.6667, 0.3333)) * 6.0 - 3.0) - 1.0);
	return lerp(float3(1.0, 1.0, 1.0), color, 0.7);
}

// specks that drift down and across. the screen is cut into squares, and there is at most one speck
// in each square. this is how bright the speck on this pixel is, or 0 if there is none.
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
	// whole pixels, so that everything is as blocky as the rest of the game
	float2 px = floor(uv * Size);

	// the camera sways, slowly, and not quite evenly
	float sway = sin(Time * 0.07) * CamSway * 0.6 + sin(Time * 0.031 + 1.0) * CamSway * 0.4;

	// where this pixel is across the shafts of light, which are slanted
	float across = px.x - px.y * RaySlant + floor(sway * 0.15);

	// the light comes and goes, like clouds are crossing the sun. it takes a minute or so, and it
	// crosses the screen along with the shafts. this is -1 in the shade and 1 in the sun.
	float cloud = sin(across * 0.0045 + Time * 0.11) * 0.6 + sin(across * 0.0021 - Time * 0.06 + 2.0) * 0.4;

	// how bright the water is. it is a little dim until the player gets going.
	float light = (0.85 + 0.4 * Mood) * (1.0 + 0.1 * cloud);

	float3 sea = water(px) * light;
	float3 color = sea;

	// the darkest that rock gets. it is the color of the water at the bottom of the screen, only darker.
	float3 rock = water(float2(px.x, Size.y)) * light * 0.45;

	// the far ridge. it is nearly the color of the water.
	float farX = px.x + floor(sway * 0.25);
	float farTop = ground(farX, FarBase, FarRough, 11.0);
	color = lerp(color, lerp(sea, rock, 0.22), step(farTop, px.y));

	// the middle ridge
	float midX = px.x + floor(sway * 0.5) + 300.0;
	float midTop = ground(midX, MidBase, MidRough, 23.0);
	float3 midFog = lerp(sea, rock, 0.5);
	float midRock = step(midTop, px.y);
	float3 midColor = midFog + 0.035 * step(px.y, midTop + 0.5);
	color = lerp(color, midColor, midRock);

	// the shafts of light. each one is a slanted stripe, and they slide across each other. they are
	// bright at the top of the screen and gone before the bottom.
	float ray = sin(across * 0.019 + Time * 0.10) + 0.7 * sin(across * 0.043 - Time * 0.07 + 1.3) + 0.5 * sin(across * 0.071 + Time * 0.045 + 4.0);
	ray = smoothstep(0.45, 1.9, ray) * (0.85 + 0.15 * sin(Time * 0.6 + across * 0.1));
	float reach = saturate(1.0 - (px.y / Size.y) * 1.05);
	ray = bands(saturate(ray * reach * (0.55 + 1.3 * Mood) * (1.0 + 0.3 * cloud)), 10.0, px);
	color += RayColor * ray * 0.2;

	// specks, far away
	float specks = snow(px, 19.0, float2(1.5, 3.0), 40.0, 0.7 + 0.3 * Mood, 1.0) * 0.5 + snow(px, 31.0, float2(3.0, 6.0), 80.0, 0.7 + 0.3 * Mood, 1.0 + 0.4 * Mood);

	// when the mood is high there are more of them, and the new ones cross the others the other way
	specks += snow(px, 23.0, float2(-2.5, 4.5), 160.0, Mood, 1.0) * Mood;
	specks *= 1.0 + 0.8 * Mood;

	// when the specks are rainbows, the colors run across the screen in slanted bands, and they are brighter
	float3 speckColor = lerp(SnowColor * 0.22, rainbow(px.x * 0.0045 + px.y * 0.0030 - Time * 0.12) * 0.6, Rainbow);
	color += speckColor * specks;

	// the near ridge is the sand that the board is over
	float nearX = px.x + floor(sway) + 700.0;
	float nearTop = ground(nearX, NearBase, NearRough, 37.0);
	float3 nearFog = lerp(sea, rock, 0.8);
	float nearRock = step(nearTop, px.y);
	float grit = step(0.86, hash(float2(nearX, px.y))) * 0.02;
	float3 nearColor = nearFog + 0.05 * step(px.y, nearTop + 0.5) + grit;
	color = lerp(color, nearColor, nearRock);

	return float4(color, 1.0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
