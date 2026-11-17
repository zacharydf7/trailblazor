using System.Text;

namespace Trailblazor;

// Follows the structure of Nayuki's qrcodegen (MIT); see THIRD-PARTY-NOTICES.md.
// Encodes text as a QR code's modules (ISO/IEC 18004): the smallest version that holds it at the given level, in the most
// compact single mode, with the mask that scores lowest. True is a dark module; the quiet zone isn't included.
internal static class QrEncoder
{
    private const string Alphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    // Error correction codewords per block and number of blocks, by level (Low, Medium, Quartile, High) and version.
    private static readonly int[][] EccPerBlock =
    [
        [-1, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30,
            30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30],
        [-1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28],
        [-1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30,
            30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30],
        [-1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30,
            30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30],
    ];

    private static readonly int[][] Blocks =
    [
        [-1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18,
            19, 19, 20, 21, 22, 24, 25],
        [-1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31,
            33, 35, 37, 38, 40, 43, 45, 47, 49],
        [-1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40,
            43, 45, 48, 51, 53, 56, 59, 62, 65, 68],
        [-1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48,
            51, 54, 57, 60, 63, 66, 70, 74, 77, 81],
    ];

    // The level's two bits in the format information, by level.
    private static readonly int[] FormatBits = [1, 0, 3, 2];

    private enum Mode
    {
        Numeric,
        Alphanumeric,
        Byte,
    }

    public static bool Fits(string text, QrErrorCorrection level) => SmallestVersion(text, level) != 0;

    public static bool[,] Encode(string text, QrErrorCorrection level)
    {
        var mode = ModeOf(text);
        var bytes = mode == Mode.Byte ? Encoding.UTF8.GetBytes(text) : [];
        var count = mode == Mode.Byte ? bytes.Length : text.Length;
        var version = SmallestVersion(text, level);
        if (version == 0)
        {
            throw new ArgumentException("The value is too long for a QR code at this error correction level. Shorten "
                + "it or use a lower ErrorCorrection.", nameof(text));
        }

        var bits = new List<bool>();
        Append(bits, mode switch { Mode.Numeric => 1, Mode.Alphanumeric => 2, _ => 4 }, 4);
        Append(bits, count, CountBits(mode, version));
        switch (mode)
        {
            case Mode.Numeric:
                for (var at = 0; at < text.Length; at += 3)
                {
                    var group = text.Substring(at, Math.Min(3, text.Length - at));
                    Append(bits, int.Parse(group, System.Globalization.CultureInfo.InvariantCulture),
                        group.Length * 3 + 1);
                }

                break;
            case Mode.Alphanumeric:
                for (var at = 0; at < text.Length; at += 2)
                {
                    var first = Alphanumeric.IndexOf(text[at], StringComparison.Ordinal);
                    if (at + 1 < text.Length)
                    {
                        Append(bits, first * 45 + Alphanumeric.IndexOf(text[at + 1], StringComparison.Ordinal), 11);
                    }
                    else
                    {
                        Append(bits, first, 6);
                    }
                }

                break;
            default:
                foreach (var b in bytes)
                {
                    Append(bits, b, 8);
                }

                break;
        }

        // The terminator, then zeros to a whole byte, then the two pad bytes in turn until the data is full.
        var capacity = DataCodewords(version, level) * 8;
        Append(bits, 0, Math.Min(4, capacity - bits.Count));
        Append(bits, 0, (8 - bits.Count % 8) % 8);
        for (var pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11)
        {
            Append(bits, pad, 8);
        }

        var data = new byte[bits.Count / 8];
        for (var at = 0; at < bits.Count; at++)
        {
            data[at >> 3] |= (byte)((bits[at] ? 1 : 0) << (7 - (at & 7)));
        }

        return Draw(Interleave(data, version, level), version, level);
    }

    private static Mode ModeOf(string text) => text.All(char.IsAsciiDigit) ? Mode.Numeric
        : text.All(c => Alphanumeric.Contains(c, StringComparison.Ordinal)) ? Mode.Alphanumeric : Mode.Byte;

