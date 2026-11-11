using System.Globalization;
using System.Text.RegularExpressions;

namespace Trailblazor;

// A color in OKLCH, the space the palette tokens are written in, with the conversions a contrast check needs.
internal readonly partial record struct OklchColor(double L, double C, double H)
{
    public static OklchColor White { get; } = new(1, 0, 0);

    // Reads "#rgb", "#rrggbb" or "oklch(l c h)", with l as a number or a percentage.
    public static bool TryParse(string? text, out OklchColor color)
    {
        color = default;
        var value = text?.Trim() ?? "";
        if (HexPattern().Match(value) is { Success: true } hex)
        {
            var digits = hex.Groups[1].Value;
            if (digits.Length == 3)
            {
                digits = string.Concat(digits.Select(digit => $"{digit}{digit}"));
            }

            color = FromSrgb(Channel(digits, 0), Channel(digits, 2), Channel(digits, 4));
            return true;
        }

        if (OklchPattern().Match(value) is { Success: true } oklch)
        {
            var lightness = double.Parse(oklch.Groups[1].Value, CultureInfo.InvariantCulture);
            color = new(oklch.Groups[2].Success ? lightness / 100 : lightness,
                double.Parse(oklch.Groups[3].Value, CultureInfo.InvariantCulture),
                double.Parse(oklch.Groups[4].Value, CultureInfo.InvariantCulture));
            return color.L is >= 0 and <= 1 && color.C >= 0;
        }

        return false;
    }

    public static OklchColor FromSrgb(double red, double green, double blue)
    {
        var (r, g, b) = (Linear(red), Linear(green), Linear(blue));
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
        var lightness = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
        var a = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var bAxis = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        var hue = Math.Atan2(bAxis, a) * 180 / Math.PI;
        return new(lightness, Math.Sqrt(a * a + bAxis * bAxis), hue < 0 ? hue + 360 : hue);
    }

    public OklchColor With(double? lightness = null, double? chroma = null) =>
        new(Math.Clamp(lightness ?? L, 0, 1), Math.Max(chroma ?? C, 0), H);

    // Relative luminance as WCAG defines it, after bringing the color into sRGB the way a browser shows it.
    public double Luminance()
    {
        var (r, g, b) = InGamut().LinearSrgb();
        return 0.2126 * Math.Clamp(r, 0, 1) + 0.7152 * Math.Clamp(g, 0, 1) + 0.0722 * Math.Clamp(b, 0, 1);
    }

    public double ContrastWith(OklchColor other)
    {
        var (lighter, darker) = (Math.Max(Luminance(), other.Luminance()), Math.Min(Luminance(), other.Luminance()));
        return (lighter + 0.05) / (darker + 0.05);
    }

    public string ToCss() => string.Create(CultureInfo.InvariantCulture, $"oklch({L:0.####} {C:0.####} {H:0.##})");

    // Lowers the chroma until the color fits in sRGB, keeping its lightness and hue.
    private OklchColor InGamut()
    {
        if (Fits(this))
        {
            return this;
        }

        var (low, high) = (0.0, C);
        for (var step = 0; step < 24; step++)
        {
            var middle = (low + high) / 2;
            (low, high) = Fits(With(chroma: middle)) ? (middle, high) : (low, middle);
        }

        return With(chroma: low);
    }

    private static bool Fits(OklchColor color)
    {
        const double Tolerance = 0.0001;
        var (r, g, b) = color.LinearSrgb();
        return new[] { r, g, b }.All(channel => channel is >= -Tolerance and <= 1 + Tolerance);
    }

    private (double R, double G, double B) LinearSrgb()
    {
        var radians = H * Math.PI / 180;
        var (a, b) = (C * Math.Cos(radians), C * Math.Sin(radians));
        var l = Math.Pow(L + 0.3963377774 * a + 0.2158037573 * b, 3);
        var m = Math.Pow(L - 0.1055613458 * a - 0.0638541728 * b, 3);
        var s = Math.Pow(L - 0.0894841775 * a - 1.2914855480 * b, 3);
        return (4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    private static double Channel(string digits, int start) =>
        int.Parse(digits.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;

    private static double Linear(double channel) =>
        channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexPattern();

    [GeneratedRegex(@"^oklch\(\s*([0-9]*\.?[0-9]+)(%)?\s+([0-9]*\.?[0-9]+)\s+([0-9]*\.?[0-9]+)\s*\)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex OklchPattern();
}
