namespace Trailblazor;

/// <summary>A panel put away on a strip along an edge of a <see cref="TbDockManager"/>, where only a button with its
/// title shows until the user slides the panel out over the dock.</summary>
public sealed record DockAutoHidden
{
    // The least and the most of the dock a slid-out panel takes, in percent.
    private const double SmallestSize = 10;
    private const double LargestSize = 90;

    /// <summary>Creates a panel's place on a strip.</summary>
    /// <param name="panel">The panel's id.</param>
    /// <param name="edge">The edge of the dock its strip runs along: Start, End, Top or Bottom.</param>
    /// <exception cref="ArgumentException">The id is empty or has white space in it.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The edge is Before or After, which put a panel beside another
    /// panel.</exception>
    public DockAutoHidden(string panel, DockPosition edge)
    {
        if (string.IsNullOrEmpty(panel) || panel.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("A panel's id can't be empty or have white space in it.", nameof(panel));
        }

        if (edge is not (DockPosition.Start or DockPosition.End or DockPosition.Top or DockPosition.Bottom))
        {
            throw new ArgumentOutOfRangeException(nameof(edge), edge,
                "A strip runs along an edge of the dock: Start, End, Top or Bottom.");
        }

        (Panel, Edge) = (panel, edge);
    }

    /// <summary>The panel's id.</summary>
    public string Panel { get; }

    /// <summary>The edge of the dock the panel's strip runs along.</summary>
    public DockPosition Edge { get; }

    /// <summary>How much of the dock the panel takes when it slides out, in percent of the dock's width for a strip
    /// at the start or the end and of its height for one at the top or the bottom. Defaults to 25. A layout keeps
    /// it between 10 and 90.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size isn't a finite number.</exception>
    public double Size
    {
        get;
        init => field = double.IsFinite(value) ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "A size is a finite number.");
    } = 25;

    // The place with its size between the smallest and the largest a slid-out panel takes.
    internal DockAutoHidden Tidied() => Size is >= SmallestSize and <= LargestSize ? this
        : this with { Size = Math.Clamp(Size, SmallestSize, LargestSize) };

    // The box the panel takes when it slides out, in percent of the dock.
    internal DockBox Box => Edge switch
    {
        DockPosition.Start => new(0, 0, Size, 100),
        DockPosition.End => new(100 - Size, 0, Size, 100),
        DockPosition.Top => new(0, 0, 100, Size),
        _ => new(0, 100 - Size, 100, Size),
    };
}
