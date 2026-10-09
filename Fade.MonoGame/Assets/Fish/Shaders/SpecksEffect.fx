#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the specks that drift down behind the fish. they used to be part of SeabedEffect.fx, which draws
// everything else that is behind the board. they are a sprite of their own now, so that they can be in
// front of the coral, which is also behind the fish. see ORDER_SPECKS in fish_headers.
// they are only light. a faint one lets nearly all of what is behind it show through.
// the specks that drift in front of the fish are another sprite again, with SnowEffect.fx.
// it is drawn onto one plain sprite that covers the whole screen, so there is no texture.

// the game sets all of these on every frame. a parameter that the game never sets does not keep
// a value that it is given here, so the things that never change are #defines instead.
float Time;      // in seconds
float2 Size;     // how big the screen is, in pixels
float Clock;     // the time that the specks move by, in seconds. it runs faster when the mood is high, so they speed up without jumping.
float Rainbow;   // 0 to 1. the specks turn the colors of the rainbow in a long run of pops, every time more fish pop.
float Mood;      // how long the run of pops that is going on is, from 0 to 1. there are more specks.

// something small that swims along behind the fish, now and then, and the specks glow where it is.
// see THE HINTS in fish_headers. xy is where it is, and zw is where it was a moment ago, both in pixels.
// the specks glow all the way from the one to the other, and a lot less toward where it was, so that
// the glow dies away quickly behind it. that is what says which way it is going.
float4 Glow;
float2 GlowPower; // x is how strong it is, from 0 to 1, or more with the "listen" card, and y is how far from it the specks glow, in pixels

// 1 when the player has the "listen" card, and the thing was brought by a big group of fish that
// popped. see LISTEN in fish_headers. it leaves a trail of light behind it, along the way that it came.
float Listen;

// the path that the thing took, for the trail: eight spots, from where it is now to where it was a
// while ago, each as long before the last as the one before. two spots in each of these, xy and then
// zw. the game rounds the corners of the path off, so the line through them bends where the thing turned.
float4 Trail0;
float4 Trail1;
float4 Trail2;
float4 Trail3;

// the trail is a ribbon of light along that path. this is how bright it is where the thing is, and how
// far to either side of the path it reaches there, as a share of how far the specks glow. it gets
// fainter and thinner the further back along the path it is, down to nothing at the far end.
#define TRAIL_BRIGHT 0.5
#define TRAIL_WIDE 0.34

// where the coral is, so that the specks of a hint can glow more over it. against the rock they are
// hard to see otherwise. this is the same picture that CoralEffect.fx is given: a square for every cell
// of the board that has coral on it. Board is where the board is: xy is its top left corner, in pixels,
// and z is how big a cell is. Cells is how many columns and rows it has.
Texture2D CoralTexture;
sampler2D CoralTextureSampler = sampler_state
{
	Texture = <CoralTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};
float4 Board;
float2 Cells;

// how many times as much a speck over coral glows, on top of what it does anyway
#define GLOW_OVER_CORAL 3.0

// how much brighter a speck gets when it is right where the thing is. a speck is 0.22 of this color
// to start with, so 2.5 makes it about twelve times as bright.
#define GLOW_SPECKS 2.5

#define SnowColor   float3(0.620, 0.780, 0.820)

// the sand that the board is over, at the bottom of the screen, is in front of the specks. this is
// the same ridge that SeabedEffect.fx draws, and the numbers have to match the ones there.
#define NearBase  0.945
#define NearRough 26.0
#define CamSway 16.0

// a number from 0 to 1 that is different for every pixel
float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
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

// how lit up this pixel is by the thing that is swimming by, from 0 to 1
float glowAt(float2 px)
{
	float2 along = Glow.zw - Glow.xy;
	float back = saturate(dot(px - Glow.xy, along) / max(dot(along, along), 0.001));
	float dist = length(px - (Glow.xy + along * back));
	float close = saturate(1.0 - dist / max(GlowPower.y, 1.0));
	return close * (2.0 - close) * (1.0 - back) * (1.0 - back) * clamp(GlowPower.x, 0.0, 4.0);
}

