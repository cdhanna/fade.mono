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

// the size of the sprite in pixels, so that the edge can be measured in pixels.
float2 Size = float2(64, 64);
float Radius = 5.0;
float Feather = 4.0;

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	// signed distance, in pixels, to the edge of a rounded box that fills the sprite.
	float2 p = (uv - .5) * Size;
	float2 q = abs(p) - (Size * .5 - Radius);
	float d = length(max(q, 0)) + min(max(q.x, q.y), 0) - Radius;

	// 1 inside the box, fading to 0 at the edge.
	float hole = saturate(-d / Feather);
	return float4(hole, hole, hole, 1);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};