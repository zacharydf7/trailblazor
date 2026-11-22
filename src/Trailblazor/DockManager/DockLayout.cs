using System.Diagnostics.CodeAnalysis;

namespace Trailblazor;

/// <summary>Where each panel of a <see cref="TbDockManager"/> is: a tree of <see cref="DockSplit"/>s and
/// <see cref="DockGroup"/>s, the windows that float above it, the panels put away on the strips along its edges,
/// and the panels the user closed. A layout never changes; each method that changes something returns a new layout.
/// <see cref="ToJson"/> and <see cref="TryParse"/> save and restore one.</summary>
public sealed record DockLayout
{
    /// <summary>Creates a layout. Its tree is tidied as it's taken: a panel listed twice stays where it's first
    /// listed, a group with no panels is dropped, a split with one part gives its place to that part, a split
    /// inside a split of the same direction joins it, and only the first group marked
    /// <see cref="DockGroup.Maximized"/> stays so, and only while the layout has another group for it to cover.
    /// A panel that is docked isn't also in a window, a window left with no panels is dropped, and each window's
    /// box is kept inside the dock. A panel that is docked or in a window isn't also on a strip.</summary>
    /// <param name="root">The docked panels: a group, or a split of groups and other splits. Null docks nothing.
    /// </param>
    /// <param name="closed">The ids of the panels the user closed, which stay hidden until
    /// <see cref="Open"/> shows them again.</param>
    /// <param name="floating">The windows that float above the docked panels, back to front.</param>
    /// <param name="autoHidden">The panels put away on the strips along the dock's edges, in the order their buttons
    /// show.</param>
    /// <param name="revealed">The id of the auto-hidden panel that is slid out over the dock, or null when none is.
    /// </param>
    /// <exception cref="ArgumentException">A window or an auto-hidden panel is null.</exception>
    public DockLayout(DockNode? root = null, IEnumerable<string>? closed = null,
        IEnumerable<DockFloat>? floating = null, IEnumerable<DockAutoHidden>? autoHidden = null,
        string? revealed = null)
    {
        var placed = new HashSet<string>(StringComparer.Ordinal);
        Root = root is null ? null : Tidy(root, placed);
        var alone = Root is DockGroup;
        foreach (var extra in Groups.Where(group => group.Maximized).Skip(alone ? 0 : 1).ToList())
        {
            Root = Swap(Root!, extra, extra with { Maximized = false });
        }

        var windows = new List<DockFloat>();
        foreach (var window in floating ?? [])
        {
            var panels = (window ?? throw new ArgumentException("A layout can't hold a null window.",
                nameof(floating))).Group.Panels.Where(placed.Add).ToList();
            if (panels.Count > 0)
            {
                windows.Add(window.Tidied(panels));
            }
        }

        Floating = windows;
        var strips = new List<DockAutoHidden>();
        foreach (var hidden in autoHidden ?? [])
        {
            if (placed.Add((hidden ?? throw new ArgumentException("A layout can't hold a null auto-hidden panel.",
                nameof(autoHidden))).Panel))
            {
                strips.Add(hidden.Tidied());
            }
        }

        AutoHidden = strips;
        Revealed = strips.Any(hidden => hidden.Panel == revealed) ? revealed : null;
        Closed = [.. (closed ?? []).Distinct().Where(panel => !placed.Contains(panel))];
    }

    /// <summary>The docked panels: a group, or a split of groups and other splits. Null when nothing is docked.
    /// </summary>
    public DockNode? Root { get; }

    /// <summary>The ids of the panels the user closed, oldest first.</summary>
    public IReadOnlyList<string> Closed { get; }

    /// <summary>The windows that float above the docked panels, back to front.</summary>
    public IReadOnlyList<DockFloat> Floating { get; }

    /// <summary>The panels put away on the strips along the dock's edges, in the order their buttons show.
    /// </summary>
    public IReadOnlyList<DockAutoHidden> AutoHidden { get; }

    /// <summary>The id of the auto-hidden panel that is slid out over the dock, or null when none is.</summary>
    public string? Revealed { get; }

    /// <summary>The docked groups, in reading order.</summary>
    public IEnumerable<DockGroup> Groups => GroupsIn(Root);

