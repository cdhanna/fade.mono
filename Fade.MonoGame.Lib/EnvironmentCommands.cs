using System;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Returns the value of an environment variable, as text.</para>
    /// <para>Returns an empty string when the variable is not set.</para>
    /// </summary>
    /// <remarks>
    /// Environment variables are settings handed to a program from outside it, by whatever
    /// launched it. They are a good way to change how a game starts without changing the game:
    /// skipping an intro while you are working on it, say, or picking a level to start on.
    ///
    /// If all you need to know is whether a switch is turned on, use
    /// <see cref="IsEnvSet">is env set</see> instead.
    ///
    /// The browser has no environment variables, so there this always returns an empty string.
    /// </remarks>
    /// <example>
    /// Start on a level named from outside the game:
    /// <code>
    /// level$ = env$("START_LEVEL")
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "level: " + level$
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the variable, or an empty string if it is not set.</returns>
    /// <seealso cref="IsEnvSet">is env set</seealso>
    [FadeBasicCommand("env$")]
    public static string GetEnv(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return Environment.GetEnvironmentVariable(name) ?? "";
    }

    /// <summary>
    /// <para>Checks whether an environment variable is turned on.</para>
    /// <para>Returns <c>1</c> when the variable is set to anything other than nothing,
    /// <c>0</c>, <c>false</c>, <c>no</c> or <c>off</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is the easy way to add a switch to a game that is flipped from outside it.
    /// A variable that is missing counts as off, so the game behaves normally for anybody
    /// who has not set it.
    ///
    /// Use <see cref="GetEnv">env$</see> when you need the value itself.
    /// </remarks>
    /// <example>
    /// Skip the intro when a variable is set:
    /// <code>
    /// font 1, "font"
    ///
    /// IF is env set("SKIP_INTRO")
    ///   text 1, 470, 200, 1, "straight to the game"
    /// ELSE
    ///   text 1, 470, 200, 1, "the intro goes here"
    /// ENDIF
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns><c>1</c> if the variable is turned on, <c>0</c> if it is off or missing.</returns>
    /// <seealso cref="GetEnv">env$</seealso>
    [FadeBasicCommand("is env set")]
    public static int IsEnvSet(string name)
    {
        var value = GetEnv(name).Trim();
        if (value.Length == 0) return 0;

        switch (value.ToLowerInvariant())
        {
            case "0":
            case "false":
            case "no":
            case "off":
                return 0;
            default:
                return 1;
        }
    }
}
