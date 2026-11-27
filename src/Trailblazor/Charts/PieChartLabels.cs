namespace Trailblazor;

/// <summary>The words a <see cref="TbPieChart{TItem}"/> shows and says to screen readers. The chart's own
/// <c>Title</c> or <c>AriaLabel</c> names it.</summary>
public sealed record PieChartLabels
{
    /// <summary>The text of the control under the chart that opens its data table. Leave it null for the app's text
    /// for <see cref="TbStrings.ChartTable"/>.</summary>
    public string? Table { get; init; }

    /// <summary>Shown in place of the pie when no item has a value above zero. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartEmpty"/>.</summary>
    public string? Empty { get; init; }

    /// <summary>What screen readers hear for the drawing, with <c>{0}</c> for the kind of chart and <c>{1}</c> for
    /// its slices' labels. Leave it null for the app's text for <see cref="TbStrings.ChartSummary"/>.</summary>
    public string? Summary { get; init; }

    /// <summary>The kind of a chart without a hole, for <see cref="Summary"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartPie"/>.</summary>
    public string? Pie { get; init; }

    /// <summary>The kind of a chart with a hole, for <see cref="Summary"/>. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartDonut"/>.</summary>
    public string? Donut { get; init; }

    /// <summary>What screen readers hear for a slice the keys are on, with <c>{0}</c> for its label, <c>{1}</c> for
    /// its value and share, <c>{2}</c> for its position among the items and <c>{3}</c> for how many items there are.
    /// Leave it null for the app's text for <see cref="TbStrings.ChartSlice"/>.</summary>
    public string? Slice { get; init; }

    /// <summary>How a slice's value and share are written together in the tooltip and in a slice's name, with
    /// <c>{0}</c> for the value and <c>{1}</c> for the share. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartValueAndShare"/>.</summary>
    public string? ValueAndShare { get; init; }

    /// <summary>The heading of the data table's column of values, such as "Jobs". Leave it null for the app's text
    /// for <see cref="TbStrings.ChartValue"/>.</summary>
    public string? Value { get; init; }

    /// <summary>The heading of the data table's column of shares. Leave it null for the app's text for
    /// <see cref="TbStrings.ChartShare"/>.</summary>
    public string? Share { get; init; }
}
