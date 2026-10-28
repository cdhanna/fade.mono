#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// THE TRIDENT IN THE CLOUDS. once a game has summoned the god, the clouds of the main menu draw themselves
// together into his trident over the boat, hold it a while, let it go, and do it again. see THE MAIN MENU
// REMEMBERS THE GOD in fish_headers_neptune.
// it is not a picture over the sky. it is the sky's own clouds: this works out the same long thin clouds,
// drifting the same way, that SurfaceEffect.fx draws, for the pixels that it covers, and lets them grow
// thicker where the trident is, and a little thicker around it. so as it comes together, the clouds that
// are drifting past swell and gather into it, and as it goes, they thin back out into what they were.
// the trident lies flat in the layer of the clouds, high over the sea, the way that a cloud does, and it
// is seen from the boat, from under it: the end of its shaft is the nearest of it, big and high up the
// screen, and it points away, toward the horizon, so its prongs are further off, smaller and flatter. it
// is turned, so that it lies along the sky the same way that the other clouds do: they rise to the right,
// and so does it, from its prongs, low on the left, to the end of its shaft, high on the right.
// the formula of the clouds has to be kept the same as the one in SurfaceEffect.fx by hand.
// in the day there are faint rays of light down from behind it, once it is all there.
// it is drawn on a plain sprite of its own, over the sky, because SurfaceEffect.fx has no room left.

float4x4 MatrixTransform;

// the game sets these on every frame.
//   Cloud  x the time, in seconds: the same time that the sky has. y how far the trident has come
//          together, from 0 to 1. z is 1 in the day and 0 in the evening. w is 1 when the clouds are sick.
//   Place  x and y where the top left of the sprite is, in the pixels of the sky. z where the horizon is.
//          w how much of it is there, from 0 to 1, as it fades when the camera goes under.
float4 Cloud;
float4 Place;

// how big the sprite is, in pixels. the trident is in it, with room for its rays. its bottom edge is a
// little over the horizon.
#define WIDE 150.0
#define TALL 124.0

// THE LAYER OF THE CLOUDS, as it is seen from the boat. a point that is DY pixels over the horizon is
// LAYER_FAR / DY away, and a pixel across the screen there is DY / LAYER_WIDE of the layer across. the
// trident is in the units of the layer, around LAYER_MID away, turned by LAYER_TURN radians.
#define LAYER_FAR 200.0
#define LAYER_WIDE 2.1
#define LAYER_MID 3.8
#define LAYER_TURN -0.62

// how much thicker the clouds get inside the trident, which is enough to fill it, and how far out
// around it they get thicker too, in pixels
#define BOOST 3.4
#define REACH 4.5

#define PICK(evening, day) lerp(evening, day, Cloud.z)
#define CloudColor  PICK(float3(0.380, 0.200, 0.380), float3(1.000, 1.000, 1.000))
#define CloudEdge   PICK(float3(1.000, 0.560, 0.400), float3(0.700, 0.790, 0.900))
#define SickCloud   float3(0.085, 0.150, 0.075)
#define RayColor    float3(1.000, 0.970, 0.860)

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 Quad : TEXCOORD0;
};

VertexShaderOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	VertexShaderOutput output;
	position.xy = floor(position.xy + .5);
	output.Position = mul(position, MatrixTransform);
	output.Color = color;
	output.Quad = texCoord;
	return output;
}

// how far a point is from a stroke from a to b, r pixels thick
float stroke(float2 p, float2 a, float2 b, float r)
{
	float2 ab = b - a;
	float h = saturate(dot(p - a, ab) / dot(ab, ab));
	return length(p - a - ab * h) - r;
}

