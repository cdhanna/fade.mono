#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the text of a catch, which was drawn to its own render target: the same as ScoreTextEffect.fx, and the
// letters are all the colors of the rainbow, which run across them. the dark shadow under them stays dark.
// see fish_headers_wanted.
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

float Time;      // in seconds

float4 MainPS(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float4 texts = tex2D(SpriteTextureSampler, uv);

	// the texts were alpha blended onto a see-through target, which multiplied their color by
	// their alpha, and their alpha by itself. undo both, so that blending them again comes out right.
	float alpha = sqrt(texts.a);
	float3 color = texts.rgb / max(alpha, 0.001);

	// the light parts are the letters, and the dark parts are the shadow
	float letter = saturate((dot(color, float3(0.3, 0.59, 0.11)) - 0.25) * 3.0);
	float along = uv.x * 9.0 + uv.y * 4.0 - Time * 1.6;
	float3 hue = 0.5 + 0.5 * cos(6.2832 * (along + float3(0.0, 0.33, 0.67)));
	color = lerp(color, saturate(hue * 1.1 + 0.15), letter);

	return float4(color, alpha) * tint;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
