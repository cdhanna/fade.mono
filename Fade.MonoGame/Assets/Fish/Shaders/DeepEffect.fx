#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the dark backdrop of the upgrade screen, which is one flat color. in the deep, it closes in from the
// corners until it hides the game completely. it is a sprite, under everything else on the upgrade screen,
// so unlike the Vignette of ScreenEffect.fx, it does not darken the cards and the words.

// 0 is the flat color, and 1 is all of the corners
float Deep;

// where the middle is, from 0 to 1 across and down, and how wide the screen is for its height
#define Middle float2(0.5, 0.55)
#define Aspect 1.78

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float2 fromMiddle = (uv - Middle) * float2(Aspect, 1.0);
	float corners = smoothstep(0.2, 0.75, length(fromMiddle)) * Deep;
	return float4(color.rgb, lerp(color.a, 1.0, corners));
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
