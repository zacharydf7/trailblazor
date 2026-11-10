using System.Text;

namespace Trailblazor;

// Writes rows as CSV (RFC 4180): commas between fields, CRLF between rows, and quotes around a field that holds a
// comma, a quote or a line break.
internal static class CsvExport
{
    // A spreadsheet runs text starting with one of these as a formula, so the export puts an apostrophe in front.
    private static readonly char[] FormulaStarts = ['=', '+', '-', '@', '\t', '\r'];

    public static string Write(IEnumerable<string> headings, IEnumerable<IEnumerable<ExportCell>> rows)
    {
        var csv = new StringBuilder();
        AddLine(csv, headings.Select(heading => Field(heading, guard: true)));
        foreach (var row in rows)
        {
            AddLine(csv, row.Select(cell => Field(cell.Text ?? "", guard: !cell.IsNumber)));
        }

        return csv.ToString();
    }

    private static void AddLine(StringBuilder csv, IEnumerable<string> fields) =>
        csv.Append(string.Join(',', fields)).Append("\r\n");

    private static string Field(string text, bool guard)
    {
        if (guard && text.Length > 0 && FormulaStarts.Contains(text[0]))
        {
            text = "'" + text;
        }

        return text.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }
}

// A cell to export: its value, which a workbook keeps as a number, date or yes-or-no, and its text as the grid
// shows it.
internal readonly record struct ExportCell(object? Value, string? Text)
{
    public bool IsNumber => Value is not null && NumberTypes.IsNumber(Value.GetType());
}
