#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// text in a font that is drawn at the size it was baked at. the letters of a font have smooth
// edges, and at this size the smoothing is most of the letter, so it reads as a blur. this snaps
// every pixel to all the way there, or not there at all. see SCORE_FONT_16 in fish.
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

// how much of a pixel a letter has to cover for the pixel to be drawn. lower makes the letters
// bolder, and higher makes them thinner.
#define Cover 0.5

float4 MainPS(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float4 glyph = tex2D(SpriteTextureSampler, uv);

	// the letters are stored with their color multiplied by their alpha. undo it, so that a pixel
	// that is kept is the full color. the icons of the font come through here too, with their own colors.
	float3 color = saturate(glyph.rgb / max(glyph.a, 0.001));

	return float4(color * tint.rgb, step(Cover, glyph.a) * tint.a);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
