using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core;

/// <summary>
/// Which MonoGame backend is actually running, decided at runtime.
///
/// This assembly is backend-agnostic: it compiles against whichever desktop
/// MonoGame package satisfies the compiler, and the GAME chooses the one that
/// ships (DesktopGL vs Native) with a plain PackageReference. Nothing here can
/// know that at build time — deliberately, because a build-time answer would
/// have to travel from the consuming repo into this one, and NuGet restore
/// cannot carry it. So we ask the assembly that is loaded.
///
/// The tell is embedded effect resources. DesktopGL carries the stock effects as
/// *.ogl.mgfxo; the Native build (Vulkan / DX12) compiles them into its native
/// runtime library and embeds none.
///
/// The Web build is a genuinely different framework (KNI BlazorGL) that bakes
/// and ships only the OpenGL variant, so the answer there is pinned at compile
/// time via the BROWSER define rather than detected.
/// </summary>
public static class GraphicsBackend
{
#if BROWSER
    /// <summary>"ogl" — a Web/KNI build bakes and ships only the ogl variant.</summary>
    public static string Suffix { get; } = "ogl";

    /// <summary>Human-readable name: "OpenGL".</summary>
    public static string Name { get; } = "OpenGL";
#else
    /// <summary>"ogl" or "vk" — the suffix of the baked shader variant to load.</summary>
    public static string Suffix { get; } = IsOpenGl() ? "ogl" : "vk";

    /// <summary>Human-readable name, for debug UI: "OpenGL" or "Vulkan".</summary>
    public static string Name { get; } = IsOpenGl() ? "OpenGL" : "Vulkan";
#endif

    static bool IsOpenGl()
    {
        try
        {
            return typeof(GraphicsDevice).Assembly
                .GetManifestResourceNames()
                .Any(n => n.EndsWith(".ogl.mgfxo", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // Never let a debug readout take the process down; OpenGL is the
            // older, likelier default if reflection is ever restricted.
            return true;
        }
    }
}
