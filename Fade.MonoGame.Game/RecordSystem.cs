using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.Fade.SpriteBatch;

namespace Fade.MonoGame.Core;

/// <summary>
/// Records the game to a video file, frame by frame, through ffmpeg.
///
/// While a recording runs, the game's clock is stepped by exactly one frame of the video for
/// every frame that is drawn, however long that frame really took. A slow frame (and the copy
/// of every frame back from the graphics card is slow) never drops a frame from the video or
/// makes it stutter, and the same program records the same video every time.
///
/// What is recorded is the finished picture, with the screen effect, at a size of its own
/// that has nothing to do with the window. The debug UI and the gizmos are not in it.
/// </summary>
public static class RecordSystem
{
    public static int width = 1920;
    public static int height = 1080;
    public static int framesPerSecond = 60;

    // the part of the main buffer that is recorded, in its own pixels. a width of 0 is all of it.
    // a part of it fills the video, which is a close up. it can change on every frame, to push in.
    public static float viewX, viewY, viewWidth, viewHeight;

    public static bool IsRecording => _process != null;
    public static long frameCount;

    // THE SIDECAR. the sounds are not recorded, but what played, and on which frame, is written down next
    // to the video, in a .json of the same name, with the marks that the program made. something that
    // puts the videos together (build/trailer_cut.py) mixes the sounds back in from that, and lines the
    // marks up with the music.
    //
    // written with Fade's own JSON (IJsonable), not System.Text.Json, which finds the properties by
    // reflection and is switched off in a trimmed release build. it has no long and no float, so those
    // go through an int and a double on the way; the keys, and so the file, are what they always were.
    public class SoundEvent : FadeBasic.Json.IJsonable
    {
        public long frame { get; set; }
        public int sfx { get; set; } // the sound slot: playing it again cuts off what it was playing
        public string asset { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public float pan { get; set; }
        public int group { get; set; }
        public bool loop { get; set; }

        public void ProcessJson<T>(ref T op) where T : FadeBasic.Json.IJsonOperation
        {
            int f = (int)frame, s = sfx, g = group;
            string a = asset;
            double v = volume, p = pitch, n = pan;
            bool l = loop;
            op.IncludeField(nameof(frame), ref f);
            op.IncludeField(nameof(sfx), ref s);
            op.IncludeField(nameof(asset), ref a);
            op.IncludeField(nameof(volume), ref v);
            op.IncludeField(nameof(pitch), ref p);
            op.IncludeField(nameof(pan), ref n);
            op.IncludeField(nameof(group), ref g);
            op.IncludeField(nameof(loop), ref l);
            frame = f; sfx = s; asset = a; volume = (float)v; pitch = (float)p; pan = (float)n; group = g; loop = l;
        }
    }

    public class MarkEvent : FadeBasic.Json.IJsonable
    {
        public long frame { get; set; }
        public string name { get; set; }

        public void ProcessJson<T>(ref T op) where T : FadeBasic.Json.IJsonOperation
        {
            int f = (int)frame;
            string n = name;
            op.IncludeField(nameof(frame), ref f);
            op.IncludeField(nameof(name), ref n);
            frame = f; name = n;
        }
    }

    public class Sidecar : FadeBasic.Json.IJsonable
    {
        public string video { get; set; }
        public int fps { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public long frames { get; set; }
        public List<MarkEvent> marks { get; set; } = new List<MarkEvent>();
        public List<SoundEvent> sounds { get; set; } = new List<SoundEvent>();

        public void ProcessJson<T>(ref T op) where T : FadeBasic.Json.IJsonOperation
        {
            string v = video;
            int r = fps, w = width, h = height, f = (int)frames;
            var m = marks;
            var s = sounds;
            op.IncludeField(nameof(video), ref v);
            op.IncludeField(nameof(fps), ref r);
            op.IncludeField(nameof(width), ref w);
            op.IncludeField(nameof(height), ref h);
            op.IncludeField(nameof(frames), ref f);
            op.IncludeField(nameof(marks), ref m);
            op.IncludeField(nameof(sounds), ref s);
            video = v; fps = r; width = w; height = h; frames = f; marks = m; sounds = s;
        }
    }

    static Sidecar _sidecar;

