#if BROWSER
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Content;

namespace Fade.MonoGame.Core;

// Browser-only ContentManager that serves XNBs from an in-memory dictionary
// instead of TitleContainer / filesystem. The page (Playground main.ts)
// reads `.xnb` bytes out of OPFS and calls Game1.RegisterAsset(name, bytes)
// before each LoadProgram; that fills this manager's `Assets` map. When
// fbasic source later runs `texture 1, "Catfish"`, the runtime calls
// `Content.Load<Texture2D>("Catfish")`, which lands in OpenStream below
// and resolves against the dict.
//
// Naming convention: asset names match the OPFS filename minus the `.xnb`
// extension. So `Catfish.xnb` in the project registers under "Catfish" and
// fbasic loads it as `texture 1, "Catfish"`. Subfolders are not supported
// yet — flat names only, matching the workspace layout.
public sealed class BrowserContentManager : ContentManager
{
    // Asset name → XNB bytes. Lookup happens in OpenStream; mutation goes
    // through RegisterAsset / Clear so callers don't need to reach into the
    // dictionary directly.
    private readonly Dictionary<string, byte[]> _assets =
        new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    public BrowserContentManager(IServiceProvider services) : base(services)
    {
    }

    // Asset names that have been re-registered since the last drain via
    // ConsumeReloadedAssets(). RenderSystem.RefreshEffects polls this each
    // frame so an effect whose .fx source changed in the editor gets re-
    // Loaded into its RuntimeEffect slot without the game restarting.
    //
    // We only care about *replacements*, not the initial registration —
    // first-time RegisterAsset is just the asset becoming available, not
    // a hot-reload event. RuntimeEffect's first Load happens via the
    // explicit `effect` command at program start; reloads are what we
    // signal here.
    private readonly HashSet<string> _reloadedSinceDrain =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Set of names that have been UnregisterAsset'd since their most-
    // recent RegisterAsset call. The playground's syncAssetsToRuntime
    // emits an unregister+register pair for any asset whose bytes
    // changed — by the time RegisterAsset runs, _assets no longer
    // contains the name, so a simple ContainsKey check would miss
    // these. Tracking the recent-unregister set bridges the gap.
    private readonly HashSet<string> _recentlyUnregistered =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public void RegisterAsset(string name, byte[] bytes)
    {
        if (string.IsNullOrEmpty(name)) return;
        // ContentManager strips its own .xnb extension before calling
        // OpenStream, so register under the bare name. Tolerate callers
        // that pass the extension anyway.
        if (name.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - 4);
        }
        bool isReload = _recentlyUnregistered.Remove(name) || _assets.ContainsKey(name);
        _assets[name] = bytes;
        if (isReload) _reloadedSinceDrain.Add(name);
    }

    /// <summary>
    /// Returns the set of asset names that have been re-registered since
    /// the previous call, then clears the internal set. Mainly used by
    /// RenderSystem.RefreshEffects to detect which Effect / Texture
    /// instances need re-Loading mid-game.
    /// </summary>
    public IReadOnlyCollection<string> ConsumeReloadedAssets()
    {
        if (_reloadedSinceDrain.Count == 0) return Array.Empty<string>();
        var snapshot = _reloadedSinceDrain.ToArray();
        _reloadedSinceDrain.Clear();
        return snapshot;
    }

    public bool TryGetAsset(string name, out byte[] bytes) =>
        _assets.TryGetValue(name, out bytes);

    public bool HasAsset(string name) => _assets.ContainsKey(name);

    public IEnumerable<string> RegisteredNames => _assets.Keys;

    /// <summary>
    /// Evict a single asset by name. Removes the registered bytes AND
    /// any cached Texture2D / SpriteFont / etc. instance, disposing
    /// GPU resources. Used by the playground's per-asset sync path to
    /// invalidate stale entries without nuking the whole asset registry
    /// the way ClearAssets does.
    /// </summary>
    // KNI's ContentManager doesn't expose its loaded-assets dictionary
    // (it's private, unlike MonoGame's protected `LoadedAssets`). We
    // need to evict by name to invalidate stale Texture2D / SpriteFont
    // instances after RegisterAsset replaces an asset's bytes. Reach in
    // via reflection — the field is named `loadedAssets` in both
    // MonoGame and KNI as of net8.0, but try a couple of fallback names
    // so a future rename doesn't silently start re-using disposed
    // textures.
    private static readonly FieldInfo _loadedAssetsField =
        ResolveLoadedAssetsField();

    private static FieldInfo ResolveLoadedAssetsField()
    {
        var t = typeof(ContentManager);
        foreach (var name in new[] { "loadedAssets", "_loadedAssets", "LoadedAssets" })
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null) return f;
        }
        return null;
    }

    /// <summary>
    /// Evict a single asset by name. Removes the registered bytes AND
    /// any cached Texture2D / SpriteFont / etc. instance, disposing
    /// GPU resources. Used by the playground's per-asset sync path so
    /// unchanged assets stay cached across Runs (avoiding repeated
    /// `decodeAudioData` + texture re-uploads) while changed ones are
    /// invalidated without nuking the whole asset registry.
    /// </summary>
    public void UnregisterAsset(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (name.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - 4);
        }
        // Remember we evicted this — the next RegisterAsset for the same
        // name is a hot-reload, not a first-time register.
        if (_assets.ContainsKey(name)) _recentlyUnregistered.Add(name);
        _assets.Remove(name);

        // Per-name eviction via reflection into KNI's private loadedAssets
        // dictionary. When this succeeds, the next Content.Load<T>(name)
        // pulls fresh bytes through OpenStream — cheap and surgical.
        bool perNameEvicted = false;
        if (_loadedAssetsField?.GetValue(this) is IDictionary loaded && loaded.Contains(name))
        {
            var existing = loaded[name];
            loaded.Remove(name);
            if (existing is IDisposable d)
            {
                try { d.Dispose(); } catch { /* GC will clean up */ }
            }
            perNameEvicted = true;
        }

        // Fallback: when reflection can't see the field (KNI version skew
        // or a future rename), call the public Unload() — the sledgehammer
        // that disposes every loaded asset and clears the cache. Costs the
        // re-construction of *other* assets on next access (their source
        // bytes are still in our _assets dict, so this is just GPU re-upload
        // work), but guarantees correctness for the asset that was edited.
        //
        // This branch is the difference between "shader hot-reload works"
        // and "shader hot-reload fails until full page refresh" — the
        // latter is what was observed when the reflection path silently
        // failed to find the field.
        if (!perNameEvicted)
        {
            try { Unload(); } catch { /* best effort */ }
        }
    }

    public void ClearAssets()
    {
        _assets.Clear();
        _recentlyUnregistered.Clear();
        _reloadedSinceDrain.Clear();
        // Also flush the base ContentManager's cache of loaded objects
        // (Texture2D, SoundEffect, etc.) so subsequent `Content.Load<T>`
        // calls go back through OpenStream and pick up the fresh bytes.
        // Without this, swapping a texture's underlying bytes via
        // RegisterAsset has no visible effect — Load returns the
        // already-cached GPU Texture2D from the previous run, so macro
        // tweaks (e.g. `dxt5` → `color`) appear to silently fail.
        //
        // Unload() disposes the cached assets too — fine here because
        // the playground calls ClearAssets right before swapping in a
        // brand-new fbasic VM context (BeginPendingProgram), so nothing
        // in the running program still references them.
        Unload();
    }

    protected override Stream OpenStream(string assetName)
    {
        // ContentManager.Load<T>("Foo") calls this with "Foo" (no extension,
        // no RootDirectory prefix). The stock implementation appends ".xnb"
        // and asks TitleContainer for the file; we just hit the dict instead.
        if (_assets.TryGetValue(assetName, out var bytes))
        {
            // Caller owns Dispose; MemoryStream over a buffer we own is fine
            // to hand out because ContentReader reads + discards.
            return new MemoryStream(bytes, writable: false);
        }
        throw new ContentLoadException(
            $"Asset '{assetName}' is not registered with BrowserContentManager. " +
            $"Registered: [{string.Join(", ", _assets.Keys)}]");
    }
}
#endif
