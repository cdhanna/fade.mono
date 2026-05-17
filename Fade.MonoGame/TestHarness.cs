using System;
using System.Threading;
using System.Threading.Tasks;
using Fade.MonoGame.Core;
using FadeBasic.Sdk;
using FadeBasic.Testing;

namespace Fade.MonoGame;

public class MonoGameTestHost: IFadeTestHost
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