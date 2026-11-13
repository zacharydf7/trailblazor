using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Trailblazor.Internal.Events;

namespace Trailblazor;

public partial class TbDataGrid<TItem>
{
    private const double DefaultColumnWidth = 160;
    // How many unpinned columns are drawn beyond each side of the box, so a key or a short scroll lands on a drawn one.
    private const int ColumnOverscan = 2;

    // The first and last unpinned columns in view, by place among them, as the script last reported; until it does, as
    // many as a wide screen shows.
    private (int First, int Last) _columnWindow = (0, 9);
    // How many pixels a rem is, as the script last reported, so a column whose Width is in rem follows the page's font
    // size.
    private double _remPixels = 16;
    private List<DrawnColumn> _drawnColumns = [];
    private Dictionary<string, int> _columnPlaces = [];

    /// <summary>Draws only the columns in view, and two either side, for grids with hundreds of columns, as
    /// <see cref="Virtualize"/> does for rows. Pinned columns are always drawn. Each column needs a width the grid knows
    /// before it's drawn: the width the user resized it to, or else its <see cref="TbColumn{TItem}.Width"/> in px or rem,
    /// or else 160px; a width in rem follows the page's font size from the next scroll or resize. The arrow keys, Home
    /// and End reach columns not yet drawn. Not available with column groups.</summary>
    [Parameter]
    public bool VirtualizeColumns { get; set; }

    // Keys for the spacers among keyed cells, which no column key can equal.
    private readonly object _spacerBefore = new();
    private readonly object _spacerAfter = new();

    // A column a row draws, or a spacer that holds the width of the unpinned columns it skips.
    private readonly record struct DrawnColumn(TbColumn<TItem>? Column, double SpacerWidth = 0, object? SpacerKey = null);

    private static string SpacerStyle(DrawnColumn drawn) =>
        $"width: {drawn.SpacerWidth.ToString(CultureInfo.InvariantCulture)}px";

    private TbColumn<TItem>? FirstDrawn => _drawnColumns.FirstOrDefault(drawn => drawn.Column is not null).Column;

    private void ArrangeDrawnColumns()
    {
        _columnPlaces = _shownColumns.Select((column, at) => (column.Key, at)).ToDictionary(place => place.Key,
            place => place.at);
        List<TbColumn<TItem>> middle = [.. Unpinned];
        if (!VirtualizeColumns || middle.Count == 0)
        {
            _drawnColumns = [.. _shownColumns.Select(column => new DrawnColumn(column))];
            return;
        }

        var first = Math.Clamp(_columnWindow.First - ColumnOverscan, 0, middle.Count - 1);
        var last = Math.Clamp(_columnWindow.Last + ColumnOverscan, first, middle.Count - 1);
        _drawnColumns =
        [
            .. _shownColumns.Where(column => column.Pinned == ColumnPin.Start).Select(column => new DrawnColumn(column)),
            new(null, middle.Take(first).Sum(PixelsOf), _spacerBefore),
            .. middle.GetRange(first, last - first + 1).Select(column => new DrawnColumn(column)),
            new(null, middle.Skip(last + 1).Sum(PixelsOf), _spacerAfter),
            .. _shownColumns.Where(column => column.Pinned == ColumnPin.End).Select(column => new DrawnColumn(column)),
        ];
    }

    // The column's place among all the grid's columns, counted from 1, as aria-colindex gives it.
    private int? ColumnIndexOf(TbColumn<TItem> column) =>
        VirtualizeColumns ? LeadCount + _columnPlaces[column.Key] + 1 : null;

    // A width the grid knows before the column is drawn: the user's, or else Width in px or rem, or else the default.
    private double PixelsOf(TbColumn<TItem> column)
    {
        if (_widths.TryGetValue(column.Key, out var width))
        {
            return width;
        }

        var given = column.Width?.Trim() ?? "";
        var (number, scale) = given.EndsWith("rem", StringComparison.Ordinal) ? (given[..^3], _remPixels)
            : given.EndsWith("px", StringComparison.Ordinal) ? (given[..^2], 1) : ("", 0);
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size > 0
            ? size * scale : DefaultColumnWidth;
    }

    // The unpinned columns' keys, as a JSON array, and widths, which the script reads to work out which are in view.
    private string? UnpinnedKeys => VirtualizeColumns
        ? $"[{string.Join(",", Unpinned.Select(column => $"\"{JsonEncodedText.Encode(column.Key)}\""))}]" : null;

    private string? UnpinnedWidths => VirtualizeColumns
        ? string.Join(",", Unpinned.Select(column => PixelsOf(column).ToString(CultureInfo.InvariantCulture))) : null;

    private IEnumerable<TbColumn<TItem>> Unpinned => _shownColumns.Where(column => column.Pinned == ColumnPin.None);

    private void OnColumnWindow(ColumnWindowEventArgs args)
    {
        var remPixels = args.RemPixels > 0 ? args.RemPixels : _remPixels;
        if ((args.First, args.Last) != _columnWindow || remPixels != _remPixels)
        {
            (_columnWindow, _remPixels) = ((args.First, args.Last), remPixels);
            RenderRows();
        }
    }

    private RenderFragment SpacerCell(DrawnColumn drawn) => builder =>
    {
        builder.OpenElement(0, "td");
        builder.SetKey(drawn.SpacerKey);
        builder.AddAttribute(1, "aria-hidden", "true");
        builder.AddAttribute(2, "data-part", "column-spacer");
        builder.AddAttribute(3, "class", "tb-data-grid__column-spacer");
        builder.CloseElement();
    };
}
