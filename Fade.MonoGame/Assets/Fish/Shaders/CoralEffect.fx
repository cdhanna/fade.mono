#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the coral, behind the fish. see CORAL in fish_headers.
// it is rock, with coral of a few kinds growing on it. none of it is a picture. every cell of the board
// that has coral on it has a few lumps in it, and the lumps of cells that are next to each other run
// together into one rock, the way that drops of water do. the lumps are always in the same places, so
// the rock does not move. the light comes from the top left, and the rock is shaded by which way it
// slopes. the shafts of light that cross the sea floor cross the rock too, and the fish cast shadows on it.
//
// it is drawn onto one sprite that covers the whole screen. the texture of that sprite says where the
// coral is: the game draws a white square into it for every cell that has coral, and a square that is
// only part of the way there is coral that is still growing in. a square with no green in it is horror
// coral: the rock of it is sickly, and it has tentacles, eyes and skulls coming out of it.
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

// every fish on the board, drawn again as a flat shape, right where the fish is. that is what casts the shadows.
Texture2D ShadowTexture;
sampler2D ShadowTextureSampler = sampler_state
{
	Texture = <ShadowTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

// the game sets all of these on every frame. a parameter that the game never sets does not keep a
// value that it is given here, so the things that never change are #defines instead.
float Time;      // in seconds
float Mood;      // how long the run of pops that is going on is, from 0 to 1. the light is brighter.
float2 Size;     // how big the screen is, in pixels
float4 Board;    // xy is the top left corner of the board, in pixels, and z is how big a cell is
float2 Cells;    // how many columns and rows the board has

// a hint that is passing. see THE HINTS in fish_headers. the coral lights up where it is. xy is where
// it is, and zw is where it was a moment ago, both in pixels. the coral is lit all the way from the one
// to the other, and a lot less toward where it was. these are the same numbers that SpecksEffect.fx gets.
float4 Glow;
float2 GlowPower; // x is how strong it is, from 0 to 1, or more with the "listen" card, and y is how far from it the coral lights up, in pixels

// how much the coral lights up, from 0 (not at all) to 1, and the light that it takes on
#define CORAL_GLOW 0.6
#define CoralGlowColor float3(0.050, 0.130, 0.120)
#define SingGlowColor float3(0.200, 0.090, 0.110)

// THE ROCK

// its color, from where the light does not get to, to where it is brightest, and the line around it
#define RockDark    float3(0.100, 0.120, 0.235)
#define RockMid     float3(0.215, 0.260, 0.390)
#define RockLit     float3(0.390, 0.470, 0.570)
#define RockLine    float3(0.045, 0.060, 0.140)
#define RayColor    float3(0.450, 0.780, 0.800)

// the color of the rock drifts across the board, between these three tints. each one is what the colors
// above are multiplied by: the first leans blue, the second leans violet, and the third leans green.
// ROCK_TINT is how much of that there is, where 0 is none and the rock is one color everywhere, and
// ROCK_TINT_SIZE is how many cells it takes to get from one tint to the next.
#define RockTintA   float3(0.86, 1.00, 1.14)
#define RockTintB   float3(1.20, 0.92, 1.06)
#define RockTintC   float3(0.86, 1.12, 0.98)
#define ROCK_TINT 1.0
#define ROCK_TINT_SIZE 3.0

// how much of a lump there has to be on a pixel for it to be rock. lower makes the rock fatter.
#define ROCK_EDGE 0.3

// how ragged the edge of the rock is. 0 leaves it as round as the lumps are, and more than about 0.5
// starts to break bits off of it. ROCK_RAGGED_SIZE is how big the bites out of the edge are, in pixels.
#define ROCK_RAGGED 0.42
#define ROCK_RAGGED_SIZE 6.0

// how steep the rock looks. higher makes the light and the dark sides further apart.
#define ROCK_STEEP 0.3

// and how tall the bumps on it look
#define ROCK_KNOBS 8.0

// where the light comes from: the left, the top, and toward the eye
#define LIGHT_DIR float3(-0.52, -0.62, 0.59)

// THE SHADE AROUND THE ROCK. the water right around the coral is darker, as if the rock was keeping the
// light off of it. it is only in the water: the rock itself is drawn over it.
//   SHADE_DARK    how dark it is right up against the rock, from 0 (there is none) to 1 (as dark as SHADE_COLOR)
//   SHADE_REACH   how far out from the rock it goes, from 0 (nowhere) to 1 (about two thirds of a cell)
//   SHADE_SOFT    how it dies away. 1 is evenly, more than 1 keeps it close in to the rock, and less than 1 spreads it out.
//   SHADE_SHIFT   how far the whole thing is moved, in pixels. the light comes from the top left, so
//                 moving it down and to the right makes it more of a shadow, and 0, 0 makes it even all around.
//   SHADE_STEPS   how many flat steps it dies away in. more is smoother.
//   SHADE_COLOR   the color that the water is darkened toward
#define SHADE_DARK 0.4
#define SHADE_REACH 0.8
#define SHADE_SOFT .9
#define SHADE_SHIFT float2(1.0, 1.0)
#define SHADE_STEPS 6.0
#define SHADE_COLOR float3(0.020, 0.030, 0.090)

// THE SHADOWS OF THE FISH. how far a shadow falls from its fish, in pixels, and how dark it is.
#define SHADOW_REACH float2(2.0, 3.0)
#define SHADOW_DARK 0.45

// WHAT GROWS ON THE ROCK. see growth, further down.
// how many of the spots that something could grow on have something, from 0 to 1
#define GROWTH_SHARE 0.85

// how far from the middle of its cell a thing has to be, as a share of the cell. the fish is in the
// middle, and this keeps the things that grow out from under it.
#define GROWTH_CLEAR 0.34

// the soft kinds wave in the water. this is how far, in radians, and how fast.
#define GROWTH_SWAY 0.32
#define GROWTH_SPEED 1.3

// HORROR CORAL. coral that a song with the dead in it brought. see HORROR CORAL in fish_headers.
// the rock of it is the color of something that is not well: what the rock would have been is turned to
// grey, and then to SickTint, which is darker. veins run through it, in VeinColor, and they throb.
//   SICK_VEIN_SIZE    how far apart the veins are, in pixels
//   SICK_VEIN_WIDTH   how wide a vein is, from 0 (there are none) to about 0.1
//   SICK_VEIN_SPEED   how fast they throb
#define SickTint    float3(0.500, 0.610, 0.360)
#define VeinColor   float3(0.330, 0.070, 0.220)
#define SICK_VEIN_SIZE 6.0
#define SICK_VEIN_WIDTH 0.025
#define SICK_VEIN_SPEED 2.2

// and what comes out of it. see horror, further down.
#define TentacleViolet float3(0.640, 0.300, 0.600)
#define TentacleGreen  float3(0.620, 0.700, 0.300)
#define SuckerColor    float3(0.960, 0.860, 0.780)
#define EyeWhite       float3(0.860, 0.840, 0.600)
#define IrisAmber      float3(0.800, 0.420, 0.080)
#define IrisGreen      float3(0.380, 0.640, 0.160)
#define BoneColor      float3(0.800, 0.780, 0.620)
#define EmberColor     float3(0.900, 0.180, 0.100)
#define TENTACLE_SPEED 2.0
#define SKULL_SPEED 0.45

// the same slant and sway that the shafts of light have on the sea floor. see SeabedEffect.fx.
#define RaySlant 0.55
#define CamSway 16.0

// a number from 0 to 1 that is different for every pixel
float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
}

