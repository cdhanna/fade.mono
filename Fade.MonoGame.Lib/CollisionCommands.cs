using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Creates an axis-aligned box collider at the given position and size.</para>
    /// <para>The collider is static by default and will not move on its own. Attach it
    /// to a transform with <see cref="AttachColliderToTransform">attach collider to transform</see>
    /// if you need it to follow a game object.</para>
    /// </summary>
    /// <remarks>
    /// Box colliders are the building blocks of Fade's collision system. You create them,
    /// optionally parent them to transforms, and then each frame you call
    /// <see cref="DoHitCheck">perform collider checks</see> to find out what's overlapping.
    /// After that, use <see cref="AreCollidersHitting">get collision</see> to query specific pairs.
    ///
    /// A typical setup for a game entity looks like this: create a transform with
    /// <see cref="CreateTransform">transform</see>, create a sprite with
    /// <see cref="Sprite">sprite</see> and attach it via
    /// <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>, then create
    /// a collider here and attach it with
    /// <see cref="AttachColliderToTransform">attach collider to transform</see>. Now moving
    /// the transform moves everything together.
    ///
    /// Collider positions are relative to their attached transform (if any). If you set
    /// x=<c>0</c>, y=<c>0</c> and attach to a transform, the collider sits at the
    /// transform's origin. Offset x and y to shift it relative to that anchor point.
    ///
    /// There's no limit on the number of colliders you can create, but keep in mind that
    /// <see cref="DoHitCheck">perform collider checks</see> is an O(n^2) broad-phase, so
    /// hundreds of active colliders will start to cost you.
    /// </remarks>
    /// <example>
    /// Create a collider for a player character and attach it to a transform.
    /// <code>
    /// ` set up the player entity
    /// playerId = 1
    /// transform playerId, 100, 200
    /// box collider playerId, 0, 0, 32, 32
    /// attach collider to transform playerId, playerId
    /// </code>
    /// </example>
    /// <example>
    /// Create a static wall collider that does not move.
    /// <code>
    /// ` place a wall at the bottom of the screen
    /// wallId = 99
    /// box collider wallId, 0, 460, 640, 20
    /// </code>
    /// </example>
    /// <param name="colliderId">The ID to assign to this collider.</param>
    /// <param name="x">The X position of the collider's top-left corner.</param>
    /// <param name="y">The Y position of the collider's top-left corner.</param>
    /// <param name="w">The width of the collider in pixels.</param>
    /// <param name="h">The height of the collider in pixels.</param>
    /// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
    /// <seealso cref="DoHitCheck">perform collider checks</seealso>
    /// <seealso cref="AreCollidersHitting">get collision</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    [FadeBasicCommand("box collider")]
    public static void CreateBoxCollider(int colliderId, int x, int y, int w, int h)
    {
        CollisionSystem.GetColliderIndex(colliderId, out var index, out var collider);
        collider.position = new Vector2(x, y);
        collider.size = new Vector2(w, h);
        CollisionSystem.aabbs[index] = collider;
    }

    /// <summary>
    /// <para>Attaches a collider to a transform so it follows the transform's position each frame.</para>
    /// <para>Once attached, the collider's x and y become offsets relative to the transform rather than absolute screen positions.</para>
    /// </summary>
    /// <remarks>
    /// This is how you make a collider stick to a moving game object. Without this, the
    /// collider just sits wherever you placed it with
    /// <see cref="CreateBoxCollider">box collider</see>. The collision system reads the
    /// transform's world position before doing its sweep each frame, so the collider
    /// automatically stays in sync.
    ///
    /// Pairs naturally with <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>.
    /// The typical entity has a transform, a sprite attached to it, and a collider
    /// attached to it. Move the transform and everything follows.
    /// </remarks>
    /// <example>
    /// Build a complete game entity with a transform, sprite, and collider.
    /// <code>
    /// ` create the entity's transform
    /// enemyId = 5
    /// transform enemyId, 300, 100
    ///
    /// ` create and attach a sprite
    /// sprite enemyId, 0, 0
    /// attach sprite to transform enemyId, enemyId
    ///
    /// ` create and attach a collider
    /// box collider enemyId, -16, -16, 32, 32
    /// attach collider to transform enemyId, enemyId
    ///
    /// ` now moving the transform moves everything
    /// set transform position enemyId, 400, 200
    /// </code>
    /// </example>
    /// <param name="colliderId">The ID of the collider to attach.</param>
    /// <param name="transformId">The ID of the transform to follow.</param>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    [FadeBasicCommand("attach collider to transform")]
    public static void AttachColliderToTransform(int colliderId, int transformId)
    {
        CollisionSystem.GetColliderIndex(colliderId, out var index, out _);
        CollisionSystem.aabbs[index].targetTransformId = transformId;
    }

    /// <summary>
    /// <para>Runs the broad-phase collision sweep across all active colliders.</para>
    /// <para>You must call this once per frame before using
    /// <see cref="AreCollidersHitting">get collision</see>, or you'll be reading stale
    /// hit data from the previous frame.</para>
    /// </summary>
    /// <remarks>
    /// Collision detection in Fade works in two phases. First, you call this command to
    /// sweep all active colliders and build up the internal hit list. Then you query
    /// specific pairs with <see cref="AreCollidersHitting">get collision</see>. This
    /// two-phase design means the expensive broad-phase only runs once per frame, no
    /// matter how many pairs you check afterward.
    ///
    /// Call this once per frame in your <c>DO...LOOP</c>, after you've moved everything
    /// but before you check for hits. Calling it multiple times per frame is harmless but
    /// wasteful. Forgetting to call it means
    /// <see cref="AreCollidersHitting">get collision</see> will never see new overlaps.
    /// </remarks>
    /// <example>
    /// A typical game loop that moves objects, sweeps collisions, then checks for hits.
    /// <code>
    /// ` set up a player and an enemy
    /// playerId = 1
    /// enemyId = 2
    /// transform playerId, 100, 200
    /// transform enemyId, 300, 200
    /// box collider playerId, 0, 0, 32, 32
    /// box collider enemyId, 0, 0, 32, 32
    /// attach collider to transform playerId, playerId
    /// attach collider to transform enemyId, enemyId
    ///
    /// set sync rate 16
    /// DO
    ///   ` move the player toward the enemy
    ///   px = get local transform x(playerId)
    ///   set transform position playerId, px + 1, 200
    ///
    ///   ` sweep all colliders, then check for hits
    ///   perform collider checks
    ///   hit = get collision(playerId, enemyId)
    ///   IF hit = 1 THEN
    ///     print "collision detected!"
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="AreCollidersHitting">get collision</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
    /// <seealso cref="GetTransformLocalX">get local transform x</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    [FadeBasicCommand("perform collider checks")]
    public static void DoHitCheck()
    {
        CollisionSystem.FindHits();
    }

    /// <summary>
    /// <para>Checks whether two colliders are currently overlapping.</para>
    /// <para>You must call <see cref="DoHitCheck">perform collider checks</see> earlier in
    /// the frame for this to return up-to-date results. Without that, you're reading stale
    /// hit data from the previous frame.</para>
    /// </summary>
    /// <remarks>
    /// This is the query side of Fade's two-phase collision system. After
    /// <see cref="DoHitCheck">perform collider checks</see> has done its sweep, call this
    /// to ask about any specific pair of colliders. You can call it as many times as you
    /// want per frame because the expensive work already happened in the sweep.
    ///
    /// The order of the two collider IDs does not matter. Checking (a, b) is the same as
    /// checking (b, a).
    ///
    /// If either collider ID doesn't exist or hasn't been involved in any collision, this
    /// returns <c>0</c> rather than throwing an error.
    /// </remarks>
    /// <example>
    /// Check if a bullet hit any of three enemies.
    /// <code>
    /// ` assume bullet and enemy colliders are already set up
    /// perform collider checks
    /// FOR e = 1 TO 3
    ///   hit = get collision(bulletId, e)
    ///   IF hit = 1 THEN
    ///     print "enemy hit!"
    ///   ENDIF
    /// NEXT e
    /// </code>
    /// </example>
    /// <example>
    /// React to a player touching a pickup item.
    /// <code>
    /// ` inside the game loop, after perform collider checks
    /// hit = get collision(playerId, coinId)
    /// IF hit = 1 THEN
    ///   score = score + 10
    ///   ` move the coin off screen so it stops colliding
    ///   set transform position coinId, -100, -100
    /// ENDIF
    /// </code>
    /// </example>
    /// <param name="aColliderId">The ID of the first collider.</param>
    /// <param name="bColliderId">The ID of the second collider.</param>
    /// <returns><c>1</c> if the two colliders are overlapping, <c>0</c> otherwise.</returns>
    /// <seealso cref="DoHitCheck">perform collider checks</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    [FadeBasicCommand("get collision")]
    public static bool AreCollidersHitting(int aColliderId, int bColliderId)
    {
        if (!CollisionSystem.colliderIdToHitIds.TryGetValue(aColliderId, out var aHits))
            return false;

        // TODO: more caching could happen here... :( 
        foreach (var hitIndex in aHits)
        {
            if (CollisionSystem.hits[hitIndex].aId == aColliderId && CollisionSystem.hits[hitIndex].bId == bColliderId)
            {
                return true;
            }
            if (CollisionSystem.hits[hitIndex].aId == bColliderId && CollisionSystem.hits[hitIndex].bId == aColliderId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <para>Resizes and repositions a collider so its bounds match the given
    /// sprite's current closest-fit AABB on screen.</para>
    /// <para>The sprite's position, scale, rotation, origin, and any attached
    /// transform are all taken into account. For a rotated sprite the AABB
    /// expands to enclose the rotated rectangle — that's the "closest-fit"
    /// behavior an axis-aligned collider can offer.</para>
    /// </summary>
    /// <remarks>
    /// <para><b>Snap detaches the collider from any transform it was attached to.</b>
    /// The collider's <c>x</c>/<c>y</c>/<c>width</c>/<c>height</c> become absolute
    /// world coordinates after this call — keeping it attached would cause the
    /// next per-frame collision update to compose those world coords with the
    /// parent transform a second time, putting the collider in the wrong place.
    /// If you need the collider to stay aligned with a moving sprite, call this
    /// command each frame inside your game loop.</para>
    /// <para>No-op when the sprite has no texture loaded yet (frame size is zero).</para>
    /// </remarks>
    /// <example>
    /// Keep a collider glued to a rotating sprite's drawn bounds every frame:
    /// <code>
    /// texture 1, "Images/Player"
    /// sprite 1, 200, 200, 1
    /// make collider 2, 0, 0, 1, 1
    /// angle = 0
    /// DO
    ///   angle = angle + 1
    ///   rotate sprite 1, angle
    ///   snap collider to sprite 2, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider to resize and reposition.</param>
    /// <param name="spriteId">The sprite to read the AABB from.</param>
    /// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
    /// <seealso cref="IsMouseOverSprite">mouse over sprite</seealso>
    [FadeBasicCommand("snap collider to sprite")]
    public static void SnapColliderToSprite(int colliderId, int spriteId)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out _, out var sprite);

        // Frame size in unscaled texture pixels. Skip silently if the
        // sprite has no texture loaded yet — better to leave the collider
        // alone than collapse it to a zero-area degenerate.
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);
        var src = TextureSystem.GetSourceRect(ref runtimeTex, ref sprite);
        float frameW = src.Width;
        float frameH = src.Height;
        if (frameW <= 0 || frameH <= 0) return;
        var originPx = new Vector2(frameW * sprite.origin.X, frameH * sprite.origin.Y);

        // Composite world transform fresh — same approach as the hit-tests,
        // no cache reads, so a sprite that was just moved this frame snaps
        // to its real position rather than last frame's.
        var position = sprite.position;
        var rotation = sprite.rotation;
        var scale = sprite.scale;
        if (sprite.anchorTransformId > 0)
        {
            var localMat = TransformSystem.CreateMatrix(position, rotation, scale);
            var parentMat = ResolveWorldMatrixByTransformId(sprite.anchorTransformId);
            var worldMat = localMat * parentMat;
            TransformSystem.DecomposeMatrix(worldMat, out var p3, out var r3, out var s3);
            position = new Vector2(p3.X, p3.Y);
            rotation = r3.Z;
            scale = new Vector2(s3.X, s3.Y);
        }

        // Four corners of the sprite's drawn rectangle in world space.
        // Local rect spans [-origin*scale, (frame - origin)*scale] before
        // rotation; rotate each corner and pick the min/max for the AABB.
        var minLocalX = -originPx.X * scale.X;
        var maxLocalX = (frameW - originPx.X) * scale.X;
        var minLocalY = -originPx.Y * scale.Y;
        var maxLocalY = (frameH - originPx.Y) * scale.Y;
        var cos = (float)System.Math.Cos(rotation);
        var sin = (float)System.Math.Sin(rotation);
        Vector2 RotatedCorner(float lx, float ly) => new Vector2(
            position.X + lx * cos - ly * sin,
            position.Y + lx * sin + ly * cos);
        var c0 = RotatedCorner(minLocalX, minLocalY);
        var c1 = RotatedCorner(maxLocalX, minLocalY);
        var c2 = RotatedCorner(maxLocalX, maxLocalY);
        var c3 = RotatedCorner(minLocalX, maxLocalY);
        var minX = System.Math.Min(System.Math.Min(c0.X, c1.X), System.Math.Min(c2.X, c3.X));
        var maxX = System.Math.Max(System.Math.Max(c0.X, c1.X), System.Math.Max(c2.X, c3.X));
        var minY = System.Math.Min(System.Math.Min(c0.Y, c1.Y), System.Math.Min(c2.Y, c3.Y));
        var maxY = System.Math.Max(System.Math.Max(c0.Y, c1.Y), System.Math.Max(c2.Y, c3.Y));

        // Detach + write absolute world coords. See remarks: if we left
        // targetTransformId set, the next CollisionSystem update would
        // compose these world coords with the parent matrix a second time.
        CollisionSystem.GetColliderIndex(colliderId, out var idx, out var box);
        box.position = new Vector2(minX, minY);
        box.size = new Vector2(maxX - minX, maxY - minY);
        box.targetTransformId = 0;
        CollisionSystem.aabbs[idx] = box;
    }
}