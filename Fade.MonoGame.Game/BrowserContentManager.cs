#if BROWSER
using System;
using System.Collections.Generic;
using System.IO;
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

    public void ClearAssets() => _assets.Clear();

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
