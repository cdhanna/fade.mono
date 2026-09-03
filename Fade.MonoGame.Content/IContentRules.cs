using MonoGame.Framework.Content.Pipeline.Builder;

namespace Fade.MonoGame.Content;

/// <summary>
/// A game's own content-pipeline rules, applied after the engine's defaults.
/// </summary>
/// <remarks>
/// <para>THIS EXISTS SO THE ENGINE DOES NOT HAVE TO KNOW A GAME'S CONVENTIONS. The engine
/// previously carried a hardcoded rule turning premultiplication off for <c>*_packed.png</c>,
/// which is a BeatBoys naming convention that has no business in a general-purpose engine. A
/// game states its own rules here instead.</para>
///
/// <para>It has to be an assembly loaded by PATH rather than a project reference, because of
/// where the content build runs. Release shells out to the fadecontent CLI during the game
/// project's own compilation, so the CLI cannot reference the game — and the game must not
/// reference the pipeline, or the publish carries MGCB's per-platform tool binaries (that
/// mistake cost 314 MB of ffmpeg, crunch and basisu in a shipped build). A separate small
/// assembly, built first and handed over by path, satisfies both.</para>
///
/// <para>BOTH BUILD PATHS MUST USE THE SAME RULES or Debug and Release disagree about the
/// content they produce, which is the single most expensive class of bug in this pipeline. The
/// CLI takes <c>--rules &lt;dll&gt;</c>; the in-process Debug builder takes an instance
/// directly. Wire both.</para>
/// </remarks>
public interface IContentRules
{
    /// <summary>
    /// Adjust the collection after the engine's default rules have been added. Later rules win,
    /// so an Include here overrides the engine's wildcard for the same pattern.
    /// </summary>
    void Configure(ContentCollection collection);
}
