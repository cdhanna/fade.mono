using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.Fade.SpriteBatch;

namespace Fade.MonoGame.Core;


public class RenderOutput
{
    public int id;
    public int order;
    
    public RenderTarget2D target;
    public int targetTextureId; // is there a reserved texture id? 
    public Color clearColor;
    public bool clearTarget;

    public bool spritesOrderDirty;
    // public List<int> orderedSpriteIds = new List<int>();
    // public List<int> orderedTextIds = new List<int>();
    public List<RenderOutputItem> orderedItems = new List<RenderOutputItem>();
}

public static class RenderOutputItemExtensions
{
    public static void RemoveSprite(this List<RenderOutputItem> self, int spriteIndex)
    {
        for (var i = self.Count - 1; i >= 0; i--)
        {
            if (self[i].type == RenderOutputItem.TYPE_SPRITE && self[i].index == spriteIndex)
            {
                self.RemoveAt(i);
            }
        }  
    } 
    public static void RemoveText(this List<RenderOutputItem> self, int textIndex)
    {
        for (var i = self.Count - 1; i >= 0; i--)
        {
            if (self[i].type == RenderOutputItem.TYPE_TEXT && self[i].index == textIndex)
            {
                self.RemoveAt(i);
            }
        }  
    } 
    public static void AddSprite(this List<RenderOutputItem> self, int spriteIndex) => self.Add(new RenderOutputItem
    {
        index = spriteIndex, type = RenderOutputItem.TYPE_SPRITE
    });
    public static void AddText(this List<RenderOutputItem> self, int textIndex) => self.Add(new RenderOutputItem
    {
        index = textIndex, type = RenderOutputItem.TYPE_TEXT
    });
}

[DebuggerDisplay("{(type == 1 ? \"SPRITE\" : \"TEXT\")} - {index}")]
public struct RenderOutputItem
{
    public const byte TYPE_SPRITE = 1;
    public const byte TYPE_TEXT = 2;
    
    public int index;
    public byte type;
}


public struct RuntimeEffect
{
    public int id;
    public WatchedAsset<Effect> watchedEffect;
    public Effect effect => watchedEffect.Asset;
    public string filePath;
    // TODO: could I somehow recompile on file change?
}


public static class RenderSystem
{
    public static Color backgroundColor = Color.CornflowerBlue;

    public static RenderTarget2D mainBuffer;
    public static Vector2 mainBufferPosition;
    public static float mainBufferScale;
    
    public static Vector2 screenShakeOffset;
    public static Vector2 screenShakeOffsetTarget;
    public static float screenShakeMag, screenShakeElastic;

    public static List<RenderOutput> outputs = new List<RenderOutput>();
    private static Dictionary<int, int> _outputMap = new Dictionary<int, int>();

    public static List<RuntimeEffect> effects = new List<RuntimeEffect>();
    private static Dictionary<int, int> _effectMap = new Dictionary<int, int>();

    public static int screenEffectIndex = -1;
    public static int highestEffectId;
    public static int highestOutputId = 1;
    
    
    public static void Reset()
    {
        backgroundColor = Color.CornflowerBlue;
        mainBuffer = null;
        mainBufferPosition = default;
        mainBufferScale = default;
        screenShakeOffset = default;
        screenShakeOffsetTarget = default;
        screenShakeMag = default;
        screenShakeElastic = default;
        effects.Clear();
        _effectMap.Clear();
        outputs.Clear();
        _outputMap.Clear();
        screenEffectIndex = -1;
        highestEffectId = 0;
        highestOutputId = 1;
    }
    
