#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// a fish out in the water behind the board, in a school. see THE SCHOOLS IN THE BACKGROUND in
// fish_headers_school. far off, the dark line around a fish is lost in the water, so the darkest
// pixels of its picture fade away, and what is left is the color of it, which the sprite tints with
// the haze.
//
//   texcoord1.x  how far out it is, from 0, just off of the board, to 1, all the way out. the line
//                fades as it goes.
//   texcoord1.y  how much of the line is left when it is all the way out, from 0 to 1
//   texcoord1.z  1 for a rainbow fish, whose edge shimmers in the rainbow

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// a pixel of the picture that is darker than this is part of the line around the fish, or its eye.
// the same as NIGHT_LINE in FishEffect.fx.
#define LINE_DARK 0.16
// how far one pixel of the picture is, for the rim of a rainbow fish: one of the 16 across of the old art
#define RIM_STEP (1.0 / 16.0)

// the time, in seconds, for the shimmer of a rainbow fish. the game sets it every frame.
float Time;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
	float4 Custom : TEXCOORD1;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1)
{
	VertexShaderOutput output;
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	output.Custom = custom;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 c = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	float shade = dot(c.rgb, float3(0.299, 0.587, 0.114));
	float line0 = step(shade, LINE_DARK);
	float out0 = saturate(input.Custom.x);
	float keep = saturate(input.Custom.y);
	c.a *= 1.0 - line0 * out0 * (1.0 - keep);
	float4 result = c * input.Color;

	// A RAINBOW FISH out in a school: the empty pixels right around its picture shimmer in the colors of the
	// rainbow, which go around it, and every pixel twinkles in its own time. see fish_headers_rainbow.
	float2 uv = input.TextureCoordinates;
	float near = max(max(tex2D(SpriteTextureSampler, uv + float2(RIM_STEP, 0.0)).a, tex2D(SpriteTextureSampler, uv - float2(RIM_STEP, 0.0)).a),
	                 max(tex2D(SpriteTextureSampler, uv + float2(0.0, RIM_STEP)).a, tex2D(SpriteTextureSampler, uv - float2(0.0, RIM_STEP)).a));
	float rim = step(0.5, near) * (1.0 - step(0.5, c.a)) * step(0.5, input.Custom.z);
	float2 fromMiddle = uv - 0.5;
	float3 hue = 0.5 + 0.5 * cos(6.2832 * (atan2(fromMiddle.y, fromMiddle.x) / 6.2832 + Time * 0.5 + float3(0.0, 0.33, 0.67)));
	float twinkle = 0.55 + 0.45 * sin(Time * 7.0 + (uv.x * 37.0 + uv.y * 23.0) * 3.0);
	float rimLight = rim * twinkle * input.Color.a;
	result.rgb = lerp(result.rgb, saturate(hue + 0.2), rimLight);
	result.a = max(result.a, rimLight);
	return result;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
