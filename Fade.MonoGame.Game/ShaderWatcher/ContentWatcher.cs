using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.Xna.Framework.Content.Pipeline.Extra;

public struct WatchedAsset<T>
{
    public T Asset;
    public DateTimeOffset UpdatedAt;
    public string assetName;
}

public class ContentWatcher
{
    private readonly ContentManager _manager;
    private FileSystemWatcher _fw;

    private HashSet<string> changedAssetNames;

    private Dictionary<string, DateTimeOffset> assetFullPathToUpdatedAt = new Dictionary<string, DateTimeOffset>();
    private Dictionary<string, Timer> pathToTimer = new Dictionary<string, Timer>();
    public ContentWatcher(ContentManager manager)
    {
        _manager = manager;
    }
    
    public void Init()
    {
        if (_fw != null) throw new InvalidOperationException("already init");

        _fw = new FileSystemWatcher()
        {
            Path = _manager.RootDirectoryFullPath,
            Filter = "*.xnb",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _fw.Changed += OnFileChanged;
        _fw.Created += OnFileCreated;
        
        
        _fw.EnableRaisingEvents = true;
        _fw.Error += (sender, args) =>
        {

        };
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        OnFileChanged(sender, e);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (assetFullPathToUpdatedAt)
        {
            if (pathToTimer.TryGetValue(e.FullPath, out var timer))
            {
                timer?.Dispose();
            }

            pathToTimer[e.FullPath] = new Timer(_ =>
            {
                assetFullPathToUpdatedAt[e.FullPath.Replace("\\", "/")] = DateTimeOffset.Now;
            }, null, 300, Timeout.Infinite);
        }
    }

    public bool TryRefreshAsset<T>(ref WatchedAsset<T> current)
    {
        var assetPath = (Path.Combine(_manager.RootDirectoryFullPath, current.assetName) + ".xnb").Replace("\\", "/");
        
        lock (assetFullPathToUpdatedAt)
        {
            if (assetFullPathToUpdatedAt.TryGetValue(assetPath, out var existing))
            {
                if (existing > current.UpdatedAt)
                {
                    _manager.UnloadAsset(current.assetName);
                    current.Asset = _manager.Load<T>(current.assetName);
                    current.UpdatedAt = existing;
                    return true;
                }
            }
            else
            {
                throw new InvalidOperationException($"Cannot refresh asset that was not watched. {current.assetName}");
            }
        }

        return false;

    }

    public WatchedAsset<T> Watch<T>(string assetName)
    {
        lock (assetFullPathToUpdatedAt)
        {
            var now = DateTimeOffset.Now;
            var assetPath = (Path.Combine(_manager.RootDirectoryFullPath, assetName) + ".xnb").Replace("\\", "/");
           
            assetFullPathToUpdatedAt[assetPath] = now;
            var asset = _manager.Load<T>(assetName);
            return new WatchedAsset<T>
            {
                Asset = asset,
                UpdatedAt = now,
                assetName = assetName
            };
        }
    }

    public static string ConvertFullPathToAssetName(string fullPath)
    {
        return "";
    }
}
