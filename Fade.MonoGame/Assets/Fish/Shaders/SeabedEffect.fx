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

// the water, at the top of the screen and at the bottom of it
#define WaterTop    float3(0.090, 0.290, 0.410)
#define WaterLow    float3(0.055, 0.115, 0.250)

#define RayColor    float3(0.450, 0.780, 0.800)

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
	color += RayColor * ray * 0.2;

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
