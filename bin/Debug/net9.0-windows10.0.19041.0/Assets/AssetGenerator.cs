using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LyricIsland.Assets;

public static class AssetGenerator
{
    private static readonly string AssetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Characters");

    public static void EnsureDefaultCharactersExist()
    {
        try
        {
            if (!Directory.Exists(AssetsFolder))
            {
                Directory.CreateDirectory(AssetsFolder);
            }

            GenerateCatGifs();
            GenerateBunnyGifs();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to generate default character assets: {ex.Message}");
        }
    }

    private static void GenerateCatGifs()
    {
        var idlePath = Path.Combine(AssetsFolder, "cat_idle.gif");
        var playingPath = Path.Combine(AssetsFolder, "cat_playing.gif");
        var pausedPath = Path.Combine(AssetsFolder, "cat_paused.gif");

        if (!File.Exists(idlePath))
        {
            CreateAnimatedGif(idlePath, 48, 48, 120, [
                dc => DrawCat(dc, 0, false, false),
                dc => DrawCat(dc, 0, true, false),
                dc => DrawCat(dc, 0, false, false),
                dc => DrawCat(dc, 0, false, false),
            ]);
        }

        if (!File.Exists(playingPath))
        {
            CreateAnimatedGif(playingPath, 48, 48, 80, [
                dc => DrawCat(dc, -2, false, true, 0),
                dc => DrawCat(dc, 0, false, true, 1),
                dc => DrawCat(dc, 2, false, true, 2),
                dc => DrawCat(dc, 0, false, true, 1),
            ]);
        }

        if (!File.Exists(pausedPath))
        {
            CreateAnimatedGif(pausedPath, 48, 48, 200, [
                dc => DrawCat(dc, 1, false, false, sleepFrame: 0),
                dc => DrawCat(dc, 1, false, false, sleepFrame: 1),
            ]);
        }
    }

    private static void GenerateBunnyGifs()
    {
        var idlePath = Path.Combine(AssetsFolder, "bunny_idle.gif");
        var playingPath = Path.Combine(AssetsFolder, "bunny_playing.gif");
        var pausedPath = Path.Combine(AssetsFolder, "bunny_paused.gif");

        if (!File.Exists(idlePath))
        {
            CreateAnimatedGif(idlePath, 48, 48, 120, [
                dc => DrawBunny(dc, 0, false, false),
                dc => DrawBunny(dc, 0, true, false),
                dc => DrawBunny(dc, 0, false, false),
            ]);
        }

        if (!File.Exists(playingPath))
        {
            CreateAnimatedGif(playingPath, 48, 48, 80, [
                dc => DrawBunny(dc, -2, false, true, 0),
                dc => DrawBunny(dc, 0, false, true, 1),
                dc => DrawBunny(dc, 2, false, true, 2),
                dc => DrawBunny(dc, 0, false, true, 1),
            ]);
        }

        if (!File.Exists(pausedPath))
        {
            CreateAnimatedGif(pausedPath, 48, 48, 200, [
                dc => DrawBunny(dc, 1, false, false, sleepFrame: 0),
                dc => DrawBunny(dc, 1, false, false, sleepFrame: 1),
            ]);
        }
    }

