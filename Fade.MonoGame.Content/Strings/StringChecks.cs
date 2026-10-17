using System.Xml.Linq;
using Fade.MonoGame.Localization;

namespace Fade.MonoGame.Content.Strings;

public enum CheckSeverity
{
    Error,
    Warning,
    Info,
}

public readonly record struct CheckResult(CheckSeverity severity, string code, string where, string message, string? key = null)
{
    /// <summary>The canonical form MSBuild and IDEs turn into a clickable error.</summary>
    public string ToMsBuild()
    {
        var kind = severity switch { CheckSeverity.Error => "error", CheckSeverity.Warning => "warning", _ => "message" };
        return severity == CheckSeverity.Info
            ? $"{where}: {code}: {message}"
            : $"{where}: {kind} {code}: {message}";
    }
}

public sealed class StringCheckOptions
{
    /// <summary>A Release build: anything a player would see as wrong is an error.</summary>
    public bool release;

    /// <summary>Off during a migration: dynamic and unknown keys are warnings instead of errors.</summary>
    public bool strict = true;

    /// <summary>The languages that ship. Null means every language column.</summary>
    public List<string>? shippedLanguages;

    /// <summary>The characters each font can draw, by font file.</summary>
    public List<(string font, HashSet<int> chars)> fonts = new();
}

/// <summary>
/// The localization build check: the rules that make a missing, mistyped or broken string a
/// build error, with the file and line to fix. See localization-plan.md, section 2.
/// </summary>
public static class StringChecks
{
    // Keys the engine itself reads (`language name$`), so they count as used.
    private static readonly string[] EngineKeys = { "lang.name" };

    public static List<CheckResult> Run(StringTable table, List<StringTableProblem> tableProblems,
        KeyAnalyzer.Result program, StringCheckOptions options, string stringsDir,
        List<string> newKeys)
    {
        var results = new List<CheckResult>();
        void Add(CheckSeverity s, string code, string where, string message) => results.Add(new CheckResult(s, code, where, message));
        // A problem with one key of the table, which `strings sync` shows in that row's status.
        void AddKey(CheckSeverity s, string code, string key, string message) => results.Add(new CheckResult(s, code, WhereKey(key), message, key));
        var strictSeverity = options.strict ? CheckSeverity.Error : CheckSeverity.Warning;

        foreach (var p in program.problems)
            Add(CheckSeverity.Error, p.code, $"{p.file}({p.line})", p.message);

        foreach (var p in tableProblems)
            Add(CheckSeverity.Error, p.code, $"{Path.Combine(stringsDir, p.file)}({p.line})", p.message);

        string WhereKey(string key)
        {
            var baseKey = key;
            if (!table.TryGetDefinition(key, out var file, out var line))
            {
                // A plural key is defined by its forms.
                table.TryGetDefinition(key + "#other", out file, out line);
                if (file == null) table.TryGetDefinition(key + "#one", out file, out line);
            }
            return file == null ? stringsDir : $"{Path.Combine(stringsDir, file)}({line})";
        }

        // --- the program's uses of keys ---

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var k in EngineKeys) used.Add(k);

        foreach (var use in program.uses)
        {
            if (use.kind == KeyExpressionKind.Concatenation)
            {
                Add(strictSeverity, "FLOC001", use.Where,
                    $"the key given to `{use.command}` is built with +, so the build cannot check it; write the whole key, or keep it in a variable with lockey$(\"...\")");
                continue;
            }
            if (use.kind != KeyExpressionKind.Literal)
            {
                if (use.constantOnly)
                    Add(strictSeverity, "FLOC001", use.Where, $"`{use.command}` needs the key written out as a string, so the build can see it");
                continue; // a key in a variable: it was written with lockey$, and checked there
            }

            var key = use.key;
            used.Add(key);

            if (!table.HasKey(key))
            {
                if (!options.release && options.strict)
                {
                    // Debug: it is a new key. It goes into the inbox for the next sync, and the
                    // game shows [key] until it has an English text.
                    if (!newKeys.Contains(key)) newKeys.Add(key);
                    Add(CheckSeverity.Warning, "FLOC020", use.Where, $"'{key}' is a new key; it was added to inbox.csv with no English text yet");
                }
                else
                {
                    Add(strictSeverity, "FLOC002", use.Where, $"'{key}' is not in the string tables");
                }
                continue;
            }

            if (use.hasArgs && use.argValueCount % 2 != 0)
                Add(CheckSeverity.Error, "FLOC006", use.Where, $"`{use.command}` was given {use.argValueCount} arg values; args come in name, value pairs");

            if (use.argNames == null) continue; // a name that is not a constant: checked when it runs

            var needed = ArgsOf(table, key);
            if (needed == null) continue; // no English text yet: FLOC020 says so
            foreach (var name in use.argNames)
            {
                if (!needed.Contains(name))
                    Add(CheckSeverity.Error, "FLOC003", use.Where, $"'{key}' has no {{${name}}} in its English text, so the '{name}' arg does nothing");
            }

            if (use.argsComplete)
            {
                var passed = new HashSet<string>(use.argNames, StringComparer.OrdinalIgnoreCase);
                foreach (var name in needed)
                    if (!passed.Contains(name))
                        Add(CheckSeverity.Error, "FLOC004", use.Where, $"'{key}' uses {{${name}}}, but `{use.command}` is not given a '{name}' arg");
                if (table.IsPlural(key) && !passed.Contains(StringResolver.COUNT_ARG))
                    Add(CheckSeverity.Error, "FLOC005", use.Where, $"'{key}' has plural forms, so it needs a '{StringResolver.COUNT_ARG}' arg to pick one");
            }
        }

