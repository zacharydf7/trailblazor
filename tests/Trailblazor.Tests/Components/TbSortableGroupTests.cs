using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbSortableGroupTests : BunitContext
{
    private readonly List<string> _todo = ["Pack food", "Check weather"];
    private readonly List<string> _done = ["Book campsite"];
    private readonly List<SortableMove<string>> _moves = [];
    private Func<SortableMove<string>, Task<MoveCheck>>? _canMoveToDone;
    private bool _doneDisabled;
    private Func<SortableMove<string>, Task<MoveCheck>>? _canMoveInTodo;
    private SortableLabels _labels = new();

    public TbSortableGroupTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldNameWhatIsMissing_WhenAGroupHasNoListId()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbSortable<string>>(parameters => parameters
            .Add(list => list.Items, _todo).Add(list => list.Label, "To do").Add(list => list.Group, "board")
            .Add(list => list.ItemTemplate, task => builder => builder.AddContent(0, task))));

        Assert.Contains("needs a ListId", error.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTwoListsOfAGroupShareAListId()
    {
        Render<TbSortable<string>>(parameters => Column(parameters, "todo", "To do", _todo));

        var error = Assert.Throws<InvalidOperationException>(() =>
            Render<TbSortable<string>>(parameters => Column(parameters, "todo", "Also to do", _done)));

        Assert.Contains("shares its ListId", error.Message);
    }

    [Fact]
    public async Task ListKey_ShouldMoveTheItemToTheSamePlaceInTheListBesideAndSaySo_WhenPressed()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderBoard();

        await cut.InvokeAsync(() => Events(0).OnListKey("Pack food", "done"));

        Assert.Equal([new("Pack food", 0, 0) { FromList = "todo", ToList = "done" }], _moves);
        Assert.Equal(["Pack food", "Book campsite"], _done);
        announcer.WaitForAssertion(() => Assert.Equal("Pack food moved to Done, position 1 of 2.",
            announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task ListKey_ShouldPutTheItemAtTheEnd_WhenTheListBesideIsShorter()
    {
        _todo.Add("Fill water");
        var cut = RenderBoard();

        await cut.InvokeAsync(() => Events(0).OnListKey("Fill water", "done"));

        Assert.Equal(["Book campsite", "Fill water"], _done);
    }

    [Theory]
    [InlineData("Book campsite", true, 0)]
    [InlineData("Book campsite", false, 1)]
    [InlineData(null, false, 1)]
    public async Task Drop_ShouldPutTheItemWhereItWasDropped_WhenOnAnotherListOfTheGroup(string? target, bool before,
        int expected)
    {
        var cut = RenderBoard();

        var check = await cut.InvokeAsync(() => Events(0).OnDragOverList("Pack food", "done", target, before));
        await cut.InvokeAsync(() => Events(0).OnDropList("Pack food", "done", target, before));

        Assert.True(check.Allowed);
        Assert.Equal(expected, _done.IndexOf("Pack food"));
        Assert.Equal(["Check weather"], _todo);
    }

    [Fact]
    public async Task Drop_ShouldAskTheListItGoesToAndSayWhy_WhenThatListRefuses()
    {
        var announcer = Render<AnnouncerHost>();
        _canMoveToDone = move => Task.FromResult(move.FromList == "todo" && move.ToList == "done"
            ? MoveCheck.Refuse("Finish it first.") : MoveCheck.Allow);
        var cut = RenderBoard();

        var check = await cut.InvokeAsync(() => Events(0).OnDragOverList("Pack food", "done", null, false));
        await cut.InvokeAsync(() => Events(0).OnDropList("Pack food", "done", null, false));

        Assert.Equal(MoveCheck.Refuse("Finish it first."), check);
        Assert.Empty(_moves);
        announcer.WaitForAssertion(() => Assert.Contains("Finish it first.",
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public async Task Drop_ShouldBeRefused_WhenTheListItGoesToIsDisabled()
    {
        _doneDisabled = true;
        var cut = RenderBoard();

        var check = await cut.InvokeAsync(() => Events(0).OnDragOverList("Pack food", "done", null, false));
        await cut.InvokeAsync(() => Events(0).OnListKey("Pack food", "done"));

        Assert.False(check.Allowed);
        Assert.Empty(_moves);
    }

    [Fact]
    public async Task Keys_ShouldNameTheListAsBothEnds_WhenTheItemMovesWithinIt()
    {
        var cut = RenderBoard();

        await cut.InvokeAsync(() => Events(0).OnKey("Pack food", "ArrowDown"));

        Assert.Equal([new("Pack food", 0, 1) { FromList = "todo", ToList = "todo" }], _moves);
    }

    [Fact]
    public async Task DragOver_ShouldNameTheListAsBothEnds_WhenTheItemIsDraggedWithinIt()
    {
        var asked = new List<SortableMove<string>>();
        _canMoveInTodo = move =>
        {
            asked.Add(move);
            return Task.FromResult(MoveCheck.Allow);
        };
        var cut = RenderBoard();

        await cut.InvokeAsync(() => Events(0).OnDragOver("Pack food", "Check weather", before: false));
        await cut.InvokeAsync(() => Events(0).OnDrop("Pack food", "Check weather", before: false));

        SortableMove<string> expected = new("Pack food", 0, 1) { FromList = "todo", ToList = "todo" };
        Assert.Equal([expected, expected], asked);
    }

    [Fact]
    public void ApplyTo_ShouldThrowAndLeaveTheList_WhenTheMoveIsBetweenLists()
    {
        var move = new SortableMove<string>("Pack food", 0, 0) { FromList = "todo", ToList = "done" };

        var error = Assert.Throws<InvalidOperationException>(() => move.ApplyTo(_done));

        Assert.Contains("ApplyBetween", error.Message);
        Assert.Equal(["Book campsite"], _done);
    }

    [Fact]
    public async Task Labels_ShouldWordTheMenuItemsAndTheAnnouncementForOtherLists_WhenGiven()
    {
        var announcer = Render<AnnouncerHost>();
        _labels = new SortableLabels { MoveToList = "Send to {0}", MovedToList = "{0} sent to {3}, {1} of {2}." };
        var cut = RenderBoard();

        await cut.InvokeAsync(() => Events(0).OnListKey("Pack food", "done"));

        Assert.Contains(cut.FindAll(".tb-sortable")[1].QuerySelectorAll("[role=menuitem]"),
            choice => choice.TextContent.Trim() == "Send to To do");
        announcer.WaitForAssertion(() => Assert.Equal("Pack food sent to Done, 1 of 2.",
            announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task Menu_ShouldOfferEachOtherListAndMoveTheItemToItsEnd_WhenOneIsChosen()
    {
        var cut = RenderBoard();
        var menu = cut.FindAll(".tb-sortable")[0].QuerySelectorAll("[data-part=item]")[0];
        Assert.Contains("Left and Right", cut.Find($"#{menu.QuerySelector("[data-part=trigger]")!
            .GetAttribute("aria-describedby")}").TextContent);

        var item = menu.QuerySelectorAll("[role=menuitem]").Single(choice => choice.TextContent.Trim() == "Move to Done");
        await item.ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Equal(["Book campsite", "Pack food"], _done);
        Assert.DoesNotContain(cut.FindAll(".tb-sortable")[1].QuerySelectorAll("[role=menuitem]"),
            choice => choice.TextContent.Trim() == "Move to Done");
    }

    [Fact]
    public void Menu_ShouldOfferAList_WhenItJoinsTheGroupAfterTheOthersRendered()
    {
        // Two roots, as two parts of a page that render at different times.
        var todo = Render<TbSortable<string>>(parameters => Column(parameters, "todo", "To do", _todo));

        Render<TbSortable<string>>(parameters => Column(parameters, "done", "Done", _done));

        Assert.Contains(todo.FindAll("[role=menuitem]"), choice => choice.TextContent.Trim() == "Move to Done");
    }

    [Fact]
    public void Menu_ShouldOfferNoOtherList_WhenTheListLeavesItsGroup()
    {
        var cut = RenderBoard();

        cut.FindComponents<TbSortable<string>>()[1].Render(parameters => parameters.Add(list => list.Group, null));

        Assert.DoesNotContain(cut.FindAll(".tb-sortable")[0].QuerySelectorAll("[role=menuitem]"),
            choice => choice.TextContent.Trim() == "Move to Done");
        Assert.False(cut.FindAll(".tb-sortable")[1].QuerySelector("ul")!.HasAttribute("data-group"));
    }

    private IRenderedComponent<IComponent> RenderBoard() => Render(builder =>
    {
        Column(builder, 0, "todo", "To do", _todo, _canMoveInTodo, false);
        Column(builder, 20, "done", "Done", _done, _canMoveToDone, _doneDisabled);
    });

    private static void Column(ComponentParameterCollectionBuilder<TbSortable<string>> parameters, string id,
        string label, List<string> tasks) => parameters.Add(list => list.Items, tasks).Add(list => list.Label, label)
        .Add(list => list.Group, "board").Add(list => list.ListId, id)
        .Add(list => list.ItemTemplate, task => content => content.AddContent(0, task));

    private void Column(RenderTreeBuilder builder, int sequence, string id, string label, List<string> tasks,
        Func<SortableMove<string>, Task<MoveCheck>>? canMove, bool disabled)
    {
        builder.OpenComponent<TbSortable<string>>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbSortable<string>.Items), tasks);
        builder.AddComponentParameter(sequence + 2, nameof(TbSortable<string>.Label), label);
        builder.AddComponentParameter(sequence + 3, nameof(TbSortable<string>.Group), "board");
        builder.AddComponentParameter(sequence + 4, nameof(TbSortable<string>.ListId), id);
        builder.AddComponentParameter(sequence + 5, nameof(TbSortable<string>.ItemTemplate),
            (RenderFragment<string>)(task => content => content.AddContent(0, task)));
        builder.AddComponentParameter(sequence + 6, nameof(TbSortable<string>.CanMove), canMove);
        builder.AddComponentParameter(sequence + 7, nameof(TbSortable<string>.Disabled), disabled);
        builder.AddComponentParameter(sequence + 9, nameof(TbSortable<string>.Labels), _labels);
        builder.AddComponentParameter(sequence + 8, nameof(TbSortable<string>.OnMove),
            EventCallback.Factory.Create<SortableMove<string>>(this, move =>
            {
                _moves.Add(move);
                move.ApplyBetween(move.FromList == "todo" ? _todo : _done, move.ToList == "todo" ? _todo : _done);
            }));
        builder.CloseComponent();
    }

    private SortableEvents Events(int list) =>
        ((DotNetObjectReference<SortableEvents>)JSInterop.Invocations["attachSortable"][list].Arguments[1]!).Value;
}
