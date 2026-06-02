using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.Fade.SpriteBatch;

namespace Fade.MonoGame.Core;

public struct SpriteGizmo
{
    public Color color;
    public float thickness;
}

public struct ColliderGizmo
{
    public Color color;
    public float thickness;
}

public struct TextGizmo
{
    public Color color;
    public float thickness;
}

public struct GizmoLineShape
{
    public Vector2 a;
    public Vector2 b;
    public Color color;
    public float thickness;
}

public struct GizmoRectShape
{
    public Vector2 position;
    public Vector2 size;
    public Color color;
    public float thickness;
}

public static class GizmoSystem
{
    public static readonly Color DefaultColor = Color.White;
    public const float DefaultThickness = 4f;

    public static Dictionary<int, SpriteGizmo> spriteGizmos = new Dictionary<int, SpriteGizmo>();
    public static Dictionary<int, ColliderGizmo> colliderGizmos = new Dictionary<int, ColliderGizmo>();
    public static Dictionary<int, TextGizmo> textGizmos = new Dictionary<int, TextGizmo>();
    public static List<GizmoLineShape> transientLines = new List<GizmoLineShape>();
    public static List<GizmoRectShape> transientRects = new List<GizmoRectShape>();

    // System-wide gizmo render switch. Default ON because gizmos are
    // a debug-time aid; a shipping build can call `disable gizmos`
    // (or flip this from the inspector) to hide every overlay at once
    // without losing any of the per-entity color/thickness state. The
    // transient queues are still drained on each Render call so a
    // disabled tick doesn't accumulate stale `gizmo line` / `gizmo
    // rect` commands across frames.
    public static bool gizmosEnabled = true;

    // 1×1 white pixel texture — every gizmo line is this stretched and rotated.
    // Game1.LoadContent fills it once at startup; survives ResetAll.
    public static Texture2D pixelTexture;

    public static void Reset()
    {
        spriteGizmos.Clear();
        colliderGizmos.Clear();
        textGizmos.Clear();
        transientLines.Clear();
        transientRects.Clear();
        // Default back to on across program restarts — Reset matches
        // the fbasic semantics of "fresh program, fresh debug state".
        gizmosEnabled = true;
    }

    // Drawn in Game1.Draw after the screen-effect composite and before
    // the debug-UI overlay. World coordinates are mainBuffer-pixel space;
    // the transformMatrix below maps them onto screen pixels so gizmos
    // line up with the composited buffer even with letterboxing.
    public static void Render(SpriteBatch sb, Vector2 mainBufferPosition, float mainBufferScale)
    {
        if (pixelTexture == null) return;
        // System-wide disable: skip rendering but drain the per-frame
        // transient queues. If we left them populated, they'd stack up
        // across frames and pop back the moment the user re-enabled.
        if (!gizmosEnabled)
        {
            transientLines.Clear();
            transientRects.Clear();
            return;
        }
        if (spriteGizmos.Count == 0 && colliderGizmos.Count == 0 && textGizmos.Count == 0
            && transientLines.Count == 0 && transientRects.Count == 0)
        {
            return;
        }

        var matrix = Matrix.CreateScale(mainBufferScale)
                     * Matrix.CreateTranslation(mainBufferPosition.X, mainBufferPosition.Y, 0);
        sb.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.PointClamp,
            transformMatrix: matrix);

        foreach (var kv in spriteGizmos)
        {
            if (!TryGetSpriteRotatedRect(kv.Key, out var p0, out var p1, out var p2, out var p3)) continue;
            DrawQuadOutline(sb, p0, p1, p2, p3, kv.Value.color, kv.Value.thickness);
        }

        foreach (var kv in colliderGizmos)
        {
            if (!TryGetColliderAabb(kv.Key, out var pos, out var size)) continue;
            DrawAabbOutline(sb, pos, size, kv.Value.color, kv.Value.thickness);
        }

        foreach (var kv in textGizmos)
        {
            if (!TryGetTextRotatedRect(kv.Key, out var p0, out var p1, out var p2, out var p3)) continue;
            DrawQuadOutline(sb, p0, p1, p2, p3, kv.Value.color, kv.Value.thickness);
        }

        for (var i = 0; i < transientLines.Count; i++)
        {
            var l = transientLines[i];
            DrawLine(sb, l.a, l.b, l.color, l.thickness);
        }

        for (var i = 0; i < transientRects.Count; i++)
        {
            var r = transientRects[i];
            DrawAabbOutline(sb, r.position, r.size, r.color, r.thickness);
        }

        transientLines.Clear();
        transientRects.Clear();

