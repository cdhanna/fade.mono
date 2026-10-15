using System.Reflection;
using Fade.MonoGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace Fade.MonoGame.Steam;

/// <summary>
/// The pictures that Steam has of the buttons of every controller that it knows, and which
/// kind of controller the player is holding.
///
/// The game does not read the controller through Steam. It reads it the way that it always
/// has, as an Xbox controller, and Steam is what makes a PlayStation or a Switch controller
/// look like one. So all that is asked of Steam here is: what kind of controller is really
/// there, what is the button on it that sits where this button of an Xbox controller does,
/// and where is the picture of it.
///
/// Set the environment variable FADE_STEAM_INPUT_TYPE to the number of a kind of controller
/// (see <c>steam input type</c>) to be shown the pictures of one that is not plugged in. The
/// command <c>set steam input type</c> does the same while the game runs.
///
/// Facepunch.Steamworks keeps the calls for that to itself, and it is fetched by source and
/// is not ours to change. So they are found by reflection, once, when Steam starts. If any
/// of them is missing, as it may be after a move to a newer Facepunch, there is one line in
/// the log, and a game gets no pictures and carries on with its own.
/// </summary>
internal static class SteamInputGlyphs
{
    // a controller can be swapped for another while the game runs. looking is not free, so it
    // is done this often, in frames, and not on every one.
    const int LookEveryFrames = 30;

    sealed class LoadedGlyph
    {
        public string Button = "";
        public int Type;
        public int Size;
        public int Inset;
        public int Style;

        // the texture that was made. see SteamAchievements, which does the same.
        public Texture2D? Texture;
    }

    static readonly Dictionary<int, LoadedGlyph> Glyphs = new();

