using Fade.MonoGame.Core;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    private static Color UnpackGizmoColor(int packedColor)
    {
        ColorUtil.UnpackColor(packedColor, out var r, out var g, out var b, out var a);
        return new Color(r, g, b, a);
    }

    // -----------------------------------------------------------------
    // System-wide gizmo switch
    // -----------------------------------------------------------------

    /// <summary>
    /// Turns the gizmo overlay system on globally.
    ///
    /// All gizmos (sprite, collider, text outlines plus any queued
    /// <see cref="GizmoLine">gizmo line</see> / <see cref="GizmoRect">gizmo rect</see> shapes)
    /// resume drawing on the next frame. Gizmos are enabled by default
    /// when a program starts, so the typical use of this command is to
    /// flip them back on after a previous <see cref="DisableGizmos">disable gizmos</see> call.
    /// </summary>
    /// <remarks>
    /// Per-entity enable/disable state is preserved — flipping the
    /// system-wide switch off and on again restores whatever sprite/
    /// collider/text gizmos you had configured.
    ///
    /// Use this for a "debug mode" toggle in shipping builds: bind a
    /// keypress to call this command (or pair it with
    /// <see cref="DisableGizmos">disable gizmos</see>) so end-users can
    /// peek at the debug overlay on demand without you having to wire
    /// up every individual gizmo command.
    /// </remarks>
    /// <example>
    /// Toggle the whole gizmo overlay with the G key:
    /// <code>
    /// ` load a ghost and outline it with a sprite gizmo
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// enable sprite gizmo 1
    /// ` start with the whole overlay hidden
    /// disable gizmos
    /// shown = 0
    /// gKey = scanCode("G")
    /// DO
    ///   IF new key down(gKey) = 1
    ///     IF shown = 0
    ///       enable gizmos
    ///       shown = 1
    ///     ELSE
    ///       disable gizmos
    ///       shown = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="DisableGizmos">disable gizmos</seealso>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    [FadeBasicCommand("enable gizmos")]
    public static void EnableGizmos()
    {
        GizmoSystem.gizmosEnabled = true;
    }

    /// <summary>
    /// Turns the gizmo overlay system off globally.
    ///
    /// Every gizmo (sprite outlines, collider outlines, text outlines,
    /// queued <see cref="GizmoLine">gizmo line</see> / <see cref="GizmoRect">gizmo rect</see>
    /// shapes) stops drawing. Per-entity enable state is preserved so
    /// <see cref="EnableGizmos">enable gizmos</see> brings them all back.
    /// </summary>
    /// <remarks>
    /// Useful in shipping builds where you want gizmo code paths to
    /// stay in place but the overlay hidden from end-users. The
    /// per-entity gizmo dictionaries are untouched — only the render
    /// pass is short-circuited.
    /// </remarks>
    /// <example>
    /// Hide all gizmos before shipping but leave them ready to flip
    /// back on for support purposes:
    /// <code>
    /// ` load a ghost and outline it with a sprite gizmo
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// enable sprite gizmo 1
    /// ` hide every gizmo, but keep the per-sprite setup ready
    /// disable gizmos
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="EnableGizmos">enable gizmos</seealso>
    [FadeBasicCommand("disable gizmos")]
    public static void DisableGizmos()
    {
        GizmoSystem.gizmosEnabled = false;
    }

    // -----------------------------------------------------------------
    // Retained: sprite gizmos
    // -----------------------------------------------------------------

    /// <summary>
    /// Draws a debug outline around a sprite every frame, following its position, rotation, scale, and any attached transform.
    ///
    /// Gizmos always draw on top of the game and aren't affected by any screen effect, so they stay visible regardless of what your sprites or post-processing are doing. The outline keeps drawing until you call <see cref="DisableSpriteGizmo">disable sprite gizmo</see>.
    /// </summary>
    /// <remarks>
    /// This is the easiest way to see exactly where a sprite is sitting on screen, including the effects of rotation and origin offsets. It's especially handy when you're trying to figure out why a sprite isn't lining up with a collider, or why a rotation is pivoting around the wrong point.
    ///
    /// Enabling the same sprite twice is a no-op. If a gizmo is already enabled for that sprite, this command doesn't change its color or thickness. To change those after enabling, use <see cref="SetSpriteGizmoColor">set sprite gizmo color</see> and <see cref="SetSpriteGizmoThickness">set sprite gizmo thickness</see>.
    ///
    /// Both optional parameters use <c>0</c> as a sentinel for "use the default" — white at thickness <c>1</c>. Pass a real packed color (from <see cref="Rgb">rgb</see>) and a positive thickness to override on creation.
    ///
    /// Gizmos draw in world space (the same coordinate system as the sprite itself). They render after the screen effect composite, so a fullscreen shader won't tint or distort the outline.
    /// </remarks>
    /// <example>
    /// Outline a sprite so you can see its bounds while moving it around:
    /// <code>
    /// ` load the ghost image and draw it as a sprite
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// ` outline the sprite so you can see its bounds
    /// enable sprite gizmo 1
    /// DO
    ///   position sprite 1, mouse x(), mouse y()
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Enable a thicker red outline in one call:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// ` thickness 3, red opaque
    /// enable sprite gizmo 1, 3, rgb(255, 0, 0)
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to outline. Must have been created with <see cref="Sprite">sprite</see>.</param>
    /// <param name="thickness">Line thickness in pixels. Pass <c>0</c> to use the default (<c>1</c>).</param>
    /// <param name="colorCode">A packed RGBA color value. Pass <c>0</c> to use the default (opaque white). Use <see cref="Rgb">rgb</see> to build a custom one.</param>
    /// <seealso cref="DisableSpriteGizmo">disable sprite gizmo</seealso>
    /// <seealso cref="SetSpriteGizmoColor">set sprite gizmo color</seealso>
    /// <seealso cref="SetSpriteGizmoThickness">set sprite gizmo thickness</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("enable sprite gizmo")]
    public static void EnableSpriteGizmo(int spriteId, int thickness=0, int colorCode=0)
    {
        if (!GizmoSystem.spriteGizmos.ContainsKey(spriteId))
        {
            GizmoSystem.spriteGizmos[spriteId] = new SpriteGizmo
            {
                color = colorCode == 0 ? GizmoSystem.DefaultColor : UnpackGizmoColor(colorCode),
                thickness = thickness == 0 ? GizmoSystem.DefaultThickness : thickness,
            };
        }
    }

    /// <summary>
    /// Turns off the debug outline for a sprite.
    ///
    /// Safe to call even if no gizmo is currently enabled for that sprite. The sprite itself is untouched.
    /// </summary>
    /// <remarks>
    /// Use this when you're done debugging, or to toggle a gizmo on and off as a player option. The sprite keeps drawing normally — only the gizmo overlay disappears.
    ///
    /// If you just want to temporarily hide gizmos but keep their settings, there's no separate "pause" command — disable and re-<see cref="EnableSpriteGizmo">enable sprite gizmo</see> with the same color and thickness.
    /// </remarks>
    /// <example>
    /// Toggle a sprite's gizmo with a key press:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// showing = 0
    /// gKey = scanCode("G")
    /// DO
    ///   IF new key down(gKey) = 1
    ///     IF showing = 0
    ///       enable sprite gizmo 1
    ///       showing = 1
    ///     ELSE
    ///       disable sprite gizmo 1
    ///       showing = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite whose gizmo should turn off.</param>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="DisableColliderGizmo">disable collider gizmo</seealso>
    /// <seealso cref="DisableTextGizmo">disable text gizmo</seealso>
    [FadeBasicCommand("disable sprite gizmo")]
    public static void DisableSpriteGizmo(int spriteId)
    {
        GizmoSystem.spriteGizmos.Remove(spriteId);
    }

    /// <summary>
    /// Returns whether a sprite currently has its gizmo outline enabled.
    ///
    /// Returns <c>1</c> if <see cref="EnableSpriteGizmo">enable sprite gizmo</see> has been called for this sprite (and not subsequently disabled), <c>0</c> otherwise. Useful when wiring up a toggle key without keeping a separate flag variable in sync.
    /// </summary>
    /// <remarks>
    /// This only reflects per-sprite state. The system-wide <see cref="DisableGizmos">disable gizmos</see> switch can hide the outline even when this returns <c>1</c> — the per-entity bit is preserved across the system-wide toggle so flipping the system back on restores the outline without you having to re-enable each sprite.
    ///
    /// Calling this on a sprite that doesn't exist (or was never gizmo-enabled) safely returns <c>0</c>.
    /// </remarks>
    /// <example>
    /// Toggle a sprite's gizmo without tracking the on/off state yourself:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// gKey = scanCode("G")
    /// DO
    ///   IF new key down(gKey) = 1
    ///     IF get sprite gizmo enabled(1) = 1
    ///       disable sprite gizmo 1
    ///     ELSE
    ///       enable sprite gizmo 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to query.</param>
    /// <returns><c>1</c> when the sprite has a gizmo registered, <c>0</c> otherwise.</returns>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="DisableSpriteGizmo">disable sprite gizmo</seealso>
    /// <seealso cref="GetColliderGizmoEnabled">get collider gizmo enabled</seealso>
    /// <seealso cref="GetTextGizmoEnabled">get text gizmo enabled</seealso>
    [FadeBasicCommand("get sprite gizmo enabled")]
    public static int GetSpriteGizmoEnabled(int spriteId)
    {
        return GizmoSystem.spriteGizmos.ContainsKey(spriteId) ? 1 : 0;
    }

    /// <summary>
    /// Changes the color of a sprite's gizmo outline.
    ///
    /// If the sprite doesn't have a gizmo enabled yet, this enables one with default thickness and the given color.
    /// </summary>
    /// <remarks>
    /// Use this to color-code different sprites — for example, red outlines for enemies and green for pickups. The color change takes effect on the next frame.
    ///
    /// Calling this on a sprite that hasn't been gizmo-enabled is allowed and will create the gizmo for you. That's a small convenience so a setup script doesn't need to remember the order of calls.
    /// </remarks>
    /// <example>
    /// Tint a sprite's outline red:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 100, 100, 1
    /// enable sprite gizmo 1
    /// ` tint the outline red to mark it as an enemy
    /// set sprite gizmo color 1, rgb(255, 0, 0)
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite whose gizmo should change color.</param>
    /// <param name="packedColor">A packed RGBA color value. Use <see cref="Rgb">rgb</see> to build one.</param>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="SetSpriteGizmoThickness">set sprite gizmo thickness</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("set sprite gizmo color")]
    public static void SetSpriteGizmoColor(int spriteId, int packedColor)
    {
        if (!GizmoSystem.spriteGizmos.TryGetValue(spriteId, out var view))
        {
            view = new SpriteGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
        }
        view.color = UnpackGizmoColor(packedColor);
        GizmoSystem.spriteGizmos[spriteId] = view;
    }

    /// <summary>
    /// Changes the line thickness of a sprite's gizmo outline.
    ///
    /// If the sprite doesn't have a gizmo enabled yet, this enables one with the default color and the given thickness.
    /// </summary>
    /// <remarks>
    /// Useful when an outline is hard to see against a busy background, or when you want one specific sprite's gizmo to stand out. The unit is render-buffer pixels — a thickness of <c>2</c> means a 2-pixel-wide outline in the same coordinate space as <see cref="GetRenderWidth">render width</see>.
    ///
    /// A thickness of <c>0</c> or less will skip drawing the outline entirely.
    /// </remarks>
    /// <example>
    /// Give a player sprite a thicker outline than the rest:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// enable sprite gizmo 1
    /// ` make this outline 3 pixels wide so it stands out
    /// set sprite gizmo thickness 1, 3.0
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite whose gizmo thickness should change.</param>
    /// <param name="thickness">Line thickness in pixels. Values <c>1</c> through <c>4</c> are typical.</param>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="SetSpriteGizmoColor">set sprite gizmo color</seealso>
    [FadeBasicCommand("set sprite gizmo thickness")]
    public static void SetSpriteGizmoThickness(int spriteId, float thickness)
    {
        if (!GizmoSystem.spriteGizmos.TryGetValue(spriteId, out var view))
        {
            view = new SpriteGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
        }
        view.thickness = thickness;
        GizmoSystem.spriteGizmos[spriteId] = view;
    }

    // -----------------------------------------------------------------
    // Retained: collider gizmos
    // -----------------------------------------------------------------

    /// <summary>
    /// Draws a debug outline around a collider every frame, following its position, size, and any attached transform.
    ///
    /// Gizmos always draw on top of the game and aren't affected by any screen effect. The outline keeps drawing until you call <see cref="DisableColliderGizmo">disable collider gizmo</see>.
    /// </summary>
    /// <remarks>
    /// Collider gizmos make it obvious whether a hitbox actually lines up with its visual sprite, which is by far the most common source of "why doesn't this collision register?" bugs. Pair this with <see cref="EnableSpriteGizmo">enable sprite gizmo</see> on the same object and you can see at a glance if the sprite and collider have drifted apart.
    ///
    /// Enabling the same collider twice is a no-op. If a gizmo is already enabled for that collider, this command doesn't change its color or thickness. Use <see cref="SetColliderGizmoColor">set collider gizmo color</see> and <see cref="SetColliderGizmoThickness">set collider gizmo thickness</see> to update an existing one.
    ///
    /// Both optional parameters use <c>0</c> as a sentinel for "use the default" — white at thickness <c>1</c>. Colliders are axis-aligned, so the outline is always a regular rectangle even if the parent transform has rotation applied (rotation shifts the collider's center but doesn't rotate the bounds, matching how the collision system itself behaves).
    /// </remarks>
    /// <example>
    /// Show a collider outline next to its sprite to confirm they line up:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// enable sprite gizmo 1
    /// ` add a hitbox and outline it too, so you can compare the two
    /// box collider 1, 200, 200, 32, 32
    /// enable collider gizmo 1
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Enable a thicker green outline in one call:
    /// <code>
    /// box collider 5, 100, 100, 32, 32
    /// ` thickness 2, green opaque
    /// enable collider gizmo 5, 2, rgb(0, 255, 0)
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider to outline. Must have been created with <see cref="CreateBoxCollider">box collider</see>.</param>
    /// <param name="thickness">Line thickness in pixels. Pass <c>0</c> to use the default (<c>1</c>).</param>
    /// <param name="colorCode">A packed RGBA color value. Pass <c>0</c> to use the default (opaque white). Use <see cref="Rgb">rgb</see> to build a custom one.</param>
    /// <seealso cref="DisableColliderGizmo">disable collider gizmo</seealso>
    /// <seealso cref="SetColliderGizmoColor">set collider gizmo color</seealso>
    /// <seealso cref="SetColliderGizmoThickness">set collider gizmo thickness</seealso>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("enable collider gizmo")]
    public static void EnableColliderGizmo(int colliderId, int thickness=0, int colorCode=0)
    {
        if (!GizmoSystem.colliderGizmos.ContainsKey(colliderId))
        {
            GizmoSystem.colliderGizmos[colliderId] = new ColliderGizmo
            {
                color = colorCode == 0 ? GizmoSystem.DefaultColor : UnpackGizmoColor(colorCode),
                thickness = thickness == 0 ? GizmoSystem.DefaultThickness : thickness,
            };
        }
    }

    /// <summary>
    /// Turns off the debug outline for a collider.
    ///
    /// Safe to call even if no gizmo is currently enabled for that collider. The collider itself is untouched.
    /// </summary>
    /// <remarks>
    /// Use this when you're done debugging, or to toggle a gizmo on and off as a player option. Collision detection keeps running normally — only the outline overlay disappears.
    /// </remarks>
    /// <example>
    /// Toggle a collider's gizmo with a key press:
    /// <code>
    /// box collider 1, 100, 100, 32, 32
    /// showing = 0
    /// cKey = scanCode("C")
    /// DO
    ///   IF new key down(cKey) = 1
    ///     IF showing = 0
    ///       enable collider gizmo 1
    ///       showing = 1
    ///     ELSE
    ///       disable collider gizmo 1
    ///       showing = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider whose gizmo should turn off.</param>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="DisableSpriteGizmo">disable sprite gizmo</seealso>
    /// <seealso cref="DisableTextGizmo">disable text gizmo</seealso>
    [FadeBasicCommand("disable collider gizmo")]
    public static void DisableColliderGizmo(int colliderId)
    {
        GizmoSystem.colliderGizmos.Remove(colliderId);
    }

    /// <summary>
    /// Returns whether a collider currently has its gizmo outline enabled.
    ///
    /// Returns <c>1</c> if <see cref="EnableColliderGizmo">enable collider gizmo</see> has been called for this collider (and not subsequently disabled), <c>0</c> otherwise. Useful when wiring up a toggle key or a debug-overlay query without keeping a separate flag variable in sync.
    /// </summary>
    /// <remarks>
    /// This only reflects per-collider state. The system-wide <see cref="DisableGizmos">disable gizmos</see> switch can hide the outline even when this returns <c>1</c> — the per-entity bit is preserved so flipping the system back on restores all enabled outlines.
    ///
    /// Calling this on a collider that doesn't exist (or was never gizmo-enabled) safely returns <c>0</c>.
    /// </remarks>
    /// <example>
    /// Show or hide a collider's outline depending on whether it's currently visible:
    /// <code>
    /// box collider 1, 100, 100, 32, 32
    /// cKey = scanCode("C")
    /// DO
    ///   IF new key down(cKey) = 1
    ///     IF get collider gizmo enabled(1) = 1
    ///       disable collider gizmo 1
    ///     ELSE
    ///       enable collider gizmo 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider to query.</param>
    /// <returns><c>1</c> when the collider has a gizmo registered, <c>0</c> otherwise.</returns>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="DisableColliderGizmo">disable collider gizmo</seealso>
    /// <seealso cref="GetSpriteGizmoEnabled">get sprite gizmo enabled</seealso>
    /// <seealso cref="GetTextGizmoEnabled">get text gizmo enabled</seealso>
    [FadeBasicCommand("get collider gizmo enabled")]
    public static int GetColliderGizmoEnabled(int colliderId)
    {
        return GizmoSystem.colliderGizmos.ContainsKey(colliderId) ? 1 : 0;
    }

    /// <summary>
    /// Changes the color of a collider's gizmo outline.
    ///
    /// If the collider doesn't have a gizmo enabled yet, this enables one with default thickness and the given color.
    /// </summary>
    /// <remarks>
    /// Color-coding colliders by role — red for hostile, green for pickups, blue for triggers — makes a busy debug scene readable at a glance. The color change takes effect on the next frame.
    ///
    /// Calling this on a collider that hasn't been gizmo-enabled is allowed and will create the gizmo for you.
    /// </remarks>
    /// <example>
    /// Tint a hazard collider's outline red:
    /// <code>
    /// box collider 5, 300, 200, 64, 16
    /// enable collider gizmo 5
    /// ` tint the hazard hitbox red
    /// set collider gizmo color 5, rgb(255, 0, 0)
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider whose gizmo should change color.</param>
    /// <param name="packedColor">A packed RGBA color value. Use <see cref="Rgb">rgb</see> to build one.</param>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="SetColliderGizmoThickness">set collider gizmo thickness</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("set collider gizmo color")]
    public static void SetColliderGizmoColor(int colliderId, int packedColor)
    {
        if (!GizmoSystem.colliderGizmos.TryGetValue(colliderId, out var view))
        {
            view = new ColliderGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
        }
        view.color = UnpackGizmoColor(packedColor);
        GizmoSystem.colliderGizmos[colliderId] = view;
    }

    /// <summary>
    /// Changes the line thickness of a collider's gizmo outline.
    ///
    /// If the collider doesn't have a gizmo enabled yet, this enables one with the default color and the given thickness.
    /// </summary>
    /// <remarks>
    /// Bumping the thickness up is helpful when colliders are tiny or layered over busy art and the default 1-pixel outline gets lost. The unit is render-buffer pixels.
    ///
    /// A thickness of <c>0</c> or less will skip drawing the outline entirely.
    /// </remarks>
    /// <example>
    /// Give one important collider a thicker outline so it stands out:
    /// <code>
    /// box collider 1, 100, 100, 32, 32
    /// enable collider gizmo 1
    /// ` make this hitbox outline 3 pixels wide
    /// set collider gizmo thickness 1, 3.0
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider whose gizmo thickness should change.</param>
    /// <param name="thickness">Line thickness in pixels. Values <c>1</c> through <c>4</c> are typical.</param>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="SetColliderGizmoColor">set collider gizmo color</seealso>
    [FadeBasicCommand("set collider gizmo thickness")]
    public static void SetColliderGizmoThickness(int colliderId, float thickness)
    {
        if (!GizmoSystem.colliderGizmos.TryGetValue(colliderId, out var view))
        {
            view = new ColliderGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
        }
        view.thickness = thickness;
        GizmoSystem.colliderGizmos[colliderId] = view;
    }

    // -----------------------------------------------------------------
    // Retained: text gizmos
    // -----------------------------------------------------------------

    /// <summary>
    /// Draws a debug outline around a text sprite's measured bounding box every frame.
    ///
    /// The outline follows the text's position, rotation, scale, origin, and any attached transform, and it always draws on top of the game without being affected by any screen effect.
    /// </summary>
    /// <remarks>
    /// Text bounds aren't always obvious — fonts have ascenders, descenders, and padding that don't match the visible glyphs exactly. This gizmo shows the rectangle the font reports for the current string, which is what alignment commands and origin offsets actually anchor against.
    ///
    /// Enabling the same text sprite twice is a no-op. Use <see cref="SetTextGizmoColor">set text gizmo color</see> and <see cref="SetTextGizmoThickness">set text gizmo thickness</see> to change an existing one.
    ///
    /// Both optional parameters use <c>0</c> as a sentinel for "use the default" — white at thickness <c>1</c>.
    /// </remarks>
    /// <example>
    /// Outline some text so you can see where its bounds actually land:
    /// <code>
    /// font 1, "font"
    /// text 1, 550, 280, 1, "hello"
    /// ` outline the measured text bounds
    /// enable text gizmo 1
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Enable a thicker yellow outline in one call:
    /// <code>
    /// font 1, "font"
    /// text 2, 650, 380, 1, "warning"
    /// ` thickness 2, yellow opaque
    /// enable text gizmo 2, 2, rgb(255, 255, 0)
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite to outline. Must have been created with the <c>text</c> command.</param>
    /// <param name="thickness">Line thickness in pixels. Pass <c>0</c> to use the default (<c>1</c>).</param>
    /// <param name="colorCode">A packed RGBA color value. Pass <c>0</c> to use the default (opaque white). Use <see cref="Rgb">rgb</see> to build a custom one.</param>
    /// <seealso cref="DisableTextGizmo">disable text gizmo</seealso>
    /// <seealso cref="SetTextGizmoColor">set text gizmo color</seealso>
    /// <seealso cref="SetTextGizmoThickness">set text gizmo thickness</seealso>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("enable text gizmo")]
    public static void EnableTextGizmo(int textId, int thickness=0, int colorCode=0)
    {
        if (!GizmoSystem.textGizmos.ContainsKey(textId))
        {
            GizmoSystem.textGizmos[textId] = new TextGizmo
            {
                color = colorCode == 0 ? GizmoSystem.DefaultColor : UnpackGizmoColor(colorCode),
                thickness = thickness == 0 ? GizmoSystem.DefaultThickness : thickness,
            };
        }
    }

    /// <summary>
    /// Turns off the debug outline for a text sprite.
    ///
    /// Safe to call even if no gizmo is currently enabled for that text. The text itself keeps drawing normally.
    /// </summary>
    /// <remarks>
    /// Use this when you're done debugging text bounds, or to toggle the outline on and off as a player option.
    /// </remarks>
    /// <example>
    /// Toggle a text gizmo with a key press:
    /// <code>
    /// font 1, "font"
    /// text 1, 550, 280, 1, "hello"
    /// showing = 0
    /// tKey = scanCode("T")
    /// DO
    ///   IF new key down(tKey) = 1
    ///     IF showing = 0
    ///       enable text gizmo 1
    ///       showing = 1
    ///     ELSE
    ///       disable text gizmo 1
    ///       showing = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite whose gizmo should turn off.</param>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    /// <seealso cref="DisableSpriteGizmo">disable sprite gizmo</seealso>
    /// <seealso cref="DisableColliderGizmo">disable collider gizmo</seealso>
    [FadeBasicCommand("disable text gizmo")]
    public static void DisableTextGizmo(int textId)
    {
        GizmoSystem.textGizmos.Remove(textId);
    }

    /// <summary>
    /// Returns whether a text sprite currently has its gizmo outline enabled.
    ///
    /// Returns <c>1</c> if <see cref="EnableTextGizmo">enable text gizmo</see> has been called for this text sprite (and not subsequently disabled), <c>0</c> otherwise. Useful when wiring up a toggle key or a debug-overlay query without keeping a separate flag variable in sync.
    /// </summary>
    /// <remarks>
    /// This only reflects per-text state. The system-wide <see cref="DisableGizmos">disable gizmos</see> switch can hide the outline even when this returns <c>1</c> — the per-entity bit is preserved so flipping the system back on restores all enabled outlines.
    ///
    /// Calling this on a text sprite that doesn't exist (or was never gizmo-enabled) safely returns <c>0</c>.
    /// </remarks>
    /// <example>
    /// Toggle a text label's outline without tracking the on/off state yourself:
    /// <code>
    /// font 1, "font"
    /// text 1, 550, 280, 1, "hello"
    /// tKey = scanCode("T")
    /// DO
    ///   IF new key down(tKey) = 1
    ///     IF get text gizmo enabled(1) = 1
    ///       disable text gizmo 1
    ///     ELSE
    ///       enable text gizmo 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite to query.</param>
    /// <returns><c>1</c> when the text has a gizmo registered, <c>0</c> otherwise.</returns>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    /// <seealso cref="DisableTextGizmo">disable text gizmo</seealso>
    /// <seealso cref="GetSpriteGizmoEnabled">get sprite gizmo enabled</seealso>
    /// <seealso cref="GetColliderGizmoEnabled">get collider gizmo enabled</seealso>
    [FadeBasicCommand("get text gizmo enabled")]
    public static int GetTextGizmoEnabled(int textId)
    {
        return GizmoSystem.textGizmos.ContainsKey(textId) ? 1 : 0;
    }

    /// <summary>
    /// Changes the color of a text sprite's gizmo outline.
    ///
    /// If the text doesn't have a gizmo enabled yet, this enables one with default thickness and the given color.
    /// </summary>
    /// <remarks>
    /// Use this to distinguish different text labels at a glance when you have several on screen at once. The color change takes effect on the next frame.
    ///
    /// Calling this on a text sprite that hasn't been gizmo-enabled is allowed and will create the gizmo for you.
    /// </remarks>
    /// <example>
    /// Tint a debug label's outline cyan:
    /// <code>
    /// font 1, "font"
    /// text 1, 550, 280, 1, "score: 0"
    /// enable text gizmo 1
    /// ` tint the label outline cyan
    /// set text gizmo color 1, rgb(0, 255, 255)
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite whose gizmo should change color.</param>
    /// <param name="packedColor">A packed RGBA color value. Use <see cref="Rgb">rgb</see> to build one.</param>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    /// <seealso cref="SetTextGizmoThickness">set text gizmo thickness</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("set text gizmo color")]
    public static void SetTextGizmoColor(int textId, int packedColor)
    {
        if (!GizmoSystem.textGizmos.TryGetValue(textId, out var view))
        {
            view = new TextGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
        }
        view.color = UnpackGizmoColor(packedColor);
        GizmoSystem.textGizmos[textId] = view;
    }

    /// <summary>
    /// Changes the line thickness of a text sprite's gizmo outline.
    ///
    /// If the text doesn't have a gizmo enabled yet, this enables one with the default color and the given thickness.
    /// </summary>
    /// <remarks>
    /// Bump the thickness up when the default 1-pixel outline gets lost against a busy background or behind the text glyphs themselves. The unit is render-buffer pixels.
    ///
    /// A thickness of <c>0</c> or less will skip drawing the outline entirely.
    /// </remarks>
    /// <example>
    /// Give a header label a thicker outline:
    /// <code>
    /// font 1, "font"
    /// text 1, 550, 280, 1, "level 1"
    /// enable text gizmo 1
    /// ` make the header outline 2 pixels wide
    /// set text gizmo thickness 1, 2.0
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite whose gizmo thickness should change.</param>
    /// <param name="thickness">Line thickness in pixels. Values <c>1</c> through <c>4</c> are typical.</param>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    /// <seealso cref="SetTextGizmoColor">set text gizmo color</seealso>
    [FadeBasicCommand("set text gizmo thickness")]
    public static void SetTextGizmoThickness(int textId, float thickness)
    {
        if (!GizmoSystem.textGizmos.TryGetValue(textId, out var view))
        {
            view = new TextGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
        }
        view.thickness = thickness;
        GizmoSystem.textGizmos[textId] = view;
    }

    // -----------------------------------------------------------------
    // Immediate-mode: queued each frame, drawn during the next sync,
    // then cleared. -1 packed color = opaque white.
    // -----------------------------------------------------------------

    // packedColor default -1 = (0xFF,0xFF,0xFF,0xFF) = opaque white when
    // unpacked via ColorUtil. thickness default 1 matches GizmoSystem.DefaultThickness.
    // Both are written as literals because the FadeBasic command source
    // generator copies the default-expression text verbatim into a separate
    // generated file with no usings — a const reference would fail to resolve.

    /// <summary>
    /// Queues a single debug line to be drawn this frame, from one world-space point to another.
    ///
    /// The line is drawn during the next <see cref="Sync">sync</see> and then cleared, so you need to re-issue it every frame if you want it to stay visible.
    /// </summary>
    /// <remarks>
    /// This is the workhorse for ad-hoc debug visuals: connect two game objects with a line to show a relationship, draw a vector from a position out along a direction to visualize a velocity, sketch a path the AI is considering. Call it as many times as you like before <see cref="Sync">sync</see> — every queued line draws in the order it was added.
    ///
    /// Lines are in world space — the same coordinate system as <see cref="PositionSprite">position sprite</see>. They render on top of the game and aren't touched by any screen effect.
    ///
    /// Both optional parameters have sensible defaults. If you skip the color, you get opaque white. If you skip the thickness, you get a 1-pixel line. For persistent debug outlines that follow a sprite or collider automatically, prefer <see cref="EnableSpriteGizmo">enable sprite gizmo</see> and <see cref="EnableColliderGizmo">enable collider gizmo</see> — those don't need to be re-queued each frame.
    /// </remarks>
    /// <example>
    /// Draw a line from the player to the mouse cursor every frame:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// DO
    ///   px = sprite x(1)
    ///   py = sprite y(1)
    ///   ` draw a line from the ghost to the mouse cursor
    ///   gizmo line px, py, mouse x(), mouse y()
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Sketch a 3-pixel red velocity vector:
    /// <code>
    /// vx = 40
    /// vy = -20
    /// DO
    ///   gizmo line 320, 240, 320 + vx, 240 + vy, rgb(255, 0, 0), 3.0
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="x1">The X position of the line's start point in world coordinates.</param>
    /// <param name="y1">The Y position of the line's start point in world coordinates.</param>
    /// <param name="x2">The X position of the line's end point in world coordinates.</param>
    /// <param name="y2">The Y position of the line's end point in world coordinates.</param>
    /// <param name="packedColor">A packed RGBA color value. Defaults to opaque white. Use <see cref="Rgb">rgb</see> to build a custom one.</param>
    /// <param name="thickness">Line thickness in pixels. Defaults to <c>1</c>.</param>
    /// <seealso cref="GizmoRect">gizmo rect</seealso>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="Sync">sync</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("gizmo line")]
    public static void GizmoLine(float x1, float y1, float x2, float y2,
        int packedColor = -1, float thickness = 1f)
    {
        GizmoSystem.transientLines.Add(new GizmoLineShape
        {
            a = new Vector2(x1, y1),
            b = new Vector2(x2, y2),
            color = UnpackGizmoColor(packedColor),
            thickness = thickness,
        });
    }

    /// <summary>
    /// Queues an axis-aligned debug rectangle outline to be drawn this frame.
    ///
    /// The rectangle is drawn during the next <see cref="Sync">sync</see> and then cleared, so you need to re-issue it every frame if you want it to stay visible.
    /// </summary>
    /// <remarks>
    /// Use this when you want to highlight an area on screen that isn't tied to a specific sprite or collider — the bounds of a UI region, a target zone the AI is heading for, the camera's culling area while you tune it. The rectangle is just four <see cref="GizmoLine">gizmo line</see> calls under the hood, so the thickness and color rules are identical.
    ///
    /// Rectangles are in world space — the same coordinate system as <see cref="CreateBoxCollider">box collider</see>. They render on top of the game and aren't touched by any screen effect.
    ///
    /// The outline is always axis-aligned. If you need a rotated rectangle, draw the four sides yourself with <see cref="GizmoLine">gizmo line</see>, or attach the thing to a sprite and use <see cref="EnableSpriteGizmo">enable sprite gizmo</see> instead.
    /// </remarks>
    /// <example>
    /// Highlight a 100x60 target zone in the center of the screen:
    /// <code>
    /// DO
    ///   gizmo rect 270, 210, 100, 60
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Show a 2-pixel-thick yellow zone:
    /// <code>
    /// DO
    ///   gizmo rect 100, 100, 200, 80, rgb(255, 255, 0), 2.0
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="x">The X position of the rectangle's top-left corner in world coordinates.</param>
    /// <param name="y">The Y position of the rectangle's top-left corner in world coordinates.</param>
    /// <param name="w">The width of the rectangle in pixels.</param>
    /// <param name="h">The height of the rectangle in pixels.</param>
    /// <param name="packedColor">A packed RGBA color value. Defaults to opaque white. Use <see cref="Rgb">rgb</see> to build a custom one.</param>
    /// <param name="thickness">Line thickness in pixels. Defaults to <c>1</c>.</param>
    /// <seealso cref="GizmoLine">gizmo line</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="Sync">sync</seealso>
    /// <seealso cref="Rgb">rgb</seealso>
    [FadeBasicCommand("gizmo rect")]
    public static void GizmoRect(float x, float y, float w, float h,
        int packedColor = -1, float thickness = 1f)
    {
        GizmoSystem.transientRects.Add(new GizmoRectShape
        {
            position = new Vector2(x, y),
            size = new Vector2(w, h),
            color = UnpackGizmoColor(packedColor),
            thickness = thickness,
        });
    }
}
