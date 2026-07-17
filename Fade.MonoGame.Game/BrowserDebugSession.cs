// Browser-only subclass of DebugSession that exposes the queues and a few
// internal helpers the JS bridge needs. Same shape as the WebRuntime
// project's WebDebugSession — it inherits to reach protected fields and
// adds the public surface our Pages/Index.Debug.cs depends on.
//
// Lives in browser TFM only so desktop builds don't ship a Game-side
// subclass that nobody references (desktop drives DebugSession through
// its TCP server).

#if BROWSER
using System.Collections.Generic;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Core;

public sealed class BrowserDebugSession : DebugSession
{
    public BrowserDebugSession(VirtualMachine vm, DebugData dbg, CommandCollection commands, LaunchOptions options, string label)
        : base(vm, dbg, commands, options, label)
    {
        MarkConnected();
    }

    // Re-marks the session as having an attached debugger. Must be called
    // any time the session's connection state could get reset — most
    // importantly after Restart(), which sets debuggerSaidHello=0 +
    // debuggerReset=1. Without that, the next StartDebugging enters a
    // Thread.Sleep(1) wait loop expecting a PROTO_HELLO over a TCP
    // socket that doesn't exist, deadlocking the WASM main thread.
    public void MarkConnected()
    {
        didClientConnect = true;
        hasConnectedDebugger = 1;
        debuggerSaidHello = 1;
        debuggerReset = 0;
    }

    // State-preserving hot-reload rebind for the browser. Reuses Restart (rebind
    // to the new program + REV_REQUEST_RESTART so the page re-sends breakpoints)
    // but KEEPS the paused/running state: a reload accepted while paused must stay
    // paused, not resume. Restart resets the pause counters (for F1's fresh-start
    // semantics), so we snapshot IsPaused and re-assert it, then re-mark connected.
    public void RestartPreservingPause(VirtualMachine vm, DebugData dbg, CommandCollection commands)
    {
        var wasPaused = IsPaused;
        Restart(vm, dbg, commands);
        if (wasPaused)
        {
            // IsPaused => pauseRequestedByMessageId > resumeRequestedByMessageId
            pauseRequestedByMessageId = resumeRequestedByMessageId + 1;
            // Mark the paused breakpoint line as already-hit (Restart cleared it)
            // so the next continue EXECUTES this line and advances, instead of
            // immediately re-firing the same breakpoint at the same spot.
            if (instructionMap != null && instructionMap.TryFindClosestTokenBeforeIndex(vm.instructionIndex, out var tok))
                hitBreakpointToken = tok;
        }
        MarkConnected();
    }

    // Enqueue an inbound (page → VM) message. The base class's
    // receivedMessages is protected; expose a public wrapper.
    public void Enqueue(DebugMessage msg) => receivedMessages.Enqueue(msg);

    // Drain everything the session has produced since the last call. The
    // JS rAF tick calls this after Game1.Update / Game1.Tick so events
    // (paused, stepped, stopped, etc.) flow out promptly.
    public List<DebugMessage> DrainOutbound()
    {
        var result = new List<DebugMessage>();
        while (outboundMessages.TryDequeue(out var msg)) result.Add(msg);
        return result;
    }

    // Surface the DebugData we were constructed with so the JS bridge can
    // emit statementLines for the editor's breakpoint hint gutter.
    public DebugData DebugDataAccess => _dbg;
}
#endif
