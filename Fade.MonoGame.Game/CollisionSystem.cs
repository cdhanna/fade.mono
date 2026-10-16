using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Core;


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
    // Room for 16,384 up front, which no game so far has needed more of (the fish game peaks near
    // 4,000 sprites), and the array doubles if it fills. It used to be 10 million: about 6 GB
    // committed across the systems, which kept a Mac's memory load near 98% and the .NET GC
    // collecting over and over.
    public const int INITIAL_AABB_COUNT = 16_384;
    public static ColliderBox[] aabbs = new ColliderBox[INITIAL_AABB_COUNT];
    private static Dictionary<int, int> _colliderMap = new Dictionary<int, int>();
    public static int AabbsCount;

    public const int INITIAL_HIT_COUNT = 16_384;
    public static CollisionHit[] hits = new CollisionHit[INITIAL_HIT_COUNT];
    public static int HitCount;

    public static Dictionary<int, List<int>> colliderIdToHitIds = new Dictionary<int, List<int>>();


    public static void Reset()
    {
        //aabbs = new ColliderBox[INITIAL_AABB_COUNT];
        _colliderMap.Clear();
        AabbsCount = 0;

        //hits = new CollisionHit[INITIAL_HIT_COUNT];
        HitCount = 0;
    }
    
    public static void GetColliderIndex(int colliderId, out int index, out ColliderBox collider)
    {
        if (!_colliderMap.TryGetValue(colliderId, out index))
        {
            if (AabbsCount == aabbs.Length) Array.Resize(ref aabbs, aabbs.Length * 2);
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
                    if (HitCount == hits.Length) Array.Resize(ref hits, hits.Length * 2);
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