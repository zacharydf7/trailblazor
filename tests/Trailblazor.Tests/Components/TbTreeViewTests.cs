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
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private sealed record Place(int Id, string Name, int? ParentId = null, IReadOnlyList<Place>? Children = null)
    {
        public override string ToString() => Name;
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
    public void Click_ShouldLeaveARowUnselected_WhenItIsntSelectable()
    {
        var cut = RenderPlaces(Nested, parameters => parameters.Add(tree => tree.IsSelectable,
            place => place.Children is null));

        Row(cut, "1").Click();

        Assert.Empty(_selectedChanges);
        Assert.Null(Row(cut, "1").GetAttribute("aria-selected"));
    }

    [Fact]
    public void DoubleClick_ShouldActivateTheItem_WhenTheRowIsEnabled()
    {
        var cut = RenderPlaces(Nested);

        Row(cut, "1").DoubleClick();

        Assert.Equal(["North"], _activated);
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
            .Add(tree => tree.IsDisabled, place => place.Name == "Pass"));

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
            .Add(tree => tree.IsDisabled, place => place.Name == "Summit").Add(tree => tree.InitialExpandDepth, 2));

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

    private static IElement Row(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        cut.Find($"[role=treeitem][data-key='{key}']");

    private static void Toggle(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        Row(cut, key).QuerySelector("[data-part=toggle]")!.Click();

    private static void Check(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        Row(cut, key).QuerySelector("[data-part=check]")!.Click();

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
