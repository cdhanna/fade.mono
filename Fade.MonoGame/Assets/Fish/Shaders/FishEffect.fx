#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Draws a fish as a little card in 3d, that can tilt, with more copies of it stacked up behind.
//
// Everything is per sprite, so it all comes in on the two extra texcoords.
//
//   texcoord1.x  pitch, in radians. positive tips the top of the fish away, so that it faces up.
//                half a turn puts the fish upside down, with its back showing.
//   texcoord1.y  yaw, in radians. positive turns the fish to face the right.
//   texcoord1.z  how many fish are in the stack. the part after the decimal point slides the last
//                one out from behind the one in front of it, so a count can be animated.
//   texcoord1.w  the size of the sprite in pixels. the quad grows by this much, so that there is
//                room to draw the tilt and the stack outside of the sprite. 0 leaves the quad alone.
//
//   texcoord2.xy how far to push the fish out of its place, in pixels. it gets rounded to whole pixels.
//   texcoord2.z  how much bigger to draw the fish, as if it lifts up toward the eye. 0 is its own
//                size, and 1 is twice as big. less than 0 draws it smaller: -.5 is half the size.
//   texcoord2.w  how much brighter to draw it. 0 is its own color, and 1 is twice as bright.
//                -1 is a fish that does not glow at night. see THE NIGHT, further down. at night,
//                a fish that is drawn brighter glows more, too.
//                -2 is a fish in the light of the diver's flashlight, which is drawn the way that it is in the day.
//                10 more than that is a fish that shimmers around its edge at night, which is a jellyfish.
//                2000 more than all of the rest is a rainbow fish: its line is the rainbow, in waves that go
//                across the screen, and its edge shimmers in it. see A RAINBOW FISH and THE WAVES OF A RAINBOW
//                FISH, further down, and fish_headers_rainbow.
//                1000 more than all of the rest is a fish that is flooded with the colors of the rainbow, day
//                or night: they wash through it in a wave that crosses the screen, and its edge shimmers in
//                them, the way that a jellyfish's does at night. those are the rainbow fish of a burst, and
//                the fish that they take along. see THE RAINBOW FLOOD, further down, and fish_headers_rainbow.
//                100 more than all of that is a fish with a black line drawn around it, one pixel wide,
//                just outside its picture. that is for art from the CustomArt folder that has no line
//                of its own. see fish_headers_customart.

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// THE NIGHT. x is how much it is night, from 0 to 1, and y is the time, in seconds. see THE NIGHT DIVE in
// fish_headers_tide. at night a fish is bioluminescent: its body is dimmer, but it keeps its own
// colors, so that the kinds can still be told apart. the dark line around it lights up in the color
// of the fish, and the pale parts of it are a little brighter. there is a halo around it, in the
// same color. the light comes and goes in waves that cross the board. a fish that does not glow
// only gets dimmer.
// the game sets this on every frame. a parameter that the game never sets does not keep a value that
// it is given here, so the things that never change are #defines instead.
float2 Night;
// THE RAINBOW FLOOD of a catch. x is where its band of light starts, along the wave, and y where it ends: the
// near and far ends of the group. z is how far through its one sweep it has got, from 0 to 1, and w is how
// wide the band is, along the wave. the game works these out, in fish_routines_rainbow.
float4 Rainbow;
// A RAINBOW FISH, and how it looks, set by the game every frame so that it can be played with in the debug window.
// x is how strongly the edge outside of its line shimmers, from 0 to 1. y is how many times a second the
// colors go around it. z is how much of its line is the rainbow in place of its dark color, from 0 to 1.
// w is how pale the colors are: 0 is the full colors, and 1 is nearly white. see fish_headers_rainbow.
float4 RainbowRim;
// THE WAVES OF A RAINBOW FISH. the colors of the rainbow fish come from where they are on the screen, and the
// time, so that the same wave of color goes through every rainbow fish that it crosses, and fish that are
// near each other look joined. x is the way that the waves go, as an angle. y is how long a wave is, in
// pixels, from one end of the rainbow to the same color again. z is how many steps the colors come in, for
// bands with hard edges, or 0 for colors that blend. w is how much the waves bend and ripple, from 0 for
// straight. RainbowRim.y is how many waves go by in a second.
float4 RainbowWave;


