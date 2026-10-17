using System.Text;

namespace Fade.MonoGame.Localization;

/// <summary>
/// The CSV dialect of the string table snapshots: RFC 4180, which is what Google Sheets exports
/// and what a spreadsheet program writes. Fields are separated by commas; a field with a comma,
/// a quote or a line break in it is wrapped in quotes, and a quote inside it is doubled. Rows end
/// with CRLF or LF. A UTF-8 byte order mark at the start is dropped.
/// </summary>
public static class StringCsv
{
    /// <summary>
    /// The rows of a CSV, each with the 1-based line it starts on (a quoted field can span
    /// lines, so a row's line is not its index). Empty lines are skipped.
    /// </summary>
    public static List<(int line, string[] fields)> Parse(string text)
    {
        var rows = new List<(int, string[])>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var i = 0;
        var line = 1;
        var rowLine = 1;
        var inQuotes = false;
        var rowHasContent = false;

        if (text.Length > 0 && text[0] == '﻿') i = 1;

        void EndField()
        {
            fields.Add(field.ToString());
            field.Clear();
        }

        void EndRow()
        {
            EndField();
            if (rowHasContent) rows.Add((rowLine, fields.ToArray()));
            fields.Clear();
            rowHasContent = false;
        }

        for (; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else
                {
                    if (c == '\n') line++;
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    rowHasContent = true;
                    break;
                case ',':
                    EndField();
                    rowHasContent = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    EndRow();
                    line++;
                    rowLine = line;
                    break;
                default:
                    field.Append(c);
                    rowHasContent = true;
                    break;
            }
        }

        if (rowHasContent || field.Length > 0 || fields.Count > 0) EndRow();
        return rows;
    }

    /// <summary>Writes rows back out, quoting only the fields that need it. Lines end with LF.</summary>
    public static string Write(IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            for (var f = 0; f < row.Count; f++)
            {
                if (f > 0) sb.Append(',');
                var value = row[f] ?? "";
                if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                    sb.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
                else
                    sb.Append(value);
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