    public static void GetEffectIndex(int effectId, out int index, out RuntimeEffect effect)
    {
        if (!_effectMap.TryGetValue(effectId, out index))
        {
            highestEffectId = effectId > highestEffectId 
                ? effectId 
                : highestEffectId;
            
            index = _effectMap[effectId] = effects.Count;
            effect = new RuntimeEffect
            {
                id = effectId,
            };
            effects.Add(effect);
        }
        else
        {
            effect = effects[index];
        }
    }
    
    
    public static void GetOutputIndex(int outputId, out int index, out RenderOutput output)
    {
        if (outputId > 63) throw new ArgumentException("the outputId must be less than 63. Sprites use a single int with 64 bits to track which outputs it is rendering on.");
        if (outputId < 1) throw new ArgumentException("outputId must be one or greater. 1 is the default");
        
        if (!_outputMap.TryGetValue(outputId, out index))
        {
            index = _outputMap[outputId] = outputs.Count;
            highestOutputId = outputId > highestOutputId ? outputId : highestOutputId;
            
            output = new RenderOutput
            {
                id = outputId,
                target = null, // default to drawing to the screen
                targetTextureId = -1,
                clearTarget = true,
                clearColor = Color.Black,
                orderedItems = new List<RenderOutputItem>(),
                spritesOrderDirty = true
            };
            outputs.Add(output);
        }
        else
        {
            output = outputs[index];
        }
    }

    public static void Test()
    {
        Effect e = null;
        var p = e.Parameters["a"];
        var a = p.Annotations["b"];
    }


    public static void AddSpriteToOutput(int spriteIndex, int outputId, int existingFlags)
    {
        if (SpriteSystem.DoesFlagContainId(outputId, existingFlags))
        {
            // the stage already knows about this sprite, and if we do it again, the counts will be incorrect.
            return;
        }
        
        GetOutputIndex(outputId, out var index, out var output);
        output.orderedItems.AddSprite(spriteIndex);
        output.spritesOrderDirty = true; // mark this as dirty, so that the list is ordered before the next draw. 
    }
    
    public static void SetSpriteToOutput(int spriteIndex, int outputId, int existingFlags)
    {
        { // remove the sprite from any stages it may be a part of.
            for (var s = 0; s < outputs.Count; s++)
            {
                var id = outputs[s].id;
                if (SpriteSystem.DoesFlagContainId(id, existingFlags))
                {
                    outputs[s].orderedItems.RemoveSprite(spriteIndex); 
                }
            }
        }
        AddSpriteToOutput(spriteIndex, outputId, 0); // at this point, the sprite is not in any stages. We just removed them all!!
    }
    

    
    public static void AddSpriteTextToOutput(int spriteTextIndex, int outputId, int existingFlags)
    {
        if (SpriteSystem.DoesFlagContainId(outputId, existingFlags))
        {
            // the stage already knows about this sprite, and if we do it again, the counts will be incorrect.
            return;
        }
        
        GetOutputIndex(outputId, out var index, out var output);
        output.orderedItems.AddText(spriteTextIndex);
        output.spritesOrderDirty = true;
    }
    public static void SetSpriteTextToOutput(int spriteTextIndex, int outputId, int existingFlags)
    {
        { // remove the sprite from any stages it may be a part of.
            for (var s = 0; s < outputs.Count; s++)
            {
                var id = outputs[s].id;
                if (SpriteSystem.DoesFlagContainId(id, existingFlags))
                {
                    outputs[s].orderedItems.RemoveText(spriteTextIndex); // TODO: this is an expensive operation :( 
                }
            }
        }
        AddSpriteTextToOutput(spriteTextIndex, outputId, 0); // at this point, the sprite is not in any stages. We just removed them all!!
    }
    
    public static void SetMainRenderSize(int width, int height)
    {
        mainBuffer = new RenderTarget2D(GameSystem.graphicsDeviceManager.GraphicsDevice, width, height);
        ResetRenderPositioning();
    }

    public static void ResetRenderPositioning()
    {
        GetLetterboxTransform(
            GameSystem.graphicsDeviceManager.PreferredBackBufferWidth,
            GameSystem.graphicsDeviceManager.PreferredBackBufferHeight,
            mainBuffer.Width, mainBuffer.Height, out mainBufferPosition, out mainBufferScale);
    }
    
