using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core;

// The struct must hold no references (no string, no class). An array of structs with a reference
// in them is one the .NET GC reads end to end, every slot used or not, on every full collection:
// when the text array was 10 million long, that was about 300 ms a time on an M1 Mac. The
// string and the layout live in TextSystem's lists instead, and the struct only says which entry
// is its own.
public struct TextSprite
{
    public Sprite sprite;

    // One more than this text's entry in TextSystem.texts and TextSystem.layouts, so that a
    // default TextSprite (0) has none.
    public int slot;

    public string text
    {
        readonly get => slot == 0 ? null : TextSystem.texts[slot - 1];
        set => TextSystem.texts[slot - 1] = value;
    }

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
    public RichTextLayout layout
    {
        readonly get => slot == 0 ? null : TextSystem.layouts[slot - 1];
        set => TextSystem.layouts[slot - 1] = value;
    }

    // A typewriter. When revealOn is set, only the first `reveal` characters of the text are drawn,
    // counting an icon as one. The whole text is still laid out, so nothing moves as more of it
    // shows. Only text that goes through a layout (rich, or wrapped) can be revealed.
    public bool revealOn;
    public int reveal;

    // The last `size text...` call, so it can be done again when a loc text's string changes (a new
    // language, a new arg). None after `scale text`, which sets the scale by hand. See TextSizing.
    public TextSizeMode sizeMode;
    public float sizeX;
    public float sizeY;
    public float sizeMin;
    public float sizeMax;
}


public static class TextSystem
{
    // Room for 16,384 up front, which no game so far has needed more of (the fish game peaks near
    // 4,000 sprites), and the array doubles if it fills. It used to be 10 million: about 6 GB
    // committed across the systems, which kept a Mac's memory load near 98% and the .NET GC
    // collecting over and over.
    public const int INITIAL_SPRITE_TEXT_COUNT = 16_384;
    public static TextSprite[] textSprites = new TextSprite[INITIAL_SPRITE_TEXT_COUNT];
    public static int textSpriteCount = 0;
    private static Dictionary<int, int> _textSpriteMap = new Dictionary<int, int>();
    public static int highestTextId = 0;

    // The string and layout of each text, by its index. See TextSprite.slot.
    public static readonly List<string> texts = new List<string>();
    public static readonly List<RichTextLayout> layouts = new List<RichTextLayout>();

    // Icons that one text draws bigger or smaller than its font says, by the text's index, and then
    // by the font's icon id. Only a handful of texts ever have one, so it is kept out here instead of
    // on TextSprite. It is applied when the text is drawn, so the line is never laid out again for it.
    public static readonly Dictionary<int, Dictionary<int, float>> iconScales = new Dictionary<int, Dictionary<int, float>>();


    public static void Reset()
    {
        //textSprites = new TextSprite[INITIAL_SPRITE_TEXT_COUNT];
        textSpriteCount = 0;
        highestTextId = 0;
        _textSpriteMap.Clear();
        texts.Clear();
        layouts.Clear();
        iconScales.Clear();
    }
    
    public static void GetTextSpriteIndex(int textId, out int index, out TextSprite text)
    {
        if (!_textSpriteMap.TryGetValue(textId, out index))
        {
            highestTextId = textId > highestTextId ? textId : highestTextId;

            if (textSpriteCount == textSprites.Length) Array.Resize(ref textSprites, textSprites.Length * 2);
            index = _textSpriteMap[textId] = textSpriteCount;
            texts.Add(null);
            layouts.Add(null);
            text = new TextSprite
            {
                slot = index + 1,
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