float2 hash2(float2 p)
{
	return float2(hash(p), hash(p + 41.7));
}

// cut a smooth 0 to 1 into steps, with a checkerboard between one step and the next
float bands(float v, float count, float2 px)
{
	float checker = frac((floor(px.x) + floor(px.y)) * 0.5) * 2.0;
	return floor(v * count + checker * 0.5) / count;
}

// smooth bumps, from 0 to 1. `p` is in pixels, and a bump is `size` pixels across.
float bumps(float2 p, float size, float seed)
{
	float2 q = p / size;
	float2 i = floor(q);
	float2 f = frac(q);
	f = f * f * (3.0 - 2.0 * f);
	float top = lerp(hash(i + seed), hash(i + float2(1.0, 0.0) + seed), f.x);
	float low = lerp(hash(i + float2(0.0, 1.0) + seed), hash(i + float2(1.0, 1.0) + seed), f.x);
	return lerp(top, low, f.y);
}

// how high the surface of the rock is from its bumps alone: big ones, with smaller ones on top of them
float relief(float2 p)
{
	return bumps(p, 9.0, 19.0) * 0.65 + bumps(p, 4.0, 57.0) * 0.35;
}

// how much coral a cell of the board has, from 0 to 1. a cell that is off of the board has none.
float coralAt(float2 cell)
{
	float onBoard = step(0.0, cell.x) * step(0.0, cell.y) * step(cell.x, Cells.x - 1.0) * step(cell.y, Cells.y - 1.0);
	float2 at = (Board.xy + (cell + 0.5) * Board.z) / Size;
	return tex2D(SpriteTextureSampler, at).r * onBoard;
}

