#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the sea floor, behind the board: the water getting darker on the way down, shafts of light from the
// surface, and three ridges of rock one behind the other.
// it is drawn onto one plain sprite that covers the whole screen, so there is no texture.
// the specks that drift down through it are a sprite of their own, with SpecksEffect.fx, and so are
// the ones that drift in front of the fish, with SnowEffect.fx.

// the game sets all of these on every frame. a parameter that the game never sets does not keep
// a value that it is given here, so the things that never change are #defines instead.
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Void;      // 0 for the sea. toward 1, the sea fades away, and what is there in place of it is the stars, seen through a black hole. see THE TWO ENDINGS in fish_headers.
float Mood;      // how long the run of pops that is going on is, from 0 to 1. the water is brighter, and the shafts of light are stronger.
float4 Tide;     // w is how many of the marks under the moon are filled in, in halves, or less than 0 for no marks. x is where the water is, from 0 for low water to 1 for high water. y is how much it is night, from 0 to 1. z is how much of the moon is lit, from 0 for a new moon to 1 for a full one. see fish_headers_tide.

// the water, at the top of the screen and at the bottom of it
#define WaterTop    float3(0.090, 0.290, 0.410)
#define WaterLow    float3(0.055, 0.115, 0.250)

#define RayColor    float3(0.450, 0.780, 0.800)

// THE NIGHT. the same water at night, and the light that comes down through it from the moon.
#define NightTop    float3(0.030, 0.080, 0.180)
#define NightLow    float3(0.008, 0.018, 0.065)
#define MoonRay     float3(0.520, 0.640, 0.950)

// the little lights that drift in the water at night
#define PlanktonColor float3(0.200, 0.950, 0.820)

// THE SURFACE. the underside of the surface of the water, along the top of the screen. it is this many
// pixels down the screen at high water, and this many at low water.
#define SURFACE_HIGH 1.0
#define SURFACE_LOW  10.0
// what is over the surface is only a little lighter than the water under it, so that it does not
// pull the eye up to the top of the screen
#define SkyDay      float3(0.135, 0.375, 0.495)
#define SkyNight    float3(0.042, 0.105, 0.225)
#define FoamColor   float3(0.300, 0.560, 0.660)

// THE MOON, seen through the water. where the middle of it is, as a share of the way across the screen
// and in pixels down it, and how big it is. it is in the gap between the board and the count of the rounds.
#define MOON_ACROSS 0.762
#define MOON_DOWN   46.0
#define MOON_SIZE   9.0
#define MoonColor   float3(0.930, 0.950, 1.000)

// how many marks there are under the moon. this is TIDE_CYCLES_TO_TURN in fish_headers_tide, and
// the two have to be kept the same by hand.
#define MARK_COUNT 3

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
	float deep = smoothstep(0.0, 0.9, v);
	return lerp(lerp(WaterTop, WaterLow, deep), lerp(NightTop, NightLow, deep), saturate(Tide.y));
}

// how much of the moon there is at this pixel, from 0 to 1. the lit part of it is a disc with another
// disc taken out of it, which slides away to the left as the moon fills.
float moon(float2 px, float2 middle)
{
	float2 from = px - middle;
	float disc = step(length(from), MOON_SIZE);
	float dark = step(length(from - float2(saturate(Tide.z) * MOON_SIZE * 2.1, 0.0)), MOON_SIZE);
	return disc * (1.0 - dark);
}

// THE STARS. when the ancient one comes, the sea behind the board fades away, and this is what is there.
// it is space, with a great swirl of stars and dust in it, seen through a black hole: the board is in
// the dark middle of the hole, and around it there is the ring of light that a black hole has.
//   VOID_RING     how far out from the middle of the screen the ring is, in pixels. the board fits inside of it.
//   VOID_WIDE     the ring is wider than it is tall, by this much
//   VOID_TURN     how fast the swirl turns
#define VOID_RING 146.0
#define VOID_WIDE 1.22
#define VOID_TURN 0.22
#define SpaceColor  float3(0.010, 0.006, 0.030)
#define DustWarm    float3(0.300, 0.080, 0.360)
#define DustCold    float3(0.060, 0.200, 0.420)
#define RingColor   float3(1.000, 0.700, 0.400)