        // --- the table itself ---

        var languages = options.shippedLanguages ?? table.Languages.ToList();
        var baseKeys = BaseKeys(table);

        foreach (var key in table.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            // A plural form row (moves#few) is only needed by the languages that have that form.
            var hash = key.IndexOf(StringTable.PLURAL_SEPARATOR);
            var form = hash > 0 ? key.Substring(hash + 1) : null;
            var baseKey = hash > 0 ? key.Substring(0, hash) : key;
            bool Needs(string language) => form == null || PluralRules.Uses(language, form);

            var hasEnglish = table.TryGet(StringTable.DEFAULT_LANGUAGE, key, out var en);
            if (!hasEnglish)
            {
                if (Needs(StringTable.DEFAULT_LANGUAGE))
                {
                    AddKey(options.release ? CheckSeverity.Error : CheckSeverity.Warning, "FLOC020", key, $"'{key}' has no English text");
                    continue;
                }
                // A form English does not have: its translations are compared with English's "other".
                if (!table.TryGet(StringTable.DEFAULT_LANGUAGE, baseKey + StringTable.PLURAL_SEPARATOR + PluralRules.OTHER, out en))
                    continue;
            }

            var enArgs = StringTemplate.ArgNames(en);
            var enIcons = StringTemplate.IconNames(en).OrderBy(x => x, StringComparer.Ordinal).ToList();

            foreach (var language in table.Languages)
            {
                if (!table.TryGet(language, key, out var text))
                {
                    if (languages.Contains(language, StringComparer.OrdinalIgnoreCase) && language != StringTable.DEFAULT_LANGUAGE && Needs(language))
                        AddKey(options.release ? CheckSeverity.Error : CheckSeverity.Warning, "FLOC021", key, $"'{key}' has no {language} text");
                    continue;
                }

                if (language != StringTable.DEFAULT_LANGUAGE)
                {
                    var args = StringTemplate.ArgNames(text);
                    var icons = StringTemplate.IconNames(text).OrderBy(x => x, StringComparer.Ordinal).ToList();
                    if (!args.SetEquals(enArgs))
                        AddKey(CheckSeverity.Error, "FLOC011", key,
                            $"the {language} text of '{key}' has args {Show(args)}, but the English has {Show(enArgs)}");
                    if (!icons.SequenceEqual(enIcons))
                        AddKey(CheckSeverity.Error, "FLOC011", key,
                            $"the {language} text of '{key}' has icons {Show(icons)}, but the English has {Show(enIcons)}");
                }

                foreach (var part in StringTemplate.Parse(text))
                {
                    if (part.kind == TemplatePartKind.Ref && !table.HasKey(part.value))
                        AddKey(CheckSeverity.Error, "FLOC012", key, $"the {language} text of '{key}' refers to '{part.value}', which is not in the string tables");
                }

                foreach (var (font, chars) in options.fonts)
                {
                    var missing = MissingChars(text, chars);
                    if (missing.Length > 0)
                        AddKey(CheckSeverity.Error, "FLOC013", key,
                            $"the {language} text of '{key}' has {missing}, which {Path.GetFileName(font)} does not have; add them to its CharacterRegions");
                }
            }
        }

        // Every form a shipped language uses has its row: Polish needs moves#few and moves#many as well as
        // moves#one. ("other" is only for fractions in a language that has "many", so it may be left out.)
        foreach (var baseKey in baseKeys.Where(table.IsPlural).OrderBy(k => k, StringComparer.Ordinal))
        {
            foreach (var language in languages)
            {
                var forms = PluralRules.Forms(language);
                foreach (var form in forms)
                {
                    if (form == PluralRules.OTHER && forms.Contains(PluralRules.MANY)) continue;
                    var formKey = baseKey + StringTable.PLURAL_SEPARATOR + form;
                    if (!table.HasKey(formKey))
                        AddKey(options.release ? CheckSeverity.Error : CheckSeverity.Warning, "FLOC021", baseKey,
                            $"'{baseKey}' has no '{form}' form, which {language} needs; add a row '{formKey}'");
                }
            }
        }

        foreach (var cycle in FindCycles(table, baseKeys))
            AddKey(CheckSeverity.Error, "FLOC012", cycle, $"'{cycle}' refers back to itself through {{@...}}");

        // --- unused keys ---

