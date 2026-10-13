#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the zones that a quest marks on the board: a faint wash of color with an outline around it that
// the water pushes about. the sprite is a plain rectangle of any size, and the color of the sprite
// is the color of the zone. see questZone in fish_routines_quests.

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// seconds. the game sets this on every frame.
float Time;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;

	// x is how much of the sprite's alpha the wash inside gets, and y is how much the outline gets.
	// z sets this zone's waves apart from the next one's, and w is how deep the waves are, in pixels.
	float4 Custom : TEXCOORD1;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1)
{
	VertexShaderOutput output;
	position.xy = floor(position.xy + .5);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	output.Custom = custom;
	return output;
}

// how far the water has pushed the edge in at a spot along it, from 0 to about 2. it is one
// wave that runs along the edge, so that it reads as a wave. a slow sway on top of it stretches
// the wave in some places and bunches it up in others, and makes it deeper and shallower, so that
// it is not the same all the way along, and never quite repeats.
float Swell(float along, float clock, float apart)
{
	float sway = sin(along * .07 - clock * .5 + apart * 2.3);
	float wave = sin(along * .36 + clock * 1.5 + apart + sway * 1.3);
	return 1 + wave * (.65 + .3 * sway);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// where this pixel is in the rectangle, and how big the rectangle is, both in pixels
	float2 uv = input.TextureCoordinates;
	float2 perPixel = max(fwidth(uv), .0001);
	float2 p = uv / perPixel;
	float2 size = 1 / perPixel;

	// every one of the four edges is pushed in by swells of its own, which run along it
	float deep = input.Custom.w;
	float apart = input.Custom.z;
	float fromLeft = p.x - deep * Swell(p.y, Time, apart);
	float fromRight = size.x - p.x - deep * Swell(p.y, Time * .9, apart + 7.1);
	float fromTop = p.y - deep * Swell(p.x, Time * 1.1, apart + 3.3);
	float fromBottom = size.y - p.y - deep * Swell(p.x, Time * .8, apart + 11.7);
	float fromEdge = min(min(fromLeft, fromRight), min(fromTop, fromBottom));

	// outside of the wave there is nothing. the first pixel or so inside of it is the outline.
	float inside = step(0, fromEdge);
	float outline = inside * (1 - step(1.3, fromEdge));
	float alpha = saturate(input.Color.a * lerp(input.Custom.x, input.Custom.y, outline));
	return float4(input.Color.rgb, alpha * inside);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
