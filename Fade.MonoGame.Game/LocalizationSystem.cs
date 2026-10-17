using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Fade.MonoGame.Localization;

namespace Fade.MonoGame.Core;

/// <summary>
/// What a loc text was given: a key and its args. The text system holds the resolved string as
/// usual; this is what lets it be resolved again when an arg or the language changes.
/// </summary>
public sealed class LocBinding
{
    public string key = "";
    public readonly List<LocArg> args = new List<LocArg>();

    public void SetArg(string name, object value)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i].name, name, StringComparison.OrdinalIgnoreCase)) continue;
            args[i] = new LocArg(args[i].name, value);
            return;
        }
        args.Add(new LocArg(name, value));
    }
}

/// <summary>
/// Localized strings: the string table, the current language, and the texts that are bound to a
/// key ("loc texts").
///
/// The table is a folder of CSVs, one per tab of the strings sheet, loaded with `load strings`.
/// A Debug build reads them straight from the project's Assets folder and reloads them when one
/// changes on disk; any other build reads the copies the content build put in Content.
///
/// Loc texts are kept by text index in a dictionary, beside TextSystem's lists rather than in
/// TextSprite, which must hold no references (see the note at the top of TextSystem.cs). Most
/// texts are plain, so the dictionary stays small.
/// </summary>
public static class LocalizationSystem
{
    public static readonly StringResolver resolver = new StringResolver();

    /// <summary>Text index (as in TextSystem.textSprites) to what it was bound to.</summary>
    public static readonly Dictionary<int, LocBinding> bindings = new Dictionary<int, LocBinding>();

    /// <summary>The folder given to `load strings`, as written there.</summary>
    public static string loadedFolder;

    /// <summary>Where the CSVs were read from.</summary>
    public static string loadedDirectory;

    public static string Language => resolver.language;

    private static readonly HashSet<string> _reported = new HashSet<string>();
    private static FileSystemWatcher _watcher;
    private static Timer _debounce;
    private static int _changedOnDisk;

    // A Debug build knows its project folder. Found once: it is a reflection call, and plain
    // `set text` asks on every call.
    private static readonly bool IsDevBuild = !string.IsNullOrEmpty(GameReloader.GetRoot());

    static LocalizationSystem()
    {
        resolver.warn = Report;
    }

    public static void Reset()
    {
        bindings.Clear();
        resolver.table = new StringTable();
        resolver.language = StringTable.DEFAULT_LANGUAGE;
        loadedFolder = null;
        loadedDirectory = null;
        _reported.Clear();
        StopWatching();
    }

    /// <summary>
    /// Reads every CSV in a folder, named like other assets ("Fish/Lang"). Returns how many keys
    /// were loaded.
    /// </summary>
    public static int Load(string folder)
    {
#if BROWSER
        Report("string tables are not supported in the browser yet");
        return 0;
#else
        var directory = FindDirectory(folder);
        loadedFolder = folder;
        loadedDirectory = directory;
        if (directory == null)
        {
            Report($"no string table folder '{folder}' in Assets or Content");
            resolver.table = new StringTable();
            return 0;
        }

        ReadTable(directory);
        Watch(directory);
        return resolver.table.KeyCount;
#endif
    }

    private static string FindDirectory(string folder)
    {
        var relative = folder.Replace('\\', '/').Trim('/');

        // A Debug build knows its project folder, so it can read the files being edited.
        var root = GameReloader.GetRoot();
        if (!string.IsNullOrEmpty(root))
        {
            var assets = Path.Combine(root, "Assets", relative);
            if (Directory.Exists(assets)) return assets;
        }

        var content = Path.Combine(AppContext.BaseDirectory, "Content", relative);
        return Directory.Exists(content) ? content : null;
    }

