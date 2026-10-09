#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the ancient one. when more than half of the board is horror coral, the water goes bad. the edges of
// the screen close in, dark and green, the bottom of it goes green and thick, with bubbles coming up
// through it, and tentacles come up out of that, in front of the fish, and take them. see THE TWO
// ENDINGS in fish_headers.
// none of it is a picture. it is drawn onto one plain sprite that covers the whole screen, so there
// is no texture. the game hides the sprite the rest of the time.

// the game sets all of these on every frame that the sprite is showing. a parameter that the game
// never sets does not keep a value that it is given here, so the things that never change are #defines.
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Rise;      // how far along it all is, from 0 (nothing has come up yet) to 1 (all of them are up)

// a tentacle does not take the whole of that to come up. it waits for its own moment, and then it
// comes up in three heaves, with a stop after each. this is how much of the whole all of that takes,
// from 0 to 1. smaller is more sudden.
#define TENTACLE_QUICK 0.36

// it comes up curled over at the tip, and uncurls as it comes. this is how far over, in pixels.
#define TENTACLE_CURL 44.0

// it has joints: it bends a second way, in shorter bends, more toward the tip. this is how far, in
// pixels, and how fast. and there is a darker ring around it at every joint, this many pixels apart.
#define TENTACLE_JOINT 6.5
#define TENTACLE_JOINT_SPEED 1.25
#define TENTACLE_JOINT_GAP 15.0

// THE TENTACLES
// the skin of one, from its dark side to its light side. it is dark, and more green than anything.
#define SkinDark    float3(0.035, 0.055, 0.065)
#define SkinLit     float3(0.150, 0.225, 0.200)

// the belly of it, which is a paler strip down one side, the suckers on the belly, and the line around it
#define BellyColor  float3(0.265, 0.215, 0.250)
#define SuckerColor float3(0.520, 0.400, 0.440)
#define SkinLine    float3(0.010, 0.016, 0.022)

// it is wet. this is the color of the light that it catches, and how much of that there is, from 0 to 1.
#define WetColor    float3(0.560, 0.780, 0.700)
#define TENTACLE_WET 0.5

// how wide one is at the bottom of the screen, and at its tip, from the middle of it to the edge, in pixels
#define TENTACLE_WIDE 15.0
#define TENTACLE_TIP 1.5

// how far it sways from side to side, in pixels, at the bottom of the screen and at its tip. the
// sway is a slow wave that runs up it from the bottom to the tip, and this is how fast.
#define TENTACLE_SWAY_LOW 4.0
#define TENTACLE_SWAY_TIP 24.0
#define TENTACLE_SPEED 0.7

// the tip feels around, as if it was looking for something. this is how far it swings, in pixels, and how fast.
#define TENTACLE_SEARCH 17.0
#define TENTACLE_SEARCH_SPEED 0.9

// it does not come up steadily. it comes up, and sinks back a little, and comes up again. this is
// how much of its length it sinks back by at the most, from 0 to 1, and how fast it does that.
#define TENTACLE_LURK 0.09
#define TENTACLE_LURK_SPEED 0.8

// it bulges, and the bulges run up it too. this is how much thicker a bulge is, how far apart they
// are, in pixels, and how fast they go.
#define TENTACLE_BULGE 0.22
#define TENTACLE_BULGE_GAP 46.0
#define TENTACLE_BULGE_SPEED 1.6

// how far apart the suckers are, in pixels
#define SUCKER_GAP 9.0

// some of them have an eye. this is how many, from 0 (none) to 1 (all of them).
#define EYE_SHARE 0.45
#define EyeColor    float3(0.860, 0.760, 0.250)

// THE EDGES. the corners and the edges of the screen go dark and green, and close in on the middle.
// VIGNETTE_COLOR is what they go to, VIGNETTE_DARK is how much, at the very corners, from 0 to 1, and
// VIGNETTE_IN is how far from the middle of the screen it starts, where 0 is the middle and 1 is the edge.
#define VIGNETTE_COLOR float3(0.012, 0.055, 0.026)
#define VIGNETTE_DARK 0.94
#define VIGNETTE_IN 0.42

