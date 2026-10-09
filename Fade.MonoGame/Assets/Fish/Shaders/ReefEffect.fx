#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// a reef, in the sea of the main menu. see THE MAIN MENU REMEMBERS in fish_headers.
// there are two of them, and each is a sprite of its own that covers the whole screen, with a copy of
// this effect of its own, because one effect does not have room for both. each is told where it is.
// they are drawn on top of the sea that SurfaceEffect.fx draws. the part of a reef that is under the
// water has to be the color of that sea, with the same ripples over it, so the few lines that work
// those out are here too, and they have to match the ones there.

// the game sets all of these on every frame. a parameter that the game never sets does not keep
// a value that it is given here, so the things that never change are #defines instead.
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Horizon;   // how far down the screen the horizon is, in pixels
float Swell;     // how far down the screen the top of the water right in front of the camera is
float Day;       // which colors to use. 0 is the sun going down, and 1 is the middle of the day.
float4 Where;    // x is how far across the screen the middle of the reef is, and y is how far down from the horizon its waterline is, in pixels. z is how many pixels one unit of its shape is, which is how close it is. w is -1 to draw it the other way around, and 1 not to.
float Seed;      // a number of the reef's own, so that no two reefs have their lumps in the same places
float Fade;      // how much of the reef there is, from 0 to 1. it fades away as the camera goes under at the start of a game.
float Sick;      // 1 when the sea is green, which it is in the day once the ancient one has been summoned. see SurfaceEffect.fx.

// every color has two versions, one for each time of day. the first is for the evening.
#define PICK(evening, day) lerp(evening, day, Day)

// these are the same as in SurfaceEffect.fx
#define SunX 112.0
#define SeaFar      PICK(float3(0.560, 0.290, 0.400), float3(0.420, 0.700, 0.880))
#define SeaMid      PICK(float3(0.130, 0.150, 0.330), float3(0.090, 0.400, 0.680))
#define SeaNear     PICK(float3(0.063, 0.086, 0.204), float3(0.040, 0.230, 0.500))
#define SeaGlint    PICK(float3(1.000, 0.740, 0.470), float3(1.000, 1.000, 1.000))
#define FoamColor   PICK(float3(0.560, 0.700, 1.000), float3(0.880, 0.960, 1.000))
#define SICK_SKY 0.92

// THE REEF. it is a long, wide bank of rock, most of it under the water, with a
// few knuckles of it standing out. it is a real shape, and not a flat picture of one: the effect works
// out where a line of sight from every pixel first touches it, and lights it from which way it faces
// there. what is under the water is seen through the water, so it is the color of the water as much as
// it is its own, and more so the deeper it goes.
// things grow all over it: kelp, sea fans, branching coral, tube sponges and brain coral. those are flat
// pictures, stood up on the rock where they grow. see reefGrowth.
//   REEF_PITCH   how far down the camera looks at it, in radians. more shows more of the top of it, and of what is under the water.
//   REEF_BODY    how big the bank is, from its middle: across, down, and back, in units
//   REEF_SUNK    how far under the water the middle of the bank is, in units. less lifts more of it out of the water.
//   REEF_LUMPS   how lumpy the top of the bank is
//   REEF_MURK    how quickly it is lost in the water, under the surface. more hides more of it.
//   REEF_KNOBS   how knobbly the rock is
#define REEF_PITCH 0.52
#define REEF_BODY float3(3.25, 1.20, 1.95)
#define REEF_SUNK 0.98
#define REEF_LUMPS 0.21
#define REEF_MURK 0.55
#define REEF_KNOBS 0.035
#define ReefDark    PICK(float3(0.110, 0.070, 0.210), float3(0.120, 0.150, 0.290))
#define ReefLit     PICK(float3(0.720, 0.470, 0.520), float3(0.640, 0.700, 0.760))
#define KelpColor   PICK(float3(0.300, 0.420, 0.250), float3(0.250, 0.620, 0.330))

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

