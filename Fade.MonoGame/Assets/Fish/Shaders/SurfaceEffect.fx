#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the view from the surface, behind the main menu: the sun, the sky, and the sea out to the horizon.
// it is drawn onto one plain sprite that covers the whole screen, so there is no texture.
// the boat is a sprite of its own, on top of this. this only draws the reflection of it.

// the game sets all of these on every frame. a parameter that the game never sets does not keep
// a value that it is given here, so the things that never change are #defines instead.
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Horizon;   // how far down the screen the horizon is, in pixels
float Swell;     // how far down the screen the top of the water right in front of the camera is
float3 Boat;     // the middle of the boat's hull, its waterline, and half of how wide it is

float Day;       // which colors to use. 0 is the sun going down, and 1 is the middle of the day.

// what the main menu remembers of the hidden game. see THE MAIN MENU REMEMBERS in fish_headers.
// x is not used any more. the reefs of the main menu are drawn by ReefEffect.fx, on sprites of their own.
// y is 1 once the ancient one has been summoned, and then it is out on the horizon, coming toward the boat.
float2 Endings;
float Coming;    // how far it has come, from 0 (where it sets out from) to 1 (which it never gets to)

// every color has two versions, one for each time of day. the first is for the evening.
#define PICK(evening, day) lerp(evening, day, Day)

// in the evening the sun is big and sits on the horizon. in the day it is small and high up.
#define SunX 112.0
#define SunHeight   PICK(10.0, 122.0)
#define SunRadius   PICK(27.0, 13.0)
#define SunGlow     PICK(float3(0.50, 0.24, 0.06), float3(0.30, 0.30, 0.22))

#define SkyTop      PICK(float3(0.086, 0.110, 0.251), float3(0.090, 0.280, 0.640))
#define SkyHigh     PICK(float3(0.275, 0.196, 0.431), float3(0.180, 0.450, 0.800))
#define SkyLow      PICK(float3(0.745, 0.314, 0.412), float3(0.400, 0.670, 0.920))
#define SkyHorizon  PICK(float3(1.000, 0.612, 0.353), float3(0.760, 0.890, 0.980))
#define SpaceColor  float3(0.016, 0.016, 0.047)
#define HazeColor   float3(0.110, 0.060, 0.190)
#define StarColor   float3(1.000, 0.960, 0.860)
#define SunColor    PICK(float3(1.000, 0.910, 0.620), float3(1.000, 1.000, 0.900))
#define CloudColor  PICK(float3(0.380, 0.200, 0.380), float3(1.000, 1.000, 1.000))
#define CloudEdge   PICK(float3(1.000, 0.560, 0.400), float3(0.700, 0.790, 0.900))
#define SeaFar      PICK(float3(0.560, 0.290, 0.400), float3(0.420, 0.700, 0.880))
#define SeaMid      PICK(float3(0.130, 0.150, 0.330), float3(0.090, 0.400, 0.680))
#define SeaNear     PICK(float3(0.063, 0.086, 0.204), float3(0.040, 0.230, 0.500))
#define SeaGlint    PICK(float3(1.000, 0.740, 0.470), float3(1.000, 1.000, 1.000))
#define SwellColor  PICK(float3(0.039, 0.063, 0.170), float3(0.030, 0.180, 0.400))
#define SwellDeep   PICK(float3(0.024, 0.035, 0.110), float3(0.020, 0.110, 0.290))
#define FoamColor   PICK(float3(0.560, 0.700, 1.000), float3(0.880, 0.960, 1.000))
#define HullColor   PICK(float3(0.055, 0.063, 0.133), float3(0.740, 0.840, 0.920))

// THE THING ON THE HORIZON. where it sets out from and where it is headed for, across the screen in
// pixels, and how many times as big as its picture it is at each. the boat is at about 450, and the
// menu is in the middle of the screen, so it keeps to the left of that, where the sun is.
#define THING_FROM 26.0
#define THING_TO 122.0
#define THING_SMALL 0.45
#define THING_BIG 1.0
#define ThingColor  PICK(float3(0.016, 0.008, 0.030), float3(0.014, 0.020, 0.040))

