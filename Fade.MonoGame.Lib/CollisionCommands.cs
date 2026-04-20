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
}