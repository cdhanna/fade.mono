using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Core;

public class DebugUIWindow
{
    public string name;
    public List<DebugControl> buttons = new List<DebugControl>();
}

public class DebugControl
{
    public string name;
    public DebugControlType type;
}

public enum DebugControlType
{
    BUTTON, 
    TEXTFIELD,
    SAME_LINE
}

public static class DebugUISystem
{
    public static ImGuiRenderer renderer;
    public static Stack<DebugUIWindow> stack = new Stack<DebugUIWindow>();

    public static void Push()
    {
        
    }

    public static void StartDebug()
    {
        renderer.BeforeLayout(GameSystem.latestTime);
    }

    public static void EndDebug()
    {
        renderer.AfterLayout();
    }
    
    public static int x;
    public static void RenderDebugWindows(ImGuiRenderer imguiRenderer)
    {
        // Draw debug UI
        imguiRenderer.BeforeLayout(GameSystem.latestTime);

        ImGui.Begin("test");
        if (ImGui.Button("clickme"))
        {
            x++;
        }
        ImGui.LabelText("x", x.ToString());
        ImGui.End();
        foreach (var win in stack)
        {
            ImGui.Begin(win.name);
            
            // render controls...
            foreach (var control in win.buttons)
            {
                switch (control.type)
                {
                    /*
                     * debug window
                     * debug button "click"
                     * 
                     */
                    
                    case DebugControlType.BUTTON:
                        var wasClicked = ImGui.Button(control.name);
                        // TODO: hmm... in order to associate the wasClicked back to the user's fade code, 
                        //  we'd either need to state-track, or make a query system...
                        //  the core issue is that the UI does not actually run when the user's fade code is declaring it. 
                        break;
                }
            }
            
            ImGui.End();
        }
        
        imguiRenderer.AfterLayout();
    }
}