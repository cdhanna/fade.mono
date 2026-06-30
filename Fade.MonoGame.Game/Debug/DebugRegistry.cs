// Global lookup of IDebugProvider instances keyed by TypeName.
// Populated by Game1 at boot (one Register call per system) and
// consumed by the browser JS bridge in WebRuntime.MonoGame/Pages/
// Index.razor.cs — its JSInvokable methods turn "list sprites" /
// "get sprite 5" / "set sprite 5 position.X" into the matching
// provider call.
//
// Static state matches the rest of the game code's static-system
// pattern (SpriteSystem, TransformSystem, etc.) — one process, one
// registry. Tests reset via Clear() between fixtures.

using System.Collections.Generic;

namespace Fade.MonoGame.Core.Debug;

public static class DebugRegistry
{
    static readonly Dictionary<string, IDebugProvider> _providers = new();

    public static void Register(IDebugProvider provider)
    {
        if (provider == null) return;
        // Re-registering the same TypeName replaces the entry. Game1's
        // boot path is idempotent across reloads; without replacement
        // a hot-reload would leave stale references to old SpriteSystem
        // state in the registry.
        _providers[provider.TypeName] = provider;
    }

    public static IDebugProvider Get(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        _providers.TryGetValue(typeName, out var p);
        return p;
    }

    public static IReadOnlyCollection<string> ListTypes() => _providers.Keys;

    public static void Clear() => _providers.Clear();
}
