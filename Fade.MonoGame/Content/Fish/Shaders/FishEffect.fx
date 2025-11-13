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
float Time;
#define PI 3.14159265

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

	float3 p : TEXCOORD1;
	float2 o : TEXCOORD2;

};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0, 
								float4 custom   : TEXCOORD1)
{
	VertexShaderOutput output;
	
	float2 uv = float2(texCoord.x, 1 - texCoord.y);
	
	
	//float y_rot = Time*200;
	//float x_rot = 180;//40 + 120*sin(Time*2);
	
	float y_rot = custom.y;
	float x_rot = custom.x + 180;
	float fov = 90;
	float inset = 0;
	
	float sin_b = sin(y_rot / 180.0 * PI);
	float cos_b = cos(y_rot / 180.0 * PI);
	float sin_c = sin(x_rot / 180.0 * PI);
	float cos_c = cos(x_rot / 180.0 * PI);
	
	float3x3 inv_rot_mat;
	inv_rot_mat[0][0] = cos_b;
	inv_rot_mat[0][1] = 0.0;
	inv_rot_mat[0][2] = -sin_b;
	
	inv_rot_mat[1][0] = sin_b * sin_c;
	inv_rot_mat[1][1] = cos_c;
	inv_rot_mat[1][2] = cos_b * sin_c;
	
	inv_rot_mat[2][0] = sin_b * cos_c;
	inv_rot_mat[2][1] = -sin_c;
	inv_rot_mat[2][2] = cos_b * cos_c;

	float t = tan(fov / 360.0 * PI);
	output.p = mul( float3((uv - 0.5), 0.5 / t), inv_rot_mat);
	float v = (0.5 / t) + 0.5;
	output.p.xy *= v * inv_rot_mat[2].z;
	output.o = v * inv_rot_mat[2].xy;
	
	
    
	output.Position = mul(position, MatrixTransform);
	
	float2 texPixelSize = float2(16, 16);
	output.Position.xy += 2*(uv - 0.5) / texPixelSize * t * (1.0 - inset);

	output.Color = color;
	output.TextureCoordinates = texCoord;
	return output;
}


float4 MainPS(VertexShaderOutput input) : COLOR
{
	
	float2 uv = (input.p.xy / input.p.z).xy - input.o;
   	float4 c= tex2D(SpriteTextureSampler,uv + .5) * input.Color;
	
	c.a *= step(max(abs(uv.x), abs(uv.y)), 0.5);

	return c;
}

technique SpriteDrawing
{
	pass P0
	{
        VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};