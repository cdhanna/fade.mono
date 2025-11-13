using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("box collider")]
    public static void CreateBoxCollider(int colliderId, int x, int y, int w, int h)
    {
        CollisionSystem.GetColliderIndex(colliderId, out var index, out var collider);
        collider.position = new Vector2(x, y);
        collider.size = new Vector2(w, h);
        CollisionSystem.aabbs[index] = collider;
    }

    [FadeBasicCommand("attach collider to transform")]
    public static void AttachColliderToTransform(int colliderId, int transformId)
    {
        CollisionSystem.GetColliderIndex(colliderId, out var index, out _);
        CollisionSystem.aabbs[index].targetTransformId = transformId;
    }

    [FadeBasicCommand("perform collider checks")]
    public static void DoHitCheck()
    {
        CollisionSystem.FindHits();
    }

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