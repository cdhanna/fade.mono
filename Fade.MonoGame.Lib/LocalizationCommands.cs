using Fade.MonoGame.Core;
using Fade.MonoGame.Localization;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Loads the game's localized strings from a folder of string tables.</para>
    /// <para>Every <c>.csv</c> file in the folder is read. Call this once, at startup, before any <see cref="Loc">loc$</see> or <see cref="TextLoc">text loc</see>.</para>
    /// </summary>
    /// <remarks>
    /// A string table is a spreadsheet saved as CSV. Its first row names the columns: a
    /// <c>key</c> column, and one column per language, named by its code (<c>en</c>,
    /// <c>pt-BR</c>). Every other row is one string: its key, then its text in each language.
    /// The order of the columns does not matter, and columns with other names are ignored,
    /// so a sheet can keep notes for translators beside the strings.
    ///
    /// A game usually has one file per area of the game (<c>menu.csv</c>, <c>quests.csv</c>),
    /// all in one folder. A key must only appear once across all of them.
    ///
    /// Inside a string, <c>{$name}</c> is filled in with an arg from the code, <c>{@other.key}</c>
    /// is replaced with the text of another key, and anything else in braces, like
    /// <c>{swap}</c>, is left for rich text to draw as an icon. A key with different forms for
    /// one and for many is written as two rows, <c>moves#one</c> and <c>moves#other</c>, and
    /// the <c>count</c> arg picks between them.
    ///
    /// The folder is named the same way as other assets, without the <c>Assets</c> part. While
    /// you work on the game, the files are read from the project, and saving one in a
    /// spreadsheet program updates every loc text on screen right away.
    ///
    /// Problems in the files, like a key defined twice, are written to the game's log.
    /// </remarks>
    /// <example>
    /// Load the strings, then show a title in the player's language:
    /// <code>
    /// ` reads Assets/Fish/Lang/*.csv
    /// load strings "Fish/Lang"
    /// set language "pt-BR"
    ///
    /// font 1, "Fish/Fonts/ZeroCool"
    /// text loc 1, 400, 100, 1, "menu.title"
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="folder">The folder of <c>.csv</c> string tables, such as <c>"Fish/Lang"</c>.</param>
    /// <seealso cref="SetLanguage">set language</seealso>
    /// <seealso cref="Loc">loc$</seealso>
    /// <seealso cref="TextLoc">text loc</seealso>
    [FadeBasicCommand("load strings")]
    public static void LoadStrings(string folder)
    {
        LocalizationSystem.Load(folder);
    }

    /// <summary>
    /// <para>Chooses which language the game's strings are shown in.</para>
    /// <para>Every loc text on screen changes to the new language straight away. Strings already fetched with <see cref="Loc">loc$</see> do not, so fetch them again.</para>
    /// </summary>
    /// <remarks>
    /// The language is named by its column in the string tables, such as <c>"en"</c> or
    /// <c>"pt-BR"</c>. The game starts in <c>"en"</c>. A string that has no text in the chosen
    /// language falls back to its English text.
    ///
    /// Asking for a language the tables do not have leaves the language as it was, and says so
    /// in the log.
    ///
    /// There is one more language, <c>"xx"</c>, that is not in any table. It shows the English
    /// text with its letters swapped for accented ones and a little longer, like
    /// <c>[Ñéw Gámé~~~]</c>. Play the game in it to find text that was never localized (it
    /// still reads as plain English), letters the font cannot draw, and text that will not fit
    /// once it is translated.
    ///
    /// The game does not remember the language between runs. Keep the player's choice with
    /// <c>set pref</c> and set it again at startup.
    /// </remarks>
    /// <example>
    /// Switch between two languages with a key:
    /// <code>
    /// load strings "Fish/Lang"
    /// font 1, "Fish/Fonts/ZeroCool"
    /// text loc 1, 400, 100, 1, "menu.title"
    ///
    /// do
    ///   ` every loc text follows the language, with nothing else to update
    ///   IF new leftKey()
    ///     set language "en"
    ///   ENDIF
    ///   IF new rightKey()
    ///     set language "pt-BR"
    ///   ENDIF
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="language">The language's code, such as <c>"pt-BR"</c>.</param>
    /// <seealso cref="GetLanguage">language$</seealso>
    /// <seealso cref="LoadStrings">load strings</seealso>
    [FadeBasicCommand("set language")]
    public static void SetLanguage(string language)
    {
        LocalizationSystem.SetLanguage(language);
    }

    /// <summary>
    /// <para>Returns the code of the language the game's strings are shown in, such as <c>"en"</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is whatever <see cref="SetLanguage">set language</see> last chose, or <c>"en"</c>
    /// if nothing has.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// set language "pt-BR"
    /// print language$()
    /// </code>
    /// </example>
    /// <returns>The current language's code.</returns>
    /// <seealso cref="SetLanguage">set language</seealso>
    /// <seealso cref="GetLanguageCount">language count</seealso>
    [FadeBasicCommand("language$")]
    public static string GetLanguage()
    {
        return LocalizationSystem.Language;
    }

    /// <summary>
    /// <para>Returns the loaded language that best fits the player, for the language a game starts in before the player has picked one.</para>
    /// <para>The hint can be a language code, like <c>"pt-BR"</c>, or the name Steam gives a language, like <c>"brazilian"</c>. With no hint, or one the tables have no language for, it goes by the computer's own language, and then English.</para>
    /// </summary>
    /// <remarks>
    /// A language from another country counts as close: a player whose computer is in
    /// European Portuguese gets <c>"pt-BR"</c> when that is the only Portuguese there is,
    /// rather than English.
    ///
    /// Pass it the language the player chose for the game in Steam, with
    /// <c>steam language$()</c>, so that their choice there wins over the computer's. When
    /// Steam is not running, that gives <c>""</c>, and the computer's language is used.
    ///
    /// Load the string tables first: only their languages can be picked.
    /// </remarks>
    /// <example>
    /// Start in the player's language, unless they picked one in the options:
    /// <code>
    /// load strings "Fish/Lang"
    /// picked$ = ""
    /// IF get pref(PREF_LANGUAGE, 0) &gt; 0
    ///   picked$ = language code$(get pref(PREF_LANGUAGE, 0) - 1)
    /// ENDIF
    /// IF picked$ = ""
    ///   picked$ = best language$(steam language$())
    /// ENDIF
    /// set language picked$
    /// </code>
    /// </example>
    /// <param name="hint">A language code, a Steam language name, or <c>""</c>.</param>
    /// <returns>The code of a loaded language.</returns>
    /// <seealso cref="SetLanguage">set language</seealso>
    /// <seealso cref="GetLanguageCode">language code$</seealso>
    [FadeBasicCommand("best language$")]
    public static string GetBestLanguage(string hint)
    {
        return LocalizationSystem.BestLanguage(hint);
    }

    /// <summary>
    /// <para>Returns how many languages the loaded string tables have.</para>
    /// <para>Use it with <see cref="GetLanguageCode">language code$</see> to list them, for a language option in a menu.</para>
    /// </summary>
    /// <remarks>
    /// A language is any column of the string tables besides <c>key</c> and the columns kept
    /// for the translators. The pseudo language <c>"xx"</c> is not counted.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// FOR i = 0 TO language count() - 1
    ///   print language code$(i) + " " + language name$(i)
    /// NEXT
    /// </code>
    /// </example>
    /// <returns>The number of languages.</returns>
    /// <seealso cref="GetLanguageCode">language code$</seealso>
    /// <seealso cref="GetLanguageName">language name$</seealso>
    [FadeBasicCommand("language count")]
    public static int GetLanguageCount()
    {
        return LocalizationSystem.resolver.table.Languages.Count;
    }

    /// <summary>
    /// <para>Returns the code of one of the loaded languages, such as <c>"pt-BR"</c>.</para>
    /// <para>The index runs from <c>0</c> to <see cref="GetLanguageCount">language count</see> minus one, in the order the columns appear in the tables. Anything else gives <c>""</c>.</para>
    /// </summary>
    /// <remarks>
    /// Pass the code to <see cref="SetLanguage">set language</see> when the player picks it.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// ` the second language in the tables
    /// set language language code$(1)
    /// </code>
    /// </example>
    /// <param name="index">Which language, from <c>0</c>.</param>
    /// <returns>The language's code, or <c>""</c>.</returns>
    /// <seealso cref="GetLanguageCount">language count</seealso>
    /// <seealso cref="GetLanguageName">language name$</seealso>
    [FadeBasicCommand("language code$")]
    public static string GetLanguageCode(int index)
    {
        var languages = LocalizationSystem.resolver.table.Languages;
        return index >= 0 && index < languages.Count ? languages[index] : "";
    }

    /// <summary>
    /// <para>Returns a language's name for showing to players, in that language, such as <c>"Português"</c>.</para>
    /// <para>It is the language's text for the key <c>lang.name</c>. If that is missing, it is the language's code.</para>
    /// </summary>
    /// <remarks>
    /// A language menu should name each language in itself, so a player who cannot read the
    /// current language can still find their own. Give every language a <c>lang.name</c> row
    /// in the string tables for that.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// FOR i = 0 TO language count() - 1
    ///   print language name$(i)
    /// NEXT
    /// </code>
    /// </example>
    /// <param name="index">Which language, from <c>0</c>.</param>
    /// <returns>The language's own name, or its code.</returns>
    /// <seealso cref="GetLanguageCode">language code$</seealso>
    [FadeBasicCommand("language name$")]
    public static string GetLanguageName(int index)
    {
        var code = GetLanguageCode(index);
        if (code.Length == 0) return "";
        return LocalizationSystem.resolver.table.TryGet(code, "lang.name", out var name) ? name : code;
    }

    /// <summary>
    /// <para>Returns the text of a key from the string tables, in the current language, with its args filled in.</para>
    /// <para>The args come after the key in pairs, a name and then a value: <c>loc$("round.cleared", "round", 3)</c>.</para>
    /// </summary>
    /// <remarks>
    /// Use this for strings that are not texts on screen: a label for the Steam timeline, a
    /// description you keep in a variable, anything you measure or join. For a text on screen,
    /// <see cref="TextLoc">text loc</see> is better, because it changes by itself when the
    /// language or an arg changes. The string from <c>loc$</c> does not, so fetch it again
    /// after <c>set language</c>.
    ///
    /// Each arg fills the <c>{$name}</c> of the same name in the text. A value can be a whole
    /// number, a decimal or a string, and numbers need no <c>str$</c>. A string value that starts
    /// with <c>@</c> is itself a key, so <c>"fish", "@fish.tuna"</c> fills <c>{$fish}</c> with the
    /// text of <c>fish.tuna</c> in the current language. The arg named <c>count</c> also picks
    /// between a key's <c>#one</c> and <c>#other</c> forms.
    ///
    /// The key must be a fixed string, or a variable that holds one written with
    /// <see cref="LocKey">lockey$</see>. Building a key out of pieces with <c>+</c> is not
    /// allowed, because then the build cannot check that the key exists.
    ///
    /// Nothing stops the game. An unknown key comes back as <c>[the.key]</c>, and an arg the
    /// text needs but was not given comes back as <c>{$name}</c>, and both are written to the log.
    /// </remarks>
    /// <example>
    /// Strings with and without args:
    /// <code>
    /// load strings "Fish/Lang"
    ///
    /// ` "New Game" (or "Novo Jogo")
    /// title$ = loc$("menu.newgame")
    ///
    /// ` "Round {$round} cleared" becomes "Round 3 cleared"
    /// msg$ = loc$("round.cleared", "round", 3)
    ///
    /// ` two args, one of them another key's text
    /// quest$ = loc$("quest.catch", "count", 5, "fish", "@fish.tuna")
    /// </code>
    /// </example>
    /// <param name="key">The key, such as <c>"menu.newgame"</c>.</param>
    /// <param name="args">Pairs of an arg name and its value.</param>
    /// <returns>The text to show.</returns>
    /// <seealso cref="TextLoc">text loc</seealso>
    /// <seealso cref="LocKey">lockey$</seealso>
    /// <seealso cref="SetLanguage">set language</seealso>
    [FadeBasicCommand("loc$")]
    public static string Loc([FadeLocKey] string key, [FadeLocArgs(complete = true)] params object[] args)
    {
        return LocalizationSystem.Resolve(key, LocalizationSystem.ArgsFromPairs(key, args));
    }

    /// <summary>
    /// <para>Returns its key unchanged. It marks a key that is kept in a variable or an array, so the build can still check it.</para>
    /// <para>The key must be a fixed string.</para>
    /// </summary>
    /// <remarks>
    /// The build checks every key the game uses: that it is in the string tables, and that the
    /// args passed with it match its text. It can only see a key written where it is used, as
    /// in <c>loc$("menu.newgame")</c>. A key put in a variable first, to be used somewhere
    /// else, would be hidden from it, so write it with <c>lockey$</c>:
    /// <c>name$ = lockey$("menu.newgame")</c>. Then <c>loc$(name$)</c> and
    /// <c>text loc ..., name$</c> work as usual, and the key still counts as used.
    /// </remarks>
    /// <example>
    /// Keys kept in an array, one per kind of fish:
    /// <code>
    /// load strings "Fish/Lang"
    /// DIM fishNames$(3)
    /// fishNames$(0) = lockey$("fish.name.tuna")
    /// fishNames$(1) = lockey$("fish.name.cod")
    /// fishNames$(2) = lockey$("fish.name.eel")
    ///
    /// font 1, "Fish/Fonts/ZeroCool"
    /// text loc 1, 400, 100, 1, fishNames$(kind)
    /// </code>
    /// </example>
    /// <param name="key">The key, as a fixed string.</param>
    /// <returns>The same key.</returns>
    /// <seealso cref="Loc">loc$</seealso>
    /// <seealso cref="TextLoc">text loc</seealso>
    [FadeBasicCommand("lockey$")]
    public static string LocKey([FadeLocKey(constantOnly = true)] string key)
    {
        return key;
    }

    /// <summary>
    /// <para>Shows a text on screen like <c>text</c> does, but the string given is a key: what is drawn is that key's text, in the current language.</para>
    /// <para>The text changes by itself when the language changes, or when one of its args is changed with <see cref="SetTextArg(int, string, int)">set text arg</see>.</para>
    /// </summary>
    /// <remarks>
    /// A text made this way is a "loc text". Everything else about it works as for any text:
    /// position, color, scale, wrapping, rich text icons, and the typewriter all apply to the
    /// text it draws.
    ///
    /// Args come after the key in name, value pairs, as for <see cref="Loc">loc$</see>. To
    /// change one later, such as a counter, use <c>set text arg</c>, which only redraws the text
    /// when the value really changed, so it is safe to call every frame. To switch to another
    /// key, use <see cref="SetTextLoc">set text loc</see>.
    ///
    /// A plain <c>set text</c> on a loc text turns it back into a plain text, which draws its
    /// string exactly as given. Plain texts are never looked up, so text that should not be
    /// translated, like a score or a name, can stay plain.
    /// </remarks>
    /// <example>
    /// A counter that updates in any language:
    /// <code>
    /// load strings "Fish/Lang"
    /// font 1, "Fish/Fonts/ZeroCool"
    ///
    /// ` "{$count} fish left"
    /// fishLeft = 30
    /// text loc 1, 400, 100, 1, "game.fishleft", "count", fishLeft
    ///
    /// do
    ///   IF new downkey()
    ///     fishLeft = fishLeft - 1
    ///   ENDIF
    ///   ` nothing is redrawn unless the number changed
    ///   set text arg 1, "count", fishLeft
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="textId">The text's ID, as for <c>text</c>.</param>
    /// <param name="x">Where the text is, across.</param>
    /// <param name="y">Where the text is, down.</param>
    /// <param name="spriteFontId">The font to draw it with.</param>
    /// <param name="key">The key of the string to show.</param>
    /// <param name="args">Pairs of an arg name and its value.</param>
    /// <seealso cref="SetTextLoc">set text loc</seealso>
    /// <seealso cref="SetTextArg(int, string, int)">set text arg</seealso>
    /// <seealso cref="Loc">loc$</seealso>
    [FadeBasicCommand("text loc")]
    public static void TextLoc(int textId, int x, int y, int spriteFontId, [FadeLocKey] string key, [FadeLocArgs] params object[] args)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.position = new Vector2(x, y);
        textSprite.sprite.imageId = spriteFontId;
        TextSystem.textSprites[index] = textSprite;
        LocalizationSystem.Bind(index, key, LocalizationSystem.ArgsFromPairs(key, args));
    }

    /// <summary>
    /// <para>Makes a text show the text of a key, with new args, and keeps it a loc text.</para>
    /// <para>All the args it had before are replaced by the ones given here.</para>
    /// </summary>
    /// <remarks>
    /// Use it to switch a loc text to another string, or to turn a plain text into a loc text.
    /// To change a single arg and keep the rest, use
    /// <see cref="SetTextArg(int, string, int)">set text arg</see> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// font 1, "Fish/Fonts/ZeroCool"
    /// text loc 1, 400, 100, 1, "game.fishleft", "count", 3
    ///
    /// ` later, when the last fish is gone
    /// set text loc 1, "game.fishgone"
    /// </code>
    /// </example>
    /// <param name="textId">The text's ID.</param>
    /// <param name="key">The key of the string to show.</param>
    /// <param name="args">Pairs of an arg name and its value.</param>
    /// <seealso cref="TextLoc">text loc</seealso>
    /// <seealso cref="SetTextArg(int, string, int)">set text arg</seealso>
    [FadeBasicCommand("set text loc")]
    public static void SetTextLoc(int textId, [FadeLocKey] string key, [FadeLocArgs] params object[] args)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out _);
        LocalizationSystem.Bind(index, key, LocalizationSystem.ArgsFromPairs(key, args));
    }

    /// <summary>
    /// <para>Changes one arg of a loc text, and redraws it if the value changed.</para>
    /// <para>Only for texts made with <see cref="TextLoc">text loc</see> or <see cref="SetTextLoc">set text loc</see>. On a plain text it does nothing, and says so in the log.</para>
    /// </summary>
    /// <remarks>
    /// Setting an arg to the value it already has does nothing at all, so it is safe to call
    /// every frame with a counter or a score. An arg the text did not have yet is added.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// font 1, "Fish/Fonts/ZeroCool"
    /// text loc 1, 400, 100, 1, "game.fishleft", "count", 30
    /// set text arg 1, "count", 29
    /// </code>
    /// </example>
    /// <param name="textId">The loc text's ID.</param>
    /// <param name="name">The arg's name, as in <c>{$name}</c>.</param>
    /// <param name="value">Its new value.</param>
    /// <seealso cref="TextLoc">text loc</seealso>
    /// <seealso cref="SetTextLoc">set text loc</seealso>
    [FadeBasicCommand("set text arg")]
    public static void SetTextArg(int textId, string name, int value)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out _);
        LocalizationSystem.SetArg(index, name, value);
    }

    /// <summary>
    /// <para>Changes one arg of a loc text to a decimal number, and redraws it if the value changed.</para>
    /// </summary>
    /// <remarks>
    /// The same as the whole number form of <c>set text arg</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// set text arg 1, "speed", 1.5
    /// </code>
    /// </example>
    /// <param name="textId">The loc text's ID.</param>
    /// <param name="name">The arg's name.</param>
    /// <param name="value">Its new value.</param>
    /// <seealso cref="SetTextArg(int, string, int)">set text arg</seealso>
    [FadeBasicCommand("set text arg")]
    public static void SetTextArg(int textId, string name, float value)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out _);
        LocalizationSystem.SetArg(index, name, value);
    }

    /// <summary>
    /// <para>Changes one arg of a loc text to a string, and redraws it if the value changed.</para>
    /// <para>A string that starts with <c>@</c> is a key, and the arg shows that key's text.</para>
    /// </summary>
    /// <remarks>
    /// The same as the whole number form of <c>set text arg</c>. Use it for a name the player
    /// typed, or with <c>@</c> for another string from the tables, such as a fish's name.
    /// </remarks>
    /// <example>
    /// <code>
    /// ` "Catch {$count} {$fish}"
    /// text loc 1, 400, 100, 1, "quest.catch", "count", 3, "fish", "@fish.tuna"
    /// set text arg 1, "fish", "@fish.cod"
    /// </code>
    /// </example>
    /// <param name="textId">The loc text's ID.</param>
    /// <param name="name">The arg's name.</param>
    /// <param name="value">Its new value.</param>
    /// <seealso cref="SetTextArg(int, string, int)">set text arg</seealso>
    [FadeBasicCommand("set text arg")]
    public static void SetTextArg(int textId, string name, string value)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out _);
        LocalizationSystem.SetArg(index, name, value);
    }

    /// <summary>
    /// <para>Returns the string a text draws.</para>
    /// <para>For a loc text, that is its key's text in the current language, with its args filled in.</para>
    /// </summary>
    /// <remarks>
    /// Handy in tests, and for checking what a loc text ended up saying. A text that was never
    /// created gives <c>""</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// load strings "Fish/Lang"
    /// font 1, "Fish/Fonts/ZeroCool"
    /// text loc 1, 400, 100, 1, "game.fishleft", "count", 3
    /// ` "3 fish left"
    /// print get text$(1)
    /// </code>
    /// </example>
    /// <param name="textId">The text's ID.</param>
    /// <returns>The string it draws.</returns>
    /// <seealso cref="TextLoc">text loc</seealso>
    /// <seealso cref="SetText">set text</seealso>
    [FadeBasicCommand("get text$")]
    public static string GetText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out _, out var textSprite);
        return textSprite.text ?? "";
    }
}
