namespace Trailblazor;

/// <summary>How a chart places its items along it.</summary>
public enum ChartScale
{
    /// <summary>Each item is one place, and the places are evenly spaced, whatever the items' <c>X</c> values are.
    /// The default.</summary>
    Even,

    /// <summary>Each item is placed by its <c>X</c> value, which must be a number or a <see cref="DateTime"/>,
    /// <see cref="DateTimeOffset"/> or <see cref="DateOnly"/>, so items twice as far apart in value sit twice as far
    /// apart, and the axis marks round values. Give the items in order along the scale.</summary>
    Continuous,
}
