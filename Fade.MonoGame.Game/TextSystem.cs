using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Game;

public struct TextSprite
{
    public Sprite sprite;
    public string text;
}


public static class TextSystem
{
    public const int MAX_SPRITE_TEXT_COUNT = 10_000_000;

    public static TextSprite[] textSprites = new TextSprite[MAX_SPRITE_TEXT_COUNT];
    public static int textSpriteCount = 0;
    private static Dictionary<int, int> _textSpriteMap = new Dictionary<int, int>();
    
    
    public static void GetTextSpriteIndex(int textId, out int index, out TextSprite text)
    {
        if (!_textSpriteMap.TryGetValue(textId, out index))
        {
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
                    stageIdFlags = 1 // by default, put the sprite in the first stage
                }, 
            };
            RenderSystem.AddSpriteTextToStage(index, 1, 0);
            textSprites[index] = text;
            textSpriteCount++;
        }
        else
        {
            text = textSprites[index];
        }
    }
}