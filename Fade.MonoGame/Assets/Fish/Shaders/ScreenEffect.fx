#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float2 Example;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};
extern Texture2D Noise;
sampler2D NoiseSampler = sampler_state
{
	Texture = <Noise>;
};
float Time;

// THE TIDE. how much of the time has to be taken back for the wobble of the water, in seconds. the
// game adds to it, faster than the time goes, for as long as the tide wants a push, so the wobble runs
// backward then. see THE DRIFT in fish_headers_tide. it is 0 until the game sets it.
float TideBack;
float2 Resolution;
float TimeSpeed = 50.0;

// the wobble and the ripples of light are only for what is under the water.
// the bubble mask is 1 where the water is, while the wave of bubbles is going between the surface and the game.
Texture2D MaskTexture;
sampler2D MaskTextureSampler = sampler_state
{
	Texture = <MaskTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

// 1 to look at the mask to find the water. 0 when the whole screen is either under the water or above it.
float UseMask;

// and this is which. 1 is under the water, 0 is above it.
float Submerged;

// the line of water that drains down the screen when the game comes back up to the main menu.
// this is the same line that MenuCompositeEffect.fx draws, and they have to match.
float4 WaterLine;
float2 Grid;

float underLine(float2 uv)
{
	float2 grid = max(Grid, 1.0);
	float2 p = (floor(uv * grid) + 0.5) / grid;
	float y = WaterLine.x + (p.x - 0.5) * WaterLine.y + sin(p.x * 9.0 + WaterLine.z * 2.6) * 0.022 + sin(p.x * 23.0 - WaterLine.z * 3.7) * 0.009;
	return p.y - y;
}

// the rings that a move or a pop sends out across the board. the water bulges as one goes by, so the
// picture is pushed away from the middle of the ring on the front of it, and pulled back on the back of it.
// x and y are the middle of the ring, from 0 to 1 across the screen. z is how big it has got, in pixels
// of the game, and w is how strong it is, from 0 to 1. a ring that is not going has no strength.
// these are the same rings that tilt the fish. see frame_ripples, in fish_routines_seabed.
// the pushes of all of them are added up, so rings that cross each other both show.
float4 Ripple0;
float4 Ripple1;
float4 Ripple2;
float4 Ripple3;
float4 Ripple4;
float4 Ripple5;
float4 Ripple6;
float4 Ripple7;
// THE SURGE OF A BURST of the rainbow fish: one bigger ring, whose edge is ragged, that goes out from the cursor.
// x and y are where it starts, as a share of the screen, z is how far it has got, in pixels, and w is how
// strong it is, from 0 to 1, or 0 for none. see THE BURST in fish_headers_rainbow.
float4 Surge;
// THE RAINBOW CLOUDS. when a fish turns into a rainbow fish, or a rainbow fish bursts, a cloud of rainbow
// light claps out of it into the water: a ring that grows quickly and slows, and fades as it goes. it is
// not drawn on its own: it is the ripples of light that the water already has (makeWaves), turned to the
// colors of the rainbow and made brighter where the ring is, and only on the water, not the fish.
// x and y of each are where it claps out from, in pixels of the game, z is when, in seconds on the clock
// of CloudLook.z, and w is how big it gets, where 1 is CLOUD_REACH. a w of 0 is no cloud.
// see THE RAINBOW CLOUDS in fish_headers_rainbow.
float4 Cloud0;
float4 Cloud1;
float4 Cloud2;
float4 Cloud3;
float4 Cloud4;
float4 Cloud5;
float4 Cloud6;
float4 Cloud7;
// x is how strong they are, from 0 to 1, and 0 when none is going, so that none of it is worked out.
// y is how long one lasts, in seconds, and z is now, in seconds.
float4 CloudLook;

// how thick a ring is, from the middle of it to either edge, and how far it pushes the picture at its
// strongest. both are in pixels of the game. the push is small, because it is only meant to be felt.
#define RIPPLE_WIDTH 18.0
#define RIPPLE_PUSH 1.0
// THE RAINBOW CLOUDS: how far one goes out, and how wide its ring is at the start, both in pixels of the
// game for a cloud of size 1, and how much brighter the ripples of light get in one
#define CLOUD_REACH 70.0
#define CLOUD_WIDE  16.0
#define CLOUD_LIGHT 1.3

// THE SURGE: how wide its ring is, how hard it pushes, and how far its edge goes in and out, in pixels
#define SURGE_WIDTH 24.0
#define SURGE_PUSH 2.2
#define SURGE_RAGGED 9.0

// how much the wobble of the water bends a ring out of shape. 0 leaves it a perfect circle.
#define RIPPLE_BEND 5.0

// something small that swims along under the water, now and then. nothing is drawn for it. the water is
// still where it is: the wobble dies away there, and so do the ripples of light, so there is a patch of
// calm, clear water that moves along. x and y are the middle of it, from 0 to 1 across the screen. z is
// how big it is, in pixels of the game, from the middle to the edge, and w is how strong it is, from
// 0 to 1. see THE HINTS in fish_headers.
float4 Wake;

// how much of the wobble is taken away in the very middle of the patch, and how much of the ripples of
// light. 1 is all of it, and 0 is none.
#define WAKE_STILL 1.0
#define WAKE_DIM 0.7

// the water wobbles less over the coral, too. see CORAL in fish_headers. this is the same picture of
// where the coral is that CoralEffect.fx is given: a white square for every cell of the board that has
// coral on it. Board and Cells say where the board is: xy is its top left corner, in pixels of the
// game, and z is how big a cell is, and then how many columns and rows it has.
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

// how much of the wobble is taken away over coral. 1 is all of it, and 0 is none.
#define CORAL_STILL 0.75

// the words and the bars around the board get brighter along with the water, during a run of pops.
// they are all drawn in one blue, so this finds them by their color: anything that is that blue, and
// bright, is lightened. 0 leaves them alone, and 1 is as light as they get.
float HudGlow;
#define HUD_BLUE float3(0.2182, 0.4364, 0.8729) // rgb(64, 128, 255), as a direction
#define HUD_LIFT float3(0.42, 0.34, 0.0)        // what is added to it at the most

// the QR code that the fish make when TUNA is typed. see TUNA in fish_headers.
// a pool of light falls on the water, and the fish in it go dark, like fish seen against the light.
// there is a fish for every dark module of the code, and open water where a module is light, so a
// phone reads the dark fish on the light water.
// the pool has a soft edge, and the edge is not straight: it reaches further out where the ripples of
// light are bright, so it moves with them.
// xy is the middle of the pool, in pixels of the game, z is how far it goes from the middle to an
// edge, and w is how much of it shows, from 0 to 1.
float4 Qr;

// how much of the shadows of the fish show, from 0 to 1. the pool of light comes first, and the
// shadows come on in it as the fish get to their places.
float QrDeep;

// the color of the light, and what a fish in it is multiplied by. the light is faint: the water in
// it is only a little lighter than the water around it, so the pool does not stand out from the
// sea. it does not have to be bright. it only has to be lighter than the fish and their shadows.
#define QR_LIGHT float3(0.06, 0.13, 0.20)
#define QR_FISH float3(0.08, 0.10, 0.17)

// where the fish of the code are: the game draws a black square for every one, on white, the same
// size as the screen.
Texture2D QrTexture;
sampler2D QrTextureSampler = sampler_state
{
	Texture = <QrTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

// a fish does not fill its module, and a phone wants the whole module dark. so every fish has a soft
// shadow under it: the water is darker there. QR_SHADOW is how much darker, where 0 is no shadow and 1
// is black. QR_BLUR is how soft the edge of a shadow is, in pixels of the game. how big a shadow is
// is TUNA_SHADOW, in fish_headers.
// with no shadow, or too faint or too small a one, a phone has a hard time with the code.
#define QR_SHADOW 0.7
#define QR_BLUR 1.5

// how much the light flickers with the ripples of light. 0 is a flat color.
#define QR_FLICKER 0.2

// how far the pool takes to fade out past its edge, in pixels of the game, and how much further out
// the brightest ripple of light pushes it
#define QR_SOFT 40.0
#define QR_REACH 10.0

// how much of the wobble of the water is taken away over the code, so that a phone can still read it.
// 1 is all of it, and 0 is none.
#define QR_STILL 0.9

// how much light there is on this pixel, from 0 in the middle of the shadow of a fish to 1 out of it
float qrLight(float2 uv)
{
    float2 one = QR_BLUR / max(Grid, 1.0);
    float lit = tex2D(QrTextureSampler, uv + float2(one.x, one.y)).r
              + tex2D(QrTextureSampler, uv + float2(-one.x, one.y)).r
              + tex2D(QrTextureSampler, uv + float2(one.x, -one.y)).r
              + tex2D(QrTextureSampler, uv + float2(-one.x, -one.y)).r;
    return lerp(1.0, smoothstep(0.2, 0.8, lit * 0.25), saturate(QrDeep));
}

// how much of the pool of light is on this pixel, from 0 to 1
float qrShade(float2 uv, float light)
{
    float2 away = abs(uv * max(Grid, 1.0) - Qr.xy) - Qr.z;
    float dist = length(max(away, 0.0)) + min(max(away.x, away.y), 0.0);
    dist -= saturate(light) * QR_REACH;
    return (1.0 - smoothstep(0.0, QR_SOFT, dist)) * saturate(Qr.w);
}

// what the corners of the screen are multiplied by, to make them darker and bluer than the middle
#define VIGNETTE_TINT float3(0.60, 0.67, 0.84)

// xy is how far a ring pushes this pixel, and z is how much of its crest is on this pixel
float3 ripple(float2 uv, float4 ring)
{
    float2 grid = max(Grid, 1.0);
    float2 away = (uv - ring.xy) * grid;
    float dist = max(length(away), 0.001);

    // this is 0 on top of the ring, 1 at the front edge of it, and -1 at the back edge
    float ahead = clamp((dist - ring.z) / RIPPLE_WIDTH, -1.0, 1.0);
    float strength = pow(saturate(ring.w), 0.6);

    float push = sin(ahead * 3.14159) * strength * RIPPLE_PUSH;
    float crest = (0.5 + 0.5 * cos(ahead * 3.14159)) * strength;
    return float3((away / dist) * push / grid, crest);
}

// THE SURGE. the same as a ring, but its edge goes in and out all the way around, by a few waves of
// different sizes, which turn as it goes, so that it is ragged and never the same twice.
float3 surge(float2 uv)
{
    // there is none most of the time, and then none of the rest has to be worked out
    if (Surge.w <= 0.0)
        return float3(0.0, 0.0, 0.0);
    float2 grid = max(Grid, 1.0);
    float2 away = (uv - Surge.xy) * grid;
    float dist = max(length(away), 0.001);
    float around = atan2(away.y, away.x);
    float rag = sin(around * 7.0 + Time * 3.1) * 0.5 + sin(around * 13.0 - Time * 2.3) * 0.3 + sin(around * 23.0 + Time * 4.7) * 0.2;
    float ahead = clamp((dist - (Surge.z + rag * SURGE_RAGGED)) / SURGE_WIDTH, -1.0, 1.0);
    float strength = saturate(Surge.w);
    float push = sin(ahead * 3.14159) * strength * SURGE_PUSH;
    float crest = (0.5 + 0.5 * cos(ahead * 3.14159)) * strength;
    return float3((away / dist) * push / grid, crest);
}

// THE RAINBOW CLOUDS. how much of the clouds there is on this pixel, and in what color, for the ripples of
// light of the water, which are `light` bright here (the same `result` that they are drawn with).
float3 rainbowClouds(float2 uv, float light)
{
    if (CloudLook.x <= 0.0)
        return float3(0.0, 0.0, 0.0);
    float2 px = floor(uv * max(Grid, 1.0));
    float life = max(CloudLook.y, 0.05);
    float4 clouds[8] = { Cloud0, Cloud1, Cloud2, Cloud3, Cloud4, Cloud5, Cloud6, Cloud7 };
    float best = 0.0;
    float2 bestAway = float2(1.0, 0.0);
    for (int i = 0; i < 8; i++)
    {
        float4 c = clouds[i];
        float age = CloudLook.z - c.z;
        float t = age / life;
        float live = step(0.0, t) * step(t, 1.0) * step(0.001, c.w);
        float2 away = px - c.xy;
        float dist = length(away);
        // it claps out quickly, and slows, and there is a puff in the middle at the start
        float reach = CLOUD_REACH * c.w * (1.0 - (1.0 - t) * (1.0 - t));
        float wide = CLOUD_WIDE * c.w * (0.6 + t);
        float ring = saturate(1.0 - abs(dist - reach) / max(wide, 1.0));
        float puff = saturate(1.0 - dist / (CLOUD_WIDE * 1.2)) * saturate(1.0 - t * 3.0);
        float one = max(ring, puff) * (1.0 - t) * (1.0 - t) * live;
        if (one > best)
        {
            best = one;
            bestAway = away;
        }
    }
    // its colors go around it, and turn as it goes
    float around = atan2(bestAway.y, bestAway.x);
    float3 hue = 0.5 + 0.5 * cos(6.2832 * (around / 6.2832 + CloudLook.z * 0.4 + float3(0.0, 0.33, 0.67)));
    // in steps, with a checkerboard between them, like the rest of the light of the sea
    float checker = frac((px.x + px.y) * 0.5) * 2.0;
    float lit = saturate(best * (0.15 + 1.5 * light) * CloudLook.x);
    lit = floor(lit * 6.0 + checker * 0.5) / 6.0;
    return saturate(hue * 0.9 + 0.25) * lit;
}

// how calm the water is on this pixel, from 0 to 1. it is calmest in the middle of the patch, and it
// goes back to how the water is everywhere else, smoothly, by the edge.
float wake(float2 uv)
{
    float2 grid = max(Grid, 1.0);
    float dist = length((uv - Wake.xy) * grid);
    float inside = saturate(1.0 - dist / max(Wake.z, 1.0));
    return inside * inside * (3.0 - 2.0 * inside) * saturate(Wake.w);
}

// how much coral a cell of the board has, from 0 to 1. a cell that is off of the board has none.
float coralCell(float2 cell)
{
    float onBoard = step(0.0, cell.x) * step(0.0, cell.y) * step(cell.x, Cells.x - 1.0) * step(cell.y, Cells.y - 1.0);
    float2 at = (Board.xy + (cell + 0.5) * Board.z) / max(Grid, 1.0);
    return tex2D(CoralTextureSampler, at).r * onBoard;
}

// how much coral there is under this pixel, from 0 to 1. it goes smoothly from the middle of one cell
// to the middle of the next, so that the wobble does not tear along the edges of the cells.
float coralUnder(float2 uv)
{
    float2 spot = (uv * max(Grid, 1.0) - Board.xy) / max(Board.z, 1.0) - 0.5;
    float2 cell = floor(spot);
    float2 f = spot - cell;
    f = f * f * (3.0 - 2.0 * f);
    float top = lerp(coralCell(cell), coralCell(cell + float2(1.0, 0.0)), f.x);
    float low = lerp(coralCell(cell + float2(0.0, 1.0)), coralCell(cell + float2(1.0, 1.0)), f.x);
    return lerp(top, low, f.y);
}

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float fract(float x)
{
    return x - floor(x);
}

float randomVal(float inVal)
{
    float2 v = float2(inVal, 2523.2361);
    float2 d = float2(12.9898, 78.233);
    return fract(sin(dot(v, d)) * 43758.5453) - 0.5;
}

float2 randomVec2(float inVal)
{
    float x = randomVal(inVal);
    float y = randomVal(inVal + 151.523);
    float2 v = float2(x, y);
    return normalize(v);
}

float makeWaves(float2 uv, float theTime, float offset)
{
    float result = 0.0;
    // UNROLLED, so that everything here that depends only on n (the direction, the frequency and
    // the height of each wave, four sin()s through randomVal) is worked out by the compiler, and
    // only the wave itself is left for every pixel. As a loop it was five sin()s a wave, 160 a
    // pixel, on a buffer three times the size of the game's: a heavy load for an integrated GPU.
    [unroll]
    for (int n = 0; n < 16; n++)
    {
        float i = n + offset;
        float2 randVec = randomVec2(i);
        float direction = dot(uv, randVec);
        float sineWave = sin(direction * randomVal(i + 1.6516) + theTime * TimeSpeed);
        sineWave = smoothstep(0.0, 1.0, sineWave);
        result += randomVal(i + 123.0) * sineWave;
    }
    return result;
}

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
   // float2 uv = texCoord * Resolution.xy;
   // uv /= Resolution.y; // TODO: this resolution scaling is not correct.
float2 uv = texCoord;
    //return float4(uv.xy, 0, 1);

    float2 uv2 = uv * 60.0;

    uv *= 1.0;

    // the wobble runs on a time of its own, which goes backward while the tide wants a push
    float flowTime = Time - TideBack;
    float result1 = makeWaves(uv2 + float2(flowTime * TimeSpeed, 0.0), flowTime, 0.1);
    float result2 = makeWaves(uv2 - float2(flowTime * 0.8 * TimeSpeed, 0.0), flowTime * 0.8 + 0.06, 0.26);

    result1 = smoothstep(0.4, 1.1, 1.0 - abs(result1));
    result2 = smoothstep(0.4, 1.1, 1.0 - abs(result2));

    float result = 2.0 * smoothstep(0.35, 1.8, (result1 + result2) * 0.5);

    float2 p = float2(result, result2) * 0.001 + 0.05 * sin(uv * 16.0 - cos(uv.yx * 16.0 + flowTime * TimeSpeed)) * 0.1;

    // the patch of calm water. the wobble dies away in it.
    float calm = wake(uv);
    p *= 1.0 - calm * WAKE_STILL;

    // and it wobbles less over the coral
    p *= 1.0 - coralUnder(uv) * CORAL_STILL;

    // and hardly at all over the QR code
    float shade = qrShade(texCoord, result);
    p *= 1.0 - shade * QR_STILL;

    // how much of this pixel is under the water
    float water = lerp(Submerged, tex2D(MaskTextureSampler, uv).r, UseMask);
    water = lerp(water, step(0.012, underLine(uv)), WaterLine.w);

    // the rings go through the same water that the ripples of light are in, so they are not perfect
    // circles. the wobble bends them out of shape, and they push harder where the light is bright
    // and hardly at all where it is dark.
    float2 ringUv = uv + p * RIPPLE_BEND;
    float3 rings = ripple(ringUv, Ripple0) + ripple(ringUv, Ripple1) + ripple(ringUv, Ripple2) + ripple(ringUv, Ripple3)
                 + ripple(ringUv, Ripple4) + ripple(ringUv, Ripple5) + ripple(ringUv, Ripple6) + ripple(ringUv, Ripple7)
                 + surge(ringUv);
    rings.xy *= 0.45 + 1.6 * result;

    float2 texUv = uv + (p + rings.xy) * water;
    float4 tex = tex2D(SpriteTextureSampler, texUv);
    float hud = smoothstep(0.990, 0.998, dot(tex.rgb / max(length(tex.rgb), 0.001), HUD_BLUE)) * smoothstep(0.6, 0.78, tex.b);
    tex.rgb += HUD_LIFT * hud * HudGlow * water;

    // the pool of light of the QR code. what has little red or green in it is the water, which is lit
    // a little, and the rest is fish, which go dark. the water is darker in the shadow of a fish.
    float qrWater = 1.0 - smoothstep(0.3, 0.6, max(tex.r, tex.g));
    float qrGlow = 1.0 - QR_FLICKER * 0.5 + QR_FLICKER * result;
    float3 qrLit = tex.rgb * lerp(QR_FISH, float3(1.0, 1.0, 1.0), qrWater) + QR_LIGHT * qrGlow * qrWater;
    qrLit *= lerp(1.0 - QR_SHADOW, 1.0, lerp(1.0, qrLight(texCoord), qrWater));
    tex.rgb = lerp(tex.rgb, qrLit, shade);

    // the ripples of light fade out over the calm water too
    float4 finalColor = float4(.45, 0.4, .8, 1.0) * result * 0.08 * water * (1.0 - calm * WAKE_DIM) + tex;

    // the crest of a ring catches a little light
    finalColor.rgb += float3(.35, .55, .7) * min(rings.z, 1.0) * 0.025 * water;

    // THE RAINBOW CLOUDS, in the ripples of light, on the water and not on the fish. what has little red
    // or green in it is the water, the same as for the QR code.
    float cloudWater = 1.0 - smoothstep(0.3, 0.6, max(tex.r, tex.g));
    finalColor.rgb += rainbowClouds(uv, result) * CLOUD_LIGHT * water * cloudWater;

    // the water gets darker and bluer toward the corners of the screen. it goes in steps, with a
    // checkerboard between one step and the next, on the pixels of the game.
    float2 cell = floor(uv * max(Grid, 1.0));
    float2 fromMiddle = ((cell + 0.5) / max(Grid, 1.0) - 0.5) * float2(2.3, 2.0);
    float corner = smoothstep(0.8, 1.6, length(fromMiddle));
    float checker = frac((cell.x + cell.y) * 0.5) * 2.0;
    corner = floor(corner * 12.0 + checker * 0.5) / 12.0;
    finalColor.rgb *= lerp(float3(1.0, 1.0, 1.0), VIGNETTE_TINT, corner * water);

   // finalColor.rg *= Example.rg;
    return finalColor;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};