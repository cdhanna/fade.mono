using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;

namespace Fade.MonoGame.Game;

public struct Sprite
{
    public int id;
    public bool hidden;
    public int imageId;
    public int currentFrame;
    public Vector2 position;
    public Vector2 scale;
    public Vector2 origin;
    public SpriteTexCoord1 texCoord1;
    public float rotation;
    public Color color;
    public SpriteEffects effects;
    public int stageIdFlags;
    public int anchorTransformId;
    public int zOrder;
}

public static class SpriteSystem
{
    public const int MAX_SPRITE_COUNT = 10_000_000;

    public static Sprite[] sprites = new Sprite[MAX_SPRITE_COUNT];
    public static int spriteCount = 0;
    public static int highestSpriteId = 0;
    private static Dictionary<int, int> _spriteMap = new Dictionary<int, int>();

    
    public static void GetSpriteIndex(int spriteId, out int index, out Sprite sprite)
    {
        if (!_spriteMap.TryGetValue(spriteId, out index))
        {
            highestSpriteId = spriteId > highestSpriteId ? spriteId : highestSpriteId;
            
            index = _spriteMap[spriteId] = spriteCount;
            sprite = new Sprite
            {
                id = spriteId,
                color = Color.White,
                scale = Vector2.One,
                origin = Vector2.One * .5f,
                currentFrame = -1,
                stageIdFlags = 1 // by default, put the sprite in the first stage
            };
            RenderSystem.AddSpriteToStage(index, 1, 0);
            sprites[index] = sprite;
            spriteCount++;
        }
        else
        {
            sprite = sprites[index];
        }
    }

    public static int AddIdToFlags(int id, int flags)
    {
        return flags & id;
    }

    public static int RemoveIdFromFlags(int id, int flags)
    {
        return flags & ~id;
    }

    public static bool DoesFlagContainId(int id, int flags)
    {
        return (flags & id) != 0;
    }
    
    // public static void DrawSprites(SpriteBatch sb)
    // {
    //     // TODO: group into textures I suppose?
    //     
    //     
    //     for (var i = 0; i < spriteCount; i++)
    //     {
    //         var sprite = sprites[i];
    //         if (sprite.hidden) continue;
    //         
    //         TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);
    //         
    //         var tex = runtimeTex.texture;
    //         
    //         var src = new Rectangle(0, 0, tex.Width, tex.Height);
    //         if (sprite.currentFrame >= 0)
    //         {
    //             var frame = runtimeTex.descriptor.frames[sprite.currentFrame % runtimeTex.descriptor.frames.Count];
    //             src = new Rectangle(frame.xOffset, frame.yOffset, frame.xSize, frame.ySize);
    //         }
    //         
    //         sb.Draw(tex, sprite.position, src, sprite.color, sprite.rotation, new Vector2(src.Width * sprite.origin.X, src.Height * sprite.origin.Y), sprite.scale, sprite.effects, 0);
    //     }
    // }
}