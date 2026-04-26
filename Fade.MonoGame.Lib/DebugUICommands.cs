using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    // ── window ──────────────────────────────────────────────

    [FadeBasicCommand("begin debug window")]
    public static void Debug_BeginWindow([FromVm] VirtualMachine vm, string name)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, label = name, type = DebugControlType.WINDOW_START
        });
    }

    [FadeBasicCommand("end debug window")]
    public static void Debug_EndWindow([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.WINDOW_END
        });
    }

    // ── layout ──────────────────────────────────────────────

    [FadeBasicCommand("debug same line")]
    public static void Debug_SameLine([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.SAME_LINE
        });
    }

    [FadeBasicCommand("debug separator")]
    public static void Debug_Separator([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.SEPARATOR
        });
    }

    [FadeBasicCommand("begin debug tree")]
    public static int Debug_BeginTree([FromVm] VirtualMachine vm, string label)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TREE_START,
            vmInstructionIndex = vm.instructionIndex,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("end debug tree")]
    public static void Debug_EndTree([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.TREE_END
        });
    }

    [FadeBasicCommand("begin debug tab bar")]
    public static int Debug_BeginTabBar([FromVm] VirtualMachine vm, string id)
    {
        var command = new DebugUICommand
        {
            label = id,
            type = DebugControlType.TAB_BAR_START,
            vmInstructionIndex = vm.instructionIndex,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("end debug tab bar")]
    public static void Debug_EndTabBar([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.TAB_BAR_END
        });
    }

    [FadeBasicCommand("begin debug tab")]
    public static int Debug_BeginTab([FromVm] VirtualMachine vm, string label)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TAB_ITEM_START,
            vmInstructionIndex = vm.instructionIndex,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("end debug tab")]
    public static void Debug_EndTab([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.TAB_ITEM_END
        });
    }

    // ── display ─────────────────────────────────────────────

    [FadeBasicCommand("debug label")]
    public static void Debug_Label([FromVm] VirtualMachine vm, string label, string value)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = label,
            argString = value,
            type = DebugControlType.LABEL,
            vmInstructionIndex = vm.instructionIndex
        });
    }

    [FadeBasicCommand("debug text")]
    public static void Debug_Text([FromVm] VirtualMachine vm, string text)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            argString = text,
            type = DebugControlType.TEXT,
            vmInstructionIndex = vm.instructionIndex
        });
    }

    // ── interactive controls ────────────────────────────────

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

    [FadeBasicCommand("debug toggle")]
    public static int Debug_Toggle([FromVm] VirtualMachine vm, string label, ref int value)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.CHECKBOX,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug textbox")]
    public static int Debug_TextBox([FromVm] VirtualMachine vm, string label, ref string value, string placeholder = "", int maxLength = 512)
    {
        var ctrl = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TEXTFIELD,
            vmInstructionIndex = vm.instructionIndex,
            argString = value,
        };
        DebugUISystem.Push(ctrl);
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_STRING, argString = placeholder });
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_INT, argInt = maxLength });

        if (DebugUISystem.TryGetPreviousString(ctrl, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(ctrl) ? 1 : 0;
    }

    [FadeBasicCommand("debug int slider")]
    public static int Debug_Slider([FromVm] VirtualMachine vm, string name, ref int value, int min = 0, int max = 100)
    {
        var command = new DebugUICommand
        {
            label = name,
            type = DebugControlType.INT_SLIDER,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_INT, argInt = min });
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_INT, argInt = max });
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug float slider")]
    public static int Debug_Slider([FromVm] VirtualMachine vm, string label, ref float value, float min = 0, float max = 100)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.FLOAT_SLIDER,
            vmInstructionIndex = vm.instructionIndex,
            argFloat = value,
        };
        DebugUISystem.Push(command);
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_FLOAT, argFloat = min });
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_FLOAT, argFloat = max });
        if (DebugUISystem.TryGetPreviousFloat(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug drag int")]
    public static int Debug_DragInt([FromVm] VirtualMachine vm, string label, ref int value)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.DRAG_INT,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug drag float")]
    public static int Debug_DragFloat([FromVm] VirtualMachine vm, string label, ref float value)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.DRAG_FLOAT,
            vmInstructionIndex = vm.instructionIndex,
            argFloat = value,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousFloat(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug color picker")]
    public static int Debug_ColorPicker([FromVm] VirtualMachine vm, string label, ref int colorCode)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.COLOR_PICKER,
            vmInstructionIndex = vm.instructionIndex,
            argInt = colorCode,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            colorCode = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    // ── auto inspector ───────────────────────────────────────

    [FadeBasicCommand("enable debug inspector")]
    public static void Debug_EnableInspector()
    {
        DebugUISystem.autoInspectorEnabled = true;
    }

    [FadeBasicCommand("disable debug inspector")]
    public static void Debug_DisableInspector()
    {
        DebugUISystem.autoInspectorEnabled = false;
    }

    // ── resource browsers ────────────────────────────────────

    [FadeBasicCommand("debug browse sprites")]
    public static void Debug_BrowseSprites([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_sprites", type = DebugControlType.BROWSER_SPRITE,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse effects")]
    public static void Debug_BrowseEffects([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_effects", type = DebugControlType.BROWSER_EFFECT,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse transforms")]
    public static void Debug_BrowseTransforms([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_transforms", type = DebugControlType.BROWSER_TRANSFORM,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse tweens")]
    public static void Debug_BrowseTweens([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_tweens", type = DebugControlType.BROWSER_TWEEN,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse colliders")]
    public static void Debug_BrowseColliders([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_colliders", type = DebugControlType.BROWSER_COLLIDER,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse texts")]
    public static void Debug_BrowseTexts([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_texts", type = DebugControlType.BROWSER_TEXT,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse sfx")]
    public static void Debug_BrowseSfx([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_sfx", type = DebugControlType.BROWSER_SFX,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse textures")]
    public static void Debug_BrowseTextures([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_textures", type = DebugControlType.BROWSER_TEXTURE,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug browse render outputs")]
    public static void Debug_BrowseRenderOutputs([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_render_outputs", type = DebugControlType.BROWSER_RENDER_OUTPUT,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    // ── composite ───────────────────────────────────────────

    [FadeBasicCommand("debug console")]
    public static void Debug_Console([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "console", type = DebugControlType.CONSOLE,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug inspector")]
    public static void Debug_Inspector([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "inspector", type = DebugControlType.INSPECTOR,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    [FadeBasicCommand("debug metadata")]
    public static void Debug_Metadata([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "metadata", type = DebugControlType.COMPONENT_METADATA,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    // ── component inspectors ────────────────────────────────

    [FadeBasicCommand("debug sprite")]
    public static int Debug_Sprite([FromVm] VirtualMachine vm, int spriteId)
    {
        var command = new DebugUICommand
        {
            label = "sprite", type = DebugControlType.COMPONENT_SPRITE,
            vmInstructionIndex = vm.instructionIndex, argInt = spriteId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug effect")]
    public static int Debug_Effect([FromVm] VirtualMachine vm, int effectId)
    {
        var command = new DebugUICommand
        {
            label = "effect", type = DebugControlType.COMPONENT_EFFECT,
            vmInstructionIndex = vm.instructionIndex, argInt = effectId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug transform")]
    public static int Debug_Transform([FromVm] VirtualMachine vm, int transformId)
    {
        var command = new DebugUICommand
        {
            label = "transform", type = DebugControlType.COMPONENT_TRANSFORM,
            vmInstructionIndex = vm.instructionIndex, argInt = transformId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug tween")]
    public static int Debug_Tween([FromVm] VirtualMachine vm, int tweenId)
    {
        var command = new DebugUICommand
        {
            label = "tween", type = DebugControlType.COMPONENT_TWEEN,
            vmInstructionIndex = vm.instructionIndex, argInt = tweenId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug collider")]
    public static int Debug_Collider([FromVm] VirtualMachine vm, int colliderId)
    {
        var command = new DebugUICommand
        {
            label = "collider", type = DebugControlType.COMPONENT_COLLIDER,
            vmInstructionIndex = vm.instructionIndex, argInt = colliderId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug text sprite")]
    public static int Debug_TextSprite([FromVm] VirtualMachine vm, int textId)
    {
        var command = new DebugUICommand
        {
            label = "text", type = DebugControlType.COMPONENT_TEXT,
            vmInstructionIndex = vm.instructionIndex, argInt = textId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug sfx")]
    public static int Debug_Sfx([FromVm] VirtualMachine vm, int sfxId)
    {
        var command = new DebugUICommand
        {
            label = "sfx", type = DebugControlType.COMPONENT_SFX,
            vmInstructionIndex = vm.instructionIndex, argInt = sfxId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug texture")]
    public static int Debug_Texture([FromVm] VirtualMachine vm, int textureId)
    {
        var command = new DebugUICommand
        {
            label = "texture", type = DebugControlType.COMPONENT_TEXTURE,
            vmInstructionIndex = vm.instructionIndex, argInt = textureId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    [FadeBasicCommand("debug render output")]
    public static int Debug_RenderOutput([FromVm] VirtualMachine vm, int outputId)
    {
        var command = new DebugUICommand
        {
            label = "render_output", type = DebugControlType.COMPONENT_RENDER_OUTPUT,
            vmInstructionIndex = vm.instructionIndex, argInt = outputId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }
}
