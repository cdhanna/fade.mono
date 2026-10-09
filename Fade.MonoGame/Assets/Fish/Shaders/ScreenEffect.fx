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

// how thick a ring is, from the middle of it to either edge, and how far it pushes the picture at its
// strongest. both are in pixels of the game. the push is small, because it is only meant to be felt.
#define RIPPLE_WIDTH 18.0
#define RIPPLE_PUSH 1.0

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

    float result1 = makeWaves(uv2 + float2(Time * TimeSpeed, 0.0), Time, 0.1);
    float result2 = makeWaves(uv2 - float2(Time * 0.8 * TimeSpeed, 0.0), Time * 0.8 + 0.06, 0.26);

    result1 = smoothstep(0.4, 1.1, 1.0 - abs(result1));
    result2 = smoothstep(0.4, 1.1, 1.0 - abs(result2));

    float result = 2.0 * smoothstep(0.35, 1.8, (result1 + result2) * 0.5);

    float2 p = float2(result, result2) * 0.001 + 0.05 * sin(uv * 16.0 - cos(uv.yx * 16.0 + Time * TimeSpeed)) * 0.1;

    // the patch of calm water. the wobble dies away in it.
    float calm = wake(uv);
    p *= 1.0 - calm * WAKE_STILL;

    // and it wobbles less over the coral
    p *= 1.0 - coralUnder(uv) * CORAL_STILL;

    // how much of this pixel is under the water
    float water = lerp(Submerged, tex2D(MaskTextureSampler, uv).r, UseMask);
    water = lerp(water, step(0.012, underLine(uv)), WaterLine.w);

    // the rings go through the same water that the ripples of light are in, so they are not perfect
    // circles. the wobble bends them out of shape, and they push harder where the light is bright
    // and hardly at all where it is dark.
    float2 ringUv = uv + p * RIPPLE_BEND;
    float3 rings = ripple(ringUv, Ripple0) + ripple(ringUv, Ripple1) + ripple(ringUv, Ripple2) + ripple(ringUv, Ripple3)
                 + ripple(ringUv, Ripple4) + ripple(ringUv, Ripple5) + ripple(ringUv, Ripple6) + ripple(ringUv, Ripple7);
    rings.xy *= 0.45 + 1.6 * result;

    float4 tex = tex2D(SpriteTextureSampler, uv + (p + rings.xy) * water);
    float hud = smoothstep(0.990, 0.998, dot(tex.rgb / max(length(tex.rgb), 0.001), HUD_BLUE)) * smoothstep(0.6, 0.78, tex.b);
    tex.rgb += HUD_LIFT * hud * HudGlow * water;

    // the ripples of light fade out over the calm water too
    float4 finalColor = float4(.45, 0.4, .8, 1.0) * result * 0.08 * water * (1.0 - calm * WAKE_DIM) + tex;

    // the crest of a ring catches a little light
    finalColor.rgb += float3(.35, .55, .7) * min(rings.z, 1.0) * 0.025 * water;

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