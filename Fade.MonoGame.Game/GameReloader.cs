using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using FadeBasic;
using FadeBasic.Launch;

namespace Fade.MonoGame.Game;

public static class GameReloader
{
    private static FileSystemWatcher _watcher;
    static Timer? debounceTimer;
    static readonly object debounceLock = new();

    public static ILaunchable LatestBuild { get; private set; }
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

        Console.WriteLine("Watching files...");
        _watcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(csProjPath),
            Filter = "*.fbasic",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        _watcher.Changed += (sender, args) =>
        {
            HandleUpdate();
        };
        HandleUpdate();

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
        // OnBuild?.Invoke(ctx);
    }
}