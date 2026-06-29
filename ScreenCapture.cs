using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenshotFaster;

/// <summary>A capture region in physical screen pixels.</summary>
public readonly record struct RegionPx(int X, int Y, int Width, int Height);

/// <summary>Grabs a region of the screen to a PNG file using GDI.</summary>
public static class ScreenCapture
{
    /// <summary>Capture <paramref name="region"/> and save it as PNG. Returns the file path.</summary>
    public static string SavePng(RegionPx region, string outPath)
    {
        using var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height),
                CopyPixelOperation.SourceCopy);
        }
        bmp.Save(outPath, ImageFormat.Png);
        return outPath;
    }
}
