namespace Trailblazor;

/// <summary>How a line series joins its points.</summary>
public enum ChartCurve
{
    /// <summary>A straight line from each point to the next. The default.</summary>
    Straight,

    /// <summary>A curve through every point that never rises above or dips below the points it joins, so it shows
    /// no value the data doesn't have.</summary>
    Smooth,

    /// <summary>Each value held level until the next point, then a jump: for values that change at once, such as a
    /// price or a count of seats.</summary>
    Stepped,
}
