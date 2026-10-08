#if !BROWSER
using System;
using System.Runtime.InteropServices;

namespace Fade.MonoGame.Core;

/// <summary>
/// Keeps the game running and drawing while the player is dragging the edge of the window.
///
/// On Windows, dragging a window edge puts the thread into a modal loop inside the operating
/// system: the call that pumps window messages does not return until the mouse button is let
/// go. MonoGame pumps messages between frames, so for the length of the drag there ARE no
/// frames, and the window shows its last picture stretched to whatever size it is dragged to.
///
/// SDL is still told that the window needs painting at every size it passes through, and
/// reports each one to an "event watch" straight away, from inside that modal loop. This
/// registers one, and runs a frame from it. That is safe only because of where MonoGame pumps messages: between calls
/// to Tick, never inside one. A watch that fires during the pump is therefore never inside a
/// frame already.
///
/// Windows only. The other platforms do not block during a resize, and SDL makes no promise
/// there about which thread a watch is called on.
/// </summary>
internal static class LiveResize
{
    private const uint SDL_WINDOWEVENT = 0x200;
    private const byte SDL_WINDOWEVENT_EXPOSED = 3;
    private const byte SDL_WINDOWEVENT_SIZE_CHANGED = 6;
    private const uint GUI_INMOVESIZE = 0x2;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SdlEventFilter(IntPtr userdata, IntPtr sdlEvent);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_AddEventWatch(SdlEventFilter filter, IntPtr userdata);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_DelEventWatch(SdlEventFilter filter, IntPtr userdata);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public Rect rcCaret;
    }

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo info);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    // Held in a field because SDL keeps the function pointer. A delegate that is only passed
    // to native code looks unused to the garbage collector, and the watch would start calling
    // into freed memory some unpredictable time later.
    private static SdlEventFilter _filter;

    private static Game1 _game;
    private static int _gameThreadId;
    private static bool _ticking;
    private static bool _reportedFailure;

    public static void Install(Game1 game)
    {
        if (!OperatingSystem.IsWindows()) return;

        // One watch for the life of the process. A second Game1 (a reload builds one) just
        // becomes the game that the existing watch ticks.
        _game = game;
        _gameThreadId = Environment.CurrentManagedThreadId;
        if (_filter != null) return;

        try
        {
            _filter = OnSdlEvent;
            SDL_AddEventWatch(_filter, IntPtr.Zero);
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
        {
            // A backend that is not built on SDL2 (the Vulkan one). The window simply goes
            // back to freezing during a drag, which is what it did before this existed.
            _filter = null;
        }
    }

    private static int OnSdlEvent(IntPtr userdata, IntPtr sdlEvent)
    {
        // The return value is ignored for a watch, but an exception must never get back into
        // SDL: there is no managed frame above this one to catch it.
        try
        {
            if (_ticking || _game == null) return 1;

            // SDL_WindowEvent: type, timestamp, windowID (4 bytes each), then the kind of
            // window event (1 byte, padded to 4), then its two numbers.
            if ((uint)Marshal.ReadInt32(sdlEvent, 0) != SDL_WINDOWEVENT) return 1;

            // While an edge is being dragged SDL does not report the new sizes at all -- those
            // arrive in a burst once the drag is over. What it does report is that the window
            // needs painting again, once for every size it passes through.
            var kind = Marshal.ReadByte(sdlEvent, 12);
            if (kind != SDL_WINDOWEVENT_EXPOSED && kind != SDL_WINDOWEVENT_SIZE_CHANGED) return 1;

            if (Environment.CurrentManagedThreadId != _gameThreadId) return 1;

            // Only during a drag. A size change the PROGRAM asked for (`set screen size`) is
            // reported from inside the frame that asked for it, and running a frame within a
            // frame is exactly what this must not do. MonoGame handles that one normally.
            if (!TryGetDraggedWindowSize(out var width, out var height)) return 1;

            _ticking = true;
            try
            {
                _game.TickAtWindowSize(width, height);
            }
            finally
            {
                _ticking = false;
            }
        }
        catch (Exception ex)
        {
            if (!_reportedFailure)
            {
                _reportedFailure = true;
                Console.Error.WriteLine($"[fade] drawing during a window resize failed, and will not be tried again: {ex}");
            }

            // Stop ticking, rather than failing the same way on every pixel of every drag.
            _game = null;
        }

        return 1;
    }

    /// <summary>
    /// True while this thread is inside the operating system's move-or-resize loop, with the
    /// size of the window that is being dragged. The event itself cannot be asked: a repaint
    /// request carries no size.
    /// </summary>
    private static bool TryGetDraggedWindowSize(out int width, out int height)
    {
        width = height = 0;

        var info = new GuiThreadInfo { cbSize = Marshal.SizeOf<GuiThreadInfo>() };
        if (!GetGUIThreadInfo(GetCurrentThreadId(), ref info)) return false;
        if ((info.flags & GUI_INMOVESIZE) == 0) return false;
        if (info.hwndMoveSize == IntPtr.Zero) return false;

        if (!GetClientRect(info.hwndMoveSize, out var client)) return false;
        width = client.right - client.left;
        height = client.bottom - client.top;
        return width > 0 && height > 0;
    }
}
#endif
