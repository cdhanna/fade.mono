using System.Globalization;
using System.Text;

namespace Fade.MonoGame.Localization;

/// <summary>A named value for a <c>{$name}</c> placeholder.</summary>
public readonly struct LocArg
{
    public readonly string name;

    /// <summary>An int, a float or a string. A string that starts with <c>@</c> is a key.</summary>
    public readonly object value;

    public LocArg(string name, object value)
    {
        this.name = name;
        this.value = value;
    }
}

/// <summary>
/// Turns a key and its args into the text to draw, in the current language.
///
/// The steps: pick the plural form from the <c>count</c> arg, find the text in the current
/// language and then in English, expand <c>{@key}</c> references (with the same args, so a
/// referenced key can be plural too), and fill in <c>{$name}</c> args. Rich-text icons are left
/// in the result for the text system to draw.
///
/// It never throws. Anything missing shows up in the text itself, so it is easy to spot:
/// an unknown key as <c>[the.key]</c>, an arg nobody passed as <c>{$name}</c>.
/// </summary>
public sealed class StringResolver
{
    /// <summary>The fake language that shows what isn't localized. See <see cref="Pseudo"/>.</summary>
    public const string PSEUDO_LANGUAGE = "xx";

    public const string COUNT_ARG = "count";

    // A key that refers to itself, directly or through others, stops here.
    private const int MAX_DEPTH = 8;

    public StringTable table = new StringTable();
    public string language = StringTable.DEFAULT_LANGUAGE;

    /// <summary>Told about each problem, such as a missing key. May be null.</summary>
    public Action<string>? warn;

    public string Resolve(string key, IReadOnlyList<LocArg>? args)
    {
        args ??= Array.Empty<LocArg>();
        var pseudo = string.Equals(language, PSEUDO_LANGUAGE, StringComparison.OrdinalIgnoreCase);
        var text = Lookup(key, args, 0, pseudo);
        return pseudo ? "[" + text + new string('~', (text.Length * 35 + 99) / 100) + "]" : text;
    }

    private string Lookup(string key, IReadOnlyList<LocArg> args, int depth, bool pseudo)
    {
        if (depth > MAX_DEPTH)
        {
            warn?.Invoke($"'{key}' refers back to itself");
            return "[" + key + "]";
        }

        var lang = pseudo ? StringTable.DEFAULT_LANGUAGE : language;
        string template;
        if (table.IsPlural(key))
        {
            object count = 0;
            if (!TryGetArg(args, COUNT_ARG, out count))
                warn?.Invoke($"'{key}' is plural but was given no '{COUNT_ARG}' arg");

            // Each language picks its own form (Polish has more of them than English), then falls back
            // to its "other", and then to English, which picks its own form again.
            if (!TryGetForm(lang, key, count, out template)
                && !TryGetForm(StringTable.DEFAULT_LANGUAGE, key, count, out template))
            {
                warn?.Invoke($"'{key}' has no text for any plural form in '{lang}' or '{StringTable.DEFAULT_LANGUAGE}'");
                return "[" + key + "]";
            }
        }
        else if (!table.TryGet(lang, key, out template)
                 && !table.TryGet(StringTable.DEFAULT_LANGUAGE, key, out template))
        {
            warn?.Invoke(table.HasKey(key) ? $"'{key}' has no text in '{lang}' or '{StringTable.DEFAULT_LANGUAGE}'" : $"unknown key '{key}'");
            return "[" + key + "]";
        }

        var sb = new StringBuilder(template.Length + 16);
        foreach (var part in StringTemplate.Parse(template))
        {
            switch (part.kind)
            {
                case TemplatePartKind.Literal:
                    sb.Append(pseudo ? Pseudo(part.value) : part.value);
                    break;
                case TemplatePartKind.Ref:
                    sb.Append(Lookup(part.value, args, depth + 1, pseudo));
                    break;
                case TemplatePartKind.Arg:
                    if (TryGetArg(args, part.value, out var value)) sb.Append(Format(value, depth, pseudo));
                    else
                    {
                        warn?.Invoke($"'{key}' uses {{${part.value}}} but was given no '{part.value}' arg");
                        sb.Append("{$").Append(part.value).Append('}');
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private string Format(object value, int depth, bool pseudo)
    {
        switch (value)
        {
            case string s when s.Length > 1 && s[0] == '@':
                return Lookup(s.Substring(1), Array.Empty<LocArg>(), depth + 1, pseudo);
            case string s:
                return s;
            case IFormattable f:
                // Numbers are written the same way in every language for now.
                return f.ToString(null, CultureInfo.InvariantCulture);
            default:
                return value?.ToString() ?? "";
        }
    }

    private static bool TryGetArg(IReadOnlyList<LocArg> args, string name, out object value)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i].name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = args[i].value;
                return true;
            }
        }
        value = 0;
        return false;
    }

    private bool TryGetForm(string language, string key, object count, out string template)
    {
        var form = PluralRules.Form(language, count);
        var sep = StringTable.PLURAL_SEPARATOR;
        return table.TryGet(language, key + sep + form, out template)
               || table.TryGet(language, key + sep + PluralRules.OTHER, out template);
    }

    /// <summary>The plural form of a number in a language. See <see cref="PluralRules"/>.</summary>
    public static string PluralForm(string language, object count) => PluralRules.Form(language, count);

    /// <summary>
    /// Pseudo-localization: English with its letters swapped for accented ones. Text in a game
    /// running in <see cref="PSEUDO_LANGUAGE"/> that still reads as plain English was never made
    /// a key; accents that come out as holes are missing from the font. Only Latin-1 letters are
    /// used, so that a font baked with Latin-1 can draw all of it. Icons in braces are kept.
    /// </summary>
    public static string Pseudo(string text)
    {
        var sb = new StringBuilder(text.Length);
        var inBraces = false;
        foreach (var c in text)
        {
            if (c == '{') inBraces = true;
            if (inBraces)
            {
                sb.Append(c);
                if (c == '}') inBraces = false;
                continue;
            }
            sb.Append(c switch
            {
                'a' => 'á', 'e' => 'é', 'i' => 'í', 'o' => 'ö', 'u' => 'ü', 'c' => 'ç', 'n' => 'ñ', 'y' => 'ý',
                'A' => 'Å', 'E' => 'É', 'I' => 'Î', 'O' => 'Ø', 'U' => 'Ü', 'C' => 'Ç', 'N' => 'Ñ', 'Y' => 'Ý',
                _ => c
            });
        }
        return sb.ToString();
    }
}
