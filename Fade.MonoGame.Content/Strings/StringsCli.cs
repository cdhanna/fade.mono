using System.Text.Json;
using Fade.MonoGame.Localization;

namespace Fade.MonoGame.Content.Strings;

/// <summary>
/// The <c>strings</c> verbs of the content CLI.
///
///   fadecontent strings check --strings &lt;dir&gt; --inputs &lt;file&gt; [--fonts &lt;dir&gt;] [--release] [--no-strict]
///
/// <c>check</c> is the localization build check. It parses the game's program, finds every key
/// it uses, checks them and the string tables, and prints each problem in MSBuild's canonical
/// form, so they show up as build errors with a file and line. It reads no network.
///
/// The inputs file has one item per line, written by the FadeStringsCheck target:
///   source|&lt;path to a .fbasic file&gt;          (in build order)
///   ref|&lt;path to a referenced dll&gt;
///   command|&lt;assembly name&gt;|&lt;command class&gt;   (the project's FadeCommand items)
///   probe|&lt;directory&gt;                         (where dependencies can be found)
///
/// In a Debug build, a key the tables do not have yet is appended to inbox.csv in the strings
/// folder (status "new", no English), so the next `strings sync` puts it in the sheet.
/// </summary>
public static class StringsCli
{
    public static int Run(string[] args)
    {
        // Problems quote the strings, accents and all.
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length > 0 && args[0] != "check")
            return SheetSync.Run(args);

        if (args.Length == 0 || args[0] != "check")
        {
            Console.Error.WriteLine("[E] usage: strings check --strings <dir> --inputs <file> [--fonts <dir>] [--release] [--no-strict]");
            return 2;
        }