// THE DARK BEHIND IT. there is a hole in the sky behind the thing, and it stands in the mouth of it. what
// is in the hole is another sky: deep blue and violet, with a swirl of stars and dust in it that turns.
// it is not black. the thing is, so it is darker than the night that it came out of.
// the hole has no edge. it is torn, and what is in it leaks out into the sky around it in wisps, which
// drift. the hole grows as the thing comes.
//   RIFT_SIZE    about how far it is from the middle of the hole to where it gives out, in the pixels of the thing's picture
//   RIFT_UP      how far above the sea the middle of the hole is, the same way
//   RIFT_TORN    how ragged the edge of it is. 0 is a clean circle, and 1 is torn to shreds.
//   RIFT_WISP    how big the wisps are, in pixels
//   RIFT_DRIFT   how fast the wisps drift
// while the thing is there, the sky is darker toward the side that it is on. and in the day, the sky
// is not blue. it is a dark, sick green, the sea is green under it, and the sun is a hole like the one
// behind the thing, with the same other sky in it.
//   SICK_SKY     how much of the color of the sky in the day is the sick green, from 0 to 1
//   SICK_LEFT    how much darker the left edge of the sky is than the right, from 0 to 1
#define SICK_SKY 0.92
#define SICK_LEFT 0.62
#define SickHigh    float3(0.020, 0.070, 0.040)
#define SickLow     float3(0.210, 0.300, 0.110)
#define SickCloud   float3(0.085, 0.150, 0.075)
#define RIFT_SIZE 62.0
#define RIFT_UP 20.0
#define RIFT_TORN 0.8
#define RIFT_WISP 17.0
#define RIFT_DRIFT 2.6
#define RIFT_LIT 0.10 // how wide the lit edge of the hole is. more is wider.
#define RiftSpace   float3(0.070, 0.045, 0.180)
#define RiftWarm    float3(0.420, 0.100, 0.440)
#define RiftCold    float3(0.080, 0.240, 0.520)
#define RiftRing    PICK(float3(1.000, 0.560, 0.300), float3(0.860, 0.700, 1.000))
#define ThingEye    PICK(float3(1.000, 0.780, 0.300), float3(0.950, 0.900, 0.450))

// a number from 0 to 1 that is different for every pixel. the usual one-liner that is built on sin()
// leaves streaks and rows that can be seen in a field of stars, and this one does not.
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

// a smooth line of bumps
float bumps1(float x, float seed)
{
	float i = floor(x);
	float f = frac(x);
	f = f * f * (3.0 - 2.0 * f);
	return lerp(hash(float2(i, seed)), hash(float2(i + 1.0, seed)), f);
}