// join two shapes, so that they run together where they meet
float blend(float a, float b)
{
	float h = saturate(0.5 + 0.5 * (b - a) / 0.30);
	return lerp(b, a, h) - 0.30 * h * (1.0 - h);
}

// how far a place is from the rock of the reef. it is less than 0 inside of the rock. the reef is a
// long bank, with a lumpy top, and two knuckles that stand up out of it, and the rock is knobbly all over.
// `seed` is a number of the reef's own, so that no two reefs have their lumps in the same places.
float reefRock(float3 p, float seed)
{
	float d = length((p - float3(0.0, -REEF_SUNK, 0.30)) / REEF_BODY) - 1.0;
	d += sin(p.x * 2.3 + 0.7 + seed) * sin(p.z * 2.1 + 1.1 + seed * 0.7) * REEF_LUMPS + sin(p.x * 4.9) * sin(p.z * 4.3 + p.y * 3.0) * 0.07;
	d = blend(d, length(p - float3(-0.95, 0.06, 0.10)) - 0.52);
	d = blend(d, length(p - float3(1.30, -0.02, 0.30)) - 0.46);
	return d + sin(p.x * 9.0) * sin(p.y * 9.0 + 1.0) * sin(p.z * 9.0 + 2.0) * REEF_KNOBS;
}

// how high the top of the bank is, at a place on it: across, and back. the things that grow on it
// stand on this. it leaves out the knuckles and the knobs, so nothing grows on the knuckles.
float reefTop(float2 xz, float seed)
{
	float2 q = float2(xz.x / REEF_BODY.x, (xz.y - 0.30) / REEF_BODY.z);
	return REEF_BODY.y * sqrt(max(1.0 - dot(q, q), 0.0)) - REEF_SUNK - sin(xz.x * 2.3 + 0.7 + seed) * sin(xz.y * 2.1 + 1.1 + seed * 0.7) * REEF_LUMPS * 0.9;
}

// where on the screen a place on a reef is, in pixels. `where` is where the reef is, and `scale` is how big.
float2 reefSeen(float3 p, float2 where, float scale)
{
	return float2(where.x + p.x * scale, Horizon + where.y - (p.y * cos(REEF_PITCH) + p.z * sin(REEF_PITCH)) * scale);
}

// one of the bright colors of coral
float3 reefBright(float pick)
{
	float3 bright = float3(0.960, 0.470, 0.450);
	bright = lerp(bright, float3(0.980, 0.640, 0.300), step(0.25, pick));
	bright = lerp(bright, float3(0.640, 0.470, 0.920), step(0.50, pick));
	bright = lerp(bright, float3(0.320, 0.800, 0.720), step(0.75, pick));
	return bright;
}

// THE THINGS THAT GROW ON THE REEF. each of these is a flat picture of one thing, stood up on the rock.
// `d` is where this pixel is from the foot of it, in pixels: across, and up. rgb is its color on this
// pixel, and a is 1 if it is on this pixel. `seed` is a number of its own.

// kelp: three long blades, which bend in the water all the way up
float4 kelp(float2 d, float seed)
{
	float bend = sin(Time * 1.2 + seed + d.y * 0.22) * d.y * 0.16;
	float blade = step(abs(d.x - bend), 0.9) * step(d.y, 17.0);
	blade = max(blade, step(abs(d.x + 3.0 - bend * 0.8), 0.9) * step(d.y, 12.0));
	blade = max(blade, step(abs(d.x - 3.0 - bend * 1.2), 0.9) * step(d.y, 14.0));
	return float4(KelpColor * (0.75 + 0.25 * step(0.5, frac(d.y * 0.25))), blade * step(0.0, d.y));
}

// a sea fan: half of a round, flat sheet, with ribs that spread out from the foot of it
float4 seaFan(float2 d, float seed)
{
	float out0 = length(d);
	float rib = step(0.45, frac(atan2(d.x, d.y) * 3.2 + seed));
	float3 color = reefBright(frac(seed * 0.37)) * (0.55 + 0.45 * rib);
	color *= 1.0 - 0.3 * step(9.0, out0);
	return float4(color, step(out0, 10.5) * step(0.0, d.y));
}

