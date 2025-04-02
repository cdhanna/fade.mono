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

public class TextureSystem
{
    public static List<RuntimeTexture> textures = new List<RuntimeTexture>();
    private static Dictionary<int, int> _map = new Dictionary<int, int>();

    public static void GetTextureIndex(int textureId, out int index, out RuntimeTexture texture)
    {
        if (!_map.TryGetValue(textureId, out index))
        {
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
    
    public static void LoadTexture(int textureId, string filePath)
    {
        if (!filePath.EndsWith(".png"))
        {
            throw new InvalidOperationException("Only png textures are allowed");
        }
        var texture = Texture2D.FromFile(GameSystem.graphicsDeviceManager.GraphicsDevice, Path.ChangeExtension(filePath, ".cropped2.png"));
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