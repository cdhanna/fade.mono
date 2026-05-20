using System;
using System.Threading;
using System.Threading.Tasks;
using Fade.MonoGame.Core;
using FadeBasic.Sdk;
using FadeBasic.Testing;

namespace Fade.MonoGame;

// IFadeTestHost implementation that runs each test on the live Game1 VM
// via the test-queue mechanism (queuedTests → Update-loop dequeue →
// ResetFade-with-entry → currentTest.source.SetResult on completion).
//
// Lives in Fade.MonoGame.Game (not the desktop root project) so both the
// desktop Program.cs and the browser WebRuntime.MonoGame's Index.razor.cs
// can construct it against the same Game1 instance. The namespace stays
// `Fade.MonoGame` so desktop's existing `using Fade.MonoGame;` still
// resolves after the move.
//
// Lifecycle expectations (per IFadeTestHost):
//   - InitializeAsync: once-per-process. Currently a no-op — the Game1
//     instance is constructed externally and passed in.
//   - BeforeAllTestsAsync: once-per-session. Good place for "push assets"
//     work that needs to happen before any test runs.
//   - RunTestAsync: per test. Delegates to game.QueueTest, which returns
//     a Task that completes when the test VM finishes.
//   - AfterAllTestsAsync: once-per-session. Flips game.allTestsDone so the
//     Update-loop's test-mode branch can fall through to Quit(). The
//     browser typically does NOT call this between interactive test
//     invocations — instead, the page leaves test mode by calling
//     Game1.SetTestMode(false) + LoadProgram(user source) to resume
//     normal play.
public class MonoGameTestHost : IFadeTestHost
{
    public Game1 game;

    public MonoGameTestHost(Game1 game)
    {
        this.game = game;
    }

    public Task InitializeAsync(FadeTestSessionContext ctx, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task BeforeAllTestsAsync(FadeTestSessionContext ctx, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task<FadeTestResult> RunTestAsync(FadeTestRunContext ctx, CancellationToken ct)
    {
        return game.QueueTest(ctx, ct);
    }

    public Task AfterAllTestsAsync(FadeTestSessionContext ctx, CancellationToken ct)
    {
        game.allTestsDone = true;
        FileLog.WriteLine("All Tests Complete");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