    // The smallest version that holds the text at the level, or 0 when none does.
    private static int SmallestVersion(string text, QrErrorCorrection level)
    {
        var mode = ModeOf(text);
        var count = mode == Mode.Byte ? Encoding.UTF8.GetByteCount(text) : text.Length;
        return Enumerable.Range(1, 40).FirstOrDefault(version =>
            DataBits(mode, count, version) <= DataCodewords(version, level) * 8);
    }

    private static void Append(List<bool> bits, int value, int length)
    {
        for (var at = length - 1; at >= 0; at--)
        {
            bits.Add(((value >> at) & 1) != 0);
        }
    }

    private static int CountBits(Mode mode, int version) => mode switch
    {
        Mode.Numeric => version <= 9 ? 10 : version <= 26 ? 12 : 14,
        Mode.Alphanumeric => version <= 9 ? 9 : version <= 26 ? 11 : 13,
        _ => version <= 9 ? 8 : 16,
    };

    private static int DataBits(Mode mode, int count, int version) => 4 + CountBits(mode, version) + mode switch
    {
        Mode.Numeric => count / 3 * 10 + (count % 3 == 0 ? 0 : count % 3 * 3 + 1),
        Mode.Alphanumeric => count / 2 * 11 + count % 2 * 6,
        _ => count * 8,
    };

    // The modules left for codewords once the patterns and the format and version information take theirs.
    private static int RawDataModules(int version)
    {
        var modules = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            var alignments = version / 7 + 2;
            modules -= (25 * alignments - 10) * alignments - 55;
            if (version >= 7)
            {
                modules -= 36;
            }
        }

