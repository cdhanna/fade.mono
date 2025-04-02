using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("texture")]
    public static void LoadTexture(int textureId, string filePath)
    {
        TextureSystem.LoadTexture(textureId, filePath);
    }

    [FadeBasicCommand("texture frames")]
    public static int GetTextureFrameCount(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.descriptor.frames.Count;
    }
}