namespace Trailblazor.Tests;

public class DockLayoutTests
{
    private static readonly DockLayout Workbench = new(new DockSplit(
        new DockGroup("explorer", "search") { Size = 1 },
        new DockSplit(new DockGroup("editor") { Size = 3 }, new DockGroup("output", "problems"))
        {
            Vertical = true,
            Size = 3,
        }));

    [Fact]
    public void Constructor_ShouldKeepAPanelWhereItIsFirstListed_WhenGivenItTwice()
    {
        var layout = new DockLayout(new DockSplit(new DockGroup("map", "notes"), new DockGroup("notes", "photos")));

        Assert.Equal([["map", "notes"], ["photos"]], layout.Groups.Select(group => group.Panels));
    }

    [Fact]
    public void Constructor_ShouldDropAGroupWithNoPanels_WhenGivenOne()
    {
        var layout = new DockLayout(new DockSplit(new DockGroup("map"), new DockGroup(), new DockGroup("notes")));

        Assert.Equal(2, Assert.IsType<DockSplit>(layout.Root).Children.Count);
    }

    [Fact]
    public void Constructor_ShouldGiveASplitsPlaceToItsOnlyPart_WhenTheOthersAreEmpty()
    {
        var layout = new DockLayout(new DockSplit(
            new DockGroup("map"), new DockSplit(new DockGroup(), new DockGroup("notes")) { Vertical = true, Size = 2 }));

        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(new DockGroup("notes") { Size = 2 }, split.Children[1]);
    }

    [Fact]
    public void Constructor_ShouldDockNothing_WhenEveryGroupIsEmpty()
    {
        var layout = new DockLayout(new DockSplit(new DockGroup(), new DockSplit(new DockGroup())));

        Assert.Null(layout.Root);
    }

    [Fact]
    public void Constructor_ShouldJoinASplitToItsParent_WhenBothRunTheSameWay()
    {
        var layout = new DockLayout(new DockSplit(
            new DockGroup("map") { Size = 2 },
            new DockSplit(new DockGroup("notes") { Size = 1 }, new DockGroup("photos") { Size = 3 }) { Size = 2 }));

        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(3, split.Children.Count);
        // The inner split's two parts share the size it had, 2, one to three.
        Assert.Equal([2, 0.5, 1.5], split.Children.Select(child => child.Size));
    }

    [Fact]
    public void Constructor_ShouldKeepASplitInsideItsParent_WhenTheyRunDifferentWays()
    {
        var split = Assert.IsType<DockSplit>(Workbench.Root);

        Assert.True(Assert.IsType<DockSplit>(split.Children[1]).Vertical);
    }

