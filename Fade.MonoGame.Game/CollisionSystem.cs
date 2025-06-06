using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Game;


public struct ColliderBox
{
    public int id;
    public int targetTransformId;
    public Vector2 position;
    public Vector2 size;

    public Vector2 computedPosition;
    public Vector2 computedSize;

    public float Left => computedPosition.X;
    public float Right => computedPosition.X + computedSize.X;
    public float Top => computedPosition.Y;
    public float Low => computedPosition.Y + computedSize.Y;
}

public static class CollisionBoxUtil
{
    public static bool Intersects(ref ColliderBox a, ref ColliderBox b)
    {
        bool xOverlap = a.Left <= b.Right && a.Right >= b.Left;
        bool yOverlap = a.Top <= b.Low && a.Low >= b.Top;
        return xOverlap && yOverlap;
    }
}

public struct CollisionHit
{
    public int aId, bId;
}


public static class CollisionSystem
{
    public const int MAX_AABB_COUNT = 10_000_000;
    public static ColliderBox[] aabbs = new ColliderBox[MAX_AABB_COUNT];
    private static Dictionary<int, int> _colliderMap = new Dictionary<int, int>();
    public static int AabbsCount;

    public const int MAX_HIT_COUNT = 10_000_000;
    public static CollisionHit[] hits = new CollisionHit[MAX_HIT_COUNT];
    public static int HitCount;

    public static Dictionary<int, List<int>> colliderIdToHitIds = new Dictionary<int, List<int>>();


    public static void Reset()
    {
        //aabbs = new ColliderBox[MAX_AABB_COUNT];
        _colliderMap.Clear();
        AabbsCount = 0;

        //hits = new CollisionHit[MAX_HIT_COUNT];
        HitCount = 0;
    }
    
    public static void GetColliderIndex(int colliderId, out int index, out ColliderBox collider)
    {
        if (!_colliderMap.TryGetValue(colliderId, out index))
        {
            index = _colliderMap[colliderId] = AabbsCount;
            collider = new ColliderBox()
            {
                id = colliderId,
                position = Vector2.Zero,
                size = Vector2.One
            };
            aabbs[index] = collider;
            AabbsCount++;
        }
        else
        {
            collider = aabbs[index];
        }
    }
    

    public static void FindHits()
    {
        HitCount = 0; // reset hits...

        // before checking, compute all the transform math.
        for (var i = 0; i < AabbsCount; i++)
        {
            var box = aabbs[i];
            var position = box.position;
            var size = box.size;

            if (box.targetTransformId > 0)
            {
                var localMat = TransformSystem.CreateMatrix(position, 0, size);
                TransformSystem.GetTransformIndex(box.targetTransformId, out _, out var transform);
                var mat = transform.computedWorld;
                mat = localMat * mat;
                
                TransformSystem.DecomposeMatrix(mat, out var matPos, out _, out var matScale);
                position.X = matPos.X;
                position.Y = matPos.Y;
                size.X = matScale.X;
                size.Y = matScale.Y;
            }

            box.computedPosition = position;
            box.computedSize = size;
            aabbs[i] = box;
        }
        
        
        // TODO: find better perf than n^2 :( 
        for (var aIndex = 0; aIndex < AabbsCount; aIndex++)
        {
            for (var bIndex = 0; bIndex < AabbsCount; bIndex++)
            {
                if (aIndex == bIndex) continue; // cannot intersect with self...

                var a = aabbs[aIndex];
                var b = aabbs[bIndex];

                var isHit = CollisionBoxUtil.Intersects(ref a, ref b);
                if (isHit)
                {
                    hits[HitCount].aId = a.id;
                    hits[HitCount].bId = b.id;
                    HitCount++;
                }
            }
        }
        
        colliderIdToHitIds.Clear();
        // cache the hits by id so they are easy to find later
        for (var i = 0; i < HitCount; i++)
        {
            var hit = hits[i];
            if (!colliderIdToHitIds.TryGetValue(hit.aId, out var aHits))
            {
                aHits = colliderIdToHitIds[hit.aId] = new List<int>();
            }
            aHits.Add(i);
            
            if (!colliderIdToHitIds.TryGetValue(hit.bId, out var bHits))
            {
                bHits = colliderIdToHitIds[hit.bId] = new List<int>();
            }
            bHits.Add(i);
        }
        
    }
    
}