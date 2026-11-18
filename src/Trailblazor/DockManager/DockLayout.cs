using System.Diagnostics.CodeAnalysis;

namespace Trailblazor;

/// <summary>Where each panel of a <see cref="TbDockManager"/> is: a tree of <see cref="DockSplit"/>s and
/// <see cref="DockGroup"/>s, and the panels the user closed. A layout never changes; each method that changes
/// something returns a new layout. <see cref="ToJson"/> and <see cref="TryParse"/> save and restore one.</summary>
public sealed record DockLayout
{
    /// <summary>Creates a layout. Its tree is tidied as it's taken: a panel listed twice stays where it's first
    /// listed, a group with no panels is dropped, a split with one part gives its place to that part, and a split
    /// inside a split of the same direction joins it.</summary>
    /// <param name="root">The docked panels: a group, or a split of groups and other splits. Null docks nothing.
    /// </param>
    /// <param name="closed">The ids of the panels the user closed, which stay hidden until
    /// <see cref="Open"/> shows them again.</param>
    public DockLayout(DockNode? root = null, IEnumerable<string>? closed = null)
    {
        var docked = new HashSet<string>(StringComparer.Ordinal);
        Root = root is null ? null : Tidy(root, docked);
        Closed = [.. (closed ?? []).Distinct().Where(panel => !docked.Contains(panel))];
    }

    /// <summary>The docked panels: a group, or a split of groups and other splits. Null when nothing is docked.
    /// </summary>
    public DockNode? Root { get; }

    /// <summary>The ids of the panels the user closed, oldest first.</summary>
    public IReadOnlyList<string> Closed { get; }

    /// <summary>The groups, in reading order.</summary>
    public IEnumerable<DockGroup> Groups => GroupsIn(Root);

    /// <summary>The group that holds the panel with this id, or null when that panel isn't docked.</summary>
    public DockGroup? GroupOf(string panel) => Groups.FirstOrDefault(group => group.Panels.Contains(panel));

    /// <summary>Shows the panel with this id. A docked one becomes the showing tab of its group. A closed one, or
    /// one the layout doesn't hold, joins the largest group as its showing tab.</summary>
    public DockLayout Open(string panel)
    {
        var placed = Place(panel);
        var group = placed.GroupOf(panel)!;
        return group.Active == panel ? placed : placed.Replace(group, group with { Active = panel });
    }

    /// <summary>Closes the panel with this id: it leaves its group and joins <see cref="Closed"/>. When it was the
    /// showing tab, the tab after it shows instead, or the one before it when it was last. A panel that isn't docked
    /// changes nothing.</summary>
    public DockLayout Close(string panel)
    {
        if (GroupOf(panel) is not { } group)
        {
            return this;
        }

        var left = group.Panels.Where(each => each != panel).ToList();
        var index = group.Panels.TakeWhile(each => each != panel).Count();
        var active = group.Active == panel ? left.ElementAtOrDefault(Math.Min(index, left.Count - 1)) : group.Active;
        return new DockLayout(Swap(Root!, group, new DockGroup(left) { Active = active, Size = group.Size }),
            Closed.Append(panel));
    }

    /// <summary>Writes the layout as JSON, which <see cref="TryParse"/> reads back.</summary>
    public string ToJson() => DockLayoutJson.Write(this);

    /// <summary>Reads a layout that <see cref="ToJson"/> wrote, giving false for text that isn't one.</summary>
    /// <param name="json">The JSON. Text saved by an app can be stale or damaged, so anything that isn't a layout
    /// gives false instead of throwing.</param>
    /// <param name="layout">The layout, when the JSON holds one.</param>
    public static bool TryParse([NotNullWhen(true)] string? json, [NotNullWhen(true)] out DockLayout? layout) =>
        (layout = DockLayoutJson.Read(json)) is not null;

    /// <summary>Whether the other layout has an equal tree and the same closed panels in the same order.</summary>
    public bool Equals(DockLayout? other) =>
        other is not null && Equals(Root, other.Root) && Closed.SequenceEqual(other.Closed);

    /// <summary>A hash that equal layouts share.</summary>
    public override int GetHashCode() => HashCode.Combine(Root, Closed.Count);

    // The layout with the panel docked: where it is, or else at the end of the largest group, without showing it.
    internal DockLayout Place(string panel)
    {
        if (GroupOf(panel) is not null)
        {
            return this;
        }

        var largest = Arrange(_ => true).Groups.MaxBy(box => box.Box.Width * box.Box.Height)?.Group;
        return largest is null ? new DockLayout(new DockGroup(panel), Closed)
            : new DockLayout(Swap(Root!, largest,
                new DockGroup([.. largest.Panels, panel]) { Active = largest.Active, Size = largest.Size }), Closed);
    }