    /// <summary>A sound has started playing. Game time and frames are the same while recording, so it is written down on the frame about to be drawn.</summary>
    public static void LogSound(int sfxId)
    {
        if (_sidecar == null) return;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out _, out var sfx);
        AudioSystem.GetAudioEffectIndex(sfx.clipId, out _, out var clip);
        var mix = AudioMixSystem.GetSound(sfxId);
        bool loop = false;
        try { loop = sfx.instance != null && sfx.instance.IsLooped; } catch { }
        _sidecar.sounds.Add(new SoundEvent
        {
            frame = frameCount,
            sfx = sfxId,
            asset = clip.path ?? "",
            volume = mix.volume,
            pitch = mix.pitch,
            pan = mix.pan,
            group = mix.group,
            loop = loop,
        });
    }

    /// <summary>A moment of the recording that has a name, such as the frame a swap pops.</summary>
    public static void Mark(string name)
    {
        if (_sidecar == null) return;
        _sidecar.marks.Add(new MarkEvent { frame = frameCount, name = name });
    }

    static Process _process;
    static Thread _writer;
    static BlockingCollection<byte[]> _frames;
    static ConcurrentBag<byte[]> _spare = new ConcurrentBag<byte[]>();
    static RenderTarget2D _target;
    static string _path;

    // what the game had before the recording took over its pacing
    static Game _pacedGame;
    static bool _wasFixedTimeStep;
    static bool _wasVsync;

    // THE CLOCK. Once a recording has started, the game's time is no longer the real time.
    // While recording, it is a count of frames. After, it is the real time plus however far
    // the count got ahead of (or behind) it, so the clock never jumps back.
    // FADE_STEPPED_CLOCK=1 steps the clock from the very first frame, and for good. a recording then
    // starts from the same moment of everything every time, not from wherever the real time had got
    // the game to while it started up.
    // FADE_TURBO=n is for long test runs: the game is not held to the screen's pace, so it runs as fast
    // as it can, and the clock is stepped, by n frames of time for every frame that runs (1 when it is
    // not a number). everything that waits for a tween or for `game ms` waits that many times less.
    // a recording still steps by one frame, so that the video plays at the right speed.
    public static readonly int turbo = ReadTurbo();
    static int ReadTurbo()
    {
        var text = Environment.GetEnvironmentVariable("FADE_TURBO");
        if (string.IsNullOrEmpty(text) || text == "0") return 0;
        return int.TryParse(text, out var n) && n > 0 ? n : 1;
    }
    static readonly bool _alwaysStepped = Environment.GetEnvironmentVariable("FADE_STEPPED_CLOCK") == "1" || turbo > 0;
    static long _updates, _draws, _captures;
    static bool _drawnSinceStep = true;
    static int _stepsWithoutDraw;
    static readonly List<Thread> _finishing = new List<Thread>();

    /// <summary>Waits for every recording that is still being finished. Game1 calls this as the game closes.</summary>
    public static void Finish()
    {
        Stop();
        Thread[] waiting;
        lock (_finishing) waiting = _finishing.ToArray();
        foreach (var thread in waiting) thread.Join();
        lock (_finishing) _finishing.Clear();
    }
    static bool _clockTaken = _alwaysStepped;
    static bool _clockStepping = _alwaysStepped;
    static TimeSpan _clockTotal;
    static TimeSpan _clockOffset;
    static TimeSpan _lastReal;

    /// <summary>
    /// The program is starting over (a restart, or the next test). A clock that is always stepped
    /// starts over from 0 with it, so that what the program does is the same whatever ran before it.
    /// </summary>
    public static void ProgramReset()
    {
        Stop();
        viewX = viewY = viewWidth = viewHeight = 0;
        if (_alwaysStepped) _clockTotal = TimeSpan.Zero;
    }

    /// <summary>With FADE_TURBO, the game runs unpaced from the start. Game1 calls this once it is set up.</summary>
    public static void ApplyTurbo(Game game, GraphicsDeviceManager graphics)
    {
        if (turbo <= 0 || game == null) return;
        game.IsFixedTimeStep = false;
        if (graphics != null)
        {
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();
        }
        Console.WriteLine($"[fade] turbo: unpaced, {turbo} frame(s) of time per frame");
    }

    /// <summary>The time that this frame of the game should see. Game1 calls this first thing in Update and Draw.</summary>
    public static GameTime Clock(GameTime real, bool advance)
    {
        if (!_clockTaken) return real;

        var frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / Math.Max(1, framesPerSecond));
        if (turbo > 1 && !IsRecording) frame = TimeSpan.FromTicks(frame.Ticks * turbo);
        if (_clockStepping)
        {
            if (advance) _updates++; else _draws++;
            // the clock only moves on once the frame before has been drawn. the window can skip a
            // draw now and then (when it is covered, say), and the time must not get ahead of the video.
            // but never for long: if the window stops drawing altogether, the time goes on anyway, or
            // anything that waits for it would wait forever.
            if (!advance) _drawnSinceStep = true;
            if (advance)
            {
                if (_drawnSinceStep || _stepsWithoutDraw >= 2)
                {
                    _clockTotal += frame;
                    _drawnSinceStep = false;
                    _stepsWithoutDraw = 0;
                }
                else
                {
                    _stepsWithoutDraw++;
                }
            }
            _lastReal = real.TotalGameTime;
            return new GameTime(_clockTotal, frame);
        }

