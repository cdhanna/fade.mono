using Fade.MonoGame.Game;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    
    [FadeBasicCommand("free sprite id")]
    public static int GetFreeSpriteNextId(ref int spriteId)
    {
        spriteId = SpriteSystem.highestSpriteId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return spriteId;
    }
    
    [FadeBasicCommand("reserve sprite id")]
    public static int ReserveSpriteNextId(ref int spriteId)
    {
        GetFreeSpriteNextId(ref spriteId);
        SpriteSystem.GetSpriteIndex(spriteId, out _, out _);
        return spriteId;
    }

    
    
    [FadeBasicCommand("sprite")]
    public static void Sprite(int spriteId, float x, float y, int textureId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.position = new Vector2(x, y);
        sprite.imageId = textureId;
        sprite.hidden = false;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("position sprite")]
    public static void PositionSprite(int spriteId, float x, float y)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out _);
        SpriteSystem.sprites[index].position = new Vector2(x, y);
    }
    
    [FadeBasicCommand("color sprite")]
    public static void ColorSprite(int spriteId, int packedColor)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        ColorUtil.UnpackColor(packedColor, out var r, out var g, out var b, out var a);
        sprite.color = new Color(r, g, b, a);
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("order sprite")]
    public static void OrderSprite(int spriteId, int order)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.zOrder = order;
        SpriteSystem.sprites[index] = sprite;
    }

    
    [FadeBasicCommand("hide sprite")]
    public static void HideSprite(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.hidden = true;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("show sprite")]
    public static void ShowSprite(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.hidden = false;
        SpriteSystem.sprites[index] = sprite;
    }


    [FadeBasicCommand("set sprite texture")]
    public static void SetSpriteTexture(int spriteId, int textureId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        SpriteSystem.sprites[index].imageId = textureId;
    }
    
    [FadeBasicCommand("set sprite stage")]
    public static void SetSpriteStage(int spriteId, int stageId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        
        RenderSystem.SetSpriteToStage(index, stageId, sprite.stageIdFlags);
        sprite.stageIdFlags = stageId;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("reset sprite stage")]
    public static void ResetSpriteStage(int spriteId)
    {
        SetSpriteStage(spriteId, 0);
    }
    
    [FadeBasicCommand("add sprite stage")]
    public static void AddSpriteStage(int spriteId, int stageId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        RenderSystem.AddSpriteToStage(index, stageId, sprite.stageIdFlags);
        sprite.stageIdFlags = SpriteSystem.AddIdToFlags(stageId, sprite.stageIdFlags);
        SpriteSystem.sprites[index] = sprite;
    }

    
    [FadeBasicCommand("scale sprite")]
    public static void ScaleSprite(int spriteId, float x, float y)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.scale = new Vector2(x, y);
        SpriteSystem.sprites[index] = sprite;
    }

    [FadeBasicCommand("attach sprite to transform")]
    public static void SetSpriteRelativeToAnother(int spriteId, int transformId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.anchorTransformId = transformId;
        SpriteSystem.sprites[index] = sprite;
    }
    
    /// <summary>
    /// given the size of the texture, work out the math so that the sprite takes up the amount of screen space given. 
    /// </summary>
    /// <param name="spriteId"></param>
    /// <param name="xPixels"></param>
    /// <param name="yPixels"></param>
    [FadeBasicCommand("size sprite")]
    public static void SizeSprite(int spriteId, float xPixels, float yPixels)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        var xRatio = xPixels / src.Width;
        var yRatio = yPixels / src.Height;

        sprite.scale = new Vector2(xRatio, yRatio);
        
        SpriteSystem.sprites[index] = sprite;
    }

    [FadeBasicCommand("size sprite x")]
    public static void SizeSpriteAspectX(int spriteId, float xPixels)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        var xRatio = xPixels / src.Width;
        sprite.scale = new Vector2(xRatio, xRatio);
        
        SpriteSystem.sprites[index] = sprite;
    }
    [FadeBasicCommand("size sprite y")]
    public static void SizeSpriteAspectY(int spriteId, float yPixels)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        var yRatio = yPixels / src.Height;

        sprite.scale = new Vector2(yRatio, yRatio);
        
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("rotate sprite")]
    public static void RotateSprite(int spriteId, float angle)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.rotation = angle;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("set sprite offset")]
    public static void SetSpriteOffset(int spriteId, float xRatio, float yRatio)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.origin = new Vector2(xRatio, yRatio);
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("set sprite all texcoord1")]
    public static void SetSpriteTexcoord1(int spriteId, float x, float y, float z, float w)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.texCoord1 = new SpriteTexCoord1(new Vector4(x, y, z, w));
        SpriteSystem.sprites[index] = sprite;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="spriteId"></param>
    /// <param name="cornerIndex">
    /// 0 = top left
    /// 1 = top right
    /// 2 = bottom left
    /// 3 = bottom right
    /// </param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="w"></param>
    [FadeBasicCommand("set sprite index texcoord1")]
    public static void SetSpriteTexcoord1(int spriteId, int cornerIndex, float x, float y, float z, float w)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        switch (cornerIndex)
        {
            case 0:
                sprite.texCoord1.tl = new Vector4(x, y, z, w);
                break;
            case 1:
                sprite.texCoord1.tr = new Vector4(x, y, z, w);
                break;
            case 2:
                sprite.texCoord1.bl = new Vector4(x, y, z, w);
                break;
            case 3:
                sprite.texCoord1.br = new Vector4(x, y, z, w);
                break;
        }
        SpriteSystem.sprites[index] = sprite;
    }
    

    [FadeBasicCommand("set sprite diffuse")]
    public static void SetSpriteDiffuse(int spriteId, byte red, byte green, byte blue)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.color.R = red;
        sprite.color.G = green;
        sprite.color.B = blue;
        SpriteSystem.sprites[index] = sprite;
    }
    
    // TODO: add a "set sprite size" option that sets the effect width/height 
    
    [FadeBasicCommand("set sprite alpha")]
    public static void SetSpriteDiffuse(int spriteId, byte alpha)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.color.A = alpha;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("set sprite frame")]
    public static void SetSpriteFrame(int spriteId, int frameId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.currentFrame = frameId;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("set sprite flip")]
    public static void Flip(int spriteId, int flipHorizontal, int flipVertical)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        switch (flipHorizontal + flipVertical * 2)
        {
            case 0:
                sprite.effects = SpriteEffects.None;
                break;
            case 1:
                sprite.effects = SpriteEffects.FlipHorizontally;
                break;
            case 2:
                sprite.effects = SpriteEffects.FlipVertically;
                break;
            case 3:
                sprite.effects = SpriteEffects.FlipVertically | SpriteEffects.FlipVertically;
                break;
        }
        SpriteSystem.sprites[index] = sprite;

    }
    
    [FadeBasicCommand("sprite width")]
    public static float GetSpriteWidth(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        return src.Width;
    }
    [FadeBasicCommand("sprite height")]
    public static float GetSpriteHeight(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        return src.Height;
    }

    
    [FadeBasicCommand("sprite x")]
    public static float SpriteX(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        return sprite.position.X;
    }
    
    [FadeBasicCommand("sprite y")]
    public static float SpriteY(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        return sprite.position.Y;
    }
}