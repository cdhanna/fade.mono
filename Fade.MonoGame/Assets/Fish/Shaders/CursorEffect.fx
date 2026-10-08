#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float4x4 MatrixTransform;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;

	// rgb is the color of the outline, and a is how thick the outline is in pixels.
	float4 Custom : TEXCOORD1;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1)
{
	VertexShaderOutput output;

	// the corners of the square land on whole pixels. the square is centered on its spot, so when it
	// is an odd number of pixels wide its edges would fall halfway through a pixel, and then the
	// outline shows on two of its sides and is hit and miss on the other two.
	position.xy = floor(position.xy + .5);

	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	output.Custom = custom;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// how far this pixel is from the nearest edge of the square, in pixels. the square can be any size,
	// and it changes size all the time, so this is worked out from how fast the coordinates change.
	float2 uv = input.TextureCoordinates;
	float2 fromEdge = min(uv, 1 - uv) / max(fwidth(uv), .0001);

	// the middle of the first pixel in from an edge is half a pixel away from it, and the next one is
	// a pixel and a half away. the cut off sits between the two, not right on one of them, so that a
	// tiny rounding error cannot tip a whole side of the outline in or out.
	float outline = step(min(fromEdge.x, fromEdge.y), input.Custom.a - .25);

	// the color of the sprite fills the square, and the outline goes around it
	float4 fill = input.Color;
	float4 edge = float4(input.Custom.rgb, 1);
	return lerp(fill, edge, outline);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