        string? stringsDir = null, inputs = null, fontsDir = null;
        var options = new StringCheckOptions();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--strings" when i + 1 < args.Length: stringsDir = args[++i]; break;
                case "--inputs" when i + 1 < args.Length: inputs = args[++i]; break;
                case "--fonts" when i + 1 < args.Length: fontsDir = args[++i]; break;
                case "--release": options.release = true; break;
                case "--no-strict": options.strict = false; break;
                default:
                    Console.Error.WriteLine($"[E] unknown or incomplete argument: {args[i]}");
                    return 2;
            }
        }

        try
        {
            return Check(stringsDir!, inputs!, fontsDir, options);
        }
        catch (Exception ex)
        {
            // In the canonical form, so the build fails on it rather than passing over it.
            Console.WriteLine($"{stringsDir}: error FLOC000: the strings check crashed: {ex}");
            return 1;
        }
    }

    private static int Check(string stringsDir, string inputs, string? fontsDir, StringCheckOptions options)
    {
        stringsDir = Path.GetFullPath(stringsDir);
        if (!Directory.Exists(stringsDir))
        {
            Console.WriteLine($"{stringsDir}: error FLOC016: the strings folder does not exist");
            return 1;
        }

        var (sources, commands, probes) = ReadInputs(inputs);

        // The sheet config, when there is one, says which languages ship.
        var config = Path.Combine(stringsDir, "strings.sheet.json");
        if (File.Exists(config))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(config));
            if (doc.RootElement.TryGetProperty("languages", out var langs) && langs.ValueKind == JsonValueKind.Array)
                options.shippedLanguages = langs.EnumerateArray().Select(l => l.GetString() ?? "").Where(l => l.Length > 0).ToList();
        }

        if (fontsDir != null && Directory.Exists(fontsDir))
        {
            foreach (var font in Directory.GetFiles(fontsDir, "*.spritefont", SearchOption.AllDirectories))
                options.fonts.Add((font, StringChecks.ReadFontCharacters(font)));
        }

        var (table, tableProblems) = ReadTable(stringsDir);
        var program = KeyAnalyzer.Analyze(sources, commands, probes);

        var newKeys = new List<string>();
        var results = StringChecks.Run(table, tableProblems, program, options, stringsDir, newKeys);

        if (newKeys.Count > 0) AppendToInbox(stringsDir, table, newKeys, program);

        var errors = 0;
        var warnings = 0;
        var unused = 0;
        foreach (var r in results)
        {
            switch (r.severity)
            {
                case CheckSeverity.Error: errors++; Console.WriteLine(r.ToMsBuild()); break;
                case CheckSeverity.Warning: warnings++; Console.WriteLine(r.ToMsBuild()); break;
                default: unused++; break; // listed by `strings sync`, not on every build
            }
        }

        Console.WriteLine($"[fade] strings: {program.uses.Count} key uses in {sources.Count} files, "
                          + $"{table.KeyCount} keys in {Path.GetFileName(stringsDir)}: "
                          + $"{errors} errors, {warnings} warnings, {unused} unused keys");
        return errors > 0 ? 1 : 0;
    }

    /// <summary>The program's sources, command libraries and probe folders, from the inputs file the build writes.</summary>
    public static (List<string> sources, List<(string dll, string className)> commands, List<string> probes) ReadInputs(string inputs)
    {
        var sources = new List<string>();
        var refs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var commandItems = new List<(string assembly, string className)>();
        var probes = new List<string>();
        foreach (var raw in File.ReadAllLines(inputs))
        {
            var parts = raw.Split('|');
            switch (parts[0])
            {
                case "source" when parts.Length >= 2: sources.Add(Path.GetFullPath(parts[1])); break;
                case "ref" when parts.Length >= 2: refs[Path.GetFileNameWithoutExtension(parts[1])] = parts[1]; break;
                case "command" when parts.Length >= 3: commandItems.Add((parts[1], parts[2])); break;
                case "probe" when parts.Length >= 2: probes.Add(parts[1]); break;
            }
        }

        var commands = new List<(string dll, string className)>();
        foreach (var (assembly, className) in commandItems)
        {
            if (refs.TryGetValue(assembly, out var dll)) commands.Add((dll, className));
            else Console.WriteLine($"{inputs}: warning FLOC017: no reference found for the command library '{assembly}'");
        }
        return (sources, commands, probes);
    }

    public static (StringTable, List<StringTableProblem>) ReadTable(string stringsDir)
    {
        var table = new StringTable();
        var problems = new List<StringTableProblem>();
        foreach (var file in Directory.GetFiles(stringsDir, "*.csv").OrderBy(f => f, StringComparer.Ordinal))
            table.AddCsv(Path.GetFileName(file), File.ReadAllText(file), problems);
        return (table, problems);
    }

    private static void AppendToInbox(string stringsDir, StringTable table, List<string> keys, KeyAnalyzer.Result program)
    {
        var path = Path.Combine(stringsDir, "inbox.csv");
        var languages = table.Languages.Count > 0 ? table.Languages.ToList() : new List<string> { StringTable.DEFAULT_LANGUAGE };
        if (!languages.Contains(StringTable.DEFAULT_LANGUAGE)) languages.Insert(0, StringTable.DEFAULT_LANGUAGE);

        var rows = new List<IReadOnlyList<string>>();
        if (!File.Exists(path))
        {
            var header = new List<string> { "key", "status" };
            header.AddRange(languages);
            header.Add("notes");
            header.Add("where");
            rows.Add(header);
        }

        var root = Path.GetDirectoryName(stringsDir) ?? "";
        foreach (var key in keys)
        {
            var where = string.Join(" ", program.uses
                .Where(u => u.kind == KeyExpressionKind.Literal && u.key == key)
                .Select(u => $"{Path.GetFileName(u.file)}:{u.line}")
                .Distinct());
            var row = new List<string> { key, "new" };
            row.AddRange(languages.Select(_ => ""));
            row.Add("");
            row.Add(where);
            rows.Add(row);
        }

        File.AppendAllText(path, StringCsv.Write(rows));
    }
}
