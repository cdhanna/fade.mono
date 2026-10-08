#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// the menu, which was drawn to its own render target
Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

// the bubble mask. it is 1 where the wave of bubbles has been, and 0 everywhere else
Texture2D MaskTexture;
sampler2D MaskTextureSampler = sampler_state
{
	Texture = <MaskTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

// 0 when the menu is see-through, 1 when it is fully there
float Fade = 1.0;

// 0 shows the menu where the mask is, which brings the menu in.
// 1 shows the menu where the mask is not, which takes the menu away.
float Invert = 0.0;

// the line of water that drains down the screen when the game comes back up to the main menu.
// x is how far down the screen it is, y is how much it leans, z is the time in seconds, and
// w is 1 to use it in place of the mask. ScreenEffect.fx has the same line, and they have to match.
float4 WaterLine;

// how many pixels the game's screen is made of, so that the line is as blocky as everything else
float2 Grid;

#define FoamColor float3(0.80, 0.90, 1.00)

// how far under the line a spot is. it is less than 0 above the line.
float underLine(float2 uv)
{
	float2 grid = max(Grid, 1.0);
	float2 p = (floor(uv * grid) + 0.5) / grid;
	float y = WaterLine.x + (p.x - 0.5) * WaterLine.y + sin(p.x * 9.0 + WaterLine.z * 2.6) * 0.022 + sin(p.x * 23.0 - WaterLine.z * 3.7) * 0.009;
	return p.y - y;
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
	float4 menu = tex2D(SpriteTextureSampler, uv);
	float mask = tex2D(MaskTextureSampler, uv).r;

	float under = underLine(uv);
	mask = lerp(mask, step(0.0, under), WaterLine.w);

	// the top of the water is a bright line of foam, with a paler band of water underneath it
	float foam = max(step(under, 0.0085) * 0.95, step(under, 0.045) * 0.22);
	foam *= step(0.0, under) * WaterLine.w;

	// the menu was alpha blended onto a see-through target, which multiplied its color by its
	// alpha, and its alpha by itself. undo both, so that blending it again comes out right.
	float alpha = sqrt(menu.a);
	float3 color = menu.rgb / max(alpha, 0.001);

	float shown = lerp(mask, 1 - mask, Invert);
	alpha = alpha * shown * Fade;

	// the foam is in the water, where the menu is not
	color = lerp(color, FoamColor, step(0.001, foam));
	return float4(color, max(alpha, foam));
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
