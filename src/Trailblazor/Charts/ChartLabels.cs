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

    /// <summary>Shown in place of the plot when every series is hidden. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartAllHidden"/>.</summary>
    public string? AllHidden { get; init; }

    /// <summary>What screen readers hear after a hidden series' name in a legend that has no buttons. Leave it null
    /// for the app's text for <see cref="TbStrings.ChartHidden"/>.</summary>
    public string? Hidden { get; init; }

    /// <summary>What screen readers hear for the drawing, with <c>{0}</c> for the kind of chart and <c>{1}</c> for
    /// its series' names. Leave it null for the app's text for <see cref="TbStrings.ChartSummary"/>.</summary>
    public string? Summary { get; init; }

    /// <summary>What screen readers hear for a point the keys are on, with <c>{0}</c> for its place, <c>{1}</c> for
    /// its series, <c>{2}</c> for its value, <c>{3}</c> for its position along the chart and <c>{4}</c> for how many
    /// places there are. Leave it null for the app's text for <see cref="TbStrings.ChartPoint"/>.</summary>
    public string? Point { get; init; }

    /// <summary>What the tooltip shows for a series that has no value at a place. Leave it null for the app's text
    /// for <see cref="TbStrings.ChartNoValue"/>.</summary>
    public string? NoValue { get; init; }

    /// <summary>The kind of a chart with only bar series, for <see cref="Summary"/>. Leave it null for the app's text
    /// for <see cref="TbStrings.ChartBars"/>.</summary>
    public string? Bars { get; init; }

    /// <summary>The kind of a chart with only line series, or with a mix of lines, areas and scatters, for
    /// <see cref="Summary"/>. Leave it null for the app's text for <see cref="TbStrings.ChartLines"/>.</summary>
    public string? Lines { get; init; }

    /// <summary>The kind of a chart with only area series, for <see cref="Summary"/>. Leave it null for the app's
    /// text for <see cref="TbStrings.ChartAreas"/>.</summary>
    public string? Areas { get; init; }

    /// <summary>The kind of a chart with only scatter series, for <see cref="Summary"/>. Leave it null for the
    /// app's text for <see cref="TbStrings.ChartScatter"/>.</summary>
    public string? Scatter { get; init; }

    /// <summary>The kind of a chart with bar series beside line, area or scatter series, for
    /// <see cref="Summary"/>. Leave it null for the app's text for <see cref="TbStrings.ChartBarsAndLines"/>.
    /// </summary>
    public string? BarsAndLines { get; init; }

    /// <summary>What the chart writes for a reference line, with <c>{0}</c> for its name and <c>{1}</c> for its
    /// value or its place along the items. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartReferenceLine"/>.</summary>
    public string? ReferenceLine { get; init; }

    /// <summary>What the chart writes for a reference band, with <c>{0}</c> for its name and <c>{1}</c> and
    /// <c>{2}</c> for the values or places it runs between. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartReferenceBand"/>.</summary>
    public string? ReferenceBand { get; init; }

    /// <summary>Names the button that zooms in on a zoomable chart. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartZoomIn"/>.</summary>
    public string? ZoomIn { get; init; }

    /// <summary>Names the button that zooms out. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartZoomOut"/>.</summary>
    public string? ZoomOut { get; init; }

    /// <summary>Names the button that moves the view back along the scale. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartMoveBack"/>.</summary>
    public string? MoveBack { get; init; }

    /// <summary>Names the button that moves the view forward along the scale. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartMoveForward"/>.</summary>
    public string? MoveForward { get; init; }

    /// <summary>The text of the button that shows all of the scale again. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartShowAll"/>.</summary>
    public string? ShowAll { get; init; }

    /// <summary>Says what part of the scale is shown, with <c>{0}</c> and <c>{1}</c> for its ends. Leave it null for
    /// the app's text for <see cref="TbStrings.ChartView"/>.</summary>
    public string? View { get; init; }
}
