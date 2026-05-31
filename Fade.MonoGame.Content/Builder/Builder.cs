using Fade.MonoGame.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Audio;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;

public static class FadeContentSystem
{
    // ── Platform-aware build (used by CLI and export pipeline) ───────────────

    // Builds all assets in assetsFolder. Always emits DesktopGL XNBs via the
    // MonoGame content pipeline. When platform="Web", the output XNBs are
    // post-processed in place for KNI BlazorGL (sound loopLength + MGFX v11→v10).
    public static void Build(string assetsFolder, string platform, string outputDir = "", string intermediateDir = "")
    {
        var contentBuilderParams = new ContentBuilderParams
        {
            Mode             = ContentBuilderMode.Builder,
            WorkingDirectory = AppContext.BaseDirectory,
            OutputDirectory  = outputDir,
            SourceDirectory  = assetsFolder,
            Platform         = TargetPlatform.DesktopGL,
        };

        var builder = new FadeContentBuilder(Array.Empty<ContentEntry>(), -1);
        builder.Run(contentBuilderParams);

        if (string.Equals(platform, "Web", StringComparison.OrdinalIgnoreCase))
            PatchXnbsForKni(string.IsNullOrEmpty(outputDir) ? AppContext.BaseDirectory : outputDir);
    }

    // ── Debug live-reload build (used by Game project in Debug/Desktop) ──────

    public static void Build(string assetsFolder, ContentEntry[] entries, int entriesCount)
    {
        Build(assetsFolder, entries, entriesCount, null);
    }

    public static void Build(string assetsFolder, ContentEntry[] entries, int entriesCount, List<string>? onlyPaths)
    {
        var contentBuilderParams = new ContentBuilderParams
        {
            Mode             = ContentBuilderMode.Builder,
            WorkingDirectory = AppContext.BaseDirectory,
            OutputDirectory  = "",
            SourceDirectory  = assetsFolder,
            Platform         = TargetPlatform.DesktopGL,
        };

        var builder = new FadeContentBuilder(entries, entriesCount);
        builder.Run(contentBuilderParams);
    }

    // ── KNI XNB patchers ──────────────────────────────────────────────────────
    //
    // We build XNBs with MonoGame's DesktopGL pipeline and adapt the output for
    // KNI's BlazorGL runtime in place. Two fixes are needed:
    //   • SoundEffect — KNI misreads loopLength (too small); rewrite it to the
    //     true frame count (dataSize / bytesPerFrame).
    //   • Effect (.fx) — desktop MGCB emits MGFX version 11; KNI 4.2.9001 caps at
    //     v10 and throws on anything higher. The v10→v11 bump only added two
    //     per-shader strings (SourceFile, Entrypoint), so splicing those out and
    //     stamping the version byte to 10 yields a valid v10 blob.
    // Both patchers are idempotent and skip anything they don't recognize.

    private const byte MgfxVersionKni = 10;

    public static void PatchXnbsForKni(string outputDir)
    {
        if (!Directory.Exists(outputDir)) return;
        foreach (var path in Directory.GetFiles(outputDir, "*.xnb", SearchOption.TopDirectoryOnly))
        {
            var original = File.ReadAllBytes(path);
            var patched  = PatchXnbForKni(original);
            if (!ReferenceEquals(patched, original))
                File.WriteAllBytes(path, patched);
        }
    }

    public static byte[] PatchXnbForKni(byte[] bytes)
    {
        if (!TryReadXnbObjectStart(bytes, out var rootReader, out var objectDataOffset))
            return bytes;
        if (rootReader.Contains("SoundEffectReader"))
            return PatchSoundEffectXnb(bytes, objectDataOffset);
        if (rootReader.Contains("EffectReader"))
            return PatchEffectMgfxVersion(bytes, objectDataOffset);
        return bytes;
    }

    // Parses the XNB header + type-reader manifest, returning the root reader
    // name and the byte offset where the primary object's data begins.
    private static bool TryReadXnbObjectStart(byte[] bytes, out string rootReader, out int objectDataOffset)
    {
        rootReader = "";
        objectDataOffset = -1;
        if (bytes.Length < 10) return false;
        if (bytes[0] != 'X' || bytes[1] != 'N' || bytes[2] != 'B') return false;
        if ((bytes[5] & 0xC0) != 0) return false; // compressed — skip

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true);
            ms.Seek(10, SeekOrigin.Begin);  // past the fixed XNB header