// what is left of the color of the body of a fish at night
#define NIGHT_BODY float3(0.52, 0.57, 0.70)

// a pixel of the picture that is darker than this is part of the line around the fish, or its eye
#define NIGHT_LINE 0.16

// the light of every fish leans toward this color, by this much
#define NIGHT_LIGHT float3(0.25, 1.00, 0.90)
#define NIGHT_LEAN 0.10

// a fish is this many pixels across
#define NIGHT_PIXELS 16.0
// THE RAINBOW FLOOD. how far along the wave a pixel of the screen is, across and down. this is
// RAINBOW_POP_WAVE_X# and RAINBOW_POP_WAVE_Y# in fish_headers_rainbow, and the two have to be kept the same by hand.
// how fast the colors inside the band run, how soft the band's edge is (the higher, the harder), and how much
// of the rainbow there is, away from the band and in it.
#define RAINBOW_WAVE   float2(0.0060, 0.0035)
#define RAINBOW_SPEED  1.2
#define RAINBOW_SHARP  1.5
#define RAINBOW_HIGH   0.95
// THE RAINBOW FLOOD: how much of a fish the rainbow covers at the top of the band, and how far behind
// the band a fish that it has gone over stays a little lit, fading out, in widths of the band
#define FLOOD_COVER  1.0
#define FLOOD_TRAIL  1.2
#define FLOOD_TRAIL_LIGHT 0.25
// how many pixels of the screen a fish takes, across, for the waves of a rainbow fish: CELL_ART_SIZE
#define FISH_SCREEN_PIXELS 16.0


// how many pixels across the new art is. the thick line around it is worked out on these. see THE THICK LINE.
#define FINE_PIXELS 32.0

// the color of the thick line: deep water, rgb(14, 22, 44). softer against the sea than black. see THE THICK LINE.
#define THICK_LINE_COLOR float3(14.0 / 255.0, 22.0 / 255.0, 44.0 / 255.0)

// how far out the halo goes, where 1 is the size of a fish, and how strong it is at the middle
#define NIGHT_HALO_REACH 0.95
#define NIGHT_HALO 0.70

// the waves of light: how fast they come, and how far apart they are across the screen and down it, in pixels
#define NIGHT_WAVE_SPEED 1.9
#define NIGHT_WAVE float2(0.040, 0.027)

// how far each fish in a stack sits from the one in front of it, where 1 is the size of a fish.
// this is on the face of the card, so it tilts along with it.
#define STACK_SHIFT float2(.16, -.16)

// how far behind each one is, the same way. this is what a tilt shows off.
#define STACK_DEPTH .15

// how bright each one is, next to the one in front of it. 1 does not darken at all.
#define STACK_SHADE .6

// the quad is this many times as big as the sprite
#define QUAD_GROW 2.0

// how far away the eye is, where 1 is the size of a fish. closer makes a stronger perspective.
#define EYE_DISTANCE 3.0

