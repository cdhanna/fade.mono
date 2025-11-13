using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("transform")]
    public static void CreateTransform(int transformId, float x, float y)
    {
        SetTransformPosition(transformId, x, y);
    }
    
    [FadeBasicCommand("set transform position")]
    public static void SetTransformPosition(int transformId, float x, float y)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        transform.position = new Vector2(x, y);
        TransformSystem.transforms[index] = transform;
    }

    [FadeBasicCommand("get local transform x")]
    public static float GetTransformLocalX(int transformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        return transform.position.X;
    }
    [FadeBasicCommand("get local transform y")]
    public static float GetTransformLocalY(int transformId)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        return transform.position.Y;
    }
    
    [FadeBasicCommand("set transform scale")]
    public static void SetTransformScale(int transformId, float x, float y)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        transform.scale = new Vector2(x, y);
        TransformSystem.transforms[index] = transform;
    }
    
    [FadeBasicCommand("set transform rotation")]
    public static void SetTransformRotation(int transformId, float angle)
    {
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        transform.angle = angle;
        TransformSystem.transforms[index] = transform;
    }
    
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