using System;
using System.IO;

namespace Fade.MonoGame.Core;

public static class FileLog
{
    private static string _path;
    private static readonly object _lock = new();

    public static void Init()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "_lastRun.log");
        try { File.WriteAllText(_path, string.Empty); } catch { }
    }

    public static void WriteLine(string message)
    {
        if (_path == null) Init();
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_path,
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
