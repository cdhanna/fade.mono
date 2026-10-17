using System.Globalization;

namespace Fade.MonoGame.Localization;

/// <summary>
/// Which plural form a number takes in a language, from the CLDR plural rules (for whole numbers,
/// which is what a game counts). A plural key has one row per form: <c>moves#one</c>,
/// <c>moves#other</c>, and for a language like Polish also <c>moves#few</c> and <c>moves#many</c>.
///
/// <list type="bullet">
/// <item>English, German and most others: <c>one</c> for 1, <c>other</c> for the rest.</item>
/// <item>Portuguese and French: <c>one</c> for 0 and 1 (and anything below 2), <c>other</c> for the rest.</item>
/// <item>Polish: <c>one</c> for 1; <c>few</c> for 2–4, 22–24, 32–34... but not 12–14; <c>many</c> for
/// the other whole numbers (0, 5–21, 25–31...); <c>other</c> for fractions.</item>
/// </list>
/// </summary>
public static class PluralRules
{
    public const string ONE = "one";
    public const string FEW = "few";
    public const string MANY = "many";
    public const string OTHER = "other";

    private static readonly string[] OneOther = { ONE, OTHER };
    private static readonly string[] Slavic = { ONE, FEW, MANY, OTHER };

    private static string Primary(string language) => language.Split('-', '_')[0].ToLowerInvariant();

    /// <summary>The forms a language uses, so a check can tell a missing row from one it never needs.</summary>
    public static IReadOnlyList<string> Forms(string language) => Primary(language) switch
    {
        "pl" => Slavic,
        _ => OneOther,
    };

    public static bool Uses(string language, string form)
    {
        foreach (var f in Forms(language))
            if (f == form) return true;
        return false;
    }

    public static string Form(string language, object count)
    {
        double n;
        try { n = Convert.ToDouble(count, CultureInfo.InvariantCulture); }
        catch { return OTHER; }

        var whole = Math.Abs(n % 1) < 1e-9;
        var i = (long)Math.Floor(Math.Abs(n));
        switch (Primary(language))
        {
            case "pt":
            case "fr":
                return i <= 1 ? ONE : OTHER;
            case "pl":
                if (!whole) return OTHER;
                if (i == 1) return ONE;
                var last = i % 10;
                var lastTwo = i % 100;
                if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14)) return FEW;
                return MANY;
            default:
                return whole && i == 1 ? ONE : OTHER;
        }
    }
}
