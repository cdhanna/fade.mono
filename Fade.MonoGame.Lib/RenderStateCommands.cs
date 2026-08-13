using System;
using Fade.MonoGame.Core;
using FadeBasic;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Lib;

/// <summary>
/// Depth-stencil and rasterizer state as addressable resources, in the same shape as textures
/// and render targets: an id, a constructor, and per-property getters and setters.
/// </summary>
/// <remarks>
/// Two things worth knowing before using any of it.
///
/// A state is DATA here, not a live graphics object. That is not an implementation detail you
/// can ignore -- a MonoGame state object is frozen the instant it is handed to the device, so
/// an engine that edited one in place would work until the first frame drew with it and then
/// throw on every later edit. Because these commands write to a configuration and the object
/// is rebuilt when it changes, a state can be edited freely at any time, including after it is
/// already associated with a render output.
///
/// The enum indices are the underlying XNA values, unlike the curated surface and depth FORMAT
/// lists. Every member of these four enums is legal, so nothing needed curating, and a value
/// read back is the same number that went in. Use the count/name$ pairs below to enumerate
/// them rather than hardcoding numbers.
/// </remarks>
public partial class FadeMonoGameCommands
{
    // ── enum exploration ────────────────────────────────────────────────────────────────

    /// <summary>
    /// <para>Returns how many compare functions exist. Use with
    /// <see cref="GetCompareFunctionName">get compare function name$</see> to enumerate them.</para>
    /// </summary>
    /// <returns>The number of compare functions.</returns>
    [FadeBasicCommand("get compare function count")]
    public static int GetCompareFunctionCount() => RenderStateEnums.CompareFunctionCount;

    /// <summary>
    /// <para>Returns the name of a compare function, for depth and stencil tests.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// for n = 0 to get compare function count() - 1
    ///     print str$(n) + " " + get compare function name$(n)
    /// next
    /// </code>
    /// </example>
    /// <param name="index">The compare function index.</param>
    /// <returns>The compare function's name.</returns>
    [FadeBasicCommand("get compare function name$")]
    public static string GetCompareFunctionName([FromVm] VirtualMachine _, int index)
    {
        if (RenderStateEnums.TryGetCompareFunctionName(index, out var name)) return name;
        throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
    }

    /// <summary>
    /// <para>Returns how many stencil operations exist.</para>
    /// </summary>
    /// <returns>The number of stencil operations.</returns>
    [FadeBasicCommand("get stencil operation count")]
    public static int GetStencilOperationCount() => RenderStateEnums.StencilOperationCount;

    /// <summary>
    /// <para>Returns the name of a stencil operation -- what happens to the stencil buffer on
    /// pass, fail, or depth fail.</para>
    /// </summary>
    /// <param name="index">The stencil operation index.</param>
    /// <returns>The stencil operation's name.</returns>
    [FadeBasicCommand("get stencil operation name$")]
    public static string GetStencilOperationName([FromVm] VirtualMachine _, int index)
    {
        if (RenderStateEnums.TryGetStencilOperationName(index, out var name)) return name;
        throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
    }

    /// <summary>
    /// <para>Returns how many cull modes exist.</para>
    /// </summary>
    /// <returns>The number of cull modes.</returns>
    [FadeBasicCommand("get cull mode count")]
    public static int GetCullModeCount() => RenderStateEnums.CullModeCount;

    /// <summary>
    /// <para>Returns the name of a cull mode.</para>
    /// </summary>
    /// <remarks>
    /// Sprites are wound one way, so culling the wrong face makes them vanish entirely rather
    /// than look wrong. `None` is the safe choice for 2D.
    /// </remarks>
    /// <param name="index">The cull mode index.</param>
    /// <returns>The cull mode's name.</returns>
    [FadeBasicCommand("get cull mode name$")]
    public static string GetCullModeName([FromVm] VirtualMachine _, int index)
    {
        if (RenderStateEnums.TryGetCullModeName(index, out var name)) return name;
        throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
    }