            int readerCount = Read7BitInt(br);
            if (readerCount <= 0) return false;
            rootReader = Read7BitString(br);
            br.ReadInt32();                 // root reader version
            for (int i = 1; i < readerCount; i++) { Read7BitString(br); br.ReadInt32(); }
            Read7BitInt(br);                // sharedResourceCount
            Read7BitInt(br);                // rootObjectTypeId
            objectDataOffset = (int)ms.Position;
            return true;
        }
        catch { return false; }
    }

    private static byte[] PatchSoundEffectXnb(byte[] bytes, int objectDataOffset)
    {
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true);
            ms.Seek(objectDataOffset, SeekOrigin.Begin);

            // WAVE format header (WAVEFORMATEX layout)
            int headerSize = br.ReadInt32();
            if (headerSize < 16) return bytes;
            br.ReadUInt16();                          // wFormatTag       (offset 0)
            short channels      = br.ReadInt16();     // nChannels        (offset 2)
            br.ReadUInt32();                          // nSamplesPerSec   (offset 4)
            br.ReadUInt32();                          // nAvgBytesPerSec  (offset 8)
            br.ReadInt16();                           // nBlockAlign      (offset 12)
            short bitsPerSample = br.ReadInt16();     // wBitsPerSample   (offset 14)
            if (headerSize > 16) ms.Seek(headerSize - 16, SeekOrigin.Current);

            int dataSize = br.ReadInt32();
            ms.Seek(dataSize, SeekOrigin.Current);    // skip audio data
            ms.Seek(4, SeekOrigin.Current);            // loopStart

            long loopLengthPos = ms.Position;
            int  loopLength    = br.ReadInt32();

            int bytesPerFrame = (bitsPerSample / 8) * channels;
            if (bytesPerFrame <= 0) return bytes;
            int totalFrames = dataSize / bytesPerFrame;
            if (loopLength >= totalFrames) return bytes;  // already correct

            var patched = (byte[])bytes.Clone();
            WriteInt32LE(patched, (int)loopLengthPos, totalFrames);
            return patched;
        }
        catch { return bytes; }
    }

    // Splices the v11-only (SourceFile, Entrypoint) strings out of every shader
    // record and stamps the MGFX version byte to 10. Ported from the Playground's
    // patchEffectMgfxVersionForKni (src/xnb/xnb-previews.ts).
    private static byte[] PatchEffectMgfxVersion(byte[] bytes, int od)
    {
        // objectData: int32 dataSize | 'MGFX' | byte version | byte profile |
        //             int32 effectKey | MGFX body…
        if (bytes.Length < od + 14) return bytes;
        if (bytes[od + 4] != (byte)'M' || bytes[od + 5] != (byte)'G' ||
            bytes[od + 6] != (byte)'F' || bytes[od + 7] != (byte)'X') return bytes;
        byte version = bytes[od + 8];
        if (version == MgfxVersionKni) return bytes;   // already v10
        if (version != 11) return bytes;               // only v11 → v10 implemented

        var removeRanges = new List<(int start, int end)>(); // objectData-relative
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true);
            ms.Seek(od + 14, SeekOrigin.Begin);        // MGFX body

            int cbufferCount = br.ReadInt32();
            for (int c = 0; c < cbufferCount; c++)
            {
                Read7BitString(br);                    // name
                br.ReadUInt16();                       // sizeInBytes (int16)
                int paramCount = br.ReadInt32();
                for (int p = 0; p < paramCount; p++) { br.ReadInt32(); br.ReadUInt16(); }
            }

            int shaderCount = br.ReadInt32();
            for (int s = 0; s < shaderCount; s++)
            {
                br.ReadByte();                         // isVertexShader
                int removeStart = (int)(ms.Position - od);
                Read7BitString(br);                    // SourceFile (v11)
                Read7BitString(br);                    // Entrypoint (v11)
                removeRanges.Add((removeStart, (int)(ms.Position - od)));

                int shaderLength = br.ReadInt32();
                ms.Seek(shaderLength, SeekOrigin.Current);  // bytecode

                int samplerCount = br.ReadByte();
                for (int i = 0; i < samplerCount; i++)
                {
                    br.ReadByte(); br.ReadByte(); br.ReadByte();
                    if (br.ReadByte() != 0) ms.Seek(20, SeekOrigin.Current); // sampler state
                    Read7BitString(br);                // name
                    br.ReadByte();                     // parameter index
                }

                int cbufRefCount = br.ReadByte();
                ms.Seek(cbufRefCount, SeekOrigin.Current);

                int attrCount = br.ReadByte();
                for (int i = 0; i < attrCount; i++)
                {
                    Read7BitString(br);                // name
                    br.ReadByte();                     // usage
                    br.ReadByte();                     // index
                    br.ReadUInt16();                   // location (int16)
                }
            }
        }
        catch { return bytes; }

        int totalRemoved = 0;
        foreach (var (a, b) in removeRanges) totalRemoved += b - a;

        var outBuf = new byte[bytes.Length - totalRemoved];
        int inOff = 0, outOff = 0;
        foreach (var (relStart, relEnd) in removeRanges)
        {
            int absStart = od + relStart, absEnd = od + relEnd;
            int span = absStart - inOff;
            Array.Copy(bytes, inOff, outBuf, outOff, span);
            outOff += span;
            inOff = absEnd;
        }
        Array.Copy(bytes, inOff, outBuf, outOff, bytes.Length - inOff);

        outBuf[od + 8] = MgfxVersionKni;                                  // version byte
        WriteInt32LE(outBuf, od, ReadInt32LE(bytes, od) - totalRemoved);  // EffectReader dataSize
        WriteInt32LE(outBuf, 6,  ReadInt32LE(bytes, 6)  - totalRemoved);  // XNB header fileSize
        return outBuf;
    }

    private static int Read7BitInt(BinaryReader br)
    {
        int value = 0, shift = 0;
        for (int i = 0; i < 5; i++)
        {
            byte b = br.ReadByte();
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return value;
            shift += 7;
        }
        throw new InvalidDataException("Malformed 7-bit encoded int in XNB");
    }

    private static string Read7BitString(BinaryReader br)
    {
        int len = Read7BitInt(br);
        return System.Text.Encoding.UTF8.GetString(br.ReadBytes(len));
    }

    private static void WriteInt32LE(byte[] buf, int offset, int value)
    {
        buf[offset]     = (byte) value;
        buf[offset + 1] = (byte)(value >>  8);
        buf[offset + 2] = (byte)(value >> 16);
        buf[offset + 3] = (byte)(value >> 24);
    }

    private static int ReadInt32LE(byte[] buf, int offset)
        => buf[offset] | (buf[offset + 1] << 8) | (buf[offset + 2] << 16) | (buf[offset + 3] << 24);
}

