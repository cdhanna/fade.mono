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

    /// <summary>
    /// The camera this output views the world through, or 0 for none. The association lives on
    /// the output rather than on each binding because every binding in an MRT set is filled by
    /// the same draw call through one vertex transform -- a per-binding camera is not merely
    /// undesirable, it is unimplementable.
    /// </summary>
    public int cameraId;

    /// <summary>
    /// How this output's sprites blend with what is already in the target.
    ///
    /// Per output rather than per sprite because blending is a property of the batch: every
    /// sprite on a light-accumulation target wants Additive, and letting individual sprites
    /// disagree would fragment the batch for no gain. Defaults to NonPremultiplied, which is
    /// what every output did before this field existed.
    /// </summary>
    public BlendState blendState;

    /// <summary>
    /// The mode <c>set render target blend</c> was last given, kept so a later per-attachment
    /// override can rebuild <see cref="blendState"/> around it. The built state cannot be
    /// interrogated for this: BlendState.Additive and friends are shared singletons.
    /// </summary>
    public int blendMode;

    /// <summary>
    /// Per-attachment blend mode overrides, or null when every attachment shares
    /// <see cref="blendMode"/>. An entry of -1 means "no override, use the output's mode".
    ///
    /// Null rather than an array of -1 so the common case builds no independent blend state at
    /// all, and so an output that never asks for one keeps the shared BlendState singletons.
    /// </summary>
    public int[] attachmentBlendModes;

    /// <summary>
    /// The depth-stencil state this output draws with, or 0 for the sprite batch's default
    /// (DepthStencilState.None).
    ///
    /// An ID rather than the state object, deliberately: the state is resolved fresh each
    /// frame, so editing it after associating it with an output takes effect. Holding the
    /// object would freeze whatever it looked like at the moment of association.
    /// </summary>
    public int depthStencilId;

    /// <summary>
    /// The rasterizer state this output draws with, or 0 for the sprite batch's default
    /// (RasterizerState.CullCounterClockwise). ID rather than object, for the same reason as
    /// <see cref="depthStencilId"/>.
    /// </summary>
    public int rasterizerId;

    public RenderTarget2D[] targets;
    public int[] bindingTextureIds;
    
    // public RenderTarget2D target;
    // public int targetTextureId; // is there a reserved texture id? 
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

    /// <summary>
    /// Re-points every output binding that renders into <paramref name="textureId"/> at
    /// <paramref name="target"/>.
    ///
    /// Needed because a texture id and an output binding are two references to the same
    /// RenderTarget2D, and replacing the texture's asset on its own leaves the output still
    /// drawing into the OLD target while everything sampling the texture reads the new one.
    /// Nothing errors; the buffer simply goes blank and stays blank.
    ///
    /// The replaced target is deliberately NOT disposed. Effects hold their texture parameters
    /// as plain references, so a bound target could still be sitting in an Effect from an
    /// earlier `set effect param texture`, and disposing it would fault on the next draw
    /// instead of merely wasting memory. Creating targets is a setup-time operation, so the
    /// bounded leak is the cheaper mistake.
    /// </summary>
    public static void RebindOutputsToTarget(int textureId, RenderTarget2D target)
    {
        foreach (var output in outputs)
        {
            if (output.bindingTextureIds == null || output.targets == null)
            {
                continue;
            }

            var count = Math.Min(output.bindingTextureIds.Length, output.targets.Length);
            for (var i = 0; i < count; i++)
            {
                if (output.bindingTextureIds[i] == textureId)
                {
                    output.targets[i] = target;
                }
            }
        }
    }
    private static Dictionary<int, int> _outputMap = new Dictionary<int, int>();

    public static List<RuntimeEffect> effects = new List<RuntimeEffect>();
    private static Dictionary<int, int> _effectMap = new Dictionary<int, int>();

    public static int screenEffectIndex = -1;
    public static int highestEffectId;
    public static int highestOutputId = 1;

    /// <summary>
    /// How many attachments a blend state can describe independently. Four is the hardware
    /// number, and it is also the width of MonoGame's BlendState.
    /// </summary>
    public const int MAX_BLEND_ATTACHMENTS = 4;

    /// <summary>
    /// Rebuilds <see cref="RenderOutput.blendState"/> from the output's own mode plus any
    /// per-attachment overrides. Called by both blend commands, so the two compose in either
    /// order rather than the later one erasing the earlier.
    /// </summary>
    public static void RebuildOutputBlend(RenderOutput output)
    {
        // No overrides means the shared singletons, which is what every output had before
        // per-attachment blending existed. Worth keeping: an independent blend state is a fresh
        // object, so building one unconditionally would defeat the batcher's state comparison.
        if (output.attachmentBlendModes == null)
        {
            output.blendState = SharedBlendState(output.blendMode);
            return;
        }

        var built = new BlendState { IndependentBlendEnable = true };
        for (var i = 0; i < MAX_BLEND_ATTACHMENTS; i++)
        {
            var mode = output.attachmentBlendModes[i];
            if (mode < 0) mode = output.blendMode;
            ApplyBlendMode(built[i], mode);
        }

        output.blendState = built;
    }

    private static BlendState SharedBlendState(int mode) => mode switch
    {
        1 => BlendState.Additive,
        2 => BlendState.Opaque,
        3 => BlendState.AlphaBlend,
        // Mode 4 has no singleton, and cannot share one: BlendState is mutable, so handing back a
        // cached instance would let a later output edit every earlier one's.
        4 => MaxBlendState(),
        _ => BlendState.NonPremultiplied
    };

    private static BlendState MaxBlendState()
    {
        var bs = new BlendState();
        ApplyBlendMode(bs[0], 4);
        return bs;
    }

    private static void ApplyBlendMode(TargetBlendState target, int mode)
    {
        target.ColorWriteChannels = ColorWriteChannels.All;

        switch (mode)
        {
            case 1: // additive
                target.ColorBlendFunction = BlendFunction.Add;
                target.ColorSourceBlend = Blend.SourceAlpha;
                target.ColorDestinationBlend = Blend.One;
                target.AlphaBlendFunction = BlendFunction.Add;
                target.AlphaSourceBlend = Blend.SourceAlpha;
                target.AlphaDestinationBlend = Blend.One;
                break;

            case 2: // opaque
                target.ColorBlendFunction = BlendFunction.Add;
                target.ColorSourceBlend = Blend.One;
                target.ColorDestinationBlend = Blend.Zero;
                target.AlphaBlendFunction = BlendFunction.Add;
                target.AlphaSourceBlend = Blend.One;
                target.AlphaDestinationBlend = Blend.Zero;
                break;

            case 3: // premultiplied alpha
                target.ColorBlendFunction = BlendFunction.Add;
                target.ColorSourceBlend = Blend.One;
                target.ColorDestinationBlend = Blend.InverseSourceAlpha;
                target.AlphaBlendFunction = BlendFunction.Add;
                target.AlphaSourceBlend = Blend.One;
                target.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                break;

            case 4: // maximum
                // Both factors One: Max ignores them on most hardware, but leaving them at a
                // default that scales by alpha would be a trap the day someone switches this
                // attachment back to Add.
                target.ColorBlendFunction = BlendFunction.Max;
                target.ColorSourceBlend = Blend.One;
                target.ColorDestinationBlend = Blend.One;

                // Alpha OVERWRITES rather than taking a max, so an attachment using this mode can
                // still carry one ordinary frontmost-wins value alongside the three combining
                // ones. All three colour channels share one function; alpha has its own.
                target.AlphaBlendFunction = BlendFunction.Add;
                target.AlphaSourceBlend = Blend.One;
                target.AlphaDestinationBlend = Blend.Zero;
                break;

            default: // non-premultiplied alpha
                target.ColorBlendFunction = BlendFunction.Add;
                target.ColorSourceBlend = Blend.SourceAlpha;
                target.ColorDestinationBlend = Blend.InverseSourceAlpha;
                target.AlphaBlendFunction = BlendFunction.Add;
                target.AlphaSourceBlend = Blend.SourceAlpha;
                target.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                break;
        }
    }


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
                cameraId = CameraSystem.CAMERA_ID_NONE, // identity view, so existing games are untouched
                blendState = BlendState.NonPremultiplied, // what every output did before this was settable
                blendMode = 0,                            // and the mode that produced it
                targets = null, // null is magic, and defaults to drawing on the screen
                // target = null, // default to drawing to the screen
                // targetTextureId = -1,
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
#if !BROWSER
        // Desktop: ContentWatcher's FileSystemWatcher fires when an .fx file
        // on disk changes; TryRefreshAsset checks the watch slot per frame
        // and swaps in the freshly-compiled Effect when the bytes change.
        for (var i = 0 ; i < effects.Count; i ++)
        {
            var fx = effects[i];
            // Effect ids are sparse, so this list has default-valued holes between
            // the slots a program actually filled, and a hole has no asset name.
            // TryRefreshAsset would Path.Combine(root, null) and take the process
            // down from Update with an ArgumentNullException naming only 'path2'.
            // The browser branch below has always had this guard; desktop did not.
            if (string.IsNullOrEmpty(fx.watchedEffect.assetName)) continue;
            if (GameSystem.game.ContentWatcher.TryRefreshAsset(ref fx.watchedEffect))
            {
                effects[i] = fx;
            }

        }
