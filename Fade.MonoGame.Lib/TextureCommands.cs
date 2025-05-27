using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    
    [FadeBasicCommand("font")]
    public static void LoadSpriteFont(int fontId, string filePath)
    {
        TextureSystem.LoadSpriteFontFromContent(fontId, filePath);
    }

    [FadeBasicCommand("free texture id")]
    public static int GetFreeTextureNextId(ref int textureId)
    {
        textureId = TextureSystem.highestTextureId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return textureId;
    }
    
    [FadeBasicCommand("reserve texture id")]
    public static int ReserveTextureNextId(ref int textureId)
    {
        GetFreeTextureNextId(ref textureId);
        TextureSystem.GetTextureIndex(textureId, out _, out _);
        return textureId;
    }
    
    
    [FadeBasicCommand("texture")]
    public static void LoadTexture(int textureId, string filePath)
    {
        TextureSystem.LoadTextureFromContent(textureId, filePath);
    }

    [FadeBasicCommand("set texture frame grid")]
    public static void SetTextureFramesByRowCol(int textureId, int rows, int columns)
    {
        TextureSystem.GetTextureIndex(textureId, out var index, out var tex);
        // tex.descriptor.cols = columns;
        // tex.descriptor.rows = rows;
        var total = rows * columns;
        var width = tex.texture.Width;
        var height = tex.texture.Height;

        var cellWidth = width / columns;
        var cellHeight = height / rows;
        
        var frames = tex.descriptor.frames = new List<TextureFrame>(total);
        for (var y = 0; y < rows; y++)
        {
            var yOffset = y * cellHeight;
            for (var x = 0; x < columns; x++)
            {
                var xOffset = x * cellWidth;
                frames.Add(new TextureFrame
                {
                    xOffset = xOffset, 
                    yOffset = yOffset,
                    xSize = cellWidth,
                    ySize = cellHeight
                });
            }
        }

        TextureSystem.textures[index] = tex;
    }

    [FadeBasicCommand("texture frames")]
    public static int GetTextureFrameCount(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.descriptor.frames.Count;
    }

    [FadeBasicCommand("texture width")]
    public static int GetTextureWidth(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.texture.Width;
    }
    
    [FadeBasicCommand("texture height")]
    public static int GetTextureHeight(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.texture.Height;
    }
    
    /// <summary>
    /// Aspect is h/w
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    [FadeBasicCommand("texture aspect")]
    public static float GetTextureAspect(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.texture.Height / (float)tex.texture.Width;
    }
}