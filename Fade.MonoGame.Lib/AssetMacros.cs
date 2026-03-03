using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameMacros
{
    public struct BundleElement
    {
        public string path;
    }
    
    
    [FadeBasicCommand("bundle", FadeBasicCommandUsage.Macro)]
    public void AddToBundle(string path)
    {
        
    }

    /// <summary>
    /// Run MGCB with the configured bundles
    /// </summary>
    [FadeBasicCommand("build bundles", FadeBasicCommandUsage.Macro)]
    public void BuildAssets()
    {
        // build
    }
}