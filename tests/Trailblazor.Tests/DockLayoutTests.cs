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

    [Theory]
    [InlineData(DockPosition.Before, "explorer", "problems explorer search")]
    [InlineData(DockPosition.After, "explorer", "explorer problems search")]
    [InlineData(DockPosition.After, "search", "explorer search problems")]
    public void Move_ShouldPutThePanelsTabBesideTheTargetsAndShowIt_WhenGivenBeforeOrAfter(DockPosition position,
        string target, string expected)
    {
        var layout = Workbench.Move("problems", target, position);

        var group = layout.GroupOf("problems")!;
        Assert.Equal(expected.Split(' '), group.Panels);
        Assert.Equal("problems", group.Active);
        Assert.Equal(["output"], layout.GroupOf("output")!.Panels);
        // The group keeps its size, and so does the one the panel left.
        Assert.Equal((1, 1), (group.Size, layout.GroupOf("output")!.Size));
    }

    [Fact]
    public void Move_ShouldReorderTheTabs_WhenTheTargetIsInThePanelsOwnGroup()
    {
        var layout = Workbench.Move("explorer", "search", DockPosition.After);

        Assert.Equal(["search", "explorer"], layout.GroupOf("explorer")!.Panels);
        Assert.Equal("explorer", layout.GroupOf("explorer")!.Active);
    }

    [Fact]
    public void Move_ShouldShowTheNextTabInTheGroupThePanelLeaves_WhenItWasShowing()
    {
        var layout = Workbench.Move("output", "editor", DockPosition.After);

        Assert.Equal("problems", layout.GroupOf("problems")!.Active);
    }

    [Fact]
    public void Move_ShouldGiveTheGroupsSpaceToItsSiblings_WhenItsLastPanelLeaves()
    {
        var layout = Workbench.Move("editor", "search", DockPosition.After);

        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal(new DockGroup("output", "problems") { Size = 3 }, split.Children[1]);
    }

    [Theory]
    [InlineData(DockPosition.Start, false, true)]
    [InlineData(DockPosition.End, false, false)]
    [InlineData(DockPosition.Top, true, true)]
    [InlineData(DockPosition.Bottom, true, false)]
    public void Move_ShouldSplitTheTargetsGroupInTwo_WhenGivenASide(DockPosition position, bool vertical, bool first)
    {
        var layout = new DockLayout(new DockGroup("map", "notes", "photos") { Size = 2 });

        var moved = layout.Move("photos", "map", position);

        var split = Assert.IsType<DockSplit>(moved.Root);
        Assert.Equal((vertical, 2d), (split.Vertical, split.Size));
        Assert.Equal(new DockGroup("photos"), split.Children[first ? 0 : 1]);
        Assert.Equal(new DockGroup("map", "notes"), split.Children[first ? 1 : 0]);
    }

    [Fact]
    public void Move_ShouldJoinTheNewGroupToTheSplitAroundIt_WhenTheyRunTheSameWay()
    {
        // The editor's group is 3 of the 4 parts beside the explorer; the output takes half of it.
        var layout = new DockLayout(new DockSplit(
            new DockGroup("explorer") { Size = 1 }, new DockGroup("editor", "output") { Size = 3 }));

        var moved = layout.Move("output", "editor", DockPosition.End);

        var split = Assert.IsType<DockSplit>(moved.Root);
        Assert.Equal(["explorer", "editor", "output"], split.Children.Select(child => ((DockGroup)child).Panels[0]));
        Assert.Equal([1, 1.5, 1.5], split.Children.Select(child => child.Size));
    }

    [Fact]
    public void Move_ShouldDockAClosedOrUnknownPanel_WhenItIsMoved()
    {
        var layout = Workbench.Close("search").Move("search", "editor", DockPosition.Bottom)
            .Move("terminal", "editor", DockPosition.After);

        Assert.Empty(layout.Closed);
        Assert.Equal(["search"], layout.GroupOf("search")!.Panels);
        Assert.Equal(["editor", "terminal"], layout.GroupOf("terminal")!.Panels);
    }

    [Fact]
    public void Move_ShouldChangeNothing_WhenTheTargetIsThePanelItselfOrIsNotDocked()
    {
        Assert.Same(Workbench, Workbench.Move("editor", "editor", DockPosition.End));
        Assert.Same(Workbench, Workbench.Move("editor", "terminal", DockPosition.After));
        var closed = Workbench.Close("search");
        Assert.Same(closed, closed.Move("output", "search", DockPosition.After));
    }

    [Theory]
    [InlineData(DockPosition.Start, false, true)]
    [InlineData(DockPosition.End, false, false)]
    [InlineData(DockPosition.Top, true, true)]
    [InlineData(DockPosition.Bottom, true, false)]
    public void MoveToEdge_ShouldGiveThePanelAQuarterOfTheDockAlongThatEdge_WhenGivenAnEdge(DockPosition position,
        bool vertical, bool first)
    {
        var layout = new DockLayout(new DockSplit(new DockGroup("map"), new DockGroup("notes", "photos"))
        {
            Vertical = !vertical,
        });

        var moved = layout.MoveToEdge("photos", position);

        var split = Assert.IsType<DockSplit>(moved.Root);
        Assert.Equal(vertical, split.Vertical);
        Assert.Equal(new DockGroup("photos"), split.Children[first ? 0 : 1]);
        Assert.Equal(3, split.Children[first ? 1 : 0].Size);
        Assert.Equal([["map"], ["notes"]],
            Assert.IsType<DockSplit>(split.Children[first ? 1 : 0]).Children.Select(child => ((DockGroup)child).Panels));
    }

    [Fact]
    public void MoveToEdge_ShouldKeepTheQuarter_WhenTheDockAlreadySplitsThatWay()
    {
        var layout = new DockLayout(new DockSplit(
            new DockGroup("map") { Size = 1 }, new DockGroup("notes") { Size = 2 }));

        var moved = layout.MoveToEdge("photos", DockPosition.End);

        Assert.Equal([1, 2, 1], Assert.IsType<DockSplit>(moved.Root).Children.Select(child => child.Size));
    }

    [Fact]
    public void MoveToEdge_ShouldLeaveThePanelAlone_WhenItIsTheOnlyOne()
    {
        var alone = new DockLayout(new DockGroup("map"));

        Assert.Equal(alone, alone.MoveToEdge("map", DockPosition.Top));
        Assert.Equal(alone, new DockLayout().MoveToEdge("map", DockPosition.Start));
    }

    [Theory]
    [InlineData(DockPosition.Before)]
    [InlineData(DockPosition.After)]
    public void MoveToEdge_ShouldThrow_WhenGivenATabPosition(DockPosition position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Workbench.MoveToEdge("editor", position));
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
    public void Maximize_ShouldShowThePanelAndMarkItsGroup_WhenGivenADockedPanel()
    {
        var layout = Workbench.Maximize("problems");

        Assert.Equal([false, false, true], layout.Groups.Select(group => group.Maximized));
        Assert.Equal("problems", layout.Maximized?.Active);
        // The other groups keep their places and sizes for when the group is restored.
        Assert.Equal(Workbench.Open("problems"), layout.Restore());
    }

    [Fact]
    public void Maximize_ShouldMoveTheMark_WhenAnotherGroupFilledTheDock()
    {
        var layout = Workbench.Maximize("editor").Maximize("search");

        Assert.Equal([true, false, false], layout.Groups.Select(group => group.Maximized));
    }

    [Fact]
    public void Maximize_ShouldChangeNothing_WhenGivenAPanelThatIsNotDocked()
    {
        var layout = Workbench.Close("search");

        Assert.Same(layout, layout.Maximize("search"));
        Assert.Same(layout, layout.Maximize("photos"));
    }

    [Fact]
    public void Restore_ShouldChangeNothing_WhenNoGroupFillsTheDock()
    {
        Assert.Null(Workbench.Maximized);
        Assert.Same(Workbench, Workbench.Restore());
    }

    [Fact]
    public void Constructor_ShouldKeepOnlyTheFirstMaximizedGroup_WhenGivenTwo()
    {
        var layout = new DockLayout(new DockSplit(new DockGroup("map"), new DockGroup("notes") { Maximized = true },
            new DockGroup("photos") { Maximized = true }));

        Assert.Equal([false, true, false], layout.Groups.Select(group => group.Maximized));
    }

    [Fact]
    public void Float_ShouldTakeThePanelIntoAWindowInFront_WhenCalled()
    {
        var layout = Workbench.Float("search").Float("problems");

        Assert.Equal([["search"], ["problems"]], layout.Floating.Select(window => window.Group.Panels));
        Assert.Equal([["explorer"], ["editor"], ["output"]], layout.Groups.Select(group => group.Panels));
        Assert.Equal(["problems"], layout.GroupOf("problems")?.Panels);
        // Half of the dock over its middle, and each new window a little down and along from the last.
        Assert.Equal([(25.0, 25.0, 50.0, 50.0), (28.0, 28.0, 50.0, 50.0)], layout.Floating.Select(Box));
    }

    [Fact]
    public void FloatAt_ShouldFloatAPanelThatIsClosedOrNew_WhenGivenOne()
    {
        var layout = Workbench.Close("search").FloatAt("search", 10, 20, 30, 40).FloatAt("terminal", 0, 0, 20, 20);

        Assert.Empty(layout.Closed);
        Assert.Equal(["search", "terminal"], layout.Floating.Select(window => window.Group.Active));
        Assert.Equal((10.0, 20.0, 30.0, 40.0), Box(layout.Floating[0]));
    }

    [Fact]
    public void Constructor_ShouldKeepAWindowsBoxInsideTheDock_WhenItReachesOutOrIsTooSmall()
    {
        var layout = new DockLayout(new DockGroup("map"), null, [new DockFloat(new DockGroup("notes"), 80, -5, 40, 3),
            new DockFloat(new DockGroup("photos"), 0, 0, 250, 50)]);

        Assert.Equal([(60.0, 0.0, 40.0, 10.0), (0.0, 0.0, 100.0, 50.0)], layout.Floating.Select(Box));
    }

    [Fact]
    public void Constructor_ShouldKeepAPanelInOnePlace_WhenAWindowListsOneThatIsDockedOrInAnEarlierWindow()
    {
        var layout = new DockLayout(new DockGroup("map", "notes"), ["photos"],
        [
            new DockFloat(new DockGroup("notes", "photos"), 0, 0, 50, 50),
            new DockFloat(new DockGroup("photos", "map"), 0, 0, 50, 50),
        ]);

        // The second window is left with no panel and goes, and a floating panel isn't closed.
        Assert.Equal([["photos"]], layout.Floating.Select(window => window.Group.Panels));
        Assert.Empty(layout.Closed);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAWindowIsNullOrItsBoxIsNotMadeOfNumbers()
    {
        Assert.Throws<ArgumentException>(() => new DockLayout(null, null, [null!]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DockFloat(new DockGroup("map"), double.NaN, 0, 50, 50));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DockFloat(new DockGroup("map"), 0, 0, double.PositiveInfinity, 50));
    }

    [Fact]
    public void Move_ShouldMakeThePanelATabOfTheWindowAndBringItToTheFront_WhenTheTargetFloats()
    {
        var layout = Workbench.Float("search").Float("problems");

        var joined = layout.Move("output", "search", DockPosition.Before);

        Assert.Equal([["problems"], ["output", "search"]], joined.Floating.Select(window => window.Group.Panels));
        Assert.Equal("output", joined.Floating[1].Group.Active);
        Assert.Equal((25.0, 25.0, 50.0, 50.0), Box(joined.Floating[1]));
        // A window has no sides, so a side of one puts the tab after the target's.
        Assert.Equal(["search", "editor"], layout.Move("editor", "search", DockPosition.Top).Floating[1].Group.Panels);
    }

    [Fact]
    public void Open_ShouldShowTheTabAndBringItsWindowToTheFront_WhenThePanelFloats()
    {
        var layout = Workbench.Float("search").Float("problems").Move("output", "search", DockPosition.Before);

        var opened = layout.Open("search").Open("problems");

        Assert.Equal([["output", "search"], ["problems"]], opened.Floating.Select(window => window.Group.Panels));
        Assert.Equal("search", opened.Floating[0].Group.Active);
        // A showing tab of the window in front changes nothing.
        Assert.Same(opened, opened.Open("problems"));
    }

    [Fact]
    public void Move_ShouldDockAFloatingPanelAndDropItsWindow_WhenTheTargetIsDockedOrAnEdgeIsGiven()
    {
        var layout = Workbench.Float("search");

        Assert.Equal(Workbench.Move("search", "editor", DockPosition.After),
            layout.Move("search", "editor", DockPosition.After));
        Assert.Equal(Workbench.Move("search", "editor", DockPosition.Start),
            layout.Move("search", "editor", DockPosition.Start));
        Assert.Equal(Workbench.MoveToEdge("search", DockPosition.Bottom),
            layout.MoveToEdge("search", DockPosition.Bottom));
    }

    [Fact]
    public void Close_ShouldTakeThePanelOutOfItsWindow_WhenThePanelFloats()
    {
        var layout = Workbench.Float("search").Move("problems", "search", DockPosition.After);

        // The tab before the closed one shows, as in a group, and a window goes with its last panel.
        Assert.Equal("search", layout.Close("problems").Floating[0].Group.Active);
        Assert.Empty(layout.Close("problems").Close("search").Floating);
        Assert.Equal(["problems", "search"], layout.Close("problems").Close("search").Closed);
    }

    [Fact]
    public void Maximized_ShouldStay_WhenAFloatingPanelIsShown()
    {
        var layout = Workbench.Maximize("editor").Float("search");

        // A window floats above the group that fills the dock, and isn't itself a group to maximize.
        Assert.NotNull(layout.Open("search").Maximized);
        Assert.Same(layout, layout.Maximize("search"));
    }

    [Fact]
    public void Equals_ShouldTellTwoLayoutsApart_WhenTheirWindowsDiffer()
    {
        var layout = Workbench.FloatAt("search", 10, 20, 30, 40);

        Assert.Equal(layout, Workbench.FloatAt("search", 10, 20, 30, 40));
        Assert.NotEqual(layout, Workbench.FloatAt("search", 10, 20, 30, 45));
        Assert.NotEqual(layout.Float("problems"), layout.Float("problems").Open("search"));
    }

    [Fact]
    public void Raise_ShouldBringTheWindowOfAFloatingPanelToTheFront_WhenItIsBehindAnother()
    {
        var layout = Workbench.Float("search").Float("problems");

        Assert.Equal(["problems", "search"], layout.Raise("search").Floating.Select(window => window.Group.Active));
        Assert.Same(layout, layout.Raise("problems"));
        Assert.Same(layout, layout.Raise("editor"));
    }

    [Fact]
    public void Arrange_ShouldGiveEachWindowThatShowsAPanelItsBox_WhenTheLayoutHasWindows()
    {
        var arrangement = Workbench.FloatAt("search", 10, 20, 30, 40).Float("problems")
            .Arrange(panel => panel != "problems");

        var window = Assert.Single(arrangement.Floats);
        Assert.Equal((new DockBox(10, 20, 30, 40), "search"), (window.Box, window.Active));
        Assert.Equal(3, arrangement.Groups.Count);
    }

    [Fact]
    public void ToJson_ShouldWriteTheWindowsAndReadThemBack_WhenTheLayoutHasSome()
    {
        var layout = new DockLayout(new DockGroup("map"), null,
            [new DockFloat(new DockGroup("notes", "photos") { Active = "photos" }, 10, 20, 30, 40)]);

        Assert.Equal("""{"version":1,"root":{"panels":["map"],"active":"map","size":1},"closed":[],"floating":[{"panels":["notes","photos"],"active":"photos","x":10,"y":20,"width":30,"height":40}]}""",
            layout.ToJson());
        Assert.True(DockLayout.TryParse(layout.ToJson(), out var read));
        Assert.Equal(layout, read);
    }

    [Fact]
    public void Constructor_ShouldMarkNoGroup_WhenTheLayoutHasOneGroup()
    {
        var notes = new DockLayout(new DockGroup("map", "notes") { Maximized = true });

        Assert.Null(notes.Maximized);
        // With nothing to cover, Maximize only shows the panel.
        Assert.Equal(new DockLayout(new DockGroup("map", "notes") { Active = "notes" }), notes.Maximize("notes"));
        // The mark goes when the last group it covered does.
        Assert.Null(new DockLayout(new DockSplit(new DockGroup("map") { Maximized = true }, new DockGroup("notes")))
            .Close("notes").Maximized);
    }

    [Fact]
    public void Equals_ShouldTellTwoLayoutsApart_WhenOnlyOneHasAMaximizedGroup()
    {
        Assert.NotEqual(Workbench, Workbench.Maximize("editor"));
        Assert.Equal(Workbench.Maximize("editor"), Workbench.Maximize("editor"));
    }

    [Fact]
    public void Maximized_ShouldStay_WhenAChangeLeavesItsPanelInTheGroupThatFillsTheDock()
    {
        var layout = Workbench.Maximize("output");

        Assert.Equal("problems", layout.Open("problems").Maximized?.Active);
        Assert.Equal(["problems", "output"], layout.Move("output", "problems", DockPosition.After).Maximized?.Panels);
        Assert.Equal(["problems"], layout.Close("output").Maximized?.Panels);
        // A panel that joins the group shows there.
        Assert.Equal(["output", "search", "problems"],
            layout.Move("search", "output", DockPosition.After).Maximized?.Panels);
        // A covered panel can close behind it.
        Assert.NotNull(layout.Close("explorer").Maximized);
    }

    [Fact]
    public void Maximized_ShouldEnd_WhenAPanelIsShownOutsideTheGroupThatFillsTheDockOrTheGroupGoes()
    {
        var layout = Workbench.Maximize("output");

        Assert.Equal(Workbench.Open("search"), layout.Open("search"));
        Assert.Equal(Workbench.Move("output", "editor", DockPosition.After),
            layout.Move("output", "editor", DockPosition.After));
        Assert.Equal(Workbench.Move("output", "problems", DockPosition.Start),
            layout.Move("output", "problems", DockPosition.Start));
        Assert.Equal(Workbench.MoveToEdge("output", DockPosition.Bottom),
            layout.MoveToEdge("output", DockPosition.Bottom));
        Assert.Equal(Workbench.Close("output").Close("problems"), layout.Close("output").Close("problems"));
        // A closed panel opens in the largest group, which the editor's is.
        Assert.Null(layout.Close("output").Open("output").Maximized);
    }

    [Fact]
    public void Arrange_ShouldSayWhichGroupFillsTheDockAndKeepEveryBox_WhenOneIsMaximized()
    {
        var arrangement = Workbench.Maximize("editor").Arrange(_ => true);

        Assert.Equal(1, arrangement.Maximized);
        Assert.Equal(Workbench.Arrange(_ => true).Groups.Select(group => group.Box),
            arrangement.Groups.Select(group => group.Box));
        Assert.Null(Workbench.Arrange(_ => true).Maximized);
    }

    [Fact]
    public void Arrange_ShouldSayNoGroupFillsTheDock_WhenTheMaximizedOneIsTheOnlyOneThatShows()
    {
        var layout = Workbench.Maximize("editor");

        Assert.Null(layout.Arrange(panel => panel == "editor").Maximized);
        // Nor when the maximized group is the one with no panel to show.
        Assert.Null(layout.Arrange(panel => panel != "editor").Maximized);
    }

    [Fact]
    public void ToJson_ShouldMarkTheGroupThatFillsTheDock_WhenOneDoes()
    {
        var layout = new DockLayout(new DockSplit(new DockGroup("map"), new DockGroup("notes") { Maximized = true }));

        Assert.Equal("""{"version":1,"root":{"split":"row","children":[{"panels":["map"],"active":"map","size":1},{"panels":["notes"],"active":"notes","maximized":true,"size":1}],"size":1},"closed":[]}""",
            layout.ToJson());
        Assert.True(DockLayout.TryParse(layout.ToJson(), out var read));
        Assert.Equal(layout, read);
        Assert.Equal(["notes"], read.Maximized?.Panels);
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
    [InlineData("""{"version":1,"root":{"panels":["map"],"active":"map","maximized":"yes","size":1}}""")]
    [InlineData("""{"version":1,"floating":"none"}""")]
    [InlineData("""{"version":1,"floating":[{"panels":["map"],"active":"map","x":0,"y":0,"width":50}]}""")]
    [InlineData("""{"version":1,"floating":[{"panels":["map"],"active":"map","x":"left","y":0,"width":50,"height":50}]}""")]
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

    private static (double X, double Y, double Width, double Height) Box(DockFloat window) =>
        (window.X, window.Y, window.Width, window.Height);
}
