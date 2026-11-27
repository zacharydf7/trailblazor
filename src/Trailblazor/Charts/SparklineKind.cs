namespace Trailblazor;

/// <summary>How a <see cref="TbSparkline{TValue}"/> draws its values.</summary>
public enum SparklineKind
{
    /// <summary>A line through the values, with the lowest of them at the bottom and the highest at the top.
    /// </summary>
    Line,

    /// <summary>A line with the area between it and zero filled in a tint of its color.</summary>
    Area,

    /// <summary>A bar from zero for each value.</summary>
    Bars,
}
