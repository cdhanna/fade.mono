// The FadeGameIcon build task: one square PNG in, the two icon files a desktop game needs out.
//
//   Icon.ico   the icon of the .exe itself, in Explorer, shortcuts and the Start menu.
//              Sizes 16 to 256, so Windows never has to scale one.
//   Icon.bmp   the icon of the window, set by MonoGame when the window opens. MonoGame
//              (DesktopGL and DesktopVK both) looks for exactly this resource name and hands
//              it to SDL, which only reads BMP.
//   Icon.icns  the icon of a macOS .app bundle, in Finder and the Dock. A bundle's icon is a
//              file in Contents/Resources named by Info.plist, not anything inside the
//              program, so build/make-macos-app.sh moves it there. Sizes 16 to 1024 (512@2x).
//
// Compiled by MSBuild's RoslynCodeTaskFactory from FadeBasic.MonoGame.App.targets, so it has
// no dependencies: the PNG decoder and both writers are here. It runs inside MSBuild, which is
// .NET Framework under Visual Studio and .NET under `dotnet build`, so it sticks to what both
// have (no ZLibStream, no spans).
//
// Scaling is chosen for pixel art, which is what most Fade games are drawn in. Making the
// picture BIGGER scales it by a whole number with no smoothing, and centres it, so every
// source pixel stays a sharp square. Making it SMALLER averages, which is what a painted
// icon wants. A 32x32 picture is therefore exact at 32, 64, 128 and 256.
//
// This file is NOT compiled into Fade.MonoGame.Game (see the Compile Remove there).

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class FadeGameIcon : Task
{
    [Required] public string Source { get; set; }
    [Required] public string IcoFile { get; set; }
    [Required] public string BmpFile { get; set; }
    public string IcnsFile { get; set; }
    public int BmpSize { get; set; } = 256;

    static readonly int[] IcoSizes = { 16, 24, 32, 48, 64, 128, 256 };

    public override bool Execute()
    {
        try
        {
            if (!File.Exists(Source))
            {
                Log.LogError($"FadeGameIcon: '{Source}' does not exist. $(FadeGameIcon) must name a PNG file, relative to the project.");
                return false;
            }

            int width, height;
            byte[] rgba = DecodePng(File.ReadAllBytes(Source), out width, out height);
            if (width != height)
            {
                Log.LogWarning($"FadeGameIcon: '{Source}' is {width}x{height}, not square. It is centred in a square icon with clear space round it.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(IcoFile)));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(BmpFile)));

            var images = new List<byte[]>();
            foreach (var size in IcoSizes)
            {
                var pixels = Resize(rgba, width, height, size);
                // The 256 entry is stored as a PNG, which every Windows since Vista reads; as a
                // bitmap it alone would be 256 KB in the exe. The small ones stay bitmaps, which
                // is what the shell reads fastest.
                images.Add(size == 256 ? EncodePng(pixels, size) : IcoDib(pixels, size));
            }
            File.WriteAllBytes(IcoFile, Ico(images));
            File.WriteAllBytes(BmpFile, Bmp(Resize(rgba, width, height, BmpSize), BmpSize));
            if (!string.IsNullOrEmpty(IcnsFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(IcnsFile)));
                File.WriteAllBytes(IcnsFile, Icns(rgba, width, height));
            }
            return true;
        }
        catch (Exception e)
        {
            Log.LogError($"FadeGameIcon: could not make the icon from '{Source}': {e.Message}");
            return false;
        }
    }

    // ── Scaling ──────────────────────────────────────────────────────────────

    // To a size x size RGBA square. See the note at the top for why up and down differ.
    static byte[] Resize(byte[] src, int w, int h, int size)
    {
        var dst = new byte[size * size * 4];
        int longest = Math.Max(w, h);

        if (size >= longest)
        {
            int k = size / longest;
            int ox = (size - w * k) / 2, oy = (size - h * k) / 2;
            for (int y = 0; y < h * k; y++)
            for (int x = 0; x < w * k; x++)
                Buffer.BlockCopy(src, ((y / k) * w + x / k) * 4, dst, ((oy + y) * size + ox + x) * 4, 4);
            return dst;
        }

        // Down: each output pixel is the average of the source pixels under it, weighted by
        // alpha so a transparent edge does not drag the colours towards black.
        double scale = (double)longest / size;
        int dw = (int)Math.Round(w / scale), dh = (int)Math.Round(h / scale);
        int dx0 = (size - dw) / 2, dy0 = (size - dh) / 2;
        for (int y = 0; y < dh; y++)
        for (int x = 0; x < dw; x++)
        {
            double sx0 = x * scale, sx1 = Math.Min(w, (x + 1) * scale);
            double sy0 = y * scale, sy1 = Math.Min(h, (y + 1) * scale);
            double r = 0, g = 0, b = 0, a = 0, area = 0;
            for (int sy = (int)sy0; sy < sy1; sy++)
            {
                double fy = Math.Min(sy + 1, sy1) - Math.Max(sy, sy0);
                for (int sx = (int)sx0; sx < sx1; sx++)
                {
                    double f = fy * (Math.Min(sx + 1, sx1) - Math.Max(sx, sx0));
                    int i = (sy * w + sx) * 4;
                    double pa = src[i + 3] / 255.0 * f;
                    r += src[i] * pa; g += src[i + 1] * pa; b += src[i + 2] * pa;
                    a += pa; area += f;
                }
            }
            int o = ((dy0 + y) * size + dx0 + x) * 4;
            if (a > 0)
            {
                dst[o] = ToByte(r / a); dst[o + 1] = ToByte(g / a); dst[o + 2] = ToByte(b / a);
            }
            dst[o + 3] = ToByte(a / area * 255);
        }
        return dst;
    }

    static byte ToByte(double v) => (byte)Math.Max(0, Math.Min(255, Math.Round(v)));

    // ── Writers ──────────────────────────────────────────────────────────────

    static byte[] Ico(List<byte[]> images)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)images.Count);
            int offset = 6 + 16 * images.Count;
            for (int i = 0; i < images.Count; i++)
            {
                int size = IcoSizes[i];
                bw.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
                bw.Write((byte)(size >= 256 ? 0 : size));
                bw.Write((byte)0); bw.Write((byte)0);       // no palette, reserved
                bw.Write((ushort)1); bw.Write((ushort)32);  // planes, bits per pixel
                bw.Write(images[i].Length);
                bw.Write(offset);
                offset += images[i].Length;
            }
            foreach (var image in images) bw.Write(image);
            return ms.ToArray();
        }
    }

    // The PNG-based icns entries, which every macOS since 10.7 reads (the bundle asks for 11).
    // Each is (type, pixel size); the @2x ones are the Retina versions of the size below them.
    static readonly string[] IcnsTypes = { "ic11", "ic12", "ic07", "ic13", "ic08", "ic14", "ic09", "ic10" };
    static readonly int[] IcnsSizes =    {     32,     64,    128,    256,    256,    512,    512,   1024 };
    //                                     16@2x   32@2x          128@2x          256@2x          512@2x

    // An icns file: 'icns' and the total length, then each entry as its type, its length (the
    // 8 bytes of this header included) and a PNG. All lengths are big-endian.
    static byte[] Icns(byte[] rgba, int width, int height)
    {
        var pngs = new Dictionary<int, byte[]>();
        using (var ms = new MemoryStream())
        {
            ms.Write(new byte[8], 0, 8);
            for (int e = 0; e < IcnsTypes.Length; e++)
            {
                int size = IcnsSizes[e];
                byte[] png;
                if (!pngs.TryGetValue(size, out png))
                {
                    png = EncodePng(Resize(rgba, width, height, size), size);
                    pngs[size] = png;
                }
                var head = new byte[8];
                for (int i = 0; i < 4; i++) head[i] = (byte)IcnsTypes[e][i];
                PutBE(head, 4, (uint)(png.Length + 8));
                ms.Write(head, 0, 8);
                ms.Write(png, 0, png.Length);
            }
            var file = ms.ToArray();
            file[0] = (byte)'i'; file[1] = (byte)'c'; file[2] = (byte)'n'; file[3] = (byte)'s';
            PutBE(file, 4, (uint)file.Length);
            return file;
        }
    }

    // An icon entry as a bitmap: a BITMAPINFOHEADER with twice the height (the colour rows, then
    // the 1-bit AND mask, which alpha makes redundant and is left all zero).
    static byte[] IcoDib(byte[] rgba, int size)
    {
        int maskStride = ((size + 31) / 32) * 4;
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40); bw.Write(size); bw.Write(size * 2);
            bw.Write((ushort)1); bw.Write((ushort)32);
            bw.Write(0); bw.Write(size * size * 4 + maskStride * size);
            bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
            WriteBgraBottomUp(bw, rgba, size);
            bw.Write(new byte[maskStride * size]);
            return ms.ToArray();
        }
    }

    // The window icon. A BITMAPV4HEADER with explicit masks, alpha included, so SDL keeps the
    // transparency: a plain 32-bit BMP is read as opaque by some loaders.
    static byte[] Bmp(byte[] rgba, int size)
    {
        const int headerSize = 14 + 108;
        int dataSize = size * size * 4;
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)'B'); bw.Write((byte)'M');
            bw.Write(headerSize + dataSize); bw.Write(0); bw.Write(headerSize);

            bw.Write(108); bw.Write(size); bw.Write(size);
            bw.Write((ushort)1); bw.Write((ushort)32);
            bw.Write(3);                                  // BI_BITFIELDS
            bw.Write(dataSize);
            bw.Write(2835); bw.Write(2835);               // 72 dpi
            bw.Write(0); bw.Write(0);
            bw.Write(0x00FF0000); bw.Write(0x0000FF00); bw.Write(0x000000FF); bw.Write(unchecked((int)0xFF000000));
            bw.Write(0x73524742);                         // 'sRGB'
            bw.Write(new byte[36 + 12]);                  // endpoints and gamma, unused for sRGB
            WriteBgraBottomUp(bw, rgba, size);
            return ms.ToArray();
        }
    }

    static void WriteBgraBottomUp(BinaryWriter bw, byte[] rgba, int size)
    {
        for (int y = size - 1; y >= 0; y--)
        for (int x = 0; x < size; x++)
        {
            int i = (y * size + x) * 4;
            bw.Write(rgba[i + 2]); bw.Write(rgba[i + 1]); bw.Write(rgba[i]); bw.Write(rgba[i + 3]);
        }
    }

    static byte[] EncodePng(byte[] rgba, int size)
    {
        var raw = new byte[(size * 4 + 1) * size];
        for (int y = 0; y < size; y++)
            Buffer.BlockCopy(rgba, y * size * 4, raw, y * (size * 4 + 1) + 1, size * 4); // filter 0

        byte[] deflated;
        using (var ms = new MemoryStream())
        {
            using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, true)) ds.Write(raw, 0, raw.Length);
            deflated = ms.ToArray();
        }
        var zlib = new byte[deflated.Length + 6];
        zlib[0] = 0x78; zlib[1] = 0x9C;
        Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
        uint adler = Adler32(raw);
        zlib[zlib.Length - 4] = (byte)(adler >> 24); zlib[zlib.Length - 3] = (byte)(adler >> 16);
        zlib[zlib.Length - 2] = (byte)(adler >> 8); zlib[zlib.Length - 1] = (byte)adler;

        using (var ms = new MemoryStream())
        {
            ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var ihdr = new byte[13];
            PutBE(ihdr, 0, (uint)size); PutBE(ihdr, 4, (uint)size);
            ihdr[8] = 8; ihdr[9] = 6; // 8-bit RGBA
            WriteChunk(ms, "IHDR", ihdr);
            WriteChunk(ms, "IDAT", zlib);
            WriteChunk(ms, "IEND", new byte[0]);
            return ms.ToArray();
        }
    }

    static void WriteChunk(Stream s, string type, byte[] data)
    {
        var head = new byte[8];
        PutBE(head, 0, (uint)data.Length);
        for (int i = 0; i < 4; i++) head[4 + i] = (byte)type[i];
        s.Write(head, 0, 8);
        s.Write(data, 0, data.Length);
        uint crc = Crc32(head, 4, 4, 0xFFFFFFFF);
        crc = Crc32(data, 0, data.Length, crc) ^ 0xFFFFFFFF;
        var tail = new byte[4];
        PutBE(tail, 0, crc);
        s.Write(tail, 0, 4);
    }

    static void PutBE(byte[] b, int at, uint v)
    {
        b[at] = (byte)(v >> 24); b[at + 1] = (byte)(v >> 16); b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v;
    }

    static uint[] _crcTable;
    static uint Crc32(byte[] data, int start, int count, uint crc)
    {
        if (_crcTable == null)
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                t[n] = c;
            }
            _crcTable = t;
        }
        for (int i = start; i < start + count; i++) crc = _crcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var d in data) { a = (a + d) % 65521; b = (b + a) % 65521; }
        return (b << 16) | a;
    }

    // ── PNG decoder ──────────────────────────────────────────────────────────
    // Every colour type and bit depth a PNG can have, except interlaced (Adam7) files, which
    // are rare for icons and get a clear error instead. 16-bit channels keep their high byte.

    static byte[] DecodePng(byte[] file, out int width, out int height)
    {
        byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
        for (int i = 0; i < 8; i++)
            if (file.Length < 8 || file[i] != sig[i]) throw new InvalidDataException("it is not a PNG file");

        width = height = 0;
        int depth = 0, colorType = 0, interlace = 0;
        byte[] palette = null, trns = null;
        var idat = new MemoryStream();

        int pos = 8;
        while (pos + 8 <= file.Length)
        {
            int len = (file[pos] << 24) | (file[pos + 1] << 16) | (file[pos + 2] << 8) | file[pos + 3];
            string type = System.Text.Encoding.ASCII.GetString(file, pos + 4, 4);
            int data = pos + 8;
            if (type == "IHDR")
            {
                width = (file[data] << 24) | (file[data + 1] << 16) | (file[data + 2] << 8) | file[data + 3];
                height = (file[data + 4] << 24) | (file[data + 5] << 16) | (file[data + 6] << 8) | file[data + 7];
                depth = file[data + 8]; colorType = file[data + 9]; interlace = file[data + 12];
            }
            else if (type == "PLTE") { palette = new byte[len]; Buffer.BlockCopy(file, data, palette, 0, len); }
            else if (type == "tRNS") { trns = new byte[len]; Buffer.BlockCopy(file, data, trns, 0, len); }
            else if (type == "IDAT") idat.Write(file, data, len);
            else if (type == "IEND") break;
            pos = data + len + 4;
        }

        if (width <= 0 || height <= 0) throw new InvalidDataException("the PNG has no image header");
        if (interlace != 0) throw new InvalidDataException("the PNG is interlaced. Save it again with interlacing off");

        int channels;
        switch (colorType)
        {
            case 0: channels = 1; break; // grey
            case 2: channels = 3; break; // RGB
            case 3: channels = 1; break; // palette
            case 4: channels = 2; break; // grey + alpha
            case 6: channels = 4; break; // RGBA
            default: throw new InvalidDataException("unknown PNG colour type " + colorType);
        }
        if (colorType == 3 && palette == null) throw new InvalidDataException("the PNG has a palette colour type but no palette");

        int bitsPerPixel = channels * depth;
        int bpp = Math.Max(1, bitsPerPixel / 8);    // filter distance in bytes
        int stride = (width * bitsPerPixel + 7) / 8;

        // The zlib stream, minus its 2-byte header (DeflateStream wants raw deflate).
        var raw = new byte[(stride + 1) * height];
        idat.Position = 2;
        using (var ds = new DeflateStream(idat, CompressionMode.Decompress))
        {
            int read = 0;
            while (read < raw.Length)
            {
                int n = ds.Read(raw, read, raw.Length - read);
                if (n <= 0) throw new InvalidDataException("the PNG image data is cut short");
                read += n;
            }
        }

        var cur = new byte[stride];
        var prev = new byte[stride];
        var rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            int filter = raw[y * (stride + 1)];
            Buffer.BlockCopy(raw, y * (stride + 1) + 1, cur, 0, stride);
            for (int i = 0; i < stride; i++)
            {
                int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                int add;
                switch (filter)
                {
                    case 0: add = 0; break;
                    case 1: add = a; break;
                    case 2: add = b; break;
                    case 3: add = (a + b) / 2; break;
                    case 4:
                        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                        add = pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                        break;
                    default: throw new InvalidDataException("bad PNG row filter " + filter);
                }
                cur[i] = (byte)(cur[i] + add);
            }

            for (int x = 0; x < width; x++)
            {
                int o = (y * width + x) * 4;
                byte r, g, bl, al = 255;
                switch (colorType)
                {
                    case 0:
                    {
                        int v = Sample(cur, x, depth);
                        r = g = bl = Expand(v, depth);
                        if (trns != null && trns.Length >= 2 && v == ((trns[0] << 8) | trns[1])) al = 0;
                        break;
                    }
                    case 3:
                    {
                        int v = Sample(cur, x, depth);
                        r = palette[v * 3]; g = palette[v * 3 + 1]; bl = palette[v * 3 + 2];
                        if (trns != null && v < trns.Length) al = trns[v];
                        break;
                    }
                    case 2:
                    {
                        int s = depth / 8, i = x * 3 * s;
                        r = cur[i]; g = cur[i + s]; bl = cur[i + 2 * s];
                        if (trns != null && trns.Length >= 6 && depth == 8 &&
                            r == trns[1] && g == trns[3] && bl == trns[5]) al = 0;
                        break;
                    }
                    case 4:
                    {
                        int s = depth / 8, i = x * 2 * s;
                        r = g = bl = cur[i]; al = cur[i + s];
                        break;
                    }
                    default:
                    {
                        int s = depth / 8, i = x * 4 * s;
                        r = cur[i]; g = cur[i + s]; bl = cur[i + 2 * s]; al = cur[i + 3 * s];
                        break;
                    }
                }
                rgba[o] = r; rgba[o + 1] = g; rgba[o + 2] = bl; rgba[o + 3] = al;
            }

            var swap = prev; prev = cur; cur = swap;
        }
        return rgba;
    }

    // One grey or palette sample of `depth` bits (1, 2, 4, 8 or 16) from a row.
    static int Sample(byte[] row, int x, int depth)
    {
        if (depth == 8) return row[x];
        if (depth == 16) return (row[x * 2] << 8) | row[x * 2 + 1];
        int perByte = 8 / depth;
        int shift = 8 - depth * (x % perByte + 1);
        return (row[x / perByte] >> shift) & ((1 << depth) - 1);
    }

    static byte Expand(int v, int depth)
    {
        if (depth == 16) return (byte)(v >> 8);
        return (byte)(v * 255 / ((1 << depth) - 1));
    }
}
