// https://github.com/MonoGame/MonoGame/blob/v3.8.3/MonoGame.Framework/Platform/Graphics/Effect/Resources/SpriteEffect.fx
//-----------------------------------------------------------------------------
// SpriteEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
//
// The engine's baseline sprite shader. Baked to an XNB and embedded in
// Fade.MonoGame.Game.dll at build time (see BakeEngineSpriteEffect), so it must
// compile under whichever profile $(FadeMonoGamePlatform) selects — the bake
// output is per-platform, but there is only ever one of it in the assembly.

#include "FadeMacros.fxh"

DECLARE_TEXTURE(Texture, 0);

float4x4 MatrixTransform;

#define PI 3.14159265

struct VSOutput
{
	float4 position		: POSITION_OUT;
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
	output.color = color;
	output.texCoord = texCoord;
	output.custom = custom;

	return output;
}


float4 SpritePixelShader(VSOutput input) : TARGET0
{
    return SAMPLE_TEXTURE(Texture, input.texCoord) * input.color;
}

TECHNIQUE(SpriteBatch, SpriteVertexShader, SpritePixelShader);
