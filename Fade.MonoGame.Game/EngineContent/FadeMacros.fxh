// ============================================================================
//  FadeMacros.fxh — one shader source, two backends.
//
//  $(FadeMonoGamePlatform) picks the shader profile, and the two profiles share
//  almost no syntax:
//
//      Desktop / Web  DesktopGL pipeline, MGFXC, SM 3.0, DX9-era HLSL
//                     (sampler2D + tex2D, COLOR0 output semantics)
//      DesktopVK      DesktopVK pipeline, DXC -> SPIR-V, SM 6.0
//                     (Texture2D + .Sample, SV_Target0, explicit registers)
//
//  MonoGame's own Macros.fxh solves the same problem, but its fallback branch
//  targets SM 2.0 — adopting it verbatim would quietly downgrade every GL and
//  Web shader here from 3.0, and the Web/playground path is the one that would
//  notice last. So this is a local copy whose non-SM6 branch stays at 3.0 and
//  is otherwise byte-compatible with what these shaders compiled to before.
//
//  The profile defines come from the compiler, not from us: the Vulkan profile
//  adds SM6=1 and VULKAN=1, the OpenGL profile adds GLSL=1 and OPENGL=1.
//
//  ── Parameter names are load-bearing ──────────────────────────────────────
//  The engine binds textures by name (`set effect param texture FX, "NormalTexture", …`),
//  so both branches must expose the SAME parameter name for a given texture.
//  DECLARE_TEXTURE(Foo, n) is built so that name is always `Foo`:
//    • DX9  — the sampler's `Texture = <Foo>` reference names the parameter Foo
//    • SM6  — the profile reflects SPIR-V and, for an image sampled by exactly
//             one sampler, names the parameter after the image variable: Foo
//  Rename the Texture2D and you rename the parameter; rename the sampler and
//  nothing outside the shader notices.
// ============================================================================

#if defined(SM6) || defined(VULKAN)

	// ── DesktopVK: SM 6.0 via DXC ────────────────────────────────────────────
	//
	// ValidateShaderModels in the Vulkan profile requires EXACTLY vs_6_0/ps_6_0
	// and rejects anything else, so these are not a floor to raise later.
	#define TECHNIQUE(name, vsname, psname) \
		technique name { pass P0 { \
			VertexShader = compile vs_6_0 vsname(); \
			PixelShader  = compile ps_6_0 psname(); } }

	// Pixel shader only. A pass may omit the vertex shader, in which case whatever
	// is already bound stays bound — for a sprite that is the engine's own
	// FadeSpriteBatchEffect vertex shader, so the effect only has to describe how
	// the fragment is coloured. Its VS input signature is then the one that must
	// match the vertex declaration, which is why such a shader cannot go wrong the
	// way light.fx did.
	#define TECHNIQUE_PS(name, psname) \
		technique name { pass P0 { PixelShader = compile ps_6_0 psname(); } }

	#define POSITION_OUT   SV_Position

	// Per-pixel depth out of the pixel shader. Writing it disables early-Z, so only reach for
	// it when the depth a sprite needs is not the flat one its quad was rasterised at.
	#define DEPTH_OUT      SV_Depth

	#define TARGET0        SV_Target0
	#define TARGET1        SV_Target1
	#define TARGET2        SV_Target2
	#define TARGET3        SV_Target3

	// A texture with its sampler state left to the app (GraphicsDevice.SamplerStates).
	#define DECLARE_TEXTURE(Name, index) \
		Texture2D<float4> Name : register(t##index); \
		sampler Name##Sampler : register(s##index)

	// A texture that pins point/clamp in the effect itself. The `= sampler_state`
	// block is DX9-era syntax that MonoGame's own .fx parser still reads, and it
	// carries through to the Vulkan descriptor — verified compiling, it is not
	// silently dropped. The DX11-style `SamplerState { Filter = MIN_MAG_MIP_POINT; }`
	// block does NOT parse: that vocabulary is not in MonoGame's grammar.
	#define DECLARE_TEXTURE_POINT_CLAMP(Name, index) \
		Texture2D<float4> Name : register(t##index); \
		sampler Name##Sampler : register(s##index) = sampler_state { \
			Texture = <Name>; \
			MinFilter = Point; MagFilter = Point; MipFilter = Point; \
			AddressU = Clamp; AddressV = Clamp; }

	#define DECLARE_TEXTURE_LINEAR_CLAMP(Name, index) \
		Texture2D<float4> Name : register(t##index); \
		sampler Name##Sampler : register(s##index) = sampler_state { \
			Texture = <Name>; \
			MinFilter = Linear; MagFilter = Linear; MipFilter = Linear; \
			AddressU = Clamp; AddressV = Clamp; }

	#define SAMPLE_TEXTURE(Name, texCoord) Name.Sample(Name##Sampler, texCoord)

#else

	// ── DesktopGL / Web: SM 3.0 via MGFXC ────────────────────────────────────
	//
	// 3.0, deliberately — see the header note. The OpenGL profile caps at 3.0
	// (major > 3 throws), so this is the ceiling, and dropping to 2.0 would cost
	// instruction count and interpolators that the light march actually uses.
	#define TECHNIQUE(name, vsname, psname) \
		technique name { pass P0 { \
			VertexShader = compile vs_3_0 vsname(); \
			PixelShader  = compile ps_3_0 psname(); } }

	// See the SM6 branch: a pass with no vertex shader keeps the bound one.
	#define TECHNIQUE_PS(name, psname) \
		technique name { pass P0 { PixelShader = compile ps_3_0 psname(); } }

	#define POSITION_OUT   POSITION

	// SM 3.0 spells it DEPTH; SM 6.0 spells it SV_Depth. Same thing.
	#define DEPTH_OUT      DEPTH

	#define TARGET0        COLOR0
	#define TARGET1        COLOR1
	#define TARGET2        COLOR2
	#define TARGET3        COLOR3

	#define DECLARE_TEXTURE(Name, index) \
		Texture2D Name; \
		sampler2D Name##Sampler : register(s##index) = sampler_state { Texture = <Name>; }

	#define DECLARE_TEXTURE_POINT_CLAMP(Name, index) \
		Texture2D Name; \
		sampler2D Name##Sampler : register(s##index) = sampler_state { \
			Texture = <Name>; \
			MinFilter = Point; MagFilter = Point; MipFilter = Point; \
			AddressU = Clamp; AddressV = Clamp; }

	#define DECLARE_TEXTURE_LINEAR_CLAMP(Name, index) \
		Texture2D Name; \
		sampler2D Name##Sampler : register(s##index) = sampler_state { \
			Texture = <Name>; \
			MinFilter = Linear; MagFilter = Linear; MipFilter = Linear; \
			AddressU = Clamp; AddressV = Clamp; }

	#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

#endif