    [Fact]
    public void Constructor_ShouldLeaveOutOfClosedADockedPanelAndARepeat_WhenGivenThem()
    {
        var layout = new DockLayout(new DockGroup("map"), ["notes", "map", "notes"]);

        Assert.Equal(["notes"], layout.Closed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Size_ShouldThrow_WhenGivenANumberThatIsNotAboveZero(double size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DockGroup("map") { Size = size });
    }

    [Theory]
    [InlineData("")]
    [InlineData("trail map")]
    public void DockGroup_ShouldThrow_WhenGivenAnIdThatIsEmptyOrHasWhiteSpace(string id)
    {
        Assert.Throws<ArgumentException>(() => new DockGroup("map", id));
    }

    [Fact]
    public void Active_ShouldBeTheFirstPanel_WhenLeftNullOrSetToAPanelTheGroupLacks()
    {
        Assert.Equal("map", new DockGroup("map", "notes").Active);
        Assert.Equal("map", new DockGroup("map", "notes") { Active = "photos" }.Active);
        Assert.Equal("notes", new DockGroup("map", "notes") { Active = "notes" }.Active);
        Assert.Null(new DockGroup().Active);
    }

    [Fact]
    public void Equals_ShouldCompareTheWholeTree_WhenGivenLayoutsBuiltApart()
    {
        var same = new DockLayout(new DockSplit(
            new DockGroup("explorer", "search") { Size = 1 },
            new DockSplit(new DockGroup("editor") { Size = 3 }, new DockGroup("output", "problems"))
        {
            Vertical = true,
            Size = 3,
        }));

        Assert.Equal(Workbench, same);
        Assert.Equal(Workbench.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(Workbench, same.Open("search"));
        Assert.NotEqual(Workbench, same.Close("search"));
        Assert.NotEqual(new DockLayout(new DockGroup("map")), new DockLayout(new DockGroup("map") { Size = 2 }));
        Assert.NotEqual(new DockLayout(new DockSplit(new DockGroup("map"), new DockGroup("notes"))),
            new DockLayout(new DockSplit(new DockGroup("map"), new DockGroup("notes")) { Vertical = true }));
    }

    [Fact]
    public void GroupOf_ShouldGiveThePanelsGroupOrNull_WhenCalled()
    {
        Assert.Equal(["output", "problems"], Workbench.GroupOf("problems")!.Panels);
        Assert.Null(Workbench.GroupOf("terminal"));
    }

    [Fact]
    public void Open_ShouldShowADockedPanelInItsGroup_WhenAnotherTabWasShowing()
    {
        var layout = Workbench.Open("problems");

        Assert.Equal("problems", layout.GroupOf("problems")!.Active);
        Assert.Equal(["explorer", "editor", "problems"], layout.Groups.Select(group => group.Active));
    }

    [Fact]
    public void Open_ShouldChangeNothing_WhenThePanelAlreadyShows()
    {
        Assert.Same(Workbench, Workbench.Open("editor"));
    }

    [Fact]
    public void Open_ShouldPutAnUnknownPanelInTheLargestGroupAndShowIt_WhenCalled()
    {
        var layout = Workbench.Open("terminal");

        // The editor's group is three quarters of the width and three fifths of the height.
        Assert.Equal(["editor", "terminal"], layout.GroupOf("terminal")!.Panels);
        Assert.Equal("terminal", layout.GroupOf("terminal")!.Active);
    }

    [Fact]
    public void Open_ShouldBringAClosedPanelBack_WhenCalled()
    {
        var layout = Workbench.Close("search").Open("search");

        Assert.Empty(layout.Closed);
        Assert.Equal("search", layout.GroupOf("search")!.Active);
    }

    [Fact]
    public void Open_ShouldStartAGroup_WhenNothingIsDocked()
    {
        var layout = new DockLayout().Open("map");

        Assert.Equal(new DockGroup("map"), layout.Root);
    }

    [Fact]
    public void Close_ShouldMoveThePanelToClosed_WhenItIsDocked()
    {
        var layout = Workbench.Close("problems");

        Assert.Equal(["problems"], layout.Closed);
        Assert.Equal(["output"], layout.GroupOf("output")!.Panels);
    }

    [Fact]
    public void Close_ShouldShowTheNextTabOrElseTheOneBefore_WhenTheShowingTabCloses()
    {
        var group = new DockLayout(new DockGroup("map", "notes", "photos") { Active = "notes" });

        Assert.Equal("photos", group.Close("notes").GroupOf("map")!.Active);
        Assert.Equal("notes", group.Open("photos").Close("photos").GroupOf("map")!.Active);
        Assert.Equal("notes", group.Close("map").GroupOf("notes")!.Active);
    }

    [Fact]
    public void Close_ShouldGiveTheGroupsSpaceToItsSiblings_WhenItsLastPanelCloses()
    {
        var layout = Workbench.Close("editor");

        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(new DockGroup("output", "problems") { Size = 3 }, split.Children[1]);
    }

    [Fact]
    public void Close_ShouldChangeNothing_WhenThePanelIsNotDocked()
    {
        Assert.Same(Workbench, Workbench.Close("terminal"));
    }

    [Fact]
    public void Place_ShouldAddAnUnknownPanelToTheLargestGroupWithoutShowingIt_WhenCalled()
    {
        var layout = Workbench.Place("terminal");

        Assert.Equal(["editor", "terminal"], layout.GroupOf("terminal")!.Panels);
        Assert.Equal("editor", layout.GroupOf("terminal")!.Active);
        Assert.Same(layout, layout.Place("terminal"));
    }

    [Fact]
    public void Arrange_ShouldGiveEachGroupItsShareOfTheDock_WhenEveryPanelShows()
    {
        var arrangement = Workbench.Arrange(_ => true);

        Assert.Equal([new DockBox(0, 0, 25, 100), new DockBox(25, 0, 75, 75), new DockBox(25, 75, 75, 25)],
            arrangement.Groups.Select(group => group.Box));
        Assert.Equal(["explorer", "editor", "output"], arrangement.Groups.Select(group => group.Active));
    }

    [Fact]
    public void Arrange_ShouldPutASeparatorAfterEachGroupButTheLast_WhenEveryPanelShows()
    {
        var arrangement = Workbench.Arrange(_ => true);

        Assert.Equal(2, arrangement.Separators.Count);
        var (across, down) = (arrangement.Separators[0], arrangement.Separators[1]);
        Assert.Equal((new DockBox(0, 0, 25, 100), new DockBox(25, 0, 75, 100)), (across.BeforeBox, across.AfterBox));
        Assert.Equal((new DockBox(25, 0, 75, 75), new DockBox(25, 75, 75, 25)), (down.BeforeBox, down.AfterBox));
        Assert.Equal((false, true), (across.Split.Vertical, down.Split.Vertical));
    }

    [Fact]
    public void Arrange_ShouldGiveAHiddenPartsSpaceToItsSiblings_WhenNoneOfItsPanelsShows()
    {
        var arrangement = Workbench.Arrange(panel => panel is not ("output" or "problems"));

        Assert.Equal([new DockBox(0, 0, 25, 100), new DockBox(25, 0, 75, 100)],
            arrangement.Groups.Select(group => group.Box));
        Assert.Single(arrangement.Separators);
    }

    [Fact]
    public void Arrange_ShouldShowAGroupsFirstShownPanel_WhenItsShowingPanelIsHidden()
    {
        var arrangement = Workbench.Arrange(panel => panel != "output");

        Assert.Equal(["problems"], arrangement.Groups[2].Panels);
        Assert.Equal("problems", arrangement.Groups[2].Active);
    }

    [Fact]
    public void Arrange_ShouldGiveNothing_WhenNoPanelShows()
    {
        var arrangement = Workbench.Arrange(_ => false);

        Assert.Empty(arrangement.Groups);
        Assert.Empty(arrangement.Separators);
    }

    [Fact]
    public void Resize_ShouldShareTheTwoPartsSizeAnew_WhenGivenAFraction()
    {
        var layout = new DockLayout(new DockSplit(
            new DockGroup("map") { Size = 1 }, new DockGroup("notes") { Size = 3 },
            new DockGroup("photos") { Size = 4 }));
        var split = Assert.IsType<DockSplit>(layout.Root);

        var resized = layout.Resize(split, 1, 2, 0.5);

        Assert.Equal([1, 3.5, 3.5], Assert.IsType<DockSplit>(resized.Root).Children.Select(child => child.Size));
    }

    [Fact]
    public void ToJson_ShouldWriteALayoutThatTryParseReadsBack_WhenCalled()
    {
        var layout = Workbench.Open("problems").Close("search");

        Assert.True(DockLayout.TryParse(layout.ToJson(), out var read));
        Assert.Equal(layout, read);
    }

    [Fact]
    public void ToJson_ShouldWriteTheVersionTheTreeAndTheClosedPanels_WhenCalled()
    {
        var layout = new DockLayout(new DockSplit(new DockGroup("map", "notes") { Active = "notes" },
            new DockGroup("photos") { Size = 2 }) { Vertical = true }, ["weather"]);

        Assert.Equal("""{"version":1,"root":{"split":"column","children":[{"panels":["map","notes"],"active":"notes","size":1},{"panels":["photos"],"active":"photos","size":2}],"size":1},"closed":["weather"]}""",
            layout.ToJson());
    }

    [Fact]
    public void ToJson_ShouldLeaveTheRootOut_WhenNothingIsDocked()
    {
        var json = new DockLayout(null, ["map"]).ToJson();

        Assert.Equal("""{"version":1,"closed":["map"]}""", json);
        Assert.True(DockLayout.TryParse(json, out var read));
        Assert.Equal(new DockLayout(null, ["map"]), read);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"root":{"panels":["map"],"active":"map","size":1}}""")]
    [InlineData("""{"version":2,"root":{"panels":["map"],"active":"map","size":1}}""")]
    [InlineData("""{"version":"1"}""")]
    [InlineData("""{"version":1,"root":{"panels":["map"],"active":"map"}}""")]
    [InlineData("""{"version":1,"root":{"panels":["map"],"active":"map","size":0}}""")]
    [InlineData("""{"version":1,"root":{"panels":["map"],"active":"map","size":"wide"}}""")]
    [InlineData("""{"version":1,"root":{"panels":["trail map"],"active":"map","size":1}}""")]
    [InlineData("""{"version":1,"root":{"panels":[7],"active":"map","size":1}}""")]
    [InlineData("""{"version":1,"root":{"panels":"map","active":"map","size":1}}""")]
    [InlineData("""{"version":1,"root":{"split":"diagonal","children":[],"size":1}}""")]
    [InlineData("""{"version":1,"root":{"split":"row","size":1}}""")]
    [InlineData("""{"version":1,"root":"map"}""")]
    [InlineData("""{"version":1,"closed":"map"}""")]
    [InlineData("""{"version":1,"closed":[null]}""")]
    public void TryParse_ShouldGiveFalse_WhenTheTextIsNotALayout(string? json)
    {
        Assert.False(DockLayout.TryParse(json, out var layout));
        Assert.Null(layout);
    }

    [Fact]
    public void TryParse_ShouldTidyTheTree_WhenTheJsonListsAPanelTwice()
    {
        Assert.True(DockLayout.TryParse(
            """{"version":1,"root":{"split":"row","size":1,"children":[{"panels":["map"],"active":"map","size":1},{"panels":["map"],"active":"map","size":1}]}}""",
            out var layout));

        Assert.Equal(new DockGroup("map"), layout.Root);
    }
}
