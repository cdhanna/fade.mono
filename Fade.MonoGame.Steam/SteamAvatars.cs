using Fade.MonoGame.Core;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using Steamworks.Data;

namespace Fade.MonoGame.Steam;

/// <summary>
/// What the avatar command keeps between frames: the pictures Steam is fetching, and which
/// textures hold whose picture.
///
/// Steam fetches a player's picture in the background, so a Fade program asks every frame until
/// it has it, as it does an achievement's icon. Once a texture holds a picture, asking again must
/// cost nothing, and that is what this remembers.
/// </summary>
internal static class SteamAvatars
{
    sealed class LoadedAvatar
    {
        public ulong User;

        // the texture that was made. if the id holds something else now, as it does after the
        // program has been reloaded, the picture has to be made again.
        public Texture2D? Texture;
    }

    static readonly Dictionary<int, LoadedAvatar> Loaded = new();

    // the fetches under way, by player; a fetch that came back with nothing is asked again a
    // little later, not on every frame
    static readonly Dictionary<ulong, Task<Image?>> Fetching = new();
    static readonly Dictionary<ulong, DateTime> RetryAt = new();
    static readonly TimeSpan RetryAfter = TimeSpan.FromSeconds(5);

    public static void Reset()
    {
        Loaded.Clear();
        Fetching.Clear();
        RetryAt.Clear();
    }

    public static bool Load(int textureId, string userId)
    {
        if (!SteamSystem.Available || !ulong.TryParse(userId, out var user) || user == 0) return false;

        TextureSystem.GetTextureIndex(textureId, out _, out var current);
        if (Loaded.TryGetValue(textureId, out var loaded)
            && loaded.User == user
            && loaded.Texture != null
            && ReferenceEquals(current.watchedTexture.Asset, loaded.Texture))
        {
            return true;
        }

        if (!Fetching.TryGetValue(user, out var fetch))
        {
            if (RetryAt.TryGetValue(user, out var at) && DateTime.UtcNow < at) return false;
            fetch = SteamFriends.GetMediumAvatarAsync(user);
            Fetching[user] = fetch;
        }
        if (!fetch.IsCompleted) return false;

        // nothing back: Steam could not get it, or the player has no picture. ask again later
        Image? image = fetch.IsCompletedSuccessfully ? fetch.Result : null;
        if (!image.HasValue || image.Value.Data == null)
        {
            Fetching.Remove(user);
            RetryAt[user] = DateTime.UtcNow + RetryAfter;
            return false;
        }

        if (!TextureSystem.LoadTextureFromPixels(textureId, (int)image.Value.Width, (int)image.Value.Height, image.Value.Data)) return false;

        TextureSystem.GetTextureIndex(textureId, out _, out var made);
        Loaded[textureId] = new LoadedAvatar { User = user, Texture = made.watchedTexture.Asset };
        return true;
    }
}