#define MAX_STACK 4

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;

	// where the pixel is on the quad. the fish fills -.5 to .5 when it lies flat.
	float2 Quad : TEXCOORD0;

	// the card. these are the directions of its right side, its bottom side, and its face.
	float3 AxisU : TEXCOORD1;
	float3 AxisV : TEXCOORD2;
	float3 Normal : TEXCOORD3;

	// x is how many fish are in the stack, y is how much brighter to draw, z is how much it is night,
	// and w is how brightly the fish glows right now, which is 0 for a fish that does not
	float4 CountGlow : TEXCOORD4;

	// x is 1 for a fish that shimmers around its edge at night, and 2 or more for one that is flooded with the
	// rainbow. y is 1 for one with a black line around it. z is where the fish is along the wave of the
	// rainbow, from where it is on the screen. w is 1 for a rainbow fish.
	float4 Flags : TEXCOORD5;

	// where the middle of the fish is on the screen, in pixels, for the waves of a rainbow fish
	float2 Screen : TEXCOORD6;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1,
								float4 custom2  : TEXCOORD2)
{
	VertexShaderOutput output;

	// the texcoord says which corner of the sprite this is
	float2 corner = texCoord - .5;

	// push the corners out, to make room. the picture on the quad stays the same, so pushing them
	// out further than that is what makes the fish bigger.
	float grow = (QUAD_GROW - 1.0) * step(.5, custom.w);
	float lift = 1.0 + max(custom2.z, -0.9) * step(.5, custom.w);
	position.xy += corner * custom.w * ((1.0 + grow) * lift - 1.0);

	// the push lands on whole pixels. a fish that sits part of the way between two pixels gets some
	// of its own pixels doubled up and some dropped.
	position.xy += floor(custom2.xy + .5);
	output.Quad = corner * (1.0 + grow);

	float sinP = sin(custom.x);
	float cosP = cos(custom.x);
	float sinY = sin(custom.y);
	float cosY = cos(custom.y);

	// x goes right, y goes down, and z comes out of the screen
	output.AxisU = float3(cosY, 0, -sinY);
	output.AxisV = float3(sinP * sinY, cosP, sinP * cosY);
	output.Normal = float3(cosP * sinY, -sinP, cosP * cosY);

	// the wave of light gets to every fish at its own time, by where the middle of the fish is on the screen
	float2 middle = position.xy - corner * custom.w * (1.0 + grow) * lift;
	float wave = 0.5 + 0.5 * sin(Night.y * NIGHT_WAVE_SPEED - dot(middle, NIGHT_WAVE));
	// 200 more says that it has the thick line of the 32 pixel art around it, and 100 more the line of
	// a 16 pixel picture. the rest is read from what is left. see THE LINE AROUND IT.
	float rainbowFish = step(1500.0, custom2.w);
	custom2.w -= 2000.0 * rainbowFish;
	float dayShimmer = step(500.0, custom2.w);
	custom2.w -= 1000.0 * dayShimmer;
	float thick = step(150.0, custom2.w);
	custom2.w -= 200.0 * thick;
	float outline = step(50.0, custom2.w);
	custom2.w -= 100.0 * outline;
	float glows = step(-0.5, custom2.w);

	// -2 says that the fish is in the light of the diver's flashlight, and it is not night for that fish
	float lamp = step(custom2.w, -1.5);

	// 10 or more says that it shimmers, and what is left over is how much brighter it is
	float shimmers = step(5.0, custom2.w);
	float brighter = max(custom2.w - 10.0 * shimmers, 0.0);

	output.CountGlow = float4(clamp(custom.z, 1.0, MAX_STACK), brighter, saturate(Night.x) * (1.0 - lamp), glows * (0.62 + 0.38 * wave) * (1.0 + 1.6 * brighter));
	output.Flags = float4(shimmers + 2.0 * dayShimmer, outline + 2.0 * thick, dot(middle, RAINBOW_WAVE), rainbowFish);
	output.Screen = middle;

	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	return output;
}

// 1 where the picture has something in it, at a spot on the quad. this is the picture lying flat, which
// is close enough for the pixels right next to a fish.
float solidAt(float2 quad)
{
	float2 uv = quad + 0.5;
	float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
	return step(0.5, tex2D(SpriteTextureSampler, uv).a) * inside;
}

// one fish of the stack. index 0 is the one in front.

// THE WAVES OF A RAINBOW FISH. the color of the rainbow at this place on the screen, now. see RainbowWave.
float3 rainbowAt(float2 screenPx)
{
	float2 way = float2(cos(RainbowWave.x), sin(RainbowWave.x));
	float2 side = float2(-way.y, way.x);
	float wave = max(RainbowWave.y, 1.0);
	float along = dot(screenPx, way) / wave;
	along += RainbowWave.w * 0.25 * sin(dot(screenPx, side) / (wave * 0.6) * 6.2832 + Night.y * 0.9);
	along -= Night.y * RainbowRim.y;
	float steps = RainbowWave.z;
	along = (steps >= 1.0) ? floor(along * steps) / steps : along;
	float3 hue = 0.5 + 0.5 * cos(6.2832 * (along + float3(0.0, 0.33, 0.67)));
	return lerp(hue, float3(1.0, 1.0, 1.0), RainbowRim.w * 0.6);
}

