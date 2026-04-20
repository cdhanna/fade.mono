using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    
    /// <summary>
    /// <para>Peeks at the next available transform ID without claiming it.</para>
    /// <para>This doesn't reserve the ID, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="ReserveTransformNextId">reserve transform id</see>
    /// instead, which actually claims the slot. This one is handy if you just need to know what
    /// the next ID would be, for example to pre-allocate an array. If you already know your
    /// ID, skip both of these and call <see cref="CreateTransform">transform</see> directly.
    /// </remarks>
    /// <example>
    /// Peek at the next ID to size an array, then reserve and create transforms.
    /// <code>
    /// ` find out what the next ID will be
    /// nextId = free transform id()
    /// print nextId
    /// </code>
    /// </example>
    /// <param name="transformId">Receives the next free transform ID.</param>
    /// <returns>The next available transform ID (not yet reserved).</returns>
    /// <seealso cref="ReserveTransformNextId">reserve transform id</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    [FadeBasicCommand("free transform id")]
    public static int GetFreeTransformNextId(ref int transformId)
    {
        transformId = TransformSystem.highestTransformId + 1;
        return transformId;
    }

    /// <summary>
    /// <para>Claims the next available transform ID and initializes its slot.</para>
    /// <para>The slot is created but the transform won't affect anything until you set its
    /// position with <see cref="CreateTransform">transform</see> or
    /// <see cref="SetTransformPosition">set transform position</see>.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need to wire up references to a transform before it's fully
    /// configured. The typical pattern is: reserve an ID, then call
    /// <see cref="CreateTransform">transform</see> to place it. If you don't need that
    /// setup step, just call <see cref="CreateTransform">transform</see> directly with a
    /// known ID. See also <see cref="GetFreeTransformNextId">free transform id</see> if
    /// you only need to peek without claiming.
    /// </remarks>
    /// <example>
    /// Reserve IDs for a batch of enemies, then create their transforms.
    /// <code>
    /// ` reserve five enemy transform IDs
    /// FOR i = 1 TO 5
    ///   id = reserve transform id()
    ///   transform id, i * 64, 100
    /// NEXT i
    /// </code>
    /// </example>
    /// <param name="transformId">Receives the reserved transform ID.</param>
    /// <returns>The newly reserved transform ID.</returns>
    /// <seealso cref="GetFreeTransformNextId">free transform id</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    [FadeBasicCommand("reserve transform id")]
    public static int ReserveTransformNextId(ref int transformId)
    {
        GetFreeTransformNextId(ref transformId);
        TransformSystem.GetTransformIndex(transformId, out _, out _);
        return transformId;
    }


    /// <summary>
    /// <para>Creates a transform at the given position.</para>
    /// <para>Transforms are the backbone of Fade's scene hierarchy. They let you group
    /// sprites, text, and colliders so they all move, rotate, and scale together.</para>
    /// </summary>
    /// <remarks>
    /// This is usually one of the first things you create for a game entity. The typical
    /// pattern looks like this: create a transform here, create a sprite with
    /// <see cref="Sprite">sprite</see> and attach it via
    /// <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>, create a
    /// collider with <see cref="CreateBoxCollider">box collider</see> and attach it via
    /// <see cref="AttachColliderToTransform">attach collider to transform</see>. Now moving
    /// the transform with <see cref="SetTransformPosition">set transform position</see>
    /// moves everything together.
    ///
    /// Transforms can also be parented to other transforms with
    /// <see cref="SetTransformParent">set transform parent</see>, forming a hierarchy where
    /// children inherit their parent's position, rotation, and scale.
    /// </remarks>
    /// <example>
    /// Create a full game entity with a transform, sprite, and collider.
    /// <code>
    /// ` build a player entity at the center of the screen
    /// playerId = 1
    /// transform playerId, 320, 240
    ///
    /// ` attach a sprite and a collider
    /// sprite playerId, 0, 0
    /// attach sprite to transform playerId, playerId
    /// box collider playerId, -16, -16, 32, 32
    /// attach collider to transform playerId, playerId
    /// </code>
    /// </example>
    /// <example>
    /// Create a parent transform and a child that follows it.
    /// <code>
    /// ` create a ship and an orbiting shield
    /// shipId = 1
    /// shieldId = 2
    /// transform shipId, 320, 240
    /// transform shieldId, 30, 0
    /// set transform parent shieldId, shipId
    ///
    /// ` moving the ship moves the shield too
    /// set transform position shipId, 400, 240
    /// </code>
    /// </example>
    /// <param name="transformId">The ID to assign to this transform.</param>
    /// <param name="x">The starting X position.</param>
    /// <param name="y">The starting Y position.</param>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    /// <seealso cref="SetTransformParent">set transform parent</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
    [FadeBasicCommand("transform")]
    public static void CreateTransform(int transformId, float x, float y)
    {
        SetTransformPosition(transformId, x, y);
    }

    /// <summary>
    /// <para>Sets the position of a transform.</para>
    /// <para>If this transform has children (sprites, colliders, or other transforms parented
    /// to it), they all move with it.</para>
    /// </summary>
    /// <remarks>
    /// Call this every frame for transforms that move, or once for static ones. This is
    /// the main way you drive game object movement. Move the transform, and everything
    /// attached to it follows.
    ///
    /// The position is local to the transform's parent (if it has one via
    /// <see cref="SetTransformParent">set transform parent</see>). If there's no parent,
    /// the position is in screen coordinates. You can read the position back with
    /// <see cref="GetTransformLocalX">get local transform x</see> and
    /// <see cref="GetTransformLocalY">get local transform y</see>.
    /// </remarks>
    /// <example>
    /// Move a player to the right each frame.
    /// <code>
    /// ` set up the player
    /// playerId = 1
    /// transform playerId, 0, 240
    /// px = 0
    ///
    /// set sync rate 16
    /// DO
    ///   px = px + 2
    ///   set transform position playerId, px, 240
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the transform.</param>
    /// <param name="x">The new X position.</param>
    /// <param name="y">The new Y position.</param>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="GetTransformLocalX">get local transform x</seealso>
    /// <seealso cref="GetTransformLocalY">get local transform y</seealso>
    /// <seealso cref="SetTransformParent">set transform parent</seealso>
    [FadeBasicCommand("set transform position")]
    public static void SetTransformPosition(int transformId, float x, float y)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        transform.position = new Vector2(x, y);
        TransformSystem.transforms[index] = transform;
    }

    /// <summary>
    /// <para>Returns the local X position of a transform.</para>
    /// <para>This is the position relative to the transform's parent, not its final world
    /// position. If the transform has no parent, local and world are the same thing.</para>
    /// </summary>
    /// <remarks>
    /// Use this to read back whatever you set with
    /// <see cref="SetTransformPosition">set transform position</see>. If the transform is
    /// parented via <see cref="SetTransformParent">set transform parent</see>, this returns
    /// the offset from the parent, not the on-screen position. Pairs with
    /// <see cref="GetTransformLocalY">get local transform y</see>.
    /// </remarks>
    /// <example>
    /// Read the player's X position and print it each frame.
    /// <code>
    /// ` track the player's horizontal position
    /// playerId = 1
    /// transform playerId, 100, 200
    ///
    /// set sync rate 16
    /// DO
    ///   px = get local transform x(playerId)
    ///   print px
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the transform.</param>
    /// <returns>The local X position.</returns>
    /// <seealso cref="GetTransformLocalY">get local transform y</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    /// <seealso cref="SetTransformParent">set transform parent</seealso>
    [FadeBasicCommand("get local transform x")]
    public static float GetTransformLocalX(int transformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        return transform.position.X;
    }

    /// <summary>
    /// <para>Returns the local Y position of a transform.</para>
    /// <para>This is the position relative to the transform's parent, not its final world
    /// position. If the transform has no parent, local and world are the same thing.</para>
    /// </summary>
    /// <remarks>
    /// Use this to read back whatever you set with
    /// <see cref="SetTransformPosition">set transform position</see>. If the transform is
    /// parented via <see cref="SetTransformParent">set transform parent</see>, this returns
    /// the offset from the parent, not the on-screen position. Pairs with
    /// <see cref="GetTransformLocalX">get local transform x</see>.
    /// </remarks>
    /// <example>
    /// Read both X and Y to compute distance from origin.
    /// <code>
    /// ` check how far the player is from the top-left corner
    /// playerId = 1
    /// px = get local transform x(playerId)
    /// py = get local transform y(playerId)
    /// dist = sqrt(px * px + py * py)
    /// print dist
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the transform.</param>
    /// <returns>The local Y position.</returns>
    /// <seealso cref="GetTransformLocalX">get local transform x</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    /// <seealso cref="SetTransformParent">set transform parent</seealso>
    [FadeBasicCommand("get local transform y")]
    public static float GetTransformLocalY(int transformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        return transform.position.Y;
    }

    /// <summary>
    /// <para>Returns the local X scale of a transform.</para>
    /// <para>A value of <c>1.0</c> is the default (no scaling). This does not account for
    /// parent scaling; it is just what you set on this transform.</para>
    /// </summary>
    /// <remarks>
    /// Reads back the X component of whatever you set with
    /// <see cref="SetTransformScale">set transform scale</see>. Pairs with
    /// <see cref="GetTransformLocalScaleY">get local transform scale y</see>.
    /// </remarks>
    /// <example>
    /// Check if a transform has been flipped horizontally.
    /// <code>
    /// ` read the X scale to see if the entity is facing left
    /// sx = get local transform scale x(playerId)
    /// IF sx &lt; 0 THEN
    ///   print "facing left"
    /// ENDIF
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the transform.</param>
    /// <returns>The X scale factor. <c>1.0</c> is the default.</returns>
    /// <seealso cref="GetTransformLocalScaleY">get local transform scale y</seealso>
    /// <seealso cref="SetTransformScale">set transform scale</seealso>
    [FadeBasicCommand("get local transform scale x")]
    public static float GetTransformLocalScaleX(int transformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        return transform.scale.X;
    }

    /// <summary>
    /// <para>Returns the local Y scale of a transform.</para>
    /// <para>A value of <c>1.0</c> is the default (no scaling). This does not account for
    /// parent scaling; it is just what you set on this transform.</para>
    /// </summary>
    /// <remarks>
    /// Reads back the Y component of whatever you set with
    /// <see cref="SetTransformScale">set transform scale</see>. Pairs with
    /// <see cref="GetTransformLocalScaleX">get local transform scale x</see>.
    /// </remarks>
    /// <example>
    /// Read both scale axes and print them.
    /// <code>
    /// ` inspect the current scale of an entity
    /// sx = get local transform scale x(entityId)
    /// sy = get local transform scale y(entityId)
    /// print sx
    /// print sy
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the transform.</param>
    /// <returns>The Y scale factor. <c>1.0</c> is the default.</returns>
    /// <seealso cref="GetTransformLocalScaleX">get local transform scale x</seealso>
    /// <seealso cref="SetTransformScale">set transform scale</seealso>
    [FadeBasicCommand("get local transform scale y")]
    public static float GetTransformLocalScaleY(int transformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        return transform.scale.Y;
    }

    /// <summary>
    /// <para>Sets the scale of a transform on the X and Y axes.</para>
    /// <para>A scale of <c>1.0</c> is the default. Children attached to this transform
    /// (sprites, text, colliders, and child transforms) inherit the scaling.</para>
    /// </summary>
    /// <remarks>
    /// Use this to grow or shrink everything attached to a transform at once. Pass the
    /// same value for both axes for uniform scaling, or different values to stretch.
    /// Negative values will flip the attached sprites.
    ///
    /// You can read the scale back with
    /// <see cref="GetTransformLocalScaleX">get local transform scale x</see> and
    /// <see cref="GetTransformLocalScaleY">get local transform scale y</see>.
    /// </remarks>
    /// <param name="transformId">The ID of the transform.</param>
    /// <example>
    /// Double the size of an entity uniformly.
    /// <code>
    /// ` make the boss twice as big
    /// bossId = 10
    /// transform bossId, 320, 240
    /// set transform scale bossId, 2.0, 2.0
    /// </code>
    /// </example>
    /// <example>
    /// Flip a character horizontally when they change direction.
    /// <code>
    /// ` flip the sprite to face left by using negative X scale
    /// set transform scale playerId, -1.0, 1.0
    /// </code>
    /// </example>
    /// <param name="x">The X scale factor. <c>1.0</c> is no change, <c>2.0</c> is double size.</param>
    /// <param name="y">The Y scale factor. <c>1.0</c> is no change, <c>2.0</c> is double size.</param>
    /// <seealso cref="GetTransformLocalScaleX">get local transform scale x</seealso>
    /// <seealso cref="GetTransformLocalScaleY">get local transform scale y</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    [FadeBasicCommand("set transform scale")]
    public static void SetTransformScale(int transformId, float x, float y)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        transform.scale = new Vector2(x, y);
        TransformSystem.transforms[index] = transform;
    }
    
    /// <summary>
    /// <para>Sets the rotation of a transform in radians.</para>
    /// <para>Children attached to this transform inherit the rotation, so rotating a parent
    /// spins everything attached to it.</para>
    /// </summary>
    /// <remarks>
    /// If you're working in degrees, convert with <see cref="Rad">rad</see> first. A full
    /// rotation is roughly <c>6.283</c> radians (2*pi). The rotation applies around the
    /// transform's position, which acts as the pivot point.
    ///
    /// This is the transform-level rotation. Individual sprites can also have their own
    /// rotation via <see cref="RotateSprite">rotate sprite</see>, which stacks on top of
    /// whatever the transform is doing.
    /// </remarks>
    /// <example>
    /// Spin an entity continuously each frame.
    /// <code>
    /// ` rotate a spinning coin
    /// coinId = 3
    /// transform coinId, 320, 240
    /// angle = 0.0
    ///
    /// set sync rate 16
    /// DO
    ///   angle = angle + 0.05
    ///   set transform rotation coinId, angle
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Set a fixed rotation using degrees.
    /// <code>
    /// ` tilt an entity 45 degrees
    /// set transform rotation entityId, rad(45)
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the transform.</param>
    /// <param name="angle">The rotation angle in radians. Use <see cref="Rad">rad</see> to convert from degrees.</param>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="SetTransformParent">set transform parent</seealso>
    [FadeBasicCommand("set transform rotation")]
    public static void SetTransformRotation(int transformId, float angle)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        transform.angle = angle;
        TransformSystem.transforms[index] = transform;
    }
    
    /// <summary>
    /// <para>Parents a transform to another transform.</para>
    /// <para>The child inherits the parent's position, rotation, and scale. The child's own
    /// values become relative to the parent rather than the screen.</para>
    /// </summary>
    /// <remarks>
    /// This is how you build a scene hierarchy. For example, you might parent a weapon
    /// transform to a character transform. Moving the character automatically moves the
    /// weapon, and the weapon's position becomes an offset from the character.
    ///
    /// Re-parenting is supported: calling this on a transform that already has a parent
    /// detaches it from the old parent and attaches to the new one. The system manages
    /// reference counts internally.
    ///
    /// The local getters (<see cref="GetTransformLocalX">get local transform x</see>,
    /// <see cref="GetTransformLocalY">get local transform y</see>) return the position
    /// relative to the parent, not the final on-screen position.
    /// </remarks>
    /// <example>
    /// Create a character with a weapon that follows it.
    /// <code>
    /// ` set up a character and a weapon
    /// charId = 1
    /// weaponId = 2
    /// transform charId, 200, 300
    /// transform weaponId, 20, -10
    ///
    /// ` parent the weapon to the character
    /// set transform parent weaponId, charId
    ///
    /// ` now moving the character moves the weapon too
    /// set sync rate 16
    /// cx = 200
    /// DO
    ///   cx = cx + 1
    ///   set transform position charId, cx, 300
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Build a three-level hierarchy: ship, turret, and barrel.
    /// <code>
    /// ` the barrel is offset from the turret, which is offset from the ship
    /// shipId = 1
    /// turretId = 2
    /// barrelId = 3
    /// transform shipId, 320, 400
    /// transform turretId, 0, -20
    /// transform barrelId, 10, -15
    ///
    /// set transform parent turretId, shipId
    /// set transform parent barrelId, turretId
    ///
    /// ` rotating the ship rotates everything
    /// set transform rotation shipId, rad(30)
    /// </code>
    /// </example>
    /// <param name="transformId">The ID of the child transform.</param>
    /// <param name="parentTransformId">The ID of the parent transform to attach to.</param>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    /// <seealso cref="SetTransformRotation">set transform rotation</seealso>
    /// <seealso cref="GetTransformLocalX">get local transform x</seealso>
    /// <seealso cref="GetTransformLocalY">get local transform y</seealso>
    [FadeBasicCommand("set transform parent")]
    public static void SetTransformParent(int transformId, int parentTransformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        
        TransformSystem.GetTransformIndex(parentTransformId, out var parentIndex, out var parentTransform);

        if (transform.parentIndex > 0)
        {
            TransformSystem.transforms[transform.parentIndex].referenceCount--;
        }
        
        transform.parentIndex = parentIndex;
        parentTransform.referenceCount++;
        
        TransformSystem.transforms[index] = transform;
        TransformSystem.transforms[parentIndex] = parentTransform;
    }
}