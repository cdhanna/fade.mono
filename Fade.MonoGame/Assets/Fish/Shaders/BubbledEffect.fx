#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE BUBBLED FISH (see fish_headers_bubbled). a cartoon bubble, the kind that floats out of a bubble
// wand: clear in the middle, one thin rim that wobbles and shimmers pink and blue, and a curved white
// shine at the top left. the fish inside is drawn by FishEffect, which shows it through the bubble.
// the sprite is a little bigger than the bubble, so that the rim has room to wobble.

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

	// x is the size of the sprite in pixels, so that lines can be measured in pixels.
	// y is how thick the rim is, in pixels.
	// z is the time, in seconds, plus a number of the bubble's own, so that no two wobble together.
	// w is how big the bubble is, from the middle of the sprite to its edge, where 1 is the edge.
	float4 Custom : TEXCOORD1;

	// x is how hard the rim wobbles, 1 when it has settled, and more while it grows in.
	// y is how far it has burst, from 0, whole, to 1, gone.
	float4 Custom2 : TEXCOORD2;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1,
								float4 custom2  : TEXCOORD2)
{
	VertexShaderOutput output;
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.TextureCoordinates = texCoord;
	output.Custom = custom;
	output.Custom2 = custom2;
	return output;
}

// 1 on a line of this width (in the units of d) around radius r, and 0 away from it
float lineAt(float d, float r, float width)
{
	return step(abs(d - r), width * .5);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 p = (input.TextureCoordinates - .5) * 2;
	float d = length(p);
	float a = atan2(p.y, p.x);
	float t = input.Custom.z;
	float pixel = 2.0 / max(input.Custom.x, 1.0);

	// the bubble is not quite round, and it wobbles
	float shake = max(input.Custom2.x, 1.0);
	float burst = saturate(input.Custom2.y);
	float bodyR = input.Custom.w * (1 + (sin(a * 3 + t * 2.1) * .035 + sin(a * 2 - t * 1.4) * .025 + sin(a * 5 + t * 2.9) * .012) * shake);

	float inside = step(d, bodyR);
	float rimWide = pixel * max(input.Custom.y, .5);
	float rim = lineAt(d, bodyR - rimWide * .5, rimWide);

	// the rim shimmers between pink and blue, and the colors go around it
	float shimmer = sin(a * 2 + t * 1.5) * .5 + .5;
	float3 rimColor = lerp(float3(.75, .92, 1.0), float3(1.0, .78, .93), shimmer);

	// the inside is clear, with a little color only close to the rim
	float near = saturate((d / bodyR - .75) / .25);
	float fill = (.04 + near * near * .18) * inside;

	// the shine, a curved stroke at the top left, and a dot under it
	float shineArc = lineAt(d, bodyR * .74, max(pixel * 1.5, bodyR * .07)) * step(abs(a + 2.25), .45);
	float shineDot = step(length(p - float2(-.5, -.12) * bodyR), max(pixel * .9, bodyR * .06));
	float shine = max(shineArc, shineDot) * inside;

	float3 color = lerp(float3(.8, .93, 1.0), rimColor, max(rim, near * .6));
	color = lerp(color, float3(1, 1, 1), shine);

	// A BURST. the rim breaks up into pieces, more of them gone the further it has got, and they fly
	// off with the swell of the sprite. the inside and the shine go at once, and a few drops fly out.
	float pieces = step(burst * 1.3 - .15, sin(a * 7 + t * 3) * .5 + .5);
	rim *= lerp(1.0, pieces, step(.001, burst));
	float gone = 1 - saturate(burst * 4);
	fill *= gone;
	shine *= gone;
	float drops = 0;
	for (int k = 0; k < 6; k++)
	{
		float dropA = k * 1.0472 + .4;
		float2 dropAt = float2(cos(dropA), sin(dropA)) * (bodyR * (1 + burst * .5));
		drops = max(drops, step(length(p - dropAt), pixel * 1.4 * (1 - burst)));
	}
	drops *= step(.001, burst);

	float alpha = max(max(fill, rim * .9), max(shine * .85, drops));
	alpha *= 1 - burst * burst;
	return float4(color * input.Color.rgb, alpha * input.Color.a);
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
