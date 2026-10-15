using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Steam;

public partial class FadeSteamCommands
{
    /// <summary>
    /// <para>Returns the kind of controller that the player has, as Steam knows it.</para>
    /// <para>Returns <c>0</c> when no controller is plugged in, or when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// The game reads every controller as if it were an Xbox controller, and that is how
    /// the buttons are named. This says what the controller really is, so that the game can
    /// show the right pictures of its buttons.
    ///
    /// The kinds are <c>1</c> for a Steam Controller, <c>2</c> for an Xbox 360 controller,
    /// <c>3</c> for an Xbox One or Series controller, <c>4</c> for a controller of no known
    /// make, <c>5</c> for a PlayStation 4 controller, <c>8</c> for a pair of Joy-Cons,
    /// <c>9</c> for one Joy-Con, <c>10</c> for a Switch Pro controller, <c>12</c> for a
    /// PlayStation 3 controller, <c>13</c> for a PlayStation 5 controller, and <c>14</c> for
    /// the buttons of a Steam Deck.
    ///
    /// Steam only knows of a controller that it is handling itself. It handles none for a
    /// game that it did not start, and a player can turn it off for each make of
    /// controller. The kind is then worked out from what the controller calls itself, and a
    /// controller with a name that says nothing counts as an Xbox One controller.
    ///
    /// The answer is looked up about twice a second, so it takes a moment to notice that one
    /// controller has been swapped for another.
    /// </remarks>
    /// <example>
    /// Name the confirm button for the controller that is plugged in:
    /// <code>
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   button$ = "A"
    ///   IF steam input type() = 5 THEN button$ = "Cross"
    ///   IF steam input type() = 13 THEN button$ = "Cross"
    ///   IF steam input type() = 10 THEN button$ = "B"
    ///   set text 1, "press " + button$
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The kind of controller, or <c>0</c> when there is none.</returns>
    /// <seealso cref="LoadSteamInputGlyph">steam input glyph</seealso>
    [FadeBasicCommand("steam input type")]
    public static int GetSteamInputType()
    {
        if (!SteamSystem.Available) return 0;
        return SteamInputGlyphs.ControllerType;
    }

    /// <summary>
    /// <para>Makes Steam carry on as if a controller of a kind were plugged in, whatever is.</para>
    /// <para>This is for looking at the pictures of a controller that you do not have.</para>
    /// </summary>
    /// <remarks>
    /// From here on, <see cref="GetSteamInputType">steam input type</see> returns the kind
    /// that was set, and <see cref="LoadSteamInputGlyph">steam input glyph</see> gives the
    /// pictures of that kind of controller. The kinds are the ones that
    /// <see cref="GetSteamInputType">steam input type</see> lists.
    ///
    /// Set it to <c>0</c> to go back to the controller that is really there.
    ///
    /// Nothing about how the controller is read changes. Only the pictures do. It does
    /// nothing when Steam is not up.
    /// </remarks>
    /// <example>
    /// Look at the confirm button of a PlayStation 5 controller, and of a Switch Pro controller:
    /// <code>
    /// sprite 1, 100, 100, 50
    ///
    /// DO
    ///   IF new spaceKey() THEN set steam input type 13
    ///   IF new returnKey() THEN set steam input type 10
    ///   got = steam input glyph(50, "A", 64, 0, 0)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="type">The kind of controller to carry on as, or <c>0</c> for the one that is there.</param>
    /// <seealso cref="GetSteamInputType">steam input type</seealso>
    /// <seealso cref="LoadSteamInputGlyph">steam input glyph</seealso>
    [FadeBasicCommand("set steam input type")]
    public static void SetSteamInputType(int type)
    {
        if (!SteamSystem.Available) return;
        SteamInputGlyphs.Pretend(type);
    }

    /// <summary>
    /// <para>Puts Steam's picture of a controller button into a texture, so that a sprite or a font icon can show it.</para>
    /// <para>Returns <c>1</c> if the texture holds the picture, and <c>0</c> if Steam has none to give.</para>
    /// </summary>
    /// <remarks>
    /// Steam has pictures of the buttons of every controller that it knows. The button is
    /// named the way that <c>padCode</c> names it, after an Xbox controller: <c>"A"</c>,
    /// <c>"Back"</c>, <c>"LeftShoulder"</c>, <c>"DPadUp"</c>. The picture is of the button
    /// in the same place on the controller that the player really has. On a PlayStation
    /// controller, <c>"A"</c> gives the cross.
    ///
    /// Three more names give a picture of a whole part: <c>"DPad"</c>,
    /// <c>"LeftThumbstick"</c> and <c>"RightThumbstick"</c>.
    ///
    /// The texture is a square that is <c>size</c> pixels across, up to <c>256</c>. Steam's
    /// own pictures are big, and this shrinks one to fit. Ask for the size that it will be
    /// drawn at: a picture that is drawn pixel for pixel stays sharp, and one that is shrunk
    /// again while it is drawn does not. The picture fills the square from edge to edge
    /// unless <c>inset</c> says to leave that many pixels clear on every side, which is how
    /// to match pictures of your own that have room around them.
    ///
    /// The style is <c>0</c> for pictures that go on any background, <c>1</c> for black on
    /// white, and <c>2</c> for white on black. Add <c>16</c> to the style for face buttons
    /// without their colors, and <c>32</c> for face buttons that are filled in.
    ///
    /// It returns <c>0</c> when Steam is not up, and when
    /// <see cref="GetSteamInputType">steam input type</see> is <c>0</c>. Keep pictures of
    /// your own to show then. Once a texture holds a picture, asking again costs nothing
    /// until the player changes to another kind of controller, and then the same call puts
    /// the new picture in. So it is fine to leave it in the loop.
    ///
    /// The texture id is one that you choose, the same as for a texture that is loaded from
    /// a file.
    /// </remarks>
    /// <example>
    /// Show the confirm button of whichever controller is plugged in:
    /// <code>
    /// shown = 0
    ///
    /// DO
    ///   IF steam input glyph(50, "A", 64, 0, 0)
    ///     IF shown = 0
    ///       sprite 1, 100, 100, 50
    ///       shown = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The texture to put the picture into. Any texture id that you choose.</param>
    /// <param name="button">The name of the button, like <c>"A"</c>, <c>"Start"</c>, or <c>"DPad"</c>.</param>
    /// <param name="size">How many pixels across the texture is, from <c>1</c> to <c>256</c>.</param>
    /// <param name="inset">How many pixels to leave clear on every side of the picture.</param>
    /// <param name="style"><c>0</c> for any background, <c>1</c> for black on white, <c>2</c> for white on black.</param>
    /// <returns><c>1</c> if the texture holds the picture, <c>0</c> if not.</returns>
    /// <seealso cref="GetSteamInputType">steam input type</seealso>
    /// <seealso cref="LoadSteamAchievementIcon">steam achievement icon</seealso>
    [FadeBasicCommand("steam input glyph")]
    public static int LoadSteamInputGlyph(int textureId, string button, int size, int inset, int style)
    {
        if (!SteamSystem.Available) return 0;
        return SteamInputGlyphs.LoadGlyph(textureId, button, size, inset, style) ? 1 : 0;
    }
}
