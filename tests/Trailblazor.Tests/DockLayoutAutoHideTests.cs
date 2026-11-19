namespace Trailblazor.Tests;

public class DockLayoutAutoHideTests
{
    // The explorer and the search share a group beside the editor, which sits above the output.
    private static readonly DockLayout Workbench = new(new DockSplit(
        new DockGroup("explorer", "search") { Size = 1 },
        new DockSplit(new DockGroup("editor") { Size = 3 }, new DockGroup("output")) { Vertical = true, Size = 3 }),
        ["problems"]);

    [Fact]
    public void AutoHide_ShouldPutThePanelOnTheEdgesStrip_WhenThePanelIsDockedFloatingClosedOrNew()
    {
        var layout = Workbench.Float("output")
            .AutoHide("search", DockPosition.Start)
            .AutoHide("output", DockPosition.Bottom)
            .AutoHide("problems", DockPosition.Start)
            .AutoHide("terminal", DockPosition.Bottom);

        // Each joins its strip after the panels already there, a quarter of the dock deep when it slides out.
        Assert.Equal([("search", DockPosition.Start, 25.0), ("output", DockPosition.Bottom, 25.0),
            ("problems", DockPosition.Start, 25.0), ("terminal", DockPosition.Bottom, 25.0)],
            layout.AutoHidden.Select(hidden => (hidden.Panel, hidden.Edge, hidden.Size)));
        // It is nowhere else: not in a group, not in a window, not closed, and none is slid out.
        Assert.Equal([["explorer"], ["editor"]], layout.Groups.Select(group => group.Panels));
        Assert.Empty(layout.Floating);
        Assert.Empty(layout.Closed);
        Assert.Null(layout.GroupOf("search"));
        Assert.Null(layout.Revealed);
    }

    [Fact]
    public void AutoHide_ShouldMoveThePanelToTheEndOfTheOtherStripAndKeepItsSize_WhenThePanelIsOnAStrip()
    {
        var layout = new DockLayout(new DockGroup("editor"), null, null,
            [new DockAutoHidden("search", DockPosition.Start) { Size = 40 }, new("output", DockPosition.Bottom)],
            "search");

        var moved = layout.AutoHide("search", DockPosition.Bottom);

        Assert.Equal([("output", DockPosition.Bottom, 25.0), ("search", DockPosition.Bottom, 40.0)],
            moved.AutoHidden.Select(hidden => (hidden.Panel, hidden.Edge, hidden.Size)));
        // It was slid out from the edge it left.
        Assert.Null(moved.Revealed);
    }