// how far a point of the layer of the clouds is from the trident, in the units of the layer, from its
// middle. it lies with its shaft toward the boat and its prongs away.
float trident(float2 p)
{
	float d = stroke(p, float2(0, -1.35), float2(0, 0.40), 0.12);             // the shaft
	d = min(d, stroke(p, float2(-0.85, 0.40), float2(0.85, 0.40), 0.11));    // the crossbar
	d = min(d, stroke(p, float2(-0.85, 0.40), float2(-0.85, 1.45), 0.10));   // the left prong
	d = min(d, stroke(p, float2(0.85, 0.40), float2(0.85, 1.45), 0.10));     // the right prong
	d = min(d, stroke(p, float2(0, 0.40), float2(0, 1.65), 0.11));           // the middle prong
	d = min(d, stroke(p, float2(-0.85, 1.50), float2(-1.10, 1.15), 0.08));   // the barbs, out to the sides
	d = min(d, stroke(p, float2(0.85, 1.50), float2(1.10, 1.15), 0.08));
	d = min(d, stroke(p, float2(0, 1.72), float2(-0.22, 1.40), 0.08));
	d = min(d, stroke(p, float2(0, 1.72), float2(0.22, 1.40), 0.08));
	return d;
}

// where a pixel of the sky is in the layer of the clouds, around the middle of the trident, turned. the
// answer is in the units of the layer. `over` is how many pixels the pixel is over the horizon.
float2 onLayer(float2 px, float over)
{
	float2 q = float2((px.x - (Place.x + WIDE * 0.5)) * LAYER_WIDE / over, LAYER_FAR / over - LAYER_MID);
	float c = cos(LAYER_TURN);
	float s = sin(LAYER_TURN);
	return float2(q.x * c - q.y * s, q.x * s + q.y * c);
}

// how far a pixel of the sky is from the trident, in pixels of the screen, more or less
float tridentAt(float2 px)
{
	float over = max(Place.z - px.y, 6.0);
	return trident(onLayer(px, over)) * over / LAYER_WIDE;
}

// the clouds of the sky, at a pixel of the sky: how thick they are there. the same as SurfaceEffect.fx.
float clouds(float2 px, float dy)
{
	float2 cp = float2(px.x * 0.5 + Cloud.x * 2.0, (px.y - Place.z) * 2.4 + dy);
	return sin(cp.x * 0.040 + 1.7 * sin(cp.y * 0.031)) + 0.6 * sin(cp.x * 0.093 + cp.y * 0.11 + 2.0);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float form = saturate(Cloud.y);
	float eased = form * form * (3.0 - 2.0 * form);

	// where the pixel is: in the sprite, from the middle of it, and in the sky
	float2 local = floor(input.Quad * float2(WIDE, TALL));
	float2 px = floor(Place.xy + .5) + local;

	// how thick the clouds are allowed to be here: as thick as they are anywhere in the sky, and more,
	// the closer to the trident, as much as it has come together
	float band = saturate(1.0 - abs((Place.z - px.y) - 78.0) / 46.0);
	float need = 1.62 - band * 0.95;
	float d = tridentAt(px);
	float dUnder = tridentAt(px + float2(0, 2.0));
	float boost = eased * BOOST * saturate(1.0 - d / REACH);
	float boostUnder = eased * BOOST * saturate(1.0 - dUnder / REACH);

	// a cloud is here where the clouds are thick enough. it is only drawn where the trident has thickened
	// them: everywhere else, the sky has drawn them already. the bottom edge is another color, the same
	// way that it is for the other clouds.
	float here = step(need - boost, clouds(px, 0.0)) * step(.001, boost);
	float under = step(need - boostUnder, clouds(px, 5.0));
	float sick = saturate(Cloud.w) * Cloud.z;
	float3 color = lerp(lerp(CloudEdge, CloudColor, under), SickCloud * (.8 + .4 * under), sick);
	float alpha = here * .85;

	// in the day, faint rays of light fan out from behind its head, once it is all there
	float2 head = float2(Place.x + WIDE * 0.5 - 6.0, Place.z - LAYER_FAR / (LAYER_MID + 0.9));
	float2 fromMid = px - head;
	float angle = atan2(fromMid.y, fromMid.x);
	float rays = step(0.6, .5 + .5 * sin(angle * 18.0 + Cloud.x * .25)) * saturate(1.0 - length(fromMid) / 62.0);
	rays *= Cloud.z * eased * eased * (1.0 - here) * (1.0 - sick) * .12;
	float checker = frac((local.x + local.y) * .5) * 2.0;
	rays *= step(.5, checker + .25);
	color = lerp(color, RayColor, (1.0 - here) * step(.001, rays));
	alpha = max(alpha, rays);

	return float4(color, alpha * saturate(Place.w)) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
