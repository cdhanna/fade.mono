using System.Security.Cryptography;
using Fade.MonoGame.Core;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Takes a screenshot and saves it as a PNG file.</para>
    ///
    /// <para>If the file path you pass doesn't end in <c>.png</c>, the extension gets
    /// appended automatically, so you don't need to worry about it.</para>
    /// </summary>
    /// <remarks>
    /// This captures whatever is currently in the main render buffer, so call it
    /// after <see cref="Sync(VirtualMachine)">sync</see> if you want the final
    /// composited frame. Calling it mid-frame will grab a partially drawn buffer,
    /// which is usually not what you want.
    ///
    /// The file is written synchronously, so there may be a tiny hitch on the
    /// frame you call it. For most use cases (debug screenshots, photo modes) this
    /// is fine.
    /// </remarks>
    /// <example>
    /// Save a screenshot when the player presses a key:
    /// <code>
    /// DO
    ///   ` press S to take a screenshot
    ///   IF scancode("S") = 1
    ///     screenshot "my_screenshot"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="filePath">The path to save the screenshot to. The <c>.png</c> extension is added if missing.</param>
    /// <seealso cref="Sync">sync</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
    /// <seealso cref="GetRenderWidth">render width</seealso>
    /// <seealso cref="GetRenderHeight">render height</seealso>
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

    /// <summary>
    /// <para>Sets the size of the main render buffer in pixels.</para>
    ///
    /// <para>This controls the internal resolution that everything gets drawn at, which
    /// may differ from the window size. The final image is scaled to fit the window.</para>
    /// </summary>
    /// <remarks>
    /// Call this once during setup to define your game's native resolution. For
    /// example, if you're making a pixel-art game, you might set this to something
    /// small like <c>320</c> by <c>180</c>. The engine will scale it up to the
    /// window size, keeping that crispy pixel look.
    ///
    /// Changing this mid-game is possible but will recreate the render buffer, so
    /// it's best done at startup or during a scene transition. You can read the
    /// current size back with <see cref="GetRenderWidth">render width</see> and
    /// <see cref="GetRenderHeight">render height</see>.
    /// </remarks>
    /// <example>
    /// Set up a pixel-art resolution at startup:
    /// <code>
    /// ` configure a small render buffer for pixel art
    /// set render size 320, 180
    ///
    /// ` verify the size was applied
    /// w = render width()
    /// h = render height()
    /// </code>
    /// </example>
    /// <example>
    /// Set up a standard HD resolution:
    /// <code>
    /// set render size 1280, 720
    /// </code>
    /// </example>
    /// <param name="width">Width of the render buffer in pixels.</param>
    /// <param name="height">Height of the render buffer in pixels.</param>
    /// <seealso cref="GetRenderWidth">render width</seealso>
    /// <seealso cref="GetRenderHeight">render height</seealso>
    /// <seealso cref="TakeSnapshot">screenshot</seealso>
    /// <seealso cref="SetBackgroundColor">set background color</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    [FadeBasicCommand("set render size")]
    public static void SetRenderSize(int width, int height)
    {
        RenderSystem.SetMainRenderSize(width, height);
    }
    /// <summary>
    /// <para>Returns the width of the main render buffer in pixels.</para>
    ///
    /// <para>This reflects whatever was last set with
    /// <see cref="SetRenderSize">set render size</see>.</para>
    /// </summary>
    /// <remarks>
    /// Handy when you need to position things relative to the screen edges. For
    /// instance, centering a sprite horizontally by placing it at
    /// <see cref="GetRenderWidth">render width</see> / <c>2</c>. Pair with
    /// <see cref="GetRenderHeight">render height</see> for full coverage.
    /// </remarks>
    /// <example>
    /// Center a sprite horizontally on screen:
    /// <code>
    /// ` place a sprite in the middle of the screen
    /// texture 1, "Images/Logo"
    /// cx = render width() / 2
    /// cy = render height() / 2
    /// sprite 1, cx, cy, 1
    /// </code>
    /// </example>
    /// <returns>The width of the main render buffer in pixels.</returns>
    /// <seealso cref="GetRenderHeight">render height</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("render width")]
    public static int GetRenderWidth()
    {
        return RenderSystem.mainBuffer.Width;
    }
    /// <summary>
    /// <para>Returns the height of the main render buffer in pixels.</para>
    ///
    /// <para>This reflects whatever was last set with
    /// <see cref="SetRenderSize">set render size</see>.</para>
    /// </summary>
    /// <remarks>
    /// Use this alongside <see cref="GetRenderWidth">render width</see> when you
    /// need to know the full dimensions of the render area. For example, to
    /// place HUD elements along the bottom edge, or to calculate aspect ratios.
    /// </remarks>
    /// <example>
    /// Place a HUD bar along the bottom of the screen:
    /// <code>
    /// ` draw a health bar at the bottom
    /// barY = render height() - 20
    /// barW = render width()
    /// ` use barY and barW to position your HUD sprite
    /// sprite 1, 0, barY, hudImg
    /// </code>
    /// </example>
    /// <returns>The height of the main render buffer in pixels.</returns>
    /// <seealso cref="GetRenderWidth">render width</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("render height")]
    public static int GetRenderHeight()
    {
        return RenderSystem.mainBuffer.Height;
    }

    /// <summary>
    /// <para>Sets the background clear color for the main render buffer.</para>
    ///
    /// <para>Every frame, the buffer is filled with this color before anything is
    /// drawn on top of it.</para>
    /// </summary>
    /// <remarks>
    /// This is the color you see wherever nothing else is being drawn. Think of
    /// it as the "sky" or "void" behind your game. Set it once at startup or
    /// change it dynamically for effects like day/night cycles.
    ///
    /// If you're using render targets, each target can have its own background
    /// color via <see cref="SetRenderTargetBackground">set render target background color</see>.
    /// This command only affects the main buffer.
    /// </remarks>
    /// <example>
    /// Set a dark blue background at startup:
    /// <code>
    /// ` deep blue sky color
    /// set background color rgb(20, 20, 80)
    /// </code>
    /// </example>
    /// <example>
    /// Cycle the background color over time for a day/night effect:
    /// <code>
    /// t = 0
    /// DO
    ///   t = t + 0.01
    ///   r = 40 + sin(t) * 40
    ///   g = 40 + sin(t) * 20
    ///   b = 80 + sin(t) * 60
    ///   set background color rgb(r, g, b)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colorCode">A packed RGBA color value. Use <see cref="Rgb">rgb</see> to build one.</param>
    /// <seealso cref="SetRenderTargetBackground">set render target background color</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("set background color")]
    public static void SetBackgroundColor(int colorCode)
    {
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        RenderSystem.backgroundColor = new Color(r, g, b, a);
    }



    /// <summary>
    /// <para>Returns the next available effect ID without reserving it.</para>
    ///
    /// <para>Calling this multiple times in a row returns the same ID. It doesn't
    /// advance until something actually reserves or uses that slot.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you want to peek at which ID would be assigned next without
    /// committing to it. If you just need an ID to pass straight into
    /// <see cref="LoadEffect">effect</see>, use
    /// <see cref="ReserveEffectNextId">reserve effect id</see> instead, which both
    /// grabs the ID and sets up the internal slot in one call.
    ///
    /// The typical flow is: call <see cref="ReserveEffectNextId">reserve effect id</see>,
    /// then <see cref="LoadEffect">effect</see> with the returned ID. You only
    /// need <see cref="GetFreeEffectNextId">free effect id</see> if you're doing
    /// something more advanced, like checking IDs before deciding whether to allocate.
    /// </remarks>
    /// <example>
    /// Peek at the next effect ID before deciding to allocate:
    /// <code>
    /// ` check what the next ID would be
    /// nextId = free effect id()
    /// </code>
    /// </example>
    /// <param name="effectId">Receives the next available effect ID.</param>
    /// <returns>The next available effect ID.</returns>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    [FadeBasicCommand("free effect id")]
    public static int GetFreeEffectNextId(ref int effectId)
    {
        effectId = RenderSystem.highestEffectId + 1;
        return effectId;
    }

    /// <summary>
    /// <para>Reserves the next available effect ID and initializes its internal slot.</para>
    ///
    /// <para>After calling this, the ID is yours. Nothing else will hand it out, and
    /// you can safely pass it to <see cref="LoadEffect">effect</see>.</para>
    /// </summary>
    /// <remarks>
    /// This is the recommended way to get a new effect ID. It calls
    /// <see cref="GetFreeEffectNextId">free effect id</see> internally and then
    /// makes sure the slot is ready to go.
    ///
    /// A typical setup sequence looks like: call
    /// <see cref="ReserveEffectNextId">reserve effect id</see> to get your ID,
    /// then <see cref="LoadEffect">effect</see> to load the shader, then use
    /// the various <c>set effect param</c> commands to configure it.
    /// </remarks>
    /// <example>
    /// Reserve an effect ID and load a shader:
    /// <code>
    /// ` grab an effect ID and load a bloom shader
    /// fxId = reserve effect id()
    /// effect fxId, "bloom"
    /// </code>
    /// </example>
    /// <param name="effectId">Receives the reserved effect ID.</param>
    /// <returns>The reserved effect ID.</returns>
    /// <seealso cref="GetFreeEffectNextId">free effect id</seealso>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="ClearScreenEffect">clear screen effect</seealso>
    [FadeBasicCommand("reserve effect id")]
    public static int ReserveEffectNextId(ref int effectId)
    {
        GetFreeEffectNextId(ref effectId);
        RenderSystem.GetEffectIndex(effectId, out _, out _);
        return effectId;
    }

    /// <summary>
    /// <para>Loads a shader effect from the content pipeline.</para>
    ///
    /// <para>The effect is also watched for file changes, so if you modify the
    /// shader on disk, it hot-reloads automatically without restarting.</para>
    /// </summary>
    /// <remarks>
    /// Before calling this, you need an effect ID. Either grab one with
    /// <see cref="ReserveEffectNextId">reserve effect id</see> or pick your own
    /// number. The <c>effectName</c> is the content pipeline asset name (the same
    /// name you'd use in a content project, without the file extension).
    ///
    /// Once loaded, configure the effect's parameters with commands like
    /// <see cref="SetEffectParameter_Float">set effect param float</see>,
    /// <see cref="SetEffectParameter_ColorInt">set effect param color</see>,
    /// <see cref="SetEffectParameter_Texture">set effect param texture</see>, etc.
    /// Then apply it to the screen with <see cref="SetScreenEffect">set screen effect</see>.
    ///
    /// The hot-reload watcher is great during development. Tweak your shader
    /// in an external editor and see changes live without restarting the game.
    /// </remarks>
    /// <example>
    /// Load a shader and apply it as a full-screen effect:
    /// <code>
    /// ` set up a post-processing shader
    /// fxId = reserve effect id()
    /// effect fxId, "vignette"
    /// set effect param float fxId, "Intensity", 0.5
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <example>
    /// Load a shader and update parameters each frame:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "wave_distort"
    /// set screen effect fxId
    ///
    /// DO
    ///   t = game ms() / 1000.0
    ///   set effect param float fxId, "Time", t
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="effectId">The ID to assign to this effect. Use <see cref="ReserveEffectNextId">reserve effect id</see> to get one.</param>
    /// <param name="effectName">The content pipeline asset name of the shader to load.</param>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="GetFreeEffectNextId">free effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="ClearScreenEffect">clear screen effect</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    /// <seealso cref="SetEffectParameter_Float2">set effect param float2</seealso>
    /// <seealso cref="SetEffectParameter_Float3">set effect param float3</seealso>
    /// <seealso cref="SetEffectParameter_Float4">set effect param float4</seealso>
    /// <seealso cref="GameTime">game ms</seealso>
    /// <seealso cref="Sync">sync</seealso>
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

    /// <summary>
    /// <para>Sets how intense the screen shake effect is.</para>
    ///
    /// <para>Higher values produce more dramatic shaking. Set to <c>0</c> to stop
    /// the shake entirely.</para>
    /// </summary>
    /// <remarks>
    /// Screen shake is a great way to add impact to explosions, hits, or
    /// dramatic events. The magnitude controls how far the screen can move from
    /// its normal position during a shake.
    ///
    /// Pair this with <see cref="SetScreenShakeBounce">set screen shake bounce</see>
    /// to control how quickly the shake settles down. A high magnitude with low
    /// bounce gives a single sharp jolt; high magnitude with high bounce gives a
    /// sustained rumble.
    ///
    /// The shake is applied to the final rendered image, so it affects everything
    /// on screen uniformly.
    /// </remarks>
    /// <example>
    /// Trigger a screen shake on an explosion:
    /// <code>
    /// ` big explosion shake
    /// set screen shake amount 15.0
    /// set screen shake bounce 0.8
    /// </code>
    /// </example>
    /// <example>
    /// Stop the screen shake:
    /// <code>
    /// set screen shake amount 0
    /// </code>
    /// </example>
    /// <param name="mag">The shake intensity. <c>0</c> means no shake; larger values mean more movement.</param>
    /// <seealso cref="SetScreenShakeBounce">set screen shake bounce</seealso>
    [FadeBasicCommand("set screen shake amount")]
    public static void SetScreenShakeMag(float mag)
    {
        RenderSystem.screenShakeMag = mag;
    }

    /// <summary>
    /// <para>Sets how bouncy the screen shake feels.</para>
    ///
    /// <para>This controls the elasticity, meaning how quickly the shake oscillates and
    /// settles back to center.</para>
    /// </summary>
    /// <remarks>
    /// Think of this like a spring constant. A higher bounce value makes the
    /// screen snap back and forth more aggressively, creating a jittery feel. A
    /// lower value gives a more sluggish, heavy shake.
    ///
    /// Use this alongside <see cref="SetScreenShakeMag">set screen shake amount</see>
    /// to dial in the feel you want. For a quick camera punch, try a high
    /// magnitude with moderate bounce. For a sustained earthquake effect, keep
    /// the magnitude lower and the bounce higher.
    /// </remarks>
    /// <example>
    /// Set up a sharp, punchy camera shake:
    /// <code>
    /// ` quick jolt that settles fast
    /// set screen shake amount 10.0
    /// set screen shake bounce 0.5
    /// </code>
    /// </example>
    /// <example>
    /// Set up a sustained earthquake rumble:
    /// <code>
    /// ` ongoing tremor with high elasticity
    /// set screen shake amount 4.0
    /// set screen shake bounce 2.0
    /// </code>
    /// </example>
    /// <param name="bounce">The elasticity of the shake. Higher values produce faster, snappier oscillation.</param>
    /// <seealso cref="SetScreenShakeMag">set screen shake amount</seealso>
    [FadeBasicCommand("set screen shake bounce")]
    public static void SetScreenShakeBounce(float bounce)
    {
        RenderSystem.screenShakeElastic = bounce;
    }

    /// <summary>
    /// <para>Sets a color parameter on a shader effect.</para>
    ///
    /// <para>The color is passed as a packed RGBA value and sent to the named
    /// parameter in the shader.</para>
    /// </summary>
    /// <remarks>
    /// Use this to feed color data into your custom shaders. For example, a
    /// tint color, an outline color, or a fog color. The <c>parameterName</c>
    /// must match the parameter name declared in the shader source exactly.
    ///
    /// If the parameter doesn't exist in the shader, this call is silently
    /// ignored. No error is thrown, which makes it safe to call even if the
    /// shader has been hot-reloaded and the parameter was temporarily removed.
    ///
    /// Load the effect first with <see cref="LoadEffect">effect</see>, then set
    /// its parameters with this and the other <c>set effect param</c> commands.
    /// </remarks>
    /// <example>
    /// Pass a tint color to a shader:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "color_tint"
    ///
    /// ` set a warm orange tint
    /// set effect param color fxId, "TintColor", rgb(255, 180, 80)
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to modify. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <param name="parameterName">The name of the shader parameter, exactly as declared in the shader.</param>
    /// <param name="colorCode">A packed RGBA color value. Use <see cref="Rgb">rgb</see> to build one.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_Float2">set effect param float2</seealso>
    /// <seealso cref="SetEffectParameter_Float3">set effect param float3</seealso>
    /// <seealso cref="SetEffectParameter_Float4">set effect param float4</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    [FadeBasicCommand("set effect param color")]
    public static void SetEffectParameter_ColorInt(int effectId, string parameterName, int colorCode)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        var mgColor = new Color(r, g, b, a);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(mgColor.ToVector4());
    }

    /// <summary>
    /// <para>Sets a single-number parameter on a shader effect.</para>
    ///
    /// <para>The parameter name must match the shader source exactly.</para>
    /// </summary>
    /// <remarks>
    /// This is the most common way to feed data into shaders. Things like time,
    /// intensity, threshold values, or any single number your shader needs. For
    /// example, you might pass <see cref="GameTime">game ms</see> divided by
    /// <c>1000</c> to get a seconds-based timer for animations.
    ///
    /// If the named parameter doesn't exist in the shader, the call is silently
    /// ignored. This is handy during development when you're iterating on shader
    /// code with hot-reload.
    ///
    /// Load the effect first with <see cref="LoadEffect">effect</see>.
    /// </remarks>
    /// <example>
    /// Animate a shader parameter over time:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "dissolve"
    /// set screen effect fxId
    ///
    /// DO
    ///   ` pass elapsed time in seconds to the shader
    ///   t = game ms() / 1000.0
    ///   set effect param float fxId, "Time", t
    ///   set effect param float fxId, "Threshold", 0.5
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to modify. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <param name="parameterName">The name of the shader parameter, exactly as declared in the shader.</param>
    /// <param name="value">The value to set.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
    /// <seealso cref="SetEffectParameter_Float2">set effect param float2</seealso>
    /// <seealso cref="SetEffectParameter_Float3">set effect param float3</seealso>
    /// <seealso cref="SetEffectParameter_Float4">set effect param float4</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    /// <seealso cref="GameTime">game ms</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("set effect param float")]
    public static void SetEffectParameter_Float(int effectId, string parameterName, float value)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(value);
    }

    /// <summary>
    /// <para>Sets a two-component parameter on a shader effect.</para>
    ///
    /// <para>Use this for shader parameters that expect two values, like a screen
    /// resolution or a direction vector.</para>
    /// </summary>
    /// <remarks>
    /// Common uses include passing the render size (from
    /// <see cref="GetRenderWidth">render width</see> and
    /// <see cref="GetRenderHeight">render height</see>) to a post-processing
    /// shader, or sending a normalized direction for effects like directional blur.
    ///
    /// If the named parameter doesn't exist in the shader, the call is silently
    /// ignored.
    ///
    /// Load the effect first with <see cref="LoadEffect">effect</see>.
    /// </remarks>
    /// <example>
    /// Pass the render resolution to a post-processing shader:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "pixelate"
    ///
    /// ` tell the shader the screen dimensions
    /// w = render width()
    /// h = render height()
    /// set effect param float2 fxId, "ScreenSize", w, h
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to modify. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <param name="parameterName">The name of the shader parameter, exactly as declared in the shader.</param>
    /// <param name="x">The first component.</param>
    /// <param name="y">The second component.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="GetRenderWidth">render width</seealso>
    /// <seealso cref="GetRenderHeight">render height</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
    /// <seealso cref="SetEffectParameter_Float3">set effect param float3</seealso>
    /// <seealso cref="SetEffectParameter_Float4">set effect param float4</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    [FadeBasicCommand("set effect param float2")]
    public static void SetEffectParameter_Float2(int effectId, string parameterName, float x, float y)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(new Vector2(x, y));
    }

    /// <summary>
    /// <para>Sets a three-component parameter on a shader effect.</para>
    ///
    /// <para>Use this for shader parameters that expect three values, like a position
    /// in 3D space or an RGB color without alpha.</para>
    /// </summary>
    /// <remarks>
    /// If your shader has a light position, a world-space coordinate, or a color
    /// parameter that doesn't need alpha, this is the command for it. For colors
    /// that do include alpha, consider using
    /// <see cref="SetEffectParameter_ColorInt">set effect param color</see> instead,
    /// which takes a packed RGBA value.
    ///
    /// If the named parameter doesn't exist in the shader, the call is silently
    /// ignored.
    ///
    /// Load the effect first with <see cref="LoadEffect">effect</see>.
    /// </remarks>
    /// <example>
    /// Pass a light position to a shader:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "lighting"
    ///
    /// ` set the light at world position (100, 200, 50)
    /// set effect param float3 fxId, "LightPos", 100.0, 200.0, 50.0
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <example>
    /// Pass an RGB color without alpha as three separate floats:
    /// <code>
    /// ` fog color in 0..1 range
    /// set effect param float3 fxId, "FogColor", 0.6, 0.7, 0.9
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to modify. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <param name="parameterName">The name of the shader parameter, exactly as declared in the shader.</param>
    /// <param name="x">The first component.</param>
    /// <param name="y">The second component.</param>
    /// <param name="z">The third component.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_Float2">set effect param float2</seealso>
    /// <seealso cref="SetEffectParameter_Float4">set effect param float4</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    [FadeBasicCommand("set effect param float3")]
    public static void SetEffectParameter_Float3(int effectId, string parameterName, float x, float y, float z)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(new Vector3(x, y, z));
    }

    /// <summary>
    /// <para>Sets a four-component parameter on a shader effect.</para>
    ///
    /// <para>Use this for shader parameters that expect four values, like a
    /// rectangle, a quaternion, or a custom data pack.</para>
    /// </summary>
    /// <remarks>
    /// This is the most flexible of the <c>set effect param</c> family. It can
    /// represent anything your shader needs as four numbers. If you're passing a
    /// color, though, you'll probably find
    /// <see cref="SetEffectParameter_ColorInt">set effect param color</see> more
    /// convenient since it takes a packed RGBA value directly.
    ///
    /// If the named parameter doesn't exist in the shader, the call is silently
    /// ignored.
    ///
    /// Load the effect first with <see cref="LoadEffect">effect</see>.
    /// </remarks>
    /// <example>
    /// Pass a clipping rectangle to a shader:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "clip_rect"
    ///
    /// ` define a rectangle as (x, y, width, height)
    /// set effect param float4 fxId, "ClipRect", 10.0, 20.0, 200.0, 150.0
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to modify. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <param name="parameterName">The name of the shader parameter, exactly as declared in the shader.</param>
    /// <param name="x">The first component.</param>
    /// <param name="y">The second component.</param>
    /// <param name="z">The third component.</param>
    /// <param name="w">The fourth component.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_Float2">set effect param float2</seealso>
    /// <seealso cref="SetEffectParameter_Float3">set effect param float3</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    [FadeBasicCommand("set effect param float4")]
    public static void SetEffectParameter_Float4(int effectId, string parameterName, float x, float y, float z, float w)
    {
        RenderSystem.GetEffectIndex(effectId, out var index, out var runtimeEffect);
        if (runtimeEffect.effect.Parameters.ContainsParameter(parameterName))

            runtimeEffect.effect.Parameters[parameterName].SetValue(new Vector4(x, y, z, w));
    }

    /// <summary>
    /// <para>Sets a texture parameter on a shader effect.</para>
    ///
    /// <para>The texture must already be loaded via
    /// <see cref="LoadTexture">texture</see> or obtained from a
    /// <see cref="GetRenderTargetTexture">render target texture</see>.</para>
    /// </summary>
    /// <remarks>
    /// This is how you feed images into your custom shaders. For example, a
    /// noise texture for dissolve effects, a lookup table for color grading, or
    /// a render target for multi-pass rendering.
    ///
    /// The <c>parameterName</c> must match the texture sampler name declared in
    /// the shader source exactly. If the parameter doesn't exist, the call is
    /// silently ignored.
    ///
    /// A common pattern is to create a <see cref="SetRenderTargetTexture">render target</see>,
    /// draw some sprites to it with <see cref="SetSpriteTarget">set sprite render target</see>,
    /// then pass that target's texture into a post-processing shader with this
    /// command.
    ///
    /// Load the effect first with <see cref="LoadEffect">effect</see>.
    /// </remarks>
    /// <example>
    /// Feed a noise texture into a dissolve shader:
    /// <code>
    /// ` load the noise texture
    /// texture 1, "Images/Noise"
    ///
    /// ` set up the dissolve shader
    /// fxId = reserve effect id()
    /// effect fxId, "dissolve"
    /// set effect param texture fxId, "NoiseTex", 1
    /// set effect param float fxId, "Threshold", 0.3
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <example>
    /// Use a render target's output as input to another shader:
    /// <code>
    /// ` create a render target and grab its texture
    /// rtId = reserve render target id()
    /// render target rtId, 0
    /// rtTex = render target texture(rtId)
    ///
    /// ` pass the render target texture into a blur shader
    /// fxId = reserve effect id()
    /// effect fxId, "blur"
    /// set effect param texture fxId, "SceneTex", rtTex
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to modify. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <param name="parameterName">The name of the texture sampler in the shader.</param>
    /// <param name="textureId">The texture to assign. Must have been loaded with <see cref="LoadTexture">texture</see> or obtained from a render target.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="GetRenderTargetTexture">render target texture</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
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


    /// <summary>
    /// <para>Removes the screen-wide post-processing effect, returning to normal rendering.</para>
    ///
    /// <para>After calling this, the main buffer is drawn directly to the screen with
    /// no shader applied.</para>
    /// </summary>
    /// <remarks>
    /// Use this to turn off an effect that was applied with
    /// <see cref="SetScreenEffect">set screen effect</see>. This is useful for
    /// toggling effects on and off. For example, removing a blur when a pause
    /// menu closes, or clearing a color-grading pass during a cutscene.
    ///
    /// You can call this even if no screen effect is currently set; it's harmless.
    /// </remarks>
    /// <example>
    /// Toggle a post-processing effect on and off with a key press:
    /// <code>
    /// fxId = reserve effect id()
    /// effect fxId, "grayscale"
    /// effectOn = 0
    ///
    /// DO
    ///   IF scancode("G") = 1
    ///     IF effectOn = 0
    ///       set screen effect fxId
    ///       effectOn = 1
    ///     ELSE
    ///       clear screen effect
    ///       effectOn = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="SetScreenEffect">set screen effect</seealso>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("clear screen effect")]
    public static void ClearScreenEffect()
    {
        RenderSystem.screenEffectIndex = -1;
    }

    /// <summary>
    /// <para>Applies a shader effect as a full-screen post-processing pass.</para>
    ///
    /// <para>The effect is applied to the entire main render buffer every frame until
    /// you call <see cref="ClearScreenEffect">clear screen effect</see>.</para>
    /// </summary>
    /// <remarks>
    /// This is how you add screen-wide visual effects like bloom, vignette,
    /// color grading, or CRT scanlines. Load an effect with
    /// <see cref="LoadEffect">effect</see>, configure its parameters with the
    /// various <c>set effect param</c> commands, then call this to activate it.
    ///
    /// Only one screen effect can be active at a time. Calling this again with a
    /// different effect ID replaces the previous one. To remove it entirely, call
    /// <see cref="ClearScreenEffect">clear screen effect</see>.
    ///
    /// The effect's shader receives the main render buffer as its input texture.
    /// Make sure your shader has a texture sampler set up to receive the screen
    /// contents.
    /// </remarks>
    /// <example>
    /// Apply a CRT scanline effect to the whole screen:
    /// <code>
    /// ` load and activate a CRT shader
    /// fxId = reserve effect id()
    /// effect fxId, "crt_scanlines"
    /// set effect param float fxId, "ScanlineIntensity", 0.4
    /// set screen effect fxId
    /// </code>
    /// </example>
    /// <param name="effectId">The effect to apply. Must have been loaded with <see cref="LoadEffect">effect</see>.</param>
    /// <seealso cref="ClearScreenEffect">clear screen effect</seealso>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="ReserveEffectNextId">reserve effect id</seealso>
    /// <seealso cref="SetEffectParameter_Float">set effect param float</seealso>
    /// <seealso cref="SetEffectParameter_ColorInt">set effect param color</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
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

    /// <summary>
    /// <para>Sets the background clear color for a specific render target.</para>
    ///
    /// <para>Each render target can have its own clear color, independent of the
    /// main buffer's <see cref="SetBackgroundColor">set background color</see>.</para>
    /// </summary>
    /// <remarks>
    /// When a render target is cleared each frame (controlled by
    /// <see cref="SetRenderTargetClearFlags">set render target clear flags</see>),
    /// it fills with this color before any sprites are drawn onto it. The default
    /// is typically transparent black, which is usually what you want for layered
    /// rendering. You might want an opaque color if the render target represents
    /// a self-contained scene.
    ///
    /// Create a render target first with <see cref="SetRenderTargetTexture">render target</see>,
    /// then configure its clear behavior with this command and
    /// <see cref="SetRenderTargetClearFlags">set render target clear flags</see>.
    /// </remarks>
    /// <example>
    /// Set a render target to clear with a solid color each frame:
    /// <code>
    /// rtId = reserve render target id()
    /// render target rtId, 0
    ///
    /// ` clear to opaque black each frame
    /// set render target background color rtId, rgb(0, 0, 0)
    /// </code>
    /// </example>
    /// <param name="outputId">The render target ID to configure.</param>
    /// <param name="colorCode">A packed RGBA color value to use as the clear color. Use <see cref="Rgb">rgb</see> to build one.</param>
    /// <seealso cref="SetRenderTargetClearFlags">set render target clear flags</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    /// <seealso cref="ReserveOutputNextId">reserve render target id</seealso>
    /// <seealso cref="SetBackgroundColor">set background color</seealso>
    [FadeBasicCommand("set render target background color")]
    public static void SetRenderTargetBackground(int outputId, int colorCode)
    {
        RenderSystem.GetOutputIndex(outputId, out var index, out var output);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        output.clearColor = new Color(r, g, b, a);
    }

    /// <summary>
    /// <para>Controls whether a render target is cleared each frame before drawing.</para>
    ///
    /// <para>Pass any value greater than <c>0</c> to enable clearing, or <c>0</c> to
    /// disable it.</para>
    /// </summary>
    /// <remarks>
    /// By default, render targets get cleared every frame. Disabling the clear
    /// means sprites drawn in previous frames stick around, which can be useful
    /// for trail effects, accumulation buffers, or painting-style visuals where
    /// you want things to build up over time.
    ///
    /// When clearing is enabled, the render target fills with whatever color was
    /// set by <see cref="SetRenderTargetBackground">set render target background color</see>
    /// before any sprites are drawn to it.
    ///
    /// Create a render target first with <see cref="SetRenderTargetTexture">render target</see>.
    /// </remarks>
    /// <example>
    /// Disable clearing for a paint trail effect:
    /// <code>
    /// rtId = reserve render target id()
    /// render target rtId, 0
    ///
    /// ` don't clear, so previous frames accumulate
    /// set render target clear flags rtId, 0
    /// </code>
    /// </example>
    /// <example>
    /// Re-enable clearing after a trail sequence:
    /// <code>
    /// set render target clear flags rtId, 1
    /// </code>
    /// </example>
    /// <param name="outputId">The render target ID to configure.</param>
    /// <param name="clearTarget">Greater than <c>0</c> to clear each frame, <c>0</c> to keep previous contents.</param>
    /// <seealso cref="SetRenderTargetBackground">set render target background color</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    /// <seealso cref="ReserveOutputNextId">reserve render target id</seealso>
    [FadeBasicCommand("set render target clear flags")]
    public static void SetRenderTargetClearFlags(int outputId, int clearTarget)
    {
        RenderSystem.GetOutputIndex(outputId, out var index, out var output);
        output.clearTarget = clearTarget > 0;
    }

    /// <summary>
    /// <para>Returns the texture ID associated with a render target.</para>
    ///
    /// <para>Use the returned ID anywhere you'd use a regular texture. For example,
    /// as a <see cref="Sprite">sprite</see> image or as input to a shader via
    /// <see cref="SetEffectParameter_Texture">set effect param texture</see>.</para>
    /// </summary>
    /// <remarks>
    /// Every render target has an associated texture that holds its contents.
    /// This command lets you grab that texture ID so you can use the render
    /// target's output elsewhere in your rendering pipeline.
    ///
    /// A common pattern is multi-pass rendering: draw some sprites to a render
    /// target, grab its texture with this command, then feed that texture into a
    /// post-processing shader or display it on another sprite.
    ///
    /// The render target must have been set up with
    /// <see cref="SetRenderTargetTexture">render target</see> first.
    /// </remarks>
    /// <example>
    /// Display a render target's contents as a sprite:
    /// <code>
    /// ` set up a render target
    /// rtId = reserve render target id()
    /// render target rtId, 0
    ///
    /// ` grab the texture and show it as a sprite
    /// rtTex = render target texture(rtId)
    /// sprite 10, 0, 0, rtTex
    /// </code>
    /// </example>
    /// <param name="outputId">The render target ID to query.</param>
    /// <returns>The texture ID holding this render target's contents. Use it like any other texture ID.</returns>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    /// <seealso cref="ReserveOutputNextId">reserve render target id</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    [FadeBasicCommand("render target texture")]
    public static int GetRenderTargetTexture(int outputId)
    {
        RenderSystem.GetOutputIndex(outputId, out _, out var output);
        return output.targetTextureId;
    }

    /// <summary>
    /// <para>Returns the next available render target ID without reserving it.</para>
    ///
    /// <para>Calling this multiple times in a row returns the same ID. It doesn't
    /// advance until something actually reserves or uses that slot.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you want to peek at which render target ID would be assigned
    /// next without committing to it. In most cases, you'll want
    /// <see cref="ReserveOutputNextId">reserve render target id</see> instead,
    /// which both grabs the ID and initializes the slot in one step.
    ///
    /// The typical flow is: call <see cref="ReserveOutputNextId">reserve render target id</see>,
    /// then <see cref="SetRenderTargetTexture">render target</see> to set it up.
    /// You only need this peeking command for more advanced allocation patterns.
    /// </remarks>
    /// <example>
    /// Peek at the next available render target ID:
    /// <code>
    /// nextRtId = free render target id()
    /// </code>
    /// </example>
    /// <param name="outputId">Receives the next available render target ID.</param>
    /// <returns>The next available render target ID.</returns>
    /// <seealso cref="ReserveOutputNextId">reserve render target id</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    [FadeBasicCommand("free render target id")]
    public static int GetFreeOutputNextId(ref int outputId)
    {
        outputId = RenderSystem.highestOutputId + 1;
        return outputId;
    }

    /// <summary>
    /// <para>Reserves the next available render target ID and initializes its internal slot.</para>
    ///
    /// <para>After calling this, the ID is yours. Pass it to
    /// <see cref="SetRenderTargetTexture">render target</see> to finish setting it up.</para>
    /// </summary>
    /// <remarks>
    /// This is the recommended way to get a new render target ID. It calls
    /// <see cref="GetFreeOutputNextId">free render target id</see> internally
    /// and makes sure the slot is ready to go.
    ///
    /// A typical setup sequence: call this to get the ID, then
    /// <see cref="SetRenderTargetTexture">render target</see> to create the
    /// backing texture, then optionally configure it with
    /// <see cref="SetRenderTargetBackground">set render target background color</see> and
    /// <see cref="SetRenderTargetClearFlags">set render target clear flags</see>.
    /// Finally, assign sprites to it with
    /// <see cref="SetSpriteTarget">set sprite render target</see>.
    /// </remarks>
    /// <example>
    /// Full render target setup sequence:
    /// <code>
    /// ` reserve and create a render target
    /// rtId = reserve render target id()
    /// render target rtId, 0
    ///
    /// ` configure it
    /// set render target background color rtId, rgb(0, 0, 0)
    /// set render target clear flags rtId, 1
    ///
    /// ` assign a sprite to draw on it
    /// texture 1, "Images/Player"
    /// sprite 1, 50, 50, 1
    /// set sprite render target 1, rtId
    /// </code>
    /// </example>
    /// <param name="outputId">Receives the reserved render target ID.</param>
    /// <returns>The reserved render target ID.</returns>
    /// <seealso cref="GetFreeOutputNextId">free render target id</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    /// <seealso cref="SetRenderTargetBackground">set render target background color</seealso>
    /// <seealso cref="SetRenderTargetClearFlags">set render target clear flags</seealso>
    /// <seealso cref="GetRenderTargetTexture">render target texture</seealso>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    [FadeBasicCommand("reserve render target id")]
    public static int ReserveOutputNextId(ref int outputId)
    {
        GetFreeOutputNextId(ref outputId);
        RenderSystem.GetOutputIndex(outputId, out _, out _);
        return outputId;
    }


    /// <summary>
    /// <para>Creates or configures a render target with an associated texture.</para>
    ///
    /// <para>Pass <c>0</c> for the texture ID to auto-allocate one, or <c>-1</c> to
    /// tear down the render target and release its texture.</para>
    /// </summary>
    /// <remarks>
    /// Render targets let you draw sprites to an off-screen buffer instead of
    /// (or in addition to) the main screen. This is the foundation of multi-pass
    /// rendering, post-processing, and any technique where you need to capture
    /// intermediate results.
    ///
    /// The most common pattern is to pass <c>0</c> as the texture ID, which tells
    /// the system to allocate a texture for you automatically using
    /// <see cref="ReserveTextureNextId">reserve texture id</see>. You can then
    /// retrieve that texture ID with <see cref="GetRenderTargetTexture">render target texture</see>
    /// to use it in sprites or shaders.
    ///
    /// If you pass a specific texture ID, the render target binds to that texture.
    /// If the texture ID changes from what was previously bound, a new backing
    /// buffer is created at the current <see cref="SetRenderSize">set render size</see>
    /// dimensions.
    ///
    /// Passing <c>-1</c> clears the render target. Its texture reference is
    /// removed and the backing buffer is released.
    ///
    /// Once set up, assign sprites to draw on this target using
    /// <see cref="SetSpriteTarget">set sprite render target</see>, and configure
    /// clearing behavior with <see cref="SetRenderTargetBackground">set render target background color</see>
    /// and <see cref="SetRenderTargetClearFlags">set render target clear flags</see>.
    /// </remarks>
    /// <example>
    /// Create a render target with an auto-allocated texture:
    /// <code>
    /// ` the simplest setup: pass 0 to auto-allocate
    /// rtId = reserve render target id()
    /// render target rtId, 0
    ///
    /// ` draw a sprite onto the render target
    /// texture 1, "Images/Enemy"
    /// sprite 1, 100, 100, 1
    /// set sprite render target 1, rtId
    /// </code>
    /// </example>
    /// <example>
    /// Tear down a render target when done:
    /// <code>
    /// ` release the render target and its backing buffer
    /// render target rtId, -1
    /// </code>
    /// </example>
    /// <param name="outputId">The render target ID to create or configure.</param>
    /// <param name="textureId">The texture ID to associate. Pass <c>0</c> to auto-allocate, or <c>-1</c> to release.</param>
    /// <seealso cref="ReserveOutputNextId">reserve render target id</seealso>
    /// <seealso cref="GetFreeOutputNextId">free render target id</seealso>
    /// <seealso cref="GetRenderTargetTexture">render target texture</seealso>
    /// <seealso cref="SetRenderTargetBackground">set render target background color</seealso>
    /// <seealso cref="SetRenderTargetClearFlags">set render target clear flags</seealso>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    /// <seealso cref="ReserveTextureNextId">reserve texture id</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
    /// <seealso cref="SetEffectParameter_Texture">set effect param texture</seealso>
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
