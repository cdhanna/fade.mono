using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core;

public struct TextSprite
{
    public Sprite sprite;
    public string text;

    public Color dropShadowColor;
    public Vector2 dropShadowOffset;
    public bool dropShadowEnabled;

    // Rich text draws the font's icons wherever the text names one in braces.
    public bool richText;

    // The width, in pixels on screen, that the text wraps at. Zero means that it does not wrap.
    public float wrapWidth;

    // Where each line sits inside the block when the lines are different widths. 0 is left,
    // .5 is centered, and 1 is right.
    public float align;

    // The cached layout, for text that is rich or that wraps. Go through RichTextSystem to read
    // it, because it is only rebuilt on demand.
    public RichTextLayout layout;
}


public static class TextSystem
{
#if BROWSER
    // WASM heap is bounded; 10M-element arrays of struct types blow it
    // past the 2GB cap before user code ever runs. Cap browser builds at
    // 100k — still vastly more sprites than any real game needs, and
    // expandable per-project later via a build property.
    public const int MAX_SPRITE_TEXT_COUNT = 100_000;
#else
    public const int MAX_SPRITE_TEXT_COUNT = 10_000_000;
#endif

    public static TextSprite[] textSprites = new TextSprite[MAX_SPRITE_TEXT_COUNT];
    public static int textSpriteCount = 0;
    private static Dictionary<int, int> _textSpriteMap = new Dictionary<int, int>();
    public static int highestTextId = 0;

    
    public static void Reset()
    {
        //textSprites = new TextSprite[MAX_SPRITE_TEXT_COUNT];
        textSpriteCount = 0;
        highestTextId = 0;
        _textSpriteMap.Clear();
    }
    
    public static void GetTextSpriteIndex(int textId, out int index, out TextSprite text)
    {
        if (!_textSpriteMap.TryGetValue(textId, out index))
        {
            highestTextId = textId > highestTextId ? textId : highestTextId;

            index = _textSpriteMap[textId] = textSpriteCount;
            text = new TextSprite
            {
                sprite = new Sprite
                {
                    id = textId,
                    color = Color.White,
                    scale = Vector2.One,
                    origin = Vector2.One * .5f,
                    currentFrame = -1,
                    outputIdFlags = 1,
                    stageIdFlags = 1 // by default, put the sprite in the first stage
                    
                }, 
            };
            
            RenderSystem.AddSpriteTextToOutput(index, 1, 0);
            textSprites[index] = text;
            textSpriteCount++;
        }
        else
        {
            text = textSprites[index];
        }
    }
}