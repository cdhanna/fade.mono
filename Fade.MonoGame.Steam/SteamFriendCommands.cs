using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Steam;

public partial class FadeSteamCommands
{
    /// <summary>
    /// <para>Puts a player's Steam profile picture into a texture, so that a sprite can show it.</para>
    /// <para>Returns <c>1</c> once the texture holds the picture, and <c>0</c> while Steam is still fetching it.</para>
    /// </summary>
    /// <remarks>
    /// Steam fetches a picture in the background, and asking is what starts it. So the first
    /// call returns <c>0</c>. Call this every frame until it returns <c>1</c>, and do not use
    /// the texture before then. Once it has returned <c>1</c>, calling it again costs nothing,
    /// so it is fine to leave it in the loop.
    ///
    /// The player is a Steam id, as text: the player's own from
    /// <see cref="GetSteamUserId">steam user id$</see>, or anyone's on a leaderboard from
    /// <see cref="GetSteamScoreUserId">steam score user id$</see>. Give the texture another
    /// player's id and it gets their picture instead.
    ///
    /// The pictures are 64 pixels across. Use <c>size sprite</c> to draw one at another size.
    /// The texture id is one that you choose, the same as for a texture that is loaded from a
    /// file.
    ///
    /// It stays <c>0</c> for an id that is not a Steam id, and when Steam is not up. A player
    /// with no picture of their own has Steam's question mark.
    /// </remarks>
    /// <example>
    /// Show your own picture, once it has arrived:
    /// <code>
    /// me$ = steam user id$()
    /// shown = 0
    ///
    /// DO
    ///   ` 0 until steam has fetched the picture, and 1 from then on
    ///   IF steam avatar(60, me$)
    ///     IF shown = 0
    ///       sprite 1, 100, 100, 60
    ///       shown = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The texture to put the picture into. Any texture id that you choose.</param>
    /// <param name="userId">The player's Steam id, as text.</param>
    /// <returns><c>1</c> if the texture holds the picture, <c>0</c> if not yet.</returns>
    /// <seealso cref="GetSteamUserId">steam user id$</seealso>
    /// <seealso cref="GetSteamScoreUserId">steam score user id$</seealso>
    /// <seealso cref="LoadSteamAchievementIcon">steam achievement icon</seealso>
    [FadeBasicCommand("steam avatar")]
    public static int LoadSteamAvatar(int textureId, string userId)
    {
        return SteamAvatars.Load(textureId, userId) ? 1 : 0;
    }
}
