#if !BROWSER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using ImGuiNET;
using Microsoft.Xna.Framework;
using FadeBasic;
using FadeBasic.Ast;
using FadeBasic.Launch;
using FadeBasic.Lsp;
using FadeBasic.Sdk;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Fade.MonoGame.Core;

public struct DebugUICommand
{
    public int vmInstructionIndex;
    public DebugControlType type;
    public string label;

    // essentially only one of these fields should exist, so they could occupy the same memory
    public string argString;
    public int argInt;
    public float argFloat;

    public int ControlId => HashCode.Combine(string.IsNullOrEmpty(label) ? type.ToString() : label, vmInstructionIndex);
}

public enum DebugControlType
{
    // primitive controls
    WINDOW_START,
    WINDOW_END,
    BUTTON,
    INT_SLIDER,
    FLOAT_SLIDER,
    LABEL,
    TEXTFIELD,
    SAME_LINE,
    CHECKBOX,
    SEPARATOR,
    TEXT,
    TREE_START,
    TREE_END,
    COLOR_PICKER,
    DRAG_INT,
    DRAG_FLOAT,
    TAB_BAR_START,
    TAB_BAR_END,
    TAB_ITEM_START,
    TAB_ITEM_END,

    // components
    COMPONENT_SPRITE,
    COMPONENT_EFFECT,
    COMPONENT_TRANSFORM,
    COMPONENT_TWEEN,
    COMPONENT_COLLIDER,
    COMPONENT_TEXT,
    COMPONENT_SFX,
    COMPONENT_TEXTURE,
    COMPONENT_RENDER_OUTPUT,
    COMPONENT_METADATA,

    // composite
    INSPECTOR,
    CONSOLE,

    // browsers
    BROWSER_SPRITE,
    BROWSER_EFFECT,
    BROWSER_TRANSFORM,
    BROWSER_TWEEN,
    BROWSER_COLLIDER,
    BROWSER_TEXT,
    BROWSER_SFX,
    BROWSER_TEXTURE,
    BROWSER_RENDER_OUTPUT,

    // args
    ARG_INT,
    ARG_STRING,
    ARG_FLOAT
}

public static class DebugUISystem
{
    public static ImGuiRenderer renderer;
    public static DebugSession debugSession; // set by Game1 so the console can call ReplExec
    public static CommandCollection commandCollection; // set by Game1 for console completions
    public static Queue<DebugUICommand> controls = new Queue<DebugUICommand>();
    public static Dictionary<int, bool> controlIdToBool = new Dictionary<int, bool>();
    public static Dictionary<int, int> controlIdToInt = new Dictionary<int, int>();
    public static Dictionary<int, float> controlIdToFloat = new Dictionary<int, float>();
    public static Dictionary<int, string> controlIdToString = new Dictionary<int, string>();

    // persistent state that survives EndDebug() — used by browsers to remember selection
    public static Dictionary<int, int> persistentInt = new Dictionary<int, int>();

    // ImGui texture binding cache: Fade textureId -> ImGui IntPtr handle
    static readonly Dictionary<int, IntPtr> _boundTextures = new Dictionary<int, IntPtr>();

    // remember windowed resolution so we can restore it when leaving fullscreen
    static int _windowedWidth = 1280;
    static int _windowedHeight = 720;

    // style persistence
    const string STYLE_FILE = "imgui_fade_style.json";
    static bool _styleDirty;
    static bool _styleLoaded;

    // auto-inspector: when enabled, sync automatically renders a debug inspector window
    public static bool autoInspectorEnabled;

    // wired by Game1 so the inspector can show reload state and trigger F1 reloads from a button
    public static Func<bool> isNewBuildAvailable;
    public static Action requestReload;

    public static void Push(DebugUICommand control)
    {
        controls.Enqueue(control);
    }

    public static void ClearPreviousEvents()
    {
    }

    public static bool TryGetPreviousBool(DebugUICommand command)
    {
        if (controlIdToBool.TryGetValue(command.ControlId, out var val))
        {
            return val;
        }

        return false;
    }
    public static bool TryGetPreviousInt(DebugUICommand command, out int val)
    {
        if (controlIdToInt.TryGetValue(command.ControlId, out val))
        {
            return true;
        }

        return false;
    }
    public static bool TryGetPreviousString(DebugUICommand command, out string val)
    {
        if (controlIdToString.TryGetValue(command.ControlId, out val))
        {
            return true;
        }

        return false;
    }
    public static bool TryGetPreviousFloat(DebugUICommand command, out float val)
    {
        if (controlIdToFloat.TryGetValue(command.ControlId, out val))
        {
            return true;
        }

        return false;
    }

    public static void StartDebug()
    {
        controls.Clear();
    }

    public static void EndDebug()
    {
        controlIdToBool.Clear();
        controlIdToInt.Clear();
        controlIdToFloat.Clear();
        controlIdToString.Clear();

        if (_styleDirty)
        {
            _styleDirty = false;
            SaveStyle();
        }
    }

    public static void Render()
    {
        if (!_styleLoaded)
        {
            _styleLoaded = true;
            LoadStyle();
        }

        if (GameSystem.latestTime == null) return;

        renderer.BeforeLayout(GameSystem.latestTime);

        while (controls.Count > 0)
        {
            var ctrl = controls.Dequeue();
            var ctrlId = ctrl.ControlId;
            switch (ctrl.type)
            {
                case DebugControlType.WINDOW_START:
                    ImGui.Begin(ctrl.label);
                    break;
                case DebugControlType.WINDOW_END:
                    ImGui.End();
                    break;
                case DebugControlType.BUTTON:
                    var wasClicked = ImGui.Button(ctrl.label);
                    controlIdToBool[ctrlId] = wasClicked;
                    break;
                case DebugControlType.INT_SLIDER:
                    var intMin = controls.Dequeue();
                    var intMax = controls.Dequeue();
                    controlIdToBool[ctrlId] = ImGui.SliderInt(ctrl.label, ref ctrl.argInt, intMin.argInt, intMax.argInt);
                    controlIdToInt[ctrlId] = ctrl.argInt;
                    break;
                case DebugControlType.FLOAT_SLIDER:
                    var floatMin = controls.Dequeue();
                    var floatMax = controls.Dequeue();
                    controlIdToBool[ctrlId] = ImGui.SliderFloat(ctrl.label, ref ctrl.argFloat, floatMin.argFloat, floatMax.argFloat);
                    controlIdToFloat[ctrlId] = ctrl.argFloat;
                    break;
                case DebugControlType.TEXTFIELD:
                    var strHint = controls.Dequeue();
                    var maxLen = controls.Dequeue();
                    controlIdToBool[ctrlId] = ImGui.InputTextWithHint(ctrl.label, strHint.argString, ref ctrl.argString, (uint)maxLen.argInt, ImGuiInputTextFlags.AutoSelectAll);
                    controlIdToString[ctrlId] = ctrl.argString;
                    break;
                case DebugControlType.LABEL:
                    ImGui.LabelText(ctrl.label, ctrl.argString);
                    break;
                case DebugControlType.SAME_LINE:
                    ImGui.SameLine();
                    break;
                case DebugControlType.CHECKBOX:
                    var checkVal = ctrl.argInt > 0;
                    var checkChanged = ImGui.Checkbox(ctrl.label, ref checkVal);
                    controlIdToBool[ctrlId] = checkChanged;
                    controlIdToInt[ctrlId] = checkVal ? 1 : 0;
                    break;
                case DebugControlType.SEPARATOR:
                    ImGui.Separator();
                    break;
                case DebugControlType.TEXT:
                    ImGui.Text(ctrl.argString);
                    break;
                case DebugControlType.TREE_START:
                    var treeOpen = ImGui.TreeNodeEx(ctrl.label, ImGuiTreeNodeFlags.DefaultOpen);
                    controlIdToBool[ctrlId] = treeOpen;
                    break;
                case DebugControlType.TREE_END:
                    ImGui.TreePop();
                    break;
                case DebugControlType.COLOR_PICKER:
                    var colorVec = new Vector4(
                        ((ctrl.argInt >> 24) & 0xFF) / 255f,
                        ((ctrl.argInt >> 16) & 0xFF) / 255f,
                        ((ctrl.argInt >> 8) & 0xFF) / 255f,
                        (ctrl.argInt & 0xFF) / 255f);
                    var colorChanged = ImGui.ColorEdit4(ctrl.label, ref colorVec);
                    controlIdToBool[ctrlId] = colorChanged;
                    var r = (int)(colorVec.X * 255) & 0xFF;
                    var g = (int)(colorVec.Y * 255) & 0xFF;
                    var b = (int)(colorVec.Z * 255) & 0xFF;
                    var a = (int)(colorVec.W * 255) & 0xFF;
                    controlIdToInt[ctrlId] = (r << 24) | (g << 16) | (b << 8) | a;
                    break;
                case DebugControlType.DRAG_INT:
                    controlIdToBool[ctrlId] = ImGui.DragInt(ctrl.label, ref ctrl.argInt);
                    controlIdToInt[ctrlId] = ctrl.argInt;
                    break;
                case DebugControlType.DRAG_FLOAT:
                    controlIdToBool[ctrlId] = ImGui.DragFloat(ctrl.label, ref ctrl.argFloat);
                    controlIdToFloat[ctrlId] = ctrl.argFloat;
                    break;
                case DebugControlType.TAB_BAR_START:
                    controlIdToBool[ctrlId] = ImGui.BeginTabBar(ctrl.label);
                    break;
                case DebugControlType.TAB_BAR_END:
                    ImGui.EndTabBar();
                    break;
                case DebugControlType.TAB_ITEM_START:
                    controlIdToBool[ctrlId] = ImGui.BeginTabItem(ctrl.label);
                    break;
                case DebugControlType.TAB_ITEM_END:
                    ImGui.EndTabItem();
                    break;

                // components
                case DebugControlType.COMPONENT_SPRITE:
                    RenderSprite(ctrl);
                    break;
                case DebugControlType.COMPONENT_EFFECT:
                    RenderEffect(ctrl);
                    break;
                case DebugControlType.COMPONENT_TRANSFORM:
                    RenderTransform(ctrl);
                    break;
                case DebugControlType.COMPONENT_TWEEN:
                    RenderTween(ctrl);
                    break;
                case DebugControlType.COMPONENT_COLLIDER:
                    RenderCollider(ctrl);
                    break;
                case DebugControlType.COMPONENT_TEXT:
                    RenderText(ctrl);
                    break;
                case DebugControlType.COMPONENT_SFX:
                    RenderSfx(ctrl);
                    break;
                case DebugControlType.COMPONENT_TEXTURE:
                    RenderTexture(ctrl);
                    break;
                case DebugControlType.COMPONENT_RENDER_OUTPUT:
                    RenderRenderOutput(ctrl);
                    break;
                case DebugControlType.COMPONENT_METADATA:
                    RenderMetadata(ctrl);
                    break;

                // composite
                case DebugControlType.INSPECTOR:
                    RenderInspector(ctrl);
                    break;
                case DebugControlType.CONSOLE:
                    RenderConsole(ctrl);
                    break;

                // browsers
                case DebugControlType.BROWSER_SPRITE:
                    BrowseSprites(ctrl);
                    break;
                case DebugControlType.BROWSER_EFFECT:
                    BrowseEffects(ctrl);
                    break;
                case DebugControlType.BROWSER_TRANSFORM:
                    BrowseTransforms(ctrl);
                    break;
                case DebugControlType.BROWSER_TWEEN:
                    BrowseTweens(ctrl);
                    break;
                case DebugControlType.BROWSER_COLLIDER:
                    BrowseColliders(ctrl);
                    break;
                case DebugControlType.BROWSER_TEXT:
                    BrowseTexts(ctrl);
                    break;
                case DebugControlType.BROWSER_SFX:
                    BrowseSfxInstances(ctrl);
                    break;
                case DebugControlType.BROWSER_TEXTURE:
                    BrowseTextures(ctrl);
                    break;
                case DebugControlType.BROWSER_RENDER_OUTPUT:
                    BrowseRenderOutputs(ctrl);
                    break;
            }
        }

        renderer.AfterLayout();
    }

