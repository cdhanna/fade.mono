// Texture2D → base64 PNG conversion for the inspector image previews.
//
// Used by Texture / RenderOutput / Sprite providers to surface a small
// thumbnail in the Tweakpane panel. Downsamples textures larger than
// MaxDim on each side so we don't ship 4K bitmaps over JS interop
// every poll.
//
// Two paths:
//   1. Texture is a regular Texture2D (loaded from an XNB): GetData
//      pulls pixel data via the existing readback path.
//   2. Texture is a RenderTarget2D (game-rendered): same GetData call,
//      KNI handles the format conversion.
// In both cases failures are swallowed — the panel just shows no
// preview instead of crashing.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core.Debug;

internal static class TexturePreview
{
    // Cap previews at this many pixels per side. The panel scales via
    // CSS but smaller payloads mean less JS interop overhead. Powers
    // of two work cleanly with our nearest-neighbor downsample loop.
    const int MaxDim = 128;

    /// <summary>
    /// Try to read back the texture, downsample it, encode as PNG, and
    /// return a "data:image/png;base64,..." URL. Returns null on any
    /// failure (texture not loaded, readback unsupported, encode bug —
    /// all best-effort).
    /// </summary>
    public static string TryEncode(Texture2D tex)
    {
        if (tex == null) return null;
        try
        {
            // Determine the target preview size (preserve aspect ratio).
            var srcW = tex.Width;
            var srcH = tex.Height;
            if (srcW <= 0 || srcH <= 0) return null;

            var scale = 1;
            while (srcW > MaxDim * scale || srcH > MaxDim * scale) scale *= 2;
            var dstW = Math.Max(1, srcW / scale);
            var dstH = Math.Max(1, srcH / scale);

            // Read the whole texture, then nearest-neighbor downsample
            // into a smaller buffer. (Bicubic would be nicer but the
            // ~3x quality bump isn't worth the code in a debug preview.)
            var src = new Color[srcW * srcH];
            tex.GetData(src);

            var dstRgba = new byte[dstW * dstH * 4];
            for (var y = 0; y < dstH; y++)
            {
                var sy = y * scale;
                for (var x = 0; x < dstW; x++)
                {
                    var sx = x * scale;
                    var c = src[sy * srcW + sx];
                    var i = (y * dstW + x) * 4;
                    dstRgba[i + 0] = c.R;
                    dstRgba[i + 1] = c.G;
                    dstRgba[i + 2] = c.B;
                    dstRgba[i + 3] = c.A;
                }
            }

            var png = PngEncoder.Encode(dstW, dstH, dstRgba);
            return "data:image/png;base64," + Convert.ToBase64String(png);
        }
        catch (Exception e)
        {
            // GetData throws for some target formats and on platforms
            // where it isn't supported. Don't spam the console — just
            // skip the preview.
            Console.Error.WriteLine("[debug] TexturePreview failed: " + e.Message);
            return null;
        }
    }
}