// branching coral: three branches that fan out, with a knob on the end of each
float4 branches(float2 d, float seed)
{
	float lean = sin(Time * 1.1 + seed) * 0.5;
	float2 tipA = float2(-4.5 + lean, 6.0);
	float2 tipB = float2(0.5 + lean, 8.5);
	float2 tipC = float2(5.0 + lean, 6.5);
	float stem = step(distance(d, tipA * saturate(dot(d, tipA) / dot(tipA, tipA))), 0.8);
	stem = max(stem, step(distance(d, tipB * saturate(dot(d, tipB) / dot(tipB, tipB))), 0.8));
	stem = max(stem, step(distance(d, tipC * saturate(dot(d, tipC) / dot(tipC, tipC))), 0.8));
	float knob = max(step(distance(d, tipA), 1.5), max(step(distance(d, tipB), 1.5), step(distance(d, tipC), 1.5)));
	float3 bright = reefBright(frac(seed * 0.61));
	return float4(lerp(bright * 0.65, lerp(bright, float3(1.0, 1.0, 1.0), 0.3), knob), max(stem, knob));
}

// tube sponges: three short fat tubes, each with a dark mouth at the top
float4 tubes(float2 d, float seed)
{
	float tall = 7.0 + 2.0 * frac(seed * 0.83);
	float tube = step(abs(d.x), 1.6) * step(d.y, tall);
	tube = max(tube, step(abs(d.x + 4.0), 1.6) * step(d.y, tall - 2.5));
	tube = max(tube, step(abs(d.x - 4.0), 1.6) * step(d.y, tall - 1.5));
	float mouth = step(abs(d.x), 0.8) * step(tall - 1.6, d.y) + step(abs(d.x + 4.0), 0.8) * step(tall - 4.1, d.y) + step(abs(d.x - 4.0), 0.8) * step(tall - 3.1, d.y);
	float3 color = lerp(float3(0.960, 0.800, 0.380), float3(0.800, 0.540, 0.920), step(0.5, frac(seed * 0.29)));
	color *= 0.80 + 0.20 * step(d.x * 0.5 + 0.2, 0.0);
	return float4(lerp(color, color * 0.25, saturate(mouth)), tube * step(0.0, d.y));
}

// brain coral: a dome, with winding grooves in it
float4 brain(float2 d, float seed)
{
	float2 fromMiddle = d - float2(0.0, 2.5);
	float out0 = length(fromMiddle * float2(1.0, 1.35));
	float groove = step(0.0, sin(d.x * 1.7 + sin(d.y * 1.5 + seed) * 1.7 + seed));
	float3 color = lerp(float3(0.800, 0.660, 0.440), float3(0.820, 0.540, 0.600), step(0.5, frac(seed * 0.47)));
	color *= (0.62 + 0.30 * groove) * (1.0 + 0.25 * (fromMiddle.y - fromMiddle.x) / 6.0);
	return float4(color, step(out0, 6.0) * step(0.0, d.y));
}

// put one thing that grows on the reef on top of what is there already. `foot` is where it stands on
// the reef, across and back. `thing` is the picture of it, and `under` is what is there already: rgb is
// the color, and a is 0 for nothing, 1 for something that is under the water, and 2 for something that is not.
// a thing that stands under the water is the color of the water too, and more so the deeper it stands.
float4 reefGrowth(float4 under, float4 thing, float2 foot, float3 water, float seed)
{
	float deep = max(0.0 - reefTop(foot, seed), 0.0);
	float3 color = lerp(thing.rgb, water, saturate(0.10 + deep * REEF_MURK * 0.9));
	float out0 = step(deep, 0.001);
	return float4(lerp(under.rgb, color, thing.a), lerp(under.a, 1.0 + out0, thing.a));
}