    // ── component renderers ─────────────────────────────────

    static void RenderSprite(DebugUICommand ctrl, bool useTree = true)
    {
        var spriteId = ctrl.argInt;
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        var open = !useTree || ImGui.TreeNodeEx("sprite(" + spriteId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;
            changed |= Color("color", ref sprite.color);
            changed |= Vec2Input("position", ref sprite.position);
            changed |= Vec2Input("scale", ref sprite.scale);
            changed |= Vec2Slider("origin", ref sprite.origin);

            var rotation = sprite.rotation;
            if (ImGui.SliderFloat("rotation", ref rotation, -(float)Math.PI, (float)Math.PI))
            {
                sprite.rotation = rotation;
                changed = true;
            }

            changed |= ImGui.Checkbox("hidden", ref sprite.hidden);

            var zOrder = sprite.zOrder;
            if (ImGui.DragInt("z-order", ref zOrder))
            {
                sprite.zOrder = zOrder;
                changed = true;
            }

            // texture dropdown
            CollectTextureIds();
            var imageId = sprite.imageId;
            if (ResourceIdCombo("imageId", ref imageId, _idScratch, "spr_img" + spriteId))
            {
                sprite.imageId = imageId;
                changed = true;
            }
            TexturePreviewSmall(sprite.imageId);

            ImGui.TextDisabled("frame: " + sprite.currentFrame);

            // effect dropdown
            CollectEffectIds();
            var effectId = sprite.effectId;
            if (ResourceIdCombo("effectId", ref effectId, _idScratch, "spr_fx" + spriteId))
            {
                sprite.effectId = effectId;
                changed = true;
            }

            // transform dropdown
            CollectTransformIds();
            var anchorId = sprite.anchorTransformId;
            if (ResourceIdCombo("anchorTransformId", ref anchorId, _idScratch, "spr_tf" + spriteId))
            {
                sprite.anchorTransformId = anchorId;
                changed = true;
            }

            // render output dropdown
            CollectRenderOutputIds();
            var outputId = sprite.outputIdFlags;
            if (ResourceIdCombo("renderTarget", ref outputId, _idScratch, "spr_out" + spriteId))
            {
                RenderSystem.SetSpriteToOutput(index, outputId, sprite.outputIdFlags);
                sprite.outputIdFlags = outputId;
                changed = true;
            }

            if (changed)
            {
                SpriteSystem.sprites[index] = sprite;
                controlIdToBool[ctrl.ControlId] = true;
            }
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderEffect(DebugUICommand ctrl, bool useTree = true)
    {
        var effectId = ctrl.argInt;
        RenderSystem.GetEffectIndex(effectId, out var index, out var fx);
        var name = "#invalid";
        if (string.IsNullOrEmpty(fx.filePath))
        {
            name = "#nofile";
        }
        else
        {
            name = fx.effect.Name;
        }

        var open = !useTree || ImGui.TreeNodeEx("effect(" + effectId + ") " + name, ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;
            var updatedAgo = DateTimeOffset.Now - fx.watchedEffect.UpdatedAt;
            ImGui.TextDisabled("last loaded: " + updatedAgo.ToString());
            foreach (var parameter in fx.effect.Parameters)
            {
                switch (parameter.ParameterType)
                {
                    case EffectParameterType.Texture2D:
                        ImGui.TextDisabled(parameter.Name + ": <texture>");
                        break;
                    case EffectParameterType.Single:

                        switch (parameter.ColumnCount)
                        {
                            case 1:
                                var value = parameter.GetValueSingle();
                                if (ImGui.SliderFloat(parameter.Name, ref value, 0, 1))
                                {
                                    changed = true;
                                    parameter.SetValue(value);
                                }
                                break;
                            case 2:
                                var vec2 = parameter.GetValueVector2();
                                if (Vec2Slider(parameter.Name, ref vec2))
                                {
                                    changed = true;
                                    parameter.SetValue(vec2);
                                }
                                break;
                            case 3:
                                var vec3 = parameter.GetValueVector3();
                                var imguiValue3 = new Vector3(vec3.X, vec3.Y, vec3.Z);
                                if (ImGui.SliderFloat3(parameter.Name, ref imguiValue3, 0, 1))
                                {
                                    changed = true;
                                    parameter.SetValue(new Microsoft.Xna.Framework.Vector3(imguiValue3.X, imguiValue3.Y, imguiValue3.Z));
                                }
                                break;
                            case 4:

                                if (parameter.ParameterClass == EffectParameterClass.Matrix)
                                {
                                    ImGui.TextDisabled(parameter.Name + ": <matrix4>");
                                    break;
                                }
                                
                                var vec4 = parameter.GetValueVector4();
                                var imguiValue4 = new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
                                if (ImGui.SliderFloat4(parameter.Name, ref imguiValue4, 0, 1))
                                {
                                    changed = true;
                                    parameter.SetValue(new Microsoft.Xna.Framework.Vector4(imguiValue4.X, imguiValue4.Y, imguiValue4.Z, imguiValue4.W));
                                }
                                break;
                            default:
                                ImGui.TextDisabled("(unsupported column count) " + parameter.ParameterType + " / " + parameter.ColumnCount);
                                break;
                        }
                        break;
                    default:
                        ImGui.TextDisabled("(unsupported parameter type) " + parameter.ParameterType);
                        break;
                }
            }
            controlIdToBool[ctrl.ControlId] = changed;
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderTransform(DebugUICommand ctrl, bool useTree = true)
    {
        var transformId = ctrl.argInt;
        TransformSystem.GetTransformIndex(transformId, out var index, out var transform);
        var open = !useTree || ImGui.TreeNodeEx("transform(" + transformId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;
            changed |= Vec2Input("position", ref transform.position);
            changed |= Vec2Input("scale", ref transform.scale);

            var angle = transform.angle;
            if (ImGui.SliderFloat("angle", ref angle, -(float)Math.PI, (float)Math.PI))
            {
                transform.angle = angle;
                changed = true;
            }

            // parent transform dropdown
            CollectTransformIds();
            var parentIdx = transform.parentIndex;
            // parentIndex is an internal array index, convert to user ID for the combo
            var parentId = parentIdx > 0 ? TransformSystem.transforms[parentIdx].id : 0;
            if (ResourceIdCombo("parent", ref parentId, _idScratch, "tf_parent" + transformId))
            {
                // convert back: find the internal index for the chosen ID
                if (parentId == 0)
                {
                    transform.parentIndex = 0;
                }
                else
                {
                    TransformSystem.GetTransformIndex(parentId, out var pIdx, out _);
                    transform.parentIndex = pIdx;
                }
                changed = true;
            }
            ImGui.TextDisabled("referenceCount: " + transform.referenceCount);

            if (changed)
            {
                TransformSystem.transforms[index] = transform;
                controlIdToBool[ctrl.ControlId] = true;
            }
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderTween(DebugUICommand ctrl, bool useTree = true)
    {
        var tweenId = ctrl.argInt;
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        var open = !useTree || ImGui.TreeNodeEx("tween(" + tweenId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            ImGui.Text("playing: " + (tween.isPlaying ? "yes" : "no"));

            var progress = tween.interpolator;
            ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{progress:F2}");

            ImGui.TextDisabled("value: " + tween.currValue.ToString("F3"));
            ImGui.TextDisabled("range: " + tween.startValue.ToString("F1") + " -> " + tween.endValue.ToString("F1"));
            ImGui.TextDisabled("easing: " + tween.type);
            ImGui.TextDisabled("mode: " + tween.executionType);

            controlIdToBool[ctrl.ControlId] = false;
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderCollider(DebugUICommand ctrl, bool useTree = true)
    {
        var colliderId = ctrl.argInt;
        CollisionSystem.GetColliderIndex(colliderId, out var index, out var collider);
        var open = !useTree || ImGui.TreeNodeEx("collider(" + colliderId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;
            changed |= Vec2Input("position", ref collider.position);
            changed |= Vec2Input("size", ref collider.size);

            // target transform dropdown
            CollectTransformIds();
            var tfId = collider.targetTransformId;
            if (ResourceIdCombo("targetTransform", ref tfId, _idScratch, "col_tf" + colliderId))
            {
                collider.targetTransformId = tfId;
                changed = true;
            }

            ImGui.TextDisabled("computed pos: " + collider.computedPosition);
            ImGui.TextDisabled("computed size: " + collider.computedSize);

            var isHitting = CollisionSystem.colliderIdToHitIds.ContainsKey(colliderId);
            if (isHitting)
            {
                ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), "HITTING");
            }

            if (changed)
            {
                CollisionSystem.aabbs[index] = collider;
                controlIdToBool[ctrl.ControlId] = true;
            }
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderText(DebugUICommand ctrl, bool useTree = true)
    {
        var textId = ctrl.argInt;
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        var open = !useTree || ImGui.TreeNodeEx("text(" + textId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;

            var text = textSprite.text ?? "";
            if (ImGui.InputText("text", ref text, 1024))
            {
                textSprite.text = text;
                changed = true;
            }

            changed |= Color("color", ref textSprite.sprite.color);
            changed |= Vec2Input("position", ref textSprite.sprite.position);
            changed |= Vec2Input("scale", ref textSprite.sprite.scale);
            changed |= ImGui.Checkbox("hidden", ref textSprite.sprite.hidden);

            var zOrder = textSprite.sprite.zOrder;
            if (ImGui.DragInt("z-order", ref zOrder))
            {
                textSprite.sprite.zOrder = zOrder;
                changed = true;
            }

            // font is stored in imageId for text sprites
            ImGui.TextDisabled("fontId: " + textSprite.sprite.imageId);

            // anchor transform dropdown
            CollectTransformIds();
            var anchorId = textSprite.sprite.anchorTransformId;
            if (ResourceIdCombo("anchorTransformId", ref anchorId, _idScratch, "txt_tf" + textId))
            {
                textSprite.sprite.anchorTransformId = anchorId;
                changed = true;
            }

            if (textSprite.dropShadowEnabled)
            {
                ImGui.TextDisabled("drop shadow: on");
                changed |= Color("shadow color", ref textSprite.dropShadowColor);
                changed |= Vec2Input("shadow offset", ref textSprite.dropShadowOffset);
            }
            else
            {
                ImGui.TextDisabled("drop shadow: off");
            }

            if (changed)
            {
                TextSystem.textSprites[index] = textSprite;
                controlIdToBool[ctrl.ControlId] = true;
            }
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderSfx(DebugUICommand ctrl, bool useTree = true)
    {
        var sfxId = ctrl.argInt;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        var open = !useTree || ImGui.TreeNodeEx("sfx(" + sfxId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;

            if (sfx.instance != null)
            {
                ImGui.Text("state: " + sfx.instance.State);

                var pitch = sfx.instance.Pitch;
                if (ImGui.SliderFloat("pitch", ref pitch, -1f, 1f))
                {
                    sfx.instance.Pitch = pitch;
                    changed = true;
                }

                var pan = sfx.instance.Pan;
                if (ImGui.SliderFloat("pan", ref pan, -1f, 1f))
                {
                    sfx.instance.Pan = pan;
                    changed = true;
                }

                var volume = sfx.instance.Volume;
                if (ImGui.SliderFloat("volume", ref volume, 0f, 1f))
                {
                    sfx.instance.Volume = volume;
                    changed = true;
                }

                var looped = sfx.instance.IsLooped;
                if (ImGui.Checkbox("looped", ref looped))
                {
                    sfx.instance.IsLooped = looped;
                    changed = true;
                }

                ImGui.SameLine();
                if (ImGui.Button("Play##sfx" + sfxId))
                {
                    sfx.instance.Stop();
                    sfx.instance.Play();
                }
                ImGui.SameLine();
                if (ImGui.Button("Stop##sfx" + sfxId))
                {
                    sfx.instance.Stop();
                }
            }
            else
            {
                ImGui.TextDisabled("(no instance)");
            }

            controlIdToBool[ctrl.ControlId] = changed;
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderTexture(DebugUICommand ctrl, bool useTree = true)
    {
        var textureId = ctrl.argInt;
        TextureSystem.GetTextureIndex(textureId, out _, out var runtimeTex);
        var open = !useTree || ImGui.TreeNodeEx("texture(" + textureId + ")", ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var tex = runtimeTex.texture;
            if (tex != null)
            {
                ImGui.TextDisabled(tex.Width + " x " + tex.Height);
                if (!string.IsNullOrEmpty(runtimeTex.descriptor.imageFilePath))
                    ImGui.TextDisabled("path: " + runtimeTex.descriptor.imageFilePath);

                if (runtimeTex.descriptor.frames != null && runtimeTex.descriptor.frames.Count > 0)
                    ImGui.TextDisabled("frames: " + runtimeTex.descriptor.frames.Count);

                // show the texture as an image
                var ptr = GetOrBindTexture(textureId, tex);
                var previewSize = FitSize(tex.Width, tex.Height, 128);
                ImGui.Image(ptr, previewSize);
            }
            else
            {
                ImGui.TextDisabled("(not loaded)");
            }

            controlIdToBool[ctrl.ControlId] = false;
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderRenderOutput(DebugUICommand ctrl, bool useTree = true)
    {
        var outputId = ctrl.argInt;
        RenderSystem.GetOutputIndex(outputId, out var index, out var output);

        var hasTarget = output.target != null;
        var header = hasTarget
            ? "output(" + outputId + ") " + output.target.Width + "x" + output.target.Height
            : "output(" + outputId + ") screen";

        var open = !useTree || ImGui.TreeNodeEx(header, ImGuiTreeNodeFlags.DefaultOpen);
        if (open)
        {
            var changed = false;

            changed |= Color("clearColor", ref output.clearColor);

            var clearTarget = output.clearTarget;
            if (ImGui.Checkbox("clearTarget", ref clearTarget))
            {
                output.clearTarget = clearTarget;
                changed = true;
            }

            ImGui.TextDisabled("items: " + output.orderedItems.Count);

            // texture reference
            if (output.targetTextureId > 0)
            {
                ImGui.TextDisabled("targetTextureId: " + output.targetTextureId);
            }

            // preview the render target if one exists
            if (hasTarget)
            {
                var ptr = GetOrBindRenderTarget(outputId, output.target);
                var previewSize = FitSize(output.target.Width, output.target.Height, 128);
                ImGui.Image(ptr, previewSize);
            }

            controlIdToBool[ctrl.ControlId] = changed;
            if (useTree) ImGui.TreePop();
        }
    }

    static void RenderMetadata(DebugUICommand ctrl)
    {
        // ── performance (always at top, under a tree) ───────
        if (ImGui.TreeNodeEx("Performance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var elapsed = GameSystem.latestTime?.ElapsedGameTime.TotalSeconds ?? 0;
            var fps = elapsed > 0 ? 1.0 / elapsed : 0;
            var frameMs = elapsed * 1000.0;
            var gameTimeSec = GameSystem.latestTime?.TotalGameTime.TotalSeconds ?? 0;

            RecordFpsSample(gameTimeSec, (float)fps);

            var min60 = GetFpsMinOverSeconds(gameTimeSec, 60);
            var avg10m = GetFpsAvgOverSeconds(gameTimeSec, 600);

            ImGui.Text($"FPS: {fps:F1}");
            ImGui.SameLine();
            ImGui.TextDisabled($"({frameMs:F1} ms)  min60s: {min60:F0}  avg10m: {avg10m:F0}");

            // fps history chart
            ImGui.PlotLines("##fps", ref _fpsHistory[0], FPS_HISTORY, _fpsHistoryIdx % FPS_HISTORY,
                null, 0, 120f, new Vector2(-1, 32));

            // memory
            RenderMemoryChart();

            // GC collections
            ImGui.TextDisabled($"GC: gen0={GC.CollectionCount(0)}  gen1={GC.CollectionCount(1)}  gen2={GC.CollectionCount(2)}");

            // draw item count
            var drawItems = 0;
            for (var i = 0; i < RenderSystem.outputs.Count; i++)
                drawItems += RenderSystem.outputs[i].orderedItems.Count;
            ImGui.TextDisabled($"draw items: {drawItems}  (across {RenderSystem.outputs.Count} outputs)");

            ImGui.TreePop();
        }

        ImGui.Separator();

        // ── game info ───────────────────────────────────────
        ImGui.TextDisabled("game time: " + (GameSystem.latestTime?.TotalGameTime.TotalSeconds ?? 0).ToString("F1") + "s");

        ImGui.Separator();

        // ── debugger ────────────────────────────────────────
        RenderDebuggerSection();

        ImGui.Separator();

        // ── window controls ─────────────────────────────────
        if (GameSystem.graphicsDeviceManager != null)
        {
            var gdm = GameSystem.graphicsDeviceManager;
            var winW = gdm.PreferredBackBufferWidth;
            var winH = gdm.PreferredBackBufferHeight;
            ImGui.Text("window: " + winW + "x" + winH);

            var isFs = gdm.IsFullScreen;
            if (ImGui.Checkbox("fullscreen", ref isFs))
            {
                if (isFs)
                {
                    _windowedWidth = gdm.PreferredBackBufferWidth;
                    _windowedHeight = gdm.PreferredBackBufferHeight;
                    gdm.IsFullScreen = true;
                    gdm.PreferredBackBufferWidth = gdm.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                    gdm.PreferredBackBufferHeight = gdm.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
                }
                else
                {
                    gdm.IsFullScreen = false;
                    gdm.PreferredBackBufferWidth = _windowedWidth;
                    gdm.PreferredBackBufferHeight = _windowedHeight;
                }
                gdm.ApplyChanges();
                RenderSystem.ResetRenderPositioning();
            }

            if (!isFs)
            {
                ImGui.SetNextItemWidth(80);
                ImGui.InputInt("W##win", ref winW);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(80);
                ImGui.InputInt("H##win", ref winH);
                ImGui.SameLine();
                if (ImGui.Button("Apply##win"))
                {
                    gdm.PreferredBackBufferWidth = winW;
                    gdm.PreferredBackBufferHeight = winH;
                    gdm.ApplyChanges();
                    RenderSystem.ResetRenderPositioning();
                    _windowedWidth = winW;
                    _windowedHeight = winH;
                }
            }
        }
        if (RenderSystem.mainBuffer != null)
        {
            ImGui.TextDisabled("render buffer: " + RenderSystem.mainBuffer.Width
                + "x" + RenderSystem.mainBuffer.Height);
        }

        ImGui.Separator();

        // ── ImGui style ─────────────────────────────────────
        if (ImGui.TreeNode("ImGui Style"))
        {
            var io = ImGui.GetIO();
            var scale = io.FontGlobalScale;
            if (ImGui.SliderFloat("font scale", ref scale, 0.5f, 3f))
            { io.FontGlobalScale = scale; _styleDirty = true; }

            var style = ImGui.GetStyle();

            var windowRounding = style.WindowRounding;
            if (ImGui.SliderFloat("window rounding", ref windowRounding, 0, 16))
            { style.WindowRounding = windowRounding; _styleDirty = true; }

            var frameRounding = style.FrameRounding;
            if (ImGui.SliderFloat("frame rounding", ref frameRounding, 0, 16))
            { style.FrameRounding = frameRounding; _styleDirty = true; }

            ImGui.Separator();

            var colors = style.Colors;
            _styleDirty |= StyleColor("WindowBg", colors, ImGuiCol.WindowBg);
            _styleDirty |= StyleColor("TitleBg", colors, ImGuiCol.TitleBgActive);
            _styleDirty |= StyleColor("FrameBg", colors, ImGuiCol.FrameBg);
            _styleDirty |= StyleColor("Button", colors, ImGuiCol.Button);
            _styleDirty |= StyleColor("ButtonHovered", colors, ImGuiCol.ButtonHovered);
            _styleDirty |= StyleColor("Header", colors, ImGuiCol.Header);
            _styleDirty |= StyleColor("HeaderHovered", colors, ImGuiCol.HeaderHovered);
            _styleDirty |= StyleColor("Tab", colors, ImGuiCol.Tab);
            _styleDirty |= StyleColor("TabSelected", colors, ImGuiCol.TabSelected);
            _styleDirty |= StyleColor("Text", colors, ImGuiCol.Text);
            _styleDirty |= StyleColor("TextDisabled", colors, ImGuiCol.TextDisabled);

            ImGui.Separator();
            if (ImGui.Button("Reset"))
            {
                ImGui.StyleColorsDark();
                io.FontGlobalScale = 1f;
                EnsureTransparentBg();
                _styleDirty = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Light"))
            {
                ImGui.StyleColorsLight();
                EnsureTransparentBg();
                _styleDirty = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Classic"))
            {
                ImGui.StyleColorsClassic();
                EnsureTransparentBg();
                _styleDirty = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Nord"))
            {
                ApplyNordTheme();
                _styleDirty = true;
            }
            if (ImGui.Button("Dracula"))
            {
                ApplyDraculaTheme();
                _styleDirty = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Solarized"))
            {
                ApplySolarizedTheme();
                _styleDirty = true;
            }

            ImGui.TreePop();
        }

        ImGui.Separator();

        // ── resource counts ─────────────────────────────────
        if (ImGui.TreeNode("Resource Counts"))
        {
            ImGui.TextDisabled("sprites: " + SpriteSystem.spriteCount);
            ImGui.TextDisabled("textures: " + TextureSystem.textures.Count);
            ImGui.TextDisabled("transforms: " + TransformSystem.transformCount);
            ImGui.TextDisabled("tweens: " + TweenSystem.tweenCount);
            ImGui.TextDisabled("colliders: " + CollisionSystem.AabbsCount);
            ImGui.TextDisabled("texts: " + TextSystem.textSpriteCount);
            ImGui.TextDisabled("effects: " + RenderSystem.effects.Count);
            ImGui.TextDisabled("render outputs: " + RenderSystem.outputs.Count);
            ImGui.TextDisabled("sfx instances: " + AudioInstanceSystem.audioEffects.Count);
            ImGui.TextDisabled("sfx clips: " + AudioSystem.sfxClips.Count);
            ImGui.TreePop();
        }
    }

    static void RenderDebuggerSection()
    {
        if (!ImGui.TreeNodeEx("Debugger", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        // colors used for status pills
        var ok = new Vector4(0.45f, 0.85f, 0.45f, 1f);
        var warn = new Vector4(0.95f, 0.75f, 0.30f, 1f);
        var dim = new Vector4(0.65f, 0.65f, 0.65f, 1f);

        // build configuration
#if DEBUG
        ImGui.Text("build:");
        ImGui.SameLine();
        ImGui.TextColored(warn, "DEBUG");
#else
        ImGui.Text("build:");
        ImGui.SameLine();
        ImGui.TextColored(dim, "RELEASE");
#endif

        // fade debug session info
        var sess = debugSession;
        if (sess != null)
        {
            var opts = sess._options;
            var debugOn = opts != null && opts.debug;
            ImGui.Text("fade debug mode:");
            ImGui.SameLine();
            ImGui.TextColored(debugOn ? ok : dim, debugOn ? "enabled" : "disabled");

            ImGui.Text("fade debugger:");
            ImGui.SameLine();
            ImGui.TextColored(sess.IsClientConnected ? ok : dim, sess.IsClientConnected ? "attached" : "not attached");

            if (opts != null)
            {
                ImGui.TextDisabled("debug server port: " + opts.debugPort);
                ImGui.TextDisabled("wait for connection: " + (opts.debugWaitForConnection ? "yes" : "no"));
                if (!string.IsNullOrEmpty(opts.debugLogPath))
                    ImGui.TextDisabled("log: " + opts.debugLogPath);
            }

            ImGui.Text("vm:");
            ImGui.SameLine();
            ImGui.TextColored(sess.IsPaused ? warn : ok, sess.IsPaused ? "paused" : "running");
            ImGui.SameLine();
            ImGui.TextDisabled("ip=" + sess.InstructionPointer);
        }
        else
        {
            ImGui.TextDisabled("fade debug session: none");
        }

        // .NET debugger attached (IDE/managed debugger) — secondary, shown after the fade debugger
        var managedAttached = Debugger.IsAttached;
        ImGui.Text(".net debugger:");
        ImGui.SameLine();
        ImGui.TextColored(managedAttached ? ok : dim, managedAttached ? "attached" : "not attached");

        // process info — useful when attaching an external debugger
        try
        {
            var proc = Process.GetCurrentProcess();
            ImGui.TextDisabled("process: " + proc.ProcessName + " (pid " + proc.Id + ")");
        }
        catch { /* permissions can refuse this in odd environments */ }

        ImGui.Separator();

        // ── script reload ───────────────────────────────────
        var runtime = GameReloader.LatestRuntime;
        if (runtime != null)
        {
            var ago = DateTimeOffset.Now - GameReloader.LastBuildTime;
            ImGui.TextDisabled("last script load: " + FormatTimeAgo(ago));
        }
        else
        {
            ImGui.TextDisabled("script: not loaded");
        }

        var newBuild = isNewBuildAvailable != null && isNewBuildAvailable();
        if (newBuild)
        {
            ImGui.TextColored(ok, "new build ready");
        }
        else
        {
            ImGui.TextDisabled("no pending build");
        }

        // disable the button if there is nothing to reload or no handler wired
        var canReload = newBuild && requestReload != null;
        if (!canReload) ImGui.BeginDisabled();
        if (ImGui.Button("Reload now (F1)"))
        {
            requestReload?.Invoke();
        }
        if (!canReload) ImGui.EndDisabled();

        ImGui.TreePop();
    }

    static string FormatTimeAgo(TimeSpan ago)
    {
        if (ago.TotalSeconds < 60) return $"{ago.TotalSeconds:F0}s ago";
        if (ago.TotalMinutes < 60) return $"{ago.TotalMinutes:F0}m ago";
        return $"{ago.TotalHours:F1}h ago";
    }

    static void RenderInspector(DebugUICommand ctrl)
    {
        // perf header — always visible, collapsible
        RenderInspectorHeader();

        ImGui.Separator();

        // single tab bar for everything — tools first, then resource browsers
        if (ImGui.BeginTabBar("inspector_tabs", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Console"))
            {
                RenderConsole(ctrl with { label = "console_tab" });
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Settings"))
            {
                RenderMetadata(ctrl);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Sprites"))
            {
                BrowseSprites(ctrl with { label = "browse_sprites_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Textures"))
            {
                BrowseTextures(ctrl with { label = "browse_textures_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Transforms"))
            {
                BrowseTransforms(ctrl with { label = "browse_transforms_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Tweens"))
            {
                BrowseTweens(ctrl with { label = "browse_tweens_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Colliders"))
            {
                BrowseColliders(ctrl with { label = "browse_colliders_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Texts"))
            {
                BrowseTexts(ctrl with { label = "browse_texts_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Effects"))
            {
                BrowseEffects(ctrl with { label = "browse_effects_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("SFX"))
            {
                BrowseSfxInstances(ctrl with { label = "browse_sfx_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Outputs"))
            {
                BrowseRenderOutputs(ctrl with { label = "browse_outputs_tab" }, useTree: false);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    /// Compact perf summary line that always shows at the top of the inspector.
    static void RenderInspectorHeader()
    {
        var elapsed = GameSystem.latestTime?.ElapsedGameTime.TotalSeconds ?? 0;
        var fps = elapsed > 0 ? 1.0 / elapsed : 0;
        var frameMs = elapsed * 1000.0;
        var gameTimeSec = GameSystem.latestTime?.TotalGameTime.TotalSeconds ?? 0;

        RecordFpsSample(gameTimeSec, (float)fps);

        var min60 = GetFpsMinOverSeconds(gameTimeSec, 60);
        var avg10m = GetFpsAvgOverSeconds(gameTimeSec, 600);

        var totalBytes = GC.GetTotalMemory(false);
        var totalMb = (float)(totalBytes / (1024.0 * 1024.0));
        var baselineMb = _baselineBytes > 0 ? (float)(_baselineBytes / (1024.0 * 1024.0)) : 0f;
        var dynamicMb = _baselineBytes > 0 ? Math.Max(0, totalMb - baselineMb) : totalMb;

        var drawItems = 0;
        for (var i = 0; i < RenderSystem.outputs.Count; i++)
            drawItems += RenderSystem.outputs[i].orderedItems.Count;

        ImGui.Text($"FPS: {fps:F0} ({frameMs:F1}ms)");
        ImGui.SameLine();
        ImGui.TextDisabled($"| min60s: {min60:F0} | avg10m: {avg10m:F0} | mem: {dynamicMb:F0}MB | draws: {drawItems}");
    }

    // ── browsers ─────────────────────────────────────────────

    static readonly List<int> _idScratch = new List<int>();
    static readonly List<string> _idLabelScratch = new List<string>();

    /// Expects _idScratch and _idLabelScratch to already be populated by a collector.
    /// Returns the selected resource ID, or -1 if empty or collapsed.
    /// When useTree is true, wraps content in a collapsible tree node. When false, renders inline.
    static int BrowserIdPicker(DebugUICommand ctrl, string typeName, List<int> ids, bool useTree = true)
    {
        if (useTree)
        {
            if (!ImGui.TreeNodeEx(typeName + " (" + ids.Count + ")##" + typeName + "_tree", ImGuiTreeNodeFlags.None))
                return -1;
        }
        else
        {
            ImGui.Text(typeName + ": " + ids.Count);
        }

        if (ids.Count == 0)
        {
            ImGui.TextDisabled("(none)");
            if (useTree) ImGui.TreePop();
            return -1;
        }

        var selectedIdx = 0;
        if (persistentInt.TryGetValue(ctrl.ControlId, out var prev))
        {
            var found = ids.IndexOf(prev);
            if (found >= 0) selectedIdx = found;
        }

        if (ImGui.BeginCombo("id##" + typeName + "_browse", _idLabelScratch[selectedIdx]))
        {
            for (var i = 0; i < ids.Count; i++)
            {
                var isSelected = i == selectedIdx;
                if (ImGui.Selectable(_idLabelScratch[i], isSelected))
                    selectedIdx = i;
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        var selectedId = ids[selectedIdx];
        persistentInt[ctrl.ControlId] = selectedId;
        return selectedId;
    }

    /// Must be called after BrowserIdPicker when it returns >= 0.
    static void BrowserEnd(bool useTree = true)
    {
        if (useTree) ImGui.TreePop();
    }

    static void BrowseSprites(DebugUICommand ctrl, bool useTree = true)
    {
        CollectSpriteIds();
        var id = BrowserIdPicker(ctrl, "Sprites", _idScratch, useTree);
        if (id < 0) return;
        RenderSprite(new DebugUICommand
        {
            label = "sprite_browse", type = DebugControlType.COMPONENT_SPRITE,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = id
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseEffects(DebugUICommand ctrl, bool useTree = true)
    {
        CollectEffectIds();
        var id = BrowserIdPicker(ctrl, "Effects", _idScratch, useTree);
        if (id < 0) return;
        RenderEffect(new DebugUICommand
        {
            label = "effect_browse", type = DebugControlType.COMPONENT_EFFECT,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = id
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseTransforms(DebugUICommand ctrl, bool useTree = true)
    {
        CollectTransformIds();
        var id = BrowserIdPicker(ctrl, "Transforms", _idScratch, useTree);
        if (id < 0) return;
        RenderTransform(new DebugUICommand
        {
            label = "transform_browse", type = DebugControlType.COMPONENT_TRANSFORM,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = id
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseTweens(DebugUICommand ctrl, bool useTree = true)
    {
        ClearScratch();
        for (var i = 0; i < TweenSystem.tweenCount; i++)
        {
            var id = TweenSystem.tweens[i].id;
            _idScratch.Add(id);
            _idLabelScratch.Add(id.ToString());
        }

        var picked = BrowserIdPicker(ctrl, "Tweens", _idScratch, useTree);
        if (picked < 0) return;
        RenderTween(new DebugUICommand
        {
            label = "tween_browse", type = DebugControlType.COMPONENT_TWEEN,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = picked
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseColliders(DebugUICommand ctrl, bool useTree = true)
    {
        ClearScratch();
        for (var i = 0; i < CollisionSystem.AabbsCount; i++)
        {
            var id = CollisionSystem.aabbs[i].id;
            _idScratch.Add(id);
            _idLabelScratch.Add(id.ToString());
        }

        var picked = BrowserIdPicker(ctrl, "Colliders", _idScratch, useTree);
        if (picked < 0) return;
        RenderCollider(new DebugUICommand
        {
            label = "collider_browse", type = DebugControlType.COMPONENT_COLLIDER,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = picked
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseTexts(DebugUICommand ctrl, bool useTree = true)
    {
        ClearScratch();
        for (var i = 0; i < TextSystem.textSpriteCount; i++)
        {
            var ts = TextSystem.textSprites[i];
            var id = ts.sprite.id;
            _idScratch.Add(id);
            var txt = ts.text;
            _idLabelScratch.Add(string.IsNullOrEmpty(txt) ? id.ToString() : id + " - \"" + (txt.Length > 20 ? txt.Substring(0, 20) + "..." : txt) + "\"");
        }

        var picked = BrowserIdPicker(ctrl, "Texts", _idScratch, useTree);
        if (picked < 0) return;
        RenderText(new DebugUICommand
        {
            label = "text_browse", type = DebugControlType.COMPONENT_TEXT,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = picked
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseSfxInstances(DebugUICommand ctrl, bool useTree = true)
    {
        ClearScratch();
        for (var i = 0; i < AudioInstanceSystem.audioEffects.Count; i++)
        {
            var inst = AudioInstanceSystem.audioEffects[i];
            _idScratch.Add(inst.id);
            var state = inst.instance != null ? inst.instance.State.ToString() : "no instance";
            _idLabelScratch.Add(inst.id + " - " + state);
        }

        var picked = BrowserIdPicker(ctrl, "SFX", _idScratch, useTree);
        if (picked < 0) return;
        RenderSfx(new DebugUICommand
        {
            label = "sfx_browse", type = DebugControlType.COMPONENT_SFX,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = picked
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseTextures(DebugUICommand ctrl, bool useTree = true)
    {
        CollectTextureIds();
        var id = BrowserIdPicker(ctrl, "Textures", _idScratch, useTree);
        if (id < 0) return;
        RenderTexture(new DebugUICommand
        {
            label = "texture_browse", type = DebugControlType.COMPONENT_TEXTURE,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = id
        }, useTree);
        BrowserEnd(useTree);
    }

    static void BrowseRenderOutputs(DebugUICommand ctrl, bool useTree = true)
    {
        CollectRenderOutputIds();
        var id = BrowserIdPicker(ctrl, "Render Outputs", _idScratch, useTree);
        if (id < 0) return;
        RenderRenderOutput(new DebugUICommand
        {
            label = "output_browse", type = DebugControlType.COMPONENT_RENDER_OUTPUT,
            vmInstructionIndex = ctrl.vmInstructionIndex, argInt = id
        }, useTree);
        BrowserEnd(useTree);
    }

    // ── id collectors (populate _idScratch + _idLabelScratch) ──

    static void ClearScratch()
    {
        _idScratch.Clear();
        _idLabelScratch.Clear();
    }

    static void CollectSpriteIds()
    {
        ClearScratch();
        for (var i = 0; i < SpriteSystem.spriteCount; i++)
        {
            var id = SpriteSystem.sprites[i].id;
            _idScratch.Add(id);
            _idLabelScratch.Add(id.ToString());
        }
    }

    static void CollectTextureIds()
    {
        // build a quick reverse lookup: textureId -> render output id
        _rtLookup.Clear();
        for (var i = 0; i < RenderSystem.outputs.Count; i++)
        {
            var o = RenderSystem.outputs[i];
            if (o.targetTextureId > 0)
                _rtLookup[o.targetTextureId] = o.id;
        }

        ClearScratch();
        for (var i = 0; i < TextureSystem.textures.Count; i++)
        {
            var t = TextureSystem.textures[i];
            _idScratch.Add(t.id);

            var label = t.id.ToString();
            if (_rtLookup.TryGetValue(t.id, out var outputId))
            {
                label += " - [RT output " + outputId + "]";
            }
            else if (!string.IsNullOrEmpty(t.descriptor.imageFilePath))
            {
                label += " - " + t.descriptor.imageFilePath;
            }
            _idLabelScratch.Add(label);
        }
    }
    static readonly Dictionary<int, int> _rtLookup = new Dictionary<int, int>();

    static void CollectEffectIds()
    {
        ClearScratch();
        for (var i = 0; i < RenderSystem.effects.Count; i++)
        {
            var fx = RenderSystem.effects[i];
            _idScratch.Add(fx.id);
            var name = fx.filePath;
            _idLabelScratch.Add(string.IsNullOrEmpty(name) ? fx.id.ToString() : fx.id + " - " + name);
        }
    }

    static void CollectTransformIds()
    {
        ClearScratch();
        for (var i = 0; i < TransformSystem.transformCount; i++)
        {
            var id = TransformSystem.transforms[i].id;
            _idScratch.Add(id);
            _idLabelScratch.Add(id.ToString());
        }
    }

    static void CollectSfxClipIds()
    {
        ClearScratch();
        for (var i = 0; i < AudioSystem.sfxClips.Count; i++)
        {
            var clip = AudioSystem.sfxClips[i];
            _idScratch.Add(clip.id);
            var name = clip.source?.Name;
            _idLabelScratch.Add(string.IsNullOrEmpty(name) ? clip.id.ToString() : clip.id + " - " + name);
        }
    }

    static void CollectRenderOutputIds()
    {
        ClearScratch();
        for (var i = 0; i < RenderSystem.outputs.Count; i++)
        {
            var o = RenderSystem.outputs[i];
            _idScratch.Add(o.id);
            var hasTarget = o.target != null;
            _idLabelScratch.Add(hasTarget
                ? o.id + " - " + o.target.Width + "x" + o.target.Height
                : o.id + " - screen");
        }
    }

    // ── helpers ──────────────────────────────────────────────

    /// Renders a combo dropdown for picking a resource ID.
    /// Expects _idScratch and _idLabelScratch to already be populated by a collector.
    static bool ResourceIdCombo(string label, ref int currentId, List<int> validIds, string uniqueSuffix)
    {
        // build the preview text for the currently selected item
        var preview = "(none)";
        if (currentId > 0)
        {
            var idx = validIds.IndexOf(currentId);
            preview = idx >= 0 ? _idLabelScratch[idx] : currentId.ToString();
        }

        var changed = false;
        if (ImGui.BeginCombo(label + "##" + uniqueSuffix, preview))
        {
            if (ImGui.Selectable("(none)##" + uniqueSuffix, currentId <= 0))
            {
                currentId = 0;
                changed = true;
            }

            for (var i = 0; i < validIds.Count; i++)
            {
                var isSelected = validIds[i] == currentId;
                if (ImGui.Selectable(_idLabelScratch[i] + "##" + uniqueSuffix, isSelected))
                {
                    currentId = validIds[i];
                    changed = true;
                }
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    /// Gets or lazily binds a MonoGame Texture2D into ImGui's texture system.
    static IntPtr GetOrBindTexture(int fadeTextureId, Texture2D tex)
    {
        if (_boundTextures.TryGetValue(fadeTextureId, out var ptr))
            return ptr;

        ptr = renderer.BindTexture(tex);
        _boundTextures[fadeTextureId] = ptr;
        return ptr;
    }

    // render targets use negative keys to avoid colliding with texture IDs
    static IntPtr GetOrBindRenderTarget(int outputId, RenderTarget2D target)
    {
        var key = -outputId;
        if (_boundTextures.TryGetValue(key, out var ptr))
            return ptr;

        ptr = renderer.BindTexture(target);
        _boundTextures[key] = ptr;
        return ptr;
    }

    /// Shows a small inline texture thumbnail if the texture exists.
    static void TexturePreviewSmall(int textureId)
    {
        if (textureId <= 0) return;
        TextureSystem.GetTextureIndex(textureId, out _, out var rt);
        var tex = rt.texture;
        if (tex == null) return;

        var ptr = GetOrBindTexture(textureId, tex);
        var size = FitSize(tex.Width, tex.Height, 48);
        ImGui.Image(ptr, size);
    }

    /// Scales dimensions to fit inside maxPx on the longest side, preserving aspect ratio.
    static Vector2 FitSize(int w, int h, float maxPx)
    {
        if (w <= 0 || h <= 0) return new Vector2(maxPx, maxPx);
        var scale = maxPx / Math.Max(w, h);
        return new Vector2(w * scale, h * scale);
    }

    static bool Color(string name, ref Color color)
    {
        var mVec = color.ToVector4();
        var vec = new Vector4(mVec.X, mVec.Y, mVec.Z, mVec.W);
        if (ImGui.ColorEdit4(name, ref vec))
        {
            color = new Color(new Microsoft.Xna.Framework.Vector4(vec.X, vec.Y, vec.Z, vec.W));
            return true;
        }

        return false;
    }
    static bool Vec2Slider(string name, ref Microsoft.Xna.Framework.Vector2 vec2, float min=0, float max=1)
    {
        var imguiValue2 = new Vector2(vec2.X, vec2.Y);
        if (ImGui.SliderFloat2(name, ref imguiValue2, min, max))
        {
            vec2 = (new Microsoft.Xna.Framework.Vector2(imguiValue2.X, imguiValue2.Y));
            return true;
        }

        return false;
    }
    static bool Vec2Input(string name, ref Microsoft.Xna.Framework.Vector2 vec2)
    {
        var imguiValue2 = new Vector2(vec2.X, vec2.Y);
        if (ImGui.DragFloat2(name, ref imguiValue2))
        {

            vec2 = (new Microsoft.Xna.Framework.Vector2(imguiValue2.X, imguiValue2.Y));
            return true;
        }

        return false;
    }

    static void EnsureTransparentBg()
    {
        var colors = ImGui.GetStyle().Colors;
        var bg = colors[(int)ImGuiCol.WindowBg];
        bg.W = 0.85f;
        colors[(int)ImGuiCol.WindowBg] = bg;
    }

    static bool StyleColor(string label, RangeAccessor<Vector4> colors, ImGuiCol col)
    {
        var c = colors[(int)col];
        if (ImGui.ColorEdit4(label, ref c, ImGuiColorEditFlags.NoInputs))
        {
            colors[(int)col] = c;
            return true;
        }
        return false;
    }

    // ── perf tracking ───────────────────────────────────────

    // chart history (short ring buffer for the plot line)
    const int FPS_HISTORY = 120;
    static readonly float[] _fpsHistory = new float[FPS_HISTORY];
    static int _fpsHistoryIdx;

    // rolling stats: store (time, fps) pairs for the last 10 minutes
    const int FPS_STATS_MAX = 36000; // ~10 min at 60fps
    static readonly float[] _fpsStatsValues = new float[FPS_STATS_MAX];
    static readonly double[] _fpsStatsTimes = new double[FPS_STATS_MAX];
    static int _fpsStatsCount;
    static int _fpsStatsHead;

    static void RecordFpsSample(double gameTimeSec, float fps)
    {
        // write into chart ring buffer
        _fpsHistory[_fpsHistoryIdx % FPS_HISTORY] = fps;
        _fpsHistoryIdx++;

        // write into stats ring buffer
        var idx = _fpsStatsHead % FPS_STATS_MAX;
        _fpsStatsValues[idx] = fps;
        _fpsStatsTimes[idx] = gameTimeSec;
        _fpsStatsHead++;
        if (_fpsStatsCount < FPS_STATS_MAX) _fpsStatsCount++;
    }

    static float GetFpsMinOverSeconds(double gameTimeSec, double windowSec)
    {
        var min = float.MaxValue;
        var cutoff = gameTimeSec - windowSec;
        var start = _fpsStatsHead - _fpsStatsCount;
        for (var i = _fpsStatsHead - 1; i >= start; i--)
        {
            var idx = ((i % FPS_STATS_MAX) + FPS_STATS_MAX) % FPS_STATS_MAX;
            if (_fpsStatsTimes[idx] < cutoff) break;
            var v = _fpsStatsValues[idx];
            if (v > 0 && v < min) min = v;
        }
        return min == float.MaxValue ? 0 : min;
    }

    static float GetFpsAvgOverSeconds(double gameTimeSec, double windowSec)
    {
        var sum = 0.0;
        var count = 0;
        var cutoff = gameTimeSec - windowSec;
        var start = _fpsStatsHead - _fpsStatsCount;
        for (var i = _fpsStatsHead - 1; i >= start; i--)
        {
            var idx = ((i % FPS_STATS_MAX) + FPS_STATS_MAX) % FPS_STATS_MAX;
            if (_fpsStatsTimes[idx] < cutoff) break;
            sum += _fpsStatsValues[idx];
            count++;
        }
        return count > 0 ? (float)(sum / count) : 0;
    }

    const int MEMORY_HISTORY = 120;
    static readonly float[] _memoryHistory = new float[MEMORY_HISTORY];
    static int _memoryHistoryIdx;
    static float _memoryPeakMb;

    // ── console ──────────────────────────────────────────────

    static string _consoleInput = "";
    static readonly List<ConsoleEntry> _consoleLog = new List<ConsoleEntry>();
    static readonly List<string> _consoleHistory = new List<string>();
    static int _consoleHistoryIdx = -1;
    static bool _consoleScrollToBottom;
    static List<string> _commandNames;
    static int _completionIdx = -1;
    static bool _consoleRefocus;
    static string _completionError;

    struct ConsoleEntry
    {
        public string text;
        public Vector4 color;
        public DebugEvalResult result; // non-null for expandable results
    }

    static void RenderConsole(DebugUICommand ctrl)
    {
        // output area (scrollable)
        var footerHeight = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();
        if (ImGui.BeginChild("console_scroll", new Vector2(0, -footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            for (var i = 0; i < _consoleLog.Count; i++)
            {
                var entry = _consoleLog[i];
                if (entry.result != null && entry.result.scope != null && entry.result.scope.variables.Count > 0)
                {
                    // expandable result — render as a tree
                    ImGui.PushStyleColor(ImGuiCol.Text, entry.color);
                    RenderConsoleResult(entry.result, entry.text, i);
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, entry.color);
                    ImGui.TextWrapped(entry.text);
                    ImGui.PopStyleColor();
                }
            }
            if (_consoleScrollToBottom)
            {
                ImGui.SetScrollHereY(1f);
                _consoleScrollToBottom = false;
            }
        }
        ImGui.EndChild();

        ImGui.Separator();

        // input line
        // input line
        if (_consoleRefocus)
        {
            ImGui.SetKeyboardFocusHere();
            _consoleRefocus = false;
        }
        ImGui.SetNextItemWidth(-100);
        var submitted = ImGui.InputText("##console_in", ref _consoleInput, 2048, ImGuiInputTextFlags.EnterReturnsTrue);
        var inputRectMin = ImGui.GetItemRectMin();
        var inputRectMax = ImGui.GetItemRectMax();

        // if Enter was pressed and a completion is highlighted, accept it instead of submitting
        if (submitted && _completionIdx >= 0)
        {
            var pendingCompletions = _consoleInput.Length > 0 ? GetCompletionMatches(_consoleInput) : _emptyCompletions;
            if (_completionIdx < pendingCompletions.Count)
            {
                _consoleInput = pendingCompletions[_completionIdx].insertText + " ";
                _completionIdx = -1;
                _consoleRefocus = true;
                submitted = false; // don't submit, just accept the completion
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Run") || submitted)
        {
            var input = _consoleInput.Trim();
            if (input.Length > 0)
            {
                ConsoleExec(input);
                _consoleInput = "";
                _consoleHistoryIdx = -1;
                _completionIdx = -1;
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            _consoleLog.Clear();
        }

        // history: simple buttons
        if (_consoleHistory.Count > 0)
        {
            ImGui.SameLine();
            if (ImGui.ArrowButton("##hist_up", ImGuiDir.Up))
            {
                if (_consoleHistoryIdx < 0) _consoleHistoryIdx = _consoleHistory.Count;
                if (_consoleHistoryIdx > 0) _consoleHistoryIdx--;
                _consoleInput = _consoleHistory[_consoleHistoryIdx];
            }
            ImGui.SameLine();
            if (ImGui.ArrowButton("##hist_down", ImGuiDir.Down))
            {
                if (_consoleHistoryIdx >= 0 && _consoleHistoryIdx < _consoleHistory.Count - 1)
                {
                    _consoleHistoryIdx++;
                    _consoleInput = _consoleHistory[_consoleHistoryIdx];
                }
                else
                {
                    _consoleHistoryIdx = -1;
                    _consoleInput = "";
                }
            }
        }

        // completions
        var completions = _consoleInput.Length > 0 ? GetCompletionMatches(_consoleInput) : _emptyCompletions;
        if (_completionIdx >= completions.Count) _completionIdx = completions.Count - 1;
        if (completions.Count == 0) _completionIdx = -1;

        // up/down navigate completions via ImGui key detection
        if (completions.Count > 0)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                _completionIdx = (_completionIdx + 1) % completions.Count;
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                _completionIdx = _completionIdx <= 0 ? completions.Count - 1 : _completionIdx - 1;

        }

        // popup
        if (completions.Count > 0)
        {
            var popupPos = new Vector2(inputRectMin.X, inputRectMax.Y + 2);
            ImGui.SetNextWindowPos(popupPos, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.95f);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 0), new Vector2(500, 250));

            const ImGuiWindowFlags popupFlags =
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoNav;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 1));
            if (ImGui.Begin("##completions_popup", popupFlags))
            {
                for (var i = 0; i < completions.Count; i++)
                {
                    var c = completions[i];
                    var isSelected = i == _completionIdx;

                    if (isSelected)
                    {
                        var cursorPos = ImGui.GetCursorScreenPos();
                        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
                        ImGui.GetWindowDrawList().AddRectFilled(
                            cursorPos,
                            new Vector2(cursorPos.X + ImGui.GetContentRegionAvail().X, cursorPos.Y + lineHeight),
                            ImGui.GetColorU32(new Vector4(0.3f, 0.4f, 0.6f, 0.6f)));
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, KindColor(c.kind));
                    ImGui.TextUnformatted(c.kindLabel);
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    if (ImGui.Selectable(c.label + "##c" + i, isSelected))
                    {
                        _consoleInput = c.insertText + " ";
                        _completionIdx = -1;
                        _consoleRefocus = true;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }

    struct CompletionEntry
    {
        public string label;
        public string insertText;
        public string kindLabel;
        public PortableCompletionKind kind;
    }

    static readonly List<CompletionEntry> _completionResults = new List<CompletionEntry>();
    static readonly List<CompletionEntry> _emptyCompletions = new List<CompletionEntry>();

    static Vector4 KindColor(PortableCompletionKind kind)
    {
        return kind switch
        {
            PortableCompletionKind.Variable => new Vector4(0.8f, 1f, 0.6f, 1f),
            PortableCompletionKind.Function => new Vector4(1f, 0.85f, 0.5f, 1f),
            PortableCompletionKind.Interface => new Vector4(0.6f, 0.8f, 1f, 1f), // commands
            PortableCompletionKind.Keyword => new Vector4(0.85f, 0.6f, 0.85f, 1f),
            PortableCompletionKind.Field => new Vector4(0.6f, 1f, 0.9f, 1f),
            PortableCompletionKind.Class => new Vector4(0.5f, 0.9f, 1f, 1f),
            PortableCompletionKind.Constant => new Vector4(1f, 0.7f, 0.5f, 1f),
            _ => new Vector4(0.7f, 0.7f, 0.7f, 1f),
        };
    }

    static string KindLabel(PortableCompletionKind kind)
    {
        return kind switch
        {
            PortableCompletionKind.Variable => "var",
            PortableCompletionKind.Function => "func",
            PortableCompletionKind.Interface => "cmd",
            PortableCompletionKind.Keyword => "key",
            PortableCompletionKind.Field => "field",
            PortableCompletionKind.Class => "type",
            PortableCompletionKind.Constant => "const",
            PortableCompletionKind.Reference => "label",
            _ => "",
        };
    }

    static List<CompletionEntry> GetCompletionMatches(string input)
    {
        _completionResults.Clear();

        var runtime = GameReloader.LatestRuntime;
        var commands = commandCollection;
        if (runtime == null || commands == null) return _completionResults;
        var mainProgram = runtime.Program;
        if (mainProgram == null) return _completionResults;

        try
        {
            // count lines in the main program so we can offset the console input
            // past all existing symbols (GetSymbolCompletions filters by position)
            var fullSource = runtime.SourceMap.fullSource;
            var programLineCount = 0;
            for (var ci = 0; ci < fullSource.Length; ci++)
                if (fullSource[ci] == '\n') programLineCount++;
            var offsetLine = programLineCount + 10;

            // prepend newlines so the console input's tokens are at a line number
            // past the main program — this makes both the Group walk and symbol
            // position filtering work correctly
            var padding = new string('\n', offsetLine);
            var paddedInput = padding + input;

            // lex the padded input (cheap — just newlines + one line of real code)
            var lexer = new Lexer();
            var lexResults = lexer.TokenizeWithErrors(paddedInput, commands);

            // cursor position is on the offset line, at the end of the input
            var cursorLine = offsetLine;
            var cursorChar = input.Length;

            // find the left token — same scan as CompletionHandler2
            Token leftToken = null;
            for (var i = lexResults.allTokens.Count - 1; i >= 0; i--)
            {
                var t = lexResults.allTokens[i];
                if (t.lineNumber < cursorLine)
                {
                    leftToken = t;
                    break;
                }
                if (t.lineNumber == cursorLine && t.charNumber <= cursorChar)
                {
                    leftToken = t;
                    break;
                }
            }
            if (leftToken == null)
                return _completionResults;

            // parse the padded input to get AST with correct token positions
            ProgramNode consoleProgram = null;
            if (lexResults.stream != null)
            {
                var parser = new Parser(lexResults.stream, commands);
                consoleProgram = parser.ParseProgram(new ParseOptions { ignoreChecks = true });
            }
            if (consoleProgram == null)
                return _completionResults;

            // fake cursor token at the end of the console input
            var fakeToken = new Token
            {
                lineNumber = cursorLine,
                charNumber = cursorChar
            };

            // walk the console AST for the Group — same as CompletionHandler2
            bool Visit(IAstVisitable v)
            {
                return v is ProgramNode
                    || (Token.IsLocationBeforeOrEqual(v.StartToken, fakeToken)
                        && Token.IsLocationBeforeOrEqual(fakeToken, v.EndToken));
            }
            var group = consoleProgram.Where(Visit);

            // get scope from the main program
            SymbolTable localScope;
            string funcName;
            if (mainProgram.scope.positionedVariables.entries.Count > 0)
            {
                var lastEntry = mainProgram.scope.positionedVariables.entries[
                    mainProgram.scope.positionedVariables.entries.Count - 1];
                localScope = lastEntry.value.Item1 ?? new SymbolTable();
                funcName = lastEntry.value.Item2;
            }
            else
            {
                localScope = new SymbolTable();
                funcName = null;
            }

            // fixup: the console parse is standalone so no types are resolved.
            // walk all nodes and resolve variable types from the main program's symbols.
            ResolveTypesFromMainProgram(consoleProgram, mainProgram, localScope);

            // build context — console AST for Group/LeftToken, main program for scope
            var context = new CompletionContext
            {
                FakeToken = fakeToken,
                LeftToken = leftToken,
                Program = mainProgram,
                Commands = commands,
                FunctionName = funcName,
                Group = group,
                ConstantTable = lexResults.constantTable ?? new Dictionary<string, string>(),
                LocalScope = localScope,
                IsMacro = false
            };

            var items = LSPUtil.GetCompletions(context);

            if (items.Count == 0)
            {
                // statement completions (void commands + keywords)
                items = LSPUtil.GetStatementCompletions(context, true);
                // expression completions (all commands, functions, variables — any return type)
                var exprItems = LSPUtil.GetExpressionCompletions(context);
                var existing = new HashSet<string>(items.Select(x => x.Label), StringComparer.OrdinalIgnoreCase);
                foreach (var item in exprItems)
                {
                    if (existing.Add(item.Label))
                        items.Add(item);
                }
            }

            // extract the last word for prefix filtering
            var lower = input.ToLowerInvariant();
            var filterStart = lower.Length;
            for (var ci = lower.Length - 1; ci >= 0; ci--)
            {
                var ch = lower[ci];
                if (ch == ' ' || ch == ',' || ch == '(' || ch == '.')
                {
                    filterStart = ci + 1;
                    break;
                }
                if (ci == 0) filterStart = 0;
            }
            var filter = filterStart < lower.Length ? lower.Substring(filterStart) : "";

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!seen.Add(item.Label)) continue;
                if (filter.Length > 0 && !item.Label.ToLowerInvariant().StartsWith(filter))
                    continue;

                var insert = item.InsertText ?? item.Label;
                insert = insert.Replace("($0)", "").Replace("$0", "");

                _completionResults.Add(new CompletionEntry
                {
                    label = item.Label + (string.IsNullOrEmpty(item.Detail) ? "" : "  " + item.Detail),
                    insertText = ReplaceLastWord(input, insert),
                    kindLabel = KindLabel(item.Kind),
                    kind = item.Kind
                });

                if (_completionResults.Count >= 20) break;
            }
        }
        catch (Exception ex)
        {
            _completionError = ex.Message;
        }

        return _completionResults;
    }

    /// Replaces the last word in the input with the completion text.
    /// Recursively walks the console AST and resolves variable types by looking them up
    /// in the main program's symbol tables. This bridges the gap between the standalone
    /// console parse (which has no type info) and the main program's fully-resolved scope.
    static void ResolveTypesFromMainProgram(ProgramNode consoleProgram, ProgramNode mainProgram, SymbolTable localScope)
    {
        var globals = mainProgram.scope.globalVariables;

        foreach (var node in consoleProgram.Where(_ => true))
        {
            // resolve VariableRefNode types
            if (node is VariableRefNode vrn && ((AstNode)vrn).ParsedType.unset)
            {
                if (localScope.TryGetValue(vrn.variableName, out var localSym))
                {
                    ((AstNode)vrn).ParsedType = localSym.typeInfo;
                    vrn.DeclaredFromSymbol = localSym;
                }
                else if (globals.TryGetValue(vrn.variableName, out var globalSym))
                {
                    ((AstNode)vrn).ParsedType = globalSym.typeInfo;
                    vrn.DeclaredFromSymbol = globalSym;
                }
            }

            // resolve ArrayIndexReference (function calls look like this)
            if (node is ArrayIndexReference air && ((AstNode)air).ParsedType.unset)
            {
                if (localScope.TryGetValue(air.variableName, out var localSym))
                {
                    ((AstNode)air).ParsedType = localSym.typeInfo;
                    air.DeclaredFromSymbol = localSym;
                }
                else if (globals.TryGetValue(air.variableName, out var globalSym))
                {
                    ((AstNode)air).ParsedType = globalSym.typeInfo;
                    air.DeclaredFromSymbol = globalSym;
                }
                else if (mainProgram.scope.functionSymbolTable.TryGetValue(air.variableName, out var funcSym))
                {
                    ((AstNode)air).ParsedType = funcSym.typeInfo;
                    air.DeclaredFromSymbol = funcSym;
                }
            }
        }
    }

    static string ReplaceLastWord(string input, string replacement)
    {
        for (var i = input.Length - 1; i >= 0; i--)
        {
            var ch = input[i];
            if (ch == ' ' || ch == ',' || ch == '(' || ch == '.')
                return input.Substring(0, i + 1) + replacement;
        }
        return replacement;
    }

    static void ConsoleExec(string input)
    {
        // add to history
        if (_consoleHistory.Count == 0 || _consoleHistory[_consoleHistory.Count - 1] != input)
            _consoleHistory.Add(input);
        _consoleHistoryIdx = -1;

        // echo input
        _consoleLog.Add(new ConsoleEntry { text = "> " + input, color = new Vector4(0.7f, 0.8f, 1f, 1f) });

        if (debugSession == null)
        {
            _consoleLog.Add(new ConsoleEntry { text = "error: no debug session active", color = new Vector4(1f, 0.3f, 0.3f, 1f) });
            _consoleScrollToBottom = true;
            return;
        }

        try
        {
            // try ReplExec first (handles statements and expressions)
            var result = debugSession.ReplExec(0, input);

            // if ReplExec failed, try Eval directly (better for bare expressions/variables)
            if (result.id < 0)
            {
                try
                {
                    var evalResult = debugSession.Eval(0, input);
                    if (evalResult != null && evalResult.id >= 0)
                        result = evalResult;
                }
                catch
                {
                    // Eval also failed, keep the original error
                }
            }

            if (result.id < 0)
            {
                _consoleLog.Add(new ConsoleEntry { text = result.value, color = new Vector4(1f, 0.4f, 0.4f, 1f) });
            }
            else if (result.scope != null && result.scope.variables.Count > 0)
            {
                // complex type — store the full result for tree rendering
                var typeHint = string.IsNullOrEmpty(result.type) ? "" : " (" + result.type + ")";
                var label = (string.IsNullOrEmpty(result.value) ? result.type : result.value + typeHint);
                _consoleLog.Add(new ConsoleEntry
                {
                    text = label,
                    color = new Vector4(0.5f, 1f, 0.5f, 1f),
                    result = result
                });
            }
            else if (!string.IsNullOrEmpty(result.value))
            {
                var typeHint = string.IsNullOrEmpty(result.type) ? "" : " (" + result.type + ")";
                _consoleLog.Add(new ConsoleEntry { text = result.value + typeHint, color = new Vector4(0.5f, 1f, 0.5f, 1f) });
            }
            else
            {
                _consoleLog.Add(new ConsoleEntry { text = "OK", color = new Vector4(0.5f, 0.5f, 0.5f, 1f) });
            }
        }
        catch (Exception ex)
        {
            _consoleLog.Add(new ConsoleEntry { text = "exception: " + ex.Message, color = new Vector4(1f, 0.3f, 0.3f, 1f) });
        }

        _consoleScrollToBottom = true;
    }

    /// Renders a DebugEvalResult as a tree node with expandable children.
    static void RenderConsoleResult(DebugEvalResult result, string label, int uniqueIdx)
    {
        if (ImGui.TreeNode(label + "##res" + uniqueIdx))
        {
            if (result.scope != null)
            {
                RenderDebugScope(result.scope);
            }
            ImGui.TreePop();
        }
    }

    /// Recursively renders a DebugScope's variables as tree nodes.
    static void RenderDebugScope(DebugScope scope)
    {
        foreach (var v in scope.variables)
        {
            var hasChildren = v.fieldCount > 0 || v.elementCount > 0;
            if (hasChildren)
            {
                // try to expand via the debug session
                DebugScope childScope = null;
                try
                {
                    childScope = debugSession?.variableDb?.Expand(v.id);
                }
                catch
                {
                    // expansion may fail for stale IDs
                }

                if (childScope != null && childScope.variables.Count > 0)
                {
                    var nodeLabel = v.name + ": " + (string.IsNullOrEmpty(v.value) ? v.type : v.value) + " (" + v.type + ")";
                    if (ImGui.TreeNode(nodeLabel + "##v" + v.id))
                    {
                        RenderDebugScope(childScope);
                        ImGui.TreePop();
                    }
                }
                else
                {
                    ImGui.TextDisabled(v.name + ": " + v.value + " (" + v.type + ") [" + v.elementCount + " elements]");
                }
            }
            else
            {
                var typeHint = string.IsNullOrEmpty(v.type) ? "" : " (" + v.type + ")";
                ImGui.Text(v.name + ": " + v.value + typeHint);
            }
        }
    }

    // ── memory tracking ─────────────────────────────────────

    static long _baselineBytes = -1;

    /// Call once early (after static arrays are allocated but before content loads) to snapshot the baseline.
    public static void CaptureMemoryBaseline()
    {
        if (_baselineBytes != -1) return; // only do this once. 
        GC.Collect();
        _baselineBytes = GC.GetTotalMemory(true);
    }

    static void RenderMemoryChart()
    {
        var totalBytes = GC.GetTotalMemory(false);
        var totalMb = (float)(totalBytes / (1024.0 * 1024.0));

        // if we have a baseline, show dynamic = total - baseline
        // if not, fall back to just showing total
        var baselineMb = _baselineBytes > 0 ? (float)(_baselineBytes / (1024.0 * 1024.0)) : 0f;
        var dynamicMb = _baselineBytes > 0 ? Math.Max(0, totalMb - baselineMb) : totalMb;

        _memoryHistory[_memoryHistoryIdx % MEMORY_HISTORY] = dynamicMb;
        _memoryHistoryIdx++;
        if (dynamicMb > _memoryPeakMb) _memoryPeakMb = dynamicMb;

        ImGui.Text($"Dynamic: {dynamicMb:F1} MB  (peak {_memoryPeakMb:F1} MB)");
        ImGui.TextDisabled(_baselineBytes > 0
            ? $"Total heap: {totalMb:F0} MB  (baseline {baselineMb:F0} MB)"
            : $"Total heap: {totalMb:F0} MB");
        ImGui.PlotLines("##mem", ref _memoryHistory[0], MEMORY_HISTORY, _memoryHistoryIdx % MEMORY_HISTORY,
            null, 0, Math.Max(_memoryPeakMb * 1.2f, 10f), new Vector2(-1, 40));
    }

    // ── color themes ────────────────────────────────────────

    static void ApplyNordTheme()
    {
        ImGui.StyleColorsDark();
        var c = ImGui.GetStyle().Colors;
        c[(int)ImGuiCol.WindowBg]        = new Vector4(0.18f, 0.20f, 0.25f, 0.85f);
        c[(int)ImGuiCol.TitleBgActive]   = new Vector4(0.23f, 0.26f, 0.32f, 1f);
        c[(int)ImGuiCol.FrameBg]         = new Vector4(0.23f, 0.26f, 0.32f, 1f);
        c[(int)ImGuiCol.FrameBgHovered]  = new Vector4(0.30f, 0.34f, 0.42f, 1f);
        c[(int)ImGuiCol.FrameBgActive]   = new Vector4(0.36f, 0.51f, 0.67f, 1f);
        c[(int)ImGuiCol.SliderGrab]      = new Vector4(0.36f, 0.51f, 0.67f, 1f);
        c[(int)ImGuiCol.SliderGrabActive]= new Vector4(0.53f, 0.63f, 0.75f, 1f);
        c[(int)ImGuiCol.CheckMark]       = new Vector4(0.53f, 0.63f, 0.75f, 1f);
        c[(int)ImGuiCol.Button]          = new Vector4(0.36f, 0.51f, 0.67f, 1f);
        c[(int)ImGuiCol.ButtonHovered]   = new Vector4(0.53f, 0.63f, 0.75f, 1f);
        c[(int)ImGuiCol.Header]          = new Vector4(0.36f, 0.51f, 0.67f, 0.6f);
        c[(int)ImGuiCol.HeaderHovered]   = new Vector4(0.36f, 0.51f, 0.67f, 0.8f);
        c[(int)ImGuiCol.Tab]             = new Vector4(0.23f, 0.26f, 0.32f, 1f);
        c[(int)ImGuiCol.TabSelected]     = new Vector4(0.36f, 0.51f, 0.67f, 1f);
        c[(int)ImGuiCol.Text]            = new Vector4(0.85f, 0.87f, 0.91f, 1f);
        c[(int)ImGuiCol.TextDisabled]    = new Vector4(0.55f, 0.58f, 0.63f, 1f);
    }

    static void ApplyDraculaTheme()
    {
        ImGui.StyleColorsDark();
        var c = ImGui.GetStyle().Colors;
        c[(int)ImGuiCol.WindowBg]        = new Vector4(0.16f, 0.16f, 0.21f, 0.85f);
        c[(int)ImGuiCol.TitleBgActive]   = new Vector4(0.23f, 0.20f, 0.33f, 1f);
        c[(int)ImGuiCol.FrameBg]         = new Vector4(0.23f, 0.23f, 0.31f, 1f);
        c[(int)ImGuiCol.FrameBgHovered]  = new Vector4(0.30f, 0.28f, 0.40f, 1f);
        c[(int)ImGuiCol.FrameBgActive]   = new Vector4(0.50f, 0.38f, 0.74f, 1f);
        c[(int)ImGuiCol.SliderGrab]      = new Vector4(0.50f, 0.38f, 0.74f, 1f);
        c[(int)ImGuiCol.SliderGrabActive]= new Vector4(0.60f, 0.48f, 0.84f, 1f);
        c[(int)ImGuiCol.CheckMark]       = new Vector4(0.60f, 0.48f, 0.84f, 1f);
        c[(int)ImGuiCol.Button]          = new Vector4(0.39f, 0.30f, 0.60f, 1f);
        c[(int)ImGuiCol.ButtonHovered]   = new Vector4(0.50f, 0.38f, 0.74f, 1f);
        c[(int)ImGuiCol.Header]          = new Vector4(0.39f, 0.30f, 0.60f, 0.6f);
        c[(int)ImGuiCol.HeaderHovered]   = new Vector4(0.50f, 0.38f, 0.74f, 0.8f);
        c[(int)ImGuiCol.Tab]             = new Vector4(0.23f, 0.20f, 0.33f, 1f);
        c[(int)ImGuiCol.TabSelected]     = new Vector4(0.50f, 0.38f, 0.74f, 1f);
        c[(int)ImGuiCol.Text]            = new Vector4(0.95f, 0.95f, 0.96f, 1f);
        c[(int)ImGuiCol.TextDisabled]    = new Vector4(0.48f, 0.45f, 0.55f, 1f);
    }

    static void ApplySolarizedTheme()
    {
        ImGui.StyleColorsLight();
        var c = ImGui.GetStyle().Colors;
        c[(int)ImGuiCol.WindowBg]        = new Vector4(0.99f, 0.96f, 0.89f, 0.85f);
        c[(int)ImGuiCol.TitleBgActive]   = new Vector4(0.93f, 0.90f, 0.82f, 1f);
        c[(int)ImGuiCol.FrameBg]         = new Vector4(0.93f, 0.90f, 0.82f, 1f);
        c[(int)ImGuiCol.FrameBgHovered]  = new Vector4(0.88f, 0.85f, 0.76f, 1f);
        c[(int)ImGuiCol.FrameBgActive]   = new Vector4(0.15f, 0.45f, 0.55f, 0.6f);
        c[(int)ImGuiCol.SliderGrab]      = new Vector4(0.15f, 0.45f, 0.55f, 1f);
        c[(int)ImGuiCol.SliderGrabActive]= new Vector4(0.20f, 0.55f, 0.65f, 1f);
        c[(int)ImGuiCol.CheckMark]       = new Vector4(0.15f, 0.45f, 0.55f, 1f);
        c[(int)ImGuiCol.Button]          = new Vector4(0.15f, 0.45f, 0.55f, 1f);
        c[(int)ImGuiCol.ButtonHovered]   = new Vector4(0.20f, 0.55f, 0.65f, 1f);
        c[(int)ImGuiCol.Header]          = new Vector4(0.15f, 0.45f, 0.55f, 0.4f);
        c[(int)ImGuiCol.HeaderHovered]   = new Vector4(0.15f, 0.45f, 0.55f, 0.6f);
        c[(int)ImGuiCol.Tab]             = new Vector4(0.93f, 0.90f, 0.82f, 1f);
        c[(int)ImGuiCol.TabSelected]     = new Vector4(0.15f, 0.45f, 0.55f, 1f);
        c[(int)ImGuiCol.Text]            = new Vector4(0.24f, 0.28f, 0.30f, 1f);
        c[(int)ImGuiCol.TextDisabled]    = new Vector4(0.50f, 0.52f, 0.49f, 1f);
    }

    // ── style persistence ───────────────────────────────────

    struct SavedStyle
    {
        public float FontGlobalScale { get; set; }
        public float WindowRounding { get; set; }
        public float FrameRounding { get; set; }
        public float[][] Colors { get; set; }
    }

    static void SaveStyle()
    {
        try
        {
            var style = ImGui.GetStyle();
            var io = ImGui.GetIO();
            var colorCount = (int)ImGuiCol.COUNT;
            var colors = new float[colorCount][];
            for (var i = 0; i < colorCount; i++)
            {
                var c = style.Colors[i];
                colors[i] = new[] { c.X, c.Y, c.Z, c.W };
            }

            var saved = new SavedStyle
            {
                FontGlobalScale = io.FontGlobalScale,
                WindowRounding = style.WindowRounding,
                FrameRounding = style.FrameRounding,
                Colors = colors
            };

            var json = JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(STYLE_FILE, json);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Failed to save ImGui style: " + e.Message);
        }
    }

    static void LoadStyle()
    {
        try
        {
            if (!File.Exists(STYLE_FILE)) return;
            var json = File.ReadAllText(STYLE_FILE);
            var saved = JsonSerializer.Deserialize<SavedStyle>(json);

            var style = ImGui.GetStyle();
            var io = ImGui.GetIO();
            io.FontGlobalScale = saved.FontGlobalScale;
            style.WindowRounding = saved.WindowRounding;
            style.FrameRounding = saved.FrameRounding;

            if (saved.Colors != null)
            {
                var colorCount = Math.Min(saved.Colors.Length, (int)ImGuiCol.COUNT);
                for (var i = 0; i < colorCount; i++)
                {
                    var c = saved.Colors[i];
                    if (c is { Length: 4 })
                        style.Colors[i] = new Vector4(c[0], c[1], c[2], c[3]);
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Failed to load ImGui style: " + e.Message);
        }
    }

}
#endif
