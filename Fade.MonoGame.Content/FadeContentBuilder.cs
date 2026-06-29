using System.Collections.Generic;

namespace Fade.MonoGame.Content;

/// <summary>
/// Default <see cref="IContentBuilder"/> — delegates to the FadeContentSystem
/// MGCB pipeline. A host registers an instance in <c>Game.Services</c> (the
/// template does this for Debug desktop builds) to enable in-process content
/// building + live reload. Lives here because the pipeline it wraps is
/// desktop-only and heavy.
/// </summary>
public sealed class FadeContentBuilder : IContentBuilder
{
    public void Build(string assetsFolder, ContentEntry[] entries, int count)
        => FadeContentSystem.Build(assetsFolder, entries, count);

    public void Build(string assetsFolder, ContentEntry[] entries, int count, List<string> onlyPaths)
        => FadeContentSystem.Build(assetsFolder, entries, count, onlyPaths);
}
