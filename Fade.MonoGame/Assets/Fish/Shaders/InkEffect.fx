#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the ink of the squid. see THE INK in fish_headers_squid. all of the ink on the board is drawn by
// this, on one plain sprite that covers the board, under the fish, so that ink in cells that touch is
// one smear, with no seam between the cells: a dark blot in every inked cell, flowing into the inked
// cells next to it, with a light that runs along the edge of the whole of it, and a glow that spills
// out into the water. it is not black. it is the ink of a squid of the deep, which glows.
// every pixel works out which cell it is in, and looks at that cell and the eight around it.
// BY DAY, the ink that is left has dried (THE INK OF THE DAY, in fish_headers_squid): the same wobbling
// blots, but swollen, so that cells that touch run together into one solid smear, with the hole in the
// middle of four of them filled in too, dark blue and nearly opaque, with a lighter edge around the
// whole of it, and a faint reflection of each fish in it, which shimmers. it swells from the one look
// to the other as day breaks.
// all of it, by night and by day, has a slow cloud of dark blues and purples through it, which is fixed
// to the screen, and not to the ink, so that the ink seems to be a window onto it.
// CLEAN. the cells that the ink of the day has been washed off of are the other way around from the
// ink: a pale, see-through edge around them, a glow behind the fish of each, and a sheen and glints that
// sweep across the screen through them now and then, squeaky clean. when the pot pays, they poof away
// one after another (InkFizzEffect.fx draws that), and a glow runs on ahead of the next ones to go.

float4x4 MatrixTransform;

// the game sets all of these on every frame. a parameter that the game never sets does not keep a
// value that it is given here, so the things that never change are #defines.
//   Ink       x the time, in seconds. y how much it is night, from 0 to 1. z how much of the ink is
//             left, as it fades away. w how much a made constellation is shimmering, from 0 to 1.
//   Board     x how many columns, y how many rows, z how big a cell is, in pixels, and w how far the
//             top left of the board is from the top left of the sprite, in pixels
//   Fresh     x and y the cell that was inked last, z how far its ink has got, from 0 to 1, and w how
//             far along the wobble of it landing is, from 0 to 1, once it has got there
//   FreshFrom x and y the inked cell that it spread from, and z 1 when there is one: then the ink flows
//             out of that cell into it, along with the cursor, and lands there like a drop. without one,
//             it grows from nothing.
//   Rows0..4  which cells are inked: one number for every row, four rows to each, and a bit for every
//             column, the first column in the lowest bit
//   Made0..4  the same, for the cells that are part of a made constellation
//   Clean0..4 and for the clean cells (CLEAN in fish_headers_squid), which have no ink on them
//   Wave      x and y the cell that the pot that pays poofs out from, z how far from it the cell that
//             poofs next is, in cells, for the glow, or less than nothing when there is none, and w how
//             much of the clean cells is there
//   Lurch     x and y an inked cell whose ink is reaching for the coral at z and w (CORAL WINS)
//   LurchAt   x how far it has got, in cells, and y 1 while it is
//   Day       x how much of the day's look there is, from 0 to 1. y half of how big a fish is drawn, in
//             pixels. z 1 when the fish are drawn to ShadowTexture for their reflections.
//   Area      x and y where the top left of the sprite is on the screen, in pixels, and z and w how big
//             the screen is, which ShadowTexture covers
//   ShadowTexture  the fish of the board, each a plain picture of itself where it is (the coral's
//             shadows draw them; see THE FISH CAST SHADOWS in fish_headers)
float4 Ink;
float4 Board;
float4 Fresh;
float4 FreshFrom;
float4 Rows0;
float4 Rows1;
float4 Rows2;
float4 Rows3;
float4 Rows4;
float4 Made0;
float4 Made1;
float4 Made2;
float4 Made3;
float4 Made4;
float4 Clean0;
float4 Clean1;
float4 Clean2;
float4 Clean3;
float4 Clean4;
float4 Wave;
float4 Lurch;
float4 LurchAt;
float4 Day;
float4 Area;

