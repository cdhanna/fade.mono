using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core;

// The struct must hold no references (no string, no class). The text array is 10 million long,
// and an array of structs with a reference in them is one the .NET GC reads end to end on every
// full collection: about 300 ms a time on an M1 Mac. The string and the layout live in
// TextSystem's lists instead, and the struct only says which entry is its own.
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

    // The string and layout of each text, by its index. See TextSprite.slot.
    public static readonly List<string> texts = new List<string>();
    public static readonly List<RichTextLayout> layouts = new List<RichTextLayout>();


    public static void Reset()
    {
        //textSprites = new TextSprite[MAX_SPRITE_TEXT_COUNT];
        textSpriteCount = 0;
        highestTextId = 0;
        _textSpriteMap.Clear();
        texts.Clear();
        layouts.Clear();
    }
    
    public static void GetTextSpriteIndex(int textId, out int index, out TextSprite text)
    {
        if (!_textSpriteMap.TryGetValue(textId, out index))
        {
            highestTextId = textId > highestTextId ? textId : highestTextId;

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