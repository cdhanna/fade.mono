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


extern Texture2D PreviousTexture;   // previous frame
sampler2D PreviousTextureSampler = sampler_state
{
	Texture = <PreviousTexture>;
};

float2 iResolution;  // passed from C# (RenderTarget size)
float decay = 0.98; // how quickly fire fades
float rise = -1.0;  // how fast fire moves upward

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // Get current and previous fire intensity
    float inputVal = tex2D(SpriteTextureSampler, uv).a;
    
    float3 color = inputVal;

    return float4(color, 1.0);
}


// float iTime;            // Time in seconds
// float2 iResolution;     // Screen size (resolution)

// // Constants
// static const float2x2 m = float2x2( 0.85, 0.60, -0.60, 0.80 );

// //-------------------------------------

// float noise(float2 p)
// {
//     return sin(p.x) * sin(p.y);
// }

// float fbm4(float2 p)
// {
//     float f = 0.0;
//     f += 0.5000 * noise(p); p = mul(m, p) * 2.02;
//     f += 0.2500 * noise(p); p = mul(m, p) * 2.03;
//     f += 0.1250 * noise(p); p = mul(m, p) * 2.01;
//     f += 0.0625 * noise(p);
//     return f / 0.9375;
// }

// float fbm6(float2 p)
// {
//     float f = 0.0;
//     f += 0.500000 * (0.5 + 0.5 * noise(p)); p = mul(m, p) * 2.02;
//     f += 0.250000 * (0.5 + 0.5 * noise(p)); p = mul(m, p) * 2.03;
//     f += 0.125000 * (0.5 + 0.5 * noise(p)); p = mul(m, p) * 2.01;
//     f += 0.062500 * (0.5 + 0.5 * noise(p)); p = mul(m, p) * 2.04;
//     f += 0.031250 * (0.5 + 0.5 * noise(p)); p = mul(m, p) * 2.01;
//     f += 0.015625 * (0.5 + 0.5 * noise(p));
//     return f / 0.96875;
// }

// float2 fbm4_2(float2 p)
// {
//     return float2(fbm4(p), fbm4(p + float2(7.8, 7.8)));
// }

// float2 fbm6_2(float2 p)
// {
//     return float2(fbm6(p + float2(16.8, 16.8)), fbm6(p + float2(11.5, 11.5)));
// }

// float func(float2 q, float m, out float4 ron)
// {
//     q += 0.03 * sin(float2(0.27, 0.23) * iTime + length(q) * float2(4.1, 4.3));

//     float2 o = fbm4_2(0.9 * q);

//     o += 0.04 * sin(float2(0.12, 0.14) * iTime + length(o));

//     float2 n = fbm6_2(3.0 * o);

//     ron = float4(o, n);

//     float f = 0.5 + 0.5 * fbm4(1.8 * q + 6.0 * n);

//     return lerp(f, f * f * f * 3.5, f * abs(n.x));
// }

// float random(float2 p)
// {
//     return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
// }

// float valNoise(float2 p)
// {
//     float2 i = floor(p);
//     float2 f = frac(p);

//     float a = random(i);
//     float b = random(i + float2(1.0, 0.0));
//     float c = random(i + float2(0.0, 1.0));
//     float d = random(i + float2(1.0, 1.0));

//     float2 u = f * f * (3.0 - 2.0 * f); // smoothstep interpolation

//     return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
// }

// struct VertexShaderOutput
// {
//     float4 Position : SV_POSITION;
//     float2 TexCoord : TEXCOORD0;
// };

// float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
// {
//     float2 fragCoord = input.TexCoord * iResolution;

// 	float4 t = tex2D(SpriteTextureSampler, input.TexCoord);
// 	float mask = t.a;

//     // float mask = t.b > .5;
//     // if (mask){
//     //     return float4(1, 0, 0, 1);
//     // }

//     float2 p = (2.0 * fragCoord - iResolution) / iResolution.y;
//     float e = 2.0 / iResolution.y;

//     float4 on;
//     float f = func(p, mask, on);

//     float3 col = float3(0.0, 0.0, 0.0);


//     col = lerp(float3(.3, .3, .4), float3(0.05, 0.05, 0.05), f);
//     col = lerp(col, mask*float3(0.9, 0.9, 0.9), dot(on.zw, on.zw));
//     col = lerp(col, float3(0.36, 0.3, 0.3), 0.2 + 0.5 * on.y * on.y);
//     col = lerp(col, float3(0.0, 0.1, 0.1), 0.5 * smoothstep(1.2, 1.3, abs(on.z) + abs(on.w)));
//     col = clamp(col * f * 2.0, 0.0, 1.0);

//     // Manual derivatives (better quality)
//     float4 kk;
//     float3 nor = normalize(float3(
//         func(p + float2(e, 0.0), mask, kk) - f,
//         2.0 * e,
//         func(p + float2(0.0, e), mask, kk) - f
//     ));

//     float3 lig = normalize(float3(0.9, 0.2, -0.4));
//     float dif = clamp(0.3 + 0.7 * dot(nor, lig), 0.0, 1.0);
//     float3 lin = float3(0.70, 0.60, 0.65) * (nor.y * 0.5 + 0.5) + float3(0.15, 0.10, 0.05) * dif;

//     col *= 1.2 * lin;
//     col = 1.0 - col;
//     col = 1.1 * col * col;

//     return float4(col, 1.0);
// }

technique BasicTechnique
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }

}