#else
        // Browser: the playground recompiles .fx → MGFX XNB on file save and
        // re-registers the bytes via BrowserContentManager. RegisterAsset
        // marks the name as "reloaded" when it's replacing existing bytes;
        // ConsumeReloadedAssets drains that set so we only re-Load effects
        // whose underlying asset actually changed.
        var reloaded = GameSystem.game.BrowserContent.ConsumeReloadedAssets();
        if (reloaded.Count > 0)
        {
            for (var i = 0; i < effects.Count; i++)
            {
                var fx = effects[i];
                if (string.IsNullOrEmpty(fx.filePath)) continue;
                if (!reloaded.Contains(fx.filePath)) continue;
                try
                {
                    var fresh = GameSystem.game.Content.Load<Effect>(fx.filePath);
                    fx.watchedEffect = new WatchedAsset<Effect>
                    {
                        Asset = fresh,
                        UpdatedAt = DateTimeOffset.Now,
                        assetName = fx.filePath,
                    };
                    effects[i] = fx;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[fade] hot-reload failed for effect '{fx.filePath}': {ex.Message}");
                }
            }
        }
#endif

        foreach (var fx in effects)
        {
            // Same sparse-slot hole as the refresh loop above: an id the program
            // never loaded leaves a default entry whose Asset is null.
            if (fx.effect == null) continue;

            if (fx.effect.Parameters.ContainsParameter("Time"))
                fx.effect.Parameters["Time"].SetValue((float)GameSystem.latestTime.TotalGameTime.TotalSeconds);
            
            if (fx.effect.Parameters.ContainsParameter("Resolution"))
                fx.effect.Parameters["Resolution"].SetValue(new Vector2(mainBuffer.Width, mainBuffer.Height));
            
        }
    }


    public static bool flip = false;
    public static Matrix shakeMat;

    public static void RenderAll2(SpriteBatch sb)
    {
        var localOutputs = outputs.ToList().OrderBy(x => x.order).ToList();

        screenShakeOffsetTarget.X = (Random.Shared.NextSingle()-.5f) * screenShakeMag;
        screenShakeOffsetTarget.Y = (Random.Shared.NextSingle()-.5f) * screenShakeMag;
        var screenDelta = screenShakeOffsetTarget - screenShakeOffset;
        screenShakeOffset += screenDelta * screenShakeElastic;
        shakeMat = Matrix.Identity * Matrix.CreateTranslation(new Vector3(screenShakeOffset.X, screenShakeOffset.Y, 0));
        
        for (var i = 0; i < localOutputs.Count; i++)
        {
            var output = localOutputs[i];
            
            // initialize the output.
            if (output.targets == null)
            {
                sb.GraphicsDevice.SetRenderTarget(mainBuffer);
            }
            else
            {
                // TODO pull this out of the hot loop- no need to allocate an array every time; we could just use a pool. 
                var bindings = new RenderTargetBinding[output.targets.Length];
                for (var b = 0; b < output.targets.Length; b++)
                {
                    bindings[b] = output.targets[b];
                }
                sb.GraphicsDevice.SetRenderTargets(bindings);
            }

            // The output's view matrix, derived ONCE here rather than inside the item loop:
            // that loop re-Begins whenever the effect changes, and every one of those batches
            // has to be handed the same matrix or the scene tears along effect boundaries.
            //
            // Derived from this output's own dimensions, so a camera shared with a
            // differently-sized target still describes the same world view.
            var cameraTargetWidth = output.targets == null ? mainBuffer.Width : output.targets[0].Width;
            var cameraTargetHeight = output.targets == null ? mainBuffer.Height : output.targets[0].Height;
            var outputMatrix = CameraSystem.GetMatrix(output.cameraId, cameraTargetWidth, cameraTargetHeight);

            // Resolved once here for the same reason as the matrix above: the item loop
            // re-Begins on every effect change, and each of those batches needs the same
            // state. Both return null for id 0, which the sprite batch reads as its own
            // default -- so an output that was never given either behaves as it always did.
            var outputDepthStencil = DepthStencilSystem.Resolve(output.depthStencilId);
            var outputRasterizer = RasterizerSystem.Resolve(output.rasterizerId);

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
                        blendState: output.blendState ?? BlendState.NonPremultiplied,
                        samplerState: SamplerState.PointClamp, // TODO: allow sprites to set their own sampler state
                        depthStencilState: outputDepthStencil,
                        rasterizerState: outputRasterizer,
                        effect: effect,
                        transformMatrix: outputMatrix);
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