    [Theory]
    [InlineData(DockPosition.Before)]
    [InlineData(DockPosition.After)]
    public void AutoHide_ShouldThrow_WhenGivenAPositionThatIsNotAnEdge(DockPosition position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Workbench.AutoHide("search", position));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DockAutoHidden("search", position));
    }

    [Fact]
    public void Open_ShouldSlideThePanelOutInPlaceOfAnother_WhenThePanelIsOnAStrip()
    {
        var layout = Workbench.AutoHide("search", DockPosition.Start).AutoHide("output", DockPosition.Bottom);

        var search = layout.Open("search");
        var output = search.Open("output");

        Assert.Equal(("search", "output"), (search.Revealed, output.Revealed));
        // It stays on its strip, and a panel that is out already changes nothing.
        Assert.Equal(layout.AutoHidden, output.AutoHidden);
        Assert.Same(output, output.Open("output"));
        Assert.Equal(layout, output.Conceal());
        Assert.Same(layout, layout.Conceal());
    }

    [Fact]
    public void Close_ShouldTakeThePanelOffItsStrip_WhenThePanelIsOnAStrip()
    {
        var layout = Workbench.AutoHide("search", DockPosition.Start).Open("search");

        var closed = layout.Close("search");

        Assert.Empty(closed.AutoHidden);
        Assert.Null(closed.Revealed);
        Assert.Equal(["problems", "search"], closed.Closed);
    }

    [Fact]
    public void Move_ShouldTakeThePanelFromItsStripToWhereItSays_WhenThePanelIsOnAStrip()
    {
        var layout = Workbench.AutoHide("search", DockPosition.Start).AutoHide("output", DockPosition.Bottom)
            .Open("search");

        var beside = layout.Move("search", "editor", DockPosition.After);
        var edge = layout.MoveToEdge("search", DockPosition.Top);
        var window = layout.Float("search");

        Assert.Equal(["editor", "search"], beside.GroupOf("search")!.Panels);
        Assert.Equal(["search"], edge.Groups.First().Panels);
        Assert.Equal(["search"], window.Floating.Single().Group.Panels);
        // The other panel keeps its strip, and nothing is slid out once the panel has left.
        foreach (var moved in new[] { beside, edge, window })
        {
            Assert.Equal(["output"], moved.AutoHidden.Select(hidden => hidden.Panel));
            Assert.Null(moved.Revealed);
        }
    }

    [Fact]
    public void Constructor_ShouldKeepAPanelInOnePlace_WhenAStripListsOneThatIsDockedInAWindowOrOnAStripAlready()
    {
        var layout = new DockLayout(new DockGroup("editor"), ["problems", "terminal"],
            [new DockFloat(new DockGroup("output"), 10, 10, 50, 50)],
            [new("editor", DockPosition.Start), new("output", DockPosition.Start), new("search", DockPosition.End),
                new("search", DockPosition.Top), new("terminal", DockPosition.Bottom)],
            "editor");

        Assert.Equal([("search", DockPosition.End), ("terminal", DockPosition.Bottom)],
            layout.AutoHidden.Select(hidden => (hidden.Panel, hidden.Edge)));
        // A panel on a strip isn't closed, and the editor, which is docked, isn't the one slid out.
        Assert.Equal(["problems"], layout.Closed);
        Assert.Null(layout.Revealed);
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(10, 10)]
    [InlineData(62.5, 62.5)]
    [InlineData(250, 90)]
    public void Constructor_ShouldKeepASlidOutSizeBetweenATenthAndNineTenthsOfTheDock_WhenGivenOne(double size,
        double kept)
    {
        var layout = new DockLayout(autoHidden: [new DockAutoHidden("search", DockPosition.Start) { Size = size }]);

        Assert.Equal(kept, layout.AutoHidden.Single().Size);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAnAutoHiddenPanelIsNullOrBadlyMade()
    {
        Assert.Throws<ArgumentException>(() => new DockLayout(null, null, null, [null!]));
        Assert.Throws<ArgumentException>(() => new DockAutoHidden("two words", DockPosition.Start));
        Assert.Throws<ArgumentException>(() => new DockAutoHidden("", DockPosition.Start));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DockAutoHidden("search", DockPosition.Start) { Size = double.NaN });
    }

    [Fact]
    public void Equals_ShouldTellLayoutsApart_WhenTheirStripsOrTheSlidOutPanelDiffer()
    {
        var layout = Workbench.AutoHide("search", DockPosition.Start);

        Assert.Equal(layout, Workbench.AutoHide("search", DockPosition.Start));
        Assert.NotEqual(layout, Workbench.AutoHide("search", DockPosition.End));
        Assert.NotEqual(layout, layout.Open("search"));
        Assert.NotEqual(layout, new DockLayout(layout.Root, layout.Closed, null,
            [new DockAutoHidden("search", DockPosition.Start) { Size = 30 }]));
    }

    [Theory]
    [InlineData(DockPosition.Start, 0, 0, 30, 100)]
    [InlineData(DockPosition.End, 70, 0, 30, 100)]
    [InlineData(DockPosition.Top, 0, 0, 100, 30)]
    [InlineData(DockPosition.Bottom, 0, 70, 100, 30)]
    public void Arrange_ShouldGiveTheSlidOutPanelABoxAcrossItsEdge_WhenOneIsOut(DockPosition edge, double x, double y,
        double width, double height)
    {
        var layout = new DockLayout(new DockGroup("editor"), null, null,
            [new DockAutoHidden("search", edge) { Size = 30 }, new("output", DockPosition.Top)], "search");

        var arrangement = layout.Arrange(_ => true);

        Assert.Equal(new DockBox(x, y, width, height), arrangement.Revealed!.Box);
        Assert.Equal(["search"], arrangement.Revealed.Panels);
        Assert.Equal("search", arrangement.Revealed.Active);
        Assert.Equal(["search", "output"], arrangement.Strips.Select(hidden => hidden.Panel));
        // The docked group keeps the whole dock: a slid-out panel lies over it.
        Assert.Equal(new DockBox(0, 0, 100, 100), arrangement.Groups.Single().Box);
    }

    [Fact]
    public void Arrange_ShouldLeaveOutAStripsPanel_WhenTheDockCannotShowIt()
    {
        var layout = Workbench.AutoHide("search", DockPosition.Start).AutoHide("output", DockPosition.Start)
            .Open("search");

        var arrangement = layout.Arrange(panel => panel != "search");

        Assert.Equal(["output"], arrangement.Strips.Select(hidden => hidden.Panel));
        Assert.Null(arrangement.Revealed);
        Assert.Null(Workbench.Arrange(_ => true).Revealed);
    }

    [Fact]
    public void ToJson_ShouldWriteTheStripsButNotTheSlidOutPanel_WhenTheLayoutHasSome()
    {
        var layout = new DockLayout(new DockGroup("editor"), null, null,
            [new DockAutoHidden("search", DockPosition.Start) { Size = 40 }, new("output", DockPosition.Bottom)],
            "search");

        var json = layout.ToJson();

        Assert.Equal("""{"version":1,"root":{"panels":["editor"],"active":"editor","size":1},"closed":[],"autoHidden":[{"panel":"search","edge":"start","size":40},{"panel":"output","edge":"bottom","size":25}]}""",
            json);
        // A saved layout comes back with no panel slid out.
        Assert.True(DockLayout.TryParse(json, out var read));
        Assert.Equal(layout.Conceal(), read);
        Assert.DoesNotContain("autoHidden", Workbench.ToJson(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"version":1,"closed":[],"autoHidden":[{"panel":"search","edge":"left","size":25}]}""")]
    [InlineData("""{"version":1,"closed":[],"autoHidden":[{"panel":"search","edge":"start"}]}""")]
    [InlineData("""{"version":1,"closed":[],"autoHidden":[{"panel":"two words","edge":"start","size":25}]}""")]
    [InlineData("""{"version":1,"closed":[],"autoHidden":[{"panel":null,"edge":"start","size":25}]}""")]
    [InlineData("""{"version":1,"closed":[],"autoHidden":{"panel":"search"}}""")]
    public void TryParse_ShouldGiveFalse_WhenAStripIsNotOneTheLayoutWrote(string json)
    {
        Assert.False(DockLayout.TryParse(json, out _));
    }
}
