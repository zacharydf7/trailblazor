namespace Trailblazor;

/// <summary>Panels that share one place in a <see cref="DockLayout"/> as tabs, with one of them showing. A panel is
/// named by its id, which can't be empty or have white space in it.</summary>
public sealed record DockGroup : DockNode
{
    /// <summary>Creates a group of tabbed panels.</summary>
    /// <param name="panels">The panels' ids, in tab order.</param>
    /// <exception cref="ArgumentException">An id is empty or has white space in it.</exception>
    public DockGroup(params IEnumerable<string> panels)
    {
        ArgumentNullException.ThrowIfNull(panels);
        Panels = [.. panels];
        foreach (var panel in Panels)
        {
            if (string.IsNullOrEmpty(panel) || panel.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException("A panel's id can't be empty or have white space in it.", nameof(panels));
            }
        }
    }

    /// <summary>The panels' ids, in tab order.</summary>
    public IReadOnlyList<string> Panels { get; }

    /// <summary>The id of the panel that shows. Left null, or set to a panel the group doesn't hold, it is the first
    /// panel.</summary>
    public string? Active
    {
        get => field is not null && Panels.Contains(field) ? field : Panels.FirstOrDefault();
        init;
    }

    /// <summary>Whether the other group has the same panels in the same order, the same showing panel and the same
    /// size.</summary>
    public bool Equals(DockGroup? other) =>
        base.Equals(other) && Active == other.Active && Panels.SequenceEqual(other.Panels);

    /// <summary>A hash that equal groups share.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var panel in Panels)
        {
            hash.Add(panel);
        }

        return hash.ToHashCode();
    }
}