// the same, in x, and in y whether the coral of the cell is horror coral: 1 if it is, and 0 if not.
// the game draws the square of a cell that has horror coral with no green in it.
float2 cellAt(float2 cell)
{
	float onBoard = step(0.0, cell.x) * step(0.0, cell.y) * step(cell.x, Cells.x - 1.0) * step(cell.y, Cells.y - 1.0);
	float2 at = (Board.xy + (cell + 0.5) * Board.z) / Size;
	float4 mark = tex2D(SpriteTextureSampler, at);
	return float2(mark.r, step(mark.g, mark.r * 0.5) * step(0.02, mark.r)) * onBoard;
}

// one lump. x is how much of it is on this spot, and yz is which way it gets thicker from here.
// it is thickest in the middle, and there is none of it past its edge. everything is measured in cells.
float3 lump(float2 spot, float2 middle, float reach)
{
	float2 away = spot - middle;
	float reach2 = max(reach * reach, 0.0001);
	float t = saturate(1.0 - dot(away, away) / reach2);
	return float3(t * t, -4.0 * t * away / reach2);
}

// the lumps of one cell: a big one near the middle, and two smaller ones off to the sides, which are
// what makes the edge of the rock uneven. coral that is still growing in has smaller lumps.
// w is how much of what is on this spot is horror coral, so that where the two kinds of coral meet,
// each one reaches as far as its own lumps do.
float4 rock(float2 spot, float2 cell)
{
	float2 state = cellAt(cell);
	float grown = state.x;
	float2 middle = cell + 0.5;
	float3 sum = lump(spot, middle + (hash2(cell + 3.1) - 0.5) * 0.22, 0.88 * grown);
	sum += lump(spot, middle + (hash2(cell + 11.7) - 0.5) * 0.75, (0.38 + 0.20 * hash(cell + 5.3)) * grown);
	sum += lump(spot, middle + (hash2(cell + 23.9) - 0.5) * 0.75, (0.34 + 0.20 * hash(cell + 9.1)) * grown);
	return float4(sum, sum.x * state.y);
}

// the same lumps of one cell, made `wide` times as big as they are. the shade around the rock is made out
// of these: lumps that are bigger than the rock reach out past it, in the shape that the rock has.
// the places and the sizes have to match the ones in rock, or the shade will not fit the rock.
float rockWide(float2 spot, float2 cell, float wide)
{
	float grown = coralAt(cell) * wide;
	float2 middle = cell + 0.5;
	float sum = lump(spot, middle + (hash2(cell + 3.1) - 0.5) * 0.22, 0.88 * grown).x;
	sum += lump(spot, middle + (hash2(cell + 11.7) - 0.5) * 0.75, (0.38 + 0.20 * hash(cell + 5.3)) * grown).x;
	sum += lump(spot, middle + (hash2(cell + 23.9) - 0.5) * 0.75, (0.34 + 0.20 * hash(cell + 9.1)) * grown).x;
	return sum;
}