        // A key referenced by a used key is used too.
        var queue = new Queue<string>(used);
        while (queue.Count > 0)
        {
            var key = queue.Dequeue();
            foreach (var text in AllTexts(table, key))
                foreach (var part in StringTemplate.Parse(text))
                    if (part.kind == TemplatePartKind.Ref && used.Add(part.value)) queue.Enqueue(part.value);
        }
        foreach (var key in baseKeys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!used.Contains(key))
                AddKey(CheckSeverity.Info, "FLOC030", key, $"'{key}' is not used by the game");
        }

        return results;
    }

    private static string Show(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count == 0 ? "none" : string.Join(" ", list.Select(n => "{" + n + "}"));
    }

    /// <summary>
    /// The characters of a text the font would have to draw: its literal text, without the
    /// {$args} and {@refs} (checked as their own keys) or the {icons} (drawn as pictures).
    /// </summary>
    private static string MissingChars(string text, HashSet<int> chars)
    {
        var missing = new SortedSet<int>();
        foreach (var part in StringTemplate.Parse(text))
        {
            if (part.kind != TemplatePartKind.Literal) continue;
            var inIcon = false;
            foreach (var c in part.value)
            {
                if (c == '{') { inIcon = true; continue; }
                if (c == '}' && inIcon) { inIcon = false; continue; }
                if (inIcon || c == '\n' || c == '\r') continue; // line breaks are not glyphs
                if (!chars.Contains(c)) missing.Add(c);
            }
        }
        return string.Join(" ", missing.Select(c => $"'{(char)c}' (U+{c:X4})"));
    }

    /// <summary>The keys without their plural forms: game.moves#one and game.moves#other are game.moves.</summary>
    public static HashSet<string> BaseKeys(StringTable table)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in table.Keys)
        {
            var hash = key.IndexOf(StringTable.PLURAL_SEPARATOR);
            keys.Add(hash > 0 ? key.Substring(0, hash) : key);
        }
        return keys;
    }

    private static IEnumerable<string> AllTexts(StringTable table, string key)
    {
        var forms = table.IsPlural(key)
            ? table.Keys.Where(k => k.StartsWith(key + StringTable.PLURAL_SEPARATOR, StringComparison.Ordinal))
            : new[] { key };
        foreach (var form in forms)
            foreach (var language in table.Languages)
                if (table.TryGet(language, form, out var text)) yield return text;
    }

    /// <summary>The {$args} of a key's English text (all forms, for a plural key), or null if it has none yet.</summary>
    private static HashSet<string>? ArgsOf(StringTable table, string key)
    {
        HashSet<string>? names = null;
        var forms = table.IsPlural(key)
            ? table.Keys.Where(k => k.StartsWith(key + StringTable.PLURAL_SEPARATOR, StringComparison.Ordinal)).ToList()
            : new List<string> { key };
        foreach (var form in forms)
        {
            if (!table.TryGet(StringTable.DEFAULT_LANGUAGE, form, out var en)) continue;
            names ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            names.UnionWith(StringTemplate.ArgNames(en));
            // An arg used only by a referenced key still has to be passed: refs share the args.
            foreach (var part in StringTemplate.Parse(en))
                if (part.kind == TemplatePartKind.Ref)
                {
                    var inner = ArgsOfShallow(table, part.value);
                    if (inner != null) names.UnionWith(inner);
                }
        }
        return names;
    }

    private static HashSet<string>? ArgsOfShallow(StringTable table, string key)
    {
        if (table.TryGet(StringTable.DEFAULT_LANGUAGE, key, out var en)) return StringTemplate.ArgNames(en);
        return null;
    }

    private static List<string> FindCycles(StringTable table, HashSet<string> baseKeys)
    {
        var cycles = new List<string>();
        var state = new Dictionary<string, int>(); // 1 visiting, 2 done
        bool Visit(string key)
        {
            if (state.TryGetValue(key, out var s)) return s == 1;
            state[key] = 1;
            var found = false;
            foreach (var text in AllTexts(table, key))
                foreach (var part in StringTemplate.Parse(text))
                    if (part.kind == TemplatePartKind.Ref && table.HasKey(part.value) && Visit(part.value))
                        found = true;
            state[key] = 2;
            return found;
        }
        foreach (var key in baseKeys.OrderBy(k => k, StringComparer.Ordinal))
        {
            state.Clear();
            if (Visit(key)) cycles.Add(key);
        }
        return cycles;
    }

    /// <summary>The characters a .spritefont bakes: every CharacterRegion, from Start to End.</summary>
    public static HashSet<int> ReadFontCharacters(string spritefontPath)
    {
        var chars = new HashSet<int>();
        // PreserveWhitespace: the ASCII region starts at &#32;, a space, which is otherwise dropped.
        var doc = XDocument.Load(spritefontPath, LoadOptions.PreserveWhitespace);
        foreach (var region in doc.Descendants("CharacterRegion"))
        {
            var start = region.Element("Start")?.Value;
            var end = region.Element("End")?.Value;
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end)) continue;
            for (int c = start[0]; c <= end[0]; c++) chars.Add(c);
        }
        return chars;
    }
}
