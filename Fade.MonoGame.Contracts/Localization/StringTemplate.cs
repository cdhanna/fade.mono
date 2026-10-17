using System.Text;

namespace Fade.MonoGame.Localization;

public enum TemplatePartKind
{
    /// <summary>Text drawn as it is. Rich-text icons (<c>{swap}</c>) are literal text here too.</summary>
    Literal,

    /// <summary><c>{$name}</c>: an arg passed from code.</summary>
    Arg,

    /// <summary><c>{@some.key}</c>: the text of another key.</summary>
    Ref,
}

public readonly struct TemplatePart
{
    public readonly TemplatePartKind kind;

    /// <summary>The literal text, the arg name, or the referenced key.</summary>
    public readonly string value;

    public TemplatePart(TemplatePartKind kind, string value)
    {
        this.kind = kind;
        this.value = value;
    }
}

/// <summary>
/// The syntax inside a string table entry. <c>{$name}</c> is an arg, <c>{@key}</c> is another
/// key's text, and any other <c>{...}</c> is left alone for rich text, which draws font icons
/// named in braces. The sigils are what keep the two from colliding.
/// </summary>
public static class StringTemplate
{
    public static List<TemplatePart> Parse(string text)
    {
        var parts = new List<TemplatePart>();
        var literal = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '{' && i + 2 < text.Length && (text[i + 1] == '$' || text[i + 1] == '@'))
            {
                var close = text.IndexOf('}', i + 2);
                if (close > i + 2)
                {
                    if (literal.Length > 0)
                    {
                        parts.Add(new TemplatePart(TemplatePartKind.Literal, literal.ToString()));
                        literal.Clear();
                    }
                    var kind = text[i + 1] == '$' ? TemplatePartKind.Arg : TemplatePartKind.Ref;
                    parts.Add(new TemplatePart(kind, text.Substring(i + 2, close - i - 2).Trim()));
                    i = close + 1;
                    continue;
                }
            }
            literal.Append(c);
            i++;
        }
        if (literal.Length > 0) parts.Add(new TemplatePart(TemplatePartKind.Literal, literal.ToString()));
        return parts;
    }

    /// <summary>The arg names a string uses, for comparing a translation against English.</summary>
    public static HashSet<string> ArgNames(string text)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Parse(text))
            if (p.kind == TemplatePartKind.Arg) names.Add(p.value);
        return names;
    }

    /// <summary>The rich-text icon names a string uses (<c>{swap}</c>), in order.</summary>
    public static List<string> IconNames(string text)
    {
        var icons = new List<string>();
        foreach (var p in Parse(text))
        {
            if (p.kind != TemplatePartKind.Literal) continue;
            var s = p.value;
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] != '{') continue;
                var close = s.IndexOf('}', i + 1);
                if (close < 0) break;
                icons.Add(s.Substring(i + 1, close - i - 1));
                i = close;
            }
        }
        return icons;
    }
}
