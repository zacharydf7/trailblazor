namespace Trailblazor;

// Where the caret is, for the table menu: in a cell, in a header row, and whether the table has a header row.
internal sealed record RichTextTableState(bool InCell, bool InHeaderRow, bool HasHeaderRow);
