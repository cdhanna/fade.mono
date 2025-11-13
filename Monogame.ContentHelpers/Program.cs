// See https://aka.ms/new-console-template for more information

using Monogame.ContentHelpers;

Console.WriteLine("Hello, World!");

string mgcbFilePath = null;
string csProjPath = null;
string mgPlatform = "DesktopGL";
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--mgcb":
            mgcbFilePath = GetNext(args, i, "--mgcb");
            break;
        case "--csproj":
            csProjPath = GetNext(args, i, "--csproj");
            break;
        case "--platform":
            mgPlatform = GetNext(args, i, "--platform");
            break;
    }
}

if (string.IsNullOrEmpty(mgcbFilePath)) throw new Exception("must pass --mgcb");
if (string.IsNullOrEmpty(csProjPath)) throw new Exception("must pass --csproj");

Console.WriteLine($"parsed... mgcb=[{mgcbFilePath}] platform=[{mgPlatform}] csproj=[{csProjPath}]");

var config = new ContentBuilderConfig
{
    MgcbFilePath = mgcbFilePath,
    MgPlatform = mgPlatform,
};
if (!MgcbUtil.TryGetProjectOutDir(csProjPath, out config.GameOutDir))
{
    throw new Exception("Could not identify outdir of project");
}

using var cb = new ShaderBuilder(config);
var fx = MgcbUtil.ScanMgcbForEffects(config);

Console.WriteLine("Press anykey to exit.");
Console.ReadLine();

static string GetNext(string[] args, int argIndex, string argName)
{
    var valIndex = argIndex + 1;
    if (valIndex >= args.Length)
    {
        throw new Exception($"provided {argName} but did not pass enough arguments");
    }

    var val = args[valIndex];
    if (val.StartsWith("--"))
    {
        throw new Exception($"provided {argName} but did not pass a value");
    }

    return val;
}