// how much shade there is on a pixel, from 0 to 1, before SHADE_SOFT and SHADE_DARK have their say.
// it is 1 where the edge of the rock is, and it dies away from there on out. it is worked out from the
// same lumps that the rock is, and it is ragged in the same places, so it follows the edge of the rock
// around every bump and bite. the rock is drawn over it, so how much there is under the rock does not matter.
float shadeAt(float2 px)
{
	float2 spot = (px + 0.5 - Board.xy) / max(Board.z, 1.0);
	float2 home = floor(spot);

	// with no reach, the lumps stop where the rock does. with all of it, they are nearly half as big again.
	float wide = 0.67 + 0.78 * saturate(SHADE_REACH);
	float sum = rockWide(spot, home + float2(-1.0, -1.0), wide) + rockWide(spot, home + float2(0.0, -1.0), wide) + rockWide(spot, home + float2(1.0, -1.0), wide)
	          + rockWide(spot, home + float2(-1.0, 0.0), wide) + rockWide(spot, home, wide) + rockWide(spot, home + float2(1.0, 0.0), wide)
	          + rockWide(spot, home + float2(-1.0, 1.0), wide) + rockWide(spot, home + float2(0.0, 1.0), wide) + rockWide(spot, home + float2(1.0, 1.0), wide);

	// ragged in the same places that the rock is
	float ragged = (bumps(px, ROCK_RAGGED_SIZE, 71.0) - 0.5) + (bumps(px, ROCK_RAGGED_SIZE * 0.4, 93.0) - 0.5) * 0.5;
	float thick = sum + ragged * ROCK_RAGGED * step(0.02, sum);

	// how much of a lump that is this much bigger there is at the spot where the rock ends
	float atEdge = 1.0 - (1.0 - sqrt(ROCK_EDGE)) / (wide * wide);
	return saturate(thick / max(atEdge * atEdge, 0.02));
}

// one branch of a branching coral. x is 1 on the stalk, and y is 1 on the knob at the end of it.
// `q` is this pixel, in pixels from where the coral comes out of the rock. `way` is which way the
// branch goes, and it is `reach` pixels long.
float2 branch(float2 q, float2 way, float reach)
{
	float2 along = way * max(reach, 0.01);
	float u = saturate(dot(q, along) / dot(along, along));
	float stalk = step(distance(q, along * u), 0.6);
	float knob = step(distance(q, along), 0.9);
	return float2(stalk, knob);
}

// the bright colors, for the soft kinds of coral
float3 softColor(float pick)
{
	float3 color = float3(0.960, 0.470, 0.450);                           // pink
	color = lerp(color, float3(0.980, 0.640, 0.300), step(0.25, pick));   // orange
	color = lerp(color, float3(0.640, 0.470, 0.920), step(0.50, pick));   // violet
	color = lerp(color, float3(0.320, 0.800, 0.720), step(0.75, pick));   // teal
	return color;
}

// and the quieter ones, for the kinds that are hard
float3 hardColor(float pick)
{
	float3 color = float3(0.800, 0.660, 0.440);                           // sand
	color = lerp(color, float3(0.520, 0.720, 0.500), step(0.34, pick));   // green
	color = lerp(color, float3(0.820, 0.540, 0.600), step(0.67, pick));   // rose
	return color;
}