// one piece of the path that the thing took, from a to b, and how much of the trail this pixel gets
// from it, from 0 to 1. piece is which one it is, counting from 0 at the thing itself, out of 7.
// how old the path is where the pixel is nearest to it says how wide and how bright the trail is there,
// and that runs on smoothly from one piece into the next, so the pieces do not show.
float trailPiece(float2 px, float2 a, float2 b, float piece)
{
	float2 along = b - a;
	float back = saturate(dot(px - a, along) / max(dot(along, along), 0.001));
	float dist = length(px - (a + along * back));
	float fresh = saturate(1.0 - (piece + back) / 7.0);
	float wide = max(GlowPower.y * TRAIL_WIDE * (0.25 + 0.75 * fresh), 1.0);
	float close = saturate(1.0 - dist / wide);
	return close * close * fresh;
}

// the trail that the thing leaves behind it with the "listen" card: a soft ribbon of light along the
// path that it took, which bends where it turned. there is nothing in it that flickers or jumps.
float trail(float2 px)
{
	float most = trailPiece(px, Trail0.xy, Trail0.zw, 0.0);
	most = max(most, trailPiece(px, Trail0.zw, Trail1.xy, 1.0));
	most = max(most, trailPiece(px, Trail1.xy, Trail1.zw, 2.0));
	most = max(most, trailPiece(px, Trail1.zw, Trail2.xy, 3.0));
	most = max(most, trailPiece(px, Trail2.xy, Trail2.zw, 4.0));
	most = max(most, trailPiece(px, Trail2.zw, Trail3.xy, 5.0));
	most = max(most, trailPiece(px, Trail3.xy, Trail3.zw, 6.0));
	return most * TRAIL_BRIGHT;
}

// 1 if the cell of the board that this pixel is in has coral on it, and less while the coral is growing in
float coralUnder(float2 px)
{
	float2 cell = floor((px + 0.5 - Board.xy) / max(Board.z, 1.0));
	float onBoard = step(0.0, cell.x) * step(0.0, cell.y) * step(cell.x, Cells.x - 1.0) * step(cell.y, Cells.y - 1.0);
	float2 at = (Board.xy + (cell + 0.5) * Board.z) / Size;
	return tex2D(CoralTextureSampler, at).r * onBoard;
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	// whole pixels, so that everything is as blocky as the rest of the game
	float2 px = floor(uv * Size);

	// specks, far away
	float specks = snow(px, 19.0, float2(1.5, 3.0), 40.0, 0.7 + 0.3 * Mood, 1.0) * 0.5 + snow(px, 31.0, float2(3.0, 6.0), 80.0, 0.7 + 0.3 * Mood, 1.0 + 0.4 * Mood);

	// when the mood is high there are more of them, and the new ones cross the others the other way
	specks += snow(px, 23.0, float2(-2.5, 4.5), 160.0, Mood, 1.0) * Mood;
	specks *= 1.0 + 0.8 * Mood;

	// when the specks are rainbows, the colors run across the screen in slanted bands, and they are brighter
	float3 color = lerp(SnowColor, rainbow(px.x * 0.0045 + px.y * 0.0030 - Time * 0.12), Rainbow);
	float bright = lerp(0.22, 0.6, Rainbow) * specks;

	// where something is swimming by, the specks that are drifting through there glow. nothing is
	// added: no more specks, and no light in the water. it is only the ones that were there anyway.
	// they are whiter as well as brighter.
	float glow = glowAt(px);
	bright += specks * glow * GLOW_SPECKS * (1.0 + GLOW_OVER_CORAL * coralUnder(px));

	// with the "listen" card, the thing leaves a trail of light behind it, along the path that it took.
	// it fades the further back along the path it is. the trail is white, like the glow.
	float lit = trail(px) * saturate(GlowPower.x) * Listen;
	bright += lit;
	color = lerp(color, float3(1.0, 1.0, 1.0), saturate(lit * 2.0));
	color = lerp(color, float3(1.0, 1.0, 1.0), saturate(glow) * 0.45);

	// the sand at the bottom of the screen is in front of them
	float sway = sin(Time * 0.07) * CamSway * 0.6 + sin(Time * 0.031 + 1.0) * CamSway * 0.4;
	float nearTop = ground(px.x + floor(sway) + 700.0, NearBase, NearRough, 37.0);
	bright *= 1.0 - step(nearTop, px.y);

	// a speck is light, so how bright it is is how much of it there is over what is behind it
	return float4(color, saturate(bright));
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
