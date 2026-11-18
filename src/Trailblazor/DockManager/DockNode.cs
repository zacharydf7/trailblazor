namespace Trailblazor;

/// <summary>A part of a <see cref="DockLayout"/>: a <see cref="DockGroup"/> of tabbed panels, or a
/// <see cref="DockSplit"/> that lays other parts side by side or stacked.</summary>
public abstract record DockNode
{
    private protected DockNode()
    {
    }

    /// <summary>How much of its split this part takes, relative to its siblings: a part with 2 is twice as large as
    /// one with 1. Defaults to 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size isn't a number above zero.</exception>
    public double Size
    {
        get;
        init => field = value > 0 && double.IsFinite(value) ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "A size is a number above zero.");
    } = 1;
}