// the sprite's own picture, which is a plain white pixel. it is here so that it has the first sampler,
// which the sprite batch gives the sprite's picture, and ShadowTexture has one of its own.
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

// how big the blot is, in cells, and how wide it is where it flows into the next cell
#define BLOT 0.36
#define BRIDGE 0.30

// how far the edge of it wobbles, in cells, and how fast
#define WOBBLE 0.035
#define WOBBLE_SPEED 1.3

// how wide the bright edge is, in cells, how far the glow reaches out into the water, and how many
// steps it comes in, with a checkerboard between one step and the next, like the halo of a fish at night
#define RIM 0.075
#define GLOW_REACH 6.0
#define GLOW_STEPS 6.0
#define GLOW_BRIGHT 0.62

// how much the blots of cells that touch run together, in cells. it rounds off the corners where a
// stroke turns, so that it is a smear and not a row of beads.
#define MELT 0.12

// the colors. the inside is a deep blue, nearly black, with threads of light that drift through it.
// the edge goes between a pale blue and a violet, and the glow out in the water is deeper than that.
// a made constellation has a little of the starlight in it, and it shimmers with it when it is made.
#define DEEP      float3(0.035, 0.025, 0.095)
#define THREAD    float3(0.30, 0.42, 0.95)
#define EDGE_A    float3(0.45, 0.92, 1.00)
#define EDGE_B    float3(0.78, 0.52, 1.00)
#define HALO_A    float3(0.16, 0.48, 1.00)
#define HALO_B    float3(0.50, 0.26, 1.00)
#define STARLIGHT float3(1.00, 0.88, 0.58)

// the dried ink of the day: its inside, how much of the water shows through it, its edge, and how wide
// that is, in pixels. it stops a pixel short of the sides of the cells, and its outside corners are round.
#define DAY_ALPHA  0.86
#define DAY_EDGE   float3(0.19, 0.29, 0.58)
#define DAY_EDGE_PX 1.0
#define DAY_SHADOW float3(0.01, 0.02, 0.07)
// by day the blots are this big, and the strokes between them this wide, in cells, and where four
// cells meet, a blot this big fills the hole between them
#define BLOT_DAY   0.47
#define BRIDGE_DAY 0.40
#define QUAD_DAY   0.36

// THE REFLECTIONS. under each fish, its picture upside down, faint, with every other row of it dimmer,
// rows that slide down as it shimmers, and a little sway from side to side. it is mirrored about a line
// this much of half the size of the fish down from its middle, which is a little above the bottom of the
// picture, because the fish does not fill it, and it reaches this many times half the size of the fish
// down from there, fading as it goes.
#define REFLECT_ALPHA 0.52
#define REFLECT_LINE  0.62
#define REFLECT_REACH 1.3
#define REFLECT_TINT  float3(0.45, 0.62, 1.00)
#define REFLECT_TINT_AMOUNT 0.30

// THE CLOUD through the ink: how big its two layers are, in pixels, how fast they drift, in pixels a
// second, and its colors, from the darkest to the blue and the purple
#define CLOUD_BIG    11.0
#define CLOUD_SMALL  5.0
#define CLOUD_DRIFT  1.4
#define CLOUD_DARK   float3(0.012, 0.018, 0.070)
#define CLOUD_BLUE   float3(0.040, 0.090, 0.290)
#define CLOUD_PURPLE float3(0.190, 0.060, 0.300)
#define CLOUD_STEPS  6.0
// how much of it the ink of the night has, which is darker than the day's
#define CLOUD_NIGHT  0.55

