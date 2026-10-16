using System.IO.Compression;
using Fade.MonoGame.Content;

// Brotli-compresses a built Content folder's XNBs, for a release that ships
// $(FadeMonoCompressContent). See CompressedContent in Fade.MonoGame.Contracts, and
// FadeContentManager in Fade.MonoGame.Game for the other half.
//
// Measured 2026-10-16 on the fish game: its 138 textures are raw RGBA and mostly transparent, and
// went from 16.3 MB of XNB to 0.34 MB.
//
// Like OggAudioEncoder, the built XNBs are only read, never touched, and a .br newer than its XNB
// is not compressed again. Writes <brDir>/compressed.txt: one line per XNB compressed, its path
// under Content/ with no extension, forward slashes. The build reads it to swap each of those
// XNBs for its .xnb.br.
public static class XnbCompressor
{
    public const string CompressedListName = "compressed.txt";

    /// <param name="skip">Names (as in the list) to leave alone: the sounds that ship as .ogg.</param>
    public static void CompressAll(string contentDir, string brDir, ISet<string> skip)
    {
        if (!Directory.Exists(contentDir))
            throw new DirectoryNotFoundException($"no built content to compress at {contentDir}");
        Directory.CreateDirectory(brDir);

        var compressed = new List<string>();
        int written = 0, kept = 0;
        long xnbBytes = 0, brBytes = 0;
        var sync = new object();

        Parallel.ForEach(Directory.GetFiles(contentDir, "*.xnb", SearchOption.AllDirectories), xnbPath =>
        {
            var rel     = Path.GetRelativePath(contentDir, xnbPath).Replace('\\', '/');
            var relName = rel.Substring(0, rel.Length - ".xnb".Length);
            if (skip.Contains(relName)) return;

            var brPath   = Path.Combine(brDir, relName + CompressedContent.Suffix);
            bool upToDate = File.Exists(brPath) &&
                            File.GetLastWriteTimeUtc(brPath) >= File.GetLastWriteTimeUtc(xnbPath);
            if (!upToDate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(brPath)!);
                var temp = brPath + ".tmp";
                using (var input = File.OpenRead(xnbPath))
                using (var output = File.Create(temp))
                using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize))
                    input.CopyTo(brotli);
                File.Move(temp, brPath, overwrite: true);
            }

            lock (sync)
            {
                compressed.Add(relName);
                if (upToDate) kept++; else written++;
                xnbBytes += new FileInfo(xnbPath).Length;
                brBytes  += new FileInfo(brPath).Length;
            }
        });

        compressed.Sort(StringComparer.Ordinal);
        File.WriteAllLines(Path.Combine(brDir, CompressedListName), compressed);
        Console.WriteLine(
            $"[fadecontent] brotli: {compressed.Count} XNBs ({written} compressed, {kept} up to date), " +
            $"{xnbBytes / 1048576.0:F1} MB -> {brBytes / 1048576.0:F2} MB");
    }
}