        return modules;
    }

    private static int DataCodewords(int version, QrErrorCorrection level) =>
        RawDataModules(version) / 8 - EccPerBlock[(int)level][version] * Blocks[(int)level][version];

    // Splits the data into blocks, adds each block's error correction, and interleaves them, data then correction.
    private static byte[] Interleave(byte[] data, int version, QrErrorCorrection level)
    {
        var blocks = Blocks[(int)level][version];
        var eccLength = EccPerBlock[(int)level][version];
        var raw = RawDataModules(version) / 8;
        var shortBlocks = blocks - raw % blocks;
        var shortData = raw / blocks - eccLength;
        var divisor = Divisor(eccLength);
        var dataBlocks = new List<byte[]>();
        var eccBlocks = new List<byte[]>();
        for (int block = 0, at = 0; block < blocks; block++)
        {
            var length = shortData + (block < shortBlocks ? 0 : 1);
            var part = data[at..(at + length)];
            at += length;
            dataBlocks.Add(part);
            eccBlocks.Add(Remainder(part, divisor));
        }

        var result = new List<byte>(raw);
        for (var at = 0; at <= shortData; at++)
        {
            result.AddRange(dataBlocks.Where(block => at < block.Length).Select(block => block[at]));
        }

        for (var at = 0; at < eccLength; at++)
        {
            result.AddRange(eccBlocks.Select(block => block[at]));
        }

        return [.. result];
    }

    // The Reed-Solomon generator polynomial's coefficients, highest first, without its leading 1.
    private static byte[] Divisor(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;
        byte root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                result[j] = Multiply(result[j], root);
                if (j + 1 < degree)
                {
                    result[j] ^= result[j + 1];
                }
            }

            root = Multiply(root, 0x02);
        }

        return result;
    }

    private static byte[] Remainder(byte[] data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (var b in data)
        {
            var factor = (byte)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = 0;
            for (var at = 0; at < result.Length; at++)
            {
                result[at] ^= Multiply(divisor[at], factor);
            }
        }

        return result;
    }

    // Multiplies in GF(2^8) with the QR code's reducing polynomial, x^8 + x^4 + x^3 + x^2 + 1.
    private static byte Multiply(byte x, byte y)
    {
        var z = 0;
        for (var at = 7; at >= 0; at--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> at) & 1) * x;
        }

        return (byte)z;
    }

    private static bool[,] Draw(byte[] codewords, int version, QrErrorCorrection level)
    {
        var symbol = new Symbol(version);
        symbol.DrawPatterns();
        symbol.DrawCodewords(codewords);
        var chosen = Enumerable.Range(0, 8).MinBy(candidate =>
        {
            symbol.ApplyMask(candidate);
            symbol.DrawFormat(FormatBits[(int)level], candidate);
            var score = symbol.Penalty();
            symbol.ApplyMask(candidate);
            return score;
        });
        symbol.ApplyMask(chosen);
        symbol.DrawFormat(FormatBits[(int)level], chosen);
        return symbol.Modules;
    }

    private sealed class Symbol(int version)
    {
        private readonly int _size = version * 4 + 17;
        private readonly bool[,] _function = new bool[version * 4 + 17, version * 4 + 17];

        public bool[,] Modules { get; } = new bool[version * 4 + 17, version * 4 + 17];

        private void Set(int x, int y, bool dark)
        {
            Modules[y, x] = dark;
            _function[y, x] = true;
        }

        public void DrawPatterns()
        {
            for (var at = 0; at < _size; at++)
            {
                Set(6, at, at % 2 == 0);
                Set(at, 6, at % 2 == 0);
            }

            Finder(3, 3);
            Finder(_size - 4, 3);
            Finder(3, _size - 4);
            var positions = AlignmentPositions();
            for (var i = 0; i < positions.Length; i++)
            {
                for (var j = 0; j < positions.Length; j++)
                {
                    var corner = (i == 0 && j == 0) || (i == 0 && j == positions.Length - 1)
                        || (i == positions.Length - 1 && j == 0);
                    if (!corner)
                    {
                        Alignment(positions[i], positions[j]);
                    }
                }
            }

            // Reserves the format information's modules, filled once the mask is known.
            DrawFormat(0, 0);
            DrawVersion();
        }

        private void Finder(int x, int y)
        {
            for (var dy = -4; dy <= 4; dy++)
            {
                for (var dx = -4; dx <= 4; dx++)
                {
                    var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    if (x + dx >= 0 && x + dx < _size && y + dy >= 0 && y + dy < _size)
                    {
                        Set(x + dx, y + dy, distance != 2 && distance != 4);
                    }
                }
            }
        }

        private void Alignment(int x, int y)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    Set(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
                }
            }
        }

        private int[] AlignmentPositions()
        {
            if (version == 1)
            {
                return [];
            }

            var count = version / 7 + 2;
            var step = version == 32 ? 26 : (version * 4 + count * 2 + 1) / (count * 2 - 2) * 2;
            var result = new int[count];
            result[0] = 6;
            for (int i = count - 1, position = _size - 7; i >= 1; i--, position -= step)
            {
                result[i] = position;
            }

            return result;
        }

        public void DrawFormat(int levelBits, int mask)
        {
            var data = levelBits << 3 | mask;
            var remainder = data;
            for (var i = 0; i < 10; i++)
            {
                remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
            }

            var bits = (data << 10 | remainder) ^ 0x5412;
            for (var i = 0; i <= 5; i++)
            {
                Set(8, i, Bit(bits, i));
            }

            Set(8, 7, Bit(bits, 6));
            Set(8, 8, Bit(bits, 7));
            Set(7, 8, Bit(bits, 8));
            for (var i = 9; i < 15; i++)
            {
                Set(14 - i, 8, Bit(bits, i));
            }

            for (var i = 0; i < 8; i++)
            {
                Set(_size - 1 - i, 8, Bit(bits, i));
            }

            for (var i = 8; i < 15; i++)
            {
                Set(8, _size - 15 + i, Bit(bits, i));
            }

            Set(8, _size - 8, true);
        }

        private void DrawVersion()
        {
            if (version < 7)
            {
                return;
            }

            var remainder = version;
            for (var i = 0; i < 12; i++)
            {
                remainder = (remainder << 1) ^ ((remainder >> 11) * 0x1F25);
            }

            var bits = version << 12 | remainder;
            for (var i = 0; i < 18; i++)
            {
                var a = _size - 11 + i % 3;
                var b = i / 3;
                Set(a, b, Bit(bits, i));
                Set(b, a, Bit(bits, i));
            }
        }

        // Fills the codewords' bits in two-module columns, zigzagging up and down from the right, around the patterns.
        public void DrawCodewords(byte[] codewords)
        {
            var at = 0;
            for (var right = _size - 1; right >= 1; right -= 2)
            {
                if (right == 6)
                {
                    right = 5;
                }

                for (var step = 0; step < _size; step++)
                {
                    for (var j = 0; j < 2; j++)
                    {
                        var x = right - j;
                        var upward = ((right + 1) & 2) == 0;
                        var y = upward ? _size - 1 - step : step;
                        if (!_function[y, x] && at < codewords.Length * 8)
                        {
                            Modules[y, x] = Bit(codewords[at >> 3], 7 - (at & 7));
                            at++;
                        }
                    }
                }
            }
        }

        public void ApplyMask(int mask)
        {
            for (var y = 0; y < _size; y++)
            {
                for (var x = 0; x < _size; x++)
                {
                    var flip = mask switch
                    {
                        0 => (x + y) % 2 == 0,
                        1 => y % 2 == 0,
                        2 => x % 3 == 0,
                        3 => (x + y) % 3 == 0,
                        4 => (x / 3 + y / 2) % 2 == 0,
                        5 => x * y % 2 + x * y % 3 == 0,
                        6 => (x * y % 2 + x * y % 3) % 2 == 0,
                        _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                    };
                    if (flip && !_function[y, x])
                    {
                        Modules[y, x] = !Modules[y, x];
                    }
                }
            }
        }

        // The four penalty rules: runs of one color, 2x2 blocks, finder-like patterns, and dark and light out of balance.
        public int Penalty()
        {
            var score = 0;
            for (var line = 0; line < _size; line++)
            {
                score += LinePenalty(at => Modules[line, at]) + LinePenalty(at => Modules[at, line]);
            }

            for (var y = 0; y < _size - 1; y++)
            {
                for (var x = 0; x < _size - 1; x++)
                {
                    var color = Modules[y, x];
                    if (color == Modules[y, x + 1] && color == Modules[y + 1, x] && color == Modules[y + 1, x + 1])
                    {
                        score += 3;
                    }
                }
            }

            var dark = 0;
            foreach (var module in Modules)
            {
                dark += module ? 1 : 0;
            }

            var total = _size * _size;
            // The smallest k with (45 - 5k)% <= dark share <= (55 + 5k)%.
            var k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
            return score + k * 10;
        }

        // Runs of five or more modules of one color, and finder-like patterns: whole dark and light runs in the ratio
        // 1:1:3:1:1, scored once for each side bordered by a light run at least four units wide. The area outside
        // counts as light.
        private int LinePenalty(Func<int, bool> at)
        {
            var runs = new List<int> { _size };
            for (var i = 0; i < _size; i++)
            {
                if (at(i) == (runs.Count % 2 == 0))
                {
                    runs[^1]++;
                }
                else
                {
                    runs.Add(1);
                }
            }

            if (runs.Count % 2 == 0)
            {
                runs.Add(_size);
            }
            else
            {
                runs[^1] += _size;
            }

            var finders = 0;
            for (var j = 0; j + 6 < runs.Count; j += 2)
            {
                var n = runs[j + 1];
                if (runs[j + 2] == n && runs[j + 3] == 3 * n && runs[j + 4] == n && runs[j + 5] == n)
                {
                    finders += (runs[j] >= 4 * n && runs[j + 6] >= n ? 1 : 0)
                        + (runs[j + 6] >= 4 * n && runs[j] >= n ? 1 : 0);
                }
            }

            var score = finders * 40;
            var run = 0;
            var color = false;
            for (var i = 0; i < _size; i++)
            {
                if (i > 0 && at(i) == color)
                {
                    run++;
                    if (run == 5)
                    {
                        score += 3;
                    }
                    else if (run > 5)
                    {
                        score++;
                    }
                }
                else
                {
                    color = at(i);
                    run = 1;
                }
            }

            return score;
        }

        private static bool Bit(int value, int at) => ((value >> at) & 1) != 0;
    }
}
