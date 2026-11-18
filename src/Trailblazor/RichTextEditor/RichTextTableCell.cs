namespace Trailblazor;

/// <summary>Where a <see cref="RichTextParagraph"/> of kind <see cref="RichTextParagraphKind.TableCell"/> sits in its
/// table, and whether it's a header cell.</summary>
public sealed record RichTextTableCell
{
    /// <summary>Creates a cell's place in its table.</summary>
    /// <param name="row">The cell's row, from 0 for the first.</param>
    /// <param name="column">The cell's column, from 0 for the first.</param>
    /// <param name="header">Whether it's a header cell, written as <c>&lt;th&gt;</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The row or column is negative.</exception>
    public RichTextTableCell(int row, int column, bool header = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        Row = row;
        Column = column;
        Header = header;
    }

    /// <summary>The cell's row, from 0 for the first.</summary>
    public int Row { get; }

    /// <summary>The cell's column, from 0 for the first.</summary>
    public int Column { get; }

    /// <summary>Whether it's a header cell, written as <c>&lt;th&gt;</c>.</summary>
    public bool Header { get; }
}
