using System.Security.Cryptography;
using System.Text;

namespace Trailblazor.Tests;

// Expected symbols come from independent encoders: qrcodegen 1.8 (Nayuki), mask choice included, and python-barcode.
public class BarcodeEncoderTests
{
    [Fact]
    public void QrEncode_ShouldMatchAReferenceEncoder_WhenGivenDigits()
    {
        string[] expected =
        [
            "111111100010101111111", "100000101010101000001", "101110101011001011101", "101110100000101011101",
            "101110101111101011101", "100000101110001000001", "111111101010101111111", "000000001000000000000",
            "110100110011101110110", "001101001001010100010", "110010101101001111100", "111100010100111111011",
            "101010111101011100001", "000000001010011010000", "111111101111101011000", "100000100000001001011",
            "101110100100111001111", "101110101110011100111", "101110100111011100101", "100000101011100101010",
            "111111101100010101100",
        ];

        Assert.Equal(expected, Rows(QrEncoder.Encode("01234567", QrErrorCorrection.Low)));
    }

    [Fact]
    public void QrEncode_ShouldMatchAReferenceEncoder_WhenGivenUpperCaseLettersAndSpaces()
    {
        string[] expected =
        [
            "111111101100001111111", "100000101001001000001", "101110101001101011101", "101110101000001011101",
            "101110101010001011101", "100000100010001000001", "111111101010101111111", "000000001000000000000",
            "011010110000101011111", "010000001111000010001", "001101110110001011000", "011011010011010101110",
            "100010101011101110101", "000000001101001000101", "111111101010000101100", "100000100101101101000",
            "101110101010001111111", "101110100101010100010", "101110101001011101001", "100000101011110001011",
            "111111100001011100001",
        ];

        Assert.Equal(expected, Rows(QrEncoder.Encode("HELLO WORLD", QrErrorCorrection.Quartile)));
    }

    // Larger symbols compare by size and a hash of their rows; versions 7 and up also carry version information.
    [Theory]
    [InlineData("https://trailblazor.dev/components/qr-code", QrErrorCorrection.Medium, 29,
        "3410cee1c5170f1b4ce3a6e4069c6c07cff08efb793cbeb1f800b2864b72c30d")]
    [InlineData("Café ☕ at the trailhead", QrErrorCorrection.High, 33,
        "6c515939a508564c3d79f693188095fdba07fd97ed33864881eea41f5ba1f539")]
    [InlineData("8675309", QrErrorCorrection.Quartile, 53,
        "41c1cc0a2fb02837df6f55e44ec681424d60eed0a51537e3f059f6e83fa3f909", 40)]
    [InlineData("Meet at the north trailhead at nine. ", QrErrorCorrection.Medium, 69,
        "794a61bf07b4aba2bc770b0ccda81e5b85a9ec03f59af4b21e79a3db11a917c8", 8)]
    public void QrEncode_ShouldMatchAReferenceEncoder_WhenGivenLongerText(string text, QrErrorCorrection level,
        int size, string hash, int repeat = 1)
    {
        var modules = QrEncoder.Encode(string.Concat(Enumerable.Repeat(text, repeat)), level);

        var rows = string.Join("\n", Rows(modules));
        Assert.Equal((size, hash), (modules.GetLength(0),
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rows)))));
    }

    [Fact]
    public void QrEncode_ShouldThrow_WhenTheTextIsTooLongForAnyVersion()
    {
        var thrown = Assert.Throws<ArgumentException>(() => QrEncoder.Encode(new string('a', 3000),
            QrErrorCorrection.Low));

        Assert.StartsWith("The value is too long for a QR code at this error correction level.", thrown.Message);
    }

    [Theory]
    [InlineData("Trail log", "11010010000110111000101001001111010010110000100001101001100101000011011001100110010100001000111101010011010000100100110001100011101011")]
    [InlineData("12345678", "1101001110010110011100100010110001110001011011000010100100011101101100011101011")]
    [InlineData("Ticket #A-7", "110100100001101110001010000110100100001011001100001001010110010000100111101001101100110010010011000101000110001001101110011101101110110111101101100011101011")]
    public void Code128Encode_ShouldMatchAReferenceEncoder_WhenGivenText(string value, string expected)
    {
        Assert.Equal(expected, Bars(BarcodeEncoder.Encode(value, BarcodeFormat.Code128)));
    }

    [Theory]
    [InlineData("400638133393", "10100011010100111010111101111010001001011001101010100001010000101000010111010010000101100110101")]
    [InlineData("4006381333931", "10100011010100111010111101111010001001011001101010100001010000101000010111010010000101100110101")]
    [InlineData("978020137962", "10101110110001001010011100100110100111001100101010100001010001001110100101000011011001011100101")]
    public void Ean13Encode_ShouldMatchAReferenceEncoder_WhenGivenDigits(string value, string expected)
    {
        Assert.Equal(expected, Bars(BarcodeEncoder.Encode(value, BarcodeFormat.Ean13)));
    }

    [Theory]
    [InlineData("Trail\tlog", BarcodeFormat.Code128, "A Code 128 barcode takes one or more printable ASCII characters.")]
    [InlineData("12345", BarcodeFormat.Ean13, "An EAN-13 barcode takes 12 digits, or 13 with the check digit.")]
    [InlineData("4006381333932", BarcodeFormat.Ean13, "The EAN-13 check digit should be 1.")]
    public void BarcodeEncode_ShouldThrow_WhenTheFormatCantHoldTheValue(string value, BarcodeFormat format,
        string message)
    {
        var thrown = Assert.Throws<ArgumentException>(() => BarcodeEncoder.Encode(value, format));

        Assert.StartsWith(message, thrown.Message);
    }

    [Fact]
    public void BarPathRows_ShouldDrawOneRectanglePerRunOffsetByTheQuietZone_WhenGivenModules()
    {
        bool[,] modules = { { true, true, false, true }, { false, false, false, false } };

        Assert.Equal("M1 1h2v1h-2zM4 1h1v1h-1z", BarPath.Rows(modules, 1));
    }

    private static string[] Rows(bool[,] modules) => [.. Enumerable.Range(0, modules.GetLength(0)).Select(y =>
        string.Concat(Enumerable.Range(0, modules.GetLength(1)).Select(x => modules[y, x] ? '1' : '0')))];

    private static string Bars(bool[] bars) => string.Concat(bars.Select(bar => bar ? '1' : '0'));
}
