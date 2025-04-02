using SixLabors.ImageSharp;

namespace TexTools.Commands;

public class CleanScanCommand
{
    public static void Run(string path)
    {
        // var path = "/Users/chrishanna/Downloads/Sleigh_d of hand/heart_jog1_raw.png";
        FrameHelper.CleanScanFile(path, Color.Red, new FrameHelper.PaddingOptions
        {
            left = 155, 
            // right = 205, // for jog image
            right = 160, // for attack?
            top = 155,
            bottom = 0
        });
    }
}