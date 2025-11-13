using System.Text.Json;
using Fade.MonoGame.Game;
using FadeBasic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TexTools;

public static class FrameHelper
{
    private static readonly PngEncoder _encoder = new PngEncoder
    {
        TransparentColorMode = PngTransparentColorMode.Clear,
        // ColorType = PngColorType.al
    };

    public struct PaddingOptions
    {
        public int left, right, top, bottom;
    }

    public static void SplitSprite(string filePath, Image<Rgba32> image)
    {
        // split the image into sub parts
        var rows = 4;
        var cols = 5;
        var cellWidth = image.Width / cols;
        var cellHeight = image.Height / rows;
        var n = 0;
        
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols; x++)
            {

                var xi = x;
                var yi = y;
                var clone = image.Clone<Rgba32>(ctx =>
                {
                    var topLeft = new Point(xi * cellWidth, yi * cellHeight);
                    ctx.Crop(new Rectangle(topLeft, new Size(cellWidth, cellHeight)));
                });
                clone.SaveAsPng(Path.ChangeExtension(filePath, $".cell-{n++}-{xi}-{yi}.png"), _encoder);

            }
        }
    }

    public static void CleanScanFile(string filePath, Color lineColor, PaddingOptions paddings)
    {
        using var image = Image.Load<Rgba32>(filePath);
        
        // apply basic transforms first
        image.Mutate(ctx => ctx
            // convert the image to transparent and line-color
            .BinaryThreshold(.9f, Color.Transparent, lineColor)
            
            // rotate the image from its default view to "right side up"
            .Rotate(RotateMode.Rotate270)
        );

        image.SaveAsPng(Path.ChangeExtension(filePath, ".intermediate.png"), _encoder);
        
        
        // correct the rotation/skew
        image.Mutate(ctx =>
        {
            ctx.Rotate(.1f); // TODO: expose as property
        });
        image.SaveAsPng(Path.ChangeExtension(filePath, ".corrected.png"), _encoder);

        
        // crop out the annotation sections
        image.Mutate(ctx =>
        {
            ctx.Crop(new Rectangle(paddings.left, paddings.top, image.Width - paddings.left - paddings.right, image.Height - paddings.top - paddings.bottom));
        });
        image.SaveAsPng(Path.ChangeExtension(filePath, ".cropped.png"), _encoder);
        
        // remove the grid
        // using var noGrid = RemoveGrid(image);
        // noGrid.SaveAsPng(Path.ChangeExtension(filePath, ".nogrid.png"), _encoder);

        var finalPath = Path.ChangeExtension(filePath, ".nodots.png");
        using var noDots = RemoveDots(image);
        noDots.SaveAsPng(finalPath, _encoder);

        // save out a json file
        {
            var rows = 4;
            var cols = 5;
            var cellWidth = image.Width / cols;
            var cellHeight = image.Height / rows;
            var descriptor = new TextureDescriptor
            {
                rows = rows, cols = cols, imageFilePath = finalPath, frames = new List<TextureFrame>()
            };
            var index = 0;
            for (var y = 0; y < descriptor.rows; y++)
            {
                for (var x = 0; x < descriptor.cols; x++)
                {
                    var frame = new TextureFrame
                    {
                        index = index++,
                        row = y, col = x,
                        xOffset = x * cellWidth, yOffset = y * cellHeight, xSize = cellWidth, ySize = cellHeight
                    };
                    descriptor.frames.Add(frame);
                }
            }

            var json = JsonSerializer.Serialize(descriptor, new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true
            });
            File.WriteAllText(Path.ChangeExtension(filePath, ".metadata.json"), json);
            
        }

    }

    static Image<Rgba32> RemoveGrid(Image<Rgba32> image)
    {
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        // TODO: _COULD_ get the 4 corners of the grid and use that to inform
        //  an affine transformation to get it into a perfect rectangle. 

        Point gridStart = default;
        // identify the start of the grid.
        { 
            // the first pixels on the left are known to be part of the grid... 
            var y = 300;
            var offset = image.Width * y;
            for (var x = 0; x < image.Width; x++)
            {
                var p = pixels[x + offset];

                if (p.R > 1 && p.A > 1)
                {
                    // found the first part of the grid- start flood fill from here. 
                    gridStart.X = x;
                    gridStart.Y = y;
                    break;
                }
            }
        }

        // flood fill from the gridStart
        {
            var toExplore = new Queue<Point>();
            toExplore.Enqueue(gridStart);

            var visited = new HashSet<Point>();
            var maxWidth = image.Width;
            var maxHeight = image.Height;

            while (toExplore.Count > 0)
            {
                var curr = toExplore.Dequeue();
                
                if (visited.Contains(curr))
                {
                    // skip! we have already been here
                    continue;
                }

                if (curr.X < 0 || curr.Y < 0 || curr.X >= maxWidth || curr.Y >= maxHeight)
                {
                    // skip! we are off the edge of the map
                    continue;
                }

                visited.Add(curr);

                var index = curr.X + maxWidth * curr.Y;
                var pixel = pixels[index];
                if (pixel.R*pixel.A <= 2) 
                {
                    // skip! we are below the threshold
                    continue;
                }
                
                // ah, modify the pixel!
                pixels[index].R = 0;
                pixels[index].G = 0;
                pixels[index].B = 0;
                pixels[index].A = 0;
                
                // look at neighbors!
                toExplore.Enqueue(new Point(curr.X, curr.Y - 1));
                toExplore.Enqueue(new Point(curr.X, curr.Y + 1));
                toExplore.Enqueue(new Point(curr.X - 1, curr.Y));
                toExplore.Enqueue(new Point(curr.X + 1, curr.Y));
            }
        }

        var result = Image.LoadPixelData<Rgba32>(pixels, image.Width, image.Height);
        return result;
    }

    static Image<Rgba32> RemoveDots(Image<Rgba32> image)
    {
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        // var allPoints = new int[pixels.Length];
        // { // create all possible points, because we'll need to look through all of them. 
        //     for (var i = 0; i < pixels.Length; i++)
        //     {
        //         // allPoints.Add(i);
        //         allPoints[i] = 1;
        //     }
        // }
        var toSearchNext = new Queue<int>();
        toSearchNext.Enqueue(0);
        var visited = new HashSet<Point>();

        // flood fill from the next available point
        // while (allPoints.Any(x => x > 0))
        while (toSearchNext.Count > 0)
        {
            var start = toSearchNext.Dequeue();
            // var start = allPoints.First(x => x > 0);
            
            var toExplore = new Queue<Point>();
            
            toExplore.Enqueue(new Point(start / image.Width, start % image.Width));

            var maxWidth = image.Width;
            var maxHeight = image.Height;
            var fill = new List<int>();

            while (toExplore.Count > 0)
            {
                var curr = toExplore.Dequeue();
                
                if (visited.Contains(curr))
                {
                    // skip! we have already been here
                    continue;
                }

                if (curr.X < 0 || curr.Y < 0 || curr.X >= maxWidth || curr.Y >= maxHeight)
                {
                    // skip! we are off the edge of the map
                    continue;
                }
                var index = curr.X + maxWidth * curr.Y;

                // allPoints[index] = 0;
                visited.Add(curr);

                var pixel = pixels[index];
                if (pixel.R * pixel.A <= 254)
                // if (pixel.R )
                {
                    // skip! we are below the threshold
                    //toSearchNext.Enqueue(index);
                    toSearchNext.Enqueue(curr.X-1 + (curr.Y * maxWidth));
                    toSearchNext.Enqueue(curr.X+1 + (curr.Y * maxWidth));
                    toSearchNext.Enqueue(curr.X + ((curr.Y+1) * maxWidth));
                    toSearchNext.Enqueue(curr.X + ((curr.Y-1) * maxWidth));
                    continue;
                }
                
                // ah, modify the pixel!
                fill.Add(index);
                // pixels[index].R = 0;
                // pixels[index].G = 0;
                // pixels[index].B = 0;
                // pixels[index].A = 0;
                
                // look at neighbors!
                toExplore.Enqueue(new Point(curr.X, curr.Y - 1));
                toExplore.Enqueue(new Point(curr.X, curr.Y + 1));
                toExplore.Enqueue(new Point(curr.X - 1, curr.Y));
                toExplore.Enqueue(new Point(curr.X + 1, curr.Y));
            }

            if (fill.Count > 0 && fill.Count < 400)
            {
                // a small dot!
                foreach (var index in fill)
                {
                    pixels[index].R = 0;
                    pixels[index].G = 0;
                    pixels[index].B = 0;
                    pixels[index].A = 0;
                }
            }
        }
        
        var result = Image.LoadPixelData<Rgba32>(pixels, image.Width, image.Height);
        return result;
    }

}