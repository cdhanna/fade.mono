using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("begin debug window")]
    public static void Debug_BeginWindow([FromVm] VirtualMachine vm, string name)
    {
        // var style = ImGui.GetStyle();
        //
        // style.WindowRounding = 8f;
        // style.FrameRounding = 4f;
        // style.ScrollbarRounding = 6f;
        // style.FramePadding = new Vector2(8, 4);
        // style.ItemSpacing = new Vector2(10, 6);
        // style.WindowBorderSize = 1f;
        //
        //
        // var colors = ImGui.GetStyle().Colors;
        //
        // // colors[(int)ImGuiCol.WindowBg] = new Vector4(.3f, 0.5f, 0.8f, 1f);
        // colors[(int)ImGuiCol.TitleBg] = new Vector4(.9f, 0.5f, 0.8f, 1f);
        // colors[(int)ImGuiCol.Button] = new Vector4(0.9f, 0.5f, 0.8f, 1f);
        // colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.3f, 0.6f, 0.9f, 1f);
        // colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.1f, 0.4f, 0.7f, 1f);
        // ImGui.Begin(name);
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, label = name, type = DebugControlType.WINDOW_START
        });
    }
    [FadeBasicCommand("end debug window")]
    public static void Debug_EndWindow([FromVm]VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.WINDOW_END
        });
    }

    [FadeBasicCommand("debug button")]
    public static int Debug_Button([FromVm] VirtualMachine vm, string name)
    {
        var command = new DebugUICommand
        {
            label = name,
            type = DebugControlType.BUTTON,
            vmInstructionIndex = vm.instructionIndex
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;

    }

    [FadeBasicCommand("debug label")]
    public static void Debug_Label([FromVm] VirtualMachine vm, string label, string value)
    {
        var command = new DebugUICommand
        {
            label = label,
            argString = value,
            type = DebugControlType.LABEL,
            vmInstructionIndex = vm.instructionIndex
        };
        DebugUISystem.Push(command);
        // ImGui.LabelText(label, value);
    }
    
    [FadeBasicCommand("debug toggle")]
    public static void Debug_Toggle(string label, ref int value)
    {
        // var b = value > 0;
        // ImGui.Checkbox(label, ref b);
        // value = b ? 1 : 0;
    }
    
    [FadeBasicCommand("debug same line")]
    public static void Debug_SameLine()
    {
        // ImGui.SameLine();
    }
    
    [FadeBasicCommand("debug textbox")]
    public static int Debug_TextBox([FromVm] VirtualMachine vm, string label, ref string value, string placeholder="", int maxLength=512)
    {
        var ctrl = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TEXTFIELD,
            vmInstructionIndex = vm.instructionIndex,
            argString = value,
        };
        DebugUISystem.Push(ctrl);
        DebugUISystem.Push(new DebugUICommand
        {
            type = DebugControlType.ARG_STRING,
            argString = placeholder,
        });
        DebugUISystem.Push(new DebugUICommand
        {
            type = DebugControlType.ARG_INT,
            argInt = maxLength,
        });
        
        if (DebugUISystem.TryGetPreviousString(ctrl, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(ctrl) ? 1 : 0;
    }
    
    [FadeBasicCommand("debug int slider")]
    public static int Debug_Slider([FromVm] VirtualMachine vm, string name, ref int value, int min=0, int max=100)
    {
        var command = new DebugUICommand
        {
            label = name,
            type = DebugControlType.INT_SLIDER,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        
        DebugUISystem.Push(new DebugUICommand
        {
            type = DebugControlType.ARG_INT,
            argInt = min,
        });
        DebugUISystem.Push(new DebugUICommand
        {
            type = DebugControlType.ARG_INT,
            argInt = max,
        });
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }
    
    [FadeBasicCommand("debug float slider")]
    public static int Debug_Slider([FromVm] VirtualMachine vm, string label, ref float value, float min=0, float max=100)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.FLOAT_SLIDER,
            vmInstructionIndex = vm.instructionIndex,
            argFloat = value,
        };
        DebugUISystem.Push(command);
        
        DebugUISystem.Push(new DebugUICommand
        {
            type = DebugControlType.ARG_FLOAT,
            argFloat = min,
        });
        DebugUISystem.Push(new DebugUICommand
        {
            type = DebugControlType.ARG_FLOAT,
            argFloat = max,
        });
        if (DebugUISystem.TryGetPreviousFloat(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug sprite")]
    public static int Debug_Sprite([FromVm] VirtualMachine vm, int spriteId)
    {
        var command = new DebugUICommand
        {
            label = "sprite",
            type = DebugControlType.COMPONENT_SPRITE,
            vmInstructionIndex = vm.instructionIndex,
            argInt = spriteId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug effect")]
    public static int Debug_Effect([FromVm] VirtualMachine vm, int effectId)
    {
        var command = new DebugUICommand
        {
            label = "effect",
            type = DebugControlType.COMPONENT_EFFECT,
            vmInstructionIndex = vm.instructionIndex,
            argInt = effectId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }
}