    /// <summary>
    /// <para>Returns how many fill modes exist.</para>
    /// </summary>
    /// <returns>The number of fill modes.</returns>
    [FadeBasicCommand("get fill mode count")]
    public static int GetFillModeCount() => RenderStateEnums.FillModeCount;

    /// <summary>
    /// <para>Returns the name of a fill mode -- solid or wireframe.</para>
    /// </summary>
    /// <param name="index">The fill mode index.</param>
    /// <returns>The fill mode's name.</returns>
    [FadeBasicCommand("get fill mode name$")]
    public static string GetFillModeName([FromVm] VirtualMachine _, int index)
    {
        if (RenderStateEnums.TryGetFillModeName(index, out var name)) return name;
        throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
    }

    // ── depth stencil ───────────────────────────────────────────────────────────────────

    private static void WriteDepthStencil(ref RuntimeDepthStencil state, int index)
    {
        state.dirty = true;
        DepthStencilSystem.states[index] = state;
    }

    private static void ValidateCompareFunction(int value)
    {
        if (RenderStateEnums.TryGetCompareFunction(value, out _)) return;
        throw new Exception(
            "TODO: Change this to a VM exception, using the VirtualMachine arg. " +
            $"{value} is not a compare function. Valid values are 0 to " +
            $"{RenderStateEnums.CompareFunctionCount - 1}; see `get compare function name$`.");
    }

    private static void ValidateStencilOperation(int value)
    {
        if (RenderStateEnums.TryGetStencilOperation(value, out _)) return;
        throw new Exception(
            "TODO: Change this to a VM exception, using the VirtualMachine arg. " +
            $"{value} is not a stencil operation. Valid values are 0 to " +
            $"{RenderStateEnums.StencilOperationCount - 1}; see `get stencil operation name$`.");
    }

    /// <summary>
    /// <para>Creates or reconfigures a depth-stencil state, setting the four tests that most
    /// stencil work needs in one call.</para>
    /// </summary>
    /// <remarks>
    /// Because passing stencil operations implies you intend to use the stencil buffer, this
    /// turns the stencil test ON. Depth read and write are on too. Anything else -- masks,
    /// the reference value, two-sided operations -- keeps MonoGame's defaults and has its own
    /// setter below.
    ///
    /// A state does nothing until it is attached with
    /// <see cref="SetRenderTargetStencil">set render target stencil</see>. Note also that
    /// stencil and depth testing need a depth buffer to exist: an output rendering to textures
    /// made by <see cref="CreateRenderTarget">create texture target</see> needs a non-zero
    /// depth format on its FIRST binding, since that is the only one whose depth buffer is used.
    /// </remarks>
    /// <example>
    /// <code>
    /// ` depth test LessEqual(3), stencil test Equal(4), keep(0) on pass, keep(0) on fail
    /// stencil 1, 3, 4, 0, 0
    /// set stencil reference 1, 1
    /// set render target stencil 2, 1
    /// </code>
    /// </example>
    /// <param name="stencilId">The ID to create or reconfigure.</param>
    /// <param name="depthFunction">Compare function for the depth test.</param>
    /// <param name="stencilFunction">Compare function for the stencil test.</param>
    /// <param name="stencilPass">Stencil operation when both tests pass.</param>
    /// <param name="stencilFail">Stencil operation when the stencil test fails.</param>
    /// <seealso cref="SetRenderTargetStencil">set render target stencil</seealso>
    /// <seealso cref="GetStencilDepthFunction">get stencil depth function</seealso>
    [FadeBasicCommand("stencil")]
    public static void CreateStencil(int stencilId, int depthFunction, int stencilFunction,
        int stencilPass, int stencilFail)
    {
        ValidateCompareFunction(depthFunction);
        ValidateCompareFunction(stencilFunction);
        ValidateStencilOperation(stencilPass);
        ValidateStencilOperation(stencilFail);

        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);

        state.depthEnable = true;
        state.depthWriteEnable = true;
        state.depthFunction = depthFunction;

        state.stencilEnable = true;
        state.stencilFunction = stencilFunction;
        state.stencilPass = stencilPass;
        state.stencilFail = stencilFail;