// the thing on the horizon. rgb is its color on this pixel, and a is 1 if it is on this pixel.
// it is a long way off, so it is only a dark shape, the color of the haze, with two eyes. the head
// of it is a dome that stands out of the sea, and tentacles stand out of the sea around it and wave.
// it gets bigger as it comes.
float4 thing(float2 px)
{
	float big = lerp(THING_SMALL, THING_BIG, Coming);
	float2 at = float2(lerp(THING_FROM, THING_TO, Coming), Horizon + 3.0 + sin(Time * 0.45) * 1.5);
	float2 p = (px - at) / big;

	// the head, which is not quite round. it has a brow.
	float head = step(length(p * float2(1.0 / 34.0, 1.0 / 29.0)), 1.0);
	head = max(head, step(length((p - float2(-9.0, -19.0)) * float2(1.0 / 18.0, 1.0 / 15.0)), 1.0));

	// the tentacles. up is how far above the sea this pixel is, in the pixels of its picture.
	float up = (Horizon - px.y) / big;
	float arms = 0.0;
	float2 arm = float2(-66.0, 46.0);
	float sway = sin(up * 0.085 + Time * 0.8 + 1.0) * 6.0 * saturate(up / arm.y);
	arms = max(arms, step(abs(p.x - arm.x - sway), lerp(4.6, 0.8, saturate(up / arm.y))) * step(up, arm.y));
	arm = float2(-47.0, 30.0);
	sway = sin(up * 0.110 - Time * 0.7 + 2.3) * 5.0 * saturate(up / arm.y);
	arms = max(arms, step(abs(p.x - arm.x - sway), lerp(3.8, 0.8, saturate(up / arm.y))) * step(up, arm.y));
	arm = float2(46.0, 36.0);
	sway = sin(up * 0.095 + Time * 0.9 + 4.1) * 5.5 * saturate(up / arm.y);
	arms = max(arms, step(abs(p.x - arm.x - sway), lerp(4.0, 0.8, saturate(up / arm.y))) * step(up, arm.y));
	arm = float2(64.0, 54.0);
	sway = sin(up * 0.075 - Time * 0.6 + 0.4) * 7.0 * saturate(up / arm.y);
	arms = max(arms, step(abs(p.x - arm.x - sway), lerp(5.0, 0.8, saturate(up / arm.y))) * step(up, arm.y));
	arm = float2(88.0, 26.0);
	sway = sin(up * 0.120 + Time * 1.0 + 5.2) * 4.0 * saturate(up / arm.y);
	arms = max(arms, step(abs(p.x - arm.x - sway), lerp(3.4, 0.8, saturate(up / arm.y))) * step(up, arm.y));

	// the hole in the sky behind it. see THE DARK BEHIND IT, further up.
	float2 q = p - float2(0.0, -RIFT_UP);
	float out0 = length(q * float2(1.0, 1.12));
	float around = atan2(q.y, q.x);
	float spiral = 0.5 + 0.5 * cos(around * 2.0 - log(max(out0, 1.0)) * 4.5 + Time * 0.35);
	spiral = spiral * spiral * spiral;
	float dust = 0.5 + 0.5 * sin(q.x * 0.13 + sin(q.y * 0.17 + Time * 0.2) * 2.0);
	float3 hole = RiftSpace + lerp(RiftWarm, RiftCold, dust) * bands(spiral * saturate(1.15 - out0 / RIFT_SIZE), 5.0, px);

	// everything that is looked up by where a pixel is, is looked up by where it is from the horizon,
	// and not by where it is on the screen. the horizon goes up the screen when the camera goes under,
	// and the stars and the torn edge have to go with it, or they would crawl over the hole.
	float2 skyPx = float2(px.x, px.y - floor(Horizon));
	hole += step(0.972 - 0.02 * spiral, hash(skyPx + 17.0)) * (0.4 + 0.6 * hash(skyPx + 3.0)) * (0.7 + 0.3 * sin(Time * 2.0 + hash(skyPx + 7.0) * 40.0));

	// how much of the hole there is on this pixel. it is whole in the middle, and it gives out toward
	// the edge, sooner in some places than in others, so the edge is torn. the wisps of it that are
	// past the edge drift outward, and up.
	float2 wispAt = skyPx - q * 0.25 + float2(Time * RIFT_DRIFT * 0.6, Time * RIFT_DRIFT);
	float torn = clouds(wispAt, RIFT_WISP, 23.0) * 0.6 + clouds(wispAt * 1.9, RIFT_WISP, 47.0) * 0.4;
	float much = (1.25 - out0 / RIFT_SIZE) * 1.6 + (torn - 0.5) * 2.4 * RIFT_TORN;

	// the edge of it is sharp: a pixel is in the hole, or it is not. the very edge is lit, where it is tearing.
	float inHole = step(0.5, much);
	hole = lerp(hole, RiftRing, step(much, 0.5 + RIFT_LIT) * 0.7);
	float3 dark = hole;

	// only what is above the sea shows
	float above = step(px.y, Horizon);
	float there = max(head, arms) * above;

	// the thing itself is as dark as the hole. it is only seen because of what is behind it.
	float3 color = ThingColor;

	// its eyes, which are narrow, and which it shuts now and then
	float open = step(0.07, frac(Time * 0.13));
	float eyes = step(length((p - float2(-13.0, -11.0)) * float2(1.0 / 5.0, 1.0 / 1.6)), 1.0)
	           + step(length((p - float2(10.0, -10.0)) * float2(1.0 / 5.0, 1.0 / 1.6)), 1.0);
	color = lerp(color, ThingEye, saturate(eyes) * open * head);

	// the hole, and then the thing, standing in the mouth of it
	float4 seen = float4(dark, inHole * above);
	seen = float4(lerp(seen.rgb, color, there), max(seen.a, there));
	return seen;
}