// one thing that comes out of horror coral, in place of a thing that grows on the rock. rgb is its
// color on this pixel, and a is 1 if it is on this pixel. `q` is this pixel, in pixels from where the
// thing comes out of the rock, `outward` is the way that points away from the fish, and `sideways` is
// across that. `roll` is the three numbers that growth rolled for the spot: what kind, what color, how big.
//
// there are three kinds:
//   a tentacle   it reaches away from the fish, and writhes. it has pale suckers down one side.
//   an eye       it looks around, and blinks now and then. the pupil is a slit.
//   a skull      it rises up out of the rock, stays for a while, and sinks back in.
float4 horror(float2 q, float2 outward, float2 sideways, float grown, float3 roll, float phase)
{
	float far = length(q);

	// a tentacle. it writhes more toward the tip, and it gets thinner.
	float reach = (6.0 + 4.0 * roll.z) * grown;
	float along = dot(q, outward);
	float t = saturate(along / max(reach, 0.01));
	float bend = dot(q, sideways) - sin(along * 0.75 - Time * TENTACLE_SPEED + phase) * (0.3 + 1.8 * t);
	float tentacleHit = step(0.0, along) * step(along, reach) * step(abs(bend), lerp(1.7, 0.6, t));
	float3 flesh = lerp(TentacleViolet, TentacleGreen, step(0.5, roll.y));
	float sucker = step(0.35, bend) * step(0.5, frac(along * 0.5));
	float3 tentacleColor = lerp(flesh * (1.0 - 0.45 * step(bend, -0.35)), SuckerColor, sucker);

	// an eye. the white of it is yellowed, and darker around the edge.
	float eyeSize = (2.3 + 1.0 * roll.z) * grown;
	float eyeHit = step(far, eyeSize);
	float2 look = float2(cos(Time * 0.50 + phase), sin(Time * 0.37 + phase * 1.7)) * eyeSize * 0.32;
	float2 fromLook = q - look;
	float3 eyeColor = EyeWhite * (1.0 - 0.40 * step(eyeSize - 0.9, far));
	eyeColor = lerp(eyeColor, lerp(IrisAmber, IrisGreen, step(0.5, roll.y)), step(length(fromLook), eyeSize * 0.55));
	eyeColor = lerp(eyeColor, float3(0.03, 0.02, 0.05), step(abs(fromLook.x), 0.55) * step(abs(fromLook.y), eyeSize * 0.45));
	float blink = step(0.94, sin(Time * 0.9 + phase * 3.0));
	eyeColor = lerp(eyeColor, flesh * (1.0 - 0.5 * step(abs(q.y), 0.5)), blink);

	// a skull. `s` is this pixel from the middle of it, and it is drawn lower the further it has sunk.
	// the line that it sinks behind stays where it is, so the skull goes into the rock from the jaw up.
	float rise = smoothstep(-0.35, 0.35, sin(Time * SKULL_SPEED + phase));
	float2 s = q - float2(0.0, (1.0 - rise) * 7.5);
	float head = step(length(s - float2(0.0, -1.0)), 2.8);
	float jaw = step(abs(s.x), 1.6) * step(1.0, s.y) * step(s.y, 3.6);
	float sockets = step(length(float2(abs(s.x) - 1.25, s.y + 0.9)), 0.95);
	float nose = step(abs(s.x), 0.5) * step(abs(s.y - 0.8), 0.5);
	float teeth = step(2.4, s.y) * step(0.5, frac(s.x * 0.5 + 0.25));
	float skullHit = max(head, jaw) * step(q.y, 3.6) * step(0.5, grown);
	float3 skullColor = BoneColor * (1.0 - 0.07 * clamp(s.x + s.y, -2.0, 4.0));
	skullColor = lerp(skullColor, float3(0.06, 0.04, 0.09), saturate(sockets + nose + teeth));

	// something is alight in the sockets, once it is all the way out
	float ember = step(length(float2(abs(s.x) - 1.25, s.y + 0.9)), 0.5) * step(0.95, rise);
	skullColor = lerp(skullColor, EmberColor * (0.75 + 0.25 * sin(Time * 3.0 + phase)), ember);

	// which kind this one is
	float isTentacle = step(roll.x, 0.42);
	float isEye = step(0.42, roll.x) * step(roll.x, 0.74);
	float isSkull = step(0.74, roll.x);

	float3 color = tentacleColor * isTentacle + eyeColor * isEye + skullColor * isSkull;
	float hit = tentacleHit * isTentacle + eyeHit * isEye + skullHit * isSkull;
	return float4(color, hit);
}

