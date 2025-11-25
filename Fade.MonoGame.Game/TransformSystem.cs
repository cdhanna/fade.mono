using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Game;

public struct Transform
{
    public int id;
    public Vector2 position;
    public float angle;
    public Vector2 scale;

    public Matrix computedWorld;

    public int parentIndex;
    public int referenceCount;
}

public static class TransformSystem
{
    public const int MAX_TRANSFORM_COUNT = 10_000_000;
    public static Transform[] transforms = new Transform[MAX_TRANSFORM_COUNT];
    public static int transformCount = 0;
    public static int highestTransformId = 0;
    private static Dictionary<int, int> _transformMap = new Dictionary<int, int>();
    
    public static void Reset()
    {
       // transforms = new Transform[MAX_TRANSFORM_COUNT];
        transformCount = 0;
        highestTransformId = 0;
        _transformMap.Clear();
    }

    
    public static void GetTransformIndex(int transformId, out int index, out Transform transform)
    {
        if (!_transformMap.TryGetValue(transformId, out index))
        {
            highestTransformId = transformId > highestTransformId ? transformId : highestTransformId;
            
            index = _transformMap[transformId] = transformCount;
            transform = new Transform()
            {
                id = transformId,
                position = Vector2.Zero,
                angle = 0,
                scale = Vector2.One
                
            };
            transforms[index] = transform;
            transformCount++;
        }
        else
        {
            transform = transforms[index];
        }
    }
    
    public static void DecomposeMatrix(
        Matrix matrix,
        out Vector3 position,
        out Vector3 rotationEulerRadians,
        out Vector3 scale)
    {
        // Extract translation
        position = new Vector3(matrix.M41, matrix.M42, matrix.M43);

        // Extract and normalize scale
        Vector3 right = new Vector3(matrix.M11, matrix.M12, matrix.M13);
        Vector3 up = new Vector3(matrix.M21, matrix.M22, matrix.M23);
        Vector3 forward = new Vector3(matrix.M31, matrix.M32, matrix.M33);

        scale = new Vector3(
            right.Length(),
            up.Length(),
            forward.Length()
        );

        // Prevent divide-by-zero
        if (scale.X != 0) right /= scale.X;
        if (scale.Y != 0) up /= scale.Y;
        if (scale.Z != 0) forward /= scale.Z;

        // Build normalized rotation matrix
        Matrix rotationMatrix = new Matrix(
            right.X, right.Y, right.Z, 0,
            up.X,    up.Y,    up.Z,    0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1
        );

        // Extract Euler angles (ZXY rotation order)
        float x, y, z;

        if (Math.Abs(forward.Y) < 0.999f)
        {
            // Normal case
            x = (float)Math.Asin(-forward.Y);                         // pitch
            y = (float)Math.Atan2(forward.X, forward.Z);             // yaw
            z = (float)Math.Atan2(right.Y, up.Y);                    // roll
        }
        else
        {
            // Gimbal lock case
            x = (float)Math.Asin(-forward.Y);
            y = 0;
            z = (float)Math.Atan2(-up.X, right.X);
        }

        rotationEulerRadians = new Vector3(x, y, z);
    }

    public static Matrix CreateMatrix(Vector2 position, float angle, Vector2 scale)
    {
        var localMat =
                Matrix.Identity
                * Matrix.CreateScale(scale.X, scale.Y, 1)
                * Matrix.CreateRotationZ(angle)
                * Matrix.CreateTranslation(position.X, position.Y, 0)
            ;
        return localMat;
    }

    public static void CalculateTransforms()
    {
        // clear all world computes...
        for (var i = 0; i < transformCount; i++)
        {
            transforms[i].computedWorld = Matrix.Identity;
        }

        for (var i = 0; i < transformCount; i++)
        {
            var trans = transforms[i];

            var localMat = CreateMatrix(trans.position, trans.angle, trans.scale);
            trans.computedWorld = localMat;

            if (trans.parentIndex > 0)
            {
                var parent = transforms[trans.parentIndex];
                trans.computedWorld *= parent.computedWorld;
            }
            
            transforms[i] = trans;

        }
        
    }
}