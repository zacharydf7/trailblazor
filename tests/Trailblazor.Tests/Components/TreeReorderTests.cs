using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TreeReorderTests : BunitContext
{
    private readonly List<TreeDrop<Place>> _drops = [];

    public TreeReorderTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
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
    public void Render_ShouldMarkTheRowsThatMoveAndDescribeTheirKeys_WhenOnDropIsSet()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanDrag, place => place.Name != "South"));

        var north = Row(cut, "1");
        Assert.True(north.HasAttribute("data-draggable"));
        Assert.Equal("Alt+Shift and the arrow keys move this item.",
            cut.Find($"#{north.GetAttribute("aria-describedby")}").TextContent);
        Assert.False(Row(cut, "4").HasAttribute("data-draggable"));
        Assert.Null(Row(cut, "4").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Render_ShouldLetNoRowMove_WhenOnDropIsNotSet()
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

        var check = await cut.InvokeAsync(() => Events().OnDragOver(["1"], "2", TreeDropPosition.Inside));

        Assert.Equal(TreeDropCheck.Refuse("An item can't move into itself."), check);
    }

    [Fact]
    public async Task DragOver_ShouldAskTheApp_WhenTheMoveIsOtherwiseAllowed()
    {
        var asked = new List<TreeDrop<Place>>();
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanDrop, drop =>
        {
            asked.Add(drop);
            return Task.FromResult(drop.Position == TreeDropPosition.Inside && drop.Target.Children is null
                ? TreeDropCheck.Refuse("Lakes can't hold places.") : TreeDropCheck.Allow);
        }));

        var refused = await cut.InvokeAsync(() => Events().OnDragOver(["6"], "5", TreeDropPosition.Inside));
        var allowed = await cut.InvokeAsync(() => Events().OnDragOver(["6"], "5", TreeDropPosition.Before));

        Assert.Equal(TreeDropCheck.Refuse("Lakes can't hold places."), refused);
        Assert.Equal(TreeDropCheck.Allow, allowed);
        Assert.Equal(["Pass"], asked[0].Items.Select(place => place.Name));
        Assert.Equal("Lake", asked[0].Target.Name);
    }

    [Fact]
    public async Task Drop_ShouldRaiseOnDropAndSayWhatMoved_WhenTheMoveIsAllowed()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces();

        await cut.InvokeAsync(() => Events().OnDrop(["2", "4"], "6", TreeDropPosition.After));

        var drop = Assert.Single(_drops);
        Assert.Equal(["Ridge", "South"], drop.Items.Select(place => place.Name));
        Assert.Equal(("Pass", TreeDropPosition.After), (drop.Target.Name, drop.Position));
        announcer.WaitForAssertion(() =>
            Assert.Equal("2 items moved after Pass.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task Drop_ShouldSayWhyAndNotRaiseOnDrop_WhenTheAppRefuses()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanDrop,
            _ => Task.FromResult(TreeDropCheck.Refuse("Places are read only."))));

        await cut.InvokeAsync(() => Events().OnDrop(["6"], "4", TreeDropPosition.Inside));

        Assert.Empty(_drops);
        announcer.WaitForAssertion(() =>
            Assert.Equal("Places are read only.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task Drop_ShouldOpenTheTarget_WhenItemsMoveInsideIt()
    {
        var cut = RenderPlaces();

        await cut.InvokeAsync(() => Events().OnDrop(["6"], "4", TreeDropPosition.Inside));

        Assert.Equal("true", Row(cut, "4").GetAttribute("aria-expanded"));
    }

    [Theory]
    [InlineData("6", "MoveUp", "Ridge", TreeDropPosition.Before)]
    [InlineData("2", "MoveDown", "Pass", TreeDropPosition.After)]
    [InlineData("6", "Indent", "Ridge", TreeDropPosition.Inside)]
    [InlineData("2", "Outdent", "North", TreeDropPosition.After)]
    public async Task Keys_ShouldMoveTheRowPastItsNeighborIntoTheRowAboveOrOutOfItsParent_WhenAltShiftArrowsAreReported(
        string key, string move, string target, TreeDropPosition position)
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

        await cut.InvokeAsync(() => Events().OnDrop(["1", "2"], "4", TreeDropPosition.After));

        Assert.Equal(["North"], Assert.Single(_drops).Items.Select(place => place.Name));
    }

    [Fact]
    public async Task Keys_ShouldMoveNothing_WhenTheRowCantBeDragged()
    {
        var cut = RenderPlaces(parameters => parameters.Add(tree => tree.CanDrag, place => place.Name != "South"));

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
            .Add(tree => tree.OnDrop, (TreeDrop<Place> drop) =>
            {
                places.Remove(drop.Items[0]);
                places.Insert(places.IndexOf(drop.Target), drop.Items[0]);
            }));

        await cut.InvokeAsync(() => Events().OnDrop(["4"], "1", TreeDropPosition.Before));

        Assert.Equal(["4", "1"], cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("data-key")));
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
                .Add(tree => tree.OnDrop, (TreeDrop<Place> drop) => _drops.Add(drop));
            configure?.Invoke(parameters);
        });
}
