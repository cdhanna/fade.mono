using System.Diagnostics;
using Fade.MonoGame.Game;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    [FadeBasicCommand("free text id")]
    public static int GetFreeTextNextId(ref int textId)
    {
        textId = TextSystem.highestTextId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return textId;
    }
    
    [FadeBasicCommand("reserve text id")]
    public static int ReserveTextNextId(ref int textId)
    {
        GetFreeTextNextId(ref textId);
        TextSystem.GetTextSpriteIndex(textId, out _, out _);
        return textId;
    }

    
    [FadeBasicCommand("text")]
    public static void Text(int textId, int x, int y, int spriteFontId, string text)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.position = new Vector2(x, y);
        textSprite.sprite.imageId = spriteFontId;
        textSprite.text = text;
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("set text")]
    public static void SetText(int textId, string text)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.text = text;
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("set text position")]
    public static void SetTextPosition(int textId, int x, int y)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.position = new Vector2(x, y);
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("color text")]
    public static void SetTextColor(int textId, int colorCode)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        textSprite.sprite.color = new Color(r, g, b, a);
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("color text drop shadow")]
    public static void SetTextDropShadowColor(int textId, int colorCode)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        textSprite.dropShadowColor = new Color(r, g, b, a);
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("enable text drop shadow")]
    public static void EnableTextDropShadow(int textId, int x, int y, int colorCode)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        textSprite.dropShadowEnabled = true;
        textSprite.dropShadowOffset = new Vector2(x, y);
        textSprite.dropShadowColor = new Color(r, g, b, a);
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("disable text drop shadow")]
    public static void DisableTextDropShadow(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.dropShadowEnabled = false;
        TextSystem.textSprites[index] = textSprite;
    }

    
    [FadeBasicCommand("set text alpha")]
    public static void SetTextDiffuse(int textId, byte alpha)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.color.A = alpha;
        TextSystem.textSprites[index] = textSprite;
    }

    
    [FadeBasicCommand("scale text")]
    public static void SetTextScale(int textId, float x, float y)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.scale = new Vector2(x, y);
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("order text")]
    public static void SetTextOrder(int textId, int order)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.zOrder = order;
        TextSystem.textSprites[index] = textSprite;
        RenderSystem.GetOutputIndex(textSprite.sprite.outputIdFlags, out _, out var output);
        output.spritesOrderDirty = true;
    }
    
    
    [FadeBasicCommand("hide text")]
    public static void HideSpriteText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.hidden = true;
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("show text")]
    public static void ShowpriteText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.hidden = false;
        TextSystem.textSprites[index] = textSprite;
    }

    [FadeBasicCommand("set text render target")]
    public static void SetSpriteTextRenderTarget(int textId, int outputId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        RenderSystem.SetSpriteTextToOutput(textId, outputId, textSprite.sprite.outputIdFlags);
        textSprite.sprite.outputIdFlags = outputId;
        TextSystem.textSprites[index] = textSprite;
    }
    [FadeBasicCommand("reset text render target")]
    public static void ResetSpriteTextRenderTarget(int textId)
    {
        SetSpriteTextRenderTarget(textId, 1);
    }
    [FadeBasicCommand("add text render target")]
    public static void AddSpriteTextRenderTarget(int textId, int outputId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);

        RenderSystem.AddSpriteTextToOutput(index, outputId, textSprite.sprite.outputIdFlags);
        textSprite.sprite.outputIdFlags = SpriteSystem.AddIdToFlags(outputId, textSprite.sprite.outputIdFlags);
        TextSystem.textSprites[index] = textSprite;

    }
    
    
    [FadeBasicCommand("size text")]
    public static void SizeText(int textId, float xPixels, float yPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);

        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        var yRatio = yPixels / size.Y;

        textSprite.sprite.scale = new Vector2(xRatio, yRatio);
        TextSystem.textSprites[index] = textSprite;
    }
    
    
    [FadeBasicCommand("size text x")]
    public static void SizeSpriteTextAspectX(int textId, float xPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text x {textId}, {xPixels}` has no effect, because text has no font yet");
            return;
        }
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        textSprite.sprite.scale = new Vector2(xRatio, xRatio);
        TextSystem.textSprites[index] = textSprite;

    }
    
    [FadeBasicCommand("size text x")]
    public static void SizeSpriteTextAspectX(int textId, float xPixels, float min, float max)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text x {textId}, {xPixels}` has no effect, because text has no font yet");
            return;
        }
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        xRatio = Math.Clamp(xRatio, min, max);
        
        textSprite.sprite.scale = new Vector2(xRatio, xRatio);
        TextSystem.textSprites[index] = textSprite;

    }
    
    
    [FadeBasicCommand("size text y")]
    public static void SizeSpriteTextAspectY(int textId, float yPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text y {textId}, {yPixels}` has no effect, because text has no font yet");
            return;
        }
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var yRatio = yPixels / size.Y;
        textSprite.sprite.scale = new Vector2(yRatio, yRatio);
        TextSystem.textSprites[index] = textSprite;
    }
    
    
    [FadeBasicCommand("attach text to transform")]
    public static void SetSpriteTextRelativeToAnother(int textId, int transformId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.anchorTransformId = transformId;
        TextSystem.textSprites[index] = textSprite;
    }
    
    [FadeBasicCommand("rotate text")]
    public static void RotateSpriteText(int textId, float angle)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var text);
        text.sprite.rotation = angle;
        TextSystem.textSprites[index] = text;
    }
    
    [FadeBasicCommand("set text offset")]
    public static void SetSpriteTextOffset(int textId, float xRatio, float yRatio)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var text);
        text.sprite.origin = new Vector2(xRatio, yRatio);
        TextSystem.textSprites[index] = text;
    }
    
    [FadeBasicCommand("text x")]
    public static float TextX(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var sprite);
        return sprite.sprite.position.X;
    }
    
    [FadeBasicCommand("text y")]
    public static float TextY(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var sprite);
        return sprite.sprite.position.Y;
    }
}