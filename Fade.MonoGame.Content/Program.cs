// CLI entrypoint for the Fade content pipeline.
//
// Drives FadeContentSystem.Build (the same in-process builder the Game uses
// for Debug live-reload) so that builds — both consumer game projects and the
// engine's baked FadeSpriteBatchEffect — can compile raw assets to XNBs via a
// single canonical path instead of raw `dotnet mgcb`.
//
//   fadecontent --platform <Desktop|Web> --source <assetsDir>
//               --output <xnbDir> --intermediate <objDir>
//
// Platform semantics match FadeContentSystem.Build: XNBs are always produced
// with the DesktopGL pipeline; "Web" additionally patches them in place for
// KNI BlazorGL (MGFX v11->v10, SoundEffect loopLength).

string platform = "Desktop";
string source = "";
string output = "";
string intermediate = "";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--platform"     when i + 1 < args.Length: platform     = args[++i]; break;
        case "--source"       when i + 1 < args.Length: source       = args[++i]; break;
        case "--output"       when i + 1 < args.Length: output       = args[++i]; break;
        case "--intermediate" when i + 1 < args.Length: intermediate = args[++i]; break;
        default:
            Console.Error.WriteLine($"[E] unknown or incomplete argument: {args[i]}");
            return 2;
    }
}

if (string.IsNullOrEmpty(source))
{
    Console.Error.WriteLine("[E] --source <assetsDir> is required");
    return 2;
}

if (!Directory.Exists(source))
{
    Console.Error.WriteLine($"[E] source directory not found: {source}");
    return 2;
}

try
{
    Console.WriteLine($"[fadecontent] platform={platform} source={source} output={output} intermediate={intermediate}");
    FadeContentSystem.Build(source, platform, output, intermediate);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[E] content build failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}
