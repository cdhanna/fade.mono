#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif


extern float4 BorderColor;
extern float4 CenterColor;
extern float2 UvOffset;
extern float4x4 MatrixTransform;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

extern Texture2D CenterTexture;
sampler2D CenterTextureSampler = sampler_state
{
	Texture = <CenterTexture>;
};
extern Texture2D BorderTexture;
sampler2D BorderTextureSampler = sampler_state
{
	Texture = <BorderTexture>;
};


struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 Custom : TEXCOORD1;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 c = tex2D(SpriteTextureSampler,input.TextureCoordinates) * input.Color;

    float m = c.r <= .95;
    float lineMask = c.r <= .05;

    float4 centerValue = tex2D(CenterTextureSampler, input.TextureCoordinates + UvOffset + 5.0 * (input.Custom.xy - .5)) * CenterColor;
    float4 borderValue = tex2D(BorderTextureSampler, input.TextureCoordinates) * BorderColor;

    c.rgb = lerp(borderValue.rgb, centerValue.rgb, m);
    c.rgb = lerp(c.rgb, 0, lineMask) * input.Color.rgb;
    c.rgb *= c.a;
    
    //c.rgb *= input.Custom.x;
    
    return c;
}

technique SpriteDrawing
{
    pass P0
    {
        //VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
