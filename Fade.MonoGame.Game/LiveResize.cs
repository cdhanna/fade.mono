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

        // One hook for the life of the process. A second Game1 (a reload builds one) just
        // becomes the game that the existing hook ticks.
        _game = game;
        _gameThreadId = Environment.CurrentManagedThreadId;
        if (_filter != null || _wndProc != null) return;

        // The Vulkan backend has SDL built into its own native runtime, where nothing outside
        // can reach it. An SDL2 library loaded by name here would be a second, separate SDL
        // that never sees the window -- the watch would install fine and never fire. So there,
        // the window's own message handler is hooked instead (InstallWindowHook).
        if (GraphicsBackend.Name == "Vulkan")
        {
            // The window is not up yet while the game is starting, so this is tried again
            // every frame for a while (EnsureWindowHook).
            _hookTries = 0;
            return;
        }

        try
        {
            _filter = OnSdlEvent;
            SDL_AddEventWatch(_filter, IntPtr.Zero);
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
        {
            // A backend that is not built on SDL2. The window simply goes back to freezing
            // during a drag, which is what it did before this existed.
            _filter = null;
        }
    }

    private static int OnSdlEvent(IntPtr userdata, IntPtr sdlEvent)
    {
        // The return value is ignored for a watch, but an exception must never get back into
        // SDL: there is no managed frame above this one to catch it.
        try
        {
            // SDL_WindowEvent: type, timestamp, windowID (4 bytes each), then the kind of
            // window event (1 byte, padded to 4), then its two numbers.
            if ((uint)Marshal.ReadInt32(sdlEvent, 0) != SDL_WINDOWEVENT) return 1;

            // While an edge is being dragged SDL does not report the new sizes at all -- those
            // arrive in a burst once the drag is over. What it does report is that the window
            // needs painting again, once for every size it passes through.
            var kind = Marshal.ReadByte(sdlEvent, 12);
            if (kind != SDL_WINDOWEVENT_EXPOSED && kind != SDL_WINDOWEVENT_SIZE_CHANGED) return 1;

            TickIfDragging();
        }
        catch (Exception ex)
        {
            GiveUp(ex);
        }

        return 1;
    }

    /// <summary>
    /// Run a frame at the size the window is being dragged to, if it is being dragged now.
    /// </summary>
    private static void TickIfDragging()
    {
        if (_ticking || _game == null) return;
        if (Environment.CurrentManagedThreadId != _gameThreadId) return;

        // Only during a drag. A size change the PROGRAM asked for (`set screen size`) is
        // reported from inside the frame that asked for it, and running a frame within a
        // frame is exactly what this must not do. MonoGame handles that one normally.
        if (!TryGetDraggedWindowSize(out var width, out var height)) return;

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

    private static void GiveUp(Exception ex)
    {
        if (!_reportedFailure)
        {
            _reportedFailure = true;
            Console.Error.WriteLine($"[fade] drawing during a window resize failed, and will not be tried again: {ex}");
        }

        // Stop ticking, rather than failing the same way on every pixel of every drag.
        _game = null;
    }

    // ---- the Vulkan backend: the window's own message handler ------------------------------------
    //
    // The modal loop that a drag runs still sends the window a WM_SIZE for every size it passes
    // through, to the window procedure, from inside the loop. So the window procedure is wrapped
    // (subclassed): every message goes on to the one that was there, and after a WM_SIZE during a
    // drag a frame is run at the new size, as the SDL watch does on the OpenGL backend.

    private const int GWLP_WNDPROC = -4;
    private const uint WM_SIZE = 0x0005;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr previous, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr lParam);

    // Held in a field for the same reason as _filter: the window keeps the function pointer.
    private static WndProc _wndProc;
    private static IntPtr _previousProc;

    // Frames the window hook has been looked for (-1, not wanted or done with), and how many
    // to look for before giving up.
    private static int _hookTries = -1;
    private const int HookTriesMax = 600;

    /// <summary>
    /// Called every frame (Game1.Draw): on the Vulkan backend, hooks the game's window once it is
    /// up. Nothing to do once it is hooked, or given up on.
    /// </summary>
    public static void EnsureWindowHook()
    {
        if (_hookTries < 0 || _wndProc != null || _game == null) return;
        _hookTries++;
        if (TryInstallWindowHook(_game))
        {
            _hookTries = -1;
            return;
        }
        if (_hookTries >= HookTriesMax)
        {
            _hookTries = -1;
            Console.Error.WriteLine("[fade] no game window found to keep drawing in while it is resized");
        }
    }

    private static bool TryInstallWindowHook(Game1 game)
    {
        try
        {
            var hwnd = FindGameWindow(game);
            if (hwnd == IntPtr.Zero) return false;

            _wndProc = OnWindowMessage;
            _previousProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
            if (_previousProc == IntPtr.Zero)
            {
                _wndProc = null;
                return false;
            }
            Console.WriteLine($"[fade] the window keeps drawing while it is resized (window 0x{hwnd.ToInt64():x})");
            return true;
        }
        catch (Exception ex)
        {
            _wndProc = null;
            _hookTries = -1;
            Console.Error.WriteLine($"[fade] could not hook the game window to keep drawing in while it is resized: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// The game's window: MonoGame's own handle when it is a window this thread made, or else
    /// the biggest visible window this thread made (the Vulkan runtime's handle may not be a
    /// Windows one). Never a window of another thread or process.
    /// </summary>
    private static IntPtr FindGameWindow(Game1 game)
    {
        var thread = GetCurrentThreadId();
        var handle = game.Window?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero && IsWindow(handle) && GetWindowThreadProcessId(handle, out _) == thread)
        {
            return handle;
        }

        var best = IntPtr.Zero;
        var bestArea = 0;
        EnumThreadWindows(thread, (hwnd, _) =>
        {
            if (IsWindowVisible(hwnd) && GetClientRect(hwnd, out var r))
            {
                var area = (r.right - r.left) * (r.bottom - r.top);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = hwnd;
                }
            }
            return true;
        }, IntPtr.Zero);
        return best;
    }

    private static IntPtr OnWindowMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // The window's own handler first, always: this only ever adds a frame after it.
        var result = CallWindowProc(_previousProc, hwnd, msg, wParam, lParam);
        if (msg != WM_SIZE) return result;

        // As with the watch, nothing may escape back into the operating system's loop.
        try
        {
            TickIfDragging();
        }
        catch (Exception ex)
        {
            GiveUp(ex);
        }
        return result;
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