        return new GameTime(real.TotalGameTime + _clockOffset, real.ElapsedGameTime);
    }

    public static void Start(string path)
    {
#if BROWSER
        Console.WriteLine("[record] there is no recording in the browser.");
#else
        if (IsRecording) Stop();

        if (!Path.HasExtension(path)) path += ".mp4";
        path = Path.GetFullPath(path);
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        var ffmpeg = Environment.GetEnvironmentVariable("FADE_FFMPEG");
        if (string.IsNullOrEmpty(ffmpeg)) ffmpeg = "ffmpeg";

        // the frames go in raw, one after another, on stdin
        var input = $"-f rawvideo -pix_fmt rgba -s {width}x{height} -r {framesPerSecond} -i -";
        var output = Path.GetExtension(path).ToLowerInvariant() switch
        {
            // near lossless, for an editor that would rather not decode h.264
            ".mov" => "-c:v prores_ks -profile:v 3 -pix_fmt yuv422p10le",
            // a folder of numbered pngs; the path should hold a %05d
            ".png" => "-c:v png",
            _ => "-c:v libx264 -preset medium -crf 14 -pix_fmt yuv420p -movflags +faststart",
        };
        var info = new ProcessStartInfo(ffmpeg, $"-y -loglevel error {input} {output} \"{path}\"")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            _process = Process.Start(info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[record] could not start ffmpeg ({ffmpeg}): {ex.Message}. put it on the PATH, or set FADE_FFMPEG to it.");
            _process = null;
            return;
        }

        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) Console.WriteLine("[record] ffmpeg: " + e.Data);
        };
        _process.BeginErrorReadLine();

        _path = path;
        frameCount = 0;
        _updates = _draws = _captures = 0;
        _sidecar = new Sidecar { video = Path.GetFileName(path), fps = framesPerSecond, width = width, height = height };
        _frames = new BlockingCollection<byte[]>(boundedCapacity: 8);
        var stdin = _process.StandardInput.BaseStream;
        var frames = _frames;
        _writer = new Thread(() =>
        {
            try
            {
                foreach (var frame in frames.GetConsumingEnumerable())
                {
                    stdin.Write(frame, 0, frame.Length);
                    _spare.Add(frame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[record] writing to ffmpeg failed: " + ex.Message);
                // keep taking frames, so that the game is never left waiting on a full queue
                foreach (var _ in frames.GetConsumingEnumerable()) { }
            }
        }) { IsBackground = true, Name = "record writer" };
        _writer.Start();

        // every frame is drawn, and none is held back for the screen
        var game = GameSystem.game;
        if (game != null)
        {
            _pacedGame = game;
            _wasFixedTimeStep = game.IsFixedTimeStep;
            game.IsFixedTimeStep = false;
            var graphics = GameSystem.graphicsDeviceManager;
            if (graphics != null)
            {
                _wasVsync = graphics.SynchronizeWithVerticalRetrace;
                graphics.SynchronizeWithVerticalRetrace = false;
                graphics.ApplyChanges();
            }
        }

        // the shake of the screen goes the same way in every recording
        RenderSystem.shakeRandom = new Random(1);

        // the clock carries on from the time the game last saw
        if (!_clockStepping)
        {
            _clockTotal = GameSystem.latestTime?.TotalGameTime ?? TimeSpan.Zero;
            _clockTaken = true;
            _clockStepping = true;
        }

        Console.WriteLine($"[record] recording {width}x{height} at {framesPerSecond} fps to {path}");
#endif
    }

    public static void Stop()
    {
#if !BROWSER
        if (_process == null) return;

        // the rest of the frames are written, and ffmpeg finishes the file, on a thread of its own. the
        // game goes straight on: waiting for it here held the window up for seconds at a time, long
        // enough for Windows to call the game "not responding". Finish waits for all of them.
        var process = _process;
        var writer = _writer;
        var frames = _frames;
        var sidecar = _sidecar;
        var path = _path;
        var count = frameCount;
        var updates = _updates;
        var draws = _draws;
        if (sidecar != null) sidecar.frames = count;
        frames.CompleteAdding();
        var finishing = new Thread(() =>
        {
            writer.Join();
            try
            {
                process.StandardInput.BaseStream.Flush();
                process.StandardInput.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[record] closing ffmpeg's input failed: " + ex.Message);
            }
            if (!process.WaitForExit(300_000)) Console.WriteLine($"[record] ffmpeg did not finish {path} in five minutes; the file may be cut short.");
            else if (process.ExitCode != 0) Console.WriteLine($"[record] ffmpeg failed with exit code {process.ExitCode} for {path}.");
            else Console.WriteLine($"[record] wrote {count} frames to {path} (updates {updates}, draws {draws})");

            if (sidecar != null)
            {
                var sidecarPath = Path.ChangeExtension(path, ".json");
                try
                {
                    File.WriteAllText(sidecarPath, FadeBasic.Json.JsonableExtensions.Jsonify(sidecar));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[record] could not write {sidecarPath}: {ex.Message}");
                }
            }
            process.Dispose();
            frames.Dispose();
        }) { IsBackground = false, Name = "record finish" };
        finishing.Start();
        lock (_finishing) _finishing.Add(finishing);

        _sidecar = null;
        _process = null;
        _writer = null;
        _frames = null;

        if (_pacedGame != null)
        {
            _pacedGame.IsFixedTimeStep = _wasFixedTimeStep;
            var graphics = GameSystem.graphicsDeviceManager;
            if (graphics != null)
            {
                graphics.SynchronizeWithVerticalRetrace = _wasVsync;
                graphics.ApplyChanges();
            }
            _pacedGame = null;
        }

        // from here the clock follows the real time again, from where the count left it
        if (!_alwaysStepped)
        {
            _clockStepping = false;
            _clockOffset = _clockTotal - _lastReal;
        }
#endif
    }

    /// <summary>
    /// Draws the finished picture of this frame into the recording. Game1 calls this from Draw,
    /// with the same picture and screen effect that it puts in the window.
    /// </summary>
    public static void Capture(GraphicsDevice device, SpriteBatch spriteBatch, Texture2D source, Effect screenEffect)
    {
#if !BROWSER
        if (_process == null || source == null) return;

        if (_target == null || _target.IsDisposed || _target.Width != width || _target.Height != height)
        {
            _target?.Dispose();
            _target = new RenderTarget2D(device, width, height);
        }

        // the part of the source that is recorded. the source can be bigger than the main buffer
        // (the screen effect's own buffer), so the view is scaled to it.
        var mainWidth = RenderSystem.mainBuffer != null ? RenderSystem.renderWidth : source.Width;
        var toSource = source.Width / (float)mainWidth;
        Rectangle? part = null;
        float partWidth = source.Width, partHeight = source.Height;
        if (viewWidth > 0 && viewHeight > 0)
        {
            var rect = new Rectangle(
                (int)MathF.Round(viewX * toSource), (int)MathF.Round(viewY * toSource),
                (int)MathF.Round(viewWidth * toSource), (int)MathF.Round(viewHeight * toSource));
            rect = Rectangle.Intersect(rect, new Rectangle(0, 0, source.Width, source.Height));
            if (rect.Width > 0 && rect.Height > 0)
            {
                part = rect;
                partWidth = rect.Width;
                partHeight = rect.Height;
            }
        }

        // as big as it fits, in the middle
        var scale = MathF.Min(width / partWidth, height / partHeight);
        var position = new Vector2(
            MathF.Round((width - partWidth * scale) / 2),
            MathF.Round((height - partHeight * scale) / 2));

        // the shake is in pixels of the main buffer
        var shakeScale = toSource * scale;
        var shake = Matrix.CreateTranslation(RenderSystem.screenShakeOffset.X * shakeScale, RenderSystem.screenShakeOffset.Y * shakeScale, 0);

        var sampler = Math.Abs(scale - MathF.Round(scale)) > 0.001f ? SamplerState.LinearClamp : SamplerState.PointClamp;

        device.SetRenderTarget(_target);
        device.Clear(Color.Black);
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Immediate,
            blendState: BlendState.Opaque,
            samplerState: sampler,
            transformMatrix: shake,
            effect: screenEffect);
        spriteBatch.Draw(source, position, part, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        spriteBatch.End();
        device.SetRenderTarget(null);

        var size = width * height * 4;
        if (!_spare.TryTake(out var frame) || frame.Length != size) frame = new byte[size];
        _target.GetData(frame);

        // the picture is opaque, but the alpha that the buffers hold is not always 255, and a png would keep it
        for (var i = 3; i < frame.Length; i += 4) frame[i] = 255;

        _frames.Add(frame);
        frameCount++;
#endif
    }
}
