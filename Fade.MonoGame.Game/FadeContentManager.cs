#if !BROWSER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fade.MonoGame.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using NVorbis;

namespace Fade.MonoGame.Core;

// The desktop ContentManager. For everything a dev build has, it is the stock one: an asset with
// an .xnb next to it loads exactly as before, and nothing below runs.
//
// A release built with $(FadeMonoCompressContent) ships its XNBs Brotli-compressed, as
// Content/<name>.xnb.br, and OpenStream inflates each one as it is loaded (see CompressedContent).
//
// A release built with $(FadeMonoEncodeAudioToOgg) ships its sounds as Content/<name>.ogg instead
// of Content/<name>.xnb (see OggAudioEncoder in Fade.MonoGame.Content). This turns each one back
// into the SoundEffect XNB the pipeline made, so `Content.Load<SoundEffect>` and everything above
// it never know the difference.
//
// PreloadOggSounds runs before the program starts (Game1 calls it in Initialize, before the VM
// exists), so no sound is ever decoded while the game is running. It decodes each sound once and
// keeps the XNB in a cache on disk; a later launch opens those files the way it would open a
// shipped .xnb, and decodes nothing.
//
// THE CACHE. Content/sounds.manifest lists every sound with a hash of its .ogg, and the decoded XNB
// is stored as <cache>/v1/<hash>.xnb. A sound that an update did not change keeps its hash, and
// its file; one it changed has a new hash, misses, and is decoded. Files no sound in the manifest
// points at any more are deleted, so the cache never holds more than one copy of the game's sounds.
// Bump CacheVersion when the XNB this writes changes, and every old file is a miss.
//
// Where the cache is, the first of these that can be written:
//   1. The game's install folder: next to the executable, or on macOS next to the .app (not in
//      it: writing into a signed bundle breaks its signature), so an uninstall takes it too.
//      Not when the .app is straight in an Applications folder, which is not the game's own.
//   2. The player's cache folder: %LOCALAPPDATA%\<name>, ~/Library/Caches/<name>, or
//      $XDG_CACHE_HOME (~/.cache)/<name> on Linux. <name> is CacheName.
// Nothing here depends on how the game was installed: a Steam library is writable and gets (1);
// a read-only Program Files install or a disk image gets (2). If neither can be written, every
// sound is decoded in memory on every launch, and the game runs exactly as it would with no cache.
public sealed class FadeContentManager : ContentManager
{
    private const string CacheVersion = "v1";
    private const string CacheFolderName = "SoundCache";

    /// <summary>
    /// The folder name the cache uses under the player's cache folder, when the install folder
    /// cannot be written. Set it before the game runs. Defaults to the executable's name.
    /// </summary>
    public string CacheName { get; set; } =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "FadeGame";

    /// <summary>The folder the decoded sounds are cached in; null when there is none.</summary>
    public string CacheDirectory { get; private set; }

    // Asset name (forward slashes, no extension) -> the XNB bytes decoded from its .ogg this launch.
    // Handed out once and then dropped: the SoundEffect made from it holds the audio from then on.
    private readonly ConcurrentDictionary<string, byte[]> _decoded = new ConcurrentDictionary<string, byte[]>();

    // Asset name -> its decoded XNB in the cache. Kept, so a sound loaded again opens the file again.
    private readonly ConcurrentDictionary<string, string> _cached = new ConcurrentDictionary<string, string>();

    public FadeContentManager(IServiceProvider services, string rootDirectory) : base(services, rootDirectory)
    {
    }

    public void PreloadOggSounds()
    {
        string root;
        try { root = this.GetRootDirectoryFullPath(); }
        catch { return; }
        if (!Directory.Exists(root)) return;

        var sw = Stopwatch.StartNew();

        // name -> hash. No manifest (an older build, or .ogg files dropped in by hand) means no
        // cache: every .ogg is decoded, as before.
        var sounds = ReadManifest(Path.Combine(root, OggAudioTags.ManifestFileName));
        if (sounds == null)
        {
            sounds = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(root, "*.ogg", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, file);
                sounds[Normalize(rel.Substring(0, rel.Length - ".ogg".Length))] = null;
            }
        }
        if (sounds.Count == 0) return;