// THE BAD WATER. the water at the bottom of the screen has gone bad. it is green, and the green gets
// thicker toward the bottom, with no line where it starts. it is cloudy, and the clouds ooze along.
// green bubbles come up through it, and go on up past the top of it, fading as they go.
//   BAD_DEEP     how much of the screen it reaches up from the bottom, from 0 to 1, once it is all the way up
//   BAD_THICK    how much of what is behind it it hides, at the very bottom of the screen, from 0 to 1
//   BAD_SOFT     how it thins out on the way up. 1 is evenly, and more than 1 keeps it down at the bottom.
//   BUBBLE_GAP   how far apart the bubbles are across the screen, in pixels
//   BUBBLE_HIGH  how far up the screen a bubble gets before it is gone, from 0 to 1
#define BadDark     float3(0.022, 0.075, 0.020)
#define BadLit      float3(0.085, 0.190, 0.045)
#define BubbleColor float3(0.360, 0.560, 0.180)
#define BAD_DEEP 0.62
#define BAD_THICK 0.90
#define BAD_SOFT 1.5
#define BUBBLE_GAP 19.0
#define BUBBLE_HIGH 0.78

// a number from 0 to 1 that is different for every tentacle
float hash1(float n)
{
	return frac(sin(n * 12.9898 + 4.1414) * 43758.5453);
}

// a number from 0 to 1 that is different for every pixel
float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
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

// cut a smooth 0 to 1 into steps, with a checkerboard between one step and the next
float bands(float v, float count, float2 px)
{
	float checker = frac((px.x + px.y) * 0.5) * 2.0;
	return floor(v * count + checker * 0.5) / count;
}