float3 sky(float2 px)
{
	// 0 at the horizon, 1 high up
	float h = saturate((Horizon - px.y) / 170.0);
	h = bands(h, 14.0, px);

	float3 color = lerp(SkyHorizon, SkyLow, smoothstep(0.0, 0.28, h));
	color = lerp(color, SkyHigh, smoothstep(0.2, 0.62, h));
	color = lerp(color, SkyTop, smoothstep(0.55, 1.0, h));

	// where this pixel is in the sky, measured up from the horizon. the camera can look a long way up,
	// and the stars have to stay where they are in the sky when it does.
	float2 wp = float2(px.x, floor(Horizon) - px.y);

	// far above the sunset, the sky goes black. that is where the splash screen is.
	float space = smoothstep(190.0, 560.0, wp.y);
	color = lerp(color, SpaceColor, space);

	// a band of faint haze runs across the stars at a slant, and the stars are thicker inside of it
	float across = (wp.x * 0.45 + wp.y * 0.9 - 760.0) / 95.0;
	float haze = exp(-across * across) * space;
	float clumps = 0.6 + 0.4 * sin(wp.x * 0.043 - wp.y * 0.021) * sin(wp.x * 0.017 + wp.y * 0.038 + 1.0);
	color += HazeColor * floor(haze * clumps * 6.0) / 6.0;

	// a few stars, high up where it is dark, and more of them in space. most of them are faint, and
	// they twinkle.
	float star = step(1.0 - (0.0014 + space * 0.0022 + haze * 0.0030), hash(wp));
	float bright = hash(wp + 31.0);
	bright = 0.3 + 0.7 * bright * bright;
	float twinkle = 0.7 + 0.3 * sin(Time * 2.0 + hash(wp + 7.0) * 40.0);
	color += star * twinkle * bright * max(smoothstep(0.6, 0.95, h) * (1.0 - Day), space);

	// and some bigger ones that are a little cross of pixels. there is at most one in every square of sky.
	float2 cell = floor(wp / 26.0);
	float2 spot = floor(float2(hash(cell + 3.0), hash(cell + 11.0)) * 22.0) + 2.0;
	float2 away = abs(wp - cell * 26.0 - spot);
	float bigStar = step(away.x + away.y, 1.0) * step(0.8, hash(cell)) * space;
	float bigTwinkle = 0.65 + 0.35 * sin(Time * 1.3 + hash(cell + 5.0) * 40.0);
	color = lerp(color, StarColor, bigStar * bigTwinkle);

	// the sun, with a glow around it
	float2 sunPos = float2(SunX, Horizon - SunHeight);
	float sunDist = length(px - sunPos);
	float glow = bands(saturate(1.0 - sunDist / 150.0), 8.0, px);

	// in the day, with the thing there, the sky is a dark, sick green. see THE DARK BEHIND IT, further up.
	float sick = Endings.y * Day;
	color = lerp(color, lerp(SickLow, SickHigh, smoothstep(0.0, 0.75, h)), sick * SICK_SKY);
	color += SunGlow * glow * glow * (1.0 - 0.75 * sick);

	// long thin clouds, drifting. the bottom edge of a cloud is another color: in the evening the sun
	// lights it from underneath, and in the day that is where its shadow is.
	float2 cp = float2(px.x * 0.5 + Time * 2.0, (px.y - Horizon) * 2.4);
	float cloud = sin(cp.x * 0.040 + 1.7 * sin(cp.y * 0.031)) + 0.6 * sin(cp.x * 0.093 + cp.y * 0.11 + 2.0);
	float cloudBand = saturate(1.0 - abs((Horizon - px.y) - 78.0) / 46.0);
	float cloudHere = step(1.62 - cloudBand * 0.95, cloud);
	float cloudUnder = step(1.62 - cloudBand * 0.95, sin((cp.x) * 0.040 + 1.7 * sin((cp.y + 5.0) * 0.031)) + 0.6 * sin(cp.x * 0.093 + (cp.y + 5.0) * 0.11 + 2.0));
	float3 cloudColor = lerp(lerp(CloudEdge, CloudColor, cloudUnder), SickCloud * (0.8 + 0.4 * cloudUnder), sick);
	color = lerp(color, cloudColor, cloudHere * 0.85);

	float sun = step(sunDist, SunRadius);

	// and then the sun is a hole, with the other sky in it: a swirl of dust that turns, a few stars,
	// and a lit edge
	float2 inSun = px - sunPos;
	float turn = 0.5 + 0.5 * cos(atan2(inSun.y, inSun.x) * 2.0 - log(max(sunDist, 1.0)) * 4.5 + Time * 0.5);
	float3 sunHole = RiftSpace + lerp(RiftWarm, RiftCold, 0.5 + 0.5 * sin(inSun.x * 0.4)) * bands(turn * turn, 4.0, px);
	sunHole += step(0.93, hash(float2(px.x, px.y - floor(Horizon)) + 5.0)) * 0.8;
	sunHole = lerp(sunHole, RiftRing, step(SunRadius - 1.5, sunDist));
	color = lerp(color, lerp(SunColor, sunHole, sick), sun);

	// while the thing is there, the sky is darker toward the side that it is on
	float left = saturate(1.0 - px.x / (Size.x * 0.7));
	color *= 1.0 - SICK_LEFT * Endings.y * left * sqrt(left);

	return color;
}

