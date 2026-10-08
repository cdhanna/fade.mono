using System;
using System.IO;
using FadeBasic.Json;

namespace Fade.MonoGame.Core;

/// <summary>The shape of window.json: the size the window was the last time it changed.</summary>
public class WindowMemoryFile : IJsonable
{
    public int width;
    public int height;

    public void ProcessJson<T>(ref T op) where T : IJsonOperation
    {
        op.IncludeField(nameof(width), ref width);
        op.IncludeField(nameof(height), ref height);
    }
}

/// <summary>
/// Remembers how big the window was, in window.json in the working directory, so that a
/// game can open at the size the player left it. Off until `remember window size` turns it on.
///
/// Kept apart from the preferences on purpose. Preferences are the program's own numbers,
/// under indexes the program chose; this is the engine's bookkeeping about its own window,
/// and a program should not have to give up a preference index, or know one, to get it.
/// </summary>
public static class WindowMemorySystem
{
    public const string FILE_NAME = "window.json";

    // Smaller than this and a window is hard to grab hold of to make bigger again. A size
    // below it in the file is a damaged file, not something a player chose.
    private const int MIN_WIDTH = 160;
    private const int MIN_HEIGHT = 120;

    public static bool enabled;

    private static int _savedWidth;
    private static int _savedHeight;
    private static bool _reportedFailure;

    public static string FilePath => Path.Combine(Environment.CurrentDirectory, FILE_NAME);

    /// <summary>
    /// Stop remembering. The file is left alone: a reload of the program must not lose the
    /// size, and the program turns remembering back on when it runs again.
    /// </summary>
    public static void Reset()
    {
        enabled = false;
        _savedWidth = _savedHeight = 0;
    }

    /// <summary>
    /// The size that was remembered, if there is one that makes sense.
    /// </summary>
    public static bool TryLoad(out int width, out int height)
    {
        width = height = 0;
#if !BROWSER
        try
        {
            if (!File.Exists(FilePath)) return false;

            var file = JsonableExtensions.FromJson<WindowMemoryFile>(File.ReadAllText(FilePath));
            if (file.width < MIN_WIDTH || file.height < MIN_HEIGHT) return false;

            width = file.width;
            height = file.height;
            _savedWidth = width;
            _savedHeight = height;
            return true;
        }
        catch (Exception ex)
        {
            Report($"could not read {FILE_NAME}, so the window keeps the size it has: {ex.Message}");
        }
#endif
        return false;
    }

    /// <summary>
    /// The window is this size now. Called whenever the size settles: at the end of a drag,
    /// and when the program sets a size. Not called while the game is fullscreen, because the
    /// size of the monitor is not a size the player picked for the window.
    /// </summary>
    public static void NoteSize(int width, int height)
    {
        if (!enabled) return;
        if (width < MIN_WIDTH || height < MIN_HEIGHT) return;
        if (width == _savedWidth && height == _savedHeight) return;

        _savedWidth = width;
        _savedHeight = height;

#if !BROWSER
        try
        {
            var file = new WindowMemoryFile { width = width, height = height };

            // Written beside the real file and then swapped in, so that a crash half way
            // through leaves the old size rather than half a file.
            var path = FilePath;
            var temp = path + ".tmp";
            File.WriteAllText(temp, file.Jsonify());
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            Report($"could not write {FILE_NAME}: {ex.Message}");
        }
#endif
    }

    private static void Report(string message)
    {
        if (_reportedFailure) return;
        _reportedFailure = true;
        Console.Error.WriteLine($"[fade] window memory: {message}");
    }
}