    /// <summary>The group that fills the dock and covers the others, or null when every group shows.</summary>
    public DockGroup? Maximized => Groups.FirstOrDefault(group => group.Maximized);

    /// <summary>The group that holds the panel with this id, docked or in a window, or null when the panel is on
    /// a strip, closed or not in the layout.</summary>
    public DockGroup? GroupOf(string panel) => Groups.Concat(Floating.Select(window => window.Group))
        .FirstOrDefault(group => group.Panels.Contains(panel));

    /// <summary>Shows the panel with this id. A docked one becomes the showing tab of its group, and one in a window
    /// the showing tab of its window, which comes to the front. One on a strip slides out over the dock. A closed
    /// one, or one the layout doesn't hold, joins the largest group as its showing tab. A group that covers the
    /// panel's group is restored.</summary>
    public DockLayout Open(string panel)
    {
        if (StripOf(panel) is not null)
        {
            return Revealed == panel ? this : new(Root, Closed, Floating, AutoHidden, panel);
        }

        var placed = Place(panel);
        if (placed.FloatOf(panel) is { } window)
        {
            return placed.InFront(window, window.With(Holding(window.Group, window.Group.Panels, panel)));
        }

        var group = placed.GroupOf(panel)!;
        return (group.Active == panel ? placed : placed.Replace(group, group with { Active = panel })).Uncovered(panel);
    }

    /// <summary>Closes the panel with this id: it leaves its group, its window or its strip and joins
    /// <see cref="Closed"/>. When it was the showing tab, the tab after it shows instead, or the one before it when
    /// it was last. A panel the layout shows nowhere changes nothing.</summary>
    public DockLayout Close(string panel) =>
        GroupOf(panel) is null && StripOf(panel) is null ? this : new(Without(panel), Closed.Append(panel),
            FloatingWithout(panel), AutoHidden.Where(hidden => hidden.Panel != panel), Revealed);

    /// <summary>Moves the panel with this id beside another panel: to a tab before or after the other's tab, as the
    /// group's showing tab, or to a new group on one side of the other's group, which takes half of that group's
    /// space. A closed panel, a floating one, one on a strip, or one the layout doesn't hold, is docked there. A
    /// target in a window takes the panel as a tab of that window, which comes to the front; a window has no sides,
    /// so a side of one puts the tab after the target's. A target the layout shows nowhere changes nothing. A group
    /// that covers the group the panel ends in is restored.</summary>
    /// <param name="panel">The id of the panel to move.</param>
    /// <param name="target">The id of the panel it goes beside.</param>
    /// <param name="position">Where it goes.</param>
    public DockLayout Move(string panel, string target, DockPosition position)
    {
        if (panel == target || GroupOf(target) is null)
        {
            return this;
        }

        var lifted = Lifted(panel);
        var group = lifted.GroupOf(target)!;
        var at = group.Panels.TakeWhile(each => each != target).Count() + (position == DockPosition.Before ? 0 : 1);
        var joined = Holding(group, [.. group.Panels.Take(at), panel, .. group.Panels.Skip(at)], panel);
        if (lifted.FloatOf(target) is { } window)
        {
            return lifted.InFront(window, window.With(joined));
        }

        return lifted.Replace(group, position is DockPosition.Before or DockPosition.After ? joined
            : Beside(group with { Size = 1 }, panel, position, group.Size)).Uncovered(panel);
    }

    /// <summary>Moves the panel with this id to a new group along an edge of the dock, which takes a quarter of the
    /// dock, or all of it when nothing else is docked. A closed panel, a floating one, one on a strip, or one the
    /// layout doesn't hold, is docked there. A group that filled the dock is restored.</summary>
    /// <param name="panel">The id of the panel to move.</param>
    /// <param name="edge">The edge: Start, End, Top or Bottom.</param>
    /// <exception cref="ArgumentOutOfRangeException">The edge is Before or After, which put a panel beside another
    /// panel.</exception>
    public DockLayout MoveToEdge(string panel, DockPosition edge)
    {
        if (edge is DockPosition.Before or DockPosition.After)
        {
            throw new ArgumentOutOfRangeException(nameof(edge), edge,
                "Before and After put a panel beside another panel; use Move for those.");
        }

        var lifted = Lifted(panel);
        return lifted.With(
            lifted.Root is { } rest ? Beside(rest with { Size = 3 }, panel, edge, 1) : new DockGroup(panel),
            lifted.Floating).Uncovered(panel);
    }

