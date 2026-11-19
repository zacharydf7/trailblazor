namespace Trailblazor;

/// <summary>A window that floats above the docked groups of a <see cref="DockLayout"/>: a group of tabbed panels and
/// the box it takes, in percent of the dock.</summary>
public sealed record DockFloat
{
    // The least a window takes of the dock's width and of its height, in percent.
    private const double SmallestSize = 10;

    /// <summary>Creates a floating window. A layout keeps its box inside the dock, at a tenth of the dock's width
    /// and height or more.</summary>
    /// <param name="group">Its panels, as tabs. A group's <see cref="DockNode.Size"/> and
    /// <see cref="DockGroup.Maximized"/> mean nothing in a window.</param>
    /// <param name="x">Where the box starts, in percent of the dock's width, from the side where text starts.</param>
    /// <param name="y">Where the box starts, in percent of the dock's height, from the top.</param>
    /// <param name="width">The box's width, in percent of the dock's.</param>
    /// <param name="height">The box's height, in percent of the dock's.</param>
    /// <exception cref="ArgumentOutOfRangeException">A number isn't finite.</exception>
    public DockFloat(DockGroup group, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(group);
        Group = group;
        (X, Y, Width, Height) = (Finite(x, nameof(x)), Finite(y, nameof(y)), Finite(width, nameof(width)),
            Finite(height, nameof(height)));
    }

    /// <summary>The window's panels, as tabs, with the one that shows.</summary>
    public DockGroup Group { get; }

    /// <summary>Where the box starts, in percent of the dock's width, from the side where text starts.</summary>
    public double X { get; }

    /// <summary>Where the box starts, in percent of the dock's height, from the top.</summary>
    public double Y { get; }

    /// <summary>The box's width, in percent of the dock's.</summary>
    public double Width { get; }

    /// <summary>The box's height, in percent of the dock's.</summary>
    public double Height { get; }

    // The window with these panels, and its box inside the dock at the smallest size or more.
    internal DockFloat Tidied(IReadOnlyList<string> panels)
    {
        var width = Math.Clamp(Width, SmallestSize, 100);
        var height = Math.Clamp(Height, SmallestSize, 100);
        var (x, y) = (Math.Clamp(X, 0, 100 - width), Math.Clamp(Y, 0, 100 - height));
        var plain = panels.Count == Group.Panels.Count && Group.Size == 1 && !Group.Maximized;
        return plain && (x, y, width, height) == (X, Y, Width, Height) ? this
            : new DockFloat(plain ? Group : new DockGroup(panels) { Active = Group.Active }, x, y, width, height);
    }

    // The window with another group, or in another box.
    internal DockFloat With(DockGroup group) => new(group, X, Y, Width, Height);

    internal DockFloat At(double x, double y, double width, double height) => new(Group, x, y, width, height);

    private static double Finite(double value, string name) => double.IsFinite(value) ? value
        : throw new ArgumentOutOfRangeException(name, value, "A window's box is made of finite numbers.");
}