// one tentacle. rgb is its color on this pixel, and a is 1 if it is on this pixel.
// it comes up from under the bottom of the screen, `across` of the way from the left of it to the
// right, and it leans over by `lean`, which is how far to the right it goes for every pixel that it
// goes up. `full` is how long it is when it is all the way up, in pixels. `which` is a number of
// its own, so that no two of them move together. `when` is its moment to come up, from 0 to 1.
float4 tentacle(float2 px, float across, float lean, float full, float which, float when)
{
	// how far up from the bottom of the screen this pixel is, and how far off to the side of it
	float along = Size.y - px.y;
	float aside = px.x - (Size.x * across + along * lean);

	// each one waits for its moment, and then it comes up in three heaves, with a stop after each.
	// they do not come as far as each other. and once it is up, it sinks back a little and comes up
	// again, over and over.
	float heave = saturate((Rise - when) / TENTACLE_QUICK);
	float risen = (smoothstep(0.0, 0.24, heave) + smoothstep(0.36, 0.60, heave) + smoothstep(0.72, 1.0, heave)) / 3.0;
	float lurk = (0.5 + 0.5 * sin(Time * TENTACLE_LURK_SPEED + which * 4.3)) * TENTACLE_LURK * (1.0 - risen * 0.6);
	float reach = max(risen - lurk, 0.0) * full * (0.75 + 0.25 * hash1(which + 31.0));

	// how far back from the tip this pixel is, in pixels, and as a share of the whole length
	float back = reach - along;
	float share = saturate(back / max(full, 1.0));

	// it sways, slowly, in a wave that runs from the bottom of the screen up to the tip, and the tip
	// sways a lot more than the rest of it does. the last of it feels around.
	float sway = lerp(TENTACLE_SWAY_TIP, TENTACLE_SWAY_LOW, share);
	float middle = sin(along * 0.030 - Time * TENTACLE_SPEED + which * 2.1) * sway
	             + sin(along * 0.011 - Time * TENTACLE_SPEED * 0.45 + which) * 9.0;
	float atTip = 1.0 - saturate(back / 44.0);
	middle += atTip * atTip * TENTACLE_SEARCH * sin(Time * TENTACLE_SEARCH_SPEED + which * 3.0 + sin(Time * 0.37 + which) * 2.0);

	// it comes up curled over at the tip, one way or the other, and uncurls as it comes
	middle += atTip * atTip * TENTACLE_CURL * (1.0 - risen) * (step(0.5, hash1(which + 3.0)) * 2.0 - 1.0);

	// its joints: shorter bends, which are bigger toward the tip, and which run up it faster
	middle += sin(along * 0.078 - Time * TENTACLE_JOINT_SPEED + which * 5.0) * TENTACLE_JOINT * (1.0 - share);

	// it is thin at the tip and thick at the bottom, and bulges run up it
	float halfWide = lerp(TENTACLE_TIP, TENTACLE_WIDE, pow(share, 0.6));
	halfWide *= 1.0 + TENTACLE_BULGE * sin(along * 6.2832 / TENTACLE_BULGE_GAP - Time * TENTACLE_BULGE_SPEED + which * 1.7) * saturate(share * 4.0);
	float side = (aside - middle) / max(halfWide, 0.5);
	float inside = step(abs(side), 1.0) * step(0.0, back);

	// it is round, so it has a light side and a dark side
	float lit = bands(saturate(0.5 - side * 0.55), 4.0, px);
	float3 color = lerp(SkinDark, SkinLit, lit);

	// it is mottled, with darker blotches that stay where they are on it
	color *= 0.72 + 0.28 * step(0.42, clouds(float2(along, (aside - middle) * 2.0), 7.0, which * 13.0));

	// and there is a darker ring around it at every joint
	color *= 1.0 - 0.22 * step(0.84, frac(along / TENTACLE_JOINT_GAP + which * 0.37));

	// the belly is a paler strip down one side, with a row of suckers on it. there are none right at
	// the tip, where it is too thin for them.
	float belly = step(0.25, side) * step(side, 0.82);
	color = lerp(color, BellyColor * (0.6 + 0.4 * lit), belly);
	float2 toSucker = float2((side - 0.52) * halfWide, frac(along / SUCKER_GAP) * SUCKER_GAP - SUCKER_GAP * 0.5);
	float sucker = step(length(toSucker), 1.7) * step(5.0, halfWide);
	color = lerp(color, SuckerColor, sucker);
	color = lerp(color, SkinLine, sucker * step(length(toSucker), 0.7));

	// it is wet. a thin line of light runs down the light side of it, broken up. the breaks stay
	// where they are on it, so the light moves with the tentacle and not along it.
	float sheen = step(abs(side + 0.42), 0.13) * step(0.6, frac(along * 0.045 + which));
	sheen *= step(3.0, halfWide);
	color = lerp(color, WetColor, sheen * TENTACLE_WET);

	// some of them have an eye, part of the way up, which looks around and blinks
	float hasEye = step(hash1(which + 57.0), EYE_SHARE) * step(full * 0.40, reach);
	float2 toEye = float2(aside - middle, along - full * 0.34);
	float2 look = float2(sin(Time * 0.9 + which * 5.0), cos(Time * 0.7 + which * 3.0)) * 1.2;
	float open = step(0.12, frac(Time * 0.23 + hash1(which + 71.0)));
	float eye = step(length(toEye * float2(1.0, 1.0 + 2.5 * (1.0 - open))), 4.2) * hasEye;
	color = lerp(color, SkinLine, step(length(toEye), 5.4) * hasEye);
	color = lerp(color, EyeColor, eye);
	color = lerp(color, SkinLine, eye * step(length((toEye - look) * float2(2.2, 1.0)), 2.6));

	// a dark line around it
	float edge = step(1.0 - 1.2 / max(halfWide, 1.0), abs(side));
	color = lerp(color, SkinLine, edge);

	return float4(color, inside);
}

// put one thing in front of what is there already. a is how much of what is behind it it hides.
float4 over(float4 under, float4 top)
{
	return float4(lerp(under.rgb, top.rgb, top.a), under.a + (1.0 - under.a) * top.a);
}