    // The layout with one separator moved: the parts on its two sides share their combined size anew, the one
    // before taking the given fraction of it.
    internal DockLayout Resize(DockSplit split, int before, int after, double fraction)
    {
        var total = split.Children[before].Size + split.Children[after].Size;
        var children = split.Children.ToArray();
        children[before] = children[before] with { Size = total * fraction };
        children[after] = children[after] with { Size = total * (1 - fraction) };
        return Replace(split, new DockSplit(children) { Vertical = split.Vertical, Size = split.Size });
    }

    // Boxes for the groups that show a panel and for the separators between them, in reading order. A part with no
    // panel to show leaves its space to its siblings.
    internal DockArrangement Arrange(Func<string, bool> shows)
    {
        var groups = new List<DockGroupBox>();
        var separators = new List<DockSeparatorBox>();
        if (Root is not null && Shows(Root, shows))
        {
            Arrange(Root, new DockBox(0, 0, 100, 100), shows, groups, separators);
        }

        return new DockArrangement(groups, separators);
    }

    private DockLayout Replace(DockNode node, DockNode replacement) =>
        new(Swap(Root!, node, replacement), Closed);

    private static void Arrange(DockNode node, DockBox box, Func<string, bool> shows, List<DockGroupBox> groups,
        List<DockSeparatorBox> separators)
    {
        if (node is DockGroup group)
        {
            var panels = group.Panels.Where(shows).ToList();
            groups.Add(new DockGroupBox(group, box, panels, shows(group.Active!) ? group.Active! : panels[0]));
            return;
        }

        var split = (DockSplit)node;
        var shown = Enumerable.Range(0, split.Children.Count).Where(index => Shows(split.Children[index], shows))
            .ToList();
        var total = shown.Sum(index => split.Children[index].Size);
        var start = split.Vertical ? box.Y : box.X;
        (int Index, DockBox Box)? last = null;
        foreach (var index in shown)
        {
            var length = (split.Vertical ? box.Height : box.Width) * split.Children[index].Size / total;
            var child = split.Vertical ? box with { Y = start, Height = length }
                : box with { X = start, Width = length };
            if (last is { } before)
            {
                separators.Add(new DockSeparatorBox(split, before.Index, index, before.Box, child));
            }

            Arrange(split.Children[index], child, shows, groups, separators);
            last = (index, child);
            start += length;
        }
    }

    private static bool Shows(DockNode node, Func<string, bool> shows) =>
        GroupsIn(node).Any(group => group.Panels.Any(shows));

    private static IEnumerable<DockGroup> GroupsIn(DockNode? node) => node switch
    {
        DockGroup group => [group],
        DockSplit split => split.Children.SelectMany(GroupsIn),
        _ => [],
    };

    // The tree with one node, found by reference, replaced.
    private static DockNode Swap(DockNode node, DockNode target, DockNode replacement) =>
        ReferenceEquals(node, target) ? replacement
        : node is DockSplit split ? new DockSplit(split.Children.Select(child => Swap(child, target, replacement)))
            { Vertical = split.Vertical, Size = split.Size }
        : node;

    private static DockNode? Tidy(DockNode node, HashSet<string> docked)
    {
        if (node is DockGroup group)
        {
            var panels = group.Panels.Where(docked.Add).ToList();
            return panels.Count == 0 ? null
                : panels.Count == group.Panels.Count ? group
                : new DockGroup(panels) { Active = group.Active, Size = group.Size };
        }

        var split = (DockSplit)node;
        var children = new List<DockNode>();
        foreach (var child in split.Children.Select(child => Tidy(child, docked)).OfType<DockNode>())
        {
            // A split of the same direction joins this one, its parts sharing the size it had.
            if (child is DockSplit inner && inner.Vertical == split.Vertical)
            {
                var total = inner.Children.Sum(each => each.Size);
                children.AddRange(inner.Children.Select(each => each with { Size = child.Size * each.Size / total }));
            }
            else
            {
                children.Add(child);
            }
        }

        return children.Count switch
        {
            0 => null,
            1 => children[0] with { Size = split.Size },
            _ => children.SequenceEqual(split.Children, ReferenceEqualityComparer.Instance) ? split
                : new DockSplit(children) { Vertical = split.Vertical, Size = split.Size },
        };
    }
}