        sb.End();
    }

    private static bool TryGetSpriteRotatedRect(int spriteId,
        out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
    {
        p0 = p1 = p2 = p3 = default;
        SpriteSystem.GetSpriteIndex(spriteId, out _, out var sprite);
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);
        if (runtimeTex.texture == null) return false;
        var src = TextureSystem.GetSourceRect(ref runtimeTex, ref sprite);
        float frameW = src.Width;
        float frameH = src.Height;
        if (frameW <= 0 || frameH <= 0) return false;
        var originPx = new Vector2(frameW * sprite.origin.X, frameH * sprite.origin.Y);

        var position = sprite.position;
        var rotation = sprite.rotation;
        var scale = sprite.scale;
        if (sprite.anchorTransformId > 0)
        {
            // Same pattern RenderSystem.RenderAll2 uses for anchored sprites:
            // compose with the cached computedWorld from CalculateTransforms.
            var localMat = TransformSystem.CreateMatrix(position, rotation, scale);
            TransformSystem.GetTransformIndex(sprite.anchorTransformId, out _, out var transform);
            var worldMat = localMat * transform.computedWorld;
            TransformSystem.DecomposeMatrix(worldMat, out var p, out var r, out var s);
            position = new Vector2(p.X, p.Y);
            rotation = r.Z;
            scale = new Vector2(s.X, s.Y);
        }

        ComputeRotatedRectCorners(position, rotation, scale, originPx, frameW, frameH,
            out p0, out p1, out p2, out p3);
        return true;
    }

    private static bool TryGetTextRotatedRect(int textId,
        out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
    {
        p0 = p1 = p2 = p3 = default;
        TextSystem.GetTextSpriteIndex(textId, out _, out var text);
        if (string.IsNullOrEmpty(text.text)) return false;
        TextureSystem.GetSpriteFontIndex(text.sprite.imageId, out _, out var runtimeFont);
        var font = runtimeFont.font;
        if (font == null) return false;
        var size = font.MeasureString(text.text);
        float frameW = size.X;
        float frameH = size.Y;
        if (frameW <= 0 || frameH <= 0) return false;
        var originPx = new Vector2(frameW * text.sprite.origin.X, frameH * text.sprite.origin.Y);

        var position = text.sprite.position;
        var rotation = text.sprite.rotation;
        var scale = text.sprite.scale;
        if (text.sprite.anchorTransformId > 0)
        {
            var localMat = TransformSystem.CreateMatrix(position, rotation, scale);
            TransformSystem.GetTransformIndex(text.sprite.anchorTransformId, out _, out var transform);
            var worldMat = localMat * transform.computedWorld;
            TransformSystem.DecomposeMatrix(worldMat, out var p, out var r, out var s);
            position = new Vector2(p.X, p.Y);
            rotation = r.Z;
            scale = new Vector2(s.X, s.Y);
        }

        ComputeRotatedRectCorners(position, rotation, scale, originPx, frameW, frameH,
            out p0, out p1, out p2, out p3);
        return true;
    }

    private static bool TryGetColliderAabb(int colliderId, out Vector2 position, out Vector2 size)
    {
        position = size = default;
        CollisionSystem.GetColliderIndex(colliderId, out _, out var box);
        var pos = box.position;
        var sz = box.size;
        if (box.targetTransformId > 0)
        {
            // Mirrors CollisionSystem.FindHits — collider box stays axis-aligned;
            // we just shift/scale by the parent transform's cached world matrix.
            var localMat = TransformSystem.CreateMatrix(pos, 0, sz);
            TransformSystem.GetTransformIndex(box.targetTransformId, out _, out var transform);
            var worldMat = localMat * transform.computedWorld;
            TransformSystem.DecomposeMatrix(worldMat, out var p, out _, out var s);
            pos = new Vector2(p.X, p.Y);
            sz = new Vector2(s.X, s.Y);
        }
        if (sz.X <= 0 || sz.Y <= 0) return false;
        position = pos;
        size = sz;
        return true;
    }

    private static void ComputeRotatedRectCorners(
        Vector2 position, float rotation, Vector2 scale, Vector2 originPx,
        float frameW, float frameH,
        out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
    {
        var minLocalX = -originPx.X * scale.X;
        var maxLocalX = (frameW - originPx.X) * scale.X;
        var minLocalY = -originPx.Y * scale.Y;
        var maxLocalY = (frameH - originPx.Y) * scale.Y;
        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);
        p0 = new Vector2(position.X + minLocalX * cos - minLocalY * sin,
                          position.Y + minLocalX * sin + minLocalY * cos);
        p1 = new Vector2(position.X + maxLocalX * cos - minLocalY * sin,
                          position.Y + maxLocalX * sin + minLocalY * cos);
        p2 = new Vector2(position.X + maxLocalX * cos - maxLocalY * sin,
                          position.Y + maxLocalX * sin + maxLocalY * cos);
        p3 = new Vector2(position.X + minLocalX * cos - maxLocalY * sin,
                          position.Y + minLocalX * sin + maxLocalY * cos);
    }

    public static void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thickness)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 0.0001f) return;
        var angle = MathF.Atan2(dy, dx);
        // origin (0, 0.5) on the 1×1 pixel keeps the thickness centered on
        // the segment. FadeSpriteBatch's Vector2-scale overload requires the
        // texCoord1 argument; default zero is fine — gizmos don't use it.
        sb.Draw(pixelTexture, a, null, color, angle,
            new Vector2(0, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None, 0, new SpriteTexCoord1());
    }

    private static void DrawQuadOutline(SpriteBatch sb,
        Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color, float thickness)
    {
        DrawLine(sb, p0, p1, color, thickness);
        DrawLine(sb, p1, p2, color, thickness);
        DrawLine(sb, p2, p3, color, thickness);
        DrawLine(sb, p3, p0, color, thickness);
    }

    private static void DrawAabbOutline(SpriteBatch sb, Vector2 pos, Vector2 size, Color color, float thickness)
    {
        var p0 = pos;
        var p1 = new Vector2(pos.X + size.X, pos.Y);
        var p2 = new Vector2(pos.X + size.X, pos.Y + size.Y);
        var p3 = new Vector2(pos.X, pos.Y + size.Y);
        DrawQuadOutline(sb, p0, p1, p2, p3, color, thickness);
    }
}
