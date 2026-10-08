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
	color += SunGlow * glow * glow;

	// long thin clouds, drifting. the bottom edge of a cloud is another color: in the evening the sun
	// lights it from underneath, and in the day that is where its shadow is.
	float2 cp = float2(px.x * 0.5 + Time * 2.0, (px.y - Horizon) * 2.4);
	float cloud = sin(cp.x * 0.040 + 1.7 * sin(cp.y * 0.031)) + 0.6 * sin(cp.x * 0.093 + cp.y * 0.11 + 2.0);
	float cloudBand = saturate(1.0 - abs((Horizon - px.y) - 78.0) / 46.0);
	float cloudHere = step(1.62 - cloudBand * 0.95, cloud);
	float cloudUnder = step(1.62 - cloudBand * 0.95, sin((cp.x) * 0.040 + 1.7 * sin((cp.y + 5.0) * 0.031)) + 0.6 * sin(cp.x * 0.093 + (cp.y + 5.0) * 0.11 + 2.0));
	float3 cloudColor = lerp(CloudEdge, CloudColor, cloudUnder);
	color = lerp(color, cloudColor, cloudHere * 0.85);

	float sun = step(sunDist, SunRadius);
	color = lerp(color, SunColor, sun);

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

	// the camera is right at the surface, so the water in front of it rolls by as one dark swell
	// across the bottom of the screen, with a pale line of foam along the top of it
	float crest = Swell + sin(px.x * 0.018 + Time * 0.9) * 6.0 + sin(px.x * 0.047 - Time * 1.3) * 2.5;
	float under = px.y - crest;
	float3 swell = lerp(SwellColor, SwellDeep, saturate(under / 40.0));
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
