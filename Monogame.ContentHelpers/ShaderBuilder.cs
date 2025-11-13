using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;

namespace Monogame.ContentHelpers;

public struct ContentBuilderConfig
{
    public string MgPlatform;
    public string MgcbFilePath;
    public string GameOutDir;
}

public class ShaderBuilder : IDisposable
{
    private readonly ContentBuilderConfig _config;
    private string? _rootFolder;
    private FileSystemWatcher _fileWatcher;

    private Dictionary<string, Timer> _pathToTimers = new Dictionary<string, Timer>();


    public ShaderBuilder(ContentBuilderConfig config)
    {
        _config = config;

        _rootFolder = Path.GetDirectoryName(_config.MgcbFilePath);

        _fileWatcher = new FileSystemWatcher
        {
            Path = _rootFolder,
            Filter = "*.fx",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        _fileWatcher.Changed += FileChanged;
    }

    private void FileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_pathToTimers)
        {
            if (_pathToTimers.TryGetValue(e.FullPath, out var timer))
            {
                timer?.Dispose();
            }

            _pathToTimers[e.FullPath] = new Timer(_ =>
            {

                var mgcb = MgcbUtil.ScanMgcbForEffects(_config);
                var match = mgcb.effects.FirstOrDefault(fx =>
                    fx.fullPath.Equals(e.FullPath, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(match.mgcbArgs))
                {
                    Console.WriteLine($"ignoring path=[{e.FullPath}] because it was not found in the original given .mgcb file");
                    return;
                }
        
                // invoke mgcb with the args
                if (MgcbUtil.InvokeMgcb(_config, mgcb, match))
                {
                    // time to copy the resulting .xnb file to the game's folder...
                    var xnbAsset = Path.ChangeExtension(match.assetName, ".xnb");
                    var outputPath = Path.Combine(mgcb.outputDir, xnbAsset);
                    if (!File.Exists(outputPath))
                    {
                        Console.Error.WriteLine($"couldn't copy file, because xnb did not exist=[{outputPath}]");
                        return;
                    }

                    var targetPath = Path.Combine(_config.GameOutDir, "Content", xnbAsset);
            
                    File.Copy(outputPath, targetPath, true);
                    Console.WriteLine($"Copied to [{targetPath}]");
                }
            }, null, 300, Timeout.Infinite);
        }
        
    }


    public void Dispose()
    {
        _fileWatcher.Dispose();
    }
}