public class FadeContentBuilder : ContentBuilder
{
    private readonly int            _entryCount;
    private readonly ContentEntry[] _entries;

    public FadeContentBuilder(ContentEntry[] entries, int entryCount)
    {
        _entries    = entries;
        _entryCount = entryCount;
    }

    public override IContentCollection GetContentCollection()
    {
        var contentCollection = new ContentCollection();

        contentCollection.Include<WildcardRule>("*");
        contentCollection.Include<WildcardRule>("*.mp3", contentProcessor: new SoundEffectProcessor());
        contentCollection.Exclude<WildcardRule>("*.ttf");

        // Per-entry overrides supplied by the caller (e.g. live-reload from Game).
        for (var i = _entryCount; i >= 0; i--)
        {
            var entry     = _entries[i];
            var processor = GetProcessor(ref entry);
            var importer  = GetImporter(ref entry);
            contentCollection.Include(entry.path, entry.name, importer, processor);
        }

        return contentCollection;
    }

    public static FontDescriptionProcessor GetFontProcessor(ref ContentEntry entry)
    {
        var p = new FontDescriptionProcessor();
        if (entry.parameters.TryGetValue(nameof(p.TextureFormat), out var x) &&
            Enum.TryParse(typeof(TextureProcessorOutputFormat), x, true, out var format))
            p.TextureFormat = (TextureProcessorOutputFormat)format;
        else
            p.TextureFormat = TextureProcessorOutputFormat.NoChange;

        if (entry.parameters.TryGetValue(nameof(p.PremultiplyAlpha), out x) && bool.TryParse(x, out var b))
            p.PremultiplyAlpha = b;
        else
            p.PremultiplyAlpha = true;

        return p;
    }

    public static SoundEffectProcessor GetSfxProcessor(ref ContentEntry entry)
    {
        var p = new SoundEffectProcessor();
        if (entry.parameters.TryGetValue(nameof(p.Quality), out var x) &&
            Enum.TryParse(typeof(ConversionQuality), x, true, out var format))
            p.Quality = (ConversionQuality)format;
        return p;
    }