float3 sea(float2 px)
{
	float dy = px.y - Horizon;

	// 0 at the horizon, 1 at the bottom of the screen
	float depth = saturate(dy / 170.0);
	float shade = bands(depth, 12.0, px);

	float3 color = lerp(SeaFar, SeaMid, smoothstep(0.0, 0.22, shade));
	color = lerp(color, SeaNear, smoothstep(0.15, 0.8, shade));

	// under a sick sky, the sea is green. see THE DARK BEHIND IT, further up.
	color *= lerp(float3(1.0, 1.0, 1.0), float3(0.42, 0.80, 0.44), Endings.y * Day * SICK_SKY);


	// the ripples are rows of short dashes. they are small and close together at the horizon,
	// and get longer and further apart toward the camera.
	float rowScale = 0.25 + depth * 2.2;
	float wx = (px.x - Size.x * 0.5) / rowScale;
	float row = 13.0 * log(1.0 + dy * 0.16);
	float ripple = sin(wx * 0.10 + 2.4 * sin(row * 1.3 + Time * 0.7)) * sin(row * 2.3 - Time * 1.0);

	// the sun lays a path of light across the water, which gets wider toward the camera
	float path = saturate(1.0 - abs(px.x - SunX) / (10.0 + dy * 0.55));
	float glint = step(0.82 - path * 0.75, ripple);
	float3 glintColor = lerp(color + 0.07, SeaGlint, saturate(path * 1.6) * (1.0 - depth * 0.45));
	color = lerp(color, glintColor, glint);

	// and the troughs between the ripples are a little darker
	color *= 1.0 - 0.14 * step(ripple, -0.8);

	// the reflection of the boat. it wobbles, and breaks up further down.
	float by = px.y - Boat.y;
	float reach = by / 38.0;
	float wobble = sin(by * 0.8 + Time * 2.2) * 2.5 + sin(by * 0.37 - Time * 1.3) * 2.0;
	float inside = step(abs(px.x - Boat.x + wobble * saturate(reach * 3.0)), Boat.z * (1.0 - reach * 0.45));
	float broken = step(reach * 2.0 - 1.0, sin(by * 1.9 - Time * 1.6));
	float reflection = inside * broken * step(0.0, by) * step(reach, 1.0);
	color = lerp(color, HullColor, reflection * 0.8);

	return color;
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	// whole pixels, so that everything is as blocky as the rest of the game
	float2 px = floor(uv * Size);

	float3 color = lerp(sky(px), sea(px), step(Horizon, px.y));

	// the thing on the horizon, once the ancient one has been summoned. the sea under it is darker.
	float4 seen = thing(px);
	color = lerp(color, seen.rgb, seen.a * Endings.y);
	float2 underIt = float2(px.x - lerp(THING_FROM, THING_TO, Coming), px.y - Horizon) / lerp(THING_SMALL, THING_BIG, Coming);
	float shadow = step(abs(underIt.x), 40.0 - underIt.y * 1.3) * step(0.0, underIt.y) * step(underIt.y, 22.0);
	color *= 1.0 - 0.30 * shadow * step(0.0, sin(underIt.y * 1.7 - Time * 1.4)) * Endings.y;

	// the camera is right at the surface, so the water in front of it rolls by as one dark swell
	// across the bottom of the screen, with a pale line of foam along the top of it
	float crest = Swell + sin(px.x * 0.018 + Time * 0.9) * 6.0 + sin(px.x * 0.047 - Time * 1.3) * 2.5;
	float under = px.y - crest;
	float3 swell = lerp(SwellColor, SwellDeep, saturate(under / 40.0));

	// under a sick sky, this water is green too. see THE DARK BEHIND IT, further up.
	swell *= lerp(float3(1.0, 1.0, 1.0), float3(0.42, 0.80, 0.44), Endings.y * Day * SICK_SKY);

	swell = lerp(swell, FoamColor, step(under, 1.5) * 0.9);
	float fleck = step(0.8, sin(px.x * 0.21 + Time * 1.7 + under * 0.9)) * step(3.0, under) * step(under, 8.0);
	swell = lerp(swell, FoamColor, fleck * 0.35);
	color = lerp(color, swell, step(0.0, under));

	return float4(color, 1.0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
