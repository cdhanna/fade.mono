using Fade.MonoGame.Core;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using Steamworks.Data;

namespace Fade.MonoGame.Steam;

/// <summary>
/// What the achievement commands keep between frames: which textures hold the icon of
/// which achievement.
///
/// Steam does not have an icon on hand until it has been downloaded, and asking for one is
/// what starts the download. So a Fade program asks every frame until it gets it. Once a
/// texture holds an icon, asking again must cost nothing, and that is what this remembers.
/// </summary>
internal static class SteamAchievements
{
    sealed class LoadedIcon
    {
        public string Name = "";

        // an achievement has two icons, and Steam gives the one for how it is now
        public bool Unlocked;

        // the texture that was made. if the id holds something else now, as it does after the
        // program has been reloaded, the icon has to be made again.
        public Texture2D? Texture;
    }

    static readonly Dictionary<int, LoadedIcon> Icons = new();

    public static void Reset() => Icons.Clear();

    public static string NameAt(int index)
    {
        if (!SteamSystem.Available || index < 0) return "";
        return SteamUserStats.Achievements.Skip(index).Select(a => a.Identifier).FirstOrDefault() ?? "";
    }

    public static bool LoadIcon(int textureId, string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return false;

        var achievement = new Achievement(name);
        var unlocked = achievement.State;

        TextureSystem.GetTextureIndex(textureId, out _, out var current);
        if (Icons.TryGetValue(textureId, out var loaded)
            && loaded.Name == name
            && loaded.Unlocked == unlocked
            && loaded.Texture != null
            && ReferenceEquals(current.watchedTexture.Asset, loaded.Texture))
        {
            return true;
        }

        // nothing yet means that Steam is still fetching it, or that there is no such achievement
        var image = achievement.GetIcon();
        if (!image.HasValue || image.Value.Data == null) return false;

        var width = (int)image.Value.Width;
        var height = (int)image.Value.Height;
        if (!TextureSystem.LoadTextureFromPixels(textureId, width, height, image.Value.Data)) return false;

        TextureSystem.GetTextureIndex(textureId, out _, out var made);
        Icons[textureId] = new LoadedIcon { Name = name, Unlocked = unlocked, Texture = made.watchedTexture.Asset };
        return true;
    }
}