    private static void CreateAnimatedGif(string filePath, int width, int height, ushort delayCentiseconds, Action<DrawingContext>[] frameDrawers)
    {
        var encoder = new GifBitmapEncoder();

        foreach (var drawer in frameDrawers)
        {
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                drawer(dc);
            }
            rtb.Render(dv);
            rtb.Freeze();

            var metadata = new BitmapMetadata("gif");
            metadata.SetQuery("/grctlext/Delay", delayCentiseconds);

            var frame = BitmapFrame.Create(rtb, null, metadata, null);
            encoder.Frames.Add(frame);
        }

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        encoder.Save(fs);
    }

    private static void DrawCat(DrawingContext dc, double yOffset, bool blink, bool dancing, int danceStep = 0, int sleepFrame = -1)
    {
        var bodyBrush = new SolidColorBrush(Color.FromRgb(240, 235, 255));
        var earInnerBrush = new SolidColorBrush(Color.FromRgb(244, 114, 182)); // Pink
        var eyeBrush = new SolidColorBrush(Color.FromRgb(40, 20, 60));
        var glowBrush = new SolidColorBrush(Color.FromArgb(80, 168, 85, 247)); // Purple glow

        // Subtle aura glow
        dc.DrawEllipse(glowBrush, null, new Point(24, 26 + yOffset), 18, 16);

        // Body
        dc.DrawEllipse(bodyBrush, null, new Point(24, 28 + yOffset), 14, 12);

        // Ears
        var leftEar = new StreamGeometry();
        using (var ctx = leftEar.Open())
        {
            ctx.BeginFigure(new Point(14, 20 + yOffset), true, true);
            ctx.LineTo(new Point(18, 10 + yOffset), true, false);
            ctx.LineTo(new Point(22, 19 + yOffset), true, false);
        }
        leftEar.Freeze();
        dc.DrawGeometry(bodyBrush, null, leftEar);

        var rightEar = new StreamGeometry();
        using (var ctx = rightEar.Open())
        {
            ctx.BeginFigure(new Point(26, 19 + yOffset), true, true);
            ctx.LineTo(new Point(30, 10 + yOffset), true, false);
            ctx.LineTo(new Point(34, 20 + yOffset), true, false);
        }
        rightEar.Freeze();
        dc.DrawGeometry(bodyBrush, null, rightEar);

        // Inner ears
        dc.DrawEllipse(earInnerBrush, null, new Point(18, 16 + yOffset), 2, 3);
        dc.DrawEllipse(earInnerBrush, null, new Point(30, 16 + yOffset), 2, 3);

        // Head / Face
        dc.DrawEllipse(bodyBrush, null, new Point(24, 22 + yOffset), 12, 10);

        if (sleepFrame >= 0)
        {
            // Sleeping eyes (- -)
            var pen = new Pen(eyeBrush, 1.5);
            dc.DrawLine(pen, new Point(17, 22 + yOffset), new Point(21, 22 + yOffset));
            dc.DrawLine(pen, new Point(27, 22 + yOffset), new Point(31, 22 + yOffset));

            // Sleep Z z
            var zBrush = new SolidColorBrush(Color.FromRgb(192, 132, 252));
            var zPen = new Pen(zBrush, 1.2);
            double zY = 12 + yOffset - (sleepFrame * 3);
            dc.DrawLine(zPen, new Point(32, zY), new Point(36, zY));
            dc.DrawLine(zPen, new Point(36, zY), new Point(32, zY + 4));
            dc.DrawLine(zPen, new Point(32, zY + 4), new Point(36, zY + 4));
        }
        else if (blink)
        {
            var pen = new Pen(eyeBrush, 1.5);
            dc.DrawLine(pen, new Point(17, 22 + yOffset), new Point(21, 22 + yOffset));
            dc.DrawLine(pen, new Point(27, 22 + yOffset), new Point(31, 22 + yOffset));
        }
        else
        {
            // Big cute eyes
            dc.DrawEllipse(eyeBrush, null, new Point(19, 21 + yOffset), 2.5, 3);
            dc.DrawEllipse(eyeBrush, null, new Point(29, 21 + yOffset), 2.5, 3);
            // Eye sparkles
            dc.DrawEllipse(Brushes.White, null, new Point(18.5, 20 + yOffset), 0.8, 0.8);
            dc.DrawEllipse(Brushes.White, null, new Point(28.5, 20 + yOffset), 0.8, 0.8);
        }

        // Cute pink nose
        dc.DrawEllipse(earInnerBrush, null, new Point(24, 24 + yOffset), 1.2, 1.0);

        // Music note if dancing
        if (dancing)
        {
            var noteBrush = new SolidColorBrush(Color.FromRgb(216, 180, 254));
            double noteX = danceStep == 0 ? 6 : (danceStep == 2 ? 38 : 36);
            double noteY = 8 + (danceStep % 2 * 3);
            dc.DrawEllipse(noteBrush, null, new Point(noteX, noteY + 4), 2, 1.5);
            dc.DrawLine(new Pen(noteBrush, 1.2), new Point(noteX + 1.5, noteY + 4), new Point(noteX + 1.5, noteY - 3));
        }
    }

    private static void DrawBunny(DrawingContext dc, double yOffset, bool blink, bool dancing, int danceStep = 0, int sleepFrame = -1)
    {
        var bodyBrush = new SolidColorBrush(Color.FromRgb(255, 240, 248));
        var earInnerBrush = new SolidColorBrush(Color.FromRgb(251, 146, 198));
        var eyeBrush = new SolidColorBrush(Color.FromRgb(60, 20, 50));
        var glowBrush = new SolidColorBrush(Color.FromArgb(80, 236, 72, 153));

        // Subtle glow
        dc.DrawEllipse(glowBrush, null, new Point(24, 26 + yOffset), 18, 16);

        // Body
        dc.DrawEllipse(bodyBrush, null, new Point(24, 28 + yOffset), 13, 12);

        // Long tall bunny ears
        double earWobble = dancing ? (danceStep - 1) * 2 : 0;
        dc.DrawRoundedRectangle(bodyBrush, null, new Rect(16 + earWobble, 6 + yOffset, 5, 14), 2.5, 2.5);
        dc.DrawRoundedRectangle(bodyBrush, null, new Rect(27 - earWobble, 6 + yOffset, 5, 14), 2.5, 2.5);

        // Inner ears
        dc.DrawRoundedRectangle(earInnerBrush, null, new Rect(17.5 + earWobble, 8 + yOffset, 2, 10), 1, 1);
        dc.DrawRoundedRectangle(earInnerBrush, null, new Rect(28.5 - earWobble, 8 + yOffset, 2, 10), 1, 1);

        // Head
        dc.DrawEllipse(bodyBrush, null, new Point(24, 22 + yOffset), 11, 10);

        if (sleepFrame >= 0)
        {
            var pen = new Pen(eyeBrush, 1.5);
            dc.DrawLine(pen, new Point(17, 22 + yOffset), new Point(21, 22 + yOffset));
            dc.DrawLine(pen, new Point(27, 22 + yOffset), new Point(31, 22 + yOffset));

            var zBrush = new SolidColorBrush(Color.FromRgb(244, 114, 182));
            var zPen = new Pen(zBrush, 1.2);
            double zY = 12 + yOffset - (sleepFrame * 3);
            dc.DrawLine(zPen, new Point(33, zY), new Point(37, zY));
            dc.DrawLine(zPen, new Point(37, zY), new Point(33, zY + 4));
            dc.DrawLine(zPen, new Point(33, zY + 4), new Point(37, zY + 4));
        }
        else if (blink)
        {
            var pen = new Pen(eyeBrush, 1.5);
            dc.DrawLine(pen, new Point(17, 22 + yOffset), new Point(21, 22 + yOffset));
            dc.DrawLine(pen, new Point(27, 22 + yOffset), new Point(31, 22 + yOffset));
        }
        else
        {
            // Ruby eyes
            dc.DrawEllipse(eyeBrush, null, new Point(19, 21 + yOffset), 2.3, 2.8);
            dc.DrawEllipse(eyeBrush, null, new Point(29, 21 + yOffset), 2.3, 2.8);
            dc.DrawEllipse(Brushes.White, null, new Point(18.5, 20 + yOffset), 0.8, 0.8);
            dc.DrawEllipse(Brushes.White, null, new Point(28.5, 20 + yOffset), 0.8, 0.8);
        }

        // Little nose
        dc.DrawEllipse(earInnerBrush, null, new Point(24, 24 + yOffset), 1.2, 1.0);

        // Musical sparkles if dancing
        if (dancing)
        {
            var sparkleBrush = new SolidColorBrush(Color.FromRgb(253, 186, 116));
            double sX = danceStep == 0 ? 38 : 8;
            double sY = 10 + (danceStep * 2);
            dc.DrawEllipse(sparkleBrush, null, new Point(sX, sY), 1.8, 1.8);
        }
    }
}