    public static void GetLetterboxTransform(
        int screenWidth, int screenHeight,
        int renderTargetWidth, int renderTargetHeight,
        out Vector2 position, out float scale)
    {
        // Compute scale factors to fit render target into screen
        float scaleX = screenWidth / (float)renderTargetWidth;
        float scaleY = screenHeight / (float)renderTargetHeight;

        // Use the smaller scale to ensure the render target fits
        scale = MathF.Min(scaleX, scaleY);

        // Compute the size of the scaled render target
        float displayWidth = renderTargetWidth * scale;
        float displayHeight = renderTargetHeight * scale;

        // Center the render target on screen
        float offsetX = (screenWidth - displayWidth) / 2f;
        float offsetY = (screenHeight - displayHeight) / 2f;

        position = new Vector2(offsetX, offsetY);
    }
    
    // Hashing function to generate pseudo-random values
    private static double Hash(int x)
    {
        x = (x << 13) ^ x;
        return (1.0 - ((x * (x * x * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0);
    }

    public static void RefreshEffects(FadeSpriteEffect fadeFx)
    {
        for (var i = 0 ; i < effects.Count; i ++)
        {
            var fx = effects[i];
            if (GameSystem.game.ContentWatcher.TryRefreshAsset(ref fx.watchedEffect))
            {
                effects[i] = fx;
            }
            
        }

        foreach (var fx in effects)
        {
            
            if (fx.effect.Parameters.ContainsParameter("Time"))
                fx.effect.Parameters["Time"].SetValue((float)GameSystem.latestTime.TotalGameTime.TotalSeconds);
            
            if (fx.effect.Parameters.ContainsParameter("Resolution"))
                fx.effect.Parameters["Resolution"].SetValue(new Vector2(mainBuffer.Width, mainBuffer.Height));
            
        }
    }


    public static bool flip = false;
    public static void RenderAll2(SpriteBatch sb)
    {
        var localOutputs = outputs.ToList().OrderBy(x => x.order).ToList();

        for (var i = 0; i < localOutputs.Count; i++)
        {
            var output = localOutputs[i];
            
            // initialize the output.
            var targetBuffer = output.target;
            if (targetBuffer == null)
            {
                targetBuffer = mainBuffer;
            }
            sb.GraphicsDevice.SetRenderTarget(targetBuffer);
            if (output.clearTarget)
            {
                sb.GraphicsDevice.Clear(output.clearColor);
            }
            
            // do we need to order the sprites?
            if (output.spritesOrderDirty)
            {
                output.spritesOrderDirty = false;
                output.orderedItems = output.orderedItems.OrderBy(x =>
                {
                    switch (x.type)
                    {
                        case RenderOutputItem.TYPE_SPRITE:
                            return SpriteSystem.sprites[x.index].zOrder;
                        case RenderOutputItem.TYPE_TEXT:
                            return TextSystem.textSprites[x.index].sprite.zOrder;
                        default:
                            throw new InvalidOperationException("Invalid render item type.");
                    }
                }).ToList();
            }

            // draw all the sprites in this output...
            var hasBatch = false;
            var needBatch = true;
            
            Effect effect = null;

            // foreach (var spriteIndex in output.orderedSpriteIds)
            Sprite sprite = default;
            TextSprite text = default;
            for (var j = 0; j < output.orderedItems.Count; j ++)
            {
                var item = output.orderedItems[j];
                switch (item.type)
                {
                    case RenderOutputItem.TYPE_SPRITE:
                        sprite = SpriteSystem.sprites[item.index];
                        text = default;
                        break;
                    case RenderOutputItem.TYPE_TEXT:
                        text = TextSystem.textSprites[item.index];
                        sprite = text.sprite;
                        break;
                }
                
                // var spriteIndex = output.orderedSpriteIds[j];
                // var sprite = SpriteSystem.sprites[spriteIndex];

                Effect spriteEffect = default; // start by assuming the sprite has no effect.
                if (_effectMap.TryGetValue(sprite.effectId, out var effectIndex))
                {
                    spriteEffect = effects[effectIndex].effect;
                }

                if (spriteEffect != effect)
                {
                    // we need to create a new batch!
                    needBatch = true;
                    effect = spriteEffect;
                }

                if (needBatch)
                {
                    if (hasBatch)
                    {
                        // need to end the old batch.
                        sb.End();
                    }
                    
                    sb.Begin(
                        sortMode: SpriteSortMode.BackToFront, 
                        blendState: BlendState.NonPremultiplied, // TODO: allow sprites to set their own blend mode. 
                        samplerState: SamplerState.PointClamp, // TODO: allow sprites to set their own sampler state
                        effect: effect);
                    hasBatch = true;
                    needBatch = false;
                }

                switch (item.type)
                {
                    case RenderOutputItem.TYPE_TEXT:
                    {

                        if (text.sprite.hidden) continue;

                        TextureSystem.GetSpriteFontIndex(text.sprite.imageId, out _, out var runtimeFont);
                        var font = runtimeFont.font;

                        // cannot render text without a default font.
                        if (font == null) continue;

                        var size = font.MeasureString(text.text);
                        var origin = new Vector2(size.X * text.sprite.origin.X, size.Y * text.sprite.origin.Y);

                        var position = text.sprite.position;
                        var angle = text.sprite.rotation;
                        var scale = text.sprite.scale;


                        if (text.sprite.anchorTransformId > 0)
                        {
                            var localMat = TransformSystem.CreateMatrix(position, angle, scale);

                            TransformSystem.GetTransformIndex(text.sprite.anchorTransformId, out _, out var transform);
                            var mat = transform.computedWorld;
                            mat = localMat * mat;

                            TransformSystem.DecomposeMatrix(mat, out var matPos, out var matRot, out var matScale);
                            position.X = matPos.X;
                            position.Y = matPos.Y;
                            angle = matRot.Z;
                            scale.X = matScale.X;
                            scale.Y = matScale.Y;
                        }

                        // var order = 1 - ((text.sprite.zOrder / 200f) + (text.sprite.id / 500f));
                        var order = 1 - ((sprite.zOrder / 500f) +
                                         .001f * (sprite.id /
                                                  500f)); //TODO: Why doesn't deferred rendering work here????
                        sb.DrawString(font, text.text, position, text.sprite.color, angle, origin, scale,
                            text.sprite.effects, order);

                        if (text.dropShadowEnabled)
                        {
                            position -= text.dropShadowOffset;
                            // order += (2f / 200f);
                            order += .001f;
                            var color = text.dropShadowColor;
                            if (text.sprite.color.A <= 1)
                            {
                            }

                            color.A = text.sprite.color.A;

                            sb.DrawString(font, text.text, position, color, angle, origin, scale, text.sprite.effects,
                                order);

                        }



                        break;
                    }
                    case RenderOutputItem.TYPE_SPRITE:
                    {
                        if (sprite.hidden) continue;

                        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);

                        var tex = runtimeTex.texture;

                        var src = TextureSystem.GetSourceRect(ref runtimeTex, ref sprite);
                        var origin = new Vector2(src.Width * sprite.origin.X, src.Height * sprite.origin.Y);

                        var position = sprite.position;
                        var angle = sprite.rotation;
                        var scale = sprite.scale;


                        if (sprite.anchorTransformId > 0)
                        {
                            var localMat = TransformSystem.CreateMatrix(position, angle, scale);

                            TransformSystem.GetTransformIndex(sprite.anchorTransformId, out _, out var transform);
                            var mat = transform.computedWorld;
                            mat = localMat * mat;

                            TransformSystem.DecomposeMatrix(mat, out var matPos, out var matRot, out var matScale);
                            position.X = matPos.X;
                            position.Y = matPos.Y;
                            angle = matRot.Z;
                            scale.X = matScale.X;
                            scale.Y = matScale.Y;
                        }

                        // var order = 1 - ((sprite.zOrder / 200f) + (sprite.id / 500f));
                        // var order = 1 - ((sprite.zOrder / 500f) + .001f * (sprite.id / 500f));
                        //float order = 1f -(sprite.zOrder / 500f);
                        var order = 1 - ((sprite.zOrder / 500f) +
                                         .001f * (sprite.id /
                                                  500f)); //TODO: Why doesn't deferred rendering work here????

                        sb.Draw(tex, position, src, sprite.color, angle, origin, scale, sprite.effects, order,
                            sprite.texCoord1);
                        break;
                    }
                }
                
            }

            if (hasBatch)
            {
                sb.End();
            }
        }
            
        
    }
    
    // public static void RenderAllStages(SpriteBatch sb)
    // {
    //     // var localStages = stages; // TODO: maybe this gets sorted someday?
    //
    //     var localStages = stages.ToList().OrderBy(x => x.id).ToList();
    //     // for (var i = 0; i < localStages.Count; i++)
    //     
    //     sb.GraphicsDevice.SetRenderTarget(mainBuffer);
    //     sb.GraphicsDevice.Clear(backgroundColor);
    //     
    //     for (var i = localStages.Count - 1; i >= 0; i --)
    //     {
    //         var stage = localStages[i];
    //         
    //         // control the output of this stage. "null" means main buffer.
    //         var target = stage.target ?? mainBuffer;
    //         var targetMatrix = Matrix.CreateScale(
    //             target.Width/(float)sb.GraphicsDevice.PresentationParameters.BackBufferWidth, 
    //             target.Height/(float)sb.GraphicsDevice.PresentationParameters.BackBufferHeight, 
    //             1);
    //         sb.GraphicsDevice.SetRenderTarget(target);
    //
    //         if (stage.clearTarget)
    //         {
    //             sb.GraphicsDevice.Clear(stage.clearColor);
    //         }
    //
    //         screenShakeOffsetTarget.X = (Random.Shared.NextSingle()-.5f) * screenShakeMag;
    //         screenShakeOffsetTarget.Y = (Random.Shared.NextSingle()-.5f) * screenShakeMag;
    //         var screenDelta = screenShakeOffsetTarget - screenShakeOffset;
    //         screenShakeOffset += screenDelta * screenShakeElastic;
    //         var mat2 = Matrix.Identity * Matrix.CreateTranslation(new Vector3(screenShakeOffset.X, screenShakeOffset.Y, 0));
    //
    //         Effect stageEffect = null;
    //         if (_effectMap.TryGetValue(stage.effectId, out var stageEffectIndex))
    //         {
    //             stageEffect = effects[stageEffectIndex].effect;
    //         }
    //
    //         var vp = sb.GraphicsDevice.Viewport;
    //         if (stageEffect?.Parameters.ContainsParameter("MatrixTransform") ?? false)
    //         {
    //             Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, -100, out var projection);
    //             stageEffect.Parameters["MatrixTransform"].SetValue(mat2 * projection);
    //
    //         }
    //
    //         
    //         // start a sprite batch with the given settings
    //         sb.Begin(
    //             sortMode: SpriteSortMode.BackToFront,
    //             blendState: stage.blendState, 
    //             effect: stageEffect,
    //             samplerState: stage.samplerState, 
    //             // samplerState: SamplerState.AnisotropicClamp, 
    //             
    //             // TODO: I had this in at one point for the card game with multiple render passes... But it wrecks pixel graphics
    //             transformMatrix: targetMatrix
    //             // transformMatrix: mat2
    //             );
    //
    //         var x = 0;
    //         // draw all the texts
    //         foreach (var spriteTextIndex in stage.stagedSpriteTextIndexes)
    //         {
    //             var text = TextSystem.textSprites[spriteTextIndex];
    //             if (text.sprite.hidden) continue;
    //             
    //             TextureSystem.GetSpriteFontIndex(text.sprite.imageId, out _, out var runtimeFont);
    //             var font = runtimeFont.font;
    //             
    //             // cannot render text without a default font.
    //             if (font == null) continue;
    //
    //             var size = font.MeasureString(text.text);
    //             var origin = new Vector2(size.X * text.sprite.origin.X, size.Y * text.sprite.origin.Y);
    //
    //             var position = text.sprite.position;
    //             var angle = text.sprite.rotation;
    //             var scale = text.sprite.scale;
    //
    //             
    //             if (text.sprite.anchorTransformId > 0)
    //             {
    //                 var localMat = TransformSystem.CreateMatrix(position, angle, scale);
    //                 
    //                 TransformSystem.GetTransformIndex(text.sprite.anchorTransformId, out _, out var transform);
    //                 var mat = transform.computedWorld;
    //                 mat = localMat * mat;
    //             
    //                 TransformSystem.DecomposeMatrix(mat, out var matPos, out var matRot, out var matScale);
    //                 position.X = matPos.X;
    //                 position.Y = matPos.Y;
    //                 angle = matRot.Z;
    //                 scale.X = matScale.X;
    //                 scale.Y = matScale.Y;
    //             }
    //
    //             var order = 1 - ((text.sprite.zOrder / 200f) + (text.sprite.id / 500f));
    //             sb.DrawString(font, text.text, position, text.sprite.color, angle, origin, scale, text.sprite.effects, order);
    //
    //             if (text.dropShadowEnabled)
    //             {
    //                 position -= text.dropShadowOffset;
    //                 order += (2f / 200f);
    //                 var color = text.dropShadowColor;
    //                 if (text.sprite.color.A <= 1)
    //                 {
    //                 }
    //                 color.A = text.sprite.color.A;
    //
    //                 sb.DrawString(font, text.text, position, color, angle, origin, scale, text.sprite.effects, order);
    //                 
    //             }
    //             
    //             
    //         }
    //         
    //         // find all sprites that should be drawn in this stage
    //         foreach (var spriteIndex in stage.stagedSpriteIndexes)
    //         {
    //             var sprite = SpriteSystem.sprites[spriteIndex];
    //             // Console.WriteLine($"DRAWING SPRITE {sprite.id}");
    //
    //             if (sprite.hidden) continue;
    //
    //             TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);
    //
    //             var tex = runtimeTex.texture;
    //
    //             var src = TextureSystem.GetSourceRect(ref runtimeTex, ref sprite);
    //             var origin = new Vector2(src.Width * sprite.origin.X, src.Height * sprite.origin.Y);
    //             
    //             var position = sprite.position;
    //             var angle = sprite.rotation;
    //             var scale = sprite.scale;
    //             
    //             
    //             if (sprite.anchorTransformId > 0)
    //             {
    //                 var localMat = TransformSystem.CreateMatrix(position, angle, scale);
    //                 
    //                 TransformSystem.GetTransformIndex(sprite.anchorTransformId, out _, out var transform);
    //                 var mat = transform.computedWorld;
    //                 mat = localMat * mat;
    //             
    //                 TransformSystem.DecomposeMatrix(mat, out var matPos, out var matRot, out var matScale);
    //                 position.X = matPos.X;
    //                 position.Y = matPos.Y;
    //                 angle = matRot.Z;
    //                 scale.X = matScale.X;
    //                 scale.Y = matScale.Y;
    //             }
    //
    //             // var order = 1 - ((sprite.zOrder / 200f) + (sprite.id / 500f));
    //              var order = 1 - ((sprite.zOrder / 500f) + .001f * (sprite.id / 500f));
    //             //float order = 1f -(sprite.zOrder / 500f);
    //
    //             
    //             sb.Draw(tex, position, src, sprite.color, angle, origin, scale, sprite.effects, order, sprite.texCoord1); 
    //         }
    //         
    //         sb.End();
    //         
    //     }
    //     
    // }
    
}