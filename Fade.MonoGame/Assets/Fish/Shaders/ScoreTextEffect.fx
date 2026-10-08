#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the score texts that pop out of the fish, which were drawn to their own render target. this is
// drawn over the whole board, so anything done here is done to all of them at once.
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

float4 MainPS(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float4 texts = tex2D(SpriteTextureSampler, uv);

	// the texts were alpha blended onto a see-through target, which multiplied their color by
	// their alpha, and their alpha by itself. undo both, so that blending them again comes out right.
	float alpha = sqrt(texts.a);
	float3 color = texts.rgb / max(alpha, 0.001);

	return float4(color, alpha) * tint;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
