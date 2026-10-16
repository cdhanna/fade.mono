using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Fade.MonoGame.Content;

// Re-encodes a built Content folder's sound XNBs as Ogg Vorbis, for a release build that ships
// $(FadeMonoEncodeAudioToOgg). See FadeContentManager in Fade.MonoGame.Game for the other half,
// which decodes them back into the same XNBs when the game starts.
//
// Why from the XNB and not from the source mp3: the XNB is exactly what a dev build plays --
// same sample rate, same length, same loop points -- so the .ogg is one lossy step away from
// what the author heard, not two.
//
// The built XNBs are only read, never touched: the content builder's own incremental cache keeps
// working, and turning the flag off again ships them as before. The .ogg files go to their own
// folder, one per quality, and an .ogg newer than its XNB is not encoded again.
//
// Writes <oggDir>/converted.txt: one line per sound converted, its path under Content/ with no
// extension, forward slashes. The build reads it to swap each of those XNBs for its .ogg.
// Also writes <oggDir>/sounds.manifest, which ships: each of those names with a hash of its .ogg,
// which the game caches the decoded sound under (see FadeContentManager).
public static class OggAudioEncoder
{
    public const string ConvertedListName = "converted.txt";

    /// <returns>The sounds converted, as in converted.txt.</returns>
    public static List<string> EncodeAll(string contentDir, string oggDir, double quality)
    {
        if (!Directory.Exists(contentDir))
            throw new DirectoryNotFoundException($"no built content to encode at {contentDir}");

        Directory.CreateDirectory(oggDir);
        var tempDir = Path.Combine(oggDir, "tmp");
        Directory.CreateDirectory(tempDir);

        var q = quality.ToString(CultureInfo.InvariantCulture);
        var converted = new List<string>();
        var manifest  = new List<string>();
        int encodedCount = 0, keptCount = 0;
        long xnbBytes = 0, oggBytes = 0;
        var sync = new object();

        var xnbs = Directory.GetFiles(contentDir, "*.xnb", SearchOption.AllDirectories);
        Parallel.ForEach(xnbs, xnbPath =>
        {
            var bytes = File.ReadAllBytes(xnbPath);
            if (!TryReadSoundEffect(bytes, out var sound)) return;

            var rel     = Path.GetRelativePath(contentDir, xnbPath).Replace('\\', '/');
            var relName = rel.Substring(0, rel.Length - ".xnb".Length);
            var oggPath = Path.Combine(oggDir, relName + ".ogg");

            bool upToDate = File.Exists(oggPath) &&
                            File.GetLastWriteTimeUtc(oggPath) >= File.GetLastWriteTimeUtc(xnbPath);
            if (!upToDate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(oggPath)!);
                var wavPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".wav");
                try
                {
                    WriteWav(wavPath, bytes, sound);
                    var args =
                        $"-y -v error -i \"{wavPath}\" -map_metadata -1 -c:a libvorbis -q:a {q} " +
                        $"-metadata {OggAudioTags.Samples}={sound.Samples} " +
                        $"-metadata {OggAudioTags.LoopStart}={sound.LoopStart} " +
                        $"-metadata {OggAudioTags.LoopLength}={sound.LoopLength} " +
                        $"-metadata {OggAudioTags.DurationMs}={sound.DurationMs} " +
                        $"-metadata {OggAudioTags.XnbPlatform}={(char)bytes[3]} " +
                        $"\"{oggPath}\"";
                    var exit = MonoGame.Tool.FFmpeg.Run(args, out _, out var stderr);
                    if (exit != 0 || !File.Exists(oggPath))
                        throw new InvalidOperationException($"ffmpeg could not encode {rel} (exit {exit}): {stderr}");
                }
                finally
                {
                    File.Delete(wavPath);
                }
            }

            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(oggPath)), 0, 16).ToLowerInvariant();
            lock (sync)
            {
                converted.Add(relName);
                manifest.Add(relName + "\t" + hash);
                if (upToDate) keptCount++; else encodedCount++;
                xnbBytes += bytes.Length;
                oggBytes += new FileInfo(oggPath).Length;
            }
        });

        try { Directory.Delete(tempDir, recursive: true); } catch { /* leftovers are harmless */ }

        converted.Sort(StringComparer.Ordinal);
        File.WriteAllLines(Path.Combine(oggDir, ConvertedListName), converted);
        manifest.Sort(StringComparer.Ordinal);
        File.WriteAllLines(Path.Combine(oggDir, OggAudioTags.ManifestFileName), manifest);
        Console.WriteLine(
            $"[fadecontent] ogg q{q}: {converted.Count} sounds ({encodedCount} encoded, {keptCount} up to date), " +
            $"{xnbBytes / 1048576.0:F1} MB of XNB -> {oggBytes / 1048576.0:F1} MB of Ogg");
        return converted;
    }

    private struct SoundEffectXnb
    {
        public int Channels, SampleRate, DataOffset, DataSize;
        public int Samples, LoopStart, LoopLength, DurationMs;
    }

    // A SoundEffect XNB holding 16-bit PCM, which is what the pipeline makes from an mp3 at the
    // default quality. Anything else (ADPCM from Quality=Medium, other content) is left as an XNB.
    private static bool TryReadSoundEffect(byte[] bytes, out SoundEffectXnb sound)
    {
        sound = default;
        if (!FadeContentSystem.TryReadXnbObjectStart(bytes, out var reader, out var offset)) return false;
        if (!reader.Contains("SoundEffectReader")) return false;

        using var br = new BinaryReader(new MemoryStream(bytes, writable: false));
        br.BaseStream.Position = offset;
        int formatSize = br.ReadInt32();
        if (formatSize < 16) return false;
        long formatStart = br.BaseStream.Position;
        ushort formatTag  = br.ReadUInt16();
        sound.Channels    = br.ReadInt16();
        sound.SampleRate  = br.ReadInt32();
        br.ReadInt32();                       // average bytes per second
        br.ReadInt16();                       // block align
        short bits = br.ReadInt16();
        if (formatTag != 1 || bits != 16) return false;
        br.BaseStream.Position = formatStart + formatSize;

        sound.DataSize   = br.ReadInt32();
        sound.DataOffset = (int)br.BaseStream.Position;
        br.BaseStream.Position += sound.DataSize;
        sound.LoopStart  = br.ReadInt32();
        sound.LoopLength = br.ReadInt32();
        sound.DurationMs = br.ReadInt32();
        sound.Samples    = sound.DataSize / (2 * sound.Channels);
        return true;
    }

    private static void WriteWav(string path, byte[] xnb, SoundEffectXnb sound)
    {
        using var w = new BinaryWriter(File.Create(path));
        int blockAlign = 2 * sound.Channels;
        w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + sound.DataSize);
        w.Write(Encoding.ASCII.GetBytes("WAVE"));
        w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16);
        w.Write((short)1); w.Write((short)sound.Channels); w.Write(sound.SampleRate);
        w.Write(sound.SampleRate * blockAlign); w.Write((short)blockAlign); w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data")); w.Write(sound.DataSize);
        w.Write(xnb, sound.DataOffset, sound.DataSize);
    }
}
