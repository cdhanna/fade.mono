using System.Collections.Generic;

namespace Fade.MonoGame.Content;

/// <summary>
/// Optional content-build seam. <c>Fade.MonoGame.Game</c> resolves an
/// implementation from <c>Game.Services</c> (registered by the host — e.g. the
/// <c>dotnet new fadebasic-monogame</c> template's Program.cs, in Debug desktop
/// builds) and, when present, builds/rebuilds the project's assets in-process so
/// they hot-reload. When no implementation is registered (Release, Web) the
/// engine simply skips it and uses the baked/published content.
///
/// The real implementation, <c>FadeContentBuilder</c>, lives in
/// <c>Fade.MonoGame.Content</c>, which carries the desktop-only MGCB pipeline —
/// keeping that heavy, native, desktop-only dependency out of <c>.Game</c> and
/// out of every shipped/web build.
/// </summary>
public interface IContentBuilder
{
    void Build(string assetsFolder, ContentEntry[] entries, int count);
    void Build(string assetsFolder, ContentEntry[] entries, int count, List<string> onlyPaths);
}