    /// <summary>Takes the panel with this id out of its group into a window of its own that floats in front of the
    /// others: half as wide and as tall as the dock, over its middle, and a little further down and along for each
    /// window already floating. A closed panel, one on a strip, or one the layout doesn't hold, floats the same way.
    /// </summary>
    public DockLayout Float(string panel)
    {
        // Each new window sits a little down and along from the last, so it doesn't hide the one before it.
        var step = 3 * (Floating.Count % 6);
        return FloatAt(panel, 25 + step, 25 + step, 50, 50);
    }

    /// <summary>Takes the panel with this id out of its group into a window of its own that floats in front of the
    /// others, in this box. A closed panel, one on a strip, or one the layout doesn't hold, floats the same way.
    /// </summary>
    /// <param name="panel">The id of the panel to float.</param>
    /// <param name="x">Where the box starts, in percent of the dock's width, from the side where text starts.</param>
    /// <param name="y">Where the box starts, in percent of the dock's height, from the top.</param>
    /// <param name="width">The box's width, in percent of the dock's.</param>
    /// <param name="height">The box's height, in percent of the dock's.</param>
    public DockLayout FloatAt(string panel, double x, double y, double width, double height)
    {
        var lifted = Lifted(panel);
        return lifted.With(lifted.Root, [.. lifted.Floating, new DockFloat(new DockGroup(panel), x, y, width, height)]);
    }

    /// <summary>Gives the window that holds the panel with this id another box, which the layout keeps inside the
    /// dock. The window keeps its panels and its place among the windows. A panel that isn't in a window changes
    /// nothing.</summary>
    /// <param name="panel">The id of a panel in the window.</param>
    /// <param name="x">Where the box starts, in percent of the dock's width, from the side where text starts.</param>
    /// <param name="y">Where the box starts, in percent of the dock's height, from the top.</param>
    /// <param name="width">The box's width, in percent of the dock's.</param>
    /// <param name="height">The box's height, in percent of the dock's.</param>
    /// <exception cref="ArgumentOutOfRangeException">A number isn't finite.</exception>
    public DockLayout MoveWindow(string panel, double x, double y, double width, double height) =>
        FloatOf(panel) is not { } window ? this : With(Root,
            Floating.Select(each => ReferenceEquals(each, window) ? each.At(x, y, width, height) : each));

    /// <summary>Puts the panel with this id away on the strip along an edge of the dock, after the panels already
    /// there: it leaves its group or its window, and only its title shows until <see cref="Open"/> or the user
    /// slides it out. A closed panel, or one the layout doesn't hold, goes to the strip the same way, and one
    /// already on a strip moves to this edge's and keeps its <see cref="DockAutoHidden.Size"/>.</summary>
    /// <param name="panel">The id of the panel to put away.</param>
    /// <param name="edge">The edge: Start, End, Top or Bottom.</param>
    /// <exception cref="ArgumentOutOfRangeException">The edge is Before or After, which put a panel beside another
    /// panel.</exception>
    public DockLayout AutoHide(string panel, DockPosition edge)
    {
        var hidden = StripOf(panel) is { } was ? new DockAutoHidden(panel, edge) { Size = was.Size }
            : new DockAutoHidden(panel, edge);
        var lifted = Lifted(panel);
        return new(lifted.Root, Closed, lifted.Floating,
            [.. AutoHidden.Where(each => each.Panel != panel), hidden], Revealed == panel ? null : Revealed);
    }

    /// <summary>Gives the auto-hidden panel with this id another size to slide out to, in percent of the dock, which
    /// the layout keeps between 10 and 90. A panel that isn't on a strip changes nothing.</summary>
    /// <param name="panel">The id of the panel on a strip.</param>
    /// <param name="size">How much of the dock it takes when it slides out, as <see cref="DockAutoHidden.Size"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The panel is on a strip and the size isn't a finite number.
    /// </exception>
    public DockLayout ResizeAutoHidden(string panel, double size) => StripOf(panel) is not { } hidden ? this
        : new(Root, Closed, Floating,
            AutoHidden.Select(each => ReferenceEquals(each, hidden) ? each with { Size = size } : each), Revealed);

