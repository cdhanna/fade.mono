using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Sdk;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Core;

public static class GameReloader
{
#if !BROWSER
    private static FileSystemWatcher _fadeScriptWatcher;
    private static FileSystemWatcher _assetWatcher;
    static Timer? debounceTimer;
    static Timer? effectTimer;
    static readonly object debounceLock = new();
    static readonly object effectLock = new();
#endif

    public static ILaunchable LatestBuild { get; private set; }
    public static FadeRuntimeContext LatestRuntime { get; private set; }
    public static VirtualMachine LatestMachine { get; private set; }
    public static DateTimeOffset LastBuildTime { get; private set; }
    // public static Action<ILaunchable> OnBuild = _ => { };

    public static string GetRoot()
    {
        var attr = Assembly.GetEntryAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjectDir");

        return attr?.Value;
    }
    
    public static string? GetCsprojPath([CallerFilePath] string callerFilePath = "")
    {
        // NOTE: gate only on !BROWSER, not DEBUG. This method ships in the engine
        // package, which is packed -c Release, so a `#if DEBUG` here compiles the
        // whole lookup out and the method becomes `return null` for every consumer
        // (that's why the template uses GameReloader.GetRoot() instead). The caller
        // decides when to invoke it (Debug-only via their FADE_CONTENT_HOTRELOAD).
#if !BROWSER
        var dir = Path.GetDirectoryName(callerFilePath);

        while (dir != null && Directory.Exists(dir))
        {
            var csprojFiles = Directory.GetFiles(dir, "*.csproj");
            if (csprojFiles.Length > 0)
            {
                return Path.GetFullPath(csprojFiles[0]);
            }

            dir = Path.GetDirectoryName(dir); // Move up
        }
#endif
        return null;
    }
    
    private static readonly HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".fx",
        ".fxh",
        ".png"
    };
    

#if !BROWSER
    public static void WatchFiles(string csProjPath, CommandCollection commands)
    {
        var files = FadeBasic.Sdk.Fade.GetFadeFilesFromProject(csProjPath);

        var projectDir = Path.GetDirectoryName(csProjPath);
        var changeFiles = new HashSet<string>();
        
        Console.WriteLine("Watching files...");
        _fadeScriptWatcher = new FileSystemWatcher
        {
            Path = projectDir,
            Filter = "*.fbasic",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _assetWatcher = new FileSystemWatcher
        {
            Path = projectDir,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        Build(csProjPath, commands);
        
        _fadeScriptWatcher.Changed += (sender, args) =>
        {
            HandleUpdate();
        };
        _fadeScriptWatcher.Created += (sender, args) =>
        {
            HandleUpdate();
        };
        _fadeScriptWatcher.Renamed += (sender, args) =>
        {
            HandleUpdate();
        };

        _assetWatcher.Created += (sender, args) =>
        {
            HandleEffectUpdate(args.FullPath);
        };
        _assetWatcher.Renamed += (sender, args) =>
        {
            HandleEffectUpdate(args.FullPath);
        };
        _assetWatcher.Changed += (sender, args) =>
        {
            HandleEffectUpdate(args.FullPath);
        };
        void HandleUpdate()
        {
            lock (debounceLock)
            {
                debounceTimer?.Dispose(); // Reset existing timer
                debounceTimer = new Timer(_ =>
                {
                    Console.WriteLine($"Detected change(s) to .fbasic files at {DateTime.Now:HH:mm:ss.fff}");
                    
                    Build(csProjPath, commands);
                }, null, 300, Timeout.Infinite);
            }
        }

        void HandleEffectUpdate(string fxPath)
        {
            if (!_extensions.Contains(Path.GetExtension(fxPath)))
                return;
            lock (effectLock)
            {
                changeFiles.Add(fxPath);
                effectTimer?.Dispose();
                effectTimer = new Timer(_ =>
                {
                    Console.WriteLine($"Detected change(s) to asset files at {DateTime.Now:HH:mm:ss.fff}");
                    BuildEffects2(changeFiles.ToList());
                    changeFiles.Clear();
                    
                }, null, 100, Timeout.Infinite);
            }
        }
    }

    // Content-only live reload for package consumers (the dotnet-new template).
    // Watches the project's source assets and rebuilds changed ones via the
    // injected IContentBuilder; ContentWatcher then hot-swaps the rebuilt XNB.
    // Unlike WatchFiles (used by the in-repo example) this does NOT reload the
    // .fbasic program — template games run a statically generated GeneratedFade.
    // Requires the project to expose its dir via [AssemblyMetadata("ProjectDir")]
    // (the template csproj does this in Debug desktop); otherwise it no-ops.
    public static void WatchContentForReload()
    {
        var projectDir = GetRoot();
        if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            return;

        var changeFiles = new HashSet<string>();
        _assetWatcher = new FileSystemWatcher
        {
            Path = projectDir,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        void HandleEffectUpdate(string fxPath)
        {
            if (!_extensions.Contains(Path.GetExtension(fxPath)))
                return;
            lock (effectLock)
            {
                changeFiles.Add(fxPath);
                effectTimer?.Dispose();
                effectTimer = new Timer(_ =>
                {
                    Console.WriteLine($"Detected change(s) to asset files at {DateTime.Now:HH:mm:ss.fff}");
                    BuildEffects2(changeFiles.ToList());
                    changeFiles.Clear();
                }, null, 100, Timeout.Infinite);
            }
        }

        _assetWatcher.Created += (s, a) => HandleEffectUpdate(a.FullPath);
        _assetWatcher.Renamed += (s, a) => HandleEffectUpdate(a.FullPath);
        _assetWatcher.Changed += (s, a) => HandleEffectUpdate(a.FullPath);
    }

    public static void BuildEffects2(List<string> paths)
    {
        ContentSystem.BuildSomeContent(paths);
    }

    public static void BuildEffects(string csProjPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build {csProjPath} -t:BuildAndCopyContent --tl:off --no-restore --no-dependencies",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = new Process();
        proc.StartInfo = psi;

        proc.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

        proc.Start();

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        proc.WaitForExit();
    }


    public static void Build(string csProjPath, CommandCollection commands)
    {
        GameSystem.ResetMacros();
        if (!FadeBasic.Sdk.Fade.TryCreateFromProject(csProjPath, commands, out var ctx, out var errs))
        {
            Console.Error.WriteLine("Build errors!");
            Console.Error.WriteLine(errs.ToDisplay());
            return;
        }

        SetBuild(ctx);
    }
#endif

    // Browser path: WebRuntime.MonoGame compiles a Fade source string
    // in-memory and hands the FadeRuntimeContext here to install it as the
    // active build. This mirrors what Build(csProjPath, ...) does on desktop,
    // minus the filesystem path. Available on both TFMs so callers can use
    // the same API regardless of platform.
    public static void SetBuild(FadeRuntimeContext ctx)
    {
        LatestRuntime = ctx;
        LatestBuild = ctx;
        LastBuildTime = DateTimeOffset.Now;
    }
}