        var cacheDir = sounds.Values.Any(h => h != null) ? FindCacheDirectory() : null;
        CacheDirectory = cacheDir;

        var toDecode = new List<(string name, string hash)>();
        foreach (var (name, hash) in sounds)
        {
            var path = cacheDir != null && hash != null ? Path.Combine(cacheDir, hash + ".xnb") : null;
            if (path != null && IsWholeXnb(path)) _cached[name] = path;
            else toDecode.Add((name, hash));
        }

        long decodedBytes = 0;
        int written = 0;
        // Longest first, so the one big file is not what the last thread starts on.
        Parallel.ForEach(toDecode.OrderByDescending(s => OggLength(root, s.name)), sound =>
        {
            try
            {
                byte[] xnb;
                using (var stream = File.OpenRead(Path.Combine(root, sound.name + ".ogg")))
                    xnb = DecodeToXnb(stream);
                _decoded[sound.name] = xnb;
                Interlocked.Add(ref decodedBytes, xnb.Length);

                if (cacheDir != null && sound.hash != null && TryWriteCacheFile(cacheDir, sound.hash, xnb))
                    Interlocked.Increment(ref written);
            }
            catch (Exception ex)
            {
                // Left for OpenStream, which tries again and reports it at the load that needs it.
                Console.Error.WriteLine($"[fade] ogg preload failed: '{sound.name}': {ex.Message}");
            }
        });

        if (cacheDir != null) DeleteUnusedCacheFiles(cacheDir, sounds.Values);

