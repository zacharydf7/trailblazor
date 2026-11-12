using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbTreeViewTests : BunitContext
{
    private readonly List<IReadOnlyCollection<int>> _expandedChanges = [];
    private readonly List<IReadOnlyCollection<int>> _selectedChanges = [];
    private readonly List<IReadOnlyCollection<int>> _checkedChanges = [];
    private readonly List<string> _activated = [];

    public TbTreeViewTests()
    {
        this.AddTrailblazor();
    }

    private sealed record Place(int Id, string Name, int? ParentId = null, IReadOnlyList<Place>? Children = null)
    {
        public override string ToString() => Name;
    }

    // A filter as a page writes one: a method reading search text that changes.
    private sealed class Search
    {
        public string Text { get; set; } = "";

        public bool Matches(Place place) => place.Name == Text;
    }

    // North (1) holds Ridge (2), which holds Summit (3), and Pass (6); South (4) holds Lake (5).
    private static IReadOnlyList<Place> Nested =>
    [
        new(1, "North", Children: [new(2, "Ridge", Children: [new(3, "Summit")]), new(6, "Pass")]),
        new(4, "South", Children: [new(5, "Lake")]),
    ];

    private static IReadOnlyList<Place> Flat =>
    [
        new(3, "Summit", 2), new(1, "North"), new(2, "Ridge", 1), new(4, "South"), new(5, "Lake", 4),
        new(6, "Lost", 99),
    ];

    [Fact]
    public void Render_ShouldShowTheRootsClosedWithTheirTreePlaces_WhenGivenNestedItems()
    {
        var cut = RenderPlaces(Nested);

        Assert.Equal(["North", "South"], Texts(cut));
        var north = Row(cut, "1");
        Assert.Equal(["1", "2", "1", "false"], new[] { "aria-level", "aria-setsize", "aria-posinset", "aria-expanded" }
            .Select(north.GetAttribute));
        Assert.Equal("0", north.GetAttribute("tabindex"));
        Assert.Equal("-1", Row(cut, "4").GetAttribute("tabindex"));
    }

    [Fact]
    public void Render_ShouldPlaceEachItemUnderItsParent_WhenGivenAFlatList()
    {
        var cut = RenderPlaces(Flat, parameters => parameters
            .Add(tree => tree.ParentKeySelector, place => place.ParentId).Add(tree => tree.InitialExpandDepth, 3),
            nested: false);

        Assert.Equal(["North", "Ridge", "Summit", "South", "Lake", "Lost"], Texts(cut));
        Assert.Equal("3", Row(cut, "3").GetAttribute("aria-level"));
        Assert.Equal("1", Row(cut, "6").GetAttribute("aria-level"));
    }

    [Fact]
    public void Render_ShouldThrowNamingTheKey_WhenTwoItemsShareOne()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => RenderPlaces([new(1, "North"), new(1, "Copy")]));

        Assert.Contains("key 1", failure.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenItsItemsArentItsKeysAndItHasNoKeySelector()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbTreeView<Place, int>>(parameters =>
            parameters.Add(tree => tree.Items, [new Place(1, "North")]).Add(tree => tree.AriaLabel, "Places")));

        Assert.Equal("TbTreeView needs a KeySelector when its items aren't their own keys.", failure.Message);
    }

    [Fact]
    public void Render_ShouldThrowNamingTheType_WhenAParentKeyIsntTheTreesKeyType()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => RenderPlaces([new(1, "A"), new(2, "B", 1)],
            parameters => parameters.Add(tree => tree.ParentKeySelector, place => (long?)place.ParentId),
            nested: false));

        Assert.Equal("TbTreeView's ParentKeySelector must give a key of type Int32 or null for a root, but gave a "
            + "Int64.", failure.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenParentsLeadBackToThemselves()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => RenderPlaces([new(1, "A", 2), new(2, "B", 1)],
            parameters => parameters.Add(tree => tree.ParentKeySelector, place => place.ParentId), nested: false));

        Assert.Contains("lead back", failure.Message);
    }

    [Fact]
    public void Toggle_ShouldShowTheChildrenAndRaiseTheKeys_WhenClicked()
    {
        var cut = RenderPlaces(Nested);

        Toggle(cut, "1");

        Assert.Equal(["North", "Ridge", "Pass", "South"], Texts(cut));
        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        Assert.Equal([1], _expandedChanges.Single());
    }

    [Fact]
    public void Render_ShouldKeepTheOpenAndSelectedRows_WhenTheItemsAreReplacedWithNewInstances()
    {
        var cut = RenderPlaces(Nested);
        Toggle(cut, "1");
        Row(cut, "2").Click();

        cut.Render(parameters => parameters.Add(tree => tree.Items, Nested));

        Assert.Equal(["North", "Ridge", "Pass", "South"], Texts(cut));
        Assert.Equal("true", Row(cut, "2").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Click_ShouldSelectOneRow_WhenTheModeIsSingle()
    {
        var cut = RenderPlaces(Nested);

        Row(cut, "1").Click();
        Row(cut, "4").Click();

        Assert.Equal([[1], [4]], _selectedChanges);
        Assert.Equal("false", Row(cut, "1").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Click_ShouldAddWithCtrlAndRangeWithShift_WhenTheModeIsMultiple()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode,
            SelectionMode.Multiple).Add(tree => tree.InitialExpandDepth, 1));

        Row(cut, "1").Click();
        Row(cut, "4").Click(new() { ShiftKey = true });
        Assert.Equal([1, 2, 4, 6], _selectedChanges.Last().Order());
        Row(cut, "5").Click(new() { CtrlKey = true });

        Assert.Equal([1, 2, 4, 5, 6], _selectedChanges.Last().Order());
        Assert.Equal("true", cut.Find("[role=tree]").GetAttribute("aria-multiselectable"));
    }

    [Fact]
    public async Task Extend_ShouldStartFromTheRowItLeft_WhenTheRowLastChosenIsInsideAClosedRow()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode,
            SelectionMode.Multiple).Add(tree => tree.InitialExpandDepth, 1));
        Row(cut, "2").Click();
        await cut.InvokeAsync(() => Events().OnKey("1", "ArrowLeft", false, false));

        await cut.InvokeAsync(() => Events().OnExtend("4", "1"));

        Assert.Equal([1, 4], _selectedChanges.Last().Order());
    }

    [Fact]
    public async Task ShiftClick_ShouldSelectJustThatRow_WhenTheRowLastChosenIsInsideAClosedRow()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode,
            SelectionMode.Multiple).Add(tree => tree.InitialExpandDepth, 1));
        Row(cut, "2").Click();
        await cut.InvokeAsync(() => Events().OnKey("1", "ArrowLeft", false, false));

        Row(cut, "4").Click(new() { ShiftKey = true });

        Assert.Equal([4], _selectedChanges.Last().Order());
    }

    [Fact]
    public void Click_ShouldLeaveARowUnselected_WhenItIsntSelectable()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.IsSelectable,
            place => place.Children is null));

        Row(cut, "1").Click();

        Assert.Empty(_selectedChanges);
        Assert.Null(Row(cut, "1").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Check_ShouldCheckEveryRowInsideAndShowParentsAsMixed_WhenARowIsChecked()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.InitialExpandDepth, 3));

        Check(cut, "2");

        Assert.Equal([2, 3], _checkedChanges.Single().Order());
        Assert.Equal("true", Row(cut, "3").GetAttribute("aria-checked"));
        Assert.Equal("mixed", Row(cut, "1").GetAttribute("aria-checked"));
        Check(cut, "5");
        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Check_ShouldCheckOnlyTheRow_WhenChecksDontCascade()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.CascadeChecks, false));

        Check(cut, "1");

        Assert.Equal([1], _checkedChanges.Single());
    }

    [Fact]
    public void Tree_ShouldBeANamedGroupThatItsDriverFinds_WhenItHasNoRows()
    {
        var cut = RenderPlaces([]);

        Assert.Equal("group", cut.Find(".tb-tree").GetAttribute("role"));
        Assert.Empty(cut.Tb().Tree().Texts);
    }

    [Fact]
    public void Tree_ShouldLeaveOutMultiselectable_WhenItHasNoRows()
    {
        var cut = RenderPlaces([], parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.Multiple));

        Assert.False(cut.Find(".tb-tree").HasAttribute("aria-multiselectable"));
    }

    [Fact]
    public void Check_ShouldLeaveAChildUnchecked_WhenChecksDontCascade()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.CascadeChecks, false).Add(tree => tree.InitialExpandDepth, 1));

        Check(cut, "1");

        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-checked"));
        Assert.Equal("false", Row(cut, "2").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Filter_ShouldShowTheMatchesInsideTheirOpenParents_WhenSet()
    {
        var cut = RenderPlaces(Nested,
            parameters => parameters.Add(tree => tree.Filter, place => place.Name == "Summit"));

        Assert.Equal(["North", "Ridge", "Summit"], Texts(cut));
        Assert.Contains("tb-tree__item--match", Row(cut, "3").ClassList);
        Assert.Empty(_expandedChanges);
    }

    [Fact]
    public void Filter_ShouldKeepTheRowsInsideAMatch_WhenFilterKeepsDescendants()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Filter, place => place.Name == "South")
            .Add(tree => tree.FilterKeepsDescendants, true).Add(tree => tree.InitialExpandDepth, 1));

        Assert.Equal(["South", "Lake"], Texts(cut));
    }

    [Fact]
    public void Render_ShouldGiveEachRowItsOwnKey_WhenPlainItemsAreTheirOwnKeys()
    {
        var cut = Render<TbTreeView<Trailhead, Trailhead>>(parameters => parameters
            .Add(tree => tree.AriaLabel, "Trailheads")
            .Add(tree => tree.Items, [new Trailhead("North"), new Trailhead("South")])
            .Add(tree => tree.ItemText, trailhead => trailhead.Name));

        var keys = cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("data-key")).ToList();

        Assert.Equal(2, keys.Distinct().Count());
    }

    [Fact]
    public void Render_ShouldGiveEachRowItsOwnKey_WhenTwoKeysHaveTheSameText()
    {
        var cut = Render<TbTreeView<Place, object>>(parameters => parameters.Add(tree => tree.AriaLabel, "Places")
            .Add(tree => tree.Items, [new Place(1, "North"), new Place(2, "South")])
            .Add(tree => tree.KeySelector, place => place.Id == 2 ? "1" : place.Id));

        var keys = cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("data-key")).ToList();

        Assert.Equal("1", keys[0]);
        Assert.NotEqual(keys[0], keys[1]);
    }

    // A plain class, whose text is its type's name, so its rows need made-up keys.
    private sealed class Trailhead(string name)
    {
        public string Name { get; } = name;
    }

    [Fact]
    public async Task Expand_ShouldLoadTheChildrenAndShowLoadingMeanwhile_WhenTheyLoadLazily()
    {
        var load = new TaskCompletionSource<IEnumerable<Place>>();
        var cut = RenderPlaces([new(1, "North")], parameters => parameters
            .Add(tree => tree.LoadChildren, (_, _) => load.Task), nested: false);

        Toggle(cut, "1");
        Assert.Single(cut.FindAll("[data-part=loading]"));
        await cut.InvokeAsync(() => load.SetResult([new(2, "Ridge")]));

        cut.WaitForAssertion(() => Assert.Equal(["North", "Ridge"], Texts(cut)));
        Assert.Empty(cut.FindAll("[data-part=loading]"));
    }

    [Fact]
    public async Task Toggle_ShouldTakeASecondClick_WhenFoundBeforeTheChildrenLoaded()
    {
        var load = new TaskCompletionSource<IEnumerable<Place>>();
        var cut = RenderPlaces([new(1, "North")], parameters => parameters
            .Add(tree => tree.LoadChildren, (_, _) => load.Task), nested: false);
        var toggle = Row(cut, "1").QuerySelector("[data-part=toggle]")!;

        toggle.Click();
        await cut.InvokeAsync(() => load.SetResult([new(2, "Ridge")]));
        cut.WaitForAssertion(() => Assert.Equal(["North", "Ridge"], Texts(cut)));
        toggle.Click();

        Assert.Equal(["North"], Texts(cut));
    }

    [Fact]
    public async Task Expand_ShouldLoadAgain_WhenTheRowReopensBeforeItsCancelledLoadEnds()
    {
        var loads = new List<TaskCompletionSource<IEnumerable<Place>>>();
        var failures = new List<Exception>();
        var cut = RenderPlaces([new(1, "North")], parameters => parameters
            .Add(tree => tree.OnLoadFailed, (Exception failure) => failures.Add(failure))
            .Add(tree => tree.LoadChildren, (_, _) =>
            {
                loads.Add(new TaskCompletionSource<IEnumerable<Place>>());
                return loads[^1].Task;
            }), nested: false);
        Toggle(cut, "1");
        Toggle(cut, "1");
        Toggle(cut, "1");

        // The first load ignored its token and ends with a provider's own error for the cancel.
        await cut.InvokeAsync(() => loads[0].SetException(new InvalidOperationException("Cancelled upstream")));
        cut.WaitForAssertion(() => Assert.Equal(2, loads.Count));
        Assert.Single(cut.FindAll("[data-part=loading]"));
        await cut.InvokeAsync(() => loads[1].SetResult([new(2, "Ridge")]));

        cut.WaitForAssertion(() => Assert.Equal(["North", "Ridge"], Texts(cut)));
        Assert.Empty(failures);
    }

    [Fact]
    public async Task Expand_ShouldOfferToRetryAndReportTheError_WhenLoadingFails()
    {
        var failures = new List<Exception>();
        var attempts = 0;
        var cut = RenderPlaces([new(1, "North")], parameters => parameters
            .Add(tree => tree.OnLoadFailed, (Exception failure) => failures.Add(failure))
            .Add(tree => tree.LoadChildren, (_, _) => ++attempts == 1
                ? Task.FromException<IEnumerable<Place>>(new InvalidOperationException("Offline"))
                : Task.FromResult<IEnumerable<Place>>([new(2, "Ridge")])), nested: false);

        Toggle(cut, "1");
        Assert.Equal("Offline", Assert.Single(failures).Message);
        await cut.InvokeAsync(() => cut.Find("[data-part=failed] button").Click());

        cut.WaitForAssertion(() => Assert.Equal(["North", "Ridge"], Texts(cut)));
    }

    [Fact]
    public void Expand_ShouldSayTheLoadFailed_WhenLoadingFails()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces([new(1, "North")], parameters => parameters.Add(tree => tree.LoadChildren,
            (_, _) => Task.FromException<IEnumerable<Place>>(new InvalidOperationException("Offline"))), nested: false);

        Toggle(cut, "1");

        announcer.WaitForAssertion(() =>
            Assert.Equal("Couldn't load these items.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task ExpandAll_ShouldOpenEveryParent_WhenNoDepthIsGiven()
    {
        var cut = RenderPlaces(Nested);

        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());

        Assert.Equal(["North", "Ridge", "Summit", "Pass", "South", "Lake"], Texts(cut));
        await cut.InvokeAsync(() => cut.Instance.CollapseAllAsync());
        Assert.Equal(["North", "South"], Texts(cut));
    }

    [Fact]
    public async Task ExpandAsync_ShouldOpenTheRowAndTheRowsItSitsIn_WhenGivenADeepKey()
    {
        var cut = RenderPlaces(Nested);

        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(2));

        Assert.Equal(["North", "Ridge", "Summit", "Pass", "South"], Texts(cut));
    }

    [Fact]
    public async Task Keys_ShouldOpenCloseActivateAndCheck_WhenTheBrowserReportsThem()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true));
        var events = Events();

        await cut.InvokeAsync(() => events.OnKey("1", "ArrowRight", false, false));
        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        await cut.InvokeAsync(() => events.OnKey("1", "ArrowLeft", false, false));
        Assert.Equal("false", Row(cut, "1").GetAttribute("aria-expanded"));
        await cut.InvokeAsync(() => events.OnKey("1", "*", false, false));
        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-expanded"));
        await cut.InvokeAsync(() => events.OnKey("4", "Enter", false, false));
        await cut.InvokeAsync(() => events.OnKey("4", " ", false, false));

        Assert.Equal(["South"], _activated);
        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-checked"));
    }

    [Fact]
    public async Task OnKey_ShouldSelectTheRowFocusMovedTo_WhenSelectionFollowsFocus()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionFollowsFocus, true));

        await cut.InvokeAsync(() => Events().OnKey("4", "Follow", false, false));

        Assert.Equal([4], _selectedChanges.Single());
        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-selected"));
        Assert.Equal("", cut.Find("[role=tree]").GetAttribute("data-follows-focus"));
    }

    [Theory]
    [InlineData(SelectionMode.Single, false, false)]
    [InlineData(SelectionMode.Multiple, true, false)]
    [InlineData(SelectionMode.Single, true, true)]
    public async Task OnKey_ShouldLeaveTheSelection_WhenFocusMovesWithoutFollowingASingleSelection(SelectionMode mode,
        bool follows, bool disabled)
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode, mode)
            .Add(tree => tree.SelectionFollowsFocus, follows).Add(tree => tree.Disabled, disabled));

        await cut.InvokeAsync(() => Events().OnKey("4", "Follow", false, false));

        Assert.Empty(_selectedChanges);
        Assert.Null(cut.Find("[role=tree]").GetAttribute("data-follows-focus"));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "")]
    public void Render_ShouldTellTheBrowserWhetherTypingFindsRows_WhenGivenTypeAhead(bool typeAhead, string? expected)
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.TypeAhead, typeAhead));

        Assert.Equal(expected, cut.Find("[role=tree]").GetAttribute("data-no-typeahead"));
    }

    [Fact]
    public async Task OnKey_ShouldOpenAndCloseARowWithChildrenAndActivateIt_WhenEnterIsPressed()
    {
        var cut = RenderPlaces(Nested);

        await cut.InvokeAsync(() => Events().OnKey("4", "Enter", false, false));
        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-expanded"));
        await cut.InvokeAsync(() => Events().OnKey("4", "Enter", false, false));

        Assert.Equal("false", Row(cut, "4").GetAttribute("aria-expanded"));
        Assert.Equal(["South", "South"], _activated);
    }

    [Fact]
    public async Task OnKey_ShouldOpenTheRowAndKeepTheTabStop_WhenADragRestsOnIt()
    {
        var cut = RenderPlaces(Nested);
        await cut.InvokeAsync(() => Events().OnFocus("4"));

        await cut.InvokeAsync(() => Events().OnKey("1", "Open", false, false));

        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        Assert.Equal("0", Row(cut, "4").GetAttribute("tabindex"));
        Assert.Equal("-1", Row(cut, "1").GetAttribute("tabindex"));
    }

    [Fact]
    public void Click_ShouldOpenAndCloseAParentRow_WhenTheRowCantBeSelected()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.None));

        Row(cut, "1").Click(new() { Detail = 1 });
        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        Row(cut, "1").Click(new() { Detail = 1 });

        Assert.Equal("false", Row(cut, "1").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Click_ShouldLeaveADisabledParentRowClosed_WhenTheRowCantBeSelected()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.None)
            .Add(tree => tree.ItemDisabled, place => place.Id == 1));

        Row(cut, "1").Click(new() { Detail = 1 });

        Assert.Equal("false", Row(cut, "1").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void DoubleClick_ShouldOpenAParentRowThatCantBeSelectedOnce_WhenItsClicksComeFirst()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.IsSelectable, place => place.Id != 1));

        Row(cut, "1").Click(new() { Detail = 1 });
        Row(cut, "1").Click(new() { Detail = 2 });
        Row(cut, "1").DoubleClick();

        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        Assert.Single(_expandedChanges);
        Assert.Equal(["North"], _activated);
    }

    [Fact]
    public void DoubleClick_ShouldOpenAndActivateARowWithChildrenButOnlyActivateALeaf_WhenEachIsDoubleClicked()
    {
        var cut = RenderPlaces(Nested);

        Row(cut, "1").DoubleClick();
        Row(cut, "6").DoubleClick();

        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        Assert.DoesNotContain(_expandedChanges, keys => keys.Contains(6));
        Assert.Equal(["North", "Pass"], _activated);
    }

    [Fact]
    public async Task OnKey_ShouldSelectAndActivateTheRowAtOnce_WhenEnterOpensChildrenThatAreStillLoading()
    {
        var load = new TaskCompletionSource<IEnumerable<Place>>();
        var cut = RenderPlaces([new(1, "North")], parameters => parameters
            .Add(tree => tree.LoadChildren, (_, _) => load.Task), nested: false);

        var enter = cut.InvokeAsync(() => Events().OnKey("1", "Enter", false, false));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-part=loading]")));
        Assert.Equal(["North"], _activated);
        Assert.Equal([1], _selectedChanges.Single());
        await cut.InvokeAsync(() => load.SetResult([]));
        await enter;
    }

    [Fact]
    public async Task Collapse_ShouldCloseARowTheFilterOpened_WhenTheUserClosesIt()
    {
        var cut = RenderPlaces(Nested,
            parameters => parameters.Add(tree => tree.Filter, place => place.Name == "Summit"));

        Toggle(cut, "2");
        Assert.Equal(["North", "Ridge"], Texts(cut));
        Assert.Equal("false", Row(cut, "2").GetAttribute("aria-expanded"));
        Assert.Empty(_expandedChanges);
        await cut.InvokeAsync(() => Events().OnKey("1", "ArrowLeft", false, false));

        Assert.Equal(["North"], Texts(cut));
    }

    [Fact]
    public void Render_ShouldOpenToTheInitialDepth_WhenTheItemsArriveAfterTheFirstRender()
    {
        var cut = RenderPlaces([], parameters => parameters.Add(tree => tree.InitialExpandDepth, 1));

        cut.Render(parameters => parameters.Add(tree => tree.Items, Nested));

        Assert.Equal(["North", "Ridge", "Pass", "South", "Lake"], Texts(cut));
    }

    [Fact]
    public void Render_ShouldLoadTheChildren_WhenTheParentOpensALazyRow()
    {
        var cut = RenderPlaces([new(1, "North")], parameters => parameters
            .Add(tree => tree.ExpandedKeys, [1])
            .Add(tree => tree.LoadChildren, (_, _) => Task.FromResult<IEnumerable<Place>>([new(2, "Ridge")])),
            nested: false);

        cut.WaitForAssertion(() => Assert.Equal(["North", "Ridge"], Texts(cut)));
    }

    [Fact]
    public async Task Check_ShouldCheckTheChildren_WhenARowCheckedBeforeLoadingLoads()
    {
        var cut = RenderPlaces([new(1, "North")], parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.LoadChildren, (_, _) =>
                Task.FromResult<IEnumerable<Place>>([new(2, "Ridge"), new(3, "Pass")])), nested: false);

        Check(cut, "1");
        Toggle(cut, "1");
        await cut.InvokeAsync(() => { });

        cut.WaitForAssertion(() => Assert.Equal("true", Row(cut, "3").GetAttribute("aria-checked")));
        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-checked"));
        Assert.Equal([1, 2, 3], _checkedChanges.Last().Order());
    }

    [Fact]
    public void Check_ShouldPassOverTurnedOffRows_WhenAParentCascades()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.ItemDisabled, place => place.Name == "Pass"));

        Check(cut, "1");
        Assert.Equal([2, 3], _checkedChanges.Single().Order());
        Assert.Equal("mixed", Row(cut, "1").GetAttribute("aria-checked"));
        Check(cut, "1");

        Assert.Empty(_checkedChanges.Last());
        Assert.Equal("false", Row(cut, "1").GetAttribute("aria-checked"));
    }

    [Fact]
    public async Task ScrollTo_ShouldOpenTheRowsAboveAndScrollThroughTheBrowserService_WhenGivenAKey()
    {
        var browser = Services.GetRequiredService<FakeBrowserService>();
        var cut = RenderPlaces(Nested);

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(3));

        Assert.Equal(["North", "Ridge", "Summit", "Pass", "South"], Texts(cut));
        Assert.Single(browser.ScrolledTo);
    }

    [Fact]
    public void Check_ShouldLeaveOutParentsAboveATurnedOffRow_WhenItIsDeeperDown()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.ItemDisabled, place => place.Name == "Summit").Add(tree => tree.InitialExpandDepth, 2));

        Check(cut, "1");

        Assert.Equal([6], _checkedChanges.Single());
        Assert.Equal("mixed", Row(cut, "1").GetAttribute("aria-checked"));
        Assert.Equal("false", Row(cut, "2").GetAttribute("aria-checked"));
    }

    [Fact]
    public async Task ScrollTo_ShouldOpenRowsTheUserClosedDuringAFilter_WhenTheTargetIsInside()
    {
        var browser = Services.GetRequiredService<FakeBrowserService>();
        var cut = RenderPlaces(Nested,
            parameters => parameters.Add(tree => tree.Filter, place => place.Name == "Summit"));
        Toggle(cut, "2");

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(3));

        Assert.Equal(["North", "Ridge", "Summit"], Texts(cut));
        Assert.Single(browser.ScrolledTo);
    }

    [Fact]
    public async Task ScrollTo_ShouldNotScroll_WhenTheFilterHidesARowThatWasShown()
    {
        var browser = Services.GetRequiredService<FakeBrowserService>();
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1));
        cut.Render(parameters => parameters.Add(tree => tree.Filter, place => place.Name == "Summit"));

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(5));

        Assert.Empty(browser.ScrolledTo);
    }

    [Fact]
    public void Render_ShouldSayItIsEmpty_WhenThereAreNoItems()
    {
        var cut = RenderPlaces([]);

        Assert.Equal("Nothing to show.", cut.Find("[data-part=empty]").TextContent.Trim());
    }

    [Fact]
    public void Select_ShouldReportTheItemsThemselves_WhenTheTreeHasNoKeySelector()
    {
        IReadOnlyCollection<string> selected = [];
        var cut = Render<TbTreeView<string, string>>(parameters => parameters
            .Add(tree => tree.Items, ["Trails", "Huts"])
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<string> keys) => selected = keys));

        cut.Find("[data-key=Huts]").Click();

        Assert.Equal(["Huts"], selected);
    }

    private TreeViewEvents Events() =>
        ((DotNetObjectReference<TreeViewEvents>)JSInterop.Invocations["createTreeView"].Single().Arguments[1]!).Value;

    [Fact]
    public void Render_ShouldBuildTheRowsOnce_WhenTheParentRendersAgainAndPassesTheOpenRowsBack()
    {
        var built = 0;
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.ChildrenSelector, place =>
        {
            built++;
            return place.Children;
        }), nested: false);

        Toggle(cut, "1");
        cut.Render(parameters => parameters.Add(tree => tree.ExpandedKeys, _expandedChanges[^1]));
        cut.Render(parameters => parameters.Add(tree => tree.AriaLabel, "Places"));

        Assert.Equal(Nested.Count + 4, built);
        Assert.Equal(["North", "Ridge", "Pass", "South"], Texts(cut));
    }

    [Fact]
    public void Filter_ShouldNotRunAgain_WhenTheParentPassesBackTheTreesOwnOpenRows()
    {
        var filtered = 0;
        Func<Place, bool> roots = place =>
        {
            filtered++;
            return place.Id is 1 or 4;
        };
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Filter, roots)
            .Add(tree => tree.FilterKeepsDescendants, true));
        Toggle(cut, "1");
        var before = filtered;

        cut.Render(parameters => parameters.Add(tree => tree.ExpandedKeys, _expandedChanges[^1]));

        Assert.Equal(before, filtered);
        Assert.Equal(["North", "Ridge", "Pass", "South"], Texts(cut));
    }

    [Fact]
    public void Filter_ShouldShowTheNewMatches_WhenTheParentPassesAFilterFromTheSameMethodReadingNewText()
    {
        var search = new Search { Text = "Ridge" };
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Filter, search.Matches));

        search.Text = "Lake";
        cut.Render(parameters => parameters.Add(tree => tree.Filter, search.Matches));

        Assert.Equal(["South", "Lake"], Texts(cut));
    }

    [Fact]
    public async Task RefreshAsync_ShouldShowChildrenAddedInPlace_WhenCalled()
    {
        List<Place> children = [new(2, "Ridge")];
        var cut = RenderPlaces([new(1, "North", Children: children)], parameters => parameters
            .Add(tree => tree.InitialExpandDepth, 1));
        children.Add(new(6, "Pass"));

        cut.Render(parameters => parameters.Add(tree => tree.AriaLabel, "Places"));
        Assert.Equal(["North", "Ridge"], Texts(cut));
        await cut.InvokeAsync(cut.Instance.RefreshAsync);

        Assert.Equal(["North", "Ridge", "Pass"], Texts(cut));
    }

    [Fact]
    public async Task RefreshAsync_ShouldLoadTheChildrenOfAnOpenRowItAdds_WhenCalled()
    {
        List<Place> places = [new(1, "North")];
        var cut = RenderPlaces(places, parameters => parameters.Add(tree => tree.ExpandedKeys, [4])
            .Add(tree => tree.LoadChildren, (place, _) =>
                Task.FromResult<IEnumerable<Place>>(place.Id == 4 ? [new(5, "Lake")] : [])), nested: false);
        places.Add(new(4, "South"));

        await cut.InvokeAsync(cut.Instance.RefreshAsync);

        Assert.Equal(["North", "South", "Lake"], Texts(cut));
    }

    [Fact]
    public void Render_ShouldAddTheItemsClassToItsRow_WhenGivenAnItemClass()
    {
        var cut = RenderPlaces(Nested, parameters => parameters
            .Add(tree => tree.ItemClass, place => place.Id == 4 ? "needs-review" : null));

        Assert.Contains("needs-review", Row(cut, "4").ClassList);
        Assert.DoesNotContain("needs-review", Row(cut, "1").ClassList);
    }

    [Fact]
    public void Render_ShouldGiveTheFirstSelectedRowTheTabStop_WhenNoRowHasBeenFocused()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectedKeys, [4]));

        Assert.Equal("0", Row(cut, "4").GetAttribute("tabindex"));
        Assert.Equal("-1", Row(cut, "1").GetAttribute("tabindex"));
    }

    [Fact]
    public void Click_ShouldDrawOnlyTheRowsThatChange_WhenOneRowIsSelected()
    {
        IReadOnlyList<Place> places = [new(1, "North"), new(2, "East"), new(3, "South"), new(4, "West")];
        var cut = RenderPlaces(places, nested: false);
        var rows = cut.FindComponents<TreeItemRow>();
        var before = rows.Select(row => row.RenderCount).ToList();

        Row(cut, "3").Click();

        // The third row is now selected, and the tab stop moves to it from the first.
        Assert.Equal([1, 0, 1, 0], rows.Select((row, index) => row.RenderCount - before[index]));
        Assert.Equal("true", Row(cut, "3").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Toggle_ShouldLeaveTheRowsBelowAlone_WhenARowOpens()
    {
        var cut = RenderPlaces(Nested);
        var south = cut.FindComponents<TreeItemRow>().Single(row => row.Find("[role=treeitem]").GetAttribute("data-key")
            == "4");
        var before = south.RenderCount;

        Toggle(cut, "1");

        Assert.Equal("true", Row(cut, "1").GetAttribute("aria-expanded"));
        Assert.Equal(before, south.RenderCount);
    }

    [Fact]
    public void Render_ShouldDrawEveryRowAgain_WhenTheParentRendersAgain()
    {
        IReadOnlyList<Place> places = [new(1, "North"), new(2, "East")];
        var cut = RenderPlaces(places, nested: false);

        cut.Render(parameters => parameters.Add(tree => tree.ItemClass, place => $"place-{place.Id}"));

        Assert.Contains("place-1", Row(cut, "1").ClassList);
        Assert.Contains("place-2", Row(cut, "2").ClassList);
    }

    [Fact]
    public async Task RefreshAsync_ShouldDrawEveryRowAgain_WhenWhatTheRowsShowChangedInPlace()
    {
        var names = new Dictionary<int, string> { [1] = "North", [2] = "East" };
        IReadOnlyList<Place> places = [new(1, "North"), new(2, "East")];
        var cut = RenderPlaces(places, parameters => parameters.Add(tree => tree.ItemText, place => names[place.Id]),
            nested: false);
        names[2] = "Far East";

        await cut.InvokeAsync(cut.Instance.RefreshAsync);

        Assert.Equal(["North", "Far East"], Texts(cut));
    }

    [Fact]
    public async Task Drop_ShouldDrawEveryRowAgain_WhenTheAppChangesWhatTheRowsShowInPlace()
    {
        var names = new Dictionary<int, string> { [1] = "North", [2] = "East", [3] = "South" };
        IReadOnlyList<Place> places = [new(1, "North"), new(2, "East"), new(3, "South")];
        var cut = RenderPlaces(places, parameters => parameters.Add(tree => tree.ItemText, place => names[place.Id])
            .Add(tree => tree.OnMove, (TreeMove<Place> _) => names[3] = "Deep South"), nested: false);

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["2"], Target = "1", Position = TreeMovePosition.Before });

        Assert.Equal("Deep South", Row(cut, "3").GetAttribute("data-text"));
    }

    private static IElement Row(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        cut.Find($"[role=treeitem][data-key='{key}']");

    private static void Toggle(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        Row(cut, key).QuerySelector("[data-part=toggle]")!.Click();

    private static void Check(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        Row(cut, key).QuerySelector("[data-part=check]")!.Click();

    [Fact]
    public async Task SelectAsync_ShouldSelectOnlyThatRowAndRaiseTheChange_WhenGivenAKey()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.Multiple));

        await cut.InvokeAsync(() => cut.Instance.SelectAsync(1));
        await cut.InvokeAsync(() => cut.Instance.SelectAsync(4));

        Assert.Equal([[1], [4]], _selectedChanges);
        Assert.Equal("true", cut.Find("[role=treeitem][data-key='4']").GetAttribute("aria-selected"));
        Assert.Equal("false", cut.Find("[role=treeitem][data-key='1']").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task SelectAsync_ShouldChangeNothing_WhenTheRowCantBeSelectedOrNoRowHasTheKey()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.IsSelectable, place => place.Id != 4));

        await cut.InvokeAsync(() => cut.Instance.SelectAsync(4));
        await cut.InvokeAsync(() => cut.Instance.SelectAsync(99));

        Assert.Empty(_selectedChanges);
    }

    [Fact]
    public void Render_ShouldAddPartClasses_WhenClassesAreSet()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.ItemEndTemplate, context => builder => builder.AddContent(0, "3 new"))
            .Add(tree => tree.InsertTemplate, context => builder => builder.AddContent(0, "Add"))
            .Add(tree => tree.Classes, new TreeViewClasses
            {
                Item = "my-item", Toggle = "my-toggle", Check = "my-check", Content = "my-content", End = "my-end",
                Insert = "my-insert",
            }));

        foreach (var (part, name) in new[]
        {
            ("item", "my-item"), ("toggle", "my-toggle"), ("check", "my-check"), ("content", "my-content"),
            ("end", "my-end"), ("insert", "my-insert"),
        })
        {
            var parts = cut.FindAll($"[data-part={part}]");
            Assert.NotEmpty(parts);
            Assert.All(parts, element => Assert.Contains(name, element.ClassList));
        }
    }

    [Fact]
    public void Render_ShouldAddTheEmptyClass_WhenTheTreeHasNoItems()
    {
        var cut = RenderPlaces([], parameters => parameters.Add(tree => tree.Classes,
            new TreeViewClasses { Empty = "my-empty" }));

        Assert.Contains("my-empty", cut.Find("[data-part=empty]").ClassList);
    }

    private static string[] Texts(IRenderedComponent<TbTreeView<Place, int>> cut) =>
        [.. cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-text")!)];

    // Nested places give their own children; the others come as a flat list or load lazily.
    private IRenderedComponent<TbTreeView<Place, int>> RenderPlaces(IReadOnlyList<Place> items,
        Action<ComponentParameterCollectionBuilder<TbTreeView<Place, int>>>? configure = null, bool nested = true) =>
        Render<TbTreeView<Place, int>>(parameters =>
        {
            if (nested)
            {
                parameters.Add(tree => tree.ChildrenSelector, place => place.Children);
            }

            parameters.Add(tree => tree.Items, items).Add(tree => tree.KeySelector, place => place.Id)
                .Add(tree => tree.AriaLabel, "Places")
                .Add(tree => tree.ExpandedKeysChanged, (IReadOnlyCollection<int> keys) => _expandedChanges.Add(keys))
                .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => _selectedChanges.Add(keys))
                .Add(tree => tree.CheckedKeysChanged, (IReadOnlyCollection<int> keys) => _checkedChanges.Add(keys))
                .Add(tree => tree.OnActivate, (Place place) => _activated.Add(place.Name));
            configure?.Invoke(parameters);
        });
}
