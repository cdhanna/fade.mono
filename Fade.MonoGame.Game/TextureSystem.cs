using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Game;

public struct TextureDescriptor
{
    public string imageFilePath;
    public int rows, cols;
    public List<TextureFrame> frames;
}

public struct TextureFrame
{
    public int index, row, col;
    public int xOffset, yOffset, xSize, ySize;
}

public struct RuntimeTexture
{
    public int id;
    public Texture2D texture;
    public TextureDescriptor descriptor;
}

public struct RuntimeFont
{
    public int id;
    public SpriteFont font;
}


public class TextureSystem
{
    public static List<RuntimeTexture> textures = new List<RuntimeTexture>();
    private static Dictionary<int, int> _map = new Dictionary<int, int>();
    public static int highestTextureId;
    
    public static List<RuntimeFont> fonts = new List<RuntimeFont>();
    private static Dictionary<int, int> _fontMap = new Dictionary<int, int>();

    public static void Reset()
    {
       textures.Clear();
       _map.Clear();
       highestTextureId = 0;
       
       fonts.Clear();
       _fontMap.Clear();
    }

    
    public static void GetTextureIndex(int textureId, out int index, out RuntimeTexture texture)
    {
        if (!_map.TryGetValue(textureId, out index))
        {
            highestTextureId = textureId > highestTextureId ? textureId : highestTextureId;
            index = _map[textureId] = textures.Count;
            texture = new RuntimeTexture()
            {
                id = textureId,
            };
            textures.Add(texture);
        }
        else
        {
            texture = textures[index];
        }
    }

    public static void GetSpriteFontIndex(int fontId, out int index, out RuntimeFont font)
    {
        if (!_fontMap.TryGetValue(fontId, out index))
        {
            index = _fontMap[fontId] = fonts.Count;
            font = new RuntimeFont()
            {
                id = fontId,
            };
            fonts.Add(font);
        }
        else
        {
            font = fonts[index];
        }
    }

    
    public static Rectangle GetSourceRect(ref RuntimeTexture runtimeTex, ref Sprite sprite)
    {
        var tex = runtimeTex.texture;
        var src = new Rectangle(0, 0, tex.Width, tex.Height);
        if (sprite.currentFrame >= 0)
        {
            var frame = runtimeTex.descriptor.frames[sprite.currentFrame % runtimeTex.descriptor.frames.Count];
            src = new Rectangle(frame.xOffset, frame.yOffset, frame.xSize, frame.ySize);
        }

        return src;
    }

    public static void LoadTextureFromContent(int textureId, string path)
    {
        var texture = GameSystem.game.Content.Load<Texture2D>(path);
        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.descriptor = new TextureDescriptor
        {
            imageFilePath = path
        };
        runtimeTex.texture = texture;
        textures[index] = runtimeTex;
    }
    
    public static void LoadSpriteFontFromContent(int fontId, string path)
    {
        var font = GameSystem.game.Content.Load<SpriteFont>(path);
        GetSpriteFontIndex(fontId, out var index, out var runtimeFont);
        runtimeFont.font = font;
        fonts[index] = runtimeFont;
    }
    
    
    public static void LoadTexture(int textureId, string filePath)
    {
        if (!filePath.EndsWith(".png"))
        {
            throw new InvalidOperationException("Only png textures are allowed");
        }
        
        var texture = Texture2D.FromFile(GameSystem.graphicsDeviceManager.GraphicsDevice, filePath);
        var descriptor = new TextureDescriptor
        {
            imageFilePath = filePath
        };

        var metadataPath = Path.ChangeExtension(filePath, ".metadata.json");
        if (File.Exists(metadataPath))
        {
            var json = File.ReadAllText(metadataPath);
            descriptor = JsonSerializer.Deserialize<TextureDescriptor>(json, new JsonSerializerOptions
            {
                IncludeFields = true
            });
            descriptor.imageFilePath = filePath; // this ignores whatever is in the file. I guess that data is useless?
        }

        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.descriptor = descriptor;
        runtimeTex.texture = texture;
        textures[index] = runtimeTex;

    }
}