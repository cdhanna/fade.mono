using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core;

/// <summary>
/// Index/name lookups for the enums that depth-stencil and rasterizer state are built from.
///
/// Unlike the surface and depth FORMAT lists, which are curated because most SurfaceFormat
/// members are not valid render targets, these four enums are small and every member is
/// legal -- so the index IS the underlying enum value. That keeps a round trip through
/// `get compare function name$` honest, and means a value read back out of a state is the
/// same number that went in.
/// </summary>
public static class RenderStateEnums
{
    // Declaration order of the XNA enums, which is also their numeric order.
    private static readonly string[] _compareFunctions =
    {
        "Always", "Never", "Less", "LessEqual", "Equal", "GreaterEqual", "Greater", "NotEqual"
    };

    private static readonly string[] _stencilOperations =
    {
        "Keep", "Zero", "Replace", "Increment", "Decrement",
        "IncrementSaturation", "DecrementSaturation", "Invert"
    };

    private static readonly string[] _cullModes =
    {
        "None", "CullClockwiseFace", "CullCounterClockwiseFace"
    };

    private static readonly string[] _fillModes =
    {
        "Solid", "WireFrame"
    };

    public static int CompareFunctionCount => _compareFunctions.Length;
    public static int StencilOperationCount => _stencilOperations.Length;
    public static int CullModeCount => _cullModes.Length;
    public static int FillModeCount => _fillModes.Length;

    public static bool TryGetCompareFunctionName(int index, out string name)
        => TryGetName(_compareFunctions, index, out name);

    public static bool TryGetStencilOperationName(int index, out string name)
        => TryGetName(_stencilOperations, index, out name);

    public static bool TryGetCullModeName(int index, out string name)
        => TryGetName(_cullModes, index, out name);

    public static bool TryGetFillModeName(int index, out string name)
        => TryGetName(_fillModes, index, out name);

    public static bool TryGetCompareFunction(int index, out CompareFunction value)
    {
        value = CompareFunction.Always;
        if (index < 0 || index >= _compareFunctions.Length) return false;
        value = (CompareFunction)index;
        return true;
    }

    public static bool TryGetStencilOperation(int index, out StencilOperation value)
    {
        value = StencilOperation.Keep;
        if (index < 0 || index >= _stencilOperations.Length) return false;
        value = (StencilOperation)index;
        return true;
    }

    public static bool TryGetCullMode(int index, out CullMode value)
    {
        value = CullMode.None;
        if (index < 0 || index >= _cullModes.Length) return false;
        value = (CullMode)index;
        return true;
    }

    public static bool TryGetFillMode(int index, out FillMode value)
    {
        value = FillMode.Solid;
        if (index < 0 || index >= _fillModes.Length) return false;
        value = (FillMode)index;
        return true;
    }

    private static bool TryGetName(string[] names, int index, out string name)
    {
        name = "";
        if (index < 0 || index >= names.Length) return false;
        name = names[index];
        return true;
    }
}

/// <summary>
/// A depth-stencil configuration, stored as plain numbers rather than as a live
/// DepthStencilState.
///
/// That indirection is the whole point. A MonoGame state object becomes read-only the moment
/// it is handed to the graphics device -- every setter calls ThrowIfBound -- so a design that
/// edited the state object in place would work until the first frame drew with it and then
/// start throwing. Holding the configuration as data and building a FRESH state object
/// whenever it changes sidesteps that completely, and it means a state can be edited after it
/// has been associated with a render output.
/// </summary>
public struct RuntimeDepthStencil
{
    public int id;

    public bool depthEnable;
    public bool depthWriteEnable;
    public int depthFunction;

    public bool stencilEnable;
    public int stencilFunction;
    public int stencilPass;
    public int stencilFail;
    public int stencilDepthFail;

    public int referenceStencil;
    public int stencilMask;
    public int stencilWriteMask;

    public bool twoSided;
    public int ccwStencilFunction;
    public int ccwStencilPass;
    public int ccwStencilFail;
    public int ccwStencilDepthFail;

    /// <summary>The built state, rebuilt whenever <see cref="dirty"/> is set.</summary>
    public DepthStencilState resolved;

    public bool dirty;
}

/// <summary>
/// A rasterizer configuration. Same data-not-object reasoning as
/// <see cref="RuntimeDepthStencil"/>.
/// </summary>
public struct RuntimeRasterizer
{
    public int id;

    public int cullMode;
    public int fillMode;

    public float depthBias;
    public float slopeScaleDepthBias;

    public bool multiSampleAntiAlias;
    public bool scissorTestEnable;
    public bool depthClipEnable;

    public RasterizerState resolved;

    public bool dirty;
}

public static class DepthStencilSystem
{
    public static List<RuntimeDepthStencil> states = new List<RuntimeDepthStencil>();
    private static Dictionary<int, int> _map = new Dictionary<int, int>();
    public static int highestId;

    public static void Reset()
    {
        states.Clear();
        _map.Clear();
        highestId = 0;
    }

