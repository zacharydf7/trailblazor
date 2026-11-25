using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbKanbanTests : BunitContext
{
    private readonly Dictionary<string, List<string>> _cards = new()
    {
        ["todo"] = ["Clear the ridge path", "Mark the river ford"],
        ["doing"] = ["Fix the footbridge"],
        ["done"] = [],
    };
    private readonly List<SortableMove<string>> _moves = [];
    private int? _doneLimit;

    public TbKanbanTests() => this.AddTrailblazor();

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    [Fact]
    public void Render_ShouldShowEachColumnAsAGroupNamedByItsTitleWithItsCount_WhenGivenColumns()
    {
        var cut = RenderBoard();

        var columns = cut.FindAll("[data-part=column]");
        Assert.Equal(["To do", "Doing", "Done"], cut.Tb().Kanban("Trail work").Columns);
        Assert.Equal(("group", "To do", "2"), (columns[0].GetAttribute("role"),
            cut.Find($"#{columns[0].GetAttribute("aria-labelledby")}").TextContent,
            cut.Find($"#{columns[0].GetAttribute("aria-describedby")}").TextContent));
        Assert.Equal(["Clear the ridge path", "Mark the river ford"], cut.Tb().Kanban().Cards("To do"));
    }

    [Fact]
    public async Task MoveToColumn_ShouldRaiseOnMoveBetweenTheColumns_WhenACardMovesToAnother()
    {
        var cut = RenderBoard();

        await cut.Tb().Kanban().MoveToColumnAsync("Mark the river ford", "Doing");

        Assert.Equal(new SortableMove<string>("Mark the river ford", 1, 1) { FromList = "todo", ToList = "doing" },
            _moves.Single());
        Assert.Equal(["Fix the footbridge", "Mark the river ford"], cut.Tb().Kanban().Cards("Doing"));
        Assert.Equal("1", cut.FindAll("[data-part=count]")[0].TextContent);
    }

    [Fact]
    public async Task Column_ShouldReorderItsCards_WhenACardMovesWithinIt()
    {
        var cut = RenderBoard();

        await cut.Tb().Kanban().Column("To do").MoveAsync("Mark the river ford", 0);

        Assert.Equal(["Mark the river ford", "Clear the ridge path"], cut.Tb().Kanban().Cards("To do"));
    }

    [Fact]
    public async Task MoveToColumn_ShouldBeRefusedAndReadOut_WhenTheColumnIsFull()
    {
        _cards["done"].Add("Paint the trail blazes");
        _doneLimit = 1;
        var cut = RenderBoard();

        await cut.Tb().Kanban().MoveToColumnAsync("Fix the footbridge", "Done");

        Assert.Empty(_moves);
        Assert.Equal("Done is full: its limit is 1.", Announcer.Polite[^1]);
        var done = cut.FindAll("[data-part=column]")[2];
        Assert.Equal(("1 of 1", true), (done.QuerySelector("[data-part=count]")!.TextContent,
            done.HasAttribute("data-full")));
    }

    [Fact]
    public async Task Column_ShouldStillReorderItsCards_WhenTheColumnIsFull()
    {
        _cards["done"].AddRange(["Paint the trail blazes", "Mow the meadow loop"]);
        _doneLimit = 2;
        var cut = RenderBoard();

        await cut.Tb().Kanban().Column("Done").MoveAsync("Mow the meadow loop", 0);

        Assert.Equal(["Mow the meadow loop", "Paint the trail blazes"], cut.Tb().Kanban().Cards("Done"));
    }

    [Fact]
    public void Render_ShouldNameEachColumn_WhenItsIdHasSpaces()
    {
        var cut = Render<TbKanban<string>>(parameters => parameters.Add(board => board.AriaLabel, "Trail work")
            .Add(board => board.Columns, [new KanbanColumn<string>("in progress", "In progress", ["Fix the bridge"])])
            .Add(board => board.CardTemplate, card => builder => builder.AddContent(0, card)));

        var column = cut.Find("[data-part=column]");
        Assert.Equal(("In progress", "1"), (cut.Find($"#{column.GetAttribute("aria-labelledby")}").TextContent,
            cut.Find($"#{column.GetAttribute("aria-describedby")}").TextContent));
    }

    [Fact]
    public async Task Moves_ShouldBeCounted_WhenTheBoardHasATelemetryName()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderBoard(parameters => parameters.Add(board => board.TelemetryName, "trail-work"));

        await cut.Tb().Kanban().MoveToColumnAsync("Fix the footbridge", "Done");

        Assert.Equal(["move"], used.GetMeasurementSnapshot().Select(measurement =>
            measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task CanMove_ShouldBeAsked_WhenTheColumnHasRoom()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.CanMove,
            (SortableMove<string> move) => Task.FromResult(move.ToList == "done"
                ? MoveCheck.Refuse("Only a reviewer moves cards to Done.") : MoveCheck.Allow)));

        await cut.Tb().Kanban().MoveToColumnAsync("Fix the footbridge", "Done");

        Assert.Empty(_moves);
        Assert.Equal("Only a reviewer moves cards to Done.", Announcer.Polite[^1]);
    }

    [Fact]
    public void ColumnFooterTemplate_ShouldShowUnderEachColumn_WhenGiven()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.ColumnFooterTemplate,
            column => builder => builder.AddContent(0, $"Add to {column.Title}")));

        Assert.Equal(["Add to To do", "Add to Doing", "Add to Done"],
            cut.FindAll("[data-part=footer]").Select(footer => footer.TextContent));
    }

    [Fact]
    public async Task MoveToColumn_ShouldThrow_WhenTheBoardIsDisabled()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.Disabled, true));

        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => cut.Tb().Kanban().MoveToColumnAsync("Fix the footbridge", "Done"));

        Assert.Empty(_moves);
    }

    [Fact]
    public async Task MoveToColumn_ShouldOfferOnlyItsOwnBoardsColumns_WhenTwoBoardsShowAtOnce()
    {
        var cut = RenderBoard();
        var other = Render<TbKanban<string>>(parameters => parameters.Add(board => board.AriaLabel, "Camp chores")
            .Add(board => board.Columns, [new KanbanColumn<string>("later", "Later", ["Stack firewood"])])
            .Add(board => board.CardTemplate, card => builder => builder.AddContent(0, card)));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Kanban("Trail work").Column("Doing").MoveToListAsync("Fix the footbridge", "Later"));

        Assert.Contains("no other list in its group has that label", thrown.Message);
        Assert.Equal(["Stack firewood"], other.Tb().Kanban("Camp chores").Cards("Later"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoAriaLabel()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbKanban<string>>(parameters =>
            parameters.Add(board => board.CardTemplate, card => builder => builder.AddContent(0, card))));

        Assert.Equal("TbKanban needs an AriaLabel.", thrown.Message);
    }

    [Fact]
    public async Task Collapse_ShouldHideTheCardsAndKeepTheTitleAndCount_WhenTheColumnsButtonIsPressed()
    {
        IReadOnlyCollection<string>? reported = null;
        var cut = RenderBoard(parameters => parameters.Add(board => board.Collapsible, true)
            .Add(board => board.CollapsedColumnsChanged, (IReadOnlyCollection<string> ids) => reported = ids));
        var board = cut.Tb().Kanban("Trail work");
        var button = cut.Find("[data-key=todo] [data-part=collapse]");
        var before = (button.GetAttribute("aria-label"), button.GetAttribute("aria-expanded"));
        var controlsCards = cut.Find($"[id='{button.GetAttribute("aria-controls")}']").ClassList
            .Contains("tb-kanban__cards");

        await board.CollapseAsync("To do");

        var todo = cut.Find("[data-key=todo]");
        Assert.Equal(("Cards in To do", "true"), before);
        Assert.True(controlsCards);
        Assert.True(board.IsCollapsed("To do"));
        Assert.Null(todo.QuerySelector("[data-part=list]"));
        Assert.Equal("2", todo.QuerySelector("[data-part=count]")!.TextContent);
        var collapse = todo.QuerySelector("[data-part=collapse]")!;
        Assert.Equal(("Cards in To do", "false", null), (collapse.GetAttribute("aria-label"),
            collapse.GetAttribute("aria-expanded"), collapse.GetAttribute("aria-controls")));
        Assert.Empty(board.Cards("To do"));
        Assert.Equal(["todo"], reported);
        await board.ExpandAsync("To do");
        Assert.Equal(["Clear the ridge path", "Mark the river ford"], board.Cards("To do"));
    }

    [Fact]
    public void CollapsedColumns_ShouldCollapseTheColumnsGiven_WhenSet()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.Collapsible, true)
            .Add(board => board.CollapsedColumns, ["done"]));

        Assert.Equal((false, true), (cut.Tb().Kanban("Trail work").IsCollapsed("To do"),
            cut.Tb().Kanban("Trail work").IsCollapsed("Done")));
        Assert.Contains("tb-kanban__column--collapsed", cut.Find("[data-key=done]").ClassList);
    }

    [Fact]
    public async Task CollapsedColumns_ShouldKeepTheUsersToggles_WhenTheParentRendersTheSameOnes()
    {
        IReadOnlyCollection<string> given = ["done"];
        var cut = RenderBoard(parameters => parameters.Add(board => board.Collapsible, true)
            .Add(board => board.CollapsedColumns, given));
        await cut.Tb().Kanban("Trail work").ExpandAsync("Done");

        cut.Render(parameters => parameters.Add(board => board.CollapsedColumns, given));

        Assert.False(cut.Tb().Kanban("Trail work").IsCollapsed("Done"));
    }

    [Fact]
    public async Task Expand_ShouldOfferTheColumnInTheBoardsOrder_WhenItStartedCollapsed()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.Collapsible, true)
            .Add(board => board.CollapsedColumns, ["todo"]));

        await cut.Tb().Kanban("Trail work").ExpandAsync("To do");

        var group = cut.Find("[data-key=doing] [data-part=list]").GetAttribute("data-group")!;
        Assert.Equal(["todo", "doing", "done"],
            Services.GetRequiredService<SortableGroups>().Lists<ISortableGroupMember>(group)
                .Select(list => list.ListId));
    }

    [Fact]
    public async Task MoveToColumn_ShouldNotOfferACollapsedColumn_WhenOneIsCollapsed()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.Collapsible, true)
            .Add(board => board.CollapsedColumns, ["done"]));

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Kanban("Trail work").MoveToColumnAsync("Fix the footbridge", "Done"));
        Assert.Contains("Done", refused.Message);
        Assert.Empty(_moves);
    }

    [Fact]
    public async Task MoveToColumn_ShouldMoveTheCard_WhenAColumnBeforeItsOwnIsCollapsed()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.Collapsible, true)
            .Add(board => board.CollapsedColumns, ["todo"]));

        await cut.Tb().Kanban("Trail work").MoveToColumnAsync("Fix the footbridge", "Done");

        Assert.Equal(("doing", "done"), (_moves.Single().FromList, _moves.Single().ToList));
    }

    [Fact]
    public async Task Collapse_ShouldBeCounted_WhenTheBoardHasATelemetryName()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderBoard(parameters => parameters.Add(board => board.TelemetryName, "trail-work")
            .Add(board => board.Collapsible, true));

        await cut.Tb().Kanban().CollapseAsync("Doing");
        await cut.Tb().Kanban().ExpandAsync("Doing");

        Assert.Equal(["collapse", "expand"], used.GetMeasurementSnapshot().Select(measurement =>
            measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Collapse_ShouldThrow_WhenTheBoardIsNotCollapsible()
    {
        var cut = RenderBoard();

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Kanban("Trail work").CollapseAsync("Done"));
        Assert.Empty(cut.FindAll("[data-part=collapse]"));
    }

    private readonly Dictionary<string, List<string>> _cells = new()
    {
        ["north/todo"] = ["Clear the ridge path"],
        ["north/doing"] = ["Fix the footbridge"],
        ["north/done"] = [],
        ["south/todo"] = ["Mark the river ford"],
        ["south/doing"] = ["Paint the signs"],
        ["south/done"] = [],
    };

    private IRenderedComponent<TbKanban<string>> RenderLanes(int? doingLimit = null) => Render<TbKanban<string>>(
        parameters => parameters.Add(board => board.AriaLabel, "Crew board")
            .Add(board => board.Columns, [new("todo", "To do", []), new("doing", "Doing", []) { Limit = doingLimit },
                new("done", "Done", [])])
            .Add(board => board.Lanes, [Lane("north", "North crew"), Lane("south", "South crew")])
            .Add(board => board.CardTemplate, card => builder => builder.AddContent(0, card))
            .Add(board => board.OnMove, (SortableMove<string> move) =>
            {
                _moves.Add(move);
                move.ApplyBetween(_cells[move.FromList!], _cells[move.ToList!]);
            }));

    private KanbanLane<string> Lane(string id, string title) => new(id, title,
        new Dictionary<string, IReadOnlyList<string>>
    {
        ["todo"] = _cells[$"{id}/todo"],
        ["doing"] = _cells[$"{id}/doing"],
        ["done"] = _cells[$"{id}/done"],
    });

    [Fact]
    public void Lanes_ShouldShowEachLanesCellInEachColumn_WhenGiven()
    {
        var cut = RenderLanes();
        var board = cut.Tb().Kanban("Crew board");

        Assert.Equal(["North crew", "South crew"], board.Lanes);
        Assert.Equal(["Fix the footbridge"], board.Cards("Doing", "North crew"));
        Assert.Throws<InvalidOperationException>(() => board.Cards("Doing"));
        Assert.Equal("2", cut.Find("[data-key=doing] [data-part=count]").TextContent);
        Assert.Equal(["To do, North crew", "To do, South crew"], cut.FindAll("[data-key=todo] [data-part=list]")
            .Select(list => list.GetAttribute("aria-label")));
        Assert.Contains("tb-kanban--lanes", cut.Find("section").ClassList);
    }

    [Fact]
    public void Lanes_ShouldShowTheColumnsAsABoardWithoutLanes_WhenEmpty()
    {
        var cut = RenderBoard(parameters => parameters.Add(board => board.Lanes, []));

        Assert.Equal(["Clear the ridge path", "Mark the river ford"], cut.Tb().Kanban("Trail work").Cards("To do"));
        Assert.DoesNotContain("tb-kanban--lanes", cut.Find("section").ClassList);
    }

    [Fact]
    public void Lanes_ShouldThrow_WhenAnIdHoldsASlash()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbKanban<string>>(parameters => parameters
            .Add(board => board.AriaLabel, "Crew board").Add(board => board.Columns, [new("to/do", "To do", [])])
            .Add(board => board.Lanes, [new KanbanLane<string>("north", "North crew",
                new Dictionary<string, IReadOnlyList<string>>())])
            .Add(board => board.CardTemplate, card => builder => builder.AddContent(0, card))));

        Assert.Contains("to/do", thrown.Message);
    }

    [Fact]
    public async Task MoveToColumn_ShouldMoveTheCardToAnotherLanesCell_WhenTheBoardHasLanes()
    {
        var cut = RenderLanes();

        await cut.Tb().Kanban("Crew board").MoveToColumnAsync("Fix the footbridge", "Done", "South crew");

        Assert.Equal(("north/doing", "south/done"), (_moves.Single().FromList, _moves.Single().ToList));
        Assert.Equal(["Fix the footbridge"], cut.Tb().Kanban("Crew board").Cards("Done", "South crew"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Kanban("Crew board").MoveToColumnAsync("Paint the signs", "Done"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => RenderBoard().Tb().Kanban("Trail work")
                .MoveToColumnAsync("Fix the footbridge", "Done", "North crew"));
    }

    [Fact]
    public async Task Lanes_ShouldRefuseAMoveIntoAFullColumnButNotWithinIt_WhenTheColumnHasALimit()
    {
        var cut = RenderLanes(doingLimit: 2);
        var board = cut.Tb().Kanban("Crew board");

        await board.MoveToColumnAsync("Fix the footbridge", "Doing", "South crew");
        await board.MoveToColumnAsync("Clear the ridge path", "Doing", "North crew");

        Assert.Equal(["north/doing"], _moves.Select(move => move.FromList));
        Assert.Equal(["Paint the signs", "Fix the footbridge"], board.Cards("Doing", "South crew"));
    }

    private IReadOnlyList<KanbanColumn<string>> Columns() =>
    [
        new("todo", "To do", _cards["todo"]),
        new("doing", "Doing", _cards["doing"]),
        new("done", "Done", _cards["done"]) { Limit = _doneLimit },
    ];

    // The columns hold the test's own lists, so a move applied to them shows once the board renders again.
    private IRenderedComponent<TbKanban<string>> RenderBoard(
        Action<ComponentParameterCollectionBuilder<TbKanban<string>>>? configure = null) =>
        Render<TbKanban<string>>(parameters =>
        {
            parameters.Add(board => board.AriaLabel, "Trail work").Add(board => board.Columns, Columns())
                .Add(board => board.CardTemplate, card => builder => builder.AddContent(0, card))
                .Add(board => board.OnMove, (SortableMove<string> move) =>
                {
                    _moves.Add(move);
                    move.ApplyBetween(_cards[move.FromList!], _cards[move.ToList!]);
                });
            configure?.Invoke(parameters);
        });
}
