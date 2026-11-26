namespace Trailblazor;

/// <summary>How a chart's bar series share a place along the chart.</summary>
public enum ChartStacking
{
    /// <summary>Side by side, each bar from zero. The default.</summary>
    None,

    /// <summary>One on top of another, so each bar's height is its value and the stack's is their sum. Values below
    /// zero stack downward from zero.</summary>
    Stacked,

    /// <summary>One on top of another as shares of the place's total, so every stack is the same height. A value
    /// below zero has no share and takes no room.</summary>
    Percent,
}
