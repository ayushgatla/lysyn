using System;
using System.Windows.Media;

namespace LyricIsland.Core.Services;

public static class ThemeColorHelper
{
    public static Color FromHsv(double hue, double saturation, double value)
    {
        hue = (hue % 360.0 + 360.0) % 360.0;
        saturation = Math.Clamp(saturation, 0.0, 1.0);
        value = Math.Clamp(value, 0.0, 1.0);

        double c = value * saturation;
        double x = c * (1.0 - Math.Abs((hue / 60.0) % 2.0 - 1.0));
        double m = value - c;

        double rPrime = 0, gPrime = 0, bPrime = 0;
        if (hue < 60) { rPrime = c; gPrime = x; bPrime = 0; }
        else if (hue < 120) { rPrime = x; gPrime = c; bPrime = 0; }
        else if (hue < 180) { rPrime = 0; gPrime = c; bPrime = x; }
        else if (hue < 240) { rPrime = 0; gPrime = x; bPrime = c; }
        else if (hue < 300) { rPrime = x; gPrime = 0; bPrime = c; }
        else { rPrime = c; gPrime = 0; bPrime = x; }

        byte r = (byte)Math.Round((rPrime + m) * 255);
        byte g = (byte)Math.Round((gPrime + m) * 255);
        byte b = (byte)Math.Round((bPrime + m) * 255);

        return Color.FromRgb(r, g, b);
    }

    public static Color GetAccentColor(double hue)
    {
        if (hue < 0) return Color.FromRgb(255, 255, 255); // Solid White
        return FromHsv(hue, 0.95, 0.98);
    }

    public static Color GetHighlightColor(double hue)
    {
        if (hue < 0) return Color.FromRgb(255, 255, 255); // Solid White
        return FromHsv(hue, 0.45, 0.98);
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static Color FromHex(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex[..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                return Color.FromRgb(r, g, b);
            }
            if (hex.Length == 8)
            {
                byte a = Convert.ToByte(hex[..2], 16);
                byte r = Convert.ToByte(hex[2..4], 16);
                byte g = Convert.ToByte(hex[4..6], 16);
                byte b = Convert.ToByte(hex[6..8], 16);
                return Color.FromArgb(a, r, g, b);
            }
        }
        catch { }
        return fallback;
    }

    public static double GetHueFromColor(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        if (delta < 0.0001) return 0.0;

        double hue;
        if (Math.Abs(max - r) < 0.0001)
            hue = 60.0 * (((g - b) / delta) % 6);
        else if (Math.Abs(max - g) < 0.0001)
            hue = 60.0 * (((b - r) / delta) + 2);
        else
            hue = 60.0 * (((r - g) / delta) + 4);

        if (hue < 0) hue += 360.0;
        return hue;
    }
}