// smooth clouds, from 0 to 1. `p` is in pixels, and a cloud is `size` pixels across.
float clouds(float2 p, float size, float seed)
{
	float2 q = p / size;
	float2 i = floor(q);
	float2 f = frac(q);
	f = f * f * (3.0 - 2.0 * f);
	float top = lerp(hash(i + seed), hash(i + float2(1.0, 0.0) + seed), f.x);
	float low = lerp(hash(i + float2(0.0, 1.0) + seed), hash(i + float2(1.0, 1.0) + seed), f.x);
	return lerp(top, low, f.y);
}

float3 stars(float2 px)
{
	// where this pixel is from the middle of the screen: how far, and which way around
	float2 from = (px - Size * float2(0.5, 0.53)) / float2(VOID_WIDE, 1.0);
	float out0 = length(from);
	float around = atan2(from.y, from.x);

	float3 color = SpaceColor;

	// the swirl. it has two arms, which wind in toward the middle, and it turns slowly. the dust in
	// it is two colors, in clouds.
	float dust = clouds(px + float2(Time * 3.0, 0.0), 38.0, 7.0) * 0.6 + clouds(px - float2(0.0, Time * 2.0), 15.0, 19.0) * 0.4;
	float arm = 0.5 + 0.5 * cos(around * 2.0 - log(max(out0, 1.0)) * 5.0 + Time * VOID_TURN);
	arm = arm * arm * arm;
	float swirl = bands(saturate(arm * (0.35 + 0.9 * dust) * saturate(1.3 - out0 / 330.0)), 7.0, px);
	color += lerp(DustWarm, DustCold, smoothstep(0.35, 0.7, dust)) * swirl;

	// the stars. most of them are faint, and they twinkle. there are more of them in the arms.
	float star = step(1.0 - (0.004 + 0.010 * arm), hash(px + 91.0));
	float bright = hash(px + 31.0);
	color += star * (0.25 + 0.75 * bright * bright) * (0.7 + 0.3 * sin(Time * 2.0 + hash(px + 7.0) * 40.0));

	// the ring of light around the hole. it is thin and bright, with a glow outside of it, and it
	// is brighter on one side, which goes around.
	float side = 0.65 + 0.35 * cos(around - Time * 0.35);
	float ring = saturate(1.0 - abs(out0 - VOID_RING) / 3.5);
	float glow = saturate(1.0 - abs((out0 - VOID_RING) - 6.0) / 30.0) * step(VOID_RING, out0 + 4.0);
	color += RingColor * (ring * side + bands(glow * glow, 6.0, px) * 0.45 * side);

	// and inside of the ring is the hole, where there is next to nothing
	color *= lerp(0.18, 1.0, smoothstep(VOID_RING - 26.0, VOID_RING - 2.0, out0));

	return color;
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
	float night = saturate(Tide.y);
	color += lerp(RayColor * 0.2, MoonRay * 0.11, night) * ray;

	// THE NIGHT. little lights drift up through the water, and twinkle. see fish_headers_tide.
	float2 grain = floor((px + float2(sin(Time * 0.2 + px.y * 0.02) * 6.0, Time * 5.0)) / 2.0);
	float plankton = step(0.988, hash(grain + 57.0));
	float twinkle = 0.5 + 0.5 * sin(Time * 1.7 + hash(grain + 3.0) * 40.0);
	color += PlanktonColor * plankton * twinkle * twinkle * 0.55 * night;

	// THE MOON, seen through the water, so it wobbles. it is faint in the day, and at night it is bright,
	// with a glow around it, and a shaft of its light comes down under it.
	float2 moonAt = float2(floor(Size.x * MOON_ACROSS), MOON_DOWN);
	float2 bent = px + float2(floor(sin(px.y * 0.45 + Time * 1.6) * 1.2 + 0.5), 0.0);
	float moonLit = moon(bent, moonAt);
	float moonGlow = saturate(1.0 - length(px - moonAt) / (MOON_SIZE * 3.2));
	float moonFall = saturate(1.0 - abs(px.x - moonAt.x - (px.y - moonAt.y) * 0.12) / (MOON_SIZE * 1.6)) * saturate(1.0 - (px.y - moonAt.y) / (Size.y * 0.8)) * step(moonAt.y, px.y);
	color += MoonRay * bands(saturate(moonGlow * moonGlow * 0.5 + moonFall * 0.22), 8.0, px) * night * saturate(Tide.z * 1.5);
	color = lerp(color, MoonColor, moonLit * lerp(0.30, 0.95, night));

	// THE SURFACE. the underside of the surface of the water, which comes down the screen as the tide goes
	// out. over it is the sky, and the line of it is bright, with a little light under it.
	float swell = sin(px.x * 0.050 + Time * 0.9) * 1.6 + sin(px.x * 0.113 - Time * 0.6 + 2.0) * 1.0;
	float surface = floor(lerp(SURFACE_LOW, SURFACE_HIGH, saturate(Tide.x)) + swell + 0.5);
	float under = px.y - surface;
	float3 sky = lerp(SkyDay, SkyNight, night) * (0.96 + 0.04 * sin(px.x * 0.21 + Time * 1.3));
	float sheen = bands(saturate(1.0 - under / 9.0), 5.0, px) * step(0.0, under);
	color += lerp(RayColor, MoonRay, night) * sheen * 0.05;
	color = lerp(color, sky, step(under, -0.5));
	color = lerp(color, lerp(FoamColor, SkyNight * 1.8, night), step(abs(under), 0.5) * 0.55);

	// THE MARKS. a row of MARK_COUNT little marks under the moon, one for every cycle that it takes for night
	// to fall. a mark is an empty outline. its left half fills in when the water has gone all the way
	// out, and the rest of it when the water is all the way back in. at night they are all full, and
	// they empty again as the moves of the night are used up. see THE MARKS in fish_headers_tide.
	float3 markColor = lerp(MoonRay, MoonColor, 0.5);
	for (int m = 0; m < MARK_COUNT; m++)
	{
		float2 markAt = moonAt + float2((m - (MARK_COUNT - 1) * 0.5) * 9.0, MOON_SIZE + 10.0);
		float2 off = px - markAt;
		float size = abs(off.x) + abs(off.y);
		float inside = step(size, 3.0);
		float edge = inside * step(2.5, size);
		float filled = saturate(Tide.w * 0.5 - m);
		float full = step(0.75, filled) + step(0.25, filled) * (1.0 - step(0.75, filled)) * step(off.x, 0.0);
		color = lerp(color, markColor, saturate(edge * 0.45 + inside * full * 0.95) * step(0.0, Tide.w));
	}

	// the specks that drift down through all of this are drawn by SpecksEffect.fx, on a sprite of their
	// own, so that they can be in front of the coral. they go in here, behind the near ridge.

	// the near ridge is the sand that the board is over
	float nearX = px.x + floor(sway) + 700.0;
	float nearTop = ground(nearX, NearBase, NearRough, 37.0);
	float3 nearFog = lerp(sea, rock, 0.8);
	float nearRock = step(nearTop, px.y);
	float grit = step(0.86, hash(float2(nearX, px.y))) * 0.02;
	float3 nearColor = nearFog + 0.05 * step(px.y, nearTop + 0.5) + grit;
	color = lerp(color, nearColor, nearRock);

	// when the ancient one comes, all of that fades away, and the stars are there in place of it
	color = lerp(color, stars(px), saturate(Void));

	return float4(color, 1.0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
