namespace Trailblazor.Spikes.Client.Dock;

public abstract class DockNode;

public sealed class DockSplit(bool vertical, params DockNode[] children) : DockNode
{
    public bool Vertical { get; } = vertical;

    public IReadOnlyList<DockNode> Children { get; } = children;
}

public sealed class DockGroup(string id, params string[] panelIds) : DockNode
{
    public string Id { get; } = id;

    public List<string> PanelIds { get; } = [.. panelIds];

    public string? ActiveId { get; set; } = panelIds.FirstOrDefault();
}

// Where each panel is. Panels themselves are rendered once by DockHosts and never move in the render tree.
public sealed class DockLayout
{
    private DockLayout(DockNode root, DockGroup? floating, DockGroup autoHide)
    {
        Root = root;
        Floating = floating;
        AutoHide = autoHide;
    }

    public DockNode Root { get; }

    public DockGroup? Floating { get; }

    public DockGroup AutoHide { get; }

    public bool AutoHideOpen { get; set; }

    public IEnumerable<DockGroup> Groups => GroupsIn(Root).Concat(Floating is null ? [] : [Floating]).Append(AutoHide);

    public static IReadOnlyList<string> PanelIds { get; } = Enumerable.Range(0, 20).Select(i => $"p{i}").ToList();

    // Six groups: three columns (the middle one split in two), a floating window and an auto-hide strip.
    public static DockLayout CreateDefault() => new(
        new DockSplit(false,
            new DockGroup("g0", "p0", "p1", "p2", "p3"),
            new DockSplit(true, new DockGroup("g1", "p4", "p5", "p6", "p7"), new DockGroup("g2", "p8", "p9", "p10", "p11")),
            new DockGroup("g3", "p12", "p13", "p14", "p15")),
        new DockGroup("g4", "p16", "p17"),
        new DockGroup("g5", "p18", "p19"));

    // Every panel in one group, so one is visible and nineteen are hidden.
    public static DockLayout CreateSingleGroup() => new(new DockGroup("g0", [.. PanelIds]), null, new DockGroup("g5"));

    public void Move(string panelId, string groupId)
    {
        var source = Groups.Single(g => g.PanelIds.Contains(panelId));
        var target = Groups.Single(g => g.Id == groupId);
        source.PanelIds.Remove(panelId);
        if (source.ActiveId == panelId)
        {
            source.ActiveId = source.PanelIds.FirstOrDefault();
        }

        target.PanelIds.Add(panelId);
        target.ActiveId = panelId;
    }

    public void Activate(string panelId) => Groups.Single(g => g.PanelIds.Contains(panelId)).ActiveId = panelId;

    public bool IsVisible(string panelId)
    {
        var group = Groups.Single(g => g.PanelIds.Contains(panelId));
        return group.ActiveId == panelId && (group != AutoHide || AutoHideOpen);
    }

    private static IEnumerable<DockGroup> GroupsIn(DockNode node) => node switch
    {
        DockGroup group => [group],
        DockSplit split => split.Children.SelectMany(GroupsIn),
        _ => [],
    };
}