// one thing that grows on the rock. rgb is its color on this pixel, and a is 1 if it is on this pixel.
//
// the board is cut into squares that are half of a cell across, and each square is a spot that one
// thing can grow on. so a cell has room for four. `site` is which square, and `at` is this pixel, in
// pixels from the top left corner of the board. where in its square the thing is, what kind it is, how
// big, and what color, all come from which square it is, so they never change.
// a thing is pushed out from the middle of its cell, to where it shows around the fish. that puts a
// lot of them on the edges of the rock, and the ones there hang out over the edge.
//
// there are four kinds:
//   branching coral   a fan of three branches with knobs on the ends, pointing away from the fish. it waves.
//   an anemone        a ring of short arms around a pale middle. the arms wave.
//   brain coral       a dome with winding grooves in it. it is hard, so it does not move.
//   cup coral         two little cups with dark middles. these do not move either.
float4 growth(float2 at, float2 site)
{
	float gap = Board.z * 0.5;

	// where it is, and the cell that it belongs to
	float2 first = (site + 0.5 + (hash2(site + 7.7) - 0.5) * 0.7) * gap;
	float2 cell = floor(first / Board.z);
	float2 middle = (cell + 0.5) * Board.z;
	float2 fromMiddle = first - middle;
	float dist = max(length(fromMiddle), 0.01);
	float2 outward = fromMiddle / dist;
	float2 sideways = float2(-outward.y, outward.x);
	float2 root = middle + outward * max(dist, Board.z * GROWTH_CLEAR);

	// it is only as big as the coral in its cell has grown
	float2 state = cellAt(cell);
	float grown = state.x;
	float there = step(hash(site + 91.3), GROWTH_SHARE) * step(0.05, grown);

	float2 q = at - root;
	float far = length(q);
	float kind = hash(site + 33.1);
	float pick = hash(site + 51.1);
	float big = hash(site + 17.9);
	float phase = hash(site + 5.9) * 6.2832;
	float sway = sin(Time * GROWTH_SPEED + phase + root.x * 0.06) * GROWTH_SWAY;

	// branching coral
	float2 fan = branch(q, outward * cos(sway - 0.62) + sideways * sin(sway - 0.62), (4.0 + 2.0 * big) * grown);
	fan = max(fan, branch(q, outward * cos(sway * 1.3) + sideways * sin(sway * 1.3), (5.0 + 2.0 * big) * grown));
	fan = max(fan, branch(q, outward * cos(sway + 0.62) + sideways * sin(sway + 0.62), (4.0 + 2.0 * big) * grown));
	float3 soft = softColor(pick);
	float3 fanColor = lerp(soft * 0.62, lerp(soft, float3(1.0, 1.0, 1.0), 0.30), fan.y);
	float fanHit = max(fan.x, fan.y);

	// an anemone
	float around = atan2(q.y, q.x);
	float arms = 0.50 + 0.50 * cos(around * 6.0 + sway * 4.0 + phase);
	float armReach = (3.0 + 1.6 * big) * grown;
	float anemoneHit = step(far, armReach * (0.45 + 0.55 * arms));
	float3 anemoneColor = lerp(soft * 0.75, lerp(soft, float3(1.0, 1.0, 1.0), 0.55), step(far, 1.1));

	// brain coral. it is lit from the top left like the rock is, and it is darker around its edge.
	float dome = (3.2 + 1.8 * big) * grown;
	float brainHit = step(far, dome);
	float groove = step(0.0, sin(q.x * 1.7 + sin(q.y * 1.5 + phase) * 1.7 + phase));
	float3 hard = hardColor(pick);
	float3 brainColor = hard * (0.62 + 0.30 * groove) * (1.0 - 0.32 * dot(q / max(dome, 0.01), float2(0.6, 0.7)));
	brainColor *= 1.0 - 0.35 * step(dome - 0.9, far);

	// cup coral
	float2 cupA = q - (hash2(site + 61.1) - 0.5) * 3.0;
	float2 cupB = q - (hash2(site + 77.3) - 0.5) * 3.0 - float2(2.4, 1.2);
	float cupFar = min(length(cupA), length(cupB));
	float cupHit = step(cupFar, 2.0 * grown);
	float3 cupColor = lerp(float3(0.960, 0.800, 0.380), float3(0.760, 0.560, 0.900), step(0.5, pick));
	cupColor = lerp(cupColor, cupColor * 0.25, step(cupFar, 0.8 * grown));

	// which kind this one is
	float isFan = step(kind, 0.36);
	float isAnemone = step(0.36, kind) * step(kind, 0.56);
	float isBrain = step(0.56, kind) * step(kind, 0.80);
	float isCup = step(0.80, kind);

	float3 color = fanColor * isFan + anemoneColor * isAnemone + brainColor * isBrain + cupColor * isCup;
	float hit = fanHit * isFan + anemoneHit * isAnemone + brainHit * isBrain + cupHit * isCup;

	// HORROR CORAL has other things coming out of it. see horror, further down.
	float4 dread = horror(q, outward, sideways, grown, float3(kind, pick, big), phase);
	color = lerp(color, dread.rgb, state.y);
	hit = lerp(hit, dread.a, state.y);
	return float4(color, hit * there);
}

// put a thing that grows on the rock on top of what is there already
float4 over(float4 under, float4 top)
{
	return float4(lerp(under.rgb, top.rgb, top.a), max(under.a, top.a));
}

