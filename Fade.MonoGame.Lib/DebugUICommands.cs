using System.Numerics;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using ImGuiNET;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("begin debug window")]
    public static void Debug_BeginWindow(string name)
    {
        var style = ImGui.GetStyle();

        style.WindowRounding = 8f;
        style.FrameRounding = 4f;
        style.ScrollbarRounding = 6f;
        style.FramePadding = new Vector2(8, 4);
        style.ItemSpacing = new Vector2(10, 6);
        style.WindowBorderSize = 1f;
       
        
        // var colors = ImGui.GetStyle().Colors;
        //
        // // colors[(int)ImGuiCol.WindowBg] = new Vector4(.3f, 0.5f, 0.8f, 1f);
        // colors[(int)ImGuiCol.TitleBg] = new Vector4(.9f, 0.5f, 0.8f, 1f);
        // colors[(int)ImGuiCol.Button] = new Vector4(0.9f, 0.5f, 0.8f, 1f);
        // colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.3f, 0.6f, 0.9f, 1f);
        // colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.1f, 0.4f, 0.7f, 1f);
        ImGui.Begin(name);
    }
    [FadeBasicCommand("end debug window")]
    public static void Debug_EndWindow()
    {
        ImGui.End();
    }

    [FadeBasicCommand("debug button")]
    public static int Debug_Button(string name)
    {
        return ImGui.Button(name) ? 1 : 0;
    }

    [FadeBasicCommand("debug label")]
    public static void Debug_Label(string label, string value)
    {
        ImGui.LabelText(label, value);
    }
    
    [FadeBasicCommand("debug toggle")]
    public static void Debug_Toggle(string label, ref int value)
    {
        var b = value > 0;
        ImGui.Checkbox(label, ref b);
        value = b ? 1 : 0;
    }
    
    [FadeBasicCommand("debug int slider")]
    public static void Debug_Slider(string label, ref int value, int min=0, int max=100)
    {
        ImGui.SliderInt(label, ref value, min, max);
    }
    
    [FadeBasicCommand("debug float slider")]
    public static void Debug_Slider(string label, ref float value, float min=0, float max=100)
    {
        ImGui.SliderFloat(label, ref value, min, max);
    }
    /*
     *
     * 
     * debug window start
     * 
     * push debug window 1
     * clicked = debug button ("hello")
     * if clicked
     *  count = count + 1
     * endif
     * debug label "clicked" + str$(count)
     * 
     * pop debug window `optional really; all windows popped on debug window start
     * 
     * debug window 1
     * debug button 1, 1, "hello" `window, title, 
     */
}