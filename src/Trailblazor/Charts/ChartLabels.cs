namespace Trailblazor;

/// <summary>The words a <see cref="TbChart{TItem}"/> shows and says to screen readers. The chart's own
/// <c>Title</c> or <c>AriaLabel</c> names it.</summary>
public sealed record ChartLabels
{
    /// <summary>The text of the control under the chart that opens its data table. Leave it null for the app's text
    /// for <see cref="TbStrings.ChartTable"/>.</summary>
    public string? Table { get; init; }

    /// <summary>Shown in place of the plot when the chart has no items or no series. Leave it null for the app's text
    /// for <see cref="TbStrings.ChartEmpty"/>.</summary>
    public string? Empty { get; init; }

    /// <summary>What screen readers hear for the drawing, with <c>{0}</c> for the kind of chart and <c>{1}</c> for
    /// its series' names. Leave it null for the app's text for <see cref="TbStrings.ChartSummary"/>.</summary>
    public string? Summary { get; init; }

    /// <summary>The kind of a chart with only bar series, for <see cref="Summary"/>. Leave it null for the app's text
    /// for <see cref="TbStrings.ChartBars"/>.</summary>
    public string? Bars { get; init; }

    /// <summary>The kind of a chart with only line series, for <see cref="Summary"/>. Leave it null for the app's
    /// text for <see cref="TbStrings.ChartLines"/>.</summary>
    public string? Lines { get; init; }

    /// <summary>The kind of a chart with both bar and line series, for <see cref="Summary"/>. Leave it null for the
    /// app's text for <see cref="TbStrings.ChartBarsAndLines"/>.</summary>
    public string? BarsAndLines { get; init; }
}
