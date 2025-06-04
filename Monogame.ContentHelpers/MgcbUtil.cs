using System.Diagnostics;
using System.Text;

namespace Monogame.ContentHelpers;

public struct MgcbAsset
{
    public string mgcbArgs;
    public string fullPath;
    public string assetName;
}

public class MgcbFile
{
    public string dir;
    public string outputDir;
    public string globalPropertiesArgs;
    public List<MgcbAsset> effects = new List<MgcbAsset>();
}

public static class MgcbUtil
{

    public static bool TryGetProjectOutDir(string csProjPath, out string outDir)
    {
        //dotnet msbuild .\Fade.MonoGame.csproj -getProperty:OutDir
        outDir = null;
        var p = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"msbuild {csProjPath} -getProperty:OutDir",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }
        };
        if (!p.Start())
        {
            throw new Exception("Unable to start mgcb");
        }
        p.WaitForExit();

        var stdOut = p.StandardOutput.ReadToEnd();
        
        if (p.ExitCode != 0)
        {
            Console.Error.WriteLine($"failed to get outdir stderr=[{p.StandardError.ReadToEnd()}]");
            return false;
        }
        
        outDir = stdOut.Trim();
        outDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csProjPath), outDir));
        return true;
    }
    
    public static bool InvokeMgcb(ContentBuilderConfig config, MgcbFile mgcb, MgcbAsset asset)
    {
        var workingDir = Path.GetDirectoryName(config.MgcbFilePath);
        var args = $"mgcb /workingDir:{workingDir} {mgcb.globalPropertiesArgs} {asset.mgcbArgs}";
        var p = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = workingDir,
                FileName = "dotnet",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }
        };
        if (!p.Start())
        {
            throw new Exception("Unable to start mgcb");
        }
        p.WaitForExit();

        var stdOut = p.StandardOutput.ReadToEnd();
        Console.WriteLine(stdOut);
        if (p.ExitCode != 0)
        {
            Console.Error.WriteLine($"mgcb failed on asset=[{asset.assetName}], stderr=[{p.StandardError.ReadToEnd()}]");
            return false;
        }

        return true;
    }

    public static MgcbFile ScanMgcbForEffects(ContentBuilderConfig config)
    {
        var file = new MgcbFile();
        var mgcbContent = File.ReadAllText(config.MgcbFilePath);
        var mgcbFolder = Path.GetFullPath(Path.GetDirectoryName(config.MgcbFilePath));

        file.dir = mgcbFolder;
        var lines = mgcbContent.Split(Environment.NewLine);

        var startLine = -1;

        var sb = new StringBuilder();
        
        for (var i = 0 ; i < lines.Length; i ++)
        {
            var line = lines[i];
            
           
            
            // the /importer line is the start of a shader configuration
            if (line.StartsWith("/importer:EffectImporter", StringComparison.InvariantCultureIgnoreCase))
            {
                startLine = i;
            }

            if (line.StartsWith("/outputDir:", StringComparison.InvariantCultureIgnoreCase))
            {
                file.outputDir = line.Substring("/outputDir:".Length).Replace("$(Platform)", config.MgPlatform);
                file.outputDir = Path.Combine(mgcbFolder, file.outputDir);
            }

            
            if (line.StartsWith("#") && startLine > -1)
            {
                var props = sb.ToString();
                sb.Clear();

                file.globalPropertiesArgs = props;
                startLine = -1;
            }
            
            if (startLine > -1)
            {
                // include all the args
                sb.Append(line);
                sb.Append(" ");
            }
            
            // handle global properties...
            if (line.StartsWith("#") && line.Contains("Global Properties", StringComparison.InvariantCultureIgnoreCase))
            {
                startLine = i;
            }



            // the /build line is the end of a configuration
            if (line.StartsWith("/build:") && startLine > -1)
            {
                startLine = -1;
                var assetInfo = line.Substring("/build:".Length);

                var assetParts = assetInfo.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var filePath = assetParts[0];
                var assetName = filePath;
                if (assetParts.Length > 1)
                {
                    assetName = assetParts[1];
                }

                file.effects.Add(new MgcbAsset
                {
                    fullPath = Path.GetFullPath(Path.Combine(mgcbFolder, filePath)),
                    assetName = assetName,
                    mgcbArgs = sb.ToString()
                });
                sb.Clear();
            }
        }

        file.globalPropertiesArgs = file.globalPropertiesArgs.Replace("$(Platform)", config.MgPlatform);
        return file;
    }
}