        WriteDepthStencil(ref state, index);
    }

    /// <summary>Turns the depth test on or off for a depth-stencil state.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="enabled">1 to test against the depth buffer, 0 to ignore it.</param>
    [FadeBasicCommand("set stencil depth enable")]
    public static void SetStencilDepthEnable(int stencilId, int enabled)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.depthEnable = enabled > 0;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns 1 if the depth test is enabled.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get stencil depth enable")]
    public static int GetStencilDepthEnable(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.depthEnable ? 1 : 0;
    }

    /// <summary>
    /// Turns depth WRITING on or off, independently of the depth test.
    /// </summary>
    /// <remarks>
    /// Reading without writing is the usual setup for transparent geometry: it still hides
    /// behind what is already in front of it, but does not stop anything drawn later.
    /// </remarks>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="enabled">1 to write depth, 0 to leave the buffer untouched.</param>
    [FadeBasicCommand("set stencil depth write")]
    public static void SetStencilDepthWrite(int stencilId, int enabled)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.depthWriteEnable = enabled > 0;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns 1 if depth writing is enabled.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get stencil depth write")]
    public static int GetStencilDepthWrite(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.depthWriteEnable ? 1 : 0;
    }

    /// <summary>Sets the compare function used for the depth test.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="compareFunction">A compare function index.</param>
    [FadeBasicCommand("set stencil depth function")]
    public static void SetStencilDepthFunction(int stencilId, int compareFunction)
    {
        ValidateCompareFunction(compareFunction);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.depthFunction = compareFunction;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>
    /// <para>Returns the compare function used for the depth test.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// print "depth test is " + get compare function name$(get stencil depth function(1))
    /// </code>
    /// </example>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The compare function index.</returns>
    /// <seealso cref="GetCompareFunctionName">get compare function name$</seealso>
    [FadeBasicCommand("get stencil depth function")]
    public static int GetStencilDepthFunction(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.depthFunction;
    }

    /// <summary>Turns the stencil test on or off.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="enabled">1 to test against the stencil buffer, 0 to ignore it.</param>
    [FadeBasicCommand("set stencil enable")]
    public static void SetStencilEnable(int stencilId, int enabled)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilEnable = enabled > 0;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns 1 if the stencil test is enabled.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get stencil enable")]
    public static int GetStencilEnable(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilEnable ? 1 : 0;
    }

    /// <summary>Sets the compare function used for the stencil test.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="compareFunction">A compare function index.</param>
    [FadeBasicCommand("set stencil function")]
    public static void SetStencilFunction(int stencilId, int compareFunction)
    {
        ValidateCompareFunction(compareFunction);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilFunction = compareFunction;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the compare function used for the stencil test.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The compare function index.</returns>
    [FadeBasicCommand("get stencil function")]
    public static int GetStencilFunction(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilFunction;
    }

    /// <summary>Sets what happens to the stencil buffer when both tests pass.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="stencilOperation">A stencil operation index.</param>
    [FadeBasicCommand("set stencil pass")]
    public static void SetStencilPass(int stencilId, int stencilOperation)
    {
        ValidateStencilOperation(stencilOperation);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilPass = stencilOperation;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the stencil operation used when both tests pass.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The stencil operation index.</returns>
    [FadeBasicCommand("get stencil pass")]
    public static int GetStencilPass(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilPass;
    }

    /// <summary>Sets what happens to the stencil buffer when the stencil test fails.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="stencilOperation">A stencil operation index.</param>
    [FadeBasicCommand("set stencil fail")]
    public static void SetStencilFail(int stencilId, int stencilOperation)
    {
        ValidateStencilOperation(stencilOperation);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilFail = stencilOperation;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the stencil operation used when the stencil test fails.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The stencil operation index.</returns>
    [FadeBasicCommand("get stencil fail")]
    public static int GetStencilFail(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilFail;
    }

    /// <summary>
    /// Sets what happens to the stencil buffer when the stencil test passes but the DEPTH test
    /// fails.
    /// </summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="stencilOperation">A stencil operation index.</param>
    [FadeBasicCommand("set stencil depth fail")]
    public static void SetStencilDepthFail(int stencilId, int stencilOperation)
    {
        ValidateStencilOperation(stencilOperation);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilDepthFail = stencilOperation;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the stencil operation used when the depth test fails.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The stencil operation index.</returns>
    [FadeBasicCommand("get stencil depth fail")]
    public static int GetStencilDepthFail(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilDepthFail;
    }

    /// <summary>
    /// Sets the value the stencil test compares against, and the value `Replace` writes.
    /// </summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="reference">The reference value.</param>
    [FadeBasicCommand("set stencil reference")]
    public static void SetStencilReference(int stencilId, int reference)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.referenceStencil = reference;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the stencil reference value.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The reference value.</returns>
    [FadeBasicCommand("get stencil reference")]
    public static int GetStencilReference(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.referenceStencil;
    }

    /// <summary>Sets the mask applied when READING the stencil buffer.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="mask">The read mask.</param>
    [FadeBasicCommand("set stencil mask")]
    public static void SetStencilMask(int stencilId, int mask)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilMask = mask;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the stencil read mask.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The read mask.</returns>
    [FadeBasicCommand("get stencil mask")]
    public static int GetStencilMask(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilMask;
    }

    /// <summary>
    /// Sets the mask applied when WRITING the stencil buffer. Zero here makes every stencil
    /// operation a no-op, which looks like the operations being ignored.
    /// </summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="mask">The write mask.</param>
    [FadeBasicCommand("set stencil write mask")]
    public static void SetStencilWriteMask(int stencilId, int mask)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.stencilWriteMask = mask;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the stencil write mask.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The write mask.</returns>
    [FadeBasicCommand("get stencil write mask")]
    public static int GetStencilWriteMask(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.stencilWriteMask;
    }

    /// <summary>
    /// Turns two-sided stencil mode on or off. When on, back faces use the counter-clockwise
    /// operations instead of the ones above.
    /// </summary>
    /// <remarks>
    /// Of little use to sprites, which are all wound the same way, but it is part of the state
    /// and omitting it would just mean reaching for it later and finding a hole.
    /// </remarks>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="enabled">1 for two-sided, 0 for one-sided.</param>
    [FadeBasicCommand("set stencil two sided")]
    public static void SetStencilTwoSided(int stencilId, int enabled)
    {
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.twoSided = enabled > 0;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns 1 if two-sided stencil mode is on.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get stencil two sided")]
    public static int GetStencilTwoSided(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.twoSided ? 1 : 0;
    }

    /// <summary>Sets the counter-clockwise stencil compare function, used when two-sided.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="compareFunction">A compare function index.</param>
    [FadeBasicCommand("set stencil ccw function")]
    public static void SetStencilCcwFunction(int stencilId, int compareFunction)
    {
        ValidateCompareFunction(compareFunction);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.ccwStencilFunction = compareFunction;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the counter-clockwise stencil compare function.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The compare function index.</returns>
    [FadeBasicCommand("get stencil ccw function")]
    public static int GetStencilCcwFunction(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.ccwStencilFunction;
    }

    /// <summary>Sets the counter-clockwise pass operation, used when two-sided.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="stencilOperation">A stencil operation index.</param>
    [FadeBasicCommand("set stencil ccw pass")]
    public static void SetStencilCcwPass(int stencilId, int stencilOperation)
    {
        ValidateStencilOperation(stencilOperation);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.ccwStencilPass = stencilOperation;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the counter-clockwise pass operation.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The stencil operation index.</returns>
    [FadeBasicCommand("get stencil ccw pass")]
    public static int GetStencilCcwPass(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.ccwStencilPass;
    }

    /// <summary>Sets the counter-clockwise fail operation, used when two-sided.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="stencilOperation">A stencil operation index.</param>
    [FadeBasicCommand("set stencil ccw fail")]
    public static void SetStencilCcwFail(int stencilId, int stencilOperation)
    {
        ValidateStencilOperation(stencilOperation);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.ccwStencilFail = stencilOperation;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the counter-clockwise fail operation.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The stencil operation index.</returns>
    [FadeBasicCommand("get stencil ccw fail")]
    public static int GetStencilCcwFail(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.ccwStencilFail;
    }

    /// <summary>Sets the counter-clockwise depth-fail operation, used when two-sided.</summary>
    /// <param name="stencilId">The state to change.</param>
    /// <param name="stencilOperation">A stencil operation index.</param>
    [FadeBasicCommand("set stencil ccw depth fail")]
    public static void SetStencilCcwDepthFail(int stencilId, int stencilOperation)
    {
        ValidateStencilOperation(stencilOperation);
        DepthStencilSystem.GetIndex(stencilId, out var index, out var state);
        state.ccwStencilDepthFail = stencilOperation;
        WriteDepthStencil(ref state, index);
    }

    /// <summary>Returns the counter-clockwise depth-fail operation.</summary>
    /// <param name="stencilId">The state to inspect.</param>
    /// <returns>The stencil operation index.</returns>
    [FadeBasicCommand("get stencil ccw depth fail")]
    public static int GetStencilCcwDepthFail(int stencilId)
    {
        DepthStencilSystem.GetIndex(stencilId, out _, out var state);
        return state.ccwStencilDepthFail;
    }

    // ── rasterizer ──────────────────────────────────────────────────────────────────────

    private static void WriteRasterizer(ref RuntimeRasterizer state, int index)
    {
        state.dirty = true;
        RasterizerSystem.states[index] = state;
    }

    private static void ValidateCullMode(int value)
    {
        if (RenderStateEnums.TryGetCullMode(value, out _)) return;
        throw new Exception(
            "TODO: Change this to a VM exception, using the VirtualMachine arg. " +
            $"{value} is not a cull mode. Valid values are 0 to " +
            $"{RenderStateEnums.CullModeCount - 1}; see `get cull mode name$`.");
    }

    private static void ValidateFillMode(int value)
    {
        if (RenderStateEnums.TryGetFillMode(value, out _)) return;
        throw new Exception(
            "TODO: Change this to a VM exception, using the VirtualMachine arg. " +
            $"{value} is not a fill mode. Valid values are 0 to " +
            $"{RenderStateEnums.FillModeCount - 1}; see `get fill mode name$`.");
    }

    /// <summary>
    /// <para>Creates or reconfigures a rasterizer state with a cull mode and a fill mode.</para>
    /// </summary>
    /// <remarks>
    /// Depth bias, scissor testing and the rest keep their defaults and have setters below.
    ///
    /// Sprites are all wound the same way, so culling the wrong face does not make them look
    /// wrong -- it makes them disappear. Cull mode 0 (`None`) is the safe choice for 2D, and
    /// wireframe fill is a quick way to see the quads a batch is actually emitting.
    ///
    /// A state does nothing until attached with
    /// <see cref="SetRenderTargetRasterizer">set render target rasterizer</see>.
    /// </remarks>
    /// <example>
    /// <code>
    /// rasterizer 1, 0, 1        ` cull nothing, draw wireframe
    /// set render target rasterizer 1, 1
    /// </code>
    /// </example>
    /// <param name="rasterizerId">The ID to create or reconfigure.</param>
    /// <param name="cullMode">A cull mode index.</param>
    /// <param name="fillMode">A fill mode index.</param>
    /// <seealso cref="SetRenderTargetRasterizer">set render target rasterizer</seealso>
    [FadeBasicCommand("rasterizer")]
    public static void CreateRasterizer(int rasterizerId, int cullMode, int fillMode)
    {
        ValidateCullMode(cullMode);
        ValidateFillMode(fillMode);

        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.cullMode = cullMode;
        state.fillMode = fillMode;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Sets which faces are discarded.</summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="cullMode">A cull mode index.</param>
    [FadeBasicCommand("set rasterizer cull")]
    public static void SetRasterizerCull(int rasterizerId, int cullMode)
    {
        ValidateCullMode(cullMode);
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.cullMode = cullMode;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns the cull mode.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>The cull mode index.</returns>
    [FadeBasicCommand("get rasterizer cull")]
    public static int GetRasterizerCull(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.cullMode;
    }

    /// <summary>Sets solid or wireframe filling.</summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="fillMode">A fill mode index.</param>
    [FadeBasicCommand("set rasterizer fill")]
    public static void SetRasterizerFill(int rasterizerId, int fillMode)
    {
        ValidateFillMode(fillMode);
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.fillMode = fillMode;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns the fill mode.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>The fill mode index.</returns>
    [FadeBasicCommand("get rasterizer fill")]
    public static int GetRasterizerFill(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.fillMode;
    }

    /// <summary>
    /// Sets a constant offset added to depth values, for pushing coplanar geometry apart.
    /// </summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="depthBias">The bias to add.</param>
    [FadeBasicCommand("set rasterizer depth bias")]
    public static void SetRasterizerDepthBias(int rasterizerId, float depthBias)
    {
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.depthBias = depthBias;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns the constant depth bias.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>The depth bias.</returns>
    [FadeBasicCommand("get rasterizer depth bias")]
    public static float GetRasterizerDepthBias(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.depthBias;
    }

    /// <summary>Sets a depth offset scaled by how steeply a surface is sloped.</summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="slopeScaleDepthBias">The slope-scaled bias.</param>
    [FadeBasicCommand("set rasterizer slope scale depth bias")]
    public static void SetRasterizerSlopeScaleDepthBias(int rasterizerId, float slopeScaleDepthBias)
    {
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.slopeScaleDepthBias = slopeScaleDepthBias;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns the slope-scaled depth bias.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>The slope-scaled bias.</returns>
    [FadeBasicCommand("get rasterizer slope scale depth bias")]
    public static float GetRasterizerSlopeScaleDepthBias(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.slopeScaleDepthBias;
    }

    /// <summary>Turns multisample antialiasing on or off.</summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="enabled">1 to antialias, 0 for hard edges.</param>
    [FadeBasicCommand("set rasterizer multisample")]
    public static void SetRasterizerMultiSample(int rasterizerId, int enabled)
    {
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.multiSampleAntiAlias = enabled > 0;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns 1 if multisample antialiasing is on.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get rasterizer multisample")]
    public static int GetRasterizerMultiSample(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.multiSampleAntiAlias ? 1 : 0;
    }

    /// <summary>
    /// Turns scissor testing on or off, clipping drawing to the graphics device's scissor
    /// rectangle.
    /// </summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="enabled">1 to clip, 0 to draw everywhere.</param>
    [FadeBasicCommand("set rasterizer scissor")]
    public static void SetRasterizerScissor(int rasterizerId, int enabled)
    {
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.scissorTestEnable = enabled > 0;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns 1 if scissor testing is on.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get rasterizer scissor")]
    public static int GetRasterizerScissor(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.scissorTestEnable ? 1 : 0;
    }

    /// <summary>Turns clipping against the near and far planes on or off.</summary>
    /// <param name="rasterizerId">The state to change.</param>
    /// <param name="enabled">1 to clip, 0 to keep everything.</param>
    [FadeBasicCommand("set rasterizer depth clip")]
    public static void SetRasterizerDepthClip(int rasterizerId, int enabled)
    {
        RasterizerSystem.GetIndex(rasterizerId, out var index, out var state);
        state.depthClipEnable = enabled > 0;
        WriteRasterizer(ref state, index);
    }

    /// <summary>Returns 1 if depth clipping is on.</summary>
    /// <param name="rasterizerId">The state to inspect.</param>
    /// <returns>1 when enabled, 0 otherwise.</returns>
    [FadeBasicCommand("get rasterizer depth clip")]
    public static int GetRasterizerDepthClip(int rasterizerId)
    {
        RasterizerSystem.GetIndex(rasterizerId, out _, out var state);
        return state.depthClipEnable ? 1 : 0;
    }
}
