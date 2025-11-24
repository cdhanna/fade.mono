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
extern Texture2D Noise;
sampler2D NoiseSampler = sampler_state
{
	Texture = <Noise>;
};
float Time;
float2 Resolution;
float TimeSpeed = 50.0;

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float fract(float x)
{
    return x - floor(x);
}

float randomVal(float inVal)
{
    float2 v = float2(inVal, 2523.2361);
    float2 d = float2(12.9898, 78.233);
    return fract(sin(dot(v, d)) * 43758.5453) - 0.5;
}

float2 randomVec2(float inVal)
{
    float x = randomVal(inVal);
    float y = randomVal(inVal + 151.523);
    float2 v = float2(x, y);
    return normalize(v);
}

float makeWaves(float2 uv, float theTime, float offset)
{
    float result = 0.0;
    for (int n = 0; n < 16; n++)
    {
        float i = n + offset;
        float2 randVec = randomVec2(i);
        float direction = dot(uv, randVec);
        float sineWave = sin(direction * randomVal(i + 1.6516) + theTime * TimeSpeed);
        sineWave = smoothstep(0.0, 1.0, sineWave);
        result += randomVal(i + 123.0) * sineWave;
    }
    return result;
}

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
   // float2 uv = texCoord * Resolution.xy;
   // uv /= Resolution.y; // TODO: this resolution scaling is not correct.
float2 uv = texCoord;
    //return float4(uv.xy, 0, 1);

    float2 uv2 = uv * 60.0;

    uv *= 1.0;

    float result1 = makeWaves(uv2 + float2(Time * TimeSpeed, 0.0), Time, 0.1);
    float result2 = makeWaves(uv2 - float2(Time * 0.8 * TimeSpeed, 0.0), Time * 0.8 + 0.06, 0.26);

    result1 = smoothstep(0.4, 1.1, 1.0 - abs(result1));
    result2 = smoothstep(0.4, 1.1, 1.0 - abs(result2));

    float result = 2.0 * smoothstep(0.35, 1.8, (result1 + result2) * 0.5);

    float2 p = float2(result, result2) * 0.001 + 0.05 * sin(uv * 16.0 - cos(uv.yx * 16.0 + Time * TimeSpeed)) * 0.1;

    float4 tex = tex2D(SpriteTextureSampler, uv + p);
    float4 finalColor = float4(.45, 0.4, .8, 1.0) * result * 0.08 + tex;

    return finalColor;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};