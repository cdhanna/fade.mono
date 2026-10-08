using System;
using System.IO;
using FadeBasic.Json;

namespace Fade.MonoGame.Core;

/// <summary>
/// The shape of prefs.json. Two arrays of the same length: the value at every index, and
/// whether that index has ever been set.
///
/// Two arrays rather than one because a preference that was set to 0 and a preference that
/// was never set are different things -- `get pref` with a default has to tell them apart --
/// and an array of ints has no way to say "nothing here".
/// </summary>
public class PrefsFile : IJsonable
{
    public int[] values = Array.Empty<int>();
    public int[] taken = Array.Empty<int>();

    public void ProcessJson<T>(ref T op) where T : IJsonOperation
    {
        op.IncludeField(nameof(values), ref values);
        op.IncludeField(nameof(taken), ref taken);
    }
}

/// <summary>
/// Preferences: whole numbers, kept by index, that are still there the next time the game
/// runs. They live in prefs.json in the working directory.
///
/// The file is read once, the first time anything asks, and written again on every change.
/// Writing on every change is deliberate. Preferences change when a player touches a
/// setting, which is rare, and a game has no reliable moment to "save on exit": the window
/// can be closed, or the process killed, without the program hearing about it.
/// </summary>
public static class PrefsSystem
{
    public const string FILE_NAME = "prefs.json";

    /// <summary>
    /// A stray huge index would otherwise grow the arrays, and the file, to match. Nothing
    /// needs this many settings; it is a guard against a mistyped number.
    /// </summary>
    public const int MAX_INDEX = 65535;

    private static int[] _values = Array.Empty<int>();
    private static bool[] _taken = Array.Empty<bool>();
    private static bool _loaded;
    private static bool _reportedFailure;

    public static string FilePath => Path.Combine(Environment.CurrentDirectory, FILE_NAME);

    /// <summary>
    /// Forget what is in memory, so that the next use reads the file again. The file itself
    /// is left alone: a reload of the program must not lose the player's settings.
    /// </summary>
    public static void Reset()
    {
        _values = Array.Empty<int>();
        _taken = Array.Empty<bool>();
        _loaded = false;
    }

    public static bool IsValidIndex(int index) => index >= 0 && index <= MAX_INDEX;

    public static bool Exists(int index)
    {
        EnsureLoaded();
        return index >= 0 && index < _taken.Length && _taken[index];
    }

    public static int Get(int index, int fallback)
    {
        return Exists(index) ? _values[index] : fallback;
    }

    public static void Set(int index, int value)
    {
        if (!IsValidIndex(index))
        {
            Report($"pref index {index} is out of range (0 to {MAX_INDEX}), so it was not saved");
            return;
        }

        EnsureLoaded();
        if (index >= _values.Length)
        {
            Array.Resize(ref _values, index + 1);
            Array.Resize(ref _taken, index + 1);
        }

        // Nothing changed, so there is nothing to write. A program that sets its
        // preferences every frame would otherwise rewrite the file every frame.
        if (_taken[index] && _values[index] == value) return;

        _values[index] = value;
        _taken[index] = true;
        Save();
    }

    /// <summary>The highest index that is taken, or 0 when none are.</summary>
    public static int HighestTakenIndex()
    {
        EnsureLoaded();
        for (var i = _taken.Length - 1; i >= 0; i--)
        {
            if (_taken[i]) return i;
        }

        return 0;
    }

    /// <summary>Forget every preference, and delete the file.</summary>
    public static void Clear()
    {
        _values = Array.Empty<int>();
        _taken = Array.Empty<bool>();
        _loaded = true; // there is nothing to read back

#if !BROWSER
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception ex)
        {
            Report($"could not delete {FILE_NAME}: {ex.Message}");
        }
#endif
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

#if !BROWSER
        try
        {
            if (!File.Exists(FilePath)) return;

            var file = JsonableExtensions.FromJson<PrefsFile>(File.ReadAllText(FilePath));
            var values = file.values ?? Array.Empty<int>();
            var taken = file.taken ?? Array.Empty<int>();

            // A file somebody edited by hand might have arrays of different lengths. Only
            // the indexes both arrays cover can be trusted.
            var count = Math.Min(Math.Min(values.Length, taken.Length), MAX_INDEX + 1);
            _values = new int[count];
            _taken = new bool[count];
            for (var i = 0; i < count; i++)
            {
                _values[i] = values[i];
                _taken[i] = taken[i] != 0;
            }
        }
        catch (Exception ex)
        {
            // A damaged file must not stop the game from starting. It starts with no
            // preferences, and the next `set pref` writes a good file over the bad one.
            Report($"could not read {FILE_NAME}, so starting with no preferences: {ex.Message}");
            _values = Array.Empty<int>();
            _taken = Array.Empty<bool>();
        }
#endif
    }

    private static void Save()
    {
#if !BROWSER
        try
        {
            var file = new PrefsFile
            {
                values = (int[])_values.Clone(),
                taken = new int[_taken.Length]
            };
            for (var i = 0; i < _taken.Length; i++)
            {
                file.taken[i] = _taken[i] ? 1 : 0;
            }

            // Written beside the real file and then swapped in, so that a crash or a full
            // disk half way through leaves the old preferences rather than half a file.
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
        // Once. A game that cannot write its preferences (a read-only folder, say) would
        // otherwise say so on every change, forever.
        if (_reportedFailure) return;
        _reportedFailure = true;
        Console.Error.WriteLine($"[fade] prefs: {message}");
    }
}