    private static void ReadTable(string directory)
    {
        var problems = new List<StringTableProblem>();
        var table = new StringTable();
        foreach (var file in Directory.GetFiles(directory, "*.csv").OrderBy(f => f, StringComparer.Ordinal))
        {
            try
            {
                table.AddCsv(Path.GetFileName(file), File.ReadAllText(file), problems);
            }
            catch (IOException ex)
            {
                // A spreadsheet program may still be writing the file. The watcher tries again.
                Report($"could not read {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        foreach (var p in problems) Report(p.ToString());
        resolver.table = table;
    }

    public static bool SetLanguage(string language)
    {
        if (!resolver.table.HasLanguage(language)
            && !string.Equals(language, StringResolver.PSEUDO_LANGUAGE, StringComparison.OrdinalIgnoreCase))
        {
            Report($"unknown language '{language}'; staying in '{resolver.language}'");
            return false;
        }

        resolver.language = language;
        RefreshAll();
        return true;
    }

    public static string Resolve(string key, IReadOnlyList<LocArg> args) => resolver.Resolve(key, args);

    // Steam's names for its languages (SteamApps.GameLanguage), as language codes.
    private static readonly Dictionary<string, string> SteamLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["english"] = "en", ["brazilian"] = "pt-BR", ["portuguese"] = "pt-PT", ["spanish"] = "es-ES",
        ["latam"] = "es-419", ["french"] = "fr", ["german"] = "de", ["italian"] = "it", ["japanese"] = "ja",
        ["koreana"] = "ko", ["schinese"] = "zh-CN", ["tchinese"] = "zh-TW", ["russian"] = "ru",
        ["polish"] = "pl", ["turkish"] = "tr", ["dutch"] = "nl", ["swedish"] = "sv", ["danish"] = "da",
        ["norwegian"] = "no", ["finnish"] = "fi", ["czech"] = "cs", ["hungarian"] = "hu",
        ["romanian"] = "ro", ["ukrainian"] = "uk", ["greek"] = "el", ["thai"] = "th",
        ["vietnamese"] = "vi", ["indonesian"] = "id", ["arabic"] = "ar", ["bulgarian"] = "bg",
    };

    /// <summary>
    /// The loaded language that best fits a hint: a language code (<c>pt-BR</c>), or one of
    /// Steam's language names (<c>brazilian</c>). With no hint, or none that fits, the
    /// computer's own language is tried, and then English.
    /// </summary>
    public static string BestLanguage(string hint)
    {
        var languages = resolver.table.Languages;
        string Match(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            if (SteamLanguages.TryGetValue(code, out var mapped)) code = mapped;
            code = code.Replace('_', '-');
            foreach (var l in languages)
                if (string.Equals(l, code, StringComparison.OrdinalIgnoreCase)) return l;
            // The same language from somewhere else: pt-PT is closer to pt-BR than to English.
            var primary = code.Split('-')[0];
            foreach (var l in languages)
                if (string.Equals(l.Split('-')[0], primary, StringComparison.OrdinalIgnoreCase)) return l;
            return "";
        }

        var best = Match(hint);
        if (best.Length == 0) best = Match(System.Globalization.CultureInfo.CurrentUICulture.Name);
        if (best.Length == 0) best = Match(StringTable.DEFAULT_LANGUAGE);
        if (best.Length == 0 && languages.Count > 0) best = languages[0];
        return best.Length == 0 ? StringTable.DEFAULT_LANGUAGE : best;
    }

    /// <summary>Turns `params object[]` from a command (name, value, name, value...) into args.</summary>
    public static List<LocArg> ArgsFromPairs(string key, object[] pairs)
    {
        var args = new List<LocArg>(pairs == null ? 0 : pairs.Length / 2);
        if (pairs == null) return args;
        if (pairs.Length % 2 != 0) Report($"'{key}' was given {pairs.Length} arg values; they come in name, value pairs");

        for (var i = 0; i + 1 < pairs.Length; i += 2)
        {
            if (pairs[i] is string name) args.Add(new LocArg(name, pairs[i + 1]));
            else Report($"'{key}': arg {i / 2 + 1} has no name (got {pairs[i]} where a name was expected)");
        }
        return args;
    }

    /// <summary>Makes a text a loc text with this key and these args (replacing any it had).</summary>
    public static void Bind(int textIndex, string key, List<LocArg> args)
    {
        if (!bindings.TryGetValue(textIndex, out var binding))
            bindings[textIndex] = binding = new LocBinding();
        binding.key = key ?? "";
        binding.args.Clear();
        binding.args.AddRange(args);
        Refresh(textIndex, binding);
    }

    /// <summary>Back to a plain text. Its string stays what it was.</summary>
    public static void Unbind(int textIndex) => bindings.Remove(textIndex);

    public static bool IsBound(int textIndex) => bindings.ContainsKey(textIndex);

    /// <summary>Changes one arg of a loc text. Does nothing (and says so) for a plain text.</summary>
    public static void SetArg(int textIndex, string name, object value)
    {
        if (!bindings.TryGetValue(textIndex, out var binding))
        {
            Report($"`set text arg` on text {TextSystem.textSprites[textIndex].sprite.id}, which is not a loc text; use `text loc` or `set text loc` first");
            return;
        }

        foreach (var a in binding.args)
            if (string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase) && Equals(a.value, value))
                return; // nothing changed, so nothing to resolve: safe to call every frame

        binding.SetArg(name, value);
        Refresh(textIndex, binding);
    }

    /// <summary>
    /// A Debug check for plain `set text`: a literal that is a key in the table was almost
    /// certainly meant for `set text loc`.
    /// </summary>
    public static void CheckPlainText(int textId, string text)
    {
        if (!IsDevBuild || string.IsNullOrEmpty(text) || text.Length > 80 || text.IndexOf('.') < 0) return;
        if (resolver.table.HasKey(text))
            Report($"text {textId} was given \"{text}\" with `set text`, which draws it as it is; to look it up, use `set text loc`");
    }

    private static void Refresh(int textIndex, LocBinding binding)
    {
        ref var text = ref TextSystem.textSprites[textIndex];
        text.text = resolver.Resolve(binding.key, binding.args);

        // A size worked out for the old string is wrong for the new one, so the last `size text...`
        // is done again.
        if (text.sizeMode != TextSizeMode.None) ReportFit(textIndex, TextSizing.Apply(textIndex));
    }

    /// <summary>
    /// Debug: says when a loc text sized with `size text max` had to shrink well below the height it
    /// was given, which means its string is too long for its spot in this language.
    /// </summary>
    public static void ReportFit(int textIndex, float fit)
    {
        if (!IsDevBuild || fit >= .8f || !bindings.TryGetValue(textIndex, out var binding)) return;
        ref var text = ref TextSystem.textSprites[textIndex];
        Report($"'{binding.key}' ({resolver.language}) shrinks to {fit:P0} of its height to fit in {text.sizeX:0} by {text.sizeY:0}; it may be too long for its spot");
    }

    public static void RefreshAll()
    {
        foreach (var kv in bindings) Refresh(kv.Key, kv.Value);
    }

    /// <summary>Applies a change to the CSVs on disk. Called once a frame, on the game's thread.</summary>
    public static void Poll()
    {
        if (Interlocked.Exchange(ref _changedOnDisk, 0) == 0 || loadedDirectory == null) return;
        Console.WriteLine($"[fade] strings: reloading {loadedFolder}");
        _reported.Clear();
        ReadTable(loadedDirectory);
        RefreshAll();
    }

    private static void Watch(string directory)
    {
        StopWatching();
        if (!IsDevBuild) return; // only a Debug build edits its strings

        try
        {
            _watcher = new FileSystemWatcher(directory, "*.csv")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            FileSystemEventHandler changed = (_, _) =>
            {
                // A save is often several events; wait for them to settle.
                _debounce?.Dispose();
                _debounce = new Timer(_ => Interlocked.Exchange(ref _changedOnDisk, 1), null, 200, Timeout.Infinite);
            };
            _watcher.Changed += changed;
            _watcher.Created += changed;
            _watcher.Deleted += changed;
            _watcher.Renamed += (s, e) => changed(s, e);
        }
        catch (Exception ex)
        {
            Report($"could not watch {directory} for changes: {ex.Message}");
        }
    }

    private static void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        _debounce?.Dispose();
        _debounce = null;
    }

    private static void Report(string message)
    {
        // Each problem once: a missing key in a text that refreshes every frame would otherwise
        // say so every frame.
        if (_reported.Count > 1000 || !_reported.Add(message)) return;
        Console.Error.WriteLine($"[fade] strings: {message}");
    }
}
