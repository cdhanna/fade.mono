using System.Linq;
// CLI entrypoint for the Fade content pipeline.
//
// Drives FadeContentSystem.Build (the same in-process builder the Game uses
// for Debug live-reload) so that builds — both consumer game projects and the
// engine's baked FadeSpriteBatchEffect — can compile raw assets to XNBs via a
// single canonical path instead of raw `dotnet mgcb`.
//
//   fadecontent --platform <Desktop|DesktopVK|Web> --source <assetsDir>
//               --output <xnbDir> --intermediate <objDir>
//
// Platform semantics match FadeContentSystem.Build:
//   Desktop    DesktopGL pipeline — GLSL via MGFXC (Wine off Windows), SM 3.0
//   DesktopVK  DesktopVK pipeline — SPIR-V via DXC (no Wine), SM 6.0
//   Web        DesktopGL pipeline, then patched in place for KNI BlazorGL
//              (MGFX v11->v10, SoundEffect loopLength)

string platform = "Desktop";
string source = "";
string output = "";
string intermediate = "";

// A game's own content rules, loaded by path. See IContentRules for why it is a path and not a
// project reference: this CLI runs during the game project's compilation and cannot reference it,
// and the game must not reference the pipeline or its publish carries MGCB's tool binaries.
string? rulesPath = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--platform"     when i + 1 < args.Length: platform     = args[++i]; break;
        case "--source"       when i + 1 < args.Length: source       = args[++i]; break;
        case "--output"       when i + 1 < args.Length: output       = args[++i]; break;
        case "--intermediate" when i + 1 < args.Length: intermediate = args[++i]; break;
        case "--rules"        when i + 1 < args.Length: rulesPath    = args[++i]; break;
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

// Rejected rather than defaulted: an unrecognised platform used to fall through
// to DesktopGL, so a typo'd -p:FadeMonoGamePlatform produced GL shaders that a
// Vulkan build loads and fails on at runtime, with nothing said at build time.
if (!(string.Equals(platform, "Desktop", StringComparison.OrdinalIgnoreCase)
   || string.Equals(platform, "DesktopVK", StringComparison.OrdinalIgnoreCase)
   || string.Equals(platform, "Web", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine($"[E] unknown --platform '{platform}'; expected Desktop, DesktopVK or Web");
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
    Fade.MonoGame.Content.IContentRules? rules = null;
    if (!string.IsNullOrEmpty(rulesPath))
    {
        // Fail loudly. A typo here would otherwise build content with the engine defaults and
        // ship subtly wrong art -- exactly what the rules exist to prevent.
        if (!File.Exists(rulesPath))
        {
            Console.Error.WriteLine($"[E] --rules assembly not found: {rulesPath}");
            return 1;
        }
        var asm  = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(rulesPath));
        var type = asm.GetTypes().FirstOrDefault(t =>
            typeof(Fade.MonoGame.Content.IContentRules).IsAssignableFrom(t)
            && !t.IsAbstract && !t.IsInterface);
        if (type == null)
        {
            Console.Error.WriteLine($"[E] no IContentRules implementation in {rulesPath}");
            return 1;
        }
        rules = (Fade.MonoGame.Content.IContentRules?)Activator.CreateInstance(type);
        Console.WriteLine($"[fadecontent] content rules: {type.FullName}");
    }

    FadeContentSystem.Build(source, platform, output, intermediate, rules);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[E] content build failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}
