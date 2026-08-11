using System;
using System.Collections.Generic;
using Fade.MonoGame.Contracts;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Core;

/// <summary>
/// Host-supplied systems the engine drives each frame, resolved from
/// <c>Game.Services</c> at startup.
///
/// Same seam as <see cref="ContentSystem.ResolveContentBuilder"/>: the engine looks
/// for an implementation the host registered, and does nothing when there isn't one.
/// That keeps optional capabilities — a content pipeline, a netcode plugin — out of
/// the engine assembly and out of builds that don't want them.
///
/// A host registers one system, or an array of them:
/// <code>
/// game.Services.AddService(typeof(IFadeHostSystem), new MyNetSystem());
/// // or
/// game.Services.AddService(typeof(IFadeHostSystem[]), new IFadeHostSystem[] { a, b });
/// </code>
/// </summary>
public static class HostSystemRegistry
{
    static readonly List<IFadeHostSystem> Systems = new List<IFadeHostSystem>();

    public static bool Any => Systems.Count > 0;

    public static IReadOnlyList<IFadeHostSystem> All => Systems;

    public static void Reset() => Systems.Clear();

    /// <summary>
    /// Pull registered systems out of the service provider and initialize them.
    /// Called once from Game1.Initialize.
    /// </summary>
    public static void Resolve(Game game)
    {
        Systems.Clear();
        if (game?.Services == null) return;

        if (game.Services.GetService(typeof(IFadeHostSystem[])) is IFadeHostSystem[] many)
        {
            foreach (var system in many)
                if (system != null) Systems.Add(system);
        }

        if (game.Services.GetService(typeof(IFadeHostSystem)) is IFadeHostSystem single
            && !Systems.Contains(single))
        {
            Systems.Add(single);
        }

        foreach (var system in Systems)
        {
            try { system.Initialize(game.Services); }
            catch (Exception ex) { Console.Error.WriteLine($"[host-system] Initialize failed: {ex}"); }
        }

        if (Systems.Count > 0)
            Console.WriteLine($"[host-system] {Systems.Count} registered");
    }

    // A misbehaving plugin should not take the engine down mid-frame, and the
    // exception has to be visible rather than swallowed — hence log-and-continue
    // rather than either crashing or ignoring.

    public static void BeforeVmTick(GameTime time)
    {
        for (var i = 0; i < Systems.Count; i++)
        {
            try { Systems[i].BeforeVmTick(time.ElapsedGameTime.TotalSeconds, time.TotalGameTime.TotalSeconds); }
            catch (Exception ex) { Console.Error.WriteLine($"[host-system] BeforeVmTick failed: {ex}"); }
        }
    }

    public static void AfterVmTick(GameTime time)
    {
        for (var i = 0; i < Systems.Count; i++)
        {
            try { Systems[i].AfterVmTick(time.ElapsedGameTime.TotalSeconds, time.TotalGameTime.TotalSeconds); }
            catch (Exception ex) { Console.Error.WriteLine($"[host-system] AfterVmTick failed: {ex}"); }
        }
    }

    public static void Draw(GameTime time)
    {
        for (var i = 0; i < Systems.Count; i++)
        {
            try { Systems[i].Draw(time.ElapsedGameTime.TotalSeconds, time.TotalGameTime.TotalSeconds); }
            catch (Exception ex) { Console.Error.WriteLine($"[host-system] Draw failed: {ex}"); }
        }
    }

    /// <summary>
    /// Ask every system whether a reload may proceed. The first refusal wins, and its
    /// reason is surfaced so the user isn't left pressing F1 at an inert bar.
    /// </summary>
    public static bool CanReload(ReloadKind kind, out string reason)
    {
        for (var i = 0; i < Systems.Count; i++)
        {
            try
            {
                if (!Systems[i].CanReload(kind, out var why))
                {
                    reason = string.IsNullOrEmpty(why)
                        ? $"{Systems[i].GetType().Name} blocked the reload"
                        : why;
                    return false;
                }
            }
            catch (Exception ex) { Console.Error.WriteLine($"[host-system] CanReload failed: {ex}"); }
        }

        reason = null;
        return true;
    }

    public static void Shutdown()
    {
        for (var i = 0; i < Systems.Count; i++)
        {
            try { Systems[i].Shutdown(); }
            catch (Exception ex) { Console.Error.WriteLine($"[host-system] Shutdown failed: {ex}"); }
        }
    }
}