// the bad water at the bottom of the screen. rgb is its color on this pixel, and a is how much of what
// is behind it it hides.
float4 badWater(float2 px)
{
	// how far up from the bottom of the screen this pixel is, as a share of how far the bad water
	// reaches. it comes further up as the tentacles do.
	float reach = Size.y * BAD_DEEP * saturate(Rise * 1.6);
	float up = (Size.y - px.y) / max(reach, 1.0);

	// it is cloudy, and the clouds ooze along. they make it reach higher in some places than in others.
	float ooze = clouds(float2(px.x + Time * 6.0, px.y * 2.2 - Time * 2.0), 21.0, 41.0) * 0.6
	           + clouds(float2(px.x - Time * 4.0, px.y * 2.6), 8.0, 77.0) * 0.4;
	float thick = pow(saturate(1.0 - up + (ooze - 0.5) * 0.35), BAD_SOFT);
	float3 color = lerp(BadDark, BadLit, bands(ooze, 5.0, px));
	float there = bands(thick, 12.0, px) * BAD_THICK;

	// the bubbles. there is one to every strip of the screen, and each one swells as it rises, and
	// fades out as it gets to where it is going. each has a glint on it.
	float2 strip = float2(floor(px.x / BUBBLE_GAP), 0.0);
	float rising = frac(Time * (0.07 + 0.09 * hash(strip + 11.0)) + hash(strip + 3.0));
	float2 bubble = float2((strip.x + 0.2 + 0.6 * hash(strip + 7.0)) * BUBBLE_GAP + sin(Time * 1.3 + strip.x * 2.0) * 2.0, Size.y - rising * Size.y * BUBBLE_HIGH * (0.5 + 0.5 * hash(strip + 29.0)));
	float size = 1.5 + 4.0 * rising * hash(strip + 19.0);
	float away = length(px - bubble);
	float ring = step(away, size) * step(size - 1.2, away);
	float glint = step(length(px - bubble + float2(size * 0.4, size * 0.4)), 0.8) * step(2.5, size);
	float seen = max(ring, glint) * (1.0 - rising * rising) * saturate(Rise * 3.0);
	color = lerp(color, BubbleColor * (1.0 + 0.25 * glint), seen);
	there = max(there, seen * 0.85);

	return float4(color, there * saturate(Rise * 4.0));
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	// whole pixels, so that everything is as blocky as the rest of the game
	float2 px = floor(uv * Size);

	// the edges of the screen go dark and green, and close in. they are cloudy, and the clouds drift.
	float2 fromMiddle = (px / Size - 0.5) * 2.0;
	float cloud = clouds(px + float2(Time * 7.0, Time * 3.0), 34.0, 5.0) * 0.6 + clouds(px - float2(Time * 5.0, 0.0), 13.0, 9.0) * 0.4;
	float corner = smoothstep(VIGNETTE_IN, 1.25, length(fromMiddle * float2(1.0, 0.92)) + (cloud - 0.5) * 0.22);
	float dark = bands(saturate(corner * saturate(Rise * 2.5)), 10.0, px) * VIGNETTE_DARK;
	float4 scene = float4(VIGNETTE_COLOR, dark);

	// they come up a few at a time, with a wait in between: three together on the right, then one on
	// its own on the left, then two in the middle, then one, and the tallest of them last of all.
	// the ones in the back have more of the bad water in front of them, so they are darker.
	float h = Size.y;
	float4 back = float4(0.0, 0.0, 0.0, 0.0);
	back = over(back, tentacle(px, 0.20, 0.10, h * 0.70, 1.0, 0.57));
	back = over(back, tentacle(px, 0.42, -0.06, h * 0.82, 2.0, 0.33));
	back = over(back, tentacle(px, 0.63, 0.05, h * 0.76, 3.0, 0.46));
	back = over(back, tentacle(px, 0.84, -0.12, h * 0.68, 4.0, 0.02));
	back.rgb = lerp(back.rgb, VIGNETTE_COLOR, 0.5);
	scene = over(scene, back);

	float4 front = float4(0.0, 0.0, 0.0, 0.0);
	front = over(front, tentacle(px, 0.10, 0.16, h * 0.78, 11.0, 0.19));
	front = over(front, tentacle(px, 0.31, 0.04, h * 0.90, 12.0, 0.36));
	front = over(front, tentacle(px, 0.52, -0.03, h * 0.94, 13.0, 0.62));
	front = over(front, tentacle(px, 0.73, -0.05, h * 0.88, 14.0, 0.07));
	front = over(front, tentacle(px, 0.92, -0.17, h * 0.76, 15.0, 0.04));
	scene = over(scene, front);

	// the bad water is in front of all of them, so that they come up out of it
	scene = over(scene, badWater(px));

	return scene;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
