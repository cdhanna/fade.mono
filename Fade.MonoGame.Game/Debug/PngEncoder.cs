// Tiny PNG encoder for Texture2D previews in the debug inspector.
//
// Avoiding Texture2D.SaveAsPng for two reasons:
//   1. Its presence varies between MonoGame.Framework.DesktopGL and
//      nkast.Kni.Platform.Blazor.GL — we want one preview path that
//      compiles and runs everywhere.
//   2. SaveAsPng goes through StbImageWriteSharp on KNI which pulls
//      in another native-shaped dependency we'd rather not introduce.
//
// The output is a valid 8-bit RGBA PNG with one IDAT chunk. No
// interlacing, no filtering (filter byte = 0 per scanline). Zlib
// compression via System.IO.Compression.ZLibStream — present in
// .NET 6+ which both Desktop and Browser TFMs target.

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Fade.MonoGame.Core.Debug;

internal static class PngEncoder
{
    static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// Encode width×height RGBA pixels (row-major, 4 bytes per pixel)
    /// to a PNG byte array.
    /// </summary>
    public static byte[] Encode(int width, int height, byte[] rgba)
    {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        var expected = checked(width * height * 4);
        if (rgba.Length != expected)
            throw new ArgumentException($"rgba length {rgba.Length} != width*height*4 {expected}", nameof(rgba));

        using var ms = new MemoryStream();
        ms.Write(Signature, 0, Signature.Length);
        WriteChunk(ms, "IHDR", BuildIhdr(width, height));
        WriteChunk(ms, "IDAT", BuildIdat(width, height, rgba));
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    static byte[] BuildIhdr(int w, int h)
    {
        var buf = new byte[13];
        WriteBE32(buf, 0, (uint)w);
        WriteBE32(buf, 4, (uint)h);
        buf[8]  = 8;  // bit depth
        buf[9]  = 6;  // color type 6 = RGBA
        buf[10] = 0;  // compression method 0 = deflate
        buf[11] = 0;  // filter method 0 = adaptive
        buf[12] = 0;  // interlace 0 = none
        return buf;
    }

    static byte[] BuildIdat(int w, int h, byte[] rgba)
    {
        // Prefix each scanline with a filter type byte (0 = None).
        var scanlineBytes = w * 4;
        var filtered = new byte[(scanlineBytes + 1) * h];
        for (var y = 0; y < h; y++)
        {
            filtered[y * (scanlineBytes + 1)] = 0;
            Buffer.BlockCopy(rgba, y * scanlineBytes, filtered, y * (scanlineBytes + 1) + 1, scanlineBytes);
        }
        using var ms = new MemoryStream();
        // ZLibStream writes the 2-byte zlib header + DEFLATE body +
        // 4-byte Adler-32 checksum — exactly what PNG IDAT expects.
        using (var zlib = new ZLibStream(ms, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(filtered, 0, filtered.Length);
        }
        return ms.ToArray();
    }

    static void WriteChunk(Stream s, string type, byte[] data)
    {
        var lenBuf = new byte[4];
        WriteBE32(lenBuf, 0, (uint)data.Length);
        s.Write(lenBuf, 0, 4);

        var typeBuf = Encoding.ASCII.GetBytes(type);
        s.Write(typeBuf, 0, typeBuf.Length);

        if (data.Length > 0) s.Write(data, 0, data.Length);

        // CRC32 over (type + data).
        var crcInput = new byte[typeBuf.Length + data.Length];
        Buffer.BlockCopy(typeBuf, 0, crcInput, 0, typeBuf.Length);
        if (data.Length > 0) Buffer.BlockCopy(data, 0, crcInput, typeBuf.Length, data.Length);
        var crc = Crc32(crcInput);
        var crcBuf = new byte[4];
        WriteBE32(crcBuf, 0, crc);
        s.Write(crcBuf, 0, 4);
    }

    // Stateless CRC32 (IEEE polynomial). PNG specifies CRC over each
    // chunk's type+data. ~5 lines, no NuGet, no allocations beyond
    // the lookup table built on first call.
    static uint[] _crcTable;
    static uint Crc32(byte[] data)
    {
        if (_crcTable == null)
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                t[n] = c;
            }
            _crcTable = t;
        }
        var crc = 0xFFFFFFFFu;
        for (var i = 0; i < data.Length; i++)
            crc = _crcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    static void WriteBE32(byte[] buf, int offset, uint value)
    {
        buf[offset]     = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }
}