// CLEAN. the light inside, the gold that the wave leaves, and how much of each part there is
#define CLEAN_LIGHT float3(0.72, 0.95, 1.00)
#define CLEAN_FILL  0.025
#define CLEAN_RIM   0.16
#define CLEAN_RIM_PX 1.5
#define CLEAN_SPOT  0.22
// SQUEAKY CLEAN: a sheen of thin bright bands that sweep across the screen, through every clean cell at
// once, and little glints that twinkle on and off where the grid of them falls on a clean cell. the
// bands are this far apart and this fast, in pixels, and the glints this far apart, in pixels.
#define SHEEN_APART 220.0
#define SHEEN_SPEED 24.0
#define SHEEN_BRIGHT 0.14
// how far the band bends to either side, in pixels, and how tight its bends are
#define SHEEN_WAVE 7.0
#define SHEEN_RIPPLE 0.11
#define GLINT_APART 7.0
#define GLINT_BRIGHT 0.4
// how few of the spots of the grid of glints have one, from 0 (all) to 1 (none), and how slowly they twinkle
#define GLINT_RARE  0.93
#define GLINT_SLOW  0.35
#define CLEAN_SPOT_TIGHT 9.0
// how far in from the side of its cell a clean cell stops, and how round its corners are, in cells
#define CLEAN_GAP   0.05
#define CLEAN_ROUND 0.42
// and how far its edge wobbles, in cells, so that it is never a square, like the cursor
#define CLEAN_WOBBLE 0.035

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 Quad : TEXCOORD0;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	VertexShaderOutput output;
	position.xy = floor(position.xy + .5);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.Quad = texCoord;
	return output;
}

// the number of a row, out of the four numbers of each of the five parameters
float pick(float row, float4 a, float4 b, float4 c, float4 d, float4 e)
{
	return dot(a, saturate(1.0 - abs(row - float4(0, 1, 2, 3))))
	     + dot(b, saturate(1.0 - abs(row - float4(4, 5, 6, 7))))
	     + dot(c, saturate(1.0 - abs(row - float4(8, 9, 10, 11))))
	     + dot(d, saturate(1.0 - abs(row - float4(12, 13, 14, 15))))
	     + dot(e, saturate(1.0 - abs(row - float4(16, 17, 18, 19))));
}

// is the bit of this column set, in the number of a row? off of the board, it never is.
float bitOf(float rowBits, float column)
{
	float inside = step(0.0, column) * step(column, Board.x - 1.0);
	return fmod(floor(rowBits / exp2(max(column, 0.0)) + .0001), 2.0) * inside;
}

// a smooth minimum, so that two shapes that meet run together
float melt(float a, float b)
{
	float h = saturate(.5 + .5 * (b - a) / MELT);
	return lerp(b, a, h) - MELT * h * (1.0 - h);
}

// how far a point is from the stroke between the middles of two cells, `wide` cells wide
float bridge(float2 p, float2 a, float2 b, float wide)
{
	float2 ab = b - a;
	float h = saturate(dot(p - a, ab) / dot(ab, ab));
	return length(p - a - ab * h) - wide;
}

