using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TreeReorderTests : BunitContext
{
    private readonly List<TreeMove<Place>> _drops = [];

    public TreeReorderTests()
    {
        this.AddTrailblazor();
    }

    private sealed record Place(int Id, string Name, IReadOnlyList<Place>? Children = null)
    {
        public override string ToString() => Name;
    }

    // North (1) holds Ridge (2), which holds Summit (3), and Pass (6); South (4) holds Lake (5).
    private static IReadOnlyList<Place> Nested =>
    [
        new(1, "North", [new(2, "Ridge", [new(3, "Summit")]), new(6, "Pass")]),
        new(4, "South", [new(5, "Lake")]),
    ];

    [Fact]
    public void Render_ShouldMarkTheRowsThatMoveAndDescribeTheirKeys_WhenOnMoveIsSet()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanMoveItem, place => place.Name != "South"));

        var north = Row(cut, "1");
        Assert.True(north.HasAttribute("data-draggable"));
        Assert.Equal("Alt+Shift and the arrow keys move this item.",
            cut.Find($"#{north.GetAttribute("aria-describedby")}").TextContent);
        Assert.False(Row(cut, "4").HasAttribute("data-draggable"));
        Assert.Null(Row(cut, "4").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Render_ShouldLetNoRowMove_WhenOnMoveIsNotSet()
    {
        var cut = Render<TbTreeView<Place, int>>(parameters => parameters.Add(tree => tree.Items, Nested)
            .Add(tree => tree.ChildrenSelector, place => place.Children)
            .Add(tree => tree.KeySelector, place => place.Id).Add(tree => tree.AriaLabel, "Places"));

        Assert.Empty(cut.FindAll("[data-draggable]"));
        Assert.Empty(cut.FindAll("[data-part=drop-reason]"));
    }

    [Fact]
    public async Task DragOver_ShouldRefuseAMoveIntoTheItemsOwnBranch_WhenTheTargetIsItsDescendant()
    {
        var cut = RenderPlaces();

        var check = await cut.InvokeAsync(() => Events().OnDragOver(["1"], "2", TreeMovePosition.Inside));

        Assert.Equal(MoveCheck.Refuse("An item can't move into itself."), check);
    }

    [Fact]
    public async Task DragOver_ShouldAskTheApp_WhenTheMoveIsOtherwiseAllowed()
    {
        var asked = new List<TreeMove<Place>>();
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanMove, drop =>
        {
            asked.Add(drop);
            return Task.FromResult(drop.Position == TreeMovePosition.Inside && drop.Target.Children is null
                ? MoveCheck.Refuse("Lakes can't hold places.") : MoveCheck.Allow);
        }));

        var refused = await cut.InvokeAsync(() => Events().OnDragOver(["6"], "5", TreeMovePosition.Inside));
        var allowed = await cut.InvokeAsync(() => Events().OnDragOver(["6"], "5", TreeMovePosition.Before));

        Assert.Equal(MoveCheck.Refuse("Lakes can't hold places."), refused);
        Assert.Equal(MoveCheck.Allow, allowed);
        Assert.Equal(["Pass"], asked[0].Items.Select(place => place.Name));
        Assert.Equal("Lake", asked[0].Target.Name);
    }

    [Fact]
    public async Task Drop_ShouldAskTheLocalizerForTheCountedWords_WhenSeveralRowsMove()
    {
        Services.AddScoped<ITbLocalizer, PluralRecorder>();
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces();

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["2", "4"], Target = "6", Position = TreeMovePosition.After });

        announcer.WaitForAssertion(() => Assert.Equal("TreeView.ItemCount for 2 moved after Pass.",
            announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task Drop_ShouldRaiseOnMoveAndSayWhatMoved_WhenTheMoveIsAllowed()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces();

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["2", "4"], Target = "6", Position = TreeMovePosition.After });

        var drop = Assert.Single(_drops);
        Assert.Equal(["Ridge", "South"], drop.Items.Select(place => place.Name));
        Assert.Equal(("Pass", TreeMovePosition.After), (drop.Target.Name, drop.Position));
        announcer.WaitForAssertion(() =>
            Assert.Equal("2 items moved after Pass.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task Drop_ShouldSayWhyAndNotRaiseOnMove_WhenTheAppRefuses()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanMove,
            _ => Task.FromResult(MoveCheck.Refuse("Places are read only."))));

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["6"], Target = "4", Position = TreeMovePosition.Inside });

        Assert.Empty(_drops);
        announcer.WaitForAssertion(() =>
            Assert.Equal("Places are read only.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task Drop_ShouldOpenTheTarget_WhenItemsMoveInsideIt()
    {
        var cut = RenderPlaces();

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["6"], Target = "4", Position = TreeMovePosition.Inside });

        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-expanded"));
    }

    [Theory]
    [InlineData("6", "MoveUp", "Ridge", TreeMovePosition.Before)]
    [InlineData("2", "MoveDown", "Pass", TreeMovePosition.After)]
    [InlineData("6", "Indent", "Ridge", TreeMovePosition.Inside)]
    [InlineData("2", "Outdent", "North", TreeMovePosition.After)]
    public async Task Keys_ShouldMoveTheRowPastItsNeighborIntoTheRowAboveOrOutOfItsParent_WhenAltShiftArrowsAreReported(
        string key, string move, string target, TreeMovePosition position)
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.InitialExpandDepth, 2));

        await cut.InvokeAsync(() => Events().OnKey(key, move, false, false));

        var drop = Assert.Single(_drops);
        Assert.Equal((target, position), (drop.Target.Name, drop.Position));
    }

    [Theory]
    [InlineData("1", "MoveUp")]
    [InlineData("4", "MoveDown")]
    [InlineData("1", "Indent")]
    [InlineData("1", "Outdent")]
    public async Task Keys_ShouldMoveNothing_WhenTheRowHasNoNeighborOrParentThatWay(string key, string move)
    {
        var cut = RenderPlaces();

        await cut.InvokeAsync(() => Events().OnKey(key, move, false, false));

        Assert.Empty(_drops);
    }

    [Fact]
    public async Task Drop_ShouldMoveOnlyTheOutermostRows_WhenARowAndOneInsideItAreBothDragged()
    {
        var cut = RenderPlaces();

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["1", "2"], Target = "4", Position = TreeMovePosition.After });

        Assert.Equal(["North"], Assert.Single(_drops).Items.Select(place => place.Name));
    }

    [Fact]
    public async Task Keys_ShouldMoveNothing_WhenTheRowCantBeDragged()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanMoveItem, place => place.Name != "South"));

        await cut.InvokeAsync(() => Events().OnKey("4", "MoveUp", false, false));

        Assert.Empty(_drops);
    }

    [Fact]
    public async Task Keys_ShouldMovePastTheNeighborsThatShow_WhenAFilterHidesSome()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.Filter,
            place => place.Name is not ("Ridge" or "Summit")));

        await cut.InvokeAsync(() => Events().OnKey("6", "MoveUp", false, false));

        Assert.Empty(_drops);
    }

    [Fact]
    public void Render_ShouldNameEachRowByItsContentAndEnd_WhenItHasInsertSlots()
    {
        var cut = RenderPlaces(parameters => parameters
            .Add(tree => tree.ItemEnd, place => builder => builder.AddContent(0, "end"))
            .Add(tree => tree.InsertTemplate, _ => builder => builder.AddContent(0, "insert")));

        var row = Row(cut, "1");
        var parts = row.GetAttribute("aria-labelledby")!.Split(' ').Select(id => cut.Find($"#{id}").TextContent.Trim());
        Assert.Equal(["North", "end"], parts);
    }

    [Fact]
    public void Render_ShouldTellEachInsertSlotWhereItsItemWouldGo_WhenGivenAnInsertTemplate()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.InitialExpandDepth, 1)
            .Add(tree => tree.InsertTemplate, insert => builder =>
                builder.AddContent(0, $"{insert.Parent?.Name ?? "top"}:{insert.Index}")));

        Assert.Equal(["top:0", "North:0", "North:1", "North:2", "South:0", "South:1"],
            cut.FindAll("[data-part=insert]").Select(slot => slot.TextContent.Trim()));
    }

    [Fact]
    public async Task Drop_ShouldShowTheMove_WhenTheAppMovesItsItemsInPlace()
    {
        List<Place> places = [new(1, "North"), new(4, "South")];
        var cut = Render<TbTreeView<Place, int>>(parameters => parameters.Add(tree => tree.Items, places)
            .Add(tree => tree.KeySelector, place => place.Id).Add(tree => tree.AriaLabel, "Places")
            .Add(tree => tree.OnMove, (TreeMove<Place> drop) =>
            {
                places.Remove(drop.Items[0]);
                places.Insert(places.IndexOf(drop.Target), drop.Items[0]);
            }));

        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["4"], Target = "1", Position = TreeMovePosition.Before });

        Assert.Equal(["4", "1"], cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("data-key")));
    }

    [Theory]
    [InlineData(TreeMovePosition.Before)]
    [InlineData(TreeMovePosition.After)]
    [InlineData(TreeMovePosition.Inside)]
    public async Task TreeDriver_ShouldReportTheDropThroughOnMove_WhenItDragsARowOntoAnother(TreeMovePosition position)
    {
        var cut = RenderPlaces();
        var tree = cut.Tb().Tree("Places");
        await tree.ExpandAllAsync();
        Func<string[], string[], Task> drag = position switch
        {
            TreeMovePosition.Before => tree.DragBeforeAsync,
            TreeMovePosition.After => tree.DragAfterAsync,
            _ => tree.DragIntoAsync,
        };

        await drag(["North", "Pass"], ["South", "Lake"]);

        var drop = Assert.Single(_drops);
        Assert.Equal(["Pass"], drop.Items.Select(place => place.Name));
        Assert.Equal(("Lake", position), (drop.Target.Name, drop.Position));
    }

    [Fact]
    public async Task TreeDriver_ShouldDropAboveTheFirstChild_WhenItDragsARowBelowAnOpenOne()
    {
        var cut = RenderPlaces();
        var tree = cut.Tb().Tree("Places");
        await tree.ExpandAllAsync();

        await tree.DragAfterAsync(["North", "Pass"], ["South"]);

        var drop = Assert.Single(_drops);
        Assert.Equal(("Lake", TreeMovePosition.Before), (drop.Target.Name, drop.Position));
    }

    [Fact]
    public async Task TreeDriver_ShouldMoveEverySelectedRow_WhenItDragsASelectedOne()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.Multiple)
            .Add(tree => tree.SelectedKeys, [2, 6]));
        var tree = cut.Tb().Tree("Places");
        await tree.ExpandAllAsync();

        await tree.DragAfterAsync(["North", "Pass"], ["South", "Lake"]);

        Assert.Equal(["Ridge", "Pass"], Assert.Single(_drops).Items.Select(place => place.Name));
    }

    [Fact]
    public async Task TreeDriver_ShouldDropNothing_WhenTheTargetIsOneOfTheMovingRows()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.Multiple)
            .Add(tree => tree.SelectedKeys, [2, 6]));
        var tree = cut.Tb().Tree("Places");
        await tree.ExpandAllAsync();

        await tree.DragBeforeAsync(["North", "Pass"], ["North", "Ridge"]);
        await tree.DragAfterAsync(["North", "Pass"], ["North", "Ridge"]);

        Assert.Empty(_drops);
        Assert.Empty(announcer.FindAll("[role=status] p"));
    }

    [Fact]
    public async Task TreeDriver_ShouldDropNothing_WhenADropBelowAnOpenRowLandsOnAMovingChild()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.SelectionMode, SelectionMode.Multiple)
            .Add(tree => tree.SelectedKeys, [3, 6]));
        var tree = cut.Tb().Tree("Places");
        await tree.ExpandAllAsync();

        await tree.DragAfterAsync(["North", "Pass"], ["North", "Ridge"]);

        Assert.Empty(_drops);
        Assert.Empty(announcer.FindAll("[role=status] p"));
    }

    [Fact]
    public async Task TreeDriver_ShouldSayTheRowCantBeDragged_WhenTheTreeHasNoOnMove()
    {
        var cut = Render<TbTreeView<Place, int>>(parameters => parameters.Add(tree => tree.Items, Nested)
            .Add(tree => tree.ChildrenSelector, place => place.Children)
            .Add(tree => tree.KeySelector, place => place.Id).Add(tree => tree.AriaLabel, "Places"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Tree("Places").DragAfterAsync(["North"], ["South"]));
    }

    private TreeViewEvents Events() =>
        ((DotNetObjectReference<TreeViewEvents>)JSInterop.Invocations["createTreeView"].Single().Arguments[1]!).Value;

    private static IElement Row(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        cut.Find($"[role=treeitem][data-key='{key}']");

    private IRenderedComponent<TbTreeView<Place, int>> RenderPlaces(
        Action<ComponentParameterCollectionBuilder<TbTreeView<Place, int>>>? configure = null) =>
        Render<TbTreeView<Place, int>>(parameters =>
        {
            parameters.Add(tree => tree.Items, Nested).Add(tree => tree.ChildrenSelector, place => place.Children)
                .Add(tree => tree.KeySelector, place => place.Id).Add(tree => tree.AriaLabel, "Places")
                .Add(tree => tree.OnMove, (TreeMove<Place> drop) => _drops.Add(drop));
            configure?.Invoke(parameters);
        });
}
