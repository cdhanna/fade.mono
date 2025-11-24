using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Game;

public static class GameReloader
{
    private static FileSystemWatcher _fadeScriptWatcher;
    private static FileSystemWatcher _effectWatcher;
    static Timer? debounceTimer;
    static Timer? effectTimer;
    static readonly object debounceLock = new();
    static readonly object effectLock = new();

    public static ILaunchable LatestBuild { get; private set; }
    public static VirtualMachine LatestMachine { get; private set; }
    // public static Action<ILaunchable> OnBuild = _ => { };

    public static string? GetCsprojPath([CallerFilePath] string callerFilePath = "")
    {
#if DEBUG
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
    
    public static void WatchFiles(string csProjPath, CommandCollection commands)
    {
        var files = FadeBasic.Sdk.Fade.GetFadeFilesFromProject(csProjPath);

        var projectDir = Path.GetDirectoryName(csProjPath);
        
        Console.WriteLine("Watching files...");
        _fadeScriptWatcher = new FileSystemWatcher
        {
            Path = projectDir,
            Filter = "*.fbasic",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _effectWatcher = new FileSystemWatcher
        {
            Path = Path.Combine(projectDir, "Content"),
            Filter = "*.fx",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        _fadeScriptWatcher.Changed += (sender, args) =>
        {
            HandleUpdate();
        };
        
        _effectWatcher.Changed += (sender, args) =>
        {
            HandleEffectUpdate();
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

        void HandleEffectUpdate()
        {
            lock (effectLock)
            {
                effectTimer?.Dispose();
                effectTimer = new Timer(_ =>
                {
                    Console.WriteLine($"Detected change(s) to .fx files at {DateTime.Now:HH:mm:ss.fff}");
                    BuildEffects(csProjPath);
                }, null, 100, Timeout.Infinite);
            }
        }
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
        if (!FadeBasic.Sdk.Fade.TryCreateFromProject(csProjPath, commands, out var ctx, out var errs))
        {
            Console.Error.WriteLine("Build errors!");
            Console.Error.WriteLine(errs.ToDisplay());
            return;
        }

        LatestBuild = ctx;
      //  LatestMachine = ctx.Machine;
        // OnBuild?.Invoke(ctx);
    }
}