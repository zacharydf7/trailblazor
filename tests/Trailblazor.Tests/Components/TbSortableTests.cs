using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbSortableTests : BunitContext
{
    private readonly List<string> _rules = ["Block spam", "Flag invoices", "Archive the rest"];
    private readonly List<SortableMove<string>> _moves = [];

    public TbSortableTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldNameWhatIsMissing_WhenTheItemTemplateIsLeftOut()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbSortable<string>>(parameters =>
            parameters.Add(list => list.Items, _rules).Add(list => list.Label, "Rules")));

        Assert.Equal("TbSortable needs an ItemTemplate.", error.Message);
    }

    [Fact]
    public void Render_ShouldNameTheListAndEachHandle_WhenGivenItems()
    {
        var cut = RenderRules();

        Assert.Equal("Rules", cut.Find("ul").GetAttribute("aria-label"));
        Assert.Equal(["Move Block spam", "Move Flag invoices", "Move Archive the rest"],
            Handles(cut).Select(handle => handle.GetAttribute("aria-label")));
        var hint = cut.Find($"#{Handles(cut)[0].GetAttribute("aria-describedby")}");
        Assert.Contains("Arrow keys", hint.TextContent);
    }

    [Fact]
    public async Task Keys_ShouldMoveTheItemAndSaySo_WhenPressedOnItsHandle()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderRules();

        await cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));
        await cut.InvokeAsync(() => Events().OnKey("Block spam", "End"));
        await cut.InvokeAsync(() => Events().OnKey("Flag invoices", "ArrowUp"));
        await cut.InvokeAsync(() => Events().OnKey("Archive the rest", "Home"));

        Assert.Equal([new("Block spam", 0, 1), new("Block spam", 1, 2), new("Archive the rest", 1, 0)], _moves);
        Assert.Equal(["Archive the rest", "Flag invoices", "Block spam"], Texts(cut));
        announcer.WaitForAssertion(() => Assert.Equal("Archive the rest moved to position 1 of 3.",
            announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task Drop_ShouldAskFirstAndSayWhy_WhenTheMoveIsRefused()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderRules(parameters => parameters.Add(list => list.CanMove, move =>
            Task.FromResult(move.Item == "Archive the rest" ? MoveCheck.Refuse("The catch-all rule stays last.")
                : MoveCheck.Allow)));

        var check = await cut.InvokeAsync(() => Events().OnDragOver("Archive the rest", "Block spam", before: true));
        await cut.InvokeAsync(() => Events().OnDrop("Archive the rest", "Block spam", before: true));
        await cut.InvokeAsync(() => Events().OnDrop("Block spam", "Flag invoices", before: false));

        Assert.False(check.Allowed);
        Assert.Equal([new("Block spam", 0, 1)], _moves);
        announcer.WaitForAssertion(() => Assert.Contains("The catch-all rule stays last.",
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public async Task Moves_ShouldBeIgnored_WhenDisabled()
    {
        var cut = RenderRules(parameters => parameters.Add(list => list.Disabled, true));

        await cut.InvokeAsync(() => Events().OnDrop("Block spam", "Flag invoices", before: false));
        await cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));

        Assert.Empty(_moves);
        Assert.All(Handles(cut), handle => Assert.True(handle.HasAttribute("disabled")));
    }

    [Fact]
    public async Task Keys_ShouldMoveTheItemTheyWerePressedOn_WhenTheyArriveBeforeTheLastMoveShows()
    {
        var cut = RenderRules();

        await cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));
        await cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));

        Assert.Equal(["Flag invoices", "Archive the rest", "Block spam"], Texts(cut));
    }

    [Fact]
    public async Task Keys_ShouldAskAndMoveOnce_WhenPressedAgainWhileCanMoveRuns()
    {
        var asks = 0;
        var held = new TaskCompletionSource<MoveCheck>();
        var cut = RenderRules(parameters => parameters.Add(list => list.CanMove, _ =>
        {
            asks++;
            return held.Task;
        }));

        var first = cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));
        var second = cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));
        await cut.InvokeAsync(() => held.SetResult(MoveCheck.Allow));
        await Task.WhenAll(first, second);

        Assert.Equal(1, asks);
        Assert.Equal([new("Block spam", 0, 1)], _moves);
    }

    [Fact]
    public async Task Keys_ShouldLeaveTheList_WhenItChangesWhileCanMoveRuns()
    {
        var held = new TaskCompletionSource<MoveCheck>();
        var cut = RenderRules(parameters => parameters.Add(list => list.CanMove, _ => held.Task));

        var move = cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));
        _rules.Insert(0, "Skip drafts");
        await cut.InvokeAsync(() => held.SetResult(MoveCheck.Allow));
        await move;

        Assert.Empty(_moves);
    }

    [Fact]
    public async Task Keys_ShouldLeaveTheList_WhenItsEndIsRemovedWhileCanMoveRuns()
    {
        var held = new TaskCompletionSource<MoveCheck>();
        var cut = RenderRules(parameters => parameters.Add(list => list.CanMove, _ => held.Task));

        var move = cut.InvokeAsync(() => Events().OnKey("Block spam", "End"));
        _rules.RemoveAt(2);
        await cut.InvokeAsync(() => held.SetResult(MoveCheck.Allow));
        await move;

        Assert.Empty(_moves);
    }

    [Fact]
    public async Task Keys_ShouldLeaveTheList_WhenTheParentReplacesItWhileCanMoveRuns()
    {
        var held = new TaskCompletionSource<MoveCheck>();
        var cut = RenderRules(parameters => parameters.Add(list => list.CanMove, _ => held.Task));

        var move = cut.InvokeAsync(() => Events().OnKey("Block spam", "ArrowDown"));
        cut.Render(parameters =>
            parameters.Add(list => list.Items, ["Archive the rest", "Block spam", "Flag invoices"]));
        await cut.InvokeAsync(() => held.SetResult(MoveCheck.Allow));
        await move;

        Assert.Empty(_moves);
    }

    [Fact]
    public async Task Menu_ShouldMoveTheItemOrOfferNothing_WhenAMoveIsChosenFromItsHandle()
    {
        var cut = RenderRules();
        var choices = cut.FindAll("[data-part=handle]")[0].QuerySelectorAll("[role=menuitem]");

        Assert.Equal(["Move up", "Move down", "Move to the start", "Move to the end"],
            choices.Select(choice => choice.TextContent.Trim()));
        Assert.Equal("true", choices[0].GetAttribute("aria-disabled"));
        await choices[3].ClickAsync(new());

        Assert.Equal(["Flag invoices", "Archive the rest", "Block spam"], Texts(cut));
    }

    [Fact]
    public async Task Labels_ShouldWordTheHandlesTheirMenuAndTheMove_WhenGiven()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderRules(parameters => parameters.Add(list => list.Labels, new SortableLabels
        {
            Handle = "Drag {0}", Hint = "Use the arrows.", MoveUp = "Up", MoveDown = "Down", MoveToStart = "First",
            MoveToEnd = "Last", Moved = "{0} is now {1} of {2}.",
        }));

        Assert.Equal("Drag Block spam", Handles(cut)[0].GetAttribute("aria-label"));
        Assert.Equal("Use the arrows.",
            cut.Find($"#{Handles(cut)[0].GetAttribute("aria-describedby")}").TextContent.Trim());
        Assert.Equal(["Up", "Down", "First", "Last"], cut.FindAll("[data-part=handle]")[0]
            .QuerySelectorAll("[role=menuitem]").Select(choice => choice.TextContent.Trim()));
        await cut.InvokeAsync(() => Events().OnKey("Block spam", "End"));
        announcer.WaitForAssertion(() => Assert.Equal("Block spam is now 3 of 3.",
            announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task MoveAsync_ShouldMoveTheItemAsTheUserWould_WhenAnAppsButtonAsks()
    {
        var cut = RenderRules();

        var moved = await cut.InvokeAsync(() => cut.Instance.MoveAsync(2, 0));
        var stayed = await cut.InvokeAsync(() => cut.Instance.MoveAsync(0, 5));

        Assert.True(moved);
        Assert.False(stayed);
        Assert.Equal(["Archive the rest", "Block spam", "Flag invoices"], Texts(cut));
    }

    [Fact]
    public async Task Moves_ShouldBeCountedWithTheirInput_WhenTheListHasATelemetryName()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderRules(parameters => parameters.Add(list => list.TelemetryName, "rule-order"));

        await cut.InvokeAsync(() => Events().OnDrop("Block spam", "Flag invoices", before: false));
        await cut.InvokeAsync(() => Events().OnKey("Flag invoices", "ArrowDown"));
        await cut.InvokeAsync(() => cut.Instance.MoveAsync(0, 1));

        Assert.Equal(["pointer", "keyboard"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.input"]));
    }

    [Fact]
    public void ApplyTo_ShouldPutTheItemAtItsNewIndex_WhenCalled()
    {
        List<string> items = ["a", "b", "c", "d"];

        new SortableMove<string>("a", 0, 2).ApplyTo(items);
        new SortableMove<string>("d", 3, 0).ApplyTo(items);

        Assert.Equal(["d", "b", "c", "a"], items);
    }

    private IRenderedComponent<TbSortable<string>> RenderRules(
        Action<ComponentParameterCollectionBuilder<TbSortable<string>>>? configure = null) =>
        Render<TbSortable<string>>(parameters =>
        {
            parameters.Add(list => list.Items, _rules).Add(list => list.Label, "Rules")
                .Add(list => list.ItemTemplate, rule => builder => builder.AddContent(0, rule))
                .Add(list => list.OnMove, (SortableMove<string> move) =>
                {
                    _moves.Add(move);
                    move.ApplyTo(_rules);
                });
            configure?.Invoke(parameters);
        });

    private SortableEvents Events() =>
        ((DotNetObjectReference<SortableEvents>)JSInterop.Invocations["attachSortable"].Single().Arguments[1]!).Value;

    private static IReadOnlyList<AngleSharp.Dom.IElement> Handles(IRenderedComponent<TbSortable<string>> cut) =>
        cut.FindAll("[data-part=handle] > [data-part=trigger]");

    private static IEnumerable<string> Texts(IRenderedComponent<TbSortable<string>> cut) =>
        cut.FindAll("[data-part=item] .tb-sortable__content").Select(item => item.TextContent.Trim());
}
