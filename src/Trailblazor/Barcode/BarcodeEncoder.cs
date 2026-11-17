namespace Trailblazor;

// Encodes values as the bars of a one-dimensional barcode: true is a bar, false a space, one module each. The quiet
// zones around the bars aren't included.
internal static class BarcodeEncoder
{
    // Code 128's patterns by symbol value, 0 to 105 (ISO/IEC 15417), then the stop pattern.
    private static readonly string[] Code128Patterns =
    [
        "11011001100", "11001101100", "11001100110", "10010011000", "10010001100", "10001001100", "10011001000",
        "10011000100", "10001100100", "11001001000", "11001000100", "11000100100", "10110011100", "10011011100",
        "10011001110", "10111001100", "10011101100", "10011100110", "11001110010", "11001011100", "11001001110",
        "11011100100", "11001110100", "11101101110", "11101001100", "11100101100", "11100100110", "11101100100",
        "11100110100", "11100110010", "11011011000", "11011000110", "11000110110", "10100011000", "10001011000",
        "10001000110", "10110001000", "10001101000", "10001100010", "11010001000", "11000101000", "11000100010",
        "10110111000", "10110001110", "10001101110", "10111011000", "10111000110", "10001110110", "11101110110",
        "11010001110", "11000101110", "11011101000", "11011100010", "11011101110", "11101011000", "11101000110",
        "11100010110", "11101101000", "11101100010", "11100011010", "11101111010", "11001000010", "11110001010",
        "10100110000", "10100001100", "10010110000", "10010000110", "10000101100", "10000100110", "10110010000",
        "10110000100", "10011010000", "10011000010", "10000110100", "10000110010", "11000010010", "11001010000",
        "11110111010", "11000010100", "10001111010", "10100111100", "10010111100", "10010011110", "10111100100",
        "10011110100", "10011110010", "11110100100", "11110010100", "11110010010", "11011011110", "11011110110",
        "11110110110", "10101111000", "10100011110", "10001011110", "10111101000", "10111100010", "11110101000",
        "11110100010", "10111011110", "10111101110", "11101011110", "11110101110", "11010000100", "11010010000",
        "11010011100",
    ];

    private const string Code128Stop = "1100011101011";

    // EAN-13's patterns for each digit: odd parity (L), even parity (G) and the right half (R).
    private static readonly string[] EanL =
        ["0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"];

    private static readonly string[] EanG =
        ["0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111"];

    private static readonly string[] EanR =
        ["1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100"];

    // Which of the left half's digits use G, set by the first digit, which has no bars of its own.
    private static readonly string[] EanParity =
        ["LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"];

    public static bool[] Encode(string value, BarcodeFormat format)
    {
        if (Problem(value, format) is { } problem)
        {
            throw new ArgumentException(problem, nameof(value));
        }

        return Bits(format == BarcodeFormat.Ean13 ? Ean13(value) : Code128(value));
    }

    // Why the format can't hold the value, or null when it can.
    public static string? Problem(string value, BarcodeFormat format)
    {
        if (format == BarcodeFormat.Ean13)
        {
            if (value.Length is not (12 or 13) || !value.All(char.IsAsciiDigit))
            {
                return "An EAN-13 barcode takes 12 digits, or 13 with the check digit.";
            }

            var check = Ean13Check(value[..12]);
            return value.Length == 13 && value[12] - '0' != check ? $"The EAN-13 check digit should be {check}." : null;
        }

        return value.Length == 0 || value.Any(c => c < ' ' || c > '~')
            ? "A Code 128 barcode takes one or more printable ASCII characters." : null;
    }

    // An even run of digits packs two to a symbol in code set C; anything else uses code set B, printable ASCII.
    private static string Code128(string value)
    {
        var digits = value.Length % 2 == 0 && value.All(char.IsAsciiDigit);
        var symbols = new List<int> { digits ? 105 : 104 };
        if (digits)
        {
            for (var at = 0; at < value.Length; at += 2)
            {
                symbols.Add((value[at] - '0') * 10 + value[at + 1] - '0');
            }
        }
        else
        {
            symbols.AddRange(value.Select(c => c - ' '));
        }

        var check = symbols[0];
        for (var at = 1; at < symbols.Count; at++)
        {
            check += symbols[at] * at;
        }

        symbols.Add(check % 103);
        return string.Concat(symbols.Select(symbol => Code128Patterns[symbol])) + Code128Stop;
    }

    private static string Ean13(string value)
    {
        var digits = (value[..12] + Ean13Check(value[..12])).Select(c => c - '0').ToArray();
        var parity = EanParity[digits[0]];
        var left = string.Concat(Enumerable.Range(1, 6).Select(at => (parity[at - 1] == 'L' ? EanL : EanG)[digits[at]]));
        var right = string.Concat(Enumerable.Range(7, 6).Select(at => EanR[digits[at]]));
        return "101" + left + "01010" + right + "101";
    }

    // Weights the digits 1, 3, 1, 3 from the left, and the check digit brings the sum to a multiple of ten.
    public static int Ean13Check(string twelve) =>
        (10 - twelve.Select((c, at) => (c - '0') * (at % 2 == 0 ? 1 : 3)).Sum() % 10) % 10;

    private static bool[] Bits(string pattern) => [.. pattern.Select(c => c == '1')];
}