    /// <summary>Slides the auto-hidden panel that is out over the dock back to its strip, and changes nothing when
    /// none is out.</summary>
    public DockLayout Conceal() => Revealed is null ? this : new(Root, Closed, Floating, AutoHidden);

    /// <summary>Shows the panel with this id and has its group fill the dock, covering the other groups, which
    /// keep their places. A panel that isn't docked changes nothing, and in a layout with one group the panel only
    /// shows, since there is nothing to cover.</summary>
    public DockLayout Maximize(string panel)
    {
        if (!Groups.Any(group => group.Panels.Contains(panel)))
        {
            return this;
        }

        var shown = Open(panel);
        var group = shown.GroupOf(panel)!;
        return shown.Replace(group, group with { Maximized = true });
    }

    /// <summary>Shows every group again when one fills the dock, and changes nothing otherwise.</summary>
    public DockLayout Restore() => Maximized is { } group ? Replace(group, group with { Maximized = false }) : this;

    /// <summary>The layout with only these panels: any other id leaves its group, its window or its strip as
    /// <see cref="Close"/> would take it out, and leaves <see cref="Closed"/> too, so the layout doesn't remember
    /// it. Use it on a saved layout to drop the panels a newer version of the app no longer has, since a layout
    /// otherwise keeps a place for every id it ever held.</summary>
    /// <param name="panels">The ids to keep, which are usually all the panels the app has now.</param>
    public DockLayout Retain(IEnumerable<string> panels)
    {
        var kept = panels.ToHashSet(StringComparer.Ordinal);
        var gone = Groups.Concat(Floating.Select(window => window.Group)).SelectMany(group => group.Panels)
            .Concat(AutoHidden.Select(hidden => hidden.Panel)).Where(panel => !kept.Contains(panel)).ToList();
        var layout = gone.Aggregate(this, (current, panel) => current.Close(panel));
        return new(layout.Root, layout.Closed.Where(kept.Contains), layout.Floating, layout.AutoHidden,
            layout.Revealed);
    }

    /// <summary>Writes the layout as JSON, which <see cref="TryParse"/> reads back.</summary>
    public string ToJson() => DockLayoutJson.Write(this);

    /// <summary>Reads a layout that <see cref="ToJson"/> wrote, giving false for text that isn't one.</summary>
    /// <param name="json">The JSON. Text saved by an app can be stale or damaged, so anything that isn't a layout
    /// gives false instead of throwing.</param>
    /// <param name="layout">The layout, when the JSON holds one.</param>
    public static bool TryParse([NotNullWhen(true)] string? json, [NotNullWhen(true)] out DockLayout? layout) =>
        (layout = DockLayoutJson.Read(json)) is not null;

    /// <summary>Whether the other layout has an equal tree, equal windows in the same order, the same closed
    /// panels in the same order, equal strips and the same panel slid out.</summary>
    public bool Equals(DockLayout? other) => other is not null && Equals(Root, other.Root)
        && Closed.SequenceEqual(other.Closed) && Floating.SequenceEqual(other.Floating)
        && AutoHidden.SequenceEqual(other.AutoHidden) && Revealed == other.Revealed;

    /// <summary>A hash that equal layouts share.</summary>
    public override int GetHashCode() => HashCode.Combine(Root, Closed.Count);

    // The layout with the panel in it: where it is, or else at the end of the largest group, without showing it.
    internal DockLayout Place(string panel)
    {
        if (GroupOf(panel) is not null || StripOf(panel) is not null)
        {
            return this;
        }

        var largest = Arrange(_ => true).Groups.MaxBy(box => box.Box.Width * box.Box.Height)?.Group;
        return largest is null ? With(new DockGroup(panel), Floating)
            : Replace(largest, Holding(largest, [.. largest.Panels, panel], largest.Active));
    }

    // The layout with the window of a floating panel in front of the others.
    internal DockLayout Raise(string panel) => FloatOf(panel) is { } window ? InFront(window, window) : this;

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

