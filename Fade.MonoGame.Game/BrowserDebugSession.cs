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