// a number from 0 to 1 for a point, which looks random, and a smooth noise made of them
float hash21(float2 p)
{
	return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float vnoise(float2 p)
{
	float2 i = floor(p);
	float2 f = frac(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash21(i);
	float b = hash21(i + float2(1.0, 0.0));
	float c = hash21(i + float2(0.0, 1.0));
	float e = hash21(i + float2(1.0, 1.0));
	return lerp(lerp(a, b, f.x), lerp(c, e, f.x), f.y);
}

// THE CLOUD at a pixel of the screen, in steps, with a checkerboard between one step and the next
float3 cloudAt(float2 world, float time, float checker)
{
	float drift = time * CLOUD_DRIFT;
	float n = vnoise((world + float2(drift, drift * .4)) / CLOUD_BIG) * .62
	        + vnoise((world + float2(-drift * .7, drift * .8)) / CLOUD_SMALL) * .38;
	n = saturate(floor(n * CLOUD_STEPS + checker * .5) / CLOUD_STEPS);
	float3 tint = lerp(CLOUD_BLUE, CLOUD_PURPLE, saturate(n * 2.2 - .7));
	return lerp(CLOUD_DARK, tint, saturate(n * 1.8 - .1));
}

// is this cell the one at `at`?
float isCell(float2 cell, float2 at)
{
	return step(abs(cell.x - at.x) + abs(cell.y - at.y), .5);
}

// how big the blot of a cell is. the one that was inked last grows: out of nothing, when there is no
// cell that it spread from, and otherwise as the ink that flows out of that cell gets to it, and then it
// wobbles, like a drop that lands.
float sizeOf(float2 cell)
{
	float isFresh = isCell(cell, Fresh.xy);
	float spread = saturate(Fresh.z);
	float grown = spread * (2.0 - spread);
	float landed = saturate((spread - .45) / .55);
	float land = saturate(Fresh.w);
	float drop = landed * (1.0 + .24 * sin(land * 6.28318) * (1.0 - land));
	return lerp(1.0, lerp(grown, drop, FreshFrom.z), isFresh);
}

// how far a point is from the stroke between two inked cells that touch at a side. when the one was
// inked last, spreading from the other, the stroke is the ink flowing out of the other into it, along
// with the cursor, with a round head where it has got to.
float strokeOf(float2 q, float2 a, float2 b, float both, float wideOf)
{
	float aFresh = isCell(a - .5, Fresh.xy);
	float bFresh = isCell(b - .5, Fresh.xy);
	float aFrom = isCell(a - .5, FreshFrom.xy) * FreshFrom.z;
	float bFrom = isCell(b - .5, FreshFrom.xy) * FreshFrom.z;
	float backward = bFrom * aFresh;
	float flowing = max(aFrom * bFresh, backward);
	float2 src = lerp(a, b, backward);
	float2 dst = lerp(b, a, backward);
	float reach = saturate(Fresh.z);
	dst = lerp(dst, src + (dst - src) * max(reach, .02), flowing);
	float wide = wideOf * lerp(min(sizeOf(a - .5), sizeOf(b - .5)), .7 + .3 * reach, flowing);
	float head = length(q - dst) - wideOf * 1.25 + (1.0 - flowing) * 10.0;
	return min(bridge(q, src, dst, wide), head) + (1.0 - both) * 10.0;
}

// the nine cells around a pixel's own, by number, from the top left: whether each is inked, and its
// middle, on the board
#define INKED(k) (((k) / 3 == 0) ? inkU : (((k) / 3 == 1) ? inkM : inkD))[(k) - ((k) / 3) * 3]
#define MIDDLE(k) (home + float2((k) - ((k) / 3) * 3 - 1, (k) / 3 - 1) + .5)

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float cellPx = max(Board.z, 1.0);
	float time = Ink.x;
	float day = saturate(Day.x);

	// where this pixel is on the board, in cells, a whole pixel of the world at a time (the screen may be
	// drawn bigger than the world). the middle of the first cell is 0.5.
	float2 sizePx = (Board.xy + 2.0) * cellPx;
	float2 px = floor(input.Quad * sizePx);
	float2 q = (px + .5 - Board.w) / cellPx;
	float2 home = floor(q);

	// the three rows around it, and the bits of the nine cells
	float rowUp = pick(home.y - 1.0, Rows0, Rows1, Rows2, Rows3, Rows4) * step(1.0, home.y);
	float rowMid = pick(home.y, Rows0, Rows1, Rows2, Rows3, Rows4) * step(0.0, home.y);
	float rowDown = pick(home.y + 1.0, Rows0, Rows1, Rows2, Rows3, Rows4) * step(-1.0, home.y);

	// d is how far the pixel is from the ink, less than nothing inside it
	float d = 10.0;
	// the bits of the nine cells, three to a row, which is fewer registers than nine of each
	float3 inkU = 0, inkM = 0, inkD = 0;
	[unroll] for (int j = 0; j < 3; j++)
	{
		float rowBits = rowUp;
		if (j == 1) rowBits = rowMid;
		if (j == 2) rowBits = rowDown;
		[unroll] for (int i = 0; i < 3; i++)
		{
			float2 cell = home + float2(i - 1, j - 1);
			float on = bitOf(rowBits, cell.x);
			if (j == 0) inkU[i] = on;
			if (j == 1) inkM[i] = on;
			if (j == 2) inkD[i] = on;

			// the blot of the cell, which wobbles at its edge, in a time of its own
			float2 off = q - (cell + .5);
			float seed = cell.x * 1.7 + cell.y * 3.1;
			float angle = atan2(off.y, off.x);
			float wobble = WOBBLE * sin(angle * 3.0 + time * WOBBLE_SPEED + seed)
			             + WOBBLE * .6 * sin(angle * 5.0 - time * WOBBLE_SPEED * .7 + seed * 1.3);
			float blot = length(off) - (lerp(BLOT, BLOT_DAY, day) + wobble) * sizeOf(cell);
			d = melt(d, blot + (1.0 - on) * 10.0);
		}
	}

	// and the strokes between every two inked cells of the nine that touch at a side
	float bridgeWide = lerp(BRIDGE, BRIDGE_DAY, day);
	[unroll] for (int k = 0; k < 9; k++)
	{
		int kx = k - (k / 3) * 3;
		if (kx < 2)
		{
			d = melt(d, strokeOf(q, MIDDLE(k), MIDDLE(k + 1), INKED(k) * INKED(k + 1), bridgeWide));
		}
		if (k < 6)
		{
			d = melt(d, strokeOf(q, MIDDLE(k), MIDDLE(k + 3), INKED(k) * INKED(k + 3), bridgeWide));
		}
	}

	// CORAL WINS: ink that reaches for coral, out of its cell, with a round head where it has got to
	float2 lurchA = Lurch.xy + .5;
	float2 lurchTip = lurchA + (Lurch.zw + .5 - lurchA) * LurchAt.x;
	float lurch = min(bridge(q, lurchA, lurchTip, bridgeWide * .8), length(q - lurchTip) - bridgeWide * 1.1);
	d = melt(d, lurch + (1.0 - LurchAt.y) * 10.0);

	// by day, the hole where four inked cells meet is filled in
	[unroll] for (int c = 0; c < 4; c++)
	{
		int cx = c - (c / 2) * 2;
		int cy = c / 2;
		int c0 = cy * 3 + cx;
		float four = INKED(c0) * INKED(c0 + 1) * INKED(c0 + 3) * INKED(c0 + 4);
		float2 corner = home + float2(cx, cy);
		d = melt(d, length(q - corner) - QUAD_DAY * day + (1.0 - four * step(.01, day)) * 10.0);
	}

	// how far the pixel is inside the clean cells, in cells: the cell that it is in, if that is clean, as a
	// box a little smaller than the cell, open on the sides where the cell next to it is clean too, with
	// its corners rounded off where two sides that are not open meet
	float cleanUp = pick(home.y - 1.0, Clean0, Clean1, Clean2, Clean3, Clean4) * step(1.0, home.y);
	float cleanMid = pick(home.y, Clean0, Clean1, Clean2, Clean3, Clean4) * step(0.0, home.y);
	float cleanDown = pick(home.y + 1.0, Clean0, Clean1, Clean2, Clean3, Clean4) * step(-1.0, home.y);
	float2 inCell = q - home;
	float toLeft = lerp(inCell.x - CLEAN_GAP, 10.0, bitOf(cleanMid, home.x - 1.0));
	float toRight = lerp(1.0 - inCell.x - CLEAN_GAP, 10.0, bitOf(cleanMid, home.x + 1.0));
	float toTop = lerp(inCell.y - CLEAN_GAP, 10.0, bitOf(cleanUp, home.x));
	float toBottom = lerp(1.0 - inCell.y - CLEAN_GAP, 10.0, bitOf(cleanDown, home.x));
	float2 toSides = float2(min(toLeft, toRight), min(toTop, toBottom));
	float cleanInside = CLEAN_ROUND - length(max(CLEAN_ROUND - toSides, 0.0));
	cleanInside = min(cleanInside, min(toSides.x, toSides.y));
	cleanInside += CLEAN_WOBBLE * sin(q.x * 7.0 + q.y * 3.0 + time * 1.4) * sin(q.y * 6.0 - q.x * 2.0 - time * 1.1);
	float homeClean = bitOf(cleanMid, home.x);
	cleanInside = lerp(-1.0, cleanInside, homeClean);

	// is the cell that the pixel is in part of a made constellation?
	float madeRow = pick(home.y, Made0, Made1, Made2, Made3, Made4) * step(0.0, home.y);
	float claimed = bitOf(madeRow, home.x);

	// the edge goes between its two colors, slowly, along the board, and bright spots run along it
	float swing = .5 + .5 * sin((q.x * .9 - q.y * .7) + time * 1.6);
	float3 edge = lerp(EDGE_A, EDGE_B, swing);
	edge = lerp(edge, STARLIGHT, claimed * .4);
	float runner = pow(.5 + .5 * sin((q.x + q.y) * 2.2 - time * 2.6), 10.0);

	// inside: the cloud, dark, and threads of light drifting through it
	float2 world = Area.xy + px + .5;
	float checker = frac((px.x + px.y) * .5) * 2.0;
	float3 cloud = cloudAt(world, time, checker);
	float threads = sin(q.x * 9.0 + time * 1.1 + sin(q.y * 7.0 - time * .8) * 1.6);
	threads = saturate((threads - .55) * 3.0);
	float3 color = lerp(DEEP, cloud, CLOUD_NIGHT) + THREAD * threads * .35;
	float alpha = .92;

	// the bright edge
	float onEdge = saturate(1.0 - abs(d + RIM * .5) / (RIM * .5));
	onEdge = step(.35, onEdge);
	color = lerp(color, edge * (1.0 + runner * .8), onEdge * step(d, 0.0));

	// outside: the glow, in steps, with a checkerboard between them
	float glow = exp(-max(d, 0.0) * GLOW_REACH) * GLOW_BRIGHT * (.5 + .5 * saturate(Ink.y));
	float glowStep = saturate(floor(glow * GLOW_STEPS + checker * .5) / GLOW_STEPS);
	float outside = step(0.0, d);
	float3 halo = lerp(HALO_A, HALO_B, swing);
	halo = lerp(halo, STARLIGHT, claimed * .3);
	color = lerp(color, halo, outside);
	alpha = lerp(alpha, glowStep, outside);

	// a made constellation shimmers when it is made: a sweep of starlight goes over it
	float sweep = .5 + .5 * sin((q.x + q.y) * 9.0 - time * 10.0);
	float shimmer = saturate(Ink.w) * claimed * sweep * step(d, .08);
	color = lerp(color, STARLIGHT, shimmer);
	alpha = max(alpha, shimmer);

	// BY DAY. the inside is the dried ink, with a lighter edge a pixel wide, a little of the night's
	// threads still drifting through it, and a thin dark line of shadow around the outside of it.
	float insidePx = -d * cellPx;
	float dayIn = step(0.0, insidePx);
	float dayRim = step(insidePx, DAY_EDGE_PX + .5) * dayIn;
	float3 dayColor = lerp(cloud + THREAD * threads * .06, DAY_EDGE, dayRim);
	dayColor = lerp(dayColor, DAY_SHADOW, 1.0 - dayIn);
	float dayAlpha = DAY_ALPHA * dayIn + (1.0 - dayIn) * step(-insidePx, 1.5) * .3;

	// the reflection of the fish of the cell, under it, in the ink
	float belowPx = (q.y - home.y - .5) * cellPx - Day.y * REFLECT_LINE;
	float reach = Day.y * REFLECT_REACH;
	float sway = floor(sin(world.y * .9 + time * 2.6) * 1.2 + .5);
	float2 mirror = float2(world.x + sway, world.y - 2.0 * belowPx - 1.0);
	float4 fish = tex2D(ShadowTextureSampler, mirror / max(Area.zw, 1.0));
	float rows = .55 + .45 * step(.5, frac((world.y - floor(time * 5.0)) * .5));
	float mirrorAmt = fish.a * step(0.0, belowPx) * saturate(1.0 - belowPx / max(reach, 1.0)) * rows * REFLECT_ALPHA * Day.z;
	dayColor = lerp(dayColor, lerp(fish.rgb, REFLECT_TINT, REFLECT_TINT_AMOUNT), mirrorAmt * (1.0 - dayRim) * dayIn);

	// it goes from the night's look to the day's as day breaks
	color = lerp(color, dayColor, day);
	alpha = lerp(alpha, dayAlpha, day);

	// CLEAN, under the ink: a pale edge, a faint light inside, and a glow behind the fish of each cell,
	// which shimmers a little. the wave of the pot brightens each as it goes by, and leaves it gold.
	float cleanPx = cleanInside * cellPx;
	float cleanIn = step(0.0, cleanPx) * step(0.0, d);
	float cleanRim = step(cleanPx, CLEAN_RIM_PX);
	float2 fromMiddle = q - home - .5;
	float spot = exp(-dot(fromMiddle, fromMiddle) * CLEAN_SPOT_TIGHT) * homeClean;
	spot *= .85 + .15 * sin(time * 2.2 + home.x * 1.3 + home.y * 2.1);
	float waveFar = length(home - Wave.xy);
	float waveOff = waveFar - Wave.z;
	float wavePulse = exp(-waveOff * waveOff * 1.2) * step(0.0, waveOff);
	float3 cleanColor = lerp(CLEAN_LIGHT, float3(1.0, 1.0, 1.0), wavePulse * .6);
	// the band bends as it goes, like light through rippling water
	float sheenBend = sin(world.y * SHEEN_RIPPLE + time * 1.1) * SHEEN_WAVE
	                + sin(world.x * SHEEN_RIPPLE * .6 - world.y * .03 - time * .7) * SHEEN_WAVE * .6;
	float sheenAt = frac((world.x + world.y * .6 + sheenBend - time * SHEEN_SPEED) / SHEEN_APART);
	float sheen = step(abs(sheenAt - .5), .007) + step(abs(sheenAt - .52), .003) * .6;
	float2 glintCell = floor(world / GLINT_APART);
	float2 glintIn = world - glintCell * GLINT_APART;
	float glintSeed = hash21(glintCell);
	float glintOn = saturate(sin(time * GLINT_SLOW * (1.0 + glintSeed * 2.0) + glintSeed * 40.0) * 6.0 - 5.0);
	float glintShape = step(abs(glintIn.x - 3.0) + abs(glintIn.y - 3.0), 1.0 + glintOn * .6) * step(GLINT_RARE, glintSeed);
	float glint = glintShape * glintOn;
	cleanColor = lerp(cleanColor, float3(1.0, 1.0, 1.0), saturate(sheen + glint));
	float cleanAlpha = CLEAN_FILL + cleanRim * CLEAN_RIM + spot * CLEAN_SPOT + sheen * SHEEN_BRIGHT + glint * GLINT_BRIGHT;
	cleanAlpha = saturate(cleanAlpha * (1.0 + wavePulse * 1.6)) * cleanIn * saturate(Wave.w);
	float under = cleanAlpha * (1.0 - alpha);
	float bothAlpha = alpha + under;
	color = (color * alpha + cleanColor * under) / max(bothAlpha, .001);
	alpha = bothAlpha;

	// it fades away once it has popped
	alpha *= saturate(Ink.z) * tex2D(SpriteTextureSampler, input.Quad).a;
	return float4(color, alpha) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
