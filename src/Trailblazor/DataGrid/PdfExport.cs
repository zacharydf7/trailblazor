using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Trailblazor;

// Writes rows as a PDF of landscape A4 pages in Helvetica, one of the fonts every PDF reader has, so nothing is
// embedded. Each page repeats the title and headings; text too wide for its column ends with an ellipsis, and
// characters outside Windows-1252 show as question marks, since the standard fonts have no others.
internal static class PdfExport
{
    private const float PageWidth = 842;
    private const float PageHeight = 595;
    private const float Margin = 36;
    private const float FontSize = 9;
    private const float TitleSize = 12;
    private const float RowHeight = 15;
    private const float Padding = 4;
    private const float GroupIndent = 12;
    private const float MaxColumn = 240;
    private const float Usable = PageWidth - 2 * Margin;

    public static byte[] Write(string title, IReadOnlyList<string> headings, IReadOnlyList<PdfLine> lines)
    {
        var widths = WidthsOf(headings, lines);
        List<string> pages = [];
        var page = new StringBuilder();
        var y = StartPage(page, title, headings, widths);
        foreach (var line in lines)
        {
            if (y - RowHeight < Margin + RowHeight)
            {
                pages.Add(page.ToString());
                page.Clear();
                y = StartPage(page, title, headings, widths);
            }

            y -= RowHeight;
            switch (line)
            {
                case PdfGroup group:
                    var indent = (group.Level - 1) * GroupIndent;
                    Text(page, Fit(group.Text, Usable - indent, bold: true), Margin + Padding + indent, y, bold: true);
                    break;
                case PdfTotals totals:
                    Rule(page, y + RowHeight - 3);
                    Cells(page, totals.Cells, widths, y, bold: true);
                    break;
                case PdfRow row:
                    Cells(page, row.Cells, widths, y, bold: false);
                    break;
            }
        }

        pages.Add(page.ToString());
        return Assemble(title, pages);
    }

    // Each column is as wide as its widest text, up to a limit, and the columns shrink together to fit the page.
    private static float[] WidthsOf(IReadOnlyList<string> headings, IReadOnlyList<PdfLine> lines)
    {
        var widths = headings.Select(heading => Measure(heading, bold: true)).ToArray();
        foreach (var line in lines)
        {
            var (cells, bold) = line switch
            {
                PdfRow row => (row.Cells, false),
                PdfTotals totals => (totals.Cells, true),
                _ => ([], false),
            };
            for (var at = 0; at < cells.Count && at < widths.Length; at++)
            {
                widths[at] = Math.Max(widths[at], Measure(cells[at].Text ?? "", bold));
            }
        }

        widths = [.. widths.Select(width => Math.Min(width + 2 * Padding, MaxColumn))];
        var total = widths.Sum();
        return total > Usable ? [.. widths.Select(width => width * Usable / total)] : widths;
    }

    private static float StartPage(StringBuilder page, string title, IReadOnlyList<string> headings, float[] widths)
    {
        var y = PageHeight - Margin - TitleSize;
        Text(page, Fit(title, Usable, bold: true, TitleSize), Margin, y, bold: true, TitleSize);
        y -= RowHeight + 6;
        var width = widths.Sum();
        page.Append(CultureInfo.InvariantCulture, $"0.92 g {Margin} {y - 4} {N(width)} {RowHeight} re f 0 g\n");
        var x = Margin;
        for (var at = 0; at < headings.Count; at++)
        {
            Text(page, Fit(headings[at], widths[at] - 2 * Padding, bold: true), x + Padding, y, bold: true);
            x += widths[at];
        }

        return y;
    }

    // Numbers line up on the right, as a spreadsheet shows them.
    private static void Cells(StringBuilder page, IReadOnlyList<ExportCell> cells, float[] widths, float y, bool bold)
    {
        var x = Margin;
        for (var at = 0; at < cells.Count && at < widths.Length; at++)
        {
            var text = Fit(cells[at].Text ?? "", widths[at] - 2 * Padding, bold);
            var left = cells[at].IsNumber ? x + widths[at] - Padding - Measure(text, bold) : x + Padding;
            Text(page, text, left, y, bold);
            x += widths[at];
        }
    }

    private static void Rule(StringBuilder page, float y) =>
        page.Append(CultureInfo.InvariantCulture, $"0.5 w {Margin} {N(y)} m {Margin + Usable} {N(y)} l S\n");

