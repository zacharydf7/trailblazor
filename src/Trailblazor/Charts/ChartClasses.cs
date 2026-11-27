namespace Trailblazor;

/// <summary>Extra CSS classes for the main parts inside a <see cref="TbChart{TItem}"/> or a
/// <see cref="TbPieChart{TItem}"/>. Style the others by their <c>data-part</c>, under a class on the chart.</summary>
public sealed record ChartClasses
{
    /// <summary>Added to the title above the chart (<c>data-part="title"</c>).</summary>
    public string? Title { get; init; }

    /// <summary>Added to the list of series or slices and their colors (<c>data-part="legend"</c>).</summary>
    public string? Legend { get; init; }

    /// <summary>Added to the area the series or the pie is drawn in (<c>data-part="plot"</c>).</summary>
    public string? Plot { get; init; }

    /// <summary>Added to the data table's disclosure under the chart (<c>data-part="table"</c>).</summary>
    public string? Table { get; init; }
}
