// Singleton provider — exposes per-frame perf metrics + resource
// counts. The inspector panel polls Snapshot(0) on a low cadence
// (~2 Hz) to update the FPS / memory / draw-count readout.
//
// Most fields are read-only stats. The one editable field is
// `gizmosEnabled` — the system-wide gizmo render switch sits here
// because it's the natural "global toggles" home in the inspector.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fade.MonoGame.Core.Debug;

public sealed class MetadataDebugProvider : IDebugProvider
{
    public string TypeName => "metadata";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        // System-wide gizmo toggle — mirrors the `enable gizmos` /
        // `disable gizmos` fbasic commands. Editable; flipping this
        // off hides every gizmo overlay without losing per-entity
        // color/thickness state.
        new DebugField { Path = "gizmosEnabled",  Type = "bool",  Label = "gizmos enabled" },
        new DebugField { Path = "fps",            Type = "float", Label = "FPS",          ReadOnly = true },
        new DebugField { Path = "frameMs",        Type = "float", Label = "frame ms",     ReadOnly = true },
        new DebugField { Path = "gameTimeSec",    Type = "float", Label = "game time s",  ReadOnly = true },
        new DebugField { Path = "memMB",          Type = "float", Label = "managed MB",   ReadOnly = true },
        new DebugField { Path = "gcGen0",         Type = "int",   Label = "GC gen0",      ReadOnly = true },
        new DebugField { Path = "gcGen1",         Type = "int",   Label = "GC gen1",      ReadOnly = true },
        new DebugField { Path = "gcGen2",         Type = "int",   Label = "GC gen2",      ReadOnly = true },
        new DebugField { Path = "drawItems",      Type = "int",   Label = "draw items",   ReadOnly = true },
        new DebugField { Path = "spriteCount",    Type = "int",   Label = "sprites",      ReadOnly = true },
        new DebugField { Path = "transformCount", Type = "int",   Label = "transforms",   ReadOnly = true },
        new DebugField { Path = "tweenCount",     Type = "int",   Label = "tweens",       ReadOnly = true },
        new DebugField { Path = "colliderCount",  Type = "int",   Label = "colliders",    ReadOnly = true },
        new DebugField { Path = "textCount",      Type = "int",   Label = "texts",        ReadOnly = true },
        new DebugField { Path = "textureCount",   Type = "int",   Label = "textures",     ReadOnly = true },
        new DebugField { Path = "effectCount",    Type = "int",   Label = "effects",      ReadOnly = true },
        new DebugField { Path = "outputCount",    Type = "int",   Label = "render targets", ReadOnly = true },
        new DebugField { Path = "sfxClipCount",   Type = "int",   Label = "sfx clips",    ReadOnly = true },
        new DebugField { Path = "sfxInstCount",   Type = "int",   Label = "sfx instances", ReadOnly = true },
    };

    // Singleton — the panel always queries id=0.
    public IEnumerable<int> ListIds()
    {
        yield return 0;
    }

    public object Snapshot(int id)
    {
        var elapsed = GameSystem.latestTime?.ElapsedGameTime.TotalSeconds ?? 0;
        var fps = elapsed > 0 ? 1.0 / elapsed : 0;
        var frameMs = elapsed * 1000.0;
        var gameTime = GameSystem.latestTime?.TotalGameTime.TotalSeconds ?? 0;
        var memMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

        var drawItems = 0;
        for (var i = 0; i < RenderSystem.outputs.Count; i++)
            drawItems += RenderSystem.outputs[i].orderedItems.Count;

        return new Dictionary<string, object>
        {
            ["gizmosEnabled"]  = GizmoSystem.gizmosEnabled,
            ["fps"]            = fps,
            ["frameMs"]        = frameMs,
            ["gameTimeSec"]    = gameTime,
            ["memMB"]          = memMB,
            ["gcGen0"]         = GC.CollectionCount(0),
            ["gcGen1"]         = GC.CollectionCount(1),
            ["gcGen2"]         = GC.CollectionCount(2),
            ["drawItems"]      = drawItems,
            ["spriteCount"]    = SpriteSystem.spriteCount,
            ["transformCount"] = TransformSystem.transformCount,
            ["tweenCount"]     = TweenSystem.tweenCount,
            ["colliderCount"]  = CollisionSystem.AabbsCount,
            ["textCount"]      = TextSystem.textSpriteCount,
            ["textureCount"]   = TextureSystem.textures.Count,
            ["effectCount"]    = RenderSystem.effects.Count,
            ["outputCount"]    = RenderSystem.outputs.Count,
            ["sfxClipCount"]   = AudioSystem.sfxClips.Count,
            ["sfxInstCount"]   = AudioInstanceSystem.audioEffects.Count,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        switch (path)
        {
            case "gizmosEnabled":
                GizmoSystem.gizmosEnabled = value.GetBoolean();
                return true;
            default:
                return false;
        }
    }
}
