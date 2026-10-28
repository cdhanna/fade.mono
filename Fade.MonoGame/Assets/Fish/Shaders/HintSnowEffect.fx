#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE HINTS IN THE SNOW. at night, little lights drift up through the water: the plankton, which
// SeabedEffect.fx draws. up to three constellations are hidden in it, as a few of those same lights on a
// little grid, which SeabedEffect.fx draws too, so that they are exactly the plankton. this draws the
// lines: every so often, faint lines sweep across one of them, joining up the lights of each piece, and
// fade. they are on the same grains, rising and swaying the same way. see THE HINTS IN THE SNOW in
// fish_headers_squid, and starSkyDraw. it is drawn onto one plain sprite that covers the whole screen,
// just on top of the sea floor.

float Time;      // in seconds, the same as SeabedEffect.fx has
float2 Size;     // how big the screen is, in pixels

// HintAt is the grain of the top left light of each, and how many squares of the shape wide and tall it
// is. a square is HINT_PITCH grains across. HintRowsA is which squares of its first four rows are stars,
// as bits, the first square being 1, and HintRowsB is the next four. HintShown is how much each is there,
// from 0 to 1, HintLines how bright its lines are, and HintSweep how far across the shape they have
// swept, from 0 to 1. HintMade is 1 for a constellation that has been made in the ink: it is gold.
float4 HintAt0;
float4 HintAt1;
float4 HintAt2;
float4 HintRowsA0;
float4 HintRowsA1;
float4 HintRowsA2;
float4 HintRowsB0;
float4 HintRowsB1;
float4 HintRowsB2;
float4 HintShown;
float4 HintLines;
float4 HintSweep;
float4 HintMade;

// these have to be the same as STAR_SNOW_PITCH and STAR_SNOW_GRAIN in fish_headers_squid
#define HINT_PITCH 5.0
#define HINT_GRAIN 2.0

// the plankton's color, from SeabedEffect.fx, and the gold of a made one
#define PlanktonColor float3(0.200, 0.950, 0.820)
#define HINT_GOLD float3(1.0, 0.85, 0.4)

// how bright the lines are, how much paler than the plankton they are, and how bright the gold over the lights of a made one is
#define HINT_LINE 0.6
#define HINT_LINE_MADE 0.7
#define HINT_LINE_PALE 0.3
#define HINT_GOLD_LIGHT 0.6

// how many grains a line stops short of the light at either end
#define HINT_LINE_GAP 1.0

float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
}

// 1 if this square of a hint is a star, from its rows, or 0. rel is the square, from the top left.
float hintBit(float4 rowsA, float4 rowsB, float4 at, float2 rel)
{
	float inside = step(0.0, rel.x) * step(0.0, rel.y) * step(rel.x, at.z - 1.0) * step(rel.y, at.w - 1.0);
	float4 near0 = 1.0 - saturate(abs(rel.y - float4(0.0, 1.0, 2.0, 3.0)) * 2.0);
	float4 near1 = 1.0 - saturate(abs(rel.y - float4(4.0, 5.0, 6.0, 7.0)) * 2.0);
	float row = dot(rowsA, near0) + dot(rowsB, near1);
	float bit = fmod(floor(row / exp2(max(rel.x, 0.0)) + 0.01), 2.0);
	return inside * step(0.5, bit);
}

// 1 if a grain is near the box of a hint
float hintNear(float4 at, float2 grain)
{
	float2 rel = grain - at.xy;
	float2 far = (at.zw - 1.0) * HINT_PITCH;
	return step(-1.0, rel.x) * step(-1.0, rel.y) * step(rel.x, far.x + 1.0) * step(rel.y, far.y + 1.0);
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	float2 px = floor(uv * Size);

	// where this pixel is among the grains of the plankton, which drift up and sway, exactly as
	// SeabedEffect.fx has them. f is in grains, and the grain is the whole part of it.
	float2 f = (px + float2(sin(Time * 0.2 + px.y * 0.02) * 6.0, Time * 5.0)) / HINT_GRAIN;
	float2 grain = floor(f);

	// which hint this is near, if any. their boxes never overlap.
	float in0 = hintNear(HintAt0, grain);
	float in1 = hintNear(HintAt1, grain) * (1.0 - in0);
	float in2 = hintNear(HintAt2, grain) * (1.0 - in0) * (1.0 - in1);
	float shown = in0 * HintShown.x + in1 * HintShown.y + in2 * HintShown.z;
	if (shown <= 0.0)
	{
		return float4(0.0, 0.0, 0.0, 0.0);
	}
	float4 at = HintAt0 * in0 + HintAt1 * in1 + HintAt2 * in2;
	float4 rowsA = HintRowsA0 * in0 + HintRowsA1 * in1 + HintRowsA2 * in2;
	float4 rowsB = HintRowsB0 * in0 + HintRowsB1 * in1 + HintRowsB2 * in2;
	float lines = in0 * HintLines.x + in1 * HintLines.y + in2 * HintLines.z;
	float sweep = in0 * HintSweep.x + in1 * HintSweep.y + in2 * HintSweep.z;
	float made = in0 * HintMade.x + in1 * HintMade.y + in2 * HintMade.z;

	// the lights of the shape are SeabedEffect.fx's. a made one's are gold, and this puts the gold on them.
	float2 rel = grain - at.xy;
	float2 square = floor(rel / HINT_PITCH + 0.0001);
	float2 sub = rel - square * HINT_PITCH;
	float onLight = step(abs(sub.x), 0.1) * step(abs(sub.y), 0.1);
	float light = onLight * hintBit(rowsA, rowsB, at, square) * made * HINT_GOLD_LIGHT;

	// the lines, from a light to the next one across or down, when it is a star too: they are of the
	// same piece. a line is one pixel thick, on the grains of the plankton, so that it steps along with the
	// lights, pixel for pixel. a grain is two pixels, and the line is on the first of them: the top row of a
	// grain for a line across, and the left column for a line down. it stops HINT_LINE_GAP grains short of
	// the light at either end, so that every light, and every line between two of them, can be made out.
	float2 between = step(HINT_LINE_GAP + 0.5, sub) * step(sub, HINT_PITCH - HINT_LINE_GAP - 0.5);
	float star = hintBit(rowsA, rowsB, at, square);
	float2 first = float2(frac(f.x) < 0.5 ? 1.0 : 0.0, frac(f.y) < 0.5 ? 1.0 : 0.0);
	float across = star * hintBit(rowsA, rowsB, at, square + float2(1.0, 0.0)) * step(abs(sub.y), 0.1) * between.x * first.y;
	float down = star * hintBit(rowsA, rowsB, at, square + float2(0.0, 1.0)) * step(abs(sub.x), 0.1) * between.y * first.x;
	float join = max(across, down);

	// they sweep across the shape, from its top left corner
	float far = max(at.z + at.w - 2.0, 1.0);
	float along = (square.x + square.y + (sub.x + sub.y) / HINT_PITCH + 1.0) / far;
	join *= saturate((sweep * 1.2 - along) * 6.0) * lines * lerp(HINT_LINE, HINT_LINE_MADE, made);

	float3 color = lerp(lerp(PlanktonColor, float3(1.0, 1.0, 1.0), HINT_LINE_PALE * saturate(join * 4.0)), HINT_GOLD, made);
	return float4(color, saturate(max(light, join)) * shown);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
