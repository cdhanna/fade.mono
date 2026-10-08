#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Draws a fish as a little card in 3d, that can tilt, with more copies of it stacked up behind.
//
// Everything is per sprite, so it all comes in on the two extra texcoords.
//
//   texcoord1.x  pitch, in radians. positive tips the top of the fish away, so that it faces up.
//                half a turn puts the fish upside down, with its back showing.
//   texcoord1.y  yaw, in radians. positive turns the fish to face the right.
//   texcoord1.z  how many fish are in the stack. the part after the decimal point slides the last
//                one out from behind the one in front of it, so a count can be animated.
//   texcoord1.w  the size of the sprite in pixels. the quad grows by this much, so that there is
//                room to draw the tilt and the stack outside of the sprite. 0 leaves the quad alone.
//
//   texcoord2.xy how far to push the fish out of its place, in pixels. it gets rounded to whole pixels.
//   texcoord2.z  how much bigger to draw the fish, as if it lifts up toward the eye. 0 is its own
//                size, and 1 is twice as big.
//   texcoord2.w  how much brighter to draw it. 0 is its own color, and 1 is twice as bright.

Texture2D SpriteTexture;
float4x4 MatrixTransform;

// how far each fish in a stack sits from the one in front of it, where 1 is the size of a fish.
// this is on the face of the card, so it tilts along with it.
#define STACK_SHIFT float2(.16, -.16)

// how far behind each one is, the same way. this is what a tilt shows off.
#define STACK_DEPTH .15

// how bright each one is, next to the one in front of it. 1 does not darken at all.
#define STACK_SHADE .6

// the quad is this many times as big as the sprite
#define QUAD_GROW 2.0

// how far away the eye is, where 1 is the size of a fish. closer makes a stronger perspective.
#define EYE_DISTANCE 3.0

#define MAX_STACK 4

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

	// where the pixel is on the quad. the fish fills -.5 to .5 when it lies flat.
	float2 Quad : TEXCOORD0;

	// the card. these are the directions of its right side, its bottom side, and its face.
	float3 AxisU : TEXCOORD1;
	float3 AxisV : TEXCOORD2;
	float3 Normal : TEXCOORD3;

	float2 CountGlow : TEXCOORD4; // x is how many fish are in the stack, y is how much brighter to draw
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0,
								float4 custom   : TEXCOORD1,
								float4 custom2  : TEXCOORD2)
{
	VertexShaderOutput output;

	// the texcoord says which corner of the sprite this is
	float2 corner = texCoord - .5;

	// push the corners out, to make room. the picture on the quad stays the same, so pushing them
	// out further than that is what makes the fish bigger.
	float grow = (QUAD_GROW - 1.0) * step(.5, custom.w);
	float lift = 1.0 + max(custom2.z, 0.0) * step(.5, custom.w);
	position.xy += corner * custom.w * ((1.0 + grow) * lift - 1.0);

	// the push lands on whole pixels. a fish that sits part of the way between two pixels gets some
	// of its own pixels doubled up and some dropped.
	position.xy += floor(custom2.xy + .5);
	output.Quad = corner * (1.0 + grow);

	float sinP = sin(custom.x);
	float cosP = cos(custom.x);
	float sinY = sin(custom.y);
	float cosY = cos(custom.y);

	// x goes right, y goes down, and z comes out of the screen
	output.AxisU = float3(cosY, 0, -sinY);
	output.AxisV = float3(sinP * sinY, cosP, sinP * cosY);
	output.Normal = float3(cosP * sinY, -sinP, cosP * cosY);

	output.CountGlow = float2(clamp(custom.z, 1.0, MAX_STACK), max(custom2.w, 0.0));

	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	return output;
}

// one fish of the stack. index 0 is the one in front.
float4 SampleLayer(VertexShaderOutput input, float index)
{
	// 1 when this fish is all the way there, and less while it is still sliding out
	float there = saturate(input.CountGlow.x - index);
	float slot = max(index - 1.0 + there, 0.0);

	// look from the eye, through the pixel, and find where that hits the card of this fish
	float facing = input.Normal.z * EYE_DISTANCE;
	float across = dot(input.Normal.xy, input.Quad);

	// a card that is turned past edge on shows its back, which is the same picture turned over.
	// the stack stays on the far side of it.
	float behind = slot * STACK_DEPTH * (facing < 0.0 ? -1.0 : 1.0);
	float reach = facing - across;
	reach = abs(reach) < .001 ? .001 : reach;
	float t = (facing + behind) / reach;
	float3 hit = float3(input.Quad * t, EYE_DISTANCE * (1.0 - t));

	float2 uv = float2(dot(input.AxisU, hit), dot(input.AxisV, hit)) - slot * STACK_SHIFT;

	float4 c = tex2D(SpriteTextureSampler, uv + .5);
	c.a *= step(max(abs(uv.x), abs(uv.y)), .5) * step(.001, there);

	// the ones in the back are darker, so that the one in front stands out
	c.rgb *= pow(STACK_SHADE, slot);
	return c;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// back to front
	float4 result = float4(0, 0, 0, 0);
	for (int i = MAX_STACK - 1; i >= 0; i--)
	{
		float4 layer = SampleLayer(input, i);
		float alpha = layer.a + result.a * (1.0 - layer.a);
		result.rgb = (layer.rgb * layer.a + result.rgb * result.a * (1.0 - layer.a)) / max(alpha, .001);
		result.a = alpha;
	}

	// a fish that is told to glow gets brighter. nothing else here changes its colors.
	result.rgb *= 1.0 + input.CountGlow.y;

	return result * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