// how lit up this pixel is by a hint that is passing, from 0 to 1
float glowAt(float2 px)
{
	float2 along = Glow.zw - Glow.xy;
	float back = saturate(dot(px - Glow.xy, along) / max(dot(along, along), 0.001));
	float dist = length(px - (Glow.xy + along * back));
	float close = saturate(1.0 - dist / max(GlowPower.y, 1.0));
	return saturate(close * (2.0 - close) * (1.0 - back) * (1.0 - back) * clamp(GlowPower.x, 0.0, 4.0));
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	// whole pixels, so that everything is as blocky as the rest of the game
	float2 px = floor(uv * Size);

	// where this pixel is on the board, in cells, and which cell that is
	float2 spot = (px + 0.5 - Board.xy) / max(Board.z, 1.0);
	float2 home = floor(spot);

	// the lumps of this cell, and of the eight around it
	float4 sum = rock(spot, home + float2(-1.0, -1.0)) + rock(spot, home + float2(0.0, -1.0)) + rock(spot, home + float2(1.0, -1.0))
	           + rock(spot, home + float2(-1.0, 0.0)) + rock(spot, home) + rock(spot, home + float2(1.0, 0.0))
	           + rock(spot, home + float2(-1.0, 1.0)) + rock(spot, home + float2(0.0, 1.0)) + rock(spot, home + float2(1.0, 1.0));

	// the edge of the rock is ragged. there is a little more of it in some places than the lumps say,
	// and a little less in others, and that only shows where the rock runs out.
	float ragged = (bumps(px, ROCK_RAGGED_SIZE, 71.0) - 0.5) + (bumps(px, ROCK_RAGGED_SIZE * 0.4, 93.0) - 0.5) * 0.5;
	float thick = sum.x + ragged * ROCK_RAGGED * step(0.02, sum.x);
	float solid = step(ROCK_EDGE, thick);

	// which way the rock faces here. the lumps say which way it slopes on the whole, and it has bumps
	// all over it, which is what gives the middle of a big rock a light side and a dark side to every
	// knob. it is rough too, so no two pixels face quite the same way.
	float high = relief(px);
	float2 knobs = float2(relief(px + float2(1.0, 0.0)) - high, relief(px + float2(0.0, 1.0)) - high);
	float2 rough = float2(hash(px + 1.7), hash(px + 7.9)) - 0.5;
	float3 facing = normalize(float3(-sum.yz * ROCK_STEEP - knobs * ROCK_KNOBS + rough * 0.18, 1.0));

	// how much of the light it catches, in a few flat steps
	float lit = bands(saturate(dot(facing, normalize(LIGHT_DIR)) * 1.35 - 0.22), 5.0, px);
	float3 color = lerp(RockDark, RockMid, saturate(lit * 2.0));
	color = lerp(color, RockLit, saturate(lit * 2.0 - 1.0));

	// the rock is not the same color all over. it drifts from one tint to another across the board, over
	// a few cells, so that one end of a big reef is not quite the color of the other end.
	float2 whereOnBoard = (px + 0.5 - Board.xy) / max(Board.z, 1.0);
	float drift = bumps(whereOnBoard, ROCK_TINT_SIZE, 131.0);
	float drift2 = bumps(whereOnBoard, ROCK_TINT_SIZE * 0.6, 177.0);
	float3 tint = lerp(RockTintA, RockTintB, smoothstep(0.25, 0.75, drift));
	tint = lerp(tint, RockTintC, smoothstep(0.55, 0.9, drift2));
	color *= lerp(float3(1.0, 1.0, 1.0), tint, ROCK_TINT);

	// pits, and pale specks of the things that live on it
	color *= 1.0 - 0.30 * step(0.90, hash(floor(px / 2.0) + 3.7));
	color += float3(0.06, 0.08, 0.10) * step(0.95, hash(floor(px / 2.0) + 9.2));

	// horror coral. where it meets coral that is well, the line between them is speckled.
	float sick = step(0.5, sum.w / max(sum.x, 0.0001) + (hash(px + 13.3) - 0.5) * 0.3);
	float3 unwell = dot(color, float3(0.30, 0.50, 0.20)) * 1.6 * SickTint;
	float vein = step(abs(bumps(px, SICK_VEIN_SIZE, 211.0) - 0.5), SICK_VEIN_WIDTH);
	float throb = 0.5 + 0.5 * sin(Time * SICK_VEIN_SPEED - bumps(px, 14.0, 301.0) * 9.0);
	unwell = lerp(unwell, VeinColor * (0.55 + 0.75 * throb), vein);
	color = lerp(color, unwell, sick);

	// a dark line around the edge of the rock
	float rim = 1.0 - smoothstep(ROCK_EDGE, ROCK_EDGE + 0.09, thick);
	color = lerp(color, RockLine, rim * 0.85);

	// what grows on the rock. the spots that things grow on are squares that are half of a cell across,
	// and a thing can reach into the squares next to its own, so this looks at nine of them.
	float2 at = px + 0.5 - Board.xy;
	float2 site = floor(at / (Board.z * 0.5));
	float4 scene = float4(color, solid);
	scene = over(scene, growth(at, site + float2(-1.0, -1.0)));
	scene = over(scene, growth(at, site + float2(0.0, -1.0)));
	scene = over(scene, growth(at, site + float2(1.0, -1.0)));
	scene = over(scene, growth(at, site + float2(-1.0, 0.0)));
	scene = over(scene, growth(at, site));
	scene = over(scene, growth(at, site + float2(1.0, 0.0)));
	scene = over(scene, growth(at, site + float2(-1.0, 1.0)));
	scene = over(scene, growth(at, site + float2(0.0, 1.0)));
	scene = over(scene, growth(at, site + float2(1.0, 1.0)));
	color = scene.rgb;

	// the shadows of the fish. the light comes from the top left, so a shadow falls down and to the right.
	float shadow = tex2D(ShadowTextureSampler, (px + 0.5 - SHADOW_REACH) / Size).a;
	color *= 1.0 - SHADOW_DARK * step(0.5, shadow);

	// a hint that is passing lights up the coral under it: the rock, and what grows on it
	float lightUp = bands(glowAt(px + 0.5), 5.0, px) * CORAL_GLOW;
	color = lerp(color, color * 1.9 + CoralGlowColor, lightUp);

	// a fish that is singing on the coral of this cell lights it up too. the game draws the square of the
	// cell with less blue in it, the more it is lit.
	float singOn = step(0.0, home.x) * step(0.0, home.y) * step(home.x, Cells.x - 1.0) * step(home.y, Cells.y - 1.0);
	float4 singMark = tex2D(SpriteTextureSampler, (Board.xy + (home + 0.5) * Board.z) / Size);
	float sing = saturate(1.0 - singMark.b / max(singMark.g, 0.02)) * step(0.02, singMark.g) * singOn;
	color = lerp(color, color * 1.7 + SingGlowColor, bands(sing, 5.0, px) * 0.85);

	// the light comes and goes, like clouds are crossing the sun, and it is brighter the longer a run
	// of pops goes on. this is the same light that the sea floor has. see SeabedEffect.fx.
	float sway = sin(Time * 0.07) * CamSway * 0.6 + sin(Time * 0.031 + 1.0) * CamSway * 0.4;
	float across = px.x - px.y * RaySlant + floor(sway * 0.15);
	float cloud = sin(across * 0.0045 + Time * 0.11) * 0.6 + sin(across * 0.0021 - Time * 0.06 + 2.0) * 0.4;
	color *= (0.85 + 0.4 * Mood) * (1.0 + 0.1 * cloud);

	// and the same shafts of light. they show on the sides of the rock that face the light.
	float ray = sin(across * 0.019 + Time * 0.10) + 0.7 * sin(across * 0.043 - Time * 0.07 + 1.3) + 0.5 * sin(across * 0.071 + Time * 0.045 + 4.0);
	ray = smoothstep(0.45, 1.9, ray) * (0.85 + 0.15 * sin(Time * 0.6 + across * 0.1));
	float reach = saturate(1.0 - (px.y / Size.y) * 1.05);
	ray = bands(saturate(ray * reach * (0.55 + 1.3 * Mood) * (1.0 + 0.3 * cloud)), 10.0, px);
	color += RayColor * ray * (0.10 + 0.30 * lit);

	// the shade in the water around the rock. it is looked up a little way off, which is what moves it.
	float shade = bands(pow(shadeAt(px - SHADE_SHIFT), SHADE_SOFT), SHADE_STEPS, px) * SHADE_DARK;

	// where there is rock, or something growing on it, that is all there is. everywhere else there is the shade.
	float alpha = scene.a;
	return float4(lerp(SHADE_COLOR, color, alpha), max(alpha, saturate(shade)));
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
