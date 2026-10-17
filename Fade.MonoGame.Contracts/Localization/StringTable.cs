namespace Fade.MonoGame.Localization;

/// <summary>
/// Something wrong with a string table, found while reading it or checking it. The build check
/// turns these into errors with a FLOC code; the game logs them.
/// </summary>
public readonly struct StringTableProblem
{
    public readonly string code;
    public readonly string file;
    public readonly int line;
    public readonly string message;

    public StringTableProblem(string code, string file, int line, string message)
    {
        this.code = code;
        this.file = file;
        this.line = line;
        this.message = message;
    }

    public override string ToString() => $"{file}({line}): {code}: {message}";
}

/// <summary>
/// The localized strings of a game: for each language, key to text.
///
/// It is read from one CSV per sheet tab (menu.csv, quests.csv, ...). Each CSV has a header row;
/// the columns are found by name, so their order does not matter and unknown ones are ignored.
/// <c>key</c> is required. A column whose name is a language code (<c>en</c>, <c>pt-BR</c>) holds
/// that language's strings. The strings tool owns <c>status</c>, <c>notes</c>, <c>where</c> and
/// the <c>reviewed:*</c> / <c>src:*</c> columns, and the game ignores them.
///
/// A plural key is several rows, one per form: <c>game.moves#one</c>, <c>game.moves#other</c>.
/// </summary>
public sealed class StringTable
{
    public const string KEY_COLUMN = "key";
    public const string DEFAULT_LANGUAGE = "en";
    public const char PLURAL_SEPARATOR = '#';

    private static readonly HashSet<string> ToolColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "notes", "where"
    };

    // language -> key (with any #form) -> text. Empty cells are left out, so "missing" is
    // simply "not in the dictionary".
    private readonly Dictionary<string, Dictionary<string, string>> _strings = new(StringComparer.OrdinalIgnoreCase);

    // Every key in the table, with its #form if it has one, and where it was defined.
    private readonly Dictionary<string, (string file, int line)> _keys = new(StringComparer.Ordinal);

    // Base keys that have plural forms.
    private readonly HashSet<string> _pluralKeys = new(StringComparer.Ordinal);

    private readonly List<string> _languages = new();

    /// <summary>The language columns, in the order they first appeared.</summary>
    public IReadOnlyList<string> Languages => _languages;

    /// <summary>Every key, plural forms included (<c>game.moves#one</c>).</summary>
    public IEnumerable<string> Keys => _keys.Keys;

    public int KeyCount => _keys.Count;

    public static bool IsLanguageColumn(string header)
    {
        if (string.IsNullOrWhiteSpace(header)) return false;
        if (string.Equals(header, KEY_COLUMN, StringComparison.OrdinalIgnoreCase)) return false;
        if (ToolColumns.Contains(header)) return false;
        if (header.Contains(':')) return false; // reviewed:pt-BR, src:pt-BR
        return true;
    }

    /// <summary>
    /// Reads one CSV into the table. <paramref name="file"/> is only used in problem reports.
    /// A file can be added on top of others; a key defined twice is a problem (FLOC010) and
    /// the first definition wins.
    /// </summary>
    public void AddCsv(string file, string csv, List<StringTableProblem> problems)
    {
        var rows = StringCsv.Parse(csv);
        if (rows.Count == 0) return;

        var header = rows[0].fields;
        var keyColumn = -1;
        var languageColumns = new List<(int column, string language)>();
        for (var c = 0; c < header.Length; c++)
        {
            var name = header[c].Trim();
            if (string.Equals(name, KEY_COLUMN, StringComparison.OrdinalIgnoreCase)) keyColumn = c;
            else if (IsLanguageColumn(name)) languageColumns.Add((c, name));
        }

        if (keyColumn < 0)
        {
            problems.Add(new StringTableProblem("FLOC015", file, rows[0].line, "the header row has no 'key' column, so the file was skipped"));
            return;
        }

        foreach (var (_, language) in languageColumns)
        {
            if (!_languages.Exists(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase)))
                _languages.Add(language);
            if (!_strings.ContainsKey(language)) _strings[language] = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        for (var r = 1; r < rows.Count; r++)
        {
            var (line, fields) = rows[r];
            var key = keyColumn < fields.Length ? fields[keyColumn].Trim() : "";
            if (key.Length == 0) continue;

            if (_keys.TryGetValue(key, out var first))
            {
                problems.Add(new StringTableProblem("FLOC010", file, line, $"key '{key}' is already defined at {first.file}({first.line})"));
                continue;
            }
            _keys[key] = (file, line);

            var hash = key.IndexOf(PLURAL_SEPARATOR);
            if (hash > 0) _pluralKeys.Add(key.Substring(0, hash));

            foreach (var (column, language) in languageColumns)
            {
                // Sheets leaves trailing empty cells off a row, so a short row is a row whose
                // last languages are untranslated.
                var text = column < fields.Length ? fields[column] : "";
                if (text.Length > 0) _strings[language][key] = text;
            }
        }
    }

    public bool HasLanguage(string language) => _strings.ContainsKey(language);

    public bool HasKey(string key) => _keys.ContainsKey(key) || _pluralKeys.Contains(key);

    public bool IsPlural(string key) => _pluralKeys.Contains(key);

    public bool TryGetDefinition(string key, out string file, out int line)
    {
        var found = _keys.TryGetValue(key, out var at);
        file = at.file;
        line = at.line;
        return found;
    }

    /// <summary>The text of a key in one language, with no fallback. Plural forms are their own keys.</summary>
    public bool TryGet(string language, string key, out string text)
    {
        if (_strings.TryGetValue(language, out var map) && map.TryGetValue(key, out var found))
        {
            text = found;
            return true;
        }
        text = "";
        return false;
    }
}