        Console.WriteLine($"[fade] {sounds.Count} ogg sounds: {_cached.Count} from the cache, " +
                          $"{toDecode.Count} decoded ({decodedBytes / 1048576.0:F0} MB, {written} cached) " +
                          $"in {sw.ElapsedMilliseconds} ms. cache: {cacheDir ?? "none"}");
    }

    protected override Stream OpenStream(string assetName)
    {
        var name = Normalize(assetName);
        if (_decoded.TryRemove(name, out var bytes))
            return new MemoryStream(bytes, writable: false);
        if (_cached.TryGetValue(name, out var cachedPath))
        {
            try { return File.OpenRead(cachedPath); }
            catch (IOException) { _cached.TryRemove(name, out _); } // deleted under us: decode it instead
        }

        try
        {
            return base.OpenStream(assetName);
        }
        // No .xnb. An .xnb.br is any asset of a compressed release; an .ogg is a sound of an Ogg
        // release. Neither, and the stock "content file was not found" goes up as it always did.
        catch (ContentLoadException) when (TryOpenTitleFile(assetName + CompressedContent.Suffix, out var compressed))
        {
            return Inflate(compressed);
        }
        catch (ContentLoadException) when (TryOpenTitleFile(assetName + ".ogg", out var ogg))
        {
            using (ogg)
                return new MemoryStream(DecodeToXnb(ogg), writable: false);
        }
    }

    private bool TryOpenTitleFile(string relativePath, out Stream stream)
    {
        try
        {
            stream = TitleContainer.OpenStream(Path.Combine(RootDirectory, relativePath));
            return true;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            stream = null;
            return false;
        }
    }

    // An XNB that a release built with $(FadeMonoCompressContent) shipped Brotli-compressed (see
    // CompressedContent): inflated whole, so MonoGame gets an ordinary seekable stream of the
    // very bytes the pipeline wrote. Measured: the fish game's 16 MB of textures in about 0.34 MB.
    private static Stream Inflate(Stream compressed)
    {
        var xnb = new MemoryStream();
        using (compressed)
        using (var brotli = new System.IO.Compression.BrotliStream(compressed, System.IO.Compression.CompressionMode.Decompress))
            brotli.CopyTo(xnb);
        xnb.Position = 0;
        return xnb;
    }

    private static string Normalize(string assetName) => assetName.Replace('\\', '/');

    private static long OggLength(string root, string name)
    {
        try { return new FileInfo(Path.Combine(root, name + ".ogg")).Length; }
        catch { return 0; }
    }

    private static Dictionary<string, string> ReadManifest(string path)
    {
        if (!File.Exists(path)) return null;
        var sounds = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(path))
        {
            var tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            var hash = line.Substring(tab + 1).Trim();
            // The hash becomes a file name, so it is held to what the build writes: hex digits.
            sounds[line.Substring(0, tab)] = hash.Length > 0 && hash.All(Uri.IsHexDigit) ? hash : null;
        }
        return sounds;
    }

    // ── The cache folder ──────────────────────────────────────────────────────────────────────

    private string FindCacheDirectory()
    {
        foreach (var candidate in CacheCandidates())
        {
            if (candidate == null) continue;
            var dir = Path.Combine(candidate, CacheFolderName, CacheVersion);
            if (CanWrite(dir)) return dir;
        }
        return null;
    }

    private IEnumerable<string> CacheCandidates()
    {
        yield return InstallFolder();

        string userCache;
        if (OperatingSystem.IsWindows())
            userCache = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        else if (OperatingSystem.IsMacOS())
            userCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches");
        else
            userCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME") is { Length: > 0 } xdg
                ? xdg
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
        yield return string.IsNullOrEmpty(userCache) ? null : Path.Combine(userCache, CacheName);
    }

    // The folder the game was installed into: the executable's, or on macOS the one holding the
    // .app. Null for an .app that sits straight in an Applications folder.
    private static string InstallFolder()
    {
        var exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!OperatingSystem.IsMacOS()) return exeDir;

        var macOs    = new DirectoryInfo(exeDir);                 // X.app/Contents/MacOS
        var contents = macOs.Parent;
        var app      = contents?.Parent;
        if (macOs.Name != "MacOS" || contents?.Name != "Contents" || app == null ||
            !app.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            return exeDir;                                        // not in a bundle

        var holder = app.Parent?.FullName.TrimEnd('/');
        if (holder == null) return null;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('/');
        if (holder == "/Applications" || holder == home + "/Applications") return null;
        return holder;
    }

    // Created, and a file can be written and deleted in it. Read-only installs fail here.
    private static bool CanWrite(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            var probe = Path.Combine(dir, ".probe-" + Guid.NewGuid().ToString("N"));
            File.WriteAllBytes(probe, Array.Empty<byte>());
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // A cached XNB is only used whole: its header records its own length, and a file cut short by
    // a crash or a full disk does not match it.
    private static bool IsWholeXnb(string path)
    {
        try
        {
            using var file = File.OpenRead(path);
            Span<byte> header = stackalloc byte[10];
            if (file.Read(header) != 10) return false;
            return header[0] == 'X' && header[1] == 'N' && header[2] == 'B' &&
                   BitConverter.ToInt32(header.Slice(6)) == file.Length;
        }
        catch
        {
            return false;
        }
    }

    // Written under a temporary name and then renamed, so the cache never holds half a file.
    private static bool TryWriteCacheFile(string cacheDir, string hash, byte[] xnb)
    {
        var final = Path.Combine(cacheDir, hash + ".xnb");
        var temp  = final + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, xnb);
            File.Move(temp, final, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[fade] could not cache a sound in {cacheDir}: {ex.Message}");
            try { File.Delete(temp); } catch { }
            return false;
        }
    }

    // Everything in this version's folder that the manifest does not point at: sounds an update
    // replaced, and temp files a crash left. Older versions' folders go entirely. Only touches
    // what this class writes, inside its own SoundCache folder.
    private static void DeleteUnusedCacheFiles(string cacheDir, IEnumerable<string> hashes)
    {
        var keep = new HashSet<string>(hashes.Where(h => h != null).Select(h => h + ".xnb"), StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var file in Directory.GetFiles(cacheDir))
            {
                var fileName = Path.GetFileName(file);
                if (keep.Contains(fileName)) continue;
                if (fileName.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                    File.Delete(file);
            }
            foreach (var other in Directory.GetDirectories(Path.GetDirectoryName(cacheDir)!))
            {
                var dirName = Path.GetFileName(other);
                if (dirName != CacheVersion && dirName.Length > 1 && dirName[0] == 'v' && dirName.Skip(1).All(char.IsDigit))
                    Directory.Delete(other, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[fade] could not clean the sound cache: {ex.Message}");
        }
    }

    private const string SoundEffectReaderName = "Microsoft.Xna.Framework.Content.SoundEffectReader";

    // An .ogg written by OggAudioEncoder -> the 16-bit PCM SoundEffect XNB it was made from. The
    // tags carry what Vorbis cannot: the exact length and the XNB's loop points and duration. An
    // .ogg without them (one dropped in by hand) still loads, as its decoded length with no loop.
    private static byte[] DecodeToXnb(Stream oggStream)
    {
        using var reader = new VorbisReader(oggStream, closeOnDispose: false);
        int channels   = reader.Channels;
        int sampleRate = reader.SampleRate;

        long samples = TagLong(reader, OggAudioTags.Samples, -1);
        short[] pcm  = samples >= 0
            ? DecodeExactly(reader, samples * channels)
            : DecodeAll(reader);
        samples = pcm.Length / channels;

        int loopStart  = (int)TagLong(reader, OggAudioTags.LoopStart, 0);
        int loopLength = (int)TagLong(reader, OggAudioTags.LoopLength, samples);
        int durationMs = (int)TagLong(reader, OggAudioTags.DurationMs, samples * 1000 / sampleRate);
        var platform   = reader.Tags.GetTagSingle(OggAudioTags.XnbPlatform);
        byte platformByte = string.IsNullOrEmpty(platform) ? (byte)'d' : (byte)platform[0];

        int dataSize = pcm.Length * 2;
        var ms = new MemoryStream(dataSize + 128);
        using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            w.Write((byte)'X'); w.Write((byte)'N'); w.Write((byte)'B'); w.Write(platformByte);
            w.Write((byte)5);                    // XNB format version
            w.Write((byte)0);                    // flags: uncompressed
            w.Write(0);                          // total file size, patched below
            w.Write7BitEncodedInt(1);            // one type reader
            w.Write(SoundEffectReaderName);
            w.Write(0);                          // its version
            w.Write7BitEncodedInt(0);            // no shared resources
            w.Write7BitEncodedInt(1);            // the root object uses reader 1

            w.Write(18);                         // WAVEFORMATEX, 16-bit PCM
            w.Write((short)1);
            w.Write((short)channels);
            w.Write(sampleRate);
            w.Write(sampleRate * channels * 2);
            w.Write((short)(channels * 2));
            w.Write((short)16);
            w.Write((short)0);

            w.Write(dataSize);
            w.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(pcm.AsSpan()));
            w.Write(loopStart);
            w.Write(loopLength);
            w.Write(durationMs);
        }
        var bytes = ms.ToArray();
        BitConverter.TryWriteBytes(bytes.AsSpan(6), bytes.Length);
        return bytes;
    }

    // Exactly `count` interleaved samples: cut where NVorbis runs long, silence where it runs short.
    private static short[] DecodeExactly(VorbisReader reader, long count)
    {
        var pcm = new short[count];
        var buf = new float[reader.Channels * 4096];
        long at = 0;
        int read;
        while (at < count && (read = reader.ReadSamples(buf, 0, buf.Length)) > 0)
        {
            int take = (int)Math.Min(read, count - at);
            Convert(buf, take, pcm, at);
            at += take;
        }
        return pcm;
    }

    private static short[] DecodeAll(VorbisReader reader)
    {
        var pcm = new short[Math.Max(reader.TotalSamples, 0) * reader.Channels];
        var buf = new float[reader.Channels * 4096];
        long at = 0;
        int read;
        while ((read = reader.ReadSamples(buf, 0, buf.Length)) > 0)
        {
            if (at + read > pcm.Length) Array.Resize(ref pcm, (int)Math.Max(pcm.Length * 2L, at + read));
            Convert(buf, read, pcm, at);
            at += read;
        }
        if (at != pcm.Length) Array.Resize(ref pcm, (int)at);
        return pcm;
    }

    private static void Convert(float[] src, int count, short[] dst, long at)
    {
        for (int i = 0; i < count; i++)
        {
            int s = (int)MathF.Round(src[i] * 32767f);
            dst[at + i] = (short)Math.Clamp(s, short.MinValue, short.MaxValue);
        }
    }

    private static long TagLong(VorbisReader reader, string tag, long fallback)
    {
        var text = reader.Tags.GetTagSingle(tag);
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }
}
#endif