    private static void Text(StringBuilder page, string text, float x, float y, bool bold, float size = FontSize)
    {
        if (text.Length == 0)
        {
            return;
        }

        page.Append(CultureInfo.InvariantCulture, $"BT /{(bold ? "F2" : "F1")} {N(size)} Tf {N(x)} {N(y)} Td <");
        foreach (var letter in text)
        {
            page.Append(WinAnsi(letter).ToString("X2", CultureInfo.InvariantCulture));
        }

        page.Append("> Tj ET\n");
    }

    // A column is its widest text plus padding, and taking the padding back off in floating point can leave that text
    // a hair too wide.
    private static string Fit(string text, float width, bool bold, float size = FontSize)
    {
        width += 0.01f;
        if (Measure(text, bold, size) <= width)
        {
            return text;
        }

        var room = width - Measure("…", bold, size);
        var (length, used) = (0, 0f);
        while (length < text.Length && used + Measure(text[length].ToString(), bold, size) <= room)
        {
            used += Measure(text[length].ToString(), bold, size);
            length++;
        }

        return length == 0 ? "" : text[..length] + "…";
    }

    private static float Measure(string text, bool bold, float size = FontSize) =>
        text.Sum(letter => (float)GlyphWidth(WinAnsi(letter), bold)) * size / 1000;

    private static string N(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static byte[] Assemble(string title, List<string> pages)
    {
        using var pdf = new MemoryStream();
        List<long> offsets = [];
        void Write(string text) => pdf.Write(Encoding.ASCII.GetBytes(text));
        void Begin() => offsets.Add(pdf.Position);

        // Bytes above 127 in a comment tell transfer tools the file is binary.
        pdf.Write([.. "%PDF-1.4\n%"u8, 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n']);
        Begin();
        Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        Begin();
        var kids = string.Join(" ", pages.Select((_, at) => $"{6 + 2 * at} 0 R"));
        Write($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>\nendobj\n");
        Begin();
        Write("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        Begin();
        Write("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");
        Begin();
        var titleHex = Convert.ToHexString([0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes(title)]);
        Write($"5 0 obj\n<< /Title <{titleHex}> /Producer (Trailblazor) >>\nendobj\n");
        for (var at = 0; at < pages.Count; at++)
        {
            var number = 6 + 2 * at;
            Begin();
            Write($"{number} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(PageWidth)} {N(PageHeight)}] "
                + $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {number + 1} 0 R >>\nendobj\n");
            var content = Compress(pages[at] + Footer(at + 1, pages.Count));
            Begin();
            Write($"{number + 1} 0 obj\n<< /Length {content.Length} /Filter /FlateDecode >>\nstream\n");
            pdf.Write(content);
            Write("\nendstream\nendobj\n");
        }

        var xref = pdf.Position;
        var table = new StringBuilder($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            table.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        Write(table.ToString());
        Write($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R /Info 5 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return pdf.ToArray();
    }

    private static string Footer(int page, int pages)
    {
        var footer = new StringBuilder();
        var text = $"{page} / {pages}";
        Text(footer, text, PageWidth - Margin - Measure(text, bold: false), Margin / 2, bold: false);
        return footer.ToString();
    }

    private static byte[] Compress(string content)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal))
        {
            zlib.Write(Encoding.ASCII.GetBytes(content));
        }

        return compressed.ToArray();
    }

    // Windows-1252 matches Latin-1 apart from the letters and marks it keeps in 0x80 to 0x9F.
    private static byte WinAnsi(char letter) => letter switch
    {
        >= ' ' and <= '~' or >= ' ' and <= 'ÿ' => (byte)letter,
        _ => (byte)(letter != '\0' && Array.IndexOf(Extras, letter) is var at and >= 0 ? 0x80 + at : '?'),
    };

    private static readonly char[] Extras =
    [
        '€', '\0', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰', 'Š', '‹', 'Œ', '\0', 'Ž', '\0',
        '\0', '‘', '’', '“', '”', '•', '–', '—', '˜', '™', 'š', '›', 'œ', '\0', 'ž', 'Ÿ',
    ];

    // Helvetica's widths in thousandths of the font size, for the characters from space to tilde; others but the
    // ellipsis are measured as an average letter.
    private static int GlyphWidth(byte code, bool bold) =>
        code is >= 32 and <= 126 ? (bold ? BoldWidths : Widths)[code - 32] : code == 0x85 ? 1000 : 556;

    private static readonly int[] Widths =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly int[] BoldWidths =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];
}

// A line of an exported PDF: a row's cells, a group's heading, or totals, which show in bold under a rule.
internal abstract record PdfLine;

internal sealed record PdfRow(IReadOnlyList<ExportCell> Cells) : PdfLine;

internal sealed record PdfGroup(int Level, string Text) : PdfLine;

internal sealed record PdfTotals(IReadOnlyList<ExportCell> Cells) : PdfLine;
