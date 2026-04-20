using Fade.MonoGame.Core;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Peeks at the next available sprite ID without claiming it.</para>
    /// <para>This doesn't reserve the ID, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="ReserveSpriteNextId">reserve sprite id</see> instead,
    /// which actually claims the slot. This one is handy if you just need to know what the next ID
    /// would be, for example, to pre-allocate an array. If you already know your ID, skip both of
    /// these and call <see cref="Sprite">sprite</see> directly.
    /// </remarks>
    /// <example>
    /// Peek at the next sprite ID to pre-size an array.
    /// <code>
    /// ` find out what the next sprite ID will be
    /// free sprite id nextId
    /// dim spriteIds(nextId + 10)
    /// </code>
    /// </example>
    /// <param name="spriteId">Receives the next free sprite ID.</param>
    /// <returns>The next available sprite ID (not yet reserved).</returns>
    /// <seealso cref="ReserveSpriteNextId">reserve sprite id</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("free sprite id")]
    public static int GetFreeSpriteNextId(ref int spriteId)
    {
        spriteId = SpriteSystem.highestSpriteId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return spriteId;
    }

    /// <summary>
    /// <para>Claims the next available sprite ID and initializes its slot.</para>
    /// <para>The slot is created but the sprite won't be visible until you call <see cref="Sprite">sprite</see>.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need to configure a sprite (set its texture, position, etc.) before it
    /// officially exists. The typical pattern is: reserve an ID, set properties on it, then call
    /// <see cref="Sprite">sprite</see> to make it live. If you don't need that setup step, just
    /// call <see cref="Sprite">sprite</see> directly with a known ID. See also
    /// <see cref="GetFreeSpriteNextId">free sprite id</see> if you only need to peek without claiming.
    /// </remarks>
    /// <example>
    /// Reserve a sprite ID, configure it, then make it visible.
    /// <code>
    /// ` reserve a slot and set it up before showing
    /// reserve sprite id spr
    /// set sprite texture spr, texId
    /// scale sprite spr, 2.0, 2.0
    /// sprite spr, 100, 200, texId
    /// </code>
    /// </example>
    /// <param name="spriteId">Receives the reserved sprite ID.</param>
    /// <returns>The newly reserved sprite ID.</returns>
    /// <seealso cref="GetFreeSpriteNextId">free sprite id</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteTexture">set sprite texture</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    [FadeBasicCommand("reserve sprite id")]
    public static int ReserveSpriteNextId(ref int spriteId)
    {
        GetFreeSpriteNextId(ref spriteId);
        SpriteSystem.GetSpriteIndex(spriteId, out _, out _);
        return spriteId;
    }


    /// <summary>
    /// <para>Creates a sprite, or updates an existing one's position and texture.</para>
    /// <para>If the ID already exists, this overwrites its position and texture rather than creating a duplicate.</para>
    /// </summary>
    /// <remarks>
    /// This is the main way you put images on screen. You'll need to load a texture first with
    /// <see cref="LoadTexture">texture</see>. The sprite references the texture by ID and won't
    /// actually show up until the next <see cref="Sync">sync</see> call. For moving a sprite after
    /// creation, <see cref="PositionSprite">position sprite</see> is slightly more direct since it
    /// skips the texture assignment.
    /// </remarks>
    /// <example>
    /// Load a texture and create a sprite at the center of the screen.
    /// <code>
    /// ` load an image and show it on screen
    /// texture 1, "hero.png"
    /// sprite 1, 320, 240, 1
    /// sync
    /// </code>
    /// </example>
    /// <example>
    /// Create multiple sprites from the same texture.
    /// <code>
    /// ` place three copies of the same image in a row
    /// texture 1, "coin.png"
    /// FOR i = 1 TO 3
    ///   sprite i, i * 80, 100, 1
    /// NEXT i
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The unique ID for this sprite. Reusing an existing ID updates it.</param>
    /// <param name="x">The X position in screen coordinates.</param>
    /// <param name="y">The Y position in screen coordinates.</param>
    /// <param name="textureId">The ID of a previously loaded texture.</param>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="PositionSprite">position sprite</seealso>
    /// <seealso cref="SetSpriteTexture">set sprite texture</seealso>
    /// <seealso cref="HideSprite">hide sprite</seealso>
    /// <seealso cref="ShowSprite">show sprite</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    [FadeBasicCommand("sprite")]
    public static void Sprite(int spriteId, float x, float y, int textureId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.position = new Vector2(x, y);
        sprite.imageId = textureId;
        sprite.hidden = false;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Moves a sprite to the given screen position.</para>
    /// </summary>
    /// <remarks>
    /// Call this every frame for sprites that move, or once for static ones. If you just created
    /// the sprite with <see cref="Sprite">sprite</see>, the position is already set. Use this
    /// for updates after creation. The position is where the sprite's origin point lands on screen
    /// (see <see cref="SetSpriteOffset">set sprite offset</see> to control the origin).
    /// </remarks>
    /// <example>
    /// Move a sprite with the arrow keys.
    /// <code>
    /// ` simple movement loop
    /// texture 1, "player.png"
    /// sprite 1, 320, 240, 1
    /// px = 320
    /// py = 240
    /// DO
    ///   IF up key(1) THEN py = py - 2
    ///   IF down key(1) THEN py = py + 2
    ///   IF left key(1) THEN px = px - 2
    ///   IF right key(1) THEN px = px + 2
    ///   position sprite 1, px, py
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The ID of the sprite to move.</param>
    /// <param name="x">The new X position in screen coordinates.</param>
    /// <param name="y">The new Y position in screen coordinates.</param>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteOffset">set sprite offset</seealso>
    /// <seealso cref="SpriteX">sprite x</seealso>
    /// <seealso cref="SpriteY">sprite y</seealso>
    [FadeBasicCommand("position sprite")]
    public static void PositionSprite(int spriteId, float x, float y)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out _);
        SpriteSystem.sprites[index].position = new Vector2(x, y);
    }

    /// <summary>
    /// <para>Sets the tint color of a sprite using a packed RGBA integer.</para>
    /// <para>This color multiplies with the texture's own colors. A white tint (<c>0xFFFFFFFF</c>) shows the texture as-is, while other values shift the hue or darken it.</para>
    /// </summary>
    /// <remarks>
    /// Call this any time after creating the sprite with <see cref="Sprite">sprite</see>. The tint is
    /// a multiply blend, so <c>0xFF0000FF</c> (red, full alpha) makes the whole sprite red-tinted, and
    /// <c>0x808080FF</c> (half-grey, full alpha) darkens it to 50%. If you only need to change the RGB
    /// channels without touching alpha, use <see cref="SetSpriteDiffuse(int, byte, byte, byte)">set sprite diffuse</see>.
    /// To change just the transparency, use <see cref="SetSpriteDiffuse(int, byte)">set sprite alpha</see>.
    /// </remarks>
    /// <example>
    /// Tint a sprite red.
    /// <code>
    /// ` make a sprite appear red-tinted
    /// texture 1, "enemy.png"
    /// sprite 1, 100, 100, 1
    /// color sprite 1, 0xFF0000FF
    /// </code>
    /// </example>
    /// <example>
    /// Darken a sprite to 50% brightness.
    /// <code>
    /// ` half-grey tint dims the image
    /// color sprite 1, 0x808080FF
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to tint.</param>
    /// <param name="packedColor">A packed RGBA color value (e.g. <c>0xFF0000FF</c> for opaque red).</param>
    /// <seealso cref="SetSpriteDiffuse(int, byte, byte, byte)">set sprite diffuse</seealso>
    /// <seealso cref="SetSpriteDiffuse(int, byte)">set sprite alpha</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("color sprite")]
    public static void ColorSprite(int spriteId, int packedColor)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        ColorUtil.UnpackColor(packedColor, out var r, out var g, out var b, out var a);
        sprite.color = new Color(r, g, b, a);
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Sets the draw order (z-order) of a sprite.</para>
    /// <para>Higher values draw on top of lower values, so a sprite with order <c>10</c> covers one with order <c>5</c>.</para>
    /// </summary>
    /// <remarks>
    /// Ordering is per-render-target. A sprite's z-order only matters relative to other sprites on the
    /// same target. If two sprites share the same order value, their draw sequence is undefined, so always
    /// assign distinct orders when layering matters. You can call this once at setup or change it dynamically
    /// (e.g. to bring a sprite to the front during an animation). See
    /// <see cref="SetSpriteTarget">set sprite render target</see> for controlling which target a sprite draws to.
    /// </remarks>
    /// <example>
    /// Layer a background behind a player sprite.
    /// <code>
    /// ` set up two sprites with explicit draw order
    /// texture 1, "background.png"
    /// texture 2, "player.png"
    /// sprite 1, 0, 0, 1
    /// sprite 2, 160, 120, 2
    /// ` background draws first, player on top
    /// order sprite 1, 0
    /// order sprite 2, 10
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to reorder.</param>
    /// <param name="order">The z-order value. Higher values draw on top.</param>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    [FadeBasicCommand("order sprite")]
    public static void OrderSprite(int spriteId, int order)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.zOrder = order;
        RenderSystem.GetOutputIndex(sprite.outputIdFlags, out _, out var output);
        output.spritesOrderDirty = true;

        SpriteSystem.sprites[index] = sprite;
    }


    /// <summary>
    /// <para>Hides a sprite so it is not drawn.</para>
    /// <para>The sprite still exists in memory with all its properties intact. It just skips rendering until you call <see cref="ShowSprite">show sprite</see>.</para>
    /// </summary>
    /// <remarks>
    /// This is cheaper than destroying and recreating a sprite when you need to toggle visibility
    /// (e.g. blinking effects, UI panels that open and close). The sprite keeps its position, texture,
    /// scale, and everything else. Use <see cref="ShowSprite">show sprite</see> to make it visible again.
    /// </remarks>
    /// <example>
    /// Blink a sprite on and off every 30 frames.
    /// <code>
    /// ` simple blink effect
    /// texture 1, "powerup.png"
    /// sprite 1, 200, 150, 1
    /// timer = 0
    /// visible = 1
    /// DO
    ///   timer = timer + 1
    ///   IF timer &gt; 30
    ///     timer = 0
    ///     IF visible = 1
    ///       hide sprite 1
    ///       visible = 0
    ///     ELSE
    ///       show sprite 1
    ///       visible = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to hide.</param>
    /// <seealso cref="ShowSprite">show sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("hide sprite")]
    public static void HideSprite(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.hidden = true;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Makes a previously hidden sprite visible again.</para>
    /// <para>Only needed after calling <see cref="HideSprite">hide sprite</see>. Sprites are visible by default when created.</para>
    /// </summary>
    /// <remarks>
    /// This is the counterpart to <see cref="HideSprite">hide sprite</see>. Calling it on a sprite
    /// that is already visible has no effect. The sprite resumes drawing at its current position, scale,
    /// and z-order. Nothing else changes.
    /// </remarks>
    /// <example>
    /// Show a hidden UI panel when the player presses a key.
    /// <code>
    /// ` toggle an inventory panel with the tab key
    /// texture 10, "inventory.png"
    /// sprite 10, 50, 50, 10
    /// hide sprite 10
    /// panelOpen = 0
    /// DO
    ///   IF key hit(scancode("Tab")) = 1
    ///     IF panelOpen = 0
    ///       show sprite 10
    ///       panelOpen = 1
    ///     ELSE
    ///       hide sprite 10
    ///       panelOpen = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to show.</param>
    /// <seealso cref="HideSprite">hide sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("show sprite")]
    public static void ShowSprite(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.hidden = false;
        SpriteSystem.sprites[index] = sprite;
    }


    /// <summary>
    /// <para>Swaps the texture on a sprite without changing anything else.</para>
    /// <para>Position, scale, rotation, color, and all other properties stay the same. Only the image changes.</para>
    /// </summary>
    /// <remarks>
    /// Use this for things like swapping character costumes or cycling through icon states. The new
    /// texture must already be loaded via <see cref="LoadTexture">texture</see>. If the new texture has
    /// different dimensions, the sprite's visual size will change (unless you've set an explicit scale
    /// with <see cref="ScaleSprite">scale sprite</see> or <see cref="SizeSprite">size sprite</see>).
    /// If the sprite had a frame set via <see cref="SetSpriteFrame">set sprite frame</see>, the frame
    /// index carries over. Make sure the new texture has enough frames or reset the frame to <c>0</c>.
    /// </remarks>
    /// <example>
    /// Swap a character's texture when they take damage.
    /// <code>
    /// ` load both normal and hurt textures
    /// texture 1, "hero.png"
    /// texture 2, "hero_hurt.png"
    /// sprite 1, 200, 200, 1
    /// ` later, when the player gets hit
    /// set sprite texture 1, 2
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to update.</param>
    /// <param name="textureId">The ID of a previously loaded texture.</param>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SetSpriteFrame">set sprite frame</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    [FadeBasicCommand("set sprite texture")]
    public static void SetSpriteTexture(int spriteId, int textureId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        SpriteSystem.sprites[index].imageId = textureId;
    }


    /// <summary>
    /// <para>Redirects a sprite to draw on a specific render target instead of the default output.</para>
    /// <para>This replaces any previous target assignment. The sprite will only draw to the new target.</para>
    /// </summary>
    /// <remarks>
    /// By default, sprites draw to the main screen output. Use this to redirect a sprite to an off-screen
    /// buffer created with <see cref="SetRenderTargetTexture">render target</see>. This is how you build
    /// multi-pass effects, minimaps, or UI layers. The sprite's z-order only competes with other sprites
    /// on the same target. To draw a sprite on multiple targets at once, use
    /// <see cref="AddSpriteTarget">add sprite render target</see> instead. To go back to the default
    /// output, call <see cref="ResetSpriteTarget">reset sprite render target</see>.
    /// </remarks>
    /// <example>
    /// Draw a sprite to an off-screen render target for a minimap.
    /// <code>
    /// ` create a render target and draw the map icon to it
    /// render target 5, 128, 128
    /// texture 1, "map_icon.png"
    /// sprite 1, 64, 64, 1
    /// set sprite render target 1, 5
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to redirect.</param>
    /// <param name="outputId">The render target ID to draw to.</param>
    /// <seealso cref="AddSpriteTarget">add sprite render target</seealso>
    /// <seealso cref="ResetSpriteTarget">reset sprite render target</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    [FadeBasicCommand("set sprite render target")]
    public static void SetSpriteTarget(int spriteId, int outputId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);

        RenderSystem.SetSpriteToOutput(index, outputId, sprite.outputIdFlags);
        sprite.outputIdFlags = outputId;
        SpriteSystem.sprites[index] = sprite;
    }
    /// <summary>
    /// <para>Resets a sprite to draw on the default render target.</para>
    /// <para>This undoes any previous <see cref="SetSpriteTarget">set sprite render target</see> or <see cref="AddSpriteTarget">add sprite render target</see> calls.</para>
    /// </summary>
    /// <remarks>
    /// Convenience shortcut, equivalent to calling <see cref="SetSpriteTarget">set sprite render target</see>
    /// with the default output ID. Use this when you're done drawing a sprite to an off-screen buffer and
    /// want it back on the main screen.
    /// </remarks>
    /// <example>
    /// Move a sprite back to the main screen after rendering to a buffer.
    /// <code>
    /// ` redirect sprite to a render target, then reset it
    /// set sprite render target 1, 5
    /// ` ... do some off-screen rendering ...
    /// reset sprite render target 1
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to reset to the default output.</param>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    /// <seealso cref="AddSpriteTarget">add sprite render target</seealso>
    [FadeBasicCommand("reset sprite render target")]
    public static void ResetSpriteTarget(int spriteId)
    {
        SetSpriteTarget(spriteId, 1);
    }
    /// <summary>
    /// <para>Adds an additional render target for a sprite, so it draws to multiple targets at once.</para>
    /// <para>Unlike <see cref="SetSpriteTarget">set sprite render target</see>, this does not remove existing targets. It stacks.</para>
    /// </summary>
    /// <remarks>
    /// This is how you get a single sprite to appear on both the main screen and an off-screen buffer
    /// (or multiple buffers). Each call adds one more target to the sprite's output set. The sprite's
    /// z-order is evaluated independently on each target. To start fresh with a single target, use
    /// <see cref="SetSpriteTarget">set sprite render target</see> (which replaces rather than adds).
    /// To return to defaults, call <see cref="ResetSpriteTarget">reset sprite render target</see>.
    /// </remarks>
    /// <example>
    /// Draw a sprite to both the main screen and a minimap buffer.
    /// <code>
    /// ` show the player icon on the main screen and the minimap
    /// render target 5, 128, 128
    /// texture 1, "player_icon.png"
    /// sprite 1, 320, 240, 1
    /// ` add the minimap target without removing the main screen
    /// add sprite render target 1, 5
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to add a target to.</param>
    /// <param name="outputId">The render target ID to add.</param>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    /// <seealso cref="ResetSpriteTarget">reset sprite render target</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    [FadeBasicCommand("add sprite render target")]
    public static void AddSpriteTarget(int spriteId, int outputId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        RenderSystem.AddSpriteToOutput(index, outputId, sprite.outputIdFlags);
        sprite.outputIdFlags = SpriteSystem.AddIdToFlags(outputId, sprite.outputIdFlags);
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Sets the X and Y scale factors of a sprite directly.</para>
    /// <para>A scale of <c>1.0</c> is the original texture size, <c>2.0</c> doubles it, <c>0.5</c> halves it.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you want precise control over the scale multiplier. If you'd rather specify a
    /// target pixel size and let Fade figure out the scale, use <see cref="SizeSprite">size sprite</see>,
    /// <see cref="SizeSpriteAspectX">size sprite x</see>, or <see cref="SizeSpriteAspectY">size sprite y</see>
    /// instead. You can set X and Y independently to stretch or squash the sprite. Negative values will
    /// mirror the sprite (though <see cref="Flip">set sprite flip</see> is cleaner for simple flips).
    /// </remarks>
    /// <example>
    /// Double the size of a sprite uniformly.
    /// <code>
    /// ` make a sprite twice as big
    /// texture 1, "gem.png"
    /// sprite 1, 100, 100, 1
    /// scale sprite 1, 2.0, 2.0
    /// </code>
    /// </example>
    /// <example>
    /// Stretch a sprite horizontally for a squash-and-stretch effect.
    /// <code>
    /// ` squash on landing: wide and short
    /// scale sprite 1, 1.4, 0.7
    /// ` then spring back to normal
    /// scale sprite 1, 1.0, 1.0
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to scale.</param>
    /// <param name="x">Horizontal scale factor. <c>1.0</c> = original width.</param>
    /// <param name="y">Vertical scale factor. <c>1.0</c> = original height.</param>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="SizeSpriteAspectX">size sprite x</seealso>
    /// <seealso cref="SizeSpriteAspectY">size sprite y</seealso>
    /// <seealso cref="Flip">set sprite flip</seealso>
    [FadeBasicCommand("scale sprite")]
    public static void ScaleSprite(int spriteId, float x, float y)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.scale = new Vector2(x, y);
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Attaches a sprite to a transform so it follows the transform's position, rotation, and scale.</para>
    /// <para>The sprite becomes a child of the transform. Move the transform and the sprite moves with it.</para>
    /// </summary>
    /// <remarks>
    /// This is how you build hierarchical movement. For example, attaching a weapon sprite to a character
    /// transform so they move together. Create the transform first with <see cref="CreateTransform">transform</see>,
    /// then attach the sprite here. The sprite's own position becomes a local offset relative to the
    /// transform. You can also attach a collider to the same transform with
    /// <see cref="AttachColliderToTransform">attach collider to transform</see> to keep physics in sync.
    /// Call this once during setup; the attachment persists until you change it.
    /// </remarks>
    /// <example>
    /// Attach a sprite and collider to a shared transform.
    /// <code>
    /// ` create a transform and attach both a sprite and a collider
    /// transform 1
    /// texture 1, "hero.png"
    /// sprite 1, 0, 0, 1
    /// attach sprite to transform 1, 1
    /// box collider 1, 0, 0, 32, 32
    /// attach collider to transform 1, 1
    /// ` now moving the transform moves everything
    /// position transform 1, 200, 150
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to attach.</param>
    /// <param name="transformId">The transform to follow. Must be created via <see cref="CreateTransform">transform</see>.</param>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="PositionSprite">position sprite</seealso>
    [FadeBasicCommand("attach sprite to transform")]
    public static void SetSpriteRelativeToAnother(int spriteId, int transformId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.anchorTransformId = transformId;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Resizes a sprite to exact pixel dimensions by calculating the right scale internally.</para>
    /// <para>This sets X and Y scale independently, so the aspect ratio may change if the target dimensions don't match the texture's ratio.</para>
    /// </summary>
    /// <remarks>
    /// This is the easiest way to make a sprite a specific pixel size on screen. It reads the texture's
    /// frame dimensions and computes scale factors to hit the target size. If you want to preserve the
    /// aspect ratio, use <see cref="SizeSpriteAspectX">size sprite x</see> (lock width, auto height) or
    /// <see cref="SizeSpriteAspectY">size sprite y</see> (lock height, auto width) instead. For direct
    /// control over the scale multiplier itself, use <see cref="ScaleSprite">scale sprite</see>.
    /// </remarks>
    /// <example>
    /// Force a sprite to be exactly 64x64 pixels on screen.
    /// <code>
    /// ` resize a sprite to a fixed pixel size regardless of texture dimensions
    /// texture 1, "icon.png"
    /// sprite 1, 10, 10, 1
    /// size sprite 1, 64, 64
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to resize.</param>
    /// <param name="xPixels">Desired width in pixels.</param>
    /// <param name="yPixels">Desired height in pixels.</param>
    /// <seealso cref="SizeSpriteAspectX">size sprite x</seealso>
    /// <seealso cref="SizeSpriteAspectY">size sprite y</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    [FadeBasicCommand("size sprite")]
    public static void SizeSprite(int spriteId, float xPixels, float yPixels)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);

        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        var xRatio = xPixels / src.Width;
        var yRatio = yPixels / src.Height;

        sprite.scale = new Vector2(xRatio, yRatio);

        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Resizes a sprite to a target width in pixels while maintaining aspect ratio.</para>
    /// <para>The height scales uniformly with the width, so the image never stretches or squashes.</para>
    /// </summary>
    /// <remarks>
    /// This is the go-to for "make this sprite X pixels wide" without distortion. It computes the
    /// scale from the texture's frame width and applies it to both axes. If you need to lock the height
    /// instead, use <see cref="SizeSpriteAspectY">size sprite y</see>. If you want to set both width
    /// and height independently (potentially changing the aspect ratio), use
    /// <see cref="SizeSprite">size sprite</see>.
    /// </remarks>
    /// <example>
    /// Make a sprite 200 pixels wide while keeping its proportions.
    /// <code>
    /// ` set width to 200, height scales automatically
    /// texture 1, "banner.png"
    /// sprite 1, 50, 50, 1
    /// size sprite x 1, 200
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to resize.</param>
    /// <param name="xPixels">Desired width in pixels. Height adjusts automatically.</param>
    /// <seealso cref="SizeSpriteAspectY">size sprite y</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    [FadeBasicCommand("size sprite x")]
    public static void SizeSpriteAspectX(int spriteId, float xPixels)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        var xRatio = xPixels / src.Width;
        sprite.scale = new Vector2(xRatio, xRatio);

        SpriteSystem.sprites[index] = sprite;
    }
    /// <summary>
    /// <para>Resizes a sprite to a target height in pixels while maintaining aspect ratio.</para>
    /// <para>The width scales uniformly with the height, so the image never stretches or squashes.</para>
    /// </summary>
    /// <remarks>
    /// This is the counterpart to <see cref="SizeSpriteAspectX">size sprite x</see>. Use it when you
    /// want to lock the height and let the width follow. It computes the scale from the texture's frame
    /// height and applies it to both axes. For setting exact pixel dimensions on both axes independently,
    /// use <see cref="SizeSprite">size sprite</see>.
    /// </remarks>
    /// <example>
    /// Fit a sprite to a 48-pixel tall slot.
    /// <code>
    /// ` set height to 48, width scales to match
    /// texture 1, "portrait.png"
    /// sprite 1, 10, 10, 1
    /// size sprite y 1, 48
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to resize.</param>
    /// <param name="yPixels">Desired height in pixels. Width adjusts automatically.</param>
    /// <seealso cref="SizeSpriteAspectX">size sprite x</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    [FadeBasicCommand("size sprite y")]
    public static void SizeSpriteAspectY(int spriteId, float yPixels)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        var yRatio = yPixels / src.Height;

        sprite.scale = new Vector2(yRatio, yRatio);

        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Rotates a sprite to the given angle in radians.</para>
    /// <para>The sprite rotates around its offset (origin) point. By default that is the top-left corner.</para>
    /// </summary>
    /// <remarks>
    /// This sets an absolute angle, not a delta. Calling it with the same value every frame holds the
    /// rotation steady. If you want the sprite to rotate around its center, set the offset to <c>(0.5, 0.5)</c>
    /// first with <see cref="SetSpriteOffset">set sprite offset</see>. The angle is in radians; use
    /// <see cref="Rad">rad</see> to convert from degrees if needed. If the sprite is attached to a
    /// transform via <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>, this
    /// rotation is applied on top of the transform's rotation.
    /// </remarks>
    /// <example>
    /// Spin a sprite around its center continuously.
    /// <code>
    /// ` rotate a sprite around its center each frame
    /// texture 1, "star.png"
    /// sprite 1, 320, 240, 1
    /// set sprite offset 1, 0.5, 0.5
    /// angle = 0.0
    /// DO
    ///   angle = angle + 0.02
    ///   rotate sprite 1, angle
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Rotate a sprite by 45 degrees using the <see cref="Rad">rad</see> helper.
    /// <code>
    /// ` tilt a sprite 45 degrees
    /// set sprite offset 1, 0.5, 0.5
    /// rotate sprite 1, rad(45)
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to rotate.</param>
    /// <param name="angle">Rotation angle in radians. <c>0</c> is no rotation.</param>
    /// <seealso cref="SetSpriteOffset">set sprite offset</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("rotate sprite")]
    public static void RotateSprite(int spriteId, float angle)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.rotation = angle;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Sets the origin point of a sprite as a ratio of its size.</para>
    /// <para><c>(0, 0)</c> is the top-left corner, <c>(0.5, 0.5)</c> is the center, <c>(1, 1)</c> is the bottom-right. This affects both the rotation pivot and where the position anchors.</para>
    /// </summary>
    /// <remarks>
    /// By default the origin is <c>(0, 0)</c> (top-left), which means
    /// <see cref="PositionSprite">position sprite</see> places the top-left corner at the given
    /// coordinates. Set it to <c>(0.5, 0.5)</c> if you want the sprite's center at that position.
    /// This is especially important for <see cref="RotateSprite">rotate sprite</see>, which pivots
    /// around the origin. Values outside <c>0</c> to <c>1</c> are valid and shift the anchor beyond the sprite's bounds.
    /// </remarks>
    /// <example>
    /// Center a sprite's origin for rotation.
    /// <code>
    /// ` set origin to the center so rotation looks natural
    /// set sprite offset 1, 0.5, 0.5
    /// rotate sprite 1, rad(90)
    /// </code>
    /// </example>
    /// <example>
    /// Anchor a sprite from its bottom-center (useful for characters standing on a surface).
    /// <code>
    /// ` anchor at the bottom-center so the feet stay on the ground
    /// set sprite offset 1, 0.5, 1.0
    /// position sprite 1, 320, 400
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to adjust.</param>
    /// <param name="xRatio">Horizontal origin as a 0-to-1 ratio of the sprite's width.</param>
    /// <param name="yRatio">Vertical origin as a 0-to-1 ratio of the sprite's height.</param>
    /// <seealso cref="PositionSprite">position sprite</seealso>
    /// <seealso cref="RotateSprite">rotate sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("set sprite offset")]
    public static void SetSpriteOffset(int spriteId, float xRatio, float yRatio)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.origin = new Vector2(xRatio, yRatio);
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Sets the secondary texture coordinate (texcoord1) for all four vertices of a sprite at once.</para>
    /// <para>This is an advanced feature for passing custom per-sprite data to shaders. You won't need it unless you're writing custom effects.</para>
    /// </summary>
    /// <remarks>
    /// Each sprite quad has four vertices, and each vertex has a second texture coordinate slot (texcoord1)
    /// that is not used by the default rendering pipeline. When you assign a custom shader via
    /// <see cref="SetSpriteEffect">set sprite effect</see>, your shader can read these values to drive
    /// effects like dissolve thresholds, color-cycling parameters, or distortion strength. This overload
    /// sets the same value on all four corners. If you need per-corner values (e.g. for gradient effects),
    /// use <see cref="SetSpriteTexcoord1(int, int, float, float, float, float)">set sprite index texcoord1</see>.
    /// </remarks>
    /// <example>
    /// Pass a dissolve threshold to a custom shader.
    /// <code>
    /// ` set up a dissolve effect and pass the threshold via texcoord1
    /// effect 1, "dissolve.fx"
    /// set sprite effect 1, 1
    /// ` x = dissolve threshold (0.0 to 1.0), y/z/w unused
    /// set sprite all texcoord1 1, 0.5, 0.0, 0.0, 0.0
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to update.</param>
    /// <param name="x">The X component of the texcoord1 vector.</param>
    /// <param name="y">The Y component of the texcoord1 vector.</param>
    /// <param name="z">The Z component of the texcoord1 vector.</param>
    /// <param name="w">The W component of the texcoord1 vector.</param>
    /// <seealso cref="SetSpriteTexcoord1(int, int, float, float, float, float)">set sprite index texcoord1</seealso>
    /// <seealso cref="SetSpriteEffect">set sprite effect</seealso>
    /// <seealso cref="LoadEffect">effect</seealso>
    [FadeBasicCommand("set sprite all texcoord1")]
    public static void SetSpriteTexcoord1(int spriteId, float x, float y, float z, float w)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.texCoord1 = new SpriteTexCoord1(new Vector4(x, y, z, w));
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Sets the secondary texture coordinate (texcoord1) for a single corner vertex of a sprite.</para>
    /// <para>This is an advanced feature for passing per-vertex data to custom shaders. Most use cases only need <see cref="SetSpriteTexcoord1(int, float, float, float, float)">set sprite all texcoord1</see>.</para>
    /// </summary>
    /// <remarks>
    /// Each sprite is a quad with four corners. This overload lets you set a different texcoord1 value on
    /// each corner, which the GPU interpolates across the sprite's surface. This is useful for gradient-style
    /// shader effects where each corner needs a distinct value. Assign a custom shader first with
    /// <see cref="SetSpriteEffect">set sprite effect</see>, then set corner data here. Corner indices:
    /// <c>0</c> = top-left, <c>1</c> = top-right, <c>2</c> = bottom-left, <c>3</c> = bottom-right.
    /// </remarks>
    /// <example>
    /// Set up a vertical gradient by giving top corners one value and bottom corners another.
    /// <code>
    /// ` top corners get 1.0, bottom corners get 0.0 in the x channel
    /// set sprite index texcoord1 1, 0, 1.0, 0.0, 0.0, 0.0
    /// set sprite index texcoord1 1, 1, 1.0, 0.0, 0.0, 0.0
    /// set sprite index texcoord1 1, 2, 0.0, 0.0, 0.0, 0.0
    /// set sprite index texcoord1 1, 3, 0.0, 0.0, 0.0, 0.0
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to update.</param>
    /// <param name="cornerIndex">Which corner: <c>0</c> = top-left, <c>1</c> = top-right, <c>2</c> = bottom-left, <c>3</c> = bottom-right.</param>
    /// <param name="x">The X component of the texcoord1 vector.</param>
    /// <param name="y">The Y component of the texcoord1 vector.</param>
    /// <param name="z">The Z component of the texcoord1 vector.</param>
    /// <param name="w">The W component of the texcoord1 vector.</param>
    /// <seealso cref="SetSpriteTexcoord1(int, float, float, float, float)">set sprite all texcoord1</seealso>
    /// <seealso cref="SetSpriteEffect">set sprite effect</seealso>
    /// <seealso cref="LoadEffect">effect</seealso>
    [FadeBasicCommand("set sprite index texcoord1")]
    public static void SetSpriteTexcoord1(int spriteId, int cornerIndex, float x, float y, float z, float w)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        switch (cornerIndex)
        {
            case 0:
                sprite.texCoord1.tl = new Vector4(x, y, z, w);
                break;
            case 1:
                sprite.texCoord1.tr = new Vector4(x, y, z, w);
                break;
            case 2:
                sprite.texCoord1.bl = new Vector4(x, y, z, w);
                break;
            case 3:
                sprite.texCoord1.br = new Vector4(x, y, z, w);
                break;
        }
        SpriteSystem.sprites[index] = sprite;
    }


    /// <summary>
    /// <para>Assigns a custom shader effect to a sprite.</para>
    /// <para>The sprite will be drawn using this effect instead of the default pipeline. All sprites sharing an effect are batched together.</para>
    /// </summary>
    /// <remarks>
    /// Load the effect first with <see cref="LoadEffect">effect</see>, then pass its ID here. Once
    /// assigned, the sprite uses that shader every frame until you change it. You can pass per-sprite
    /// data to the shader via <see cref="SetSpriteTexcoord1(int, float, float, float, float)">set sprite all texcoord1</see>
    /// or <see cref="SetSpriteTexcoord1(int, int, float, float, float, float)">set sprite index texcoord1</see>.
    /// Sprites with the same effect are drawn together in the same batch, so grouping sprites by effect
    /// is good for performance.
    /// </remarks>
    /// <example>
    /// Apply a custom glow shader to a sprite.
    /// <code>
    /// ` load a shader and assign it to a sprite
    /// effect 1, "glow.fx"
    /// texture 1, "orb.png"
    /// sprite 1, 200, 200, 1
    /// set sprite effect 1, 1
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to apply the effect to.</param>
    /// <param name="effectId">The ID of a previously loaded effect.</param>
    /// <seealso cref="LoadEffect">effect</seealso>
    /// <seealso cref="SetSpriteTexcoord1(int, float, float, float, float)">set sprite all texcoord1</seealso>
    /// <seealso cref="SetSpriteTexcoord1(int, int, float, float, float, float)">set sprite index texcoord1</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("set sprite effect")]
    public static void SetSpriteEffect(int spriteId, int effectId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.effectId = effectId;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Sets the RGB color channels of a sprite, leaving alpha unchanged.</para>
    /// <para>Use this when you want to tint or recolor a sprite without affecting its transparency.</para>
    /// </summary>
    /// <remarks>
    /// This modifies only the red, green, and blue channels. The alpha channel stays at whatever it
    /// was before. Like <see cref="ColorSprite">color sprite</see>, these values multiply with the
    /// texture's colors. Setting all three to <c>255</c> shows the texture at full brightness. To
    /// change alpha independently, use <see cref="SetSpriteDiffuse(int, byte)">set sprite alpha</see>.
    /// To set all four channels at once with a packed integer, use <see cref="ColorSprite">color sprite</see>.
    /// </remarks>
    /// <example>
    /// Give a sprite a green tint.
    /// <code>
    /// ` tint the sprite green while keeping alpha as-is
    /// set sprite diffuse 1, 100, 255, 100
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to tint.</param>
    /// <param name="red">Red channel, <c>0</c> to <c>255</c>.</param>
    /// <param name="green">Green channel, <c>0</c> to <c>255</c>.</param>
    /// <param name="blue">Blue channel, <c>0</c> to <c>255</c>.</param>
    /// <seealso cref="SetSpriteDiffuse(int, byte)">set sprite alpha</seealso>
    /// <seealso cref="ColorSprite">color sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("set sprite diffuse")]
    public static void SetSpriteDiffuse(int spriteId, byte red, byte green, byte blue)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.color.R = red;
        sprite.color.G = green;
        sprite.color.B = blue;
        SpriteSystem.sprites[index] = sprite;
    }

    // TODO: add a "set sprite size" option that sets the effect width/height

    /// <summary>
    /// <para>Sets the transparency of a sprite.</para>
    /// <para><c>0</c> is fully transparent (invisible), <c>255</c> is fully opaque. RGB channels are not affected.</para>
    /// </summary>
    /// <remarks>
    /// This is the quickest way to fade a sprite in or out without touching its color tint. The alpha
    /// value multiplies with the texture's own alpha, so a texture pixel at 50% alpha with a sprite alpha
    /// of <c>128</c> ends up at roughly 25% opacity. To set RGB channels without touching alpha, use
    /// <see cref="SetSpriteDiffuse(int, byte, byte, byte)">set sprite diffuse</see>. To set all four
    /// channels at once, use <see cref="ColorSprite">color sprite</see>.
    /// </remarks>
    /// <example>
    /// Fade a sprite in from fully transparent to fully opaque.
    /// <code>
    /// ` gradually fade in a sprite over many frames
    /// texture 1, "title.png"
    /// sprite 1, 200, 100, 1
    /// set sprite alpha 1, 0
    /// alpha = 0
    /// DO
    ///   IF alpha &lt; 255
    ///     alpha = alpha + 3
    ///     IF alpha &gt; 255 THEN alpha = 255
    ///     set sprite alpha 1, alpha
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Make a sprite semi-transparent for a ghost effect.
    /// <code>
    /// ` 50% transparency
    /// set sprite alpha 1, 128
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to adjust.</param>
    /// <param name="alpha">Alpha value, <c>0</c> to <c>255</c>. <c>0</c> = transparent, <c>255</c> = opaque.</param>
    /// <seealso cref="SetSpriteDiffuse(int, byte, byte, byte)">set sprite diffuse</seealso>
    /// <seealso cref="ColorSprite">color sprite</seealso>
    /// <seealso cref="HideSprite">hide sprite</seealso>
    /// <seealso cref="ShowSprite">show sprite</seealso>
    [FadeBasicCommand("set sprite alpha")]
    public static void SetSpriteDiffuse(int spriteId, byte alpha)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.color.A = alpha;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Selects which frame of a spritesheet to display on a sprite.</para>
    /// <para>The texture must have its frame grid set up first via <see cref="SetTextureFramesByRowCol">set texture frame grid</see>, or this won't do anything useful.</para>
    /// </summary>
    /// <remarks>
    /// Frame indices are zero-based and count left-to-right, top-to-bottom across the grid. You can
    /// query how many frames a texture has with <see cref="GetTextureFrameCount">texture frames</see>.
    /// Call this every frame (or whenever the animation advances) to animate a sprite through its
    /// spritesheet. If the sprite's texture is a single image with no frame grid, frame <c>0</c> shows
    /// the whole texture.
    /// </remarks>
    /// <example>
    /// Animate a sprite by cycling through frames.
    /// <code>
    /// ` set up a 4x4 spritesheet and animate it
    /// texture 1, "walk.png"
    /// set texture frame grid 1, 4, 4
    /// sprite 1, 200, 200, 1
    /// frame = 0
    /// totalFrames = texture frames(1)
    /// timer = 0
    /// DO
    ///   timer = timer + 1
    ///   IF timer &gt; 5
    ///     timer = 0
    ///     frame = frame + 1
    ///     IF frame &gt;= totalFrames THEN frame = 0
    ///     set sprite frame 1, frame
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to update.</param>
    /// <param name="frameId">Zero-based frame index into the texture's frame grid.</param>
    /// <seealso cref="SetTextureFramesByRowCol">set texture frame grid</seealso>
    /// <seealso cref="SetSpriteTexture">set sprite texture</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("set sprite frame")]
    public static void SetSpriteFrame(int spriteId, int frameId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        sprite.currentFrame = frameId;
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// <para>Flips a sprite horizontally, vertically, or both.</para>
    /// <para>Pass <c>1</c> to flip an axis, <c>0</c> for normal. This is a visual flip only. Position and offset are not affected.</para>
    /// </summary>
    /// <remarks>
    /// This is the cleanest way to mirror a sprite (e.g. flipping a character to face left vs. right).
    /// It's cheaper and simpler than using negative scale values via <see cref="ScaleSprite">scale sprite</see>.
    /// Both axes can be flipped simultaneously by passing <c>1</c> for both parameters. The flip is
    /// applied after rotation, so a rotated + flipped sprite may look different than a flipped + rotated one.
    /// </remarks>
    /// <example>
    /// Flip a character sprite to face left when moving left.
    /// <code>
    /// ` flip based on movement direction
    /// IF left key(1)
    ///   set sprite flip 1, 1, 0
    ///   px = px - 2
    /// ENDIF
    /// IF right key(1)
    ///   set sprite flip 1, 0, 0
    ///   px = px + 2
    /// ENDIF
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to flip.</param>
    /// <param name="flipHorizontal"><c>1</c> to flip horizontally, <c>0</c> for normal.</param>
    /// <param name="flipVertical"><c>1</c> to flip vertically, <c>0</c> for normal.</param>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    /// <seealso cref="RotateSprite">rotate sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("set sprite flip")]
    public static void Flip(int spriteId, int flipHorizontal, int flipVertical)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        switch (flipHorizontal + flipVertical * 2)
        {
            case 0:
                sprite.effects = SpriteEffects.None;
                break;
            case 1:
                sprite.effects = SpriteEffects.FlipHorizontally;
                break;
            case 2:
                sprite.effects = SpriteEffects.FlipVertically;
                break;
            case 3:
                sprite.effects = SpriteEffects.FlipVertically | SpriteEffects.FlipVertically;
                break;
        }
        SpriteSystem.sprites[index] = sprite;

    }

    /// <summary>
    /// <para>Returns the width of the sprite's current texture frame in pixels, before any scaling is applied.</para>
    /// <para>If the texture uses a frame grid, this returns the width of a single frame, not the whole texture.</para>
    /// </summary>
    /// <remarks>
    /// Use this to get the raw pixel dimensions of what the sprite is displaying. This is the base
    /// measurement that <see cref="ScaleSprite">scale sprite</see> multiplies against. If you need the
    /// on-screen size, multiply this by the sprite's current X scale. Pair with
    /// <see cref="GetSpriteHeight">sprite height</see> for both dimensions.
    /// </remarks>
    /// <example>
    /// Center a sprite based on its width.
    /// <code>
    /// ` place a sprite so its center is at screen X = 320
    /// texture 1, "logo.png"
    /// sprite 1, 0, 100, 1
    /// w = sprite width(1)
    /// position sprite 1, 320 - w / 2, 100
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to measure.</param>
    /// <returns>Width of the current frame in pixels (before scaling).</returns>
    /// <seealso cref="GetSpriteHeight">sprite height</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="PositionSprite">position sprite</seealso>
    [FadeBasicCommand("sprite width")]
    public static float GetSpriteWidth(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        return src.Width;
    }
    /// <summary>
    /// <para>Returns the height of the sprite's current texture frame in pixels, before any scaling is applied.</para>
    /// <para>If the texture uses a frame grid, this returns the height of a single frame, not the whole texture.</para>
    /// </summary>
    /// <remarks>
    /// Use this to get the raw pixel dimensions of what the sprite is displaying. This is the base
    /// measurement that <see cref="ScaleSprite">scale sprite</see> multiplies against. If you need the
    /// on-screen size, multiply this by the sprite's current Y scale. Pair with
    /// <see cref="GetSpriteWidth">sprite width</see> for both dimensions.
    /// </remarks>
    /// <example>
    /// Stack two sprites vertically using their heights.
    /// <code>
    /// ` place sprite 2 directly below sprite 1
    /// h = sprite height(1)
    /// y1 = sprite y(1)
    /// position sprite 2, sprite x(1), y1 + h
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to measure.</param>
    /// <returns>Height of the current frame in pixels (before scaling).</returns>
    /// <seealso cref="GetSpriteWidth">sprite width</seealso>
    /// <seealso cref="ScaleSprite">scale sprite</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="SpriteY">sprite y</seealso>
    [FadeBasicCommand("sprite height")]
    public static float GetSpriteHeight(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTexture);

        var src = TextureSystem.GetSourceRect(ref runtimeTexture, ref sprite);
        return src.Height;
    }


    /// <summary>
    /// <para>Returns the current X position of a sprite.</para>
    /// <para>This is the position last set by <see cref="Sprite">sprite</see> or <see cref="PositionSprite">position sprite</see>. It does not include transform offsets.</para>
    /// </summary>
    /// <remarks>
    /// If the sprite is attached to a transform via <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>,
    /// this returns the sprite's local position, not its final on-screen position. Pair with
    /// <see cref="SpriteY">sprite y</see> for the full coordinate.
    /// </remarks>
    /// <example>
    /// Read a sprite's position and print it.
    /// <code>
    /// ` check where a sprite is
    /// px = sprite x(1)
    /// py = sprite y(1)
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to query.</param>
    /// <returns>The X position in screen coordinates (or local coordinates if attached to a transform).</returns>
    /// <seealso cref="SpriteY">sprite y</seealso>
    /// <seealso cref="PositionSprite">position sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    [FadeBasicCommand("sprite x")]
    public static float SpriteX(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        return sprite.position.X;
    }

    /// <summary>
    /// <para>Returns the current Y position of a sprite.</para>
    /// <para>This is the position last set by <see cref="Sprite">sprite</see> or <see cref="PositionSprite">position sprite</see>. It does not include transform offsets.</para>
    /// </summary>
    /// <remarks>
    /// If the sprite is attached to a transform via <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>,
    /// this returns the sprite's local position, not its final on-screen position. Pair with
    /// <see cref="SpriteX">sprite x</see> for the full coordinate.
    /// </remarks>
    /// <example>
    /// Clamp a sprite so it cannot move off the bottom of the screen.
    /// <code>
    /// ` keep the sprite above the screen floor
    /// py = sprite y(1)
    /// IF py &gt; 440 THEN position sprite 1, sprite x(1), 440
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to query.</param>
    /// <returns>The Y position in screen coordinates (or local coordinates if attached to a transform).</returns>
    /// <seealso cref="SpriteX">sprite x</seealso>
    /// <seealso cref="PositionSprite">position sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    [FadeBasicCommand("sprite y")]
    public static float SpriteY(int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        return sprite.position.Y;
    }
}
