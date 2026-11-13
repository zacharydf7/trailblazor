using Microsoft.AspNetCore.Components;
using Trailblazor.Internal.Events;

namespace Trailblazor;

public partial class TbDataGrid<TItem>
{
    // The selected range's two end cells, by row key and column key. The rows and columns between them, in the order
    // shown, are worked out again on each render, since the rows and columns can change under them.
    private (object Row, string Column)? _rangeFrom;
    private (object Row, string Column)? _rangeTo;
    private HashSet<object> _rangeRows = [];
    private HashSet<string> _rangeColumns = [];

    /// <summary>Lets the user select a range of cells, by dragging across them or with Shift and a click, an arrow key,
    /// Home or End, and copy it with Ctrl+C (Cmd+C on a Mac) as text and as a table that a spreadsheet pastes into its
    /// cells. Without a range, Ctrl+C copies the cell in focus. Escape, a click or a move without Shift clears the
    /// range. With it on, Shift with an arrow key or a click selects cells rather than rows, even when
    /// <see cref="SelectionMode"/> is <see cref="SelectionMode.Multiple"/>. A cell copies the text of its column's
    /// <see cref="TbColumn{TItem}.Field"/>, as the CSV export does, so a column with a
    /// <see cref="TbColumn{TItem}.CellTemplate"/> and no Field copies as blank; a change not yet saved in a grid that
    /// edits in batches copies as the cell shows it. A virtualized grid with a <see cref="DataProvider"/> selects
    /// among the rows it has loaded.</summary>
    [Parameter]
    public bool CellSelection { get; set; }

    private bool HasRange => _rangeRows.Count > 0;

    private bool InRange(object key, TbColumn<TItem> column) =>
        HasRange && _rangeColumns.Contains(column.Key) && _rangeRows.Contains(key);

    // An end as the script names it, so it can tell whether the copy is of the range it last reported.
    private string? RangeEnd((object Row, string Column)? end) =>
        end is { } cell ? $"{_ids.IdOf(cell.Row)}:{cell.Column}" : null;

    // The range's rows in the order shown, each with the text of its cells in the range.
    private IEnumerable<IEnumerable<string>> SelectedCellTexts()
    {
        List<TbColumn<TItem>> columns = [.. _shownColumns.Where(column => _rangeColumns.Contains(column.Key))];
        return _rows.Where(row => _rangeRows.Contains(KeyOf(row)))
            .Select(row => columns.Select(column => CopyTextOf(KeyOf(row), column, row)));
    }

    // A batch grid's change not yet saved copies as the cell shows it, rather than as the item holds it.
    private string CopyTextOf(object key, TbColumn<TItem> column, TItem item) =>
        (ChangeOf(key, column) is { } change ? column.TextOf(change.Value) : column.TextOf(item)) ?? "";

    private void OnCellRange(CellRangeEventArgs args)
    {
        _rangeFrom = EndOf(args.FromRow, args.FromColumn);
        _rangeTo = EndOf(args.ToRow, args.ToColumn);
        StateHasChanged();
    }

    private (object Row, string Column)? EndOf(string? row, string? column) =>
        column is not null && _rows.FindIndex(item => IdOf(item) == row) is >= 0 and var at
            ? (KeyOf(_rows[at]), column) : null;

    // Rows draw again only when the range covers different cells, which a sort can cause even though no row itself
    // changed.
    private void UpdateRange()
    {
        var (rows, columns) = RangeCells();
        if (rows.SetEquals(_rangeRows) && columns.SetEquals(_rangeColumns))
        {
            return;
        }

        (_rangeRows, _rangeColumns) = (rows, columns);
        _rowVersion++;
    }

    // A single cell isn't a range, so a drag or Shift move back to the starting cell clears it. An end the rows or
    // columns no longer show, such as after a filter, leaves no range until it shows again.
    private (HashSet<object> Rows, HashSet<string> Columns) RangeCells()
    {
        if (!CellSelection || _rangeFrom is not { } from || _rangeTo is not { } to)
        {
            return ([], []);
        }

        var (top, bottom) = (_rows.FindIndex(row => Equals(KeyOf(row), from.Row)),
            _rows.FindIndex(row => Equals(KeyOf(row), to.Row)));
        var (start, end) = (_shownColumns.FindIndex(column => column.Key == from.Column),
            _shownColumns.FindIndex(column => column.Key == to.Column));
        if (top < 0 || bottom < 0 || start < 0 || end < 0 || top == bottom && start == end)
        {
            return ([], []);
        }

        return ([.. _rows.GetRange(Math.Min(top, bottom), Math.Abs(bottom - top) + 1).Select(KeyOf)],
            [.. _shownColumns.GetRange(Math.Min(start, end), Math.Abs(end - start) + 1).Select(column => column.Key)]);
    }

    private void OnCellsCopied() => Telemetry.Used(TelemetryName, "copy");
}
