#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the tail that the cursor drags behind it in a quest: a soft ribbon of light along the path that
// it took, which fades and thins the further back it is, and bends where it turned. it is the
// trail that a hint leaves with the "listen" card, in SpecksEffect.fx, on a sprite of its own, so
// that a quest and a hint can each have one at the same time. the sprite covers the whole screen,
// and its color is the color of the ribbon. see questTrailDraw in fish_routines_quests.

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// how big the screen is, in pixels of the game
float2 Size;

// twelve spots along the path, from where the cursor is to the far end of the tail. x and then y of each.
float4 Trail0;
float4 Trail1;
float4 Trail2;
float4 Trail3;
float4 Trail4;
float4 Trail5;

// how wide the ribbon is at the cursor, in pixels, from the middle of it to its edge
float Wide;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	VertexShaderOutput output;
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	return output;
}

// one piece of the path, from a to b, and how much of the ribbon this pixel gets from it, from 0
// to 1. piece is which one it is, counting from 0 at the cursor, out of 11. how old the path is where
// the pixel is nearest to it says how wide and how bright the ribbon is there, and that runs on
// smoothly from one piece into the next, so the pieces do not show.
float trailPiece(float2 px, float2 a, float2 b, float piece)
{
	float2 along = b - a;
	float back = saturate(dot(px - a, along) / max(dot(along, along), 0.001));
	float dist = length(px - (a + along * back));
	float fresh = saturate(1.0 - (piece + back) / 11.0);
	float wide = max(Wide * (0.35 + 0.65 * fresh), 1.0);
	float close = saturate(1.0 - dist / wide);
	return close * close * (0.3 + 0.7 * fresh);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// whole pixels, so that it is as blocky as the rest of the game
	float2 px = floor(input.TextureCoordinates * Size);

	float most = trailPiece(px, Trail0.xy, Trail0.zw, 0.0);
	most = max(most, trailPiece(px, Trail0.zw, Trail1.xy, 1.0));
	most = max(most, trailPiece(px, Trail1.xy, Trail1.zw, 2.0));
	most = max(most, trailPiece(px, Trail1.zw, Trail2.xy, 3.0));
	most = max(most, trailPiece(px, Trail2.xy, Trail2.zw, 4.0));
	most = max(most, trailPiece(px, Trail2.zw, Trail3.xy, 5.0));
	most = max(most, trailPiece(px, Trail3.xy, Trail3.zw, 6.0));
	most = max(most, trailPiece(px, Trail3.zw, Trail4.xy, 7.0));
	most = max(most, trailPiece(px, Trail4.xy, Trail4.zw, 8.0));
	most = max(most, trailPiece(px, Trail4.zw, Trail5.xy, 9.0));
	most = max(most, trailPiece(px, Trail5.xy, Trail5.zw, 10.0));

	return float4(input.Color.rgb, input.Color.a * most);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
