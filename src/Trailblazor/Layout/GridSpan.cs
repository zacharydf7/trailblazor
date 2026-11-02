using System.Globalization;

namespace Trailblazor;

/// <summary>How much of its row a <see cref="TbGridItem"/> takes: a number of the grid's 12 columns, such as
/// <c>Span="6"</c>, or <see cref="Grow"/> or <see cref="Auto"/>.</summary>
public readonly record struct GridSpan
{
    private readonly Fit _fit;

    private GridSpan(int columns, Fit fit) => (Columns, _fit) = (columns, fit);

    private enum Fit
    {
        Columns,
        Grow,
        Auto,
    }

    /// <summary>Takes the room left in its row, shared evenly with the row's other growing items. It joins any row
    /// with room to spare, so to put it on a new row, fill the row before it, or give it its own grid.</summary>
    public static GridSpan Grow { get; } = new(0, Fit.Grow);

    /// <summary>Takes as much room as its content needs, up to the whole row.</summary>
    public static GridSpan Auto { get; } = new(0, Fit.Auto);

    // The number of columns, when it is a span of columns.
    internal int Columns { get; }

    internal bool IsColumns => _fit == Fit.Columns;

    /// <summary>A span of this many of the grid's 12 columns.</summary>
    public static implicit operator GridSpan(int columns) => new(columns, Fit.Columns);

    /// <inheritdoc />
    public override string ToString() => _fit == Fit.Columns ? Columns.ToString(CultureInfo.InvariantCulture)
        : _fit.ToString();
}