        // A group has nothing to cover when it's the only one that shows.
        var maximized = groups.Count > 1 ? groups.FindIndex(box => box.Group.Maximized) : -1;
        var floats = new List<DockGroupBox>();
        foreach (var window in Floating)
        {
            var panels = window.Group.Panels.Where(shows).ToList();
            if (panels.Count > 0)
            {
                floats.Add(new DockGroupBox(window.Group, new DockBox(window.X, window.Y, window.Width, window.Height),
                    panels, shows(window.Group.Active!) ? window.Group.Active! : panels[0]));
            }
        }

        // The slid-out panel is drawn as a window with one tab, across its edge of the dock.
        var strips = AutoHidden.Where(hidden => shows(hidden.Panel)).ToList();
        var revealed = strips.FirstOrDefault(hidden => hidden.Panel == Revealed) is { } slid
            ? new DockGroupBox(new DockGroup(slid.Panel), slid.Box, [slid.Panel], slid.Panel) : null;
        return new DockArrangement(groups, separators, maximized < 0 ? null : maximized, floats, strips, revealed);
    }

    // The place on a strip of an auto-hidden panel.
    internal DockAutoHidden? StripOf(string panel) => AutoHidden.FirstOrDefault(hidden => hidden.Panel == panel);

    // The layout with another tree and other windows, and everything else as it is. A panel that the tree or a window
    // now holds leaves its strip as the layout is tidied.
    private DockLayout With(DockNode? root, IEnumerable<DockFloat> floating) =>
        new(root, Closed, floating, AutoHidden, Revealed);

    private DockLayout Replace(DockNode node, DockNode replacement) => With(Swap(Root!, node, replacement), Floating);

    // The window that holds a panel.
    internal DockFloat? FloatOf(string panel) => Floating.FirstOrDefault(window => window.Group.Panels.Contains(panel));

    // The layout with a window, as it is or changed, in front of the others.
    private DockLayout InFront(DockFloat window, DockFloat changed) => ReferenceEquals(Floating[^1], window)
        && window.Equals(changed) ? this
        : With(Root, [.. Floating.Where(each => !ReferenceEquals(each, window)), changed]);

    // The layout with the panel out of its group or window, for the caller to place again.
    private DockLayout Lifted(string panel) => With(Without(panel), FloatingWithout(panel));

    // The windows with the panel out of the one that holds it. A window left empty goes when the layout is tidied.
    private IEnumerable<DockFloat> FloatingWithout(string panel) => Floating.Select(window =>
        window.Group.Panels.Contains(panel) ? window.With(Without(window.Group, panel)) : window);

    // The layout with nothing covering the group of a docked panel that was just shown.
    private DockLayout Uncovered(string panel) =>
        Maximized is { } group && !group.Panels.Contains(panel) ? Restore() : this;

    // A group with other panels, in the place of this one.
    private static DockGroup Holding(DockGroup group, IEnumerable<string> panels, string? active) =>
        new(panels) { Active = active, Size = group.Size, Maximized = group.Maximized };

    // The tree with the panel out of its group. A group left empty goes when the tree is tidied.
    private DockNode? Without(string panel) =>
        Groups.FirstOrDefault(each => each.Panels.Contains(panel)) is { } group
            ? Swap(Root!, group, Without(group, panel)) : Root;

    // A group with one panel out of it, where the tab after it shows in its place, or the one before it when it was
    // last.
    private static DockGroup Without(DockGroup group, string panel)
    {
        var left = group.Panels.Where(each => each != panel).ToList();
        var index = group.Panels.TakeWhile(each => each != panel).Count();
        var active = group.Active == panel ? left.ElementAtOrDefault(Math.Min(index, left.Count - 1)) : group.Active;
        return Holding(group, left, active);
    }

    // A split of a part and a new group for the panel on one side of it.
    private static DockSplit Beside(DockNode part, string panel, DockPosition position, double size)
    {
        var own = new DockGroup(panel);
        return new DockSplit(position is DockPosition.Start or DockPosition.Top ? [own, part] : [part, own])
        {
            Vertical = position is DockPosition.Top or DockPosition.Bottom,
            Size = size,
        };
    }

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
                : Holding(group, panels, group.Active);
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
