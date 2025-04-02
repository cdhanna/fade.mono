using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    
    [FadeBasicCommand("sprite")]
    public static void Sprite(int spriteId, float x, float y, int textureId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.position = new Vector2(x, y);
        sprite.imageId = textureId;
        sprite.hidden = false;
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("hide sprite")]
    public static void HideSprite(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.hidden = true;
        SpriteSystem.sprites[index] = sprite;
    }

    
    [FadeBasicCommand("scale sprite")]
    public static void ScaleSprite(int spriteId, float x, float y)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.scale = new Vector2(x, y);
        SpriteSystem.sprites[index] = sprite;
    }
    
    [FadeBasicCommand("rotate sprite")]
    public static void RotateSprite(int spriteId, float angle)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.rotation = angle;
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