// THE RAINBOW FLOOD, at this place along the wave: how much of the rainbow is on it, from 0 to 1. a band
// sweeps once across the screen, from one end of the group to the other (Rainbow), and covers what it is
// over, so only part of the group is rainbow at a time. it leaves a short trail behind it that fades out.
// for a burst, the band is so wide that it is everywhere at once. see THE RAINBOW FLOOD, further down.
float floodLit(float along)
{
	float wide = max(Rainbow.w, 0.001);
	float bandAt = lerp(Rainbow.x - Rainbow.w, Rainbow.y + Rainbow.w, saturate(Rainbow.z));
	// flat across the top, so all of a fish is covered while the band is over it, and only its ends fade
	float band = saturate((1.0 - abs(along - bandAt) / wide) * 2.5);
	float behind = (bandAt - along) / wide;
	float trail = step(0.0, behind) * saturate(1.0 - behind / FLOOD_TRAIL) * FLOOD_TRAIL_LIGHT;
	return max(band, trail) * step(0.0, Rainbow.z);
}

float4 SampleLayer(VertexShaderOutput input, float index)
{
	// 1 when this fish is all the way there, and less while it is still sliding out
	float there = saturate(input.CountGlow.x - index);
	float slot = max(index - 1.0 + there, 0.0);

	// look from the eye, through the pixel, and find where that hits the card of this fish
	float facing = input.Normal.z * EYE_DISTANCE;
	float across = dot(input.Normal.xy, input.Quad);

	// a card that is turned past edge on shows its back, which is the same picture turned over.
	// the stack stays on the far side of it.
	float behind = slot * STACK_DEPTH * (facing < 0.0 ? -1.0 : 1.0);
	float reach = facing - across;
	reach = abs(reach) < .001 ? .001 : reach;
	float t = (facing + behind) / reach;
	float3 hit = float3(input.Quad * t, EYE_DISTANCE * (1.0 - t));

	float2 uv = float2(dot(input.AxisU, hit), dot(input.AxisV, hit)) - slot * STACK_SHIFT;

	float4 c = tex2D(SpriteTextureSampler, uv + .5);
	c.a *= step(max(abs(uv.x), abs(uv.y)), .5);

	// THE LINE AROUND IT. an empty pixel with a pixel of the picture right next to it is black. this is
	// on the face of the card, so the line tilts and turns over with the fish. the pixels are the ones
	// that the fish is drawn at, NIGHT_PIXELS across, whatever the size of the picture.
	float one = 1.0 / NIGHT_PIXELS;
	float2 cell = (floor((uv + .5) * NIGHT_PIXELS) + .5) / NIGHT_PIXELS - .5;
	float beside = max(max(solidAt(cell + float2(one, 0.0)), solidAt(cell - float2(one, 0.0))), max(solidAt(cell + float2(0.0, one)), solidAt(cell - float2(0.0, one))));
	float line1 = beside * step(c.a, .5) * step(.5, input.Flags.y) * step(input.Flags.y, 1.5);

	// THE THICK LINE, for the 32 pixel art. the line above lands on the corners between its pixels,
	// and comes out one of them wide. this one is worked out on the picture's own pixels instead: an
	// empty one that is 1 or 2 from the picture, straight across or down, or 1 from it on a slant, is
	// the color of deep water. 2 of them is one pixel of the old art, so the new fish get the weight of
	// line that the old ones have, and it follows their own shape, with rounded corners. it was picked
	// from a few widths, shapes and colors tried side by side. see THE NEW ART in fish_headers_customart.
	float fine = 1.0 / FINE_PIXELS;
	float near = max(max(solidAt(uv + float2(fine, 0.0)), solidAt(uv - float2(fine, 0.0))), max(solidAt(uv + float2(0.0, fine)), solidAt(uv - float2(0.0, fine))));
	near = max(near, max(max(solidAt(uv + float2(2.0 * fine, 0.0)), solidAt(uv - float2(2.0 * fine, 0.0))), max(solidAt(uv + float2(0.0, 2.0 * fine)), solidAt(uv - float2(0.0, 2.0 * fine)))));
	near = max(near, max(max(solidAt(uv + float2(fine, fine)), solidAt(uv - float2(fine, fine))), max(solidAt(uv + float2(fine, -fine)), solidAt(uv - float2(fine, -fine)))));
	float line2 = near * step(c.a, .5) * step(1.5, input.Flags.y);

	// a rainbow fish can have its line in the colors of the rainbow, which go around it. see A RAINBOW FISH.
	// a fish that a rainbow fish takes along with it, as they burst, has the rainbow line too
	float2 linePx = input.Screen + uv * FISH_SCREEN_PIXELS;
	float lineFlooded = step(1.5, input.Flags.x);
	float3 lineHue = rainbowAt(linePx);
	float lineLit = floodLit(input.Flags.z + dot(input.Quad, float2(0.05, 0.03)));
	float lineRainbow = saturate(RainbowRim.z) * max(input.Flags.w, lineFlooded * saturate(lineLit * 2.0));
	c = lerp(c, float4(lerp(float3(0, 0, 0), lineHue, lineRainbow), 1), line1);
	c = lerp(c, float4(lerp(THICK_LINE_COLOR, lineHue, lineRainbow), 1), line2);
	c.a *= step(.001, there);

	// the ones in the back are darker, so that the one in front stands out
	c.rgb *= pow(STACK_SHADE, slot);
	return c;
}