    /// <summary>
    /// Looks up a state, creating the slot on first use.
    ///
    /// A slot starts at MonoGame's DepthStencilState.Default rather than at all-zero, because
    /// a zeroed struct would mean depth testing off with a Never compare -- a state that
    /// discards every pixel, which is a confusing thing for an id to mean before it has been
    /// configured.
    /// </summary>
    public static void GetIndex(int id, out int index, out RuntimeDepthStencil state)
    {
        if (_map.TryGetValue(id, out index))
        {
            state = states[index];
            return;
        }

        highestId = id > highestId ? id : highestId;
        index = _map[id] = states.Count;

        state = new RuntimeDepthStencil
        {
            id = id,
            depthEnable = true,
            depthWriteEnable = true,
            depthFunction = (int)CompareFunction.LessEqual,
            stencilEnable = false,
            stencilFunction = (int)CompareFunction.Always,
            stencilPass = (int)StencilOperation.Keep,
            stencilFail = (int)StencilOperation.Keep,
            stencilDepthFail = (int)StencilOperation.Keep,
            referenceStencil = 0,
            stencilMask = int.MaxValue,
            stencilWriteMask = int.MaxValue,
            twoSided = false,
            ccwStencilFunction = (int)CompareFunction.Always,
            ccwStencilPass = (int)StencilOperation.Keep,
            ccwStencilFail = (int)StencilOperation.Keep,
            ccwStencilDepthFail = (int)StencilOperation.Keep,
            dirty = true,
        };

        states.Add(state);
    }

    public static bool Exists(int id) => _map.ContainsKey(id);

    /// <summary>
    /// Returns the live state for an id, building it if the configuration changed.
    ///
    /// Returns null for id 0, which the sprite batch reads as "use your own default" -- so an
    /// output that was never given a state behaves exactly as it did before this existed.
    ///
    /// A replaced state object is deliberately not disposed: it may still be bound to the
    /// device from the frame in flight, and faulting a draw is worse than leaving a small
    /// object for the collector. Rebuilds only happen after an edit, not per frame.
    /// </summary>
    public static DepthStencilState Resolve(int id)
    {
        if (id <= 0) return null;

        GetIndex(id, out var index, out var state);

        if (state.resolved != null && !state.dirty) return state.resolved;

        RenderStateEnums.TryGetCompareFunction(state.depthFunction, out var depthFunc);
        RenderStateEnums.TryGetCompareFunction(state.stencilFunction, out var stencilFunc);
        RenderStateEnums.TryGetStencilOperation(state.stencilPass, out var pass);
        RenderStateEnums.TryGetStencilOperation(state.stencilFail, out var fail);
        RenderStateEnums.TryGetStencilOperation(state.stencilDepthFail, out var depthFail);
        RenderStateEnums.TryGetCompareFunction(state.ccwStencilFunction, out var ccwFunc);
        RenderStateEnums.TryGetStencilOperation(state.ccwStencilPass, out var ccwPass);
        RenderStateEnums.TryGetStencilOperation(state.ccwStencilFail, out var ccwFail);
        RenderStateEnums.TryGetStencilOperation(state.ccwStencilDepthFail, out var ccwDepthFail);

        state.resolved = new DepthStencilState
        {
            DepthBufferEnable = state.depthEnable,
            DepthBufferWriteEnable = state.depthWriteEnable,
            DepthBufferFunction = depthFunc,

            StencilEnable = state.stencilEnable,
            StencilFunction = stencilFunc,
            StencilPass = pass,
            StencilFail = fail,
            StencilDepthBufferFail = depthFail,

            ReferenceStencil = state.referenceStencil,
            StencilMask = state.stencilMask,
            StencilWriteMask = state.stencilWriteMask,

            TwoSidedStencilMode = state.twoSided,
            CounterClockwiseStencilFunction = ccwFunc,
            CounterClockwiseStencilPass = ccwPass,
            CounterClockwiseStencilFail = ccwFail,
            CounterClockwiseStencilDepthBufferFail = ccwDepthFail,
        };

        state.dirty = false;
        states[index] = state;

        return state.resolved;
    }
}

public static class RasterizerSystem
{
    public static List<RuntimeRasterizer> states = new List<RuntimeRasterizer>();
    private static Dictionary<int, int> _map = new Dictionary<int, int>();
    public static int highestId;

    public static void Reset()
    {
        states.Clear();
        _map.Clear();
        highestId = 0;
    }

    /// <summary>
    /// Looks up a state, creating the slot on first use with MonoGame's
    /// RasterizerState.CullCounterClockwise values -- which is what the sprite batch would
    /// have used anyway.
    /// </summary>
    public static void GetIndex(int id, out int index, out RuntimeRasterizer state)
    {
        if (_map.TryGetValue(id, out index))
        {
            state = states[index];
            return;
        }

        highestId = id > highestId ? id : highestId;
        index = _map[id] = states.Count;

        state = new RuntimeRasterizer
        {
            id = id,
            cullMode = (int)CullMode.CullCounterClockwiseFace,
            fillMode = (int)FillMode.Solid,
            depthBias = 0f,
            slopeScaleDepthBias = 0f,
            multiSampleAntiAlias = true,
            scissorTestEnable = false,
            depthClipEnable = true,
            dirty = true,
        };

        states.Add(state);
    }

    public static bool Exists(int id) => _map.ContainsKey(id);

    /// <summary>
    /// Returns the live state for an id, building it if the configuration changed. Null for
    /// id 0, so an output without one keeps the sprite batch's own default.
    /// </summary>
    public static RasterizerState Resolve(int id)
    {
        if (id <= 0) return null;

        GetIndex(id, out var index, out var state);

        if (state.resolved != null && !state.dirty) return state.resolved;

        RenderStateEnums.TryGetCullMode(state.cullMode, out var cull);
        RenderStateEnums.TryGetFillMode(state.fillMode, out var fill);

        state.resolved = new RasterizerState
        {
            CullMode = cull,
            FillMode = fill,
            DepthBias = state.depthBias,
            SlopeScaleDepthBias = state.slopeScaleDepthBias,
            MultiSampleAntiAlias = state.multiSampleAntiAlias,
            ScissorTestEnable = state.scissorTestEnable,
            DepthClipEnable = state.depthClipEnable,
        };

        state.dirty = false;
        states[index] = state;

        return state.resolved;
    }
}
