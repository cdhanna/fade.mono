using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using ImGuiNET;
using Microsoft.Xna.Framework;
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
    
    // components
    COMPONENT_SPRITE,
    COMPONENT_EFFECT,
    
    // args
    ARG_INT,
    ARG_STRING,
    ARG_FLOAT
}

public static class DebugUISystem
{
    public static ImGuiRenderer renderer;
    public static Queue<DebugUICommand> controls = new Queue<DebugUICommand>();
    public static Dictionary<int, bool> controlIdToBool = new Dictionary<int, bool>();
    public static Dictionary<int, int> controlIdToInt = new Dictionary<int, int>();
    public static Dictionary<int, float> controlIdToFloat = new Dictionary<int, float>();
    public static Dictionary<int, string> controlIdToString = new Dictionary<int, string>();

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
    }
    
    public static void Render()
    {
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
                case DebugControlType.WINDOW_END:
                    ImGui.End();
                    break;
                
                case DebugControlType.COMPONENT_EFFECT:
                    RenderEffect(ctrl);
                    break;
                case DebugControlType.COMPONENT_SPRITE:
                    RenderSprite(ctrl);
                    break;
            }
            
        }
        
        renderer.AfterLayout();
        
    }

    static void RenderSprite(DebugUICommand ctrl)
    {
        var spriteId = ctrl.argInt;
        SpriteSystem.GetSpriteIndex(spriteId, out var index, out var sprite);
        if (ImGui.TreeNodeEx("sprite(" + spriteId + ")", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var changed = false;
            changed |= Color("color", ref sprite.color);
            changed |= Vec2Input("position", ref sprite.position);
            changed |= ImGui.Checkbox("hidden", ref sprite.hidden);
                    
            if (changed)
            {
                SpriteSystem.sprites[index] = sprite;
                controlIdToBool[ctrl.ControlId] = true;
            }
            ImGui.TreePop();
        }
    }
    static void RenderEffect(DebugUICommand ctrl)
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
        
        if (ImGui.TreeNodeEx("effect(" + effectId + ") " + name, ImGuiTreeNodeFlags.DefaultOpen))
        {
            var changed = false;
            var updatedAgo = DateTimeOffset.Now - fx.watchedEffect.UpdatedAt;
            ImGui.TextDisabled("last loaded: " + updatedAgo.ToString());
            foreach (var parameter in fx.effect.Parameters)
            {
                switch (parameter.ParameterType)
                {
                    case EffectParameterType.Texture2D:
                        ImGui.TextDisabled("<no preview>");
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
            ImGui.TreePop();
        }
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

}