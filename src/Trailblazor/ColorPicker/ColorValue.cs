using System.Globalization;
using System.Text.RegularExpressions;

namespace Trailblazor;

// A color as red, green, blue and alpha bytes, read from and written as CSS hex.
internal readonly partial record struct ColorValue(byte R, byte G, byte B, byte A = 255)
{
    public string ToHex(bool alpha) =>
        alpha && A < 255 ? $"#{R:x2}{G:x2}{B:x2}{A:x2}" : $"#{R:x2}{G:x2}{B:x2}";

    // Accepts #rgb, #rrggbb and rgb(r, g, b), with or without the #, and with Alpha also #rgba and #rrggbbaa.
    public static bool TryParse(string? text, bool alpha, out ColorValue color)
    {
        color = default;
        var trimmed = text?.Trim() ?? "";
        if (Rgb().Match(trimmed) is { Success: true } rgb)
        {
            var parts = rgb.Groups.Values.Skip(1).Select(group => int.Parse(group.Value, CultureInfo.InvariantCulture))
                .ToList();
            if (parts.Any(part => part > 255))
            {
                return false;
            }

            color = new((byte)parts[0], (byte)parts[1], (byte)parts[2]);
            return true;
        }

        var hex = trimmed.TrimStart('#');
        if (!Hex().IsMatch(hex) || hex.Length is not (3 or 6) && !(alpha && hex.Length is 4 or 8))
        {
            return false;
        }

        // A short form doubles each digit, as CSS does: #369 is #336699.
        if (hex.Length <= 4)
        {
            hex = string.Concat(hex.Select(digit => new string(digit, 2)));
        }

        var bytes = Convert.FromHexString(hex);
        color = new(bytes[0], bytes[1], bytes[2], bytes.Length == 4 ? bytes[3] : (byte)255);
        return true;
    }

    // Hue in degrees, saturation and brightness from 0 to 1.
    public (double Hue, double Saturation, double Brightness) ToHsv()
    {
        var (r, g, b) = (R / 255d, G / 255d, B / 255d);
        var max = Math.Max(r, Math.Max(g, b));
        var range = max - Math.Min(r, Math.Min(g, b));
        var hue = range == 0 ? 0
            : max == r ? 60 * ((g - b) / range % 6)
            : max == g ? 60 * ((b - r) / range + 2)
            : 60 * ((r - g) / range + 4);
        return (hue < 0 ? hue + 360 : hue, max == 0 ? 0 : range / max, max);
    }

    public static ColorValue FromHsv(double hue, double saturation, double brightness, byte alpha)
    {
        double Channel(int offset)
        {
            var k = (offset + hue / 60) % 6;
            return brightness - brightness * saturation * Math.Max(0, Math.Min(k, Math.Min(4 - k, 1)));
        }

        static byte ToByte(double channel) => (byte)Math.Round(channel * 255);
        return new(ToByte(Channel(5)), ToByte(Channel(3)), ToByte(Channel(1)), alpha);
    }

    // [0-9], not \d, which also matches other scripts' digits that int.Parse rejects.
    [GeneratedRegex(@"^rgb\(\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*\)$", RegexOptions.IgnoreCase)]
    private static partial Regex Rgb();

    [GeneratedRegex("^[0-9a-fA-F]+$")]
    private static partial Regex Hex();
}