// a reef. rgb is its color on this pixel. a is 0 where there is none of it, 1 where it is under the
// water, and 2 where it stands out of the water. `water` is the color of the sea on this pixel.
// `where` is where the middle of the reef is: how far across the screen, and how far down from the
// horizon its waterline is. `scale` is how many pixels one unit of its shape is, which is how close
// it is. `flip` is -1 to draw it the other way around, and 1 not to. `seed` is a number of its own.
float4 reef(float2 screen, float3 water, float2 where, float scale, float flip, float seed)
{
	// a reef that is the other way around is drawn as if the screen was, around the middle of the reef
	float2 px = float2(where.x + (screen.x - where.x) * flip, screen.y);

	// where this pixel is, in the units of the reef, across and up from the middle of its waterline
	float2 s = float2(px.x - where.x, (Horizon + where.y) - px.y) / scale;

	// the line of sight from this pixel. the camera looks at the reef from a little above, and every
	// line of sight goes the same way, so the reef is the same shape wherever it is on the screen.
	float tilt = sin(REEF_PITCH);
	float level = cos(REEF_PITCH);
	float3 way = float3(0.0, -tilt, level);
	float3 from = float3(s.x, s.y * level, s.y * tilt) - way * 4.5;

	// walk along it until it touches the rock. each step is as far as the rock is known to be away.
	float gone = 0.0;
	[loop] for (int i = 0; i < 24; i++)
	{
		gone += reefRock(from + way * gone, seed) * 0.85;
	}
	float3 at = from + way * gone;
	float touched = step(reefRock(at, seed), 0.05) * step(gone, 9.5);

	// which way the rock faces there
	float3 slope = float3(0.0, 0.0, 0.0);
	[loop] for (int k = 0; k < 3; k++)
	{
		float3 axis = float3(step(abs(k - 0.0), 0.5), step(abs(k - 1.0), 0.5), step(abs(k - 2.0), 0.5));
		slope += axis * (reefRock(at + axis * 0.03, seed) - reefRock(at - axis * 0.03, seed));
	}
	float3 facing = normalize(slope + float3(0.0, 0.0001, 0.0));

	// it is lit from the top left, and from in front, in flat steps. the parts that face down are in the dark.
	float lit = saturate(dot(facing, normalize(float3(-0.50, 0.72, -0.48))));
	lit = bands(saturate(lit * 0.85 + 0.15 * saturate(facing.y) + 0.06), 5.0, px);
	float3 color = lerp(ReefDark, ReefLit, lit);

	// coral grows on the rock in patches, wherever it does not face down. the patches are round and
	// uneven, like lichen on a stone, and which of four colors a patch is drifts across the reef.
	// the patches are speckled, with the mouths of the coral.
	float spread = sin(at.x * 6.3 + 1.3) * sin(at.z * 5.1 + 0.7) + 0.6 * sin(at.y * 7.7 + at.x * 2.1) + 0.35 * sin(at.x * 13.0 + at.z * 11.0);
	float covered = step(0.12, spread) * step(-0.15, facing.y);
	float mouths = 1.0 - 0.30 * step(0.72, hash(floor(px / 2.0) + 9.0));
	color = lerp(color, reefBright(0.5 + 0.5 * sin(at.x * 2.3 + at.z * 1.7 + at.y * 1.1 + 0.5)) * (0.40 + 0.68 * lit) * mouths, covered * 0.82);

	// where the line of sight goes into the water, on its way to the rock. if it gets to the rock
	// first, then that part of the rock is out of the water.
	float toWater = from.y / tilt;
	float out0 = step(gone, toWater);

	// the rock that is just out of the water is wet, and darker, and there is foam around it
	color *= 1.0 - 0.28 * out0 * step(at.y, 0.10);
	float foam = out0 * step(at.y, 0.035) * step(0.2, sin(px.x * 0.9 + Time * 1.7));
	color = lerp(color, FoamColor, foam * 0.85);

	// what is under the water is seen through the water: the more water there is in the way, the more
	// of its color is the water's. the light on it shifts, the way that it does on a sea floor.
	float lost = saturate(1.0 - exp(-max(gone - toWater, 0.0) * REEF_MURK));
	float shift = 0.5 + 0.5 * sin(at.x * 7.0 + at.z * 5.0 + Time * 1.6);
	color = lerp(lerp(color * (0.86 + 0.20 * shift), water, 0.18 + 0.82 * lost), color, out0);

	float4 seen = float4(color, touched * (1.0 + out0));

	// the things that grow on it, from the back of the reef to the front, so that the ones in front
	// are drawn over the ones behind. every one of them stands on the top of the bank.
	float2 foot = float2(-1.7, 1.2);
	float2 d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, kelp(d, 1.0), foot, water, seed);
	foot = float2(1.9, 1.1);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, seaFan(d, 2.3), foot, water, seed);
	foot = float2(0.3, 1.0);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, tubes(d, 3.1), foot, water, seed);
	foot = float2(-2.4, 0.4);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, seaFan(d, 4.7), foot, water, seed);
	foot = float2(2.5, 0.2);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, kelp(d, 5.9), foot, water, seed);
	foot = float2(0.2, 0.1);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, branches(d, 6.4), foot, water, seed);
	foot = float2(-0.2, -0.6);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, brain(d, 7.2), foot, water, seed);
	foot = float2(1.9, -0.5);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, branches(d, 8.8), foot, water, seed);
	foot = float2(-1.8, -0.7);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, tubes(d, 9.5), foot, water, seed);
	foot = float2(0.9, -1.1);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, kelp(d, 10.3), foot, water, seed);
	foot = float2(-0.9, -1.2);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, seaFan(d, 11.6), foot, water, seed);
	foot = float2(2.7, -0.9);
	d = (px - reefSeen(float3(foot.x, reefTop(foot, seed), foot.y), where, scale)) * float2(1.0, -1.0);
	seen = reefGrowth(seen, brain(d, 12.9), foot, water, seed);

	return seen;
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	// whole pixels, so that everything is as blocky as the rest of the game
	float2 px = floor(uv * Size);

	// the color of the sea on this pixel, the way that SurfaceEffect.fx works it out
	float dy = px.y - Horizon;
	float depth = saturate(dy / 170.0);
	float shade = bands(depth, 12.0, px);
	float3 water = lerp(SeaFar, SeaMid, smoothstep(0.0, 0.22, shade));
	water = lerp(water, SeaNear, smoothstep(0.15, 0.8, shade));
	water *= lerp(float3(1.0, 1.0, 1.0), float3(0.42, 0.80, 0.44), Sick * SICK_SKY);

	float4 rocks = reef(px, water, Where.xy, Where.z, Where.w, Seed);
	float3 color = rocks.rgb;

	// the ripples of the sea go over the part of the reef that is under the water. these are the
	// same ripples that SurfaceEffect.fx draws, so they join up with the ones around the reef.
	float rowScale = 0.25 + depth * 2.2;
	float wx = (px.x - Size.x * 0.5) / rowScale;
	float row = 13.0 * log(1.0 + max(dy, 0.0) * 0.16);
	float ripple = sin(wx * 0.10 + 2.4 * sin(row * 1.3 + Time * 0.7)) * sin(row * 2.3 - Time * 1.0);
	float path = saturate(1.0 - abs(px.x - SunX) / (10.0 + dy * 0.55));
	float glint = step(0.82 - path * 0.75, ripple);
	float3 glintColor = lerp(color + 0.07, SeaGlint, saturate(path * 1.6) * (1.0 - depth * 0.45));
	float under = step(0.5, rocks.a) * step(rocks.a, 1.5);
	color = lerp(color, glintColor, glint * under);
	color *= 1.0 - 0.14 * step(ripple, -0.8) * under;

	// the water right in front of the camera is in front of the reef. this is the same swell that
	// SurfaceEffect.fx draws across the bottom of the screen.
	float crest = Swell + sin(px.x * 0.018 + Time * 0.9) * 6.0 + sin(px.x * 0.047 - Time * 1.3) * 2.5;
	float behind = step(px.y, crest);

	return float4(color, step(0.5, rocks.a) * behind * step(Horizon, px.y) * saturate(Fade));
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
