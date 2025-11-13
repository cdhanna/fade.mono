// https://github.com/MonoGame/MonoGame/blob/v3.8.3/MonoGame.Framework/Platform/Graphics/Effect/Resources/SpriteEffect.fx
//-----------------------------------------------------------------------------
// SpriteEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

//#include "Macros.fxh"
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D<float4> Texture : register(t0); 
sampler TextureSampler : register(s0);

float4x4 MatrixTransform;

#define PI 3.14159265

struct VSOutput
{
	float4 position		: SV_Position;
	float4 color		: COLOR0;
    float2 texCoord		: TEXCOORD0;
    float4 custom       : TEXCOORD1;
};

VSOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0, 
								float4 custom   : TEXCOORD1)
{
	VSOutput output;
    output.position = mul(position, MatrixTransform);
    float4 worldPos = mul(position, MatrixTransform);
	output.color = color;
	//output.position.x += 1;
	output.texCoord = texCoord;
	float2 uv = texCoord;
	// float2 ScreenSize = float2(1920.0, 1080.0);
	float2 ScreenSize = float2(320.0, 320.0);
	float2 TextureSize = ScreenSize;
	if (custom.x > 0.0){
	    TextureSize = custom.xy;
	}
	output.custom = custom;

	return output;
}


float4 SpritePixelShader(VSOutput input) : SV_Target0
{

    float4 x = tex2D(TextureSampler, input.texCoord) * input.color;
   
    return x;
}

technique SpriteBatch
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
        PixelShader = compile PS_SHADERMODEL SpritePixelShader();
    }
};
