using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Fade.MonoGame.Content;     // ContentEntry, ContentParameterKeys, TextureCompression, IContentBuilder
using FadeBasic.Virtual;

namespace Fade.MonoGame.Core;


public static class ContentSystem
{

    public static FastStack<ContentEntry> entries = new FastStack<ContentEntry>(4);

    public static List<ContentEntry> contentEntries = new List<ContentEntry>();

    // Global default texture compression. Macro-time `default texture
    // compression` sets this so subsequent pushes inherit it without
    // having to repeat the value. The desktop content builder consumes
    // it inside GetTextureProcessor; the Playground reads the same
    // declarations via its TS-side macro scan.
    public static TextureCompression defaultTextureCompression = TextureCompression.Auto;

    // Same idea for sound assets — `default sound compression` macro
    // sets the inherited value. Today's playground encoder only emits
    // PCM regardless; the field is wired so the macro surface is in
    // place when the ADPCM encoder lands.
    public static SoundCompression defaultSoundCompression = SoundCompression.Auto;

    public static void Reset()
    {
        if (entries.buffer == null)
        {
            entries.buffer = new ContentEntry[32];
        }
        entries.ptr = 0; // clear the stack.
        defaultTextureCompression = TextureCompression.Auto;
        defaultSoundCompression = SoundCompression.Auto;
    }

    public static ref ContentEntry Push(string path)
    {
        // NOTE: do not call this concurrently. The Push() can invalidate old ref handles.
        var parameters = new Dictionary<string, string>();
        // Seed with the global defaults so subsequent `texture compression`
        // / `sound compression` macros don't have to repeat the value.
        // Stored as parameters so the entry stays a flat key/value bag —
        // the playground + desktop builder both look up via this dict.
        if (defaultTextureCompression != TextureCompression.Auto)
        {
            parameters[ContentParameterKeys.Compression] = CompressionToString(defaultTextureCompression);
        }
        if (defaultSoundCompression != SoundCompression.Auto)
        {
            parameters[ContentParameterKeys.SoundCompression] = SoundCompressionToString(defaultSoundCompression);
        }
        var entry = new ContentEntry
        {
            path = path,
            parameters = parameters,
            importer = ContentImporterType.Auto,
            processr = ContentProcessorType.Auto,
            name = path,
        };
        entries.Push(entry);
        return ref GetCurrent();
    }

    public static string CompressionToString(TextureCompression c) => c switch
    {
        TextureCompression.Auto     => "auto",
        TextureCompression.None     => "none",
        TextureCompression.Color    => "color",
        TextureCompression.Dxt1     => "dxt1",
        TextureCompression.Dxt3     => "dxt3",
        TextureCompression.Dxt5     => "dxt5",
        TextureCompression.Alpha8   => "alpha8",
        TextureCompression.Bgra4444 => "bgra4444",
        _ => "auto",
    };

    public static string SoundCompressionToString(SoundCompression c) => c switch
    {
        SoundCompression.Auto  => "auto",
        SoundCompression.Pcm   => "pcm",
        SoundCompression.Adpcm => "adpcm",
        _ => "auto",
    };

    public static ref ContentEntry GetCurrent()
    {
        return ref entries.buffer[entries.ptr - 1];
    }

    public static void AddEntry(string path)
    {
        contentEntries.Add(new ContentEntry
        {
            path = path
        });
    }
    
    // The content pipeline (FadeContentSystem) is desktop-only and heavy, so
    // .Game never references it directly. Instead the host registers an
    // IContentBuilder in Game.Services (the template does this for Debug desktop
    // builds); we resolve it once and build through it. Null on Release/Web →
    // a clean no-op, falling back to baked/published content.
    public static IContentBuilder ContentBuilder;

    public static void ResolveContentBuilder(IServiceProvider services)
    {
        ContentBuilder = services?.GetService(typeof(IContentBuilder)) as IContentBuilder;
    }

    public static void BuildContent()
    {
        if (ContentBuilder == null) return;
        var assets = GameReloader.GetRoot() + "/Assets";
        if (!Directory.Exists(assets)) return; // nothing to build (e.g. a minimal game with no assets)
        ContentBuilder.Build(assets, entries.buffer, entries.ptr - 1);
    }

    public static void BuildSomeContent(List<string> paths)
    {
        if (ContentBuilder == null) return;
        var assets = GameReloader.GetRoot() + "/Assets";
        if (!Directory.Exists(assets)) return;
        ContentBuilder.Build(assets, entries.buffer, entries.ptr - 1, paths);
    }
}