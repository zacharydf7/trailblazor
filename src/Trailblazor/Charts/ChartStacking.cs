namespace Trailblazor;

/// <summary>How a chart's bar series, and its area series, share a place along the chart.</summary>
public enum ChartStacking
{
    /// <summary>Bars side by side, each from zero, and areas each from zero, drawn over the series before them. The
    /// default.</summary>
    None,

    /// <summary>One on top of another, so each bar's or area's height is its value and the stack's is their sum.
    /// Values below zero stack downward from zero.</summary>
    Stacked,

    /// <summary>One on top of another as shares of the place's total, so every stack is the same height. A value
    /// below zero has no share and takes no room.</summary>
    Percent,
}
