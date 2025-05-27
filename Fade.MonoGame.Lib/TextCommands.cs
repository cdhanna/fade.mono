﻿using Fade.MonoGame.Game;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

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

    [FadeBasicCommand("set text stage")]
    public static void SetSpriteTextStage(int textId, int stageId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        RenderSystem.SetSpriteTextToStage(index, stageId, textSprite.sprite.stageIdFlags);
        textSprite.sprite.stageIdFlags = stageId;
        TextSystem.textSprites[index] = textSprite;

    }
    
    [FadeBasicCommand("reset text stage")]
    public static void ResetSpriteTextStage(int textId)
    {
        SetSpriteTextStage(textId, 0);
    }
    
    [FadeBasicCommand("add text stage")]
    public static void AddSpriteTextStage(int textId, int stageId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);

        RenderSystem.AddSpriteTextToStage(index, stageId, textSprite.sprite.stageIdFlags);
        textSprite.sprite.stageIdFlags = SpriteSystem.AddIdToFlags(stageId, textSprite.sprite.stageIdFlags);
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
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        textSprite.sprite.scale = new Vector2(xRatio, xRatio);
        TextSystem.textSprites[index] = textSprite;

    }
    [FadeBasicCommand("size text y")]
    public static void SizeSpriteTextAspectY(int textId, float yPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
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
}