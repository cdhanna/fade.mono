using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fade.MonoGame.Localization;

namespace Fade.MonoGame.Content.Strings;

/// <summary>
/// The strings tool's sheet verbs. The code is the source of truth for which keys exist; the
/// Google Sheet is the source of truth for their text.
///
///   strings whoami --strings &lt;dir&gt;                 check the key; print the email to share the sheet with
///   strings init   --strings &lt;dir&gt; --sheet &lt;url&gt;    set a sheet up for the game, upload the local CSVs
///   strings pull   --strings &lt;dir&gt;                 the sheet into the snapshot CSVs, changing nothing there
///   strings sync   --strings &lt;dir&gt; [--inputs &lt;f&gt;]   new keys in, status/where/src out, then pull
///
/// Common options: --credentials &lt;key.json&gt;, --dry-run (print what would change, write nothing).
///
/// The tool owns the key, status, where and src:* columns, and never edits a person's text or
/// deletes a row. Columns are found by their header, so people can reorder them and add their own.
/// A spreadsheet is only touched after `init` has tagged it with developer metadata, so pointing
/// the tool at the wrong sheet cannot rearrange it.
/// </summary>
public static class SheetSync
{
    public const string TAG = "fade-localization";
    public const string TAG_VERSION = "1";
    public const string CONFIG_FILE = "strings.sheet.json";
    public const string INBOX_TAB = "inbox";
    public const string INBOX_FILE = "inbox.csv";

    // Status values, most urgent first.
    public static readonly string[] Statuses = { "error", "new", "stale", "missing", "draft", "unused", "ok" };

    private sealed class Options
    {
        public string verb = "";
        public string stringsDir = "";
        public string? sheet;
        public string? credentials;
        public string? inputs;
        public string? file;
        public string? language;
        public bool dryRun;
        public bool reformat;
    }

    private sealed class Tab
    {
        public string title = "";
        public int sheetId;
        public List<string> header = new();
        public List<string[]> rows = new(); // without the header

        public int Col(string name) => header.FindIndex(h => string.Equals(h.Trim(), name, StringComparison.OrdinalIgnoreCase));

        public string Cell(string[] row, string name)
        {
            var c = Col(name);
            return c >= 0 && c < row.Length ? row[c] : "";
        }
    }