    // the buttons, by the names that padCode knows them by, and the name that Steam has for
    // each on an Xbox One controller, after "XBoxOne_". the last three are not buttons to
    // padCode: the whole of the d-pad, and the whole of each stick.
    static readonly Dictionary<string, string> XboxNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = "A", ["B"] = "B", ["X"] = "X", ["Y"] = "Y",
        ["LeftShoulder"] = "LeftBumper", ["RightShoulder"] = "RightBumper",
        ["Start"] = "Menu", ["Back"] = "View",
        ["LeftTrigger"] = "LeftTrigger_Pull", ["RightTrigger"] = "RightTrigger_Pull",
        ["LeftStick"] = "LeftStick_Click", ["RightStick"] = "RightStick_Click",
        ["LeftThumbstickUp"] = "LeftStick_DPadNorth", ["LeftThumbstickDown"] = "LeftStick_DPadSouth",
        ["LeftThumbstickLeft"] = "LeftStick_DPadWest", ["LeftThumbstickRight"] = "LeftStick_DPadEast",
        ["RightThumbstickUp"] = "RightStick_DPadNorth", ["RightThumbstickDown"] = "RightStick_DPadSouth",
        ["RightThumbstickLeft"] = "RightStick_DPadWest", ["RightThumbstickRight"] = "RightStick_DPadEast",
        ["DPadUp"] = "DPad_North", ["DPadDown"] = "DPad_South", ["DPadLeft"] = "DPad_West", ["DPadRight"] = "DPad_East",
        ["DPad"] = "DPad_North", ["LeftThumbstick"] = "LeftStick_Move", ["RightThumbstick"] = "RightStick_Move",
    };

    static object? _api;
    static MethodInfo? _shutdown;
    static MethodInfo? _forGamepadIndex;
    static MethodInfo? _typeForHandle;
    static MethodInfo? _translate;
    static MethodInfo? _glyphPng;
    static FieldInfo? _handleValue;
    static FieldInfo? _controllerHandle;
    static Type? _actionOriginType;

    static bool _started;
    static int _frames;
    static int _pretendType;

    /// <summary>The kind of controller that Steam says the player has, as Steam numbers them. 0 is none.</summary>
    public static int ControllerType { get; private set; }

    public static void Start()
    {
        if (_started) return;
        try
        {
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            _api = typeof(SteamInput).GetProperty("Internal", hidden)?.GetValue(null);
            if (_api == null) throw new InvalidOperationException("Steam has no input interface.");

            var api = _api.GetType();
            _shutdown = Need(api.GetMethod("Shutdown", hidden), "Shutdown");
            _forGamepadIndex = Need(api.GetMethod("GetControllerForGamepadIndex", hidden), "GetControllerForGamepadIndex");
            _typeForHandle = Need(api.GetMethod("GetInputTypeForHandle", hidden), "GetInputTypeForHandle");
            _translate = Need(api.GetMethod("TranslateActionOrigin", hidden), "TranslateActionOrigin");
            _glyphPng = Need(api.GetMethod("GetGlyphPNGForActionOrigin", hidden), "GetGlyphPNGForActionOrigin");

            _actionOriginType = _translate.ReturnType;
            _handleValue = Need(_forGamepadIndex.ReturnType.GetField("Value", hidden), "InputHandle_t.Value");
            _controllerHandle = Need(typeof(Controller).GetField("Handle", hidden), "Controller.Handle");

            // false: Steam moves its input along by itself, when its callbacks are run
            var init = Need(api.GetMethod("Init", hidden), "Init");
            if (!(bool)init.Invoke(_api, new object[] { false })!) throw new InvalidOperationException("Steam would not start its input.");

            _started = true;
            _frames = 0;
            int.TryParse(Environment.GetEnvironmentVariable("FADE_STEAM_INPUT_TYPE"), out _pretendType);
            ControllerType = _pretendType;
        }
        catch (Exception ex)
        {
            _started = false;
            Console.WriteLine("[steam] there will be no pictures of controller buttons: " + (ex.InnerException ?? ex).Message);
        }
    }

    static T Need<T>(T? found, string name) where T : class
    {
        return found ?? throw new MissingMemberException("Facepunch.Steamworks has no " + name + " any more.");
    }

    public static void Stop()
    {
        if (!_started) return;
        _started = false;
        ControllerType = 0;
        Glyphs.Clear();
        try
        {
            _shutdown!.Invoke(_api, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[steam] could not shut the input down: " + (ex.InnerException ?? ex).Message);
        }
    }

    /// <summary>
    /// Carry on as if a controller of this kind were plugged in, whatever is. 0 goes back to
    /// looking at what is really there.
    /// </summary>
    public static void Pretend(int type)
    {
        if (!_started) return;
        _pretendType = Math.Max(type, 0);
        ControllerType = _pretendType;

        // look on the very next frame
        _frames = 0;
    }

    /// <summary>Once a frame, after the callbacks of Steam have run.</summary>
    public static void Tick()
    {
        if (!_started || _pretendType > 0) return;
        if (_frames++ % LookEveryFrames != 0) return;

        try
        {
            var handle = FindController();
            ControllerType = handle == null ? GuessType() : Convert.ToInt32(_typeForHandle!.Invoke(_api, new[] { handle }));
        }
        catch (Exception ex)
        {
            // it will not get better by asking again
            Console.WriteLine("[steam] could not look for a controller: " + (ex.InnerException ?? ex).Message);
            Stop();
        }
    }

    // The game listens to the first controller, so that is the one to ask Steam about. Steam
    // only knows which one that is while it is the one that makes the controller look like an
    // Xbox controller. When it does not say, the first controller that it has at all will do.
    static object? FindController()
    {
        var first = _forGamepadIndex!.Invoke(_api, new object[] { 0 });
        if (first != null && (ulong)_handleValue!.GetValue(first)! != 0) return first;

        foreach (var controller in SteamInput.Controllers)
        {
            if (controller.Id != 0) return _controllerHandle!.GetValue(controller);
        }

        return null;
    }

    // Steam only knows of a controller that it is handling itself. It handles none for a game
    // that it did not start, and a player can turn it off for each make of controller. The game
    // still hears the controller then, and what it is called says what kind it is. One with a
    // name that says nothing is taken for an Xbox controller, which is what it is read as.
    static int GuessType()
    {
        for (var playerIndex = PlayerIndex.One; playerIndex <= PlayerIndex.Four; playerIndex++)
        {
            var pad = GamePad.GetCapabilities(playerIndex);
            if (!pad.IsConnected) continue;

            var name = pad.DisplayName ?? "";
            bool Has(string part) => name.Contains(part, StringComparison.OrdinalIgnoreCase);

            if (Has("PS5") || Has("DualSense")) return (int)InputType.PS5Controller;
            if (Has("PS4") || Has("DualShock 4")) return (int)InputType.PS4Controller;
            if (Has("PS3")) return (int)InputType.PS3Controller;
            if (Has("Joy-Con")) return (int)InputType.SwitchJoyConPair;
            if (Has("Switch") || Has("Nintendo")) return (int)InputType.SwitchProController;
            if (Has("Steam Deck")) return (int)InputType.SteamDeckController;
            if (Has("Steam Controller")) return (int)InputType.SteamController;
            if (Has("360")) return (int)InputType.XBox360Controller;
            return (int)InputType.XBoxOneController;
        }

        return 0;
    }

    public static bool LoadGlyph(int textureId, string button, int size, int inset, int style)
    {
        if (!_started || ControllerType <= 0 || string.IsNullOrEmpty(button)) return false;
        if (!XboxNames.TryGetValue(button, out var xbox))
        {
            Console.WriteLine($"[steam] there is no controller button named \"{button}\".");
            return false;
        }

        size = Math.Clamp(size, 1, 256);
        inset = Math.Clamp(inset, 0, (size - 1) / 2);
        if (style < 0) style = 0;

        TextureSystem.GetTextureIndex(textureId, out _, out var current);
        if (Glyphs.TryGetValue(textureId, out var loaded)
            && loaded.Type == ControllerType
            && loaded.Size == size
            && loaded.Inset == inset
            && loaded.Style == style
            && loaded.Texture != null
            && string.Equals(loaded.Button, button, StringComparison.OrdinalIgnoreCase)
            && ReferenceEquals(current.watchedTexture.Asset, loaded.Texture))
        {
            return true;
        }

        try
        {
            // the button of an Xbox controller, and then the one that sits in its place on the
            // controller that is there. a controller that has nothing in that place gives nothing.
            var source = Enum.Parse(_actionOriginType!, "XBoxOne_" + xbox);
            var origin = _translate!.Invoke(_api, new[] { Enum.ToObject(typeof(InputType), ControllerType), source })!;
            if (Convert.ToInt32(origin) == 0) return false;

            // Steam has a picture of a whole d-pad, but it will not say which one goes with
            // which controller. It is named after the picture of up, so it is found from that.
            if (string.Equals(button, "DPad", StringComparison.OrdinalIgnoreCase))
            {
                var whole = origin.ToString()!.Replace("_DPad_North", "_DPad_Move");
                if (Enum.TryParse(_actionOriginType!, whole, out var moved) && moved != null) origin = moved;
            }

            // always the biggest picture that Steam has. it is shrunk to the size that was asked
            // for here, so that the game can draw it pixel for pixel, which is the only way that
            // a small picture stays sharp.
            var path = _glyphPng!.Invoke(_api, new[] { origin, GlyphSize.Large, (uint)style }) as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

            // FromStream leaves the alpha straight, which is what LoadTextureFromPixels wants
            int width, height;
            byte[] pixels;
            using (var stream = File.OpenRead(path))
            using (var image = Texture2D.FromStream(GameSystem.game.GraphicsDevice, stream))
            {
                width = image.Width;
                height = image.Height;
                pixels = new byte[width * height * 4];
                image.GetData(pixels);
            }

            if (!TextureSystem.LoadTextureFromPixels(textureId, size, size, Shrink(pixels, width, height, size, inset))) return false;

            TextureSystem.GetTextureIndex(textureId, out _, out var made);
            Glyphs[textureId] = new LoadedGlyph
            {
                Button = button, Type = ControllerType, Size = size, Inset = inset, Style = style,
                Texture = made.watchedTexture.Asset
            };
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[steam] could not get the picture of \"{button}\": {(ex.InnerException ?? ex).Message}");
            return false;
        }
    }

    // Fit a picture into the middle of a square that is `size` pixels across, leaving `inset`
    // pixels clear on every side. Every pixel of the square is the average of all of the picture
    // that lands on it, so thin lines get fainter as they shrink, and do not come and go.
    static byte[] Shrink(byte[] source, int width, int height, int size, int inset)
    {
        var result = new byte[size * size * 4];
        var room = size - inset * 2;
        var scale = Math.Max(width, height) / (double)room;
        var wide = Math.Clamp((int)Math.Round(width / scale), 1, room);
        var high = Math.Clamp((int)Math.Round(height / scale), 1, room);
        var left = inset + (room - wide) / 2;
        var top = inset + (room - high) / 2;
        var stepX = width / (double)wide;
        var stepY = height / (double)high;

        for (var y = 0; y < high; y++)
        {
            var fromY = y * stepY;
            var toY = Math.Min(fromY + stepY, height);
            for (var x = 0; x < wide; x++)
            {
                var fromX = x * stepX;
                var toX = Math.Min(fromX + stepX, width);

                // the colors are weighed by their alpha, so that the clear pixels around the
                // edge of a shape do not darken it
                double r = 0, g = 0, b = 0, a = 0, area = 0;
                for (var sy = (int)fromY; sy < toY; sy++)
                {
                    var coverY = Math.Min(sy + 1, toY) - Math.Max(sy, fromY);
                    for (var sx = (int)fromX; sx < toX; sx++)
                    {
                        var cover = coverY * (Math.Min(sx + 1, toX) - Math.Max(sx, fromX));
                        var at = (sy * width + sx) * 4;
                        var alpha = source[at + 3] * cover;
                        r += source[at] * alpha;
                        g += source[at + 1] * alpha;
                        b += source[at + 2] * alpha;
                        a += alpha;
                        area += cover;
                    }
                }

                if (a <= 0 || area <= 0) continue;
                var to = ((top + y) * size + left + x) * 4;
                result[to] = (byte)Math.Round(r / a);
                result[to + 1] = (byte)Math.Round(g / a);
                result[to + 2] = (byte)Math.Round(b / a);
                result[to + 3] = (byte)Math.Round(a / area);
            }
        }

        return result;
    }
}
