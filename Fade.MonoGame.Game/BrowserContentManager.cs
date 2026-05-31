#if BROWSER
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        _assets[name] = bytes;
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
        _assets.Remove(name);
        if (_loadedAssetsField?.GetValue(this) is IDictionary loaded && loaded.Contains(name))
        {
            var existing = loaded[name];
            loaded.Remove(name);
            if (existing is IDisposable d)
            {
                try { d.Dispose(); } catch { /* GC will clean up */ }
            }
        }
    }

    public void ClearAssets()
    {
        _assets.Clear();
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