    public static int Run(string[] args)
    {
        var o = new Options { verb = args[0] };
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--strings" when i + 1 < args.Length: o.stringsDir = Path.GetFullPath(args[++i]); break;
                case "--sheet" when i + 1 < args.Length: o.sheet = args[++i]; break;
                case "--credentials" when i + 1 < args.Length: o.credentials = args[++i]; break;
                case "--inputs" when i + 1 < args.Length: o.inputs = args[++i]; break;
                case "--file" when i + 1 < args.Length: o.file = args[++i]; break;
                case "--language" when i + 1 < args.Length: o.language = args[++i]; break;
                case "--dry-run": o.dryRun = true; break;
                case "--reformat": o.reformat = true; break;
                default:
                    Console.Error.WriteLine($"[E] unknown or incomplete argument: {args[i]}");
                    return 2;
            }
        }
        if (o.stringsDir.Length == 0 || !Directory.Exists(o.stringsDir))
        {
            Console.Error.WriteLine("[E] --strings <dir> must name the folder of string table CSVs");
            return 2;
        }

        var keyPath = SheetsClient.FindKey(o.credentials, o.stringsDir);
        if (keyPath == null)
        {
            Console.Error.WriteLine("[E] no Google credentials. Put a service-account key at .secrets/google-sheets-sa.json in the repo, set FADE_SHEETS_CREDENTIALS, or pass --credentials.");
            return 1;
        }

        try
        {
            using var client = new SheetsClient(keyPath);
            return o.verb switch
            {
                "whoami" => WhoAmI(client, o).GetAwaiter().GetResult(),
                "init" => Init(client, o).GetAwaiter().GetResult(),
                "pull" => Pull(client, o).GetAwaiter().GetResult(),
                "sync" => Sync(client, o).GetAwaiter().GetResult(),
                "import" => Import(client, o).GetAwaiter().GetResult(),
                _ => Usage(),
            };
        }
        catch (SheetsException ex)
        {
            Console.Error.WriteLine($"[E] Google Sheets: {ex.Message}");
            if (ex.status == 403 || ex.status == 404)
                Console.Error.WriteLine("    Is the sheet shared (as Editor) with the service account? Run `strings whoami` for its email.");
            return 1;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("[E] usage: strings whoami|init|pull|sync|check --strings <dir> [--sheet <url>] [--inputs <file>] [--credentials <key.json>] [--dry-run]");
        return 2;
    }

    // ---------------------------------------------------------------- whoami

    private static async Task<int> WhoAmI(SheetsClient client, Options o)
    {
        Console.WriteLine($"service account: {client.Email}");
        var config = LoadConfig(o.stringsDir);
        var id = o.sheet != null ? SheetsClient.ParseSheetId(o.sheet) : config["sheetId"]?.GetValue<string>();
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine("no sheet yet: share one with that email as Editor, then run `strings init --sheet <url>`");
            return 0;
        }
        var info = await client.GetInfoAsync(id);
        Console.WriteLine($"sheet: \"{info.title}\" ({id}), tabs: {string.Join(", ", info.tabs.Select(t => t.title))}");
        Console.WriteLine(info.metadata.ContainsKey(TAG) ? "set up for the strings tool" : "not set up yet: run `strings init`");
        return 0;
    }

    // ---------------------------------------------------------------- init

    private static async Task<int> Init(SheetsClient client, Options o)
    {
        var config = LoadConfig(o.stringsDir);
        var sheetArg = o.sheet ?? config["sheetId"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sheetArg))
        {
            Console.Error.WriteLine("[E] init needs --sheet <url>");
            return 2;
        }
        var id = SheetsClient.ParseSheetId(sheetArg);
        var languages = Languages(config, o.stringsDir);

        var info = await client.GetInfoAsync(id);
        var tagged = info.metadata.ContainsKey(TAG);
        Console.WriteLine($"sheet \"{info.title}\": {(tagged ? "already set up; making sure it is complete" : "setting it up")}");

        // A tab for every local CSV that has none. The CSVs were the tables until now.
        var localFiles = LocalCsvs(o.stringsDir).Where(f => !IsInbox(f)).ToList();
        var add = new JsonArray();
        var created = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in localFiles)
        {
            var title = Path.GetFileNameWithoutExtension(file);
            if (!info.tabs.Any(t => string.Equals(t.title, title, StringComparison.OrdinalIgnoreCase)))
            {
                add.Add(new JsonObject { ["addSheet"] = new JsonObject { ["properties"] = new JsonObject { ["title"] = title } } });
                Console.WriteLine($"  + tab '{title}'");
                created.Add(title);
            }
        }
        if (!o.dryRun && add.Count > 0)
        {
            await client.BatchUpdateAsync(id, add);
            info = await client.GetInfoAsync(id);
        }

        var tabs = await ReadTabs(client, id, info, onlyStringTabs: false);

        // Fill each empty tab from its CSV.
        var writes = new List<(string range, List<List<string>> values)>();
        foreach (var file in localFiles)
        {
            var title = Path.GetFileNameWithoutExtension(file);
            var tab = tabs.FirstOrDefault(t => string.Equals(t.title, title, StringComparison.OrdinalIgnoreCase));
            if (tab == null && !o.dryRun) continue;
            if (tab != null && (tab.header.Count > 0 || tab.rows.Count > 0)) continue;
            var rows = StringCsv.Parse(File.ReadAllText(file)).Select(r => r.fields.ToList()).ToList();
            Console.WriteLine($"  uploading {Path.GetFileName(file)} ({Math.Max(0, rows.Count - 1)} rows)");
            if (tab != null) writes.Add((SheetsClient.Range(tab.title, "A1"), rows));
            else if (rows.Count > 0)
            {
                // A dry run made no tab, so pretend it did, to show the rest of what would happen.
                tabs.Add(new Tab { title = title, header = rows[0], rows = rows.Skip(1).Select(r => r.ToArray()).ToList() });
            }
        }
        if (!o.dryRun && writes.Count > 0)
        {
            await client.WriteRangesAsync(id, writes);
            tabs = await ReadTabs(client, id, info, onlyStringTabs: false);
        }

        // Every string tab gets every column the tool owns.
        var stringTabs = tabs.Where(t => t.Col(StringTable.KEY_COLUMN) >= 0).ToList();
        if (stringTabs.Count == 0)
        {
            Console.Error.WriteLine("[E] no tab has a 'key' column, and there were no local CSVs to upload. Put a 'key' header in A1 of a tab, or add a CSV to the strings folder.");
            return 1;
        }
        await EnsureColumns(client, id, stringTabs, languages, o.dryRun);

        var toFormat = !tagged || o.reformat ? stringTabs : stringTabs.Where(t => created.Contains(t.title)).ToList();
        if (toFormat.Count > 0)
        {
            var requests = new JsonArray();
            foreach (var tab in toFormat) AddFormatting(requests, tab);
            if (!tagged)
                requests.Add(new JsonObject
                {
                    ["createDeveloperMetadata"] = new JsonObject
                    {
                        ["developerMetadata"] = new JsonObject
                        {
                            ["metadataKey"] = TAG,
                            ["metadataValue"] = TAG_VERSION,
                            ["location"] = new JsonObject { ["spreadsheet"] = true },
                            ["visibility"] = "DOCUMENT",
                        }
                    }
                });
            Console.WriteLine($"  formatting {toFormat.Count} tab(s): frozen header, status dropdown and colors, review checkboxes, hidden src columns");
            if (!o.dryRun) await client.BatchUpdateAsync(id, requests);
        }

        config["sheetId"] = id;
        if (config["languages"] == null) config["languages"] = new JsonArray(languages.Select(l => (JsonNode)JsonValue.Create(l)!).ToArray());
        if (!o.dryRun) SaveConfig(o.stringsDir, config);

        if (o.dryRun)
        {
            Console.WriteLine("dry run: nothing was written");
            return 0;
        }
        Console.WriteLine($"set up. {CONFIG_FILE} now names the sheet. Next: `strings sync`.");
        return await Pull(client, o);
    }

    private static async Task EnsureColumns(SheetsClient client, string id, List<Tab> tabs, List<string> languages, bool dryRun)
    {
        var writes = new List<(string range, List<List<string>> values)>();
        var widen = new JsonArray();
        foreach (var tab in tabs)
        {
            var missing = OwnedColumns(languages).Where(c => tab.Col(c) < 0).ToList();
            if (missing.Count == 0) continue;
            Console.WriteLine($"  tab '{tab.title}': adding columns {string.Join(", ", missing)}");
            var start = tab.header.Count;
            writes.Add((SheetsClient.Range(tab.title, ColumnLetter(start) + "1"), new List<List<string>> { missing }));
            widen.Add(new JsonObject
            {
                ["appendDimension"] = new JsonObject { ["sheetId"] = tab.sheetId, ["dimension"] = "COLUMNS", ["length"] = missing.Count }
            });
            tab.header.AddRange(missing);
        }
        if (dryRun) return;
        // Make room first: a tab made by hand may be only as wide as its data.
        if (widen.Count > 0) await client.BatchUpdateAsync(id, widen);
        await client.WriteRangesAsync(id, writes);
    }

    private static List<string> OwnedColumns(List<string> languages)
    {
        var columns = new List<string> { StringTable.KEY_COLUMN, "status" };
        columns.AddRange(languages);
        columns.Add("notes");
        columns.Add("where");
        foreach (var l in languages.Where(l => l != StringTable.DEFAULT_LANGUAGE)) columns.Add("reviewed:" + l);
        foreach (var l in languages.Where(l => l != StringTable.DEFAULT_LANGUAGE)) columns.Add("src:" + l);
        return columns;
    }

    private static void AddFormatting(JsonArray requests, Tab tab)
    {
        JsonObject Range(int startRow, int startCol, int endCol) => new()
        {
            ["sheetId"] = tab.sheetId,
            ["startRowIndex"] = startRow,
            ["startColumnIndex"] = startCol,
            ["endColumnIndex"] = endCol,
        };
        JsonObject Color(double r, double g, double b) => new() { ["red"] = r, ["green"] = g, ["blue"] = b };

        requests.Add(new JsonObject
        {
            ["updateSheetProperties"] = new JsonObject
            {
                ["properties"] = new JsonObject { ["sheetId"] = tab.sheetId, ["gridProperties"] = new JsonObject { ["frozenRowCount"] = 1 } },
                ["fields"] = "gridProperties.frozenRowCount",
            }
        });
        var header = Range(0, 0, tab.header.Count);
        header["endRowIndex"] = 1;
        requests.Add(new JsonObject
        {
            ["repeatCell"] = new JsonObject
            {
                ["range"] = header,
                ["cell"] = new JsonObject { ["userEnteredFormat"] = new JsonObject { ["textFormat"] = new JsonObject { ["bold"] = true } } },
                ["fields"] = "userEnteredFormat.textFormat.bold",
            }
        });

        var status = tab.Col("status");
        if (status >= 0)
        {
            requests.Add(new JsonObject
            {
                ["setDataValidation"] = new JsonObject
                {
                    ["range"] = Range(1, status, status + 1),
                    ["rule"] = new JsonObject
                    {
                        ["condition"] = new JsonObject
                        {
                            ["type"] = "ONE_OF_LIST",
                            ["values"] = new JsonArray(Statuses.Select(s => (JsonNode)new JsonObject { ["userEnteredValue"] = s }).ToArray()),
                        },
                        ["showCustomUi"] = true,
                        ["strict"] = false,
                    }
                }
            });

            var letter = ColumnLetter(status);
            var colors = new (string status, JsonObject color)[]
            {
                ("error", Color(0.96, 0.78, 0.78)),
                ("missing", Color(0.98, 0.87, 0.87)),
                ("new", Color(1.0, 0.95, 0.70)),
                ("stale", Color(1.0, 0.86, 0.70)),
                ("draft", Color(0.85, 0.92, 1.0)),
                ("unused", Color(0.90, 0.90, 0.90)),
            };
            foreach (var (s, color) in colors)
            {
                requests.Add(new JsonObject
                {
                    ["addConditionalFormatRule"] = new JsonObject
                    {
                        ["rule"] = new JsonObject
                        {
                            ["ranges"] = new JsonArray(Range(1, 0, tab.header.Count)),
                            ["booleanRule"] = new JsonObject
                            {
                                ["condition"] = new JsonObject
                                {
                                    ["type"] = "CUSTOM_FORMULA",
                                    ["values"] = new JsonArray(new JsonObject { ["userEnteredValue"] = $"=${letter}2=\"{s}\"" }),
                                },
                                ["format"] = new JsonObject { ["backgroundColor"] = color },
                            }
                        },
                        ["index"] = 0,
                    }
                });
            }
        }

        for (var c = 0; c < tab.header.Count; c++)
        {
            var name = tab.header[c];
            if (name.StartsWith("src:", StringComparison.OrdinalIgnoreCase))
                requests.Add(new JsonObject
                {
                    ["updateDimensionProperties"] = new JsonObject
                    {
                        ["range"] = new JsonObject { ["sheetId"] = tab.sheetId, ["dimension"] = "COLUMNS", ["startIndex"] = c, ["endIndex"] = c + 1 },
                        ["properties"] = new JsonObject { ["hiddenByUser"] = true },
                        ["fields"] = "hiddenByUser",
                    }
                });
        }

        AddReviewCheckboxes(requests, tab);
    }

    /// <summary>
    /// Checkboxes in the reviewed:* columns, on the rows that have a key and no others. A
    /// checkbox cell reads FALSE even when nobody touched it, so empty rows with one would look
    /// like rows; this clears them from below the last key, and so tidies up as rows are added.
    /// </summary>
    private static void AddReviewCheckboxes(JsonArray requests, Tab tab)
    {
        var keyed = LastKeyedRow(tab) + 1; // rows after the header that have a key
        for (var c = 0; c < tab.header.Count; c++)
        {
            if (!tab.header[c].StartsWith("reviewed:", StringComparison.OrdinalIgnoreCase)) continue;
            if (keyed > 0)
                requests.Add(new JsonObject
                {
                    ["setDataValidation"] = new JsonObject
                    {
                        ["range"] = new JsonObject { ["sheetId"] = tab.sheetId, ["startRowIndex"] = 1, ["endRowIndex"] = keyed + 1, ["startColumnIndex"] = c, ["endColumnIndex"] = c + 1 },
                        ["rule"] = new JsonObject { ["condition"] = new JsonObject { ["type"] = "BOOLEAN" } },
                    }
                });
            // No rule clears the validation; the cells' values go with it in a separate write.
            requests.Add(new JsonObject
            {
                ["setDataValidation"] = new JsonObject
                {
                    ["range"] = new JsonObject { ["sheetId"] = tab.sheetId, ["startRowIndex"] = keyed + 1, ["startColumnIndex"] = c, ["endColumnIndex"] = c + 1 },
                }
            });
            requests.Add(new JsonObject
            {
                ["updateCells"] = new JsonObject
                {
                    ["range"] = new JsonObject { ["sheetId"] = tab.sheetId, ["startRowIndex"] = keyed + 1, ["startColumnIndex"] = c, ["endColumnIndex"] = c + 1 },
                    ["fields"] = "userEnteredValue",
                }
            });
        }
    }

    /// <summary>The index (into tab.rows) of the last row with a key, or -1.</summary>
    private static int LastKeyedRow(Tab tab)
    {
        var keyCol = tab.Col(StringTable.KEY_COLUMN);
        for (var r = tab.rows.Count - 1; r >= 0; r--)
            if (keyCol >= 0 && keyCol < tab.rows[r].Length && tab.rows[r][keyCol].Trim().Length > 0) return r;
        return -1;
    }

    // ---------------------------------------------------------------- import

    /// <summary>
    /// Fills in one language's cells from a CSV of <c>key,text</c> (a batch of translations, say).
    /// Only empty cells are written: a translation somebody already put in the sheet is never
    /// replaced. A plural form the sheet has no row for yet (<c>moves#few</c>, for Polish) gets a new
    /// row in the tab of its key. Keys the sheet does not have at all are listed and left out.
    /// The language needs its column first: add it to <c>languages</c> in strings.sheet.json and run
    /// `strings init`.
    /// </summary>
    private static async Task<int> Import(SheetsClient client, Options o)
    {
        if (string.IsNullOrEmpty(o.file) || string.IsNullOrEmpty(o.language) || !File.Exists(o.file))
        {
            Console.Error.WriteLine("[E] import needs --file <key,text csv> and --language <code>");
            return 2;
        }
        var (id, info) = await OpenTagged(client, o);
        if (id == null) return 1;
        var tabs = await ReadTabs(client, id, info!, onlyStringTabs: true);

        var missingColumn = tabs.Where(t => t.Col(o.language) < 0).Select(t => t.title).ToList();
        if (missingColumn.Count > 0)
        {
            Console.Error.WriteLine($"[E] no '{o.language}' column in tab(s) {string.Join(", ", missingColumn)}: add it to languages in {CONFIG_FILE} and run `strings init`");
            return 1;
        }

        // Where each key is: its tab and its row (0-based, after the header).
        var where = new Dictionary<string, (Tab tab, int row)>(StringComparer.Ordinal);
        foreach (var tab in tabs)
        {
            var keyCol = tab.Col(StringTable.KEY_COLUMN);
            for (var r = 0; r < tab.rows.Count; r++)
            {
                var k = keyCol < tab.rows[r].Length ? tab.rows[r][keyCol].Trim() : "";
                if (k.Length > 0 && !where.ContainsKey(k)) where[k] = (tab, r);
            }
        }

        var input = StringCsv.Parse(File.ReadAllText(o.file));
        if (input.Count < 2) { Console.WriteLine("nothing to import"); return 0; }
        var head = input[0].fields.Select(h => h.Trim()).ToList();
        var keyIndex = head.FindIndex(h => string.Equals(h, StringTable.KEY_COLUMN, StringComparison.OrdinalIgnoreCase));
        var textIndex = head.FindIndex(h => string.Equals(h, "text", StringComparison.OrdinalIgnoreCase) || string.Equals(h, o.language, StringComparison.OrdinalIgnoreCase));
        if (keyIndex < 0 || textIndex < 0)
        {
            Console.Error.WriteLine($"[E] {o.file} needs a 'key' column and a 'text' (or '{o.language}') column");
            return 2;
        }

        var writes = new List<(string range, List<List<string>> values)>();
        var appends = new Dictionary<string, List<List<string>>>();
        int written = 0, kept = 0, added = 0;
        var unknown = new List<string>();
        foreach (var (_, fields) in input.Skip(1))
        {
            var key = keyIndex < fields.Length ? fields[keyIndex].Trim() : "";
            var text = textIndex < fields.Length ? fields[textIndex] : "";
            if (key.Length == 0 || text.Length == 0) continue;

            if (where.TryGetValue(key, out var at))
            {
                if (at.tab.Cell(at.tab.rows[at.row], o.language).Length > 0) { kept++; continue; }
                var letter = ColumnLetter(at.tab.Col(o.language));
                writes.Add((SheetsClient.Range(at.tab.title, $"{letter}{at.row + 2}"), new() { new() { text } }));
                written++;
                continue;
            }

            // A plural form with no row yet goes next to the forms of the same key.
            var hash = key.IndexOf(StringTable.PLURAL_SEPARATOR);
            var baseKey = hash > 0 ? key.Substring(0, hash) : "";
            var sibling = baseKey.Length == 0 ? null : where.Keys.FirstOrDefault(k => k.StartsWith(baseKey + StringTable.PLURAL_SEPARATOR, StringComparison.Ordinal));
            if (sibling == null) { unknown.Add(key); continue; }

            var home = where[sibling].tab;
            var row = home.header.Select(_ => "").ToList();
            row[home.Col(StringTable.KEY_COLUMN)] = key;
            row[home.Col(o.language)] = text;
            if (!appends.TryGetValue(home.title, out var list)) appends[home.title] = list = new();
            list.Add(row);
            added++;
        }

        Console.WriteLine($"{o.language}: {written} cell(s) to fill, {added} new plural row(s), {kept} kept as they were (already translated)");
        if (unknown.Count > 0) Console.WriteLine($"  not in the sheet, left out: {string.Join(", ", unknown)}");
        if (o.dryRun) { Console.WriteLine("dry run: nothing was written"); return 0; }

        // Many small ranges: send them in batches so one request stays a sensible size.
        for (var i = 0; i < writes.Count; i += 400)
            await client.WriteRangesAsync(id, writes.Skip(i).Take(400).ToList());
        foreach (var (tab, rows) in appends) await client.AppendRowsAsync(id, tab, rows);
        Console.WriteLine("imported. Run `strings sync` to update the statuses and the snapshot CSVs.");
        return 0;
    }

    // ---------------------------------------------------------------- pull

    private static async Task<int> Pull(SheetsClient client, Options o)
    {
        var (id, info) = await OpenTagged(client, o);
        if (id == null) return 1;
        var tabs = await ReadTabs(client, id, info!, onlyStringTabs: true);
        WriteSnapshots(o.stringsDir, tabs, o.dryRun);
        return 0;
    }

    private static void WriteSnapshots(string stringsDir, List<Tab> tabs, bool dryRun)
    {
        foreach (var tab in tabs)
        {
            var path = Path.Combine(stringsDir, tab.title + ".csv");
            var rows = new List<IReadOnlyList<string>> { tab.header };
            var keyCol = tab.Col(StringTable.KEY_COLUMN);
            rows.AddRange(tab.rows.Where(r => keyCol < r.Length && r[keyCol].Trim().Length > 0).Select(r => (IReadOnlyList<string>)r.Take(tab.header.Count).ToList()));
            var text = StringCsv.Write(rows);
            var old = File.Exists(path) ? File.ReadAllText(path) : null;
            if (old == text) continue;
            Console.WriteLine($"  {(dryRun ? "would write" : old == null ? "new" : "updated")} {Path.GetFileName(path)} ({rows.Count - 1} rows)");
            if (!dryRun) File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        // A CSV with no tab would keep strings the sheet no longer has.
        foreach (var file in LocalCsvs(stringsDir))
        {
            var title = Path.GetFileNameWithoutExtension(file);
            if (!IsInbox(file) && !tabs.Any(t => string.Equals(t.title, title, StringComparison.OrdinalIgnoreCase)))
                Console.WriteLine($"  warning: {Path.GetFileName(file)} has no tab in the sheet; its strings still load. Delete it if the tab was removed on purpose.");
        }
    }

    // ---------------------------------------------------------------- sync

    private static async Task<int> Sync(SheetsClient client, Options o)
    {
        var (id, info) = await OpenTagged(client, o);
        if (id == null) return 1;
        var config = LoadConfig(o.stringsDir);
        var languages = Languages(config, o.stringsDir);

        // The previous state, to tell which translations a person changed since the last sync.
        var previous = ReadLocalCells(o.stringsDir);

        KeyAnalyzer.Result? program = null;
        if (o.inputs != null && File.Exists(o.inputs))
        {
            var (sources, commands, probes) = StringsCli.ReadInputs(o.inputs);
            program = KeyAnalyzer.Analyze(sources, commands, probes);
            Console.WriteLine($"program: {program.uses.Count} key uses in {sources.Count} files");
        }
        else Console.WriteLine("no --inputs, so new and unused keys are not found (build the game first, or pass the fade-strings.inputs it wrote)");

        var tabs = await ReadTabs(client, id!, info!, onlyStringTabs: true);
        var sheetKeys = new HashSet<string>(tabs.SelectMany(t => t.rows.Select(r => t.Cell(r, StringTable.KEY_COLUMN).Trim())).Where(k => k.Length > 0), StringComparer.Ordinal);
        bool InSheet(string key) => sheetKeys.Contains(key) || sheetKeys.Contains(key + "#other") || sheetKeys.Contains(key + "#one");

        // --- new keys: in the code, or waiting in inbox.csv, but not in the sheet ---
        var wheres = program == null ? new Dictionary<string, string>() : program.uses
            .Where(u => u.kind == KeyExpressionKind.Literal)
            .GroupBy(u => u.key)
            .ToDictionary(g => g.Key, g => string.Join(" ", g.Select(u => $"{Path.GetFileName(u.file)}:{u.line}").Distinct()));

        var newRows = new List<(string key, string en, Dictionary<string, string> extra)>();
        var inboxPath = Path.Combine(o.stringsDir, INBOX_FILE);
        if (File.Exists(inboxPath))
        {
            var inboxRows = StringCsv.Parse(File.ReadAllText(inboxPath));
            if (inboxRows.Count > 1)
            {
                var head = inboxRows[0].fields.ToList();
                foreach (var (_, fields) in inboxRows.Skip(1))
                {
                    string Get(string name) { var c = head.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase)); return c >= 0 && c < fields.Length ? fields[c] : ""; }
                    var key = Get(StringTable.KEY_COLUMN).Trim();
                    if (key.Length == 0 || InSheet(key) || newRows.Any(r => r.key == key)) continue;
                    var extra = head.Where(h => StringTable.IsLanguageColumn(h) || h == "notes").ToDictionary(h => h, Get);
                    newRows.Add((key, Get(StringTable.DEFAULT_LANGUAGE), extra));
                }
            }
        }
        foreach (var key in wheres.Keys)
            if (!InSheet(key) && !newRows.Any(r => r.key == key)) newRows.Add((key, "", new Dictionary<string, string>()));

        if (newRows.Count > 0)
        {
            var byTab = newRows.GroupBy(r => TabFor(r.key, tabs)).ToList();
            if (byTab.Any(g => g.Key == INBOX_TAB) && !tabs.Any(t => t.title == INBOX_TAB))
            {
                Console.WriteLine($"  + tab '{INBOX_TAB}', for keys with no tab of their own");
                if (!o.dryRun)
                {
                    await client.BatchUpdateAsync(id!, new JsonArray(new JsonObject { ["addSheet"] = new JsonObject { ["properties"] = new JsonObject { ["title"] = INBOX_TAB } } }));
                    await client.WriteRangesAsync(id!, new() { (SheetsClient.Range(INBOX_TAB, "A1"), new() { OwnedColumns(languages) }) });
                    info = await client.GetInfoAsync(id!);
                    tabs = await ReadTabs(client, id!, info, onlyStringTabs: true);
                    var inbox = tabs.First(t => t.title == INBOX_TAB);
                    var formatting = new JsonArray();
                    AddFormatting(formatting, inbox);
                    await client.BatchUpdateAsync(id!, formatting);
                }
            }

            foreach (var group in byTab)
            {
                var tab = tabs.FirstOrDefault(t => t.title == group.Key);
                var header = tab?.header ?? OwnedColumns(languages);
                var rows = new List<List<string>>();
                foreach (var (key, en, extra) in group)
                {
                    var row = header.Select(_ => "").ToList();
                    void Set(string name, string value) { var c = header.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase)); if (c >= 0) row[c] = value; }
                    foreach (var kv in extra) Set(kv.Key, kv.Value);
                    Set(StringTable.KEY_COLUMN, key);
                    Set("status", "new");
                    Set(StringTable.DEFAULT_LANGUAGE, en);
                    Set("where", wheres.TryGetValue(key, out var w) ? w : "");
                    rows.Add(row);
                    Console.WriteLine($"  + '{key}' in tab '{group.Key}'");
                }
                if (!o.dryRun) await client.AppendRowsAsync(id!, group.Key, rows);
            }
            if (!o.dryRun) tabs = await ReadTabs(client, id!, info!, onlyStringTabs: true);
        }

        // --- the checks, for the error status ---
        var table = new StringTable();
        var tableProblems = new List<StringTableProblem>();
        foreach (var tab in tabs)
        {
            var rows = new List<IReadOnlyList<string>> { tab.header };
            rows.AddRange(tab.rows.Select(r => (IReadOnlyList<string>)r));
            table.AddCsv(tab.title + ".csv", StringCsv.Write(rows), tableProblems);
        }
        var options = new StringCheckOptions { shippedLanguages = languages };
        var assets = FindAssetsDir(o.stringsDir);
        if (assets != null)
            foreach (var font in Directory.GetFiles(assets, "*.spritefont", SearchOption.AllDirectories))
                options.fonts.Add((font, StringChecks.ReadFontCharacters(font)));
        var results = StringChecks.Run(table, tableProblems, program ?? new KeyAnalyzer.Result(), options, o.stringsDir, new List<string>());
        var errorsByKey = results
            .Where(r => r.key != null && r.severity == CheckSeverity.Error && r.code.CompareTo("FLOC010") >= 0 && r.code.CompareTo("FLOC014") < 0)
            .GroupBy(r => r.key!)
            .ToDictionary(g => g.Key, g => g.Select(r => r.message).ToList());
        var unused = program == null ? new HashSet<string>() : new HashSet<string>(results.Where(r => r.code == "FLOC030" && r.key != null).Select(r => r.key!));

        // --- the tool's columns, row by row ---
        var counts = Statuses.ToDictionary(s => s, _ => 0);
        var writes = new List<(string range, List<List<string>> values)>();
        var changedRows = 0;
        foreach (var tab in tabs)
        {
            var keyCol = tab.Col(StringTable.KEY_COLUMN);
            var toolColumns = new List<string> { "status" };
            if (program != null) toolColumns.Add("where");
            toolColumns.AddRange(tab.header.Where(h => h.StartsWith("src:", StringComparison.OrdinalIgnoreCase)));
            toolColumns = toolColumns.Where(c => tab.Col(c) >= 0).ToList();
            var columns = toolColumns.ToDictionary(c => c, c => new List<List<string>>());

            var lastKeyed = LastKeyedRow(tab);
            foreach (var row in tab.rows.Take(lastKeyed + 1))
            {
                var key = keyCol < row.Length ? row[keyCol].Trim() : "";
                var values = toolColumns.ToDictionary(c => c, c => tab.Cell(row, c));
                if (key.Length > 0)
                {
                    var baseKey = key.Contains('#') ? key.Substring(0, key.IndexOf('#')) : key;
                    var form = key.Contains('#') ? key.Substring(key.IndexOf('#') + 1) : null;
                    bool Needs(string language) => form == null || PluralRules.Uses(language, form);

                    var en = tab.Cell(row, StringTable.DEFAULT_LANGUAGE);
                    var needsEnglish = Needs(StringTable.DEFAULT_LANGUAGE);
                    // A form English does not have (moves#few) is translated from English's "other".
                    if (!needsEnglish) table.TryGet(StringTable.DEFAULT_LANGUAGE, baseKey + "#" + PluralRules.OTHER, out en);
                    var enHash = Hash(en);
                    var stale = false;
                    var missing = false;
                    foreach (var language in languages.Where(l => l != StringTable.DEFAULT_LANGUAGE))
                    {
                        var current = tab.Cell(row, language);
                        var src = "src:" + language;
                        if (!Needs(language))
                        {
                            if (values.ContainsKey(src)) values[src] = "";
                            continue;
                        }
                        if (current.Length == 0)
                        {
                            missing = true;
                            if (values.ContainsKey(src)) values[src] = "";
                            continue;
                        }
                        previous.TryGetValue((key, language), out var before);
                        var recorded = values.TryGetValue(src, out var r) ? r : "";
                        // Written (or changed) since the last sync: made against the English as it is now.
                        if (recorded.Length == 0 || current != before) recorded = enHash;
                        if (values.ContainsKey(src)) values[src] = recorded;
                        if (recorded != enHash) stale = true;
                    }

                    var status = errorsByKey.ContainsKey(key) || errorsByKey.ContainsKey(baseKey) ? "error"
                        : needsEnglish && en.Length == 0 ? "new"
                        : stale ? "stale"
                        : missing ? "missing"
                        : unused.Contains(baseKey) ? "unused"
                        : "ok";
                    counts[status]++;
                    values["status"] = status;
                    if (values.ContainsKey("where")) values["where"] = wheres.TryGetValue(baseKey, out var w) ? w : "";

                    if (toolColumns.Any(c => values[c] != tab.Cell(row, c))) changedRows++;
                }
                foreach (var c in toolColumns) columns[c].Add(new List<string> { values[c] });

                // Keep the in-memory rows up to date, for the snapshot written at the end.
                foreach (var c in toolColumns)
                {
                    var index = tab.Col(c);
                    if (index < row.Length) row[index] = values[c];
                }
            }

            foreach (var c in toolColumns)
            {
                if (lastKeyed < 0) continue;
                var letter = ColumnLetter(tab.Col(c));
                writes.Add((SheetsClient.Range(tab.title, $"{letter}2:{letter}{lastKeyed + 2}"), columns[c]));
            }
        }

        if (!o.dryRun && changedRows > 0) await client.WriteRangesAsync(id!, writes);
        if (!o.dryRun)
        {
            var checkboxes = new JsonArray();
            foreach (var tab in tabs)
            {
                AddReviewCheckboxes(checkboxes, tab);
                // The src:* columns are bookkeeping; a column added later (a new language) is hidden too.
                for (var c = 0; c < tab.header.Count; c++)
                    if (tab.header[c].StartsWith("src:", StringComparison.OrdinalIgnoreCase))
                        checkboxes.Add(new JsonObject
                        {
                            ["updateDimensionProperties"] = new JsonObject
                            {
                                ["range"] = new JsonObject { ["sheetId"] = tab.sheetId, ["dimension"] = "COLUMNS", ["startIndex"] = c, ["endIndex"] = c + 1 },
                                ["properties"] = new JsonObject { ["hiddenByUser"] = true },
                                ["fields"] = "hiddenByUser",
                            }
                        });
            }
            await client.BatchUpdateAsync(id!, checkboxes);
        }
        Console.WriteLine($"  {changedRows} row(s) {(o.dryRun ? "would change" : "updated")}");

        foreach (var (key, messages) in errorsByKey)
            foreach (var m in messages) Console.WriteLine($"  error: {m}");
        if (unused.Count > 0) Console.WriteLine($"  unused: {string.Join(", ", unused.OrderBy(k => k))}");

        WriteSnapshots(o.stringsDir, tabs, o.dryRun);
        if (!o.dryRun && newRows.Count > 0 && File.Exists(inboxPath))
        {
            File.Delete(inboxPath);
            Console.WriteLine($"  {INBOX_FILE} was pushed to the sheet and removed");
        }

        Console.WriteLine("status: " + string.Join(", ", Statuses.Where(s => counts[s] > 0).Select(s => $"{counts[s]} {s}")));
        if (o.dryRun) Console.WriteLine("dry run: nothing was written");
        return 0;
    }

    /// <summary>The tab a new key goes in: the one named like its first part (menu.newgame → menu), else the inbox.</summary>
    private static string TabFor(string key, List<Tab> tabs)
    {
        var dot = key.IndexOf('.');
        var prefix = dot > 0 ? key.Substring(0, dot) : key;
        return tabs.FirstOrDefault(t => string.Equals(t.title, prefix, StringComparison.OrdinalIgnoreCase))?.title ?? INBOX_TAB;
    }

    // ---------------------------------------------------------------- helpers

    private static async Task<(string? id, SheetInfo? info)> OpenTagged(SheetsClient client, Options o)
    {
        var config = LoadConfig(o.stringsDir);
        var sheetArg = o.sheet ?? config["sheetId"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sheetArg))
        {
            Console.Error.WriteLine("[E] no sheet: run `strings init --sheet <url>` first");
            return (null, null);
        }
        var id = SheetsClient.ParseSheetId(sheetArg);
        var info = await client.GetInfoAsync(id);
        if (!info.metadata.ContainsKey(TAG))
        {
            Console.Error.WriteLine($"[E] \"{info.title}\" was not set up by `strings init`, so it is left alone. Run init on it if it is the right sheet.");
            return (null, null);
        }
        return (id, info);
    }

    private static async Task<List<Tab>> ReadTabs(SheetsClient client, string id, SheetInfo info, bool onlyStringTabs)
    {
        var data = await client.ReadTabsAsync(id, info.tabs.Select(t => t.title));
        var tabs = new List<Tab>();
        foreach (var (title, sheetId, _) in info.tabs)
        {
            data.TryGetValue(title, out var rows);
            rows ??= new List<string[]>();
            var tab = new Tab { title = title, sheetId = sheetId };
            if (rows.Count > 0)
            {
                tab.header = rows[0].Select(h => h.Trim()).ToList();
                while (tab.header.Count > 0 && tab.header[^1].Length == 0) tab.header.RemoveAt(tab.header.Count - 1);
                tab.rows = rows.Skip(1).Select(r => r.Length >= tab.header.Count ? r : r.Concat(Enumerable.Repeat("", tab.header.Count - r.Length)).ToArray()).ToList();
            }
            if (onlyStringTabs && tab.Col(StringTable.KEY_COLUMN) < 0) continue;
            tabs.Add(tab);
        }
        return tabs;
    }

    /// <summary>(key, language) to text, from the snapshot CSVs as they are before this sync.</summary>
    private static Dictionary<(string, string), string> ReadLocalCells(string stringsDir)
    {
        var cells = new Dictionary<(string, string), string>();
        foreach (var file in LocalCsvs(stringsDir).Where(f => !IsInbox(f)))
        {
            var rows = StringCsv.Parse(File.ReadAllText(file));
            if (rows.Count == 0) continue;
            var header = rows[0].fields;
            var keyCol = Array.FindIndex(header, h => string.Equals(h.Trim(), StringTable.KEY_COLUMN, StringComparison.OrdinalIgnoreCase));
            if (keyCol < 0) continue;
            foreach (var (_, fields) in rows.Skip(1))
            {
                var key = keyCol < fields.Length ? fields[keyCol].Trim() : "";
                if (key.Length == 0) continue;
                for (var c = 0; c < header.Length && c < fields.Length; c++)
                    if (StringTable.IsLanguageColumn(header[c].Trim())) cells[(key, header[c].Trim())] = fields[c];
            }
        }
        return cells;
    }

    private static IEnumerable<string> LocalCsvs(string stringsDir)
        => Directory.GetFiles(stringsDir, "*.csv").OrderBy(f => f, StringComparer.Ordinal);

    private static bool IsInbox(string file) => string.Equals(Path.GetFileName(file), INBOX_FILE, StringComparison.OrdinalIgnoreCase);

    private static string? FindAssetsDir(string from)
    {
        for (var dir = new DirectoryInfo(from); dir != null; dir = dir.Parent)
            if (string.Equals(dir.Name, "Assets", StringComparison.OrdinalIgnoreCase)) return dir.FullName;
        return null;
    }

    private static JsonObject LoadConfig(string stringsDir)
    {
        var path = Path.Combine(stringsDir, CONFIG_FILE);
        return File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path))!.AsObject() : new JsonObject();
    }

    private static void SaveConfig(string stringsDir, JsonObject config)
    {
        var path = Path.Combine(stringsDir, CONFIG_FILE);
        File.WriteAllText(path, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
    }

    /// <summary>The languages that ship: the config's list, else the columns of the local CSVs, else English.</summary>
    private static List<string> Languages(JsonObject config, string stringsDir)
    {
        if (config["languages"] is JsonArray list && list.Count > 0)
            return list.Select(l => l!.GetValue<string>()).ToList();
        var (table, _) = StringsCli.ReadTable(stringsDir);
        var languages = table.Languages.ToList();
        if (!languages.Contains(StringTable.DEFAULT_LANGUAGE)) languages.Insert(0, StringTable.DEFAULT_LANGUAGE);
        return languages;
    }

    /// <summary>Column index (0-based) to its letters: 0 is A, 26 is AA.</summary>
    public static string ColumnLetter(int index)
    {
        var s = "";
        for (index++; index > 0; index = (index - 1) / 26) s = (char)('A' + (index - 1) % 26) + s;
        return s;
    }

    /// <summary>A short hash of an English text, to tell later whether a translation was made against it.</summary>
    public static string Hash(string text)
        => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text))).Substring(0, 8).ToLowerInvariant();
}
