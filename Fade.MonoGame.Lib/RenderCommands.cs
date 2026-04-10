using System.Security.Cryptography;
using Fade.MonoGame.Game;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    [FadeBasicCommand("screenshot")]
    public static void TakeSnapshot(string filePath)
    {
        if (!filePath.EndsWith(".png"))
        {
            filePath += ".png";
        }
        using var stream = File.OpenWrite(filePath);
        RenderSystem.mainBuffer.SaveAsPng(stream, RenderSystem.mainBuffer.Width, RenderSystem.mainBuffer.Height);
    }
    
    [FadeBasicCommand("set render size")]
    public static void SetRenderSize(int width, int height)
    {
        RenderSystem.SetMainRenderSize(width, height);
    }
    [FadeBasicCommand("render width")]
    public static int GetRenderWidth()
    {
        return RenderSystem.mainBuffer.Width;
    }
    [FadeBasicCommand("render height")]
    public static int GetRenderHeight()
    {
        return RenderSystem.mainBuffer.Height;
    }

    [FadeBasicCommand("set background color")]
    public static void SetBackgroundColor(int colorCode)
    {
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        RenderSystem.backgroundColor = new Color(r, g, b, a);
    }
    
    
    
    [FadeBasicCommand("free effect id")]
    public static int GetFreeEffectNextId(ref int effectId)
    {
        effectId = RenderSystem.highestEffectId + 1;
        return effectId;
    }
    
    [FadeBasicCommand("reserve effect id")]
    public static int ReserveEffectNextId(ref int effectId)
    {
        GetFreeEffectNextId(ref effectId);
        RenderSystem.GetEffectIndex(effectId, out _, out _);
        return effectId;
    }
    
    [FadeBasicCommand("effect")]
    public static void LoadEffect(int effectId, string effectName)
    {
        //var effect = GameSystem.game.Content.Load<Effect>(effectName);

        var effect = GameSystem.game.ContentWatcher.Watch<Effect>(effectName);
        
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        runtimeEffect.watchedEffect = effect;
        runtimeEffect.filePath = effectName;

        RenderSystem.effects[index] = runtimeEffect;
    }

    [FadeBasicCommand("set screen shake amount")]
    public static void SetScreenShakeMag(float mag)
    {
        RenderSystem.screenShakeMag = mag;
    }
    
    [FadeBasicCommand("set screen shake bounce")]
    public static void SetScreenShakeBounce(float bounce)
    {
        RenderSystem.screenShakeElastic = bounce;
    }

    [FadeBasicCommand("set effect param color")]
    public static void SetEffectParameter_ColorInt(int effectId, string parameterName, int colorCode)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        var mgColor = new Color(r, g, b, a);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(mgColor.ToVector4());
    }
    
    [FadeBasicCommand("set effect param float")]
    public static void SetEffectParameter_Float(int effectId, string parameterName, float value)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(value);
    }
    
    [FadeBasicCommand("set effect param float2")]
    public static void SetEffectParameter_Float2(int effectId, string parameterName, float x, float y)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(new Vector2(x, y));
    }
    
    [FadeBasicCommand("set effect param float3")]
    public static void SetEffectParameter_Float3(int effectId, string parameterName, float x, float y, float z)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(new Vector3(x, y, z));
    }
    
    [FadeBasicCommand("set effect param float4")]
    public static void SetEffectParameter_Float4(int effectId, string parameterName, float x, float y, float z, float w)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(new Vector4(x, y, z, w));
    }
    
    [FadeBasicCommand("set effect param texture")]
    public static void SetEffectParameter_Texture(int effectId, string parameterName, int textureId)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        TextureSystem.GetTextureIndex(textureId, out _, out var runtimeTexture);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))
        {
            runtimeEffect.effect.Parameters[parameterName].SetValue(runtimeTexture.texture);
        }
    }
    
    
    [FadeBasicCommand("clear screen effect")]
    public static void ClearScreenEffect()
    {
        RenderSystem.screenEffectIndex = -1;
    }
    
    [FadeBasicCommand("set screen effect")]
    public static void SetScreenEffect(int effectId)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        RenderSystem.screenEffectIndex = index;

    }
    
    //
    // [FadeBasicCommand("set stage sampler")]
    // public static void SetSamplerState(int stageId, int mode)
    // {
    //     // TextureFilter.Point, TextureAddressMode.Wrap
    //     RenderSystem.GetStageIndex(stageId, out _, out var stage);
    //     switch (mode)
    //     {
    //         case 0:
    //             stage.samplerState = SamplerState.LinearWrap;
    //             break;
    //         case 1:
    //             stage.samplerState = SamplerState.PointWrap;
    //             break;
    //     }
    // }
    //

    [FadeBasicCommand("set render target background color")]
    public static void SetRenderTargetBackground(int outputId, int colorCode)
    {
        RenderSystem.GetOutputIndex(outputId, out var index, out var output);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        output.clearColor = new Color(r, g, b, a);
    }
    
    [FadeBasicCommand("set render target clear flags")]
    public static void SetRenderTargetClearFlags(int outputId, int clearTarget)
    {
        RenderSystem.GetOutputIndex(outputId, out var index, out var output);
        output.clearTarget = clearTarget > 0;
    }

    [FadeBasicCommand("render target texture")]
    public static int GetRenderTargetTexture(int outputId)
    {
        RenderSystem.GetOutputIndex(outputId, out _, out var output);
        return output.targetTextureId;
    }

    [FadeBasicCommand("free render target id")]
    public static int GetFreeOutputNextId(ref int outputId)
    {
        outputId = RenderSystem.highestOutputId + 1;
        return outputId;
    }
    
    [FadeBasicCommand("reserve render target id")]
    public static int ReserveOutputNextId(ref int outputId)
    {
        GetFreeOutputNextId(ref outputId);
        RenderSystem.GetOutputIndex(outputId, out _, out _);
        return outputId;
    }


    [FadeBasicCommand("render target")]
    public static void SetRenderTargetTexture(int outputId, int textureId=0)
    {
        RenderSystem.GetOutputIndex(outputId, out _, out var output);
        if (textureId < 0)
        {
            output.targetTextureId = -1;
            output.target = null;
            return;
        }

        if (textureId == 0 && output.targetTextureId <= 0)
        {
            ReserveTextureNextId(ref textureId);
        }
        
        TextureSystem.GetTextureIndex(textureId, out var index, out var runtimeTex);
        if (output.targetTextureId != textureId)
        {
            output.target = new RenderTarget2D(GameSystem.graphicsDeviceManager.GraphicsDevice,
                width: (int)(RenderSystem.mainBuffer.Width),
                height: (int)(RenderSystem.mainBuffer.Height), 
                mipMap: false, 
                preferredFormat: SurfaceFormat.Color, 
                preferredDepthFormat: DepthFormat.None);
            // output.target = new RenderTarget2D(GameSystem.graphicsDeviceManager.GraphicsDevice,
            //     width: (int)(GameSystem.graphicsDeviceManager.GraphicsDevice.Viewport.Width * 1),
            //     height: (int)(GameSystem.graphicsDeviceManager.GraphicsDevice.Viewport.Height * 1));
        }

        output.targetTextureId = textureId;
        // runtimeTex.texture = output.target;
        runtimeTex.SetComputedTexture(output.target);
        TextureSystem.textures[index] = runtimeTex;
    }

}