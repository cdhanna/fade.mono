#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the credits, which were drawn to their own render target. this is drawn over the whole screen.
Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

// how far down the screen the credits start to show, and how far down they are all the way there.
// 0 is the top of the screen and 1 is the bottom. the hint sits underneath where they start.
// these are written into the shader, instead of being parameters, because a parameter that
// the game never sets does not keep the value it is given here.
#define BottomStart 0.915
#define BottomFull 0.79

// and the same for where they fade away again at the top
#define TopGone 0.02
#define TopFull 0.15

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	float4 credits = tex2D(SpriteTextureSampler, uv);

	// the credits were alpha blended onto a see-through target, which multiplied their color by
	// their alpha, and their alpha by itself. undo both, so that blending them again comes out right.
	float alpha = sqrt(credits.a);
	float3 color = credits.rgb / max(alpha, 0.001);

	// fade in on the way up from the bottom, and fade out on the way off of the top
	float fade = smoothstep(BottomStart, BottomFull, uv.y) * smoothstep(TopGone, TopFull, uv.y);

	return float4(color, alpha * fade);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
