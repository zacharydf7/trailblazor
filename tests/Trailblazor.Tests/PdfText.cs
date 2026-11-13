using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Trailblazor.Tests;

// Reads back what an exported PDF draws, page by page, so tests can check its text and where it sits.
internal static partial class PdfText
{
    public sealed record Run(string Text, float X, float Y, bool Bold);

    public static List<List<Run>> PagesOf(byte[] pdf) =>
        [.. Streams().Matches(Encoding.Latin1.GetString(pdf)).Select(match => RunsOf(Inflate(match.Groups[1].Value)))];

    public static List<List<string>> TextOf(byte[] pdf) =>
        [.. PagesOf(pdf).Select(page => page.Select(run => run.Text).ToList())];

    private static string Inflate(string stream)
    {
        using var zlib = new ZLibStream(new MemoryStream(Encoding.Latin1.GetBytes(stream)), CompressionMode.Decompress);
        using var reader = new StreamReader(zlib, Encoding.ASCII);
        return reader.ReadToEnd();
    }

    private static List<Run> RunsOf(string content) =>
        [.. Shows().Matches(content).Select(match => new Run(Decode(match.Groups[4].Value),
            float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
            float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), match.Groups[1].Value == "2"))];

    // Windows-1252 is Latin-1 apart from 0x80 to 0x9F; those codes come back as their byte values, except the ellipsis.
    private static string Decode(string hex) =>
        string.Concat(Convert.FromHexString(hex).Select(code => code == 0x85 ? '…' : (char)code));

    [GeneratedRegex(@"/FlateDecode >>\nstream\n(.*?)\nendstream", RegexOptions.Singleline)]
    private static partial Regex Streams();

    [GeneratedRegex(@"/F(\d) [\d.]+ Tf ([\d.]+) ([\d.]+) Td <([0-9A-F]*)> Tj")]
    private static partial Regex Shows();
}
