namespace Trailblazor;

/// <summary>Lays parts of a <see cref="DockLayout"/> side by side, or stacked when <see cref="Vertical"/>, with a
/// separator between each pair.</summary>
public sealed record DockSplit : DockNode
{
    /// <summary>Creates a split.</summary>
    /// <param name="children">The parts, in reading order: groups and other splits.</param>
    public DockSplit(params IEnumerable<DockNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        Children = [.. children];
        if (Children.Contains(null!))
        {
            throw new ArgumentException("A split can't hold a null part.", nameof(children));
        }
    }

    /// <summary>The parts, in reading order.</summary>
    public IReadOnlyList<DockNode> Children { get; }

    /// <summary>Stacks the parts top to bottom instead of side by side.</summary>
    public bool Vertical { get; init; }

    /// <summary>Whether the other split has equal parts in the same order, the same direction and the same size.
    /// </summary>
    public bool Equals(DockSplit? other) =>
        base.Equals(other) && Vertical == other.Vertical && Children.SequenceEqual(other.Children);

    /// <summary>A hash that equal splits share.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        hash.Add(Vertical);
        foreach (var child in Children)
        {
            hash.Add(child);
        }

        return hash.ToHashCode();
    }
}
