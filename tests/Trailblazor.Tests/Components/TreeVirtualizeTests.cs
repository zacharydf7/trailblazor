using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TreeVirtualizeTests : BunitContext
{
    public TreeVirtualizeTests() => this.AddTrailblazor();

    private sealed record Place(int Id, string Name, IReadOnlyList<Place>? Children = null);

    // A thousand rows, Place 1 to Place 1000, keyed by their number.
    private static IReadOnlyList<Place> Flat =>
        [.. Enumerable.Range(1, 1000).Select(id => new Place(id, $"Place {id}"))];

    private static IReadOnlyList<Place> Names =>
        [new(1, "Anna"), new(2, "Abby"), new(3, "Aaron"), new(4, "Banana"), new(5, "Bread")];

    [Fact]
    public void Render_ShouldRenderTheFirstRowsAndASpacerForTheRest_WhenVirtualized()
    {
        var cut = RenderPlaces(Flat);

        var rows = cut.FindAll("[role=treeitem]");
        Assert.Equal(40, rows.Count);
        Assert.Equal(["40", "1000"], [rows[^1].GetAttribute("aria-posinset")!, rows[^1].GetAttribute("aria-setsize")!]);
        Assert.Equal("--tb-tree-gap: 960", cut.Find("[data-part=spacer]").GetAttribute("style"));
        Assert.Equal("1000", cut.Find("[role=tree]").GetAttribute("data-rows"));
    }

    [Fact]
    public async Task OnView_ShouldRenderTheRowsInViewBesideTheFocusedOnes_WhenTheBrowserReportsThem()
    {
        var cut = RenderPlaces(Flat);

        await cut.InvokeAsync(() => Events().OnView(500, 20));

        Assert.Equal([0, 1, .. Enumerable.Range(500, 20)], Indexes(cut));
        Assert.Equal(["--tb-tree-gap: 498", "--tb-tree-gap: 480"],
            cut.FindAll("[data-part=spacer]").Select(spacer => spacer.GetAttribute("style")!));
    }

    [Theory]
    [InlineData("1", "Last", "1000")]
    [InlineData("1000", "First", "1")]
    [InlineData("40", "Next", "41")]
    [InlineData("41", "Previous", "40")]
    public async Task OnKey_ShouldRenderAndFocusTheRow_WhenAKeyMovesToOneThatIsntRendered(string from, string key,
        string to)
    {
        var cut = RenderPlaces(Flat);
        await cut.InvokeAsync(() => Events().OnView(int.Parse(from) - 1, 1));

        await cut.InvokeAsync(() => Events().OnKey(from, key, false, false));

        Assert.Equal("0", Row(cut, to).GetAttribute("tabindex"));
        Assert.Single(Services.GetRequiredService<FakeBrowserService>().ScrolledTo);
    }

    [Fact]
    public async Task OnKey_ShouldFocusTheParent_WhenParentIsPressedOnARowWhoseParentIsntRendered()
    {
        Place[] north = [new(1, "North", [.. Enumerable.Range(2, 500).Select(id => new Place(id, $"Trail {id}"))])];
        var cut = RenderPlaces(north, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1));
        await cut.InvokeAsync(() => Events().OnView(400, 20));
        await cut.InvokeAsync(() => Events().OnFocus("410"));

        await cut.InvokeAsync(() => Events().OnKey("410", "Parent", false, false));

        Assert.Equal("0", Row(cut, "1").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task OnKey_ShouldExtendTheSelection_WhenShiftMovesFocusThroughTheServer()
    {
        IReadOnlyCollection<int> selected = [];
        var cut = RenderPlaces(Flat, parameters => parameters
            .Add(tree => tree.SelectionMode, SelectionMode.Multiple)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => selected = keys));
        await cut.InvokeAsync(() => Events().OnKey("40", " ", false, false));

        await cut.InvokeAsync(() => Events().OnKey("40", "Next", true, false));

        Assert.Equal([40, 41], selected.Cast<int>().Order());
    }

    [Fact]
    public async Task OnKey_ShouldExtendToTheLastAndFirstRows_WhenShiftWithEndOrHomeStartsWithNothingChosen()
    {
        IReadOnlyCollection<int> selected = [];
        var cut = RenderPlaces(Flat, parameters => parameters
            .Add(tree => tree.SelectionMode, SelectionMode.Multiple)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => selected = keys));

        await cut.InvokeAsync(() => Events().OnKey("2", "Last", true, false));
        Assert.Equal(999, selected.Count);
        await cut.InvokeAsync(() => Events().OnKey("1000", "First", true, false));

        Assert.Equal([1, 2], selected.Cast<int>().Order());
    }

    [Fact]
    public async Task OnKey_ShouldOnlyMoveFocus_WhenShiftWithEndIsPressedInASingleSelectionTree()
    {
        IReadOnlyCollection<int> selected = [];
        var cut = RenderPlaces(Flat, parameters => parameters
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => selected = keys));

        await cut.InvokeAsync(() => Events().OnKey("2", "Last", true, false));

        Assert.Empty(selected);
    }

    [Theory]
    [InlineData("Next", false, false, "41", new[] { 41 })]
    [InlineData("Next", false, true, "41", new int[0])]
    [InlineData("Last", true, false, "1000", new[] { 1000 })]
    public async Task OnKey_ShouldSelectTheRowItMovesToUnlessCtrlIsHeld_WhenSelectionFollowsFocus(string key,
        bool shift, bool ctrl, string to, int[] expected)
    {
        IReadOnlyCollection<int> selected = [];
        var cut = RenderPlaces(Flat, parameters => parameters.Add(tree => tree.SelectionFollowsFocus, true)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => selected = keys));
        await cut.InvokeAsync(() => Events().OnView(39, 1));

        await cut.InvokeAsync(() => Events().OnKey("40", key, shift, ctrl));

        Assert.Equal("0", Row(cut, to).GetAttribute("tabindex"));
        Assert.Equal(expected, selected);
    }

    [Fact]
    public async Task OnFind_ShouldSelectTheRowItFinds_WhenSelectionFollowsFocus()
    {
        IReadOnlyCollection<int> selected = [];
        var cut = RenderPlaces(Flat, parameters => parameters.Add(tree => tree.SelectionFollowsFocus, true)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => selected = keys));

        await cut.InvokeAsync(() => Events().OnFind("1", "place 70"));

        Assert.Equal([70], selected);
    }

    [Fact]
    public async Task OnKey_ShouldMoveFocusWithoutSelecting_WhenTheTreeIsDisabled()
    {
        var changed = false;
        var cut = RenderPlaces(Flat, parameters => parameters.Add(tree => tree.Disabled, true)
            .Add(tree => tree.SelectionMode, SelectionMode.Multiple)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> _) => changed = true));

        await cut.InvokeAsync(() => Events().OnView(39, 1));

        await cut.InvokeAsync(() => Events().OnKey("40", "Next", true, false));

        Assert.Equal("0", Row(cut, "41").GetAttribute("tabindex"));
        Assert.False(changed);
    }

    [Fact]
    public async Task OnFind_ShouldFocusTheFirstRowStartingWithTheText_WhenItIsntRendered()
    {
        var cut = RenderPlaces(Flat);

        await cut.InvokeAsync(() => Events().OnFind("1", "place 70"));

        Assert.Equal("0", Row(cut, "70").GetAttribute("tabindex"));
    }

    [Theory]
    [InlineData("4", "bb", "5")]
    [InlineData("1", "aa", "3")]
    [InlineData("3", "aa", "1")]
    public async Task OnFind_ShouldStepOrFindTheDoubledLetter_WhenALetterIsRepeated(string from, string typed, string to)
    {
        var cut = RenderPlaces(Names);

        await cut.InvokeAsync(() => Events().OnFind(from, typed));

        Assert.Equal("0", Row(cut, to).GetAttribute("tabindex"));
    }

    [Theory]
    [InlineData("1", "aar")]
    [InlineData("3", "aar")]
    public async Task OnFind_ShouldFindTheRowWithADoubledLetter_WhenTypingGoesOn(string from, string typed)
    {
        var cut = RenderPlaces(Names);

        await cut.InvokeAsync(() => Events().OnFind(from, typed));

        Assert.Equal("0", Row(cut, "3").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task OnFind_ShouldStayOnTheRow_WhenDifferentLettersMatchNothing()
    {
        var cut = RenderPlaces(Names);

        await cut.InvokeAsync(() => Events().OnFind("4", "bx"));

        Assert.Empty(Services.GetRequiredService<FakeBrowserService>().ScrolledTo);
    }

    [Fact]
    public async Task OnFind_ShouldKeepTheRowTheBrowserFocusedRendered_WhenFocusMovesToAFarRow()
    {
        var cut = RenderPlaces(Flat);
        await cut.InvokeAsync(() => Events().OnFocus("1"));
        await cut.InvokeAsync(() => Events().OnView(500, 20));

        await cut.InvokeAsync(() => Events().OnFind("1", "place 900"));

        Assert.Contains(0, Indexes(cut));
        Assert.Equal("0", Row(cut, "900").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task ScrollTo_ShouldRenderFocusAndScrollToTheRow_WhenItIsntRendered()
    {
        var cut = RenderPlaces(Flat);

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(900, focus: true));

        Assert.Equal("0", Row(cut, "900").GetAttribute("tabindex"));
        Assert.Single(Services.GetRequiredService<FakeBrowserService>().ScrolledTo);
    }

    [Fact]
    public async Task ScrollTo_ShouldRenderTheRowAndScrollToIt_WhenItIsntRenderedAndIsntFocused()
    {
        var cut = RenderPlaces(Flat);

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(900));

        Assert.Equal("-1", Row(cut, "900").GetAttribute("tabindex"));
        Assert.Single(Services.GetRequiredService<FakeBrowserService>().ScrolledTo);
    }

    [Fact]
    public void Render_ShouldCountTheRowsInEachBranch_WhenVirtualized()
    {
        // North holds Ridge, which holds Summit and Peak, and Pass; South is closed over Lake.
        Place[] places =
        [
            new(1, "North", [new(2, "Ridge", [new(3, "Summit"), new(4, "Peak")]), new(5, "Pass")]),
            new(6, "South", [new(7, "Lake")]),
        ];

        var cut = RenderPlaces(places, parameters => parameters.Add(tree => tree.ExpandedKeys, [1, 2]));

        Assert.Equal(["4", "2", "0", "0", "0", "0"],
            cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("data-inside")!));
    }

    [Fact]
    public void Render_ShouldCountALoadingRowInItsBranch_WhenVirtualized()
    {
        var load = new TaskCompletionSource<IEnumerable<Place>>();
        var cut = RenderPlaces([new(1, "North"), new(2, "South")], parameters => parameters
            .Add(tree => tree.LoadChildren, (_, _) => load.Task).Add(tree => tree.ExpandedKeys, [1]));

        Assert.Equal(["1", "0", "0"], cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("data-inside")!));
        Assert.Equal("1", cut.Find("[data-part=loading]").GetAttribute("data-index"));
    }

    [Fact]
    public void Render_ShouldRenderEveryRow_WhenThePageIsntInteractive()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderPlaces(Flat);

        Assert.Equal(1000, cut.FindAll("[role=treeitem]").Count);
        Assert.Empty(cut.FindAll("[data-part=spacer]"));
    }

    private TreeViewEvents Events() =>
        ((DotNetObjectReference<TreeViewEvents>)JSInterop.Invocations["createTreeView"].Single().Arguments[1]!).Value;

    private static IEnumerable<int> Indexes(IRenderedComponent<TbTreeView<Place, int>> cut) =>
        cut.FindAll("[role=treeitem]").Select(row => int.Parse(row.GetAttribute("data-index")!));

    private static IElement Row(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        cut.Find($"[role=treeitem][data-key='{key}']");

    private IRenderedComponent<TbTreeView<Place, int>> RenderPlaces(IReadOnlyList<Place> places,
        Action<ComponentParameterCollectionBuilder<TbTreeView<Place, int>>>? configure = null) =>
        Render<TbTreeView<Place, int>>(parameters =>
        {
            parameters.Add(tree => tree.Items, places).Add(tree => tree.ChildrenSelector, place => place.Children)
                .Add(tree => tree.KeySelector, place => place.Id).Add(tree => tree.ItemText, place => place.Name)
                .Add(tree => tree.AriaLabel, "Places").Add(tree => tree.Virtualize, true);
            configure?.Invoke(parameters);
        });
}
