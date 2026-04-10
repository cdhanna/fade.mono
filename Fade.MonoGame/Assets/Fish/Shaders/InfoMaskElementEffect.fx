#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

Texture2D MaskTexture;
sampler2D MaskTextureSampler = sampler_state
{
	Texture = <MaskTexture>;
};

float Time;
float2 Resolution;
float TimeSpeed = 50.0;

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	float d = length(.5 - uv);

	float4 finalColor = float4(1 - (d*1.44), 1, 1, 1);
    return finalColor;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};