using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Game;

public struct Sprite
{
    public int id;
    public bool hidden;
    public int imageId;
    public int currentFrame;
    public Vector2 position;
    public Vector2 scale;
    public float rotation;
    public Color color;
    public SpriteEffects effects;
}

public static class SpriteSystem
{
    public static List<Sprite> sprites = new List<Sprite>();
    private static Dictionary<int, int> _spriteMap = new Dictionary<int, int>();
    
    
    public static void GetSpriteIndex(int spriteId, out int index, out Sprite sprite)
    {
        if (!_spriteMap.TryGetValue(spriteId, out index))
        {
            index = _spriteMap[spriteId] = sprites.Count;
            sprite = new Sprite
            {
                id = spriteId,
                color = Color.White,
                scale = Vector2.One,
                currentFrame = -1
            };
            sprites.Add(sprite);
        }
        else
        {
            sprite = sprites[index];
        }
    }
    
    public static void DrawSprites(SpriteBatch sb)
    {
        // TODO: group into textures I suppose?
        
        
        for (var i = 0; i < sprites.Count; i++)
        {
            var sprite = sprites[i];
            if (sprite.hidden) continue;
            
            TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);
            
            var tex = runtimeTex.texture;
            
            var src = new Rectangle(0, 0, tex.Width, tex.Height);
            if (sprite.currentFrame >= 0)
            {
                var frame = runtimeTex.descriptor.frames[sprite.currentFrame % runtimeTex.descriptor.frames.Count];
                src = new Rectangle(frame.xOffset, frame.yOffset, frame.xSize, frame.ySize);
            }
            sb.Draw(tex, sprite.position, src, sprite.color, sprite.rotation, new Vector2(src.Width/2f, src.Height/2f), sprite.scale, sprite.effects, 0);
        }
    }
}