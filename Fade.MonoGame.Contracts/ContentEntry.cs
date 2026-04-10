namespace Fade.MonoGame.Content;


public struct ContentEntry
{
    public string path;
    public ContentProcessorType processr;
    public ContentImporterType importer;
    public Dictionary<string, string> parameters;
    public string name;
}

public enum ContentProcessorType
{
    Auto,
    Texture, 
    Effect, 
    SoundEffect,
    SpriteFont,
}

public enum ContentImporterType
{
    Auto,
    Texture,
    Effect,
    Mp3,
    SpriteFont
}
