using System.Collections.Generic;
using System.Diagnostics;
using Fade.MonoGame.Content;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Core;


public static class ContentSystem
{

    public static FastStack<ContentEntry> entries = new FastStack<ContentEntry>();
    
    public static List<ContentEntry> contentEntries = new List<ContentEntry>();

    public static void Reset()
    {
        if (entries.buffer == null)
        {
            entries.buffer = new ContentEntry[32];
        }
        entries.ptr = 0; // clear the stack.
    }

    public static ref ContentEntry Push(string path)
    {
        // NOTE: do not call this concurrently. The Push() can invalidate old ref handles. 
        var entry = new ContentEntry
        {
            path = path,
            parameters = new Dictionary<string, string>(),
            importer = ContentImporterType.Auto,
            processr = ContentProcessorType.Auto,
            name = path
        };
        entries.Push(entry);
        return ref GetCurrent();
    }

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
    
    [Conditional("DEBUG")]
    public static void BuildContent()
    {
        FadeContentSystem.Build(GameReloader.GetRoot() + "/Assets", entries.buffer, entries.ptr - 1);
    }

    [Conditional("DEBUG")]
    public static void BuildSomeContent(List<string> paths)
    {
        FadeContentSystem.Build(GameReloader.GetRoot() + "/Assets", entries.buffer, entries.ptr - 1, paths);
        
    }
}