// a number from 0 to 1 that is different for every spot
float hash(float2 p)
{
	float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
}

// the color of the light that a fish of this color gives off: as bright as that color gets, and a little toward NIGHT_LIGHT
float3 lightOf(float3 color)
{
	float top = max(color.r, max(color.g, color.b));
	return lerp(color / max(top, 0.001), NIGHT_LIGHT, NIGHT_LEAN);
}

// what a spot of the picture adds to the color of the whole fish. the more color a spot has, the more it counts.
float3 tasteOf(float2 spot)
{
	float4 c = tex2D(SpriteTextureSampler, spot);
	float rich = max(c.r, max(c.g, c.b)) - min(c.r, min(c.g, c.b));
	return c.rgb * c.a * (rich * rich + 0.03);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// back to front
	float4 result = float4(0, 0, 0, 0);
	for (int i = MAX_STACK - 1; i >= 0; i--)
	{
		float4 layer = SampleLayer(input, i);
		float alpha = layer.a + result.a * (1.0 - layer.a);
		result.rgb = (layer.rgb * layer.a + result.rgb * result.a * (1.0 - layer.a)) / max(alpha, .001);
		result.a = alpha;
	}

	// a fish that is told to glow gets brighter. nothing else here changes its colors, until it is night.
	// at night most of that goes into its light, further down, so that the fish itself is not washed out.
	result.rgb *= 1.0 + input.CountGlow.y * (1.0 - 0.8 * input.CountGlow.z);

	// THE NIGHT. the color of the light of this fish is the color of the fish as a whole, which is
	// looked up in a few spots of its picture.
	float night = input.CountGlow.z;
	float glow = input.CountGlow.w;
	float3 taste = tasteOf(float2(0.50, 0.50)) + tasteOf(float2(0.34, 0.50)) + tasteOf(float2(0.66, 0.50)) + tasteOf(float2(0.50, 0.36)) + tasteOf(float2(0.50, 0.64));
	float3 light = lightOf(taste);

	// the body gets dimmer, and keeps its colors. the dark line around the fish lights up in the
	// color of the fish, and so does its eye. the pale parts of it are a little brighter.
	float shade = dot(result.rgb, float3(0.299, 0.587, 0.114));
	float line0 = step(shade, NIGHT_LINE);
	float pale = smoothstep(0.42, 0.80, shade);
	float2 pixel = floor((input.Quad + 0.5) * NIGHT_PIXELS);
	float3 lit = result.rgb * NIGHT_BODY * (1.0 + 0.55 * pale * glow);
	lit += light * line0 * 0.90 * glow;
	result.rgb = lerp(result.rgb, lit, night);

	// and the halo, around the fish and behind it, in the same color. it comes in steps, with a
	// checkerboard between one step and the next, like the light of the sea floor does, so that it
	// has no hard edge.
	float reach = saturate(1.0 - length(input.Quad) / NIGHT_HALO_REACH);
	float checker = frac((pixel.x + pixel.y) * 0.5) * 2.0;
	float halo = floor(reach * reach * reach * 9.0 + checker * 0.5) / 9.0 * NIGHT_HALO * night * glow;
	float withHalo = result.a + halo * (1.0 - result.a);
	result.rgb = (result.rgb * result.a + light * halo * (1.0 - result.a)) / max(withHalo, 0.001);
	result.a = withHalo;



	// THE SHIMMER, for a jellyfish. the pixels right around the edge of its picture light up, the ones
	// that are empty and have a pixel of the picture next to them. a bright stretch goes around the
	// edge, and every pixel of it twinkles in its own time, so it never holds still.
	float one = 1.0 / NIGHT_PIXELS;
	float2 spot = (pixel + 0.5) / NIGHT_PIXELS - 0.5;
	float beside = max(max(solidAt(spot + float2(one, 0.0)), solidAt(spot - float2(one, 0.0))), max(solidAt(spot + float2(0.0, one)), solidAt(spot - float2(0.0, one))));
	float rim = beside * (1.0 - solidAt(spot));
	float around = atan2(spot.y, spot.x);
	float sweep = 0.5 + 0.5 * sin(around * 2.0 - Night.y * 3.2);
	float twinkle = 0.5 + 0.5 * sin(Night.y * 9.0 + hash(pixel + 29.0) * 40.0);
	float flooded = step(1.5, input.Flags.x);
	float shimmerOn = saturate(saturate(input.Flags.x) * night + flooded);
	float shimmer = rim * saturate(0.25 + 0.75 * sweep * sweep) * (0.45 + 0.55 * twinkle) * shimmerOn;
	float3 shimmerColor = lerp(light, float3(1.0, 1.0, 1.0), 0.6);

	// THE RAINBOW FLOOD. as the band goes over a fish, all of it turns to the rainbow, not just its edge: the
	// colors run across it, and every pixel of it flickers in its own time, so it shimmers. see floodLit.
	float along = input.Flags.z + dot(input.Quad, float2(0.05, 0.03));
	float band = floodLit(along);
	float3 hue = 0.5 + 0.5 * cos(6.2832 * (along * 6.0 - Night.y * RAINBOW_SPEED + input.Quad.x * 0.15 + float3(0.0, 0.33, 0.67)));
	float sparkle = 0.75 + 0.25 * sin(Night.y * 9.0 + hash(pixel + 7.0) * 40.0);
	float floodShade = dot(result.rgb, float3(0.299, 0.587, 0.114));
	float3 floodColor = saturate(hue * (0.55 + 0.75 * floodShade) + 0.08) * sparkle;
	float wash = flooded * result.a * saturate(band * FLOOD_COVER);
	result.rgb = lerp(result.rgb, floodColor, wash);
	shimmer *= lerp(1.0, band, flooded);
	shimmerColor = lerp(shimmerColor, saturate(hue + 0.35), flooded);

	// A RAINBOW FISH. its edge always shimmers, faintly, in the colors of the rainbow, which go slowly around
	// it, so that it can be told from the others at a glance. the flood is stronger, and wins where it is.
	float3 rimHue = rainbowAt(input.Screen + spot * FISH_SCREEN_PIXELS);
	float fishRim = rim * (0.35 + 0.25 * twinkle) * saturate(RainbowRim.x) * input.Flags.w;
	float rimWins = step(shimmer, fishRim);
	shimmerColor = lerp(shimmerColor, saturate(rimHue + 0.15), rimWins);
	shimmer = max(shimmer, fishRim);


	result.rgb = lerp(result.rgb, shimmerColor, shimmer);
	result.a = max(result.a, shimmer);

	return result * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
