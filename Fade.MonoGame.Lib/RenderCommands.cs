using System.Security.Cryptography;
using Fade.MonoGame.Game;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
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
    
    [FadeBasicCommand("render stage")]
    public static void SetStage(int stageId)
    {
        RenderSystem.GetStageIndex(stageId, out var index, out var stage);
        
        RenderSystem.stages[index] = stage;
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
    
    [FadeBasicCommand("set stage effect")]
    public static void SetStageEffect(int stageId, int effectId)
    {
        RenderSystem.GetStageIndex(stageId, out _, out var stage);
        // RenderSystem.GetEffectIndex(effectId, out _, out var runtimeEffect);
        stage.effectId = effectId;
    }
    
    
    [FadeBasicCommand("set stage sampler")]
    public static void SetSamplerState(int stageId, int mode)
    {
        // TextureFilter.Point, TextureAddressMode.Wrap
        RenderSystem.GetStageIndex(stageId, out _, out var stage);
        switch (mode)
        {
            case 0:
                stage.samplerState = SamplerState.LinearWrap;
                break;
            case 1:
                stage.samplerState = SamplerState.PointWrap;
                break;
        }
    }

    [FadeBasicCommand("set stage background")]
    public static void SetBackgroundColor(int stageId, int colorCode)
    {
        RenderSystem.GetStageIndex(stageId, out var index, out var stage);
        stage.clearTarget = true;
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        stage.clearColor = new Color(r, g, b, a);
    }
    
    [FadeBasicCommand("clear stage background")]
    public static void ClearStageBackground(int stageId)
    {
        RenderSystem.GetStageIndex(stageId, out var index, out var stage);
        stage.clearTarget = false;
    }

    [FadeBasicCommand("set stage size ratio")]
    public static void SetRenderSizeRatio(int stageId, float ratio)
    {
        RenderSystem.GetStageIndex(stageId, out _, out var stage);
        stage.renderSizeRatio = ratio;

        if (stage.targetTextureId > 0)
        {
            TextureSystem.GetTextureIndex(stage.targetTextureId, out var index, out var runtimeTex);

            stage.target = new RenderTarget2D(GameSystem.graphicsDeviceManager.GraphicsDevice,
                width: (int)(GameSystem.graphicsDeviceManager.GraphicsDevice.Viewport.Width * stage.renderSizeRatio),
                height: (int)(GameSystem.graphicsDeviceManager.GraphicsDevice.Viewport.Height * stage.renderSizeRatio));
            runtimeTex.texture = stage.target;
            TextureSystem.textures[index] = runtimeTex;
        }

    }

    
    [FadeBasicCommand("grab render texture")]
    public static void GrabRenderTexture(int stageId, int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out var index, out var runtimeTex);
        
        RenderSystem.GetStageIndex(stageId, out _, out var stage);
        if (stage.target == null)
        {
            stage.target = new RenderTarget2D(GameSystem.graphicsDeviceManager.GraphicsDevice,
                width: (int)(GameSystem.graphicsDeviceManager.GraphicsDevice.Viewport.Width * stage.renderSizeRatio),
                height: (int)(GameSystem.graphicsDeviceManager.GraphicsDevice.Viewport.Height * stage.renderSizeRatio));
        }

        stage.targetTextureId = textureId;
        runtimeTex.texture = stage.target;
        TextureSystem.textures[index] = runtimeTex;
    }
}