    public static TextureProcessor GetTextureProcessor(ref ContentEntry entry)
    {
        var p = new TextureProcessor();
        if (entry.parameters.TryGetValue(nameof(p.ColorKeyEnabled),    out var x) && bool.TryParse(x, out var b)) p.ColorKeyEnabled    = b;
        if (entry.parameters.TryGetValue(nameof(p.GenerateMipmaps),    out x)     && bool.TryParse(x, out b))     p.GenerateMipmaps    = b;
        if (entry.parameters.TryGetValue(nameof(p.PremultiplyAlpha),   out x)     && bool.TryParse(x, out b))     p.PremultiplyAlpha   = b;
        if (entry.parameters.TryGetValue(nameof(p.ResizeToPowerOfTwo), out x)     && bool.TryParse(x, out b))     p.ResizeToPowerOfTwo = b;
        if (entry.parameters.TryGetValue(nameof(p.MakeSquare),         out x)     && bool.TryParse(x, out b))     p.MakeSquare         = b;

        // Fade-level "Compression" parameter (set by the `texture
        // compression` / `default texture compression` macros). Mapped to
        // MGCB's TextureProcessorOutputFormat — we collapse Dxt{1,3,5}
        // into DxtCompressed because the MGCB enum doesn't distinguish
        // them; the playground encoder honours the finer choice on its
        // side. The legacy TextureFormat parameter below still wins if
        // the user supplies one, so older content scripts keep working.
        if (entry.parameters.TryGetValue(ContentParameterKeys.Compression, out var compStr))
        {
            switch ((compStr ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "none":
                case "color":
                // Alpha8 / Bgra4444 have no direct equivalent in MGCB's
                // TextureProcessorOutputFormat. The playground encoder
                // emits them natively; the desktop builder falls back to
                // Color so the asset still loads (just bigger on disk).
                case "alpha8":
                case "bgra4444": p.TextureFormat = TextureProcessorOutputFormat.Color;         break;
                case "dxt1":
                case "dxt3":
                case "dxt5":     p.TextureFormat = TextureProcessorOutputFormat.DxtCompressed; break;
                case "auto":     p.TextureFormat = TextureProcessorOutputFormat.NoChange;      break;
            }
        }

        if (entry.parameters.TryGetValue(nameof(p.TextureFormat), out x) &&
            Enum.TryParse(typeof(TextureProcessorOutputFormat), x, true, out var format))
            p.TextureFormat = (TextureProcessorOutputFormat)format;

        if (entry.parameters.TryGetValue(nameof(p.ColorKeyColor), out x))
        {
            var parts = x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Select(int.Parse).ToArray();
            p.ColorKeyColor = new Color(parts[0], parts[1], parts[2], parts[3]);
        }
        return p;
    }

    public static IContentProcessor GetProcessor(ref ContentEntry entry, string ext)
    {
        return ext switch
        {
            ".mp3"        => GetSfxProcessor(ref entry),
            ".png"        => GetTextureProcessor(ref entry),
            ".fx"         => new EffectProcessor(),
            ".spritefont" => GetFontProcessor(ref entry),
            _             => throw new NotSupportedException("no auto processor for " + ext),
        };
    }

    public static IContentProcessor GetProcessor(ref ContentEntry entry)
    {
        return entry.processr switch
        {
            ContentProcessorType.Auto       => GetProcessor(ref entry, Path.GetExtension(entry.path)),
            ContentProcessorType.Effect     => new EffectProcessor(),
            ContentProcessorType.SoundEffect => GetSfxProcessor(ref entry),
            ContentProcessorType.SpriteFont => GetFontProcessor(ref entry),
            ContentProcessorType.Texture    => GetTextureProcessor(ref entry),
            _                               => throw new InvalidOperationException("no processor for " + entry.path),
        };
    }

    public static IContentImporter GetImporter(ref ContentEntry entry, string ext)
    {
        return ext switch
        {
            ".fx"         => new EffectImporter(),
            ".mp3"        => new Mp3Importer(),
            ".png"        => new TextureImporter(),
            ".spritefont" => new FontDescriptionImporter(),
            _             => throw new NotSupportedException("no importer for ext " + ext),
        };
    }

    public static IContentImporter GetImporter(ref ContentEntry entry)
    {
        return entry.importer switch
        {
            ContentImporterType.Auto       => GetImporter(ref entry, Path.GetExtension(entry.path)),
            ContentImporterType.Effect     => new EffectImporter(),
            ContentImporterType.Mp3        => new Mp3Importer(),
            ContentImporterType.SpriteFont => new FontDescriptionImporter(),
            ContentImporterType.Texture    => new TextureImporter(),
            _                              => throw new InvalidOperationException("no importer for " + entry.path),
        };
    }
}
