using Fade.MonoGame.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Audio;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;

// var contentCollectionArgs = new ContentBuilderParams()
// {
//     Mode = ContentBuilderMode.Builder,
//     WorkingDirectory = $"{AppContext.BaseDirectory}../../../", // path to where your content folder can be located
//     SourceDirectory = "Assets", // Not actually needed as this is the default, but added for reference
//     Platform = TargetPlatform.DesktopGL
// };
// var builder = new Builder();
//
// if (args is not null && args.Length > 0)
// {
//     builder.Run(args);
// }
// else
// {
//     builder.Run(contentCollectionArgs);
// }
//
// return builder.FailedToBuild > 0 ? -1 : 0;

public static class FadeContentSystem
{
    public static void Build(string assetsFolder, ContentEntry[] entries, int entriesCount)
    {
        Build(assetsFolder, entries, entriesCount, null);
    }
    
    public static void Build(string assetsFolder, ContentEntry[] entries, int entriesCount, List<string> onlyPaths)
    {
        var contentCollectionArgs = new ContentBuilderParams()
        {
            Mode = ContentBuilderMode.Builder,
            WorkingDirectory =  AppContext.BaseDirectory,
            OutputDirectory = "",
            SourceDirectory = assetsFolder,
            Platform = TargetPlatform.DesktopGL,
            
        };

        var builder = new FadeContentBuilder(entries, entriesCount);
        builder.Run(contentCollectionArgs);
    }
    
    
}

public class FadeContentBuilder : ContentBuilder
{
    private int _entryCount;
    private ContentEntry[] _entries;

    public FadeContentBuilder(ContentEntry[] entries, int entryCount)
    {
        // _onlyPaths = onlyPaths;
        _entries = entries;
        _entryCount = entryCount;
    }
    
    public override IContentCollection GetContentCollection()
    {
        var contentCollection = new ContentCollection();

        // include everything in the folder
        
            contentCollection.Include<WildcardRule>("*");
            contentCollection.Include<WildcardRule>("*.mp3", contentProcessor: new SoundEffectProcessor());
        
        contentCollection.Exclude<WildcardRule>("*.ttf");
        
        // user based overrides
        for (var i = _entryCount; i >= 0; i--)
        {
            var entry = _entries[i];
            
            
            var processor = GetProcessor(ref entry);
            var importer = GetImporter(ref entry); 
            contentCollection.Include(entry.path, entry.name, importer, processor);
        }
        
        // By default, all content will be imported from the Assets folder using the default importer for their file type.
        // Please add any custom content collection rules here.

        return contentCollection;
    }

    public static FontDescriptionProcessor GetFontProcessor(ref ContentEntry entry)
    {
        var p = new FontDescriptionProcessor();
        
        if (entry.parameters.TryGetValue(nameof(p.TextureFormat), out var x) && Enum.TryParse(typeof(TextureProcessorOutputFormat), x, true, out var format))
        {
            p.TextureFormat = (TextureProcessorOutputFormat)format;
        }
        else
        {
            p.TextureFormat = TextureProcessorOutputFormat.NoChange;
        }
        if (entry.parameters.TryGetValue(nameof(p.PremultiplyAlpha), out x) && bool.TryParse(x, out var b))
        {
            p.PremultiplyAlpha = b;
        }
        else
        {
            p.PremultiplyAlpha = true;
        }
        
        return p;
    }
    
    public static SoundEffectProcessor GetSfxProcessor(ref ContentEntry entry)
    {
        var p = new SoundEffectProcessor();
        if (entry.parameters.TryGetValue(nameof(p.Quality), out var x) && Enum.TryParse(typeof(ConversionQuality), x, true, out var format))
        {
            p.Quality = (ConversionQuality)format;
        }
        return p;
    }

    public static TextureProcessor GetTextureProcessor(ref ContentEntry entry)
    {
        var p = new TextureProcessor();
        if (entry.parameters.TryGetValue(nameof(p.ColorKeyEnabled), out var x) && bool.TryParse(x, out var b))
        {
            p.ColorKeyEnabled = b;
        }
        if (entry.parameters.TryGetValue(nameof(p.GenerateMipmaps), out x) && bool.TryParse(x, out b))
        {
            p.GenerateMipmaps = b;
        }
        if (entry.parameters.TryGetValue(nameof(p.PremultiplyAlpha), out x) && bool.TryParse(x, out b))
        {
            p.PremultiplyAlpha = b;
        }
        if (entry.parameters.TryGetValue(nameof(p.ResizeToPowerOfTwo), out x) && bool.TryParse(x, out b))
        {
            p.ResizeToPowerOfTwo = b;
        }
        if (entry.parameters.TryGetValue(nameof(p.MakeSquare), out x) && bool.TryParse(x, out b))
        {
            p.MakeSquare = b;
        }
        
        if (entry.parameters.TryGetValue(nameof(p.TextureFormat), out x) && Enum.TryParse(typeof(TextureProcessorOutputFormat), x, true, out var format))
        {
            p.TextureFormat = (TextureProcessorOutputFormat)format;
        }
        if (entry.parameters.TryGetValue(nameof(p.ColorKeyColor), out x))
        {
            var components = x.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
            p.ColorKeyColor = new Color(components[0], components[1], components[2], components[3]);
        }
        
        return p;
    }

    public static IContentProcessor GetProcessor(ref ContentEntry entry, string ext)
    {
        switch (ext)
        {
            case ".mp3":
                return GetSfxProcessor(ref entry);
            case ".png":
                return GetTextureProcessor(ref entry);
            case ".fx":
                return new EffectProcessor();
            case ".spritefont":
                return GetFontProcessor(ref entry);
            default:
                throw new NotImplementedException("no auto processor for " + ext);
        }
    }
    
    public static IContentProcessor GetProcessor(ref ContentEntry entry)
    {
        switch (entry.processr)
        {
            case ContentProcessorType.Auto:
                return GetProcessor(ref entry, Path.GetExtension(entry.path));
            case ContentProcessorType.Effect:
                return new EffectProcessor();
            case ContentProcessorType.SoundEffect:
                return GetSfxProcessor(ref entry);
            case ContentProcessorType.SpriteFont:
                return GetFontProcessor(ref entry);
            case ContentProcessorType.Texture:
                return GetTextureProcessor(ref entry);
            default:
                throw new InvalidOperationException("no processor for " + entry.path);
        }
    }

    public static IContentImporter GetImporter(ref ContentEntry entry)
    {
        switch (entry.importer)
        {
            case ContentImporterType.Auto:
                return GetImporter(ref entry, Path.GetExtension(entry.path));
            case ContentImporterType.Effect:
                return new EffectImporter();
            case ContentImporterType.Mp3:
                return new Mp3Importer();
            case ContentImporterType.SpriteFont:
                return new FontDescriptionImporter();
            case ContentImporterType.Texture:
                return new TextureImporter();
            default:
                throw new InvalidOperationException("no importer for " + entry.path);
        }
    }

    public static IContentImporter GetImporter(ref ContentEntry entry, string ext)
    {
        switch (ext)
        {
            case ".fx":
                return new EffectImporter();
            case ".mp3":
                return new Mp3Importer();
            case ".png":
                return new TextureImporter();
            case ".spritefont":
                return new FontDescriptionImporter();
            default:
                throw new InvalidOperationException("no importer for ext " + ext);
        }
    }
}