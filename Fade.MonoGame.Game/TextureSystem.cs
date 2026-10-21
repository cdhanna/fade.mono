using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
using Microsoft.Xna.Framework.Graphics;
using SpriteFont = Microsoft.Xna.Framework.Graphics.Fade.SpriteFont;
using SpriteFontXna = Microsoft.Xna.Framework.Graphics.SpriteFont;

namespace Fade.MonoGame.Core;

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
    public Texture2D texture => watchedTexture.Asset;
    public WatchedAsset<Texture2D> watchedTexture;
    public TextureDescriptor descriptor;

    /// <summary>
    /// The texture id of the atlas page that this picture is a region of, or 0 for a picture of its own.
    /// Its size is then the size of its region, not of the page. See TextureSystem.atlasRegions.
    /// </summary>
    public int atlasPageId;

    public void SetComputedTexture(Texture2D tex)
    {
        watchedTexture = new WatchedAsset<Texture2D>
        {
            // don't set the name/path, so it is computed. 
            Asset = tex
        };
    }
}

public struct RuntimeFont
{
    public int id;
    public SpriteFont font;

    // The icons that rich text can draw with this font. Null until the first icon is registered.
    public FontIconSet icons;

    // The effect that text in this font is drawn with, unless the text has one of its own. 0 is none.
    public int effectId;
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
       atlasRegions.Clear();
    }

    public static void RefreshTextures()
    {
#if !BROWSER
        for (var i = 0; i < textures.Count; i++)
        {
            var tex = textures[i];
            if (tex.watchedTexture.assetName != null && GameSystem.game.ContentWatcher.TryRefreshAsset(ref tex.watchedTexture))
            {
                textures[i] = tex;
            }
        }
#endif
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
        return GetSourceRect(ref runtimeTex, sprite.currentFrame);
    }

    public static Rectangle GetSourceRect(ref RuntimeTexture runtimeTex, int currentFrame)
    {
        var tex = runtimeTex.texture;
        var frames = runtimeTex.descriptor.frames;

        // No frame table: the sprite is the whole image, which is the ordinary simple case.
        if (frames == null || frames.Count == 0)
            return new Rectangle(0, 0, tex.Width, tex.Height);

        // A frame table EXISTS, so the texture is a sheet and the whole-image rect is never the
        // right answer -- not even for a sprite that never called `set sprite frame` and still
        // carries the default currentFrame of -1. It used to fall through to the whole image,
        // which is merely odd for a private sheet and actively wrong for a shared one: a runtime
        // atlas points many texture ids at ONE page, so "the whole image" becomes the whole page
        // and the sprite draws every material at once, dithered down to its own size. That looks
        // like corruption, not like a missing frame index, which is what makes it expensive to
        // diagnose.
        var index = currentFrame < 0 ? 0 : currentFrame % frames.Count;
        var frame = frames[index];
        return new Rectangle(frame.xOffset, frame.yOffset, frame.xSize, frame.ySize);
    }

    /// <summary>
    /// A picture that lives in a rectangle of an atlas page, in place of a file of its own. Registered
    /// with `atlas region`, by the path that the game would otherwise load it from.
    /// </summary>
    public struct AtlasRegion
    {
        public int pageTextureId;
        public Rectangle rect;
    }

    /// <summary>
    /// Paths that are pictures in an atlas, by the path a `texture` command would load. Matched without
    /// regard to case or slash direction, the way content paths are.
    /// </summary>
    public static Dictionary<string, AtlasRegion> atlasRegions = new Dictionary<string, AtlasRegion>();

    public static string NormalizeAtlasPath(string path) => path.Replace('\\', '/').Trim().ToLowerInvariant();

    /// <summary>
    /// If <paramref name="path"/> is a region of an atlas, point the texture at the atlas page with
    /// that region as its one frame, and say so. Every sprite that uses the texture then draws only
    /// the region, and <see cref="GetSourceRect(ref RuntimeTexture, int)"/> gives its size, so code
    /// that loads the picture by its old path does not have to know that it moved.
    /// </summary>
    static bool TryLoadFromAtlas(int textureId, string path)
    {
        if (atlasRegions.Count == 0) return false;
        if (!atlasRegions.TryGetValue(NormalizeAtlasPath(path), out var region)) return false;

        GetTextureIndex(region.pageTextureId, out _, out var page);
        if (page.texture == null)
        {
            Console.Error.WriteLine($"[fade] atlas page {region.pageTextureId} for '{path}' is not loaded");
            return false;
        }

        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.watchedTexture = page.watchedTexture;
        runtimeTex.descriptor = new TextureDescriptor
        {
            imageFilePath = path,
            frames = new List<TextureFrame>
            {
                new TextureFrame
                {
                    index = 0,
                    xOffset = region.rect.X, yOffset = region.rect.Y,
                    xSize = region.rect.Width, ySize = region.rect.Height,
                }
            }
        };
        runtimeTex.atlasPageId = region.pageTextureId;
        textures[index] = runtimeTex;
        Console.WriteLine($"[fade] texture {textureId} '{path}' from atlas {region.pageTextureId} at {region.rect.X},{region.rect.Y} ({region.rect.Width} x {region.rect.Height})");
        return true;
    }

    public static void LoadTextureFromContent(int textureId, string path)
    {
        if (TryLoadFromAtlas(textureId, path)) return;
#if BROWSER
        // Browser: BrowserContentManager serves XNBs from an in-memory dict
        // the page fills before LoadProgram. Hot-reload (the desktop
        // ContentWatcher contract) doesn't apply — re-running re-pushes the
        // asset bytes, which is enough for v1.
        Texture2D texture;
        try
        {
            texture = GameSystem.game.Content.Load<Texture2D>(path);
        }
        catch (Exception ex)
        {
            // Surface as a runtime log rather than a tick-killing exception.
            // Without this, the next sprite-render frame would NRE on the
            // null texture; we'd rather print and keep the game running so
            // the user can fix the asset and re-run.
            Console.Error.WriteLine($"[fade] texture load failed: '{path}': {ex.Message}");
            return;
        }
        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.descriptor = new TextureDescriptor { imageFilePath = path };
        runtimeTex.SetComputedTexture(texture);
        textures[index] = runtimeTex;
#else
        WatchedAsset<Texture2D> texture;
        try
        {
            texture = GameSystem.game.ContentWatcher.Watch<Texture2D>(path);
        }
        catch (Exception ex)
        {
            // Say which one, so that a build that shipped without an asset can be told from its
            // log. The texture stays unset, and the renderer skips sprites that use it.
            Console.Error.WriteLine($"[fade] texture load failed: '{path}': {ex.Message}");
            return;
        }

        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.descriptor = new TextureDescriptor
        {
            imageFilePath = path
        };
        runtimeTex.watchedTexture = texture;
        textures[index] = runtimeTex;
#endif
    }
    
    /// <summary>
    /// Make a texture out of pixels that did not come from a content file: an image that was
    /// downloaded, or drawn in code. Four bytes for every pixel, in the order red, green, blue,
    /// alpha, a row at a time from the top. The alpha is straight, not premultiplied.
    ///
    /// A texture id that already has a texture gets the new one in its place. Returns false,
    /// and leaves the id alone, when the pixels do not fit the size.
    /// </summary>
    public static bool LoadTextureFromPixels(int textureId, int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0 || rgba == null || rgba.Length < width * height * 4)
        {
            Console.Error.WriteLine($"[fade] texture {textureId} was given {rgba?.Length ?? 0} bytes for {width} x {height} pixels");
            return false;
        }

        var texture = new Texture2D(GameSystem.game.GraphicsDevice, width, height, false, SurfaceFormat.Color);
        texture.SetData(rgba, 0, width * height * 4);

        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.descriptor = new TextureDescriptor();
        runtimeTex.SetComputedTexture(texture);
        textures[index] = runtimeTex;
        return true;
    }

    /// <summary>
    /// Make a texture out of an image file on disk (a .png, .jpg or .bmp), read while the game
    /// runs instead of built into the content. A relative path is relative to the folder the
    /// executable is in, not the working directory, so it finds files shipped next to the game.
    ///
    /// A missing file is not an error: the id is left as it was and this returns false, so a
    /// game can load its own texture first and then offer a file to replace it. A file that is
    /// there but cannot be read is logged, and also returns false.
    /// </summary>
    public static bool LoadTextureFromFile(int textureId, string path)
    {
#if BROWSER
        return false;
#else
        if (string.IsNullOrEmpty(path)) return false;
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(fullPath)) return false;

        Texture2D texture;
        try
        {
            using var stream = File.OpenRead(fullPath);
            texture = Texture2D.FromStream(GameSystem.game.GraphicsDevice, stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[fade] texture file could not be read: '{fullPath}': {ex.Message}");
            return false;
        }

        GetTextureIndex(textureId, out var index, out var runtimeTex);
        runtimeTex.descriptor = new TextureDescriptor { imageFilePath = fullPath };
        runtimeTex.SetComputedTexture(texture);
        textures[index] = runtimeTex;
        Console.WriteLine($"[fade] texture {textureId} loaded from file '{fullPath}' ({texture.Width} x {texture.Height})");
        return true;
#endif
    }

    public static void LoadSpriteFontFromContent(int fontId, string path)
    {
        var xnaFont = GameSystem.game.Content.Load<SpriteFontXna>(path);
        var fadeFont = SpriteFont.FromXnaFont(xnaFont);
        
        GetSpriteFontIndex(fontId, out var index, out var runtimeFont);
        runtimeFont.font = fadeFont;
        fonts[index] = runtimeFont;
    }
    
    
    // public static void LoadTexture(int textureId, string filePath)
    // {
    //     if (!filePath.EndsWith(".png"))
    //     {
    //         throw new InvalidOperationException("Only png textures are allowed");
    //     }
    //     
    //     var texture = Texture2D.FromFile(GameSystem.graphicsDeviceManager.GraphicsDevice, filePath);
    //     var descriptor = new TextureDescriptor
    //     {
    //         imageFilePath = filePath
    //     };
    //
    //     var metadataPath = Path.ChangeExtension(filePath, ".metadata.json");
    //     if (File.Exists(metadataPath))
    //     {
    //         var json = File.ReadAllText(metadataPath);
    //         descriptor = JsonSerializer.Deserialize<TextureDescriptor>(json, new JsonSerializerOptions
    //         {
    //             IncludeFields = true
    //         });
    //         descriptor.imageFilePath = filePath; // this ignores whatever is in the file. I guess that data is useless?
    //     }
    //
    //     GetTextureIndex(textureId, out var index, out var runtimeTex);
    //     runtimeTex.descriptor = descriptor;
    //     runtimeTex.texture = texture;
    //     textures[index] = runtimeTex;
    //
    // }
}