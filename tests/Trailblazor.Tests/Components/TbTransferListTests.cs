using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbTransferListTests : BunitContext
{
    public TbTransferListTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hiker(int Id, string Name);

    private static readonly Hiker[] Hikers = [new(1, "Avery"), new(2, "Blake"), new(3, "Casey"), new(4, "Devon")];

    private List<IReadOnlyList<Hiker>> Changes { get; } = [];

    private IRenderedComponent<TbTransferList<Hiker>> RenderCrew(IReadOnlyList<Hiker> chosen,
        Action<ComponentParameterCollectionBuilder<TbTransferList<Hiker>>>? configure = null) =>
        Render<TbTransferList<Hiker>>(parameters =>
        {
            parameters.Add(list => list.Label, "Crew").Add(list => list.Items, Hikers)
                .Add(list => list.ItemText, hiker => hiker.Name).Add(list => list.Value, chosen)
                .Add(list => list.ValueChanged, (IReadOnlyList<Hiker> next) => Changes.Add(next));
            configure?.Invoke(parameters);
        });

    private static IReadOnlyList<string> Names(IEnumerable<Hiker> hikers) => [.. hikers.Select(hiker => hiker.Name)];

    [Fact]
    public void Render_ShouldListTheItemsNotChosenAndTheChosenOnes_WhenGivenAValue()
    {
        var cut = RenderCrew([Hikers[2]]);

        Assert.Equal("Crew", cut.Find("legend[data-part=label]").TextContent.Trim());
        Assert.Equal(["Avery", "Blake", "Devon"], cut.Tb().ListBox("Available").Options);
        Assert.Equal(["Casey"], cut.Tb().TransferList("Crew").Chosen);
        Assert.Equal("true", cut.Find("[data-part=move-chosen]").GetAttribute("aria-disabled"));
        Assert.Null(cut.Find("[data-part=move-all-chosen]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task MoveAsync_ShouldAskTheLocalizerForTheCountedWords_WhenItemsMove()
    {
        Services.AddScoped<ITbLocalizer, PluralRecorder>();
        var host = Render<AnnouncerHost>();
        var cut = RenderCrew([Hikers[2]]);

        await cut.Tb().TransferList("Crew").MoveAsync("Devon", "Avery");

        host.WaitForAssertion(() => Assert.Equal(["TransferList.Moved for 2"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public async Task MoveAsync_ShouldMoveTheMarkedItemsKeepingTheItemsOrderAndSaySo_WhenCalled()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderCrew([Hikers[2]]);

        await cut.Tb().TransferList("Crew").MoveAsync("Devon", "Avery");

        Assert.Equal(["Avery", "Casey", "Devon"], Names(Changes.Single()));
        host.WaitForAssertion(() => Assert.Equal(["Moved 2 to Chosen"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public async Task Labels_ShouldWordTheTitlesFiltersMovesAndAnnouncement_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderCrew([Hikers[2]], parameters => parameters.Add(list => list.Filterable, true)
            .Add(list => list.Labels, new TransferListLabels
            {
                Available = "Free", Chosen = "Going", Empty = "No one", Filter = "Find in {0}", Move = "Send to {0}",
                MoveAll = "Send all to {0}", Moved = "{0} sent to {1}",
            }));

        Assert.Equal(["Avery", "Blake", "Devon"], cut.Tb().ListBox("Free").Options);
        Assert.Equal(["Casey"], cut.Tb().ListBox("Going").Options);
        var labels = cut.FindAll("label").Select(label => label.TextContent.Trim()).ToList();
        Assert.Contains("Find in Free", labels);
        Assert.Contains("Find in Going", labels);
        Assert.Equal(["Send to Going", "Send all to Going", "Send to Free", "Send all to Free"],
            cut.FindAll("[data-part=moves] button").Select(button => button.GetAttribute("aria-label")));
        await cut.Tb().TransferList("Crew").MoveAsync("Devon");

        host.WaitForAssertion(() => Assert.Equal(["1 sent to Going"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
        await cut.Tb().TransferList("Crew").MoveBackAsync("Casey", "Devon");
        Assert.Contains("No one", cut.Find("[data-part=chosen]").TextContent);
    }

    [Fact]
    public async Task Option_ShouldMoveToTheOtherList_WhenDoubleClicked()
    {
        var cut = RenderCrew([Hikers[2]]);

        await cut.FindAll("[data-part=available] [role=option]").Single(option => option.TextContent.Trim() == "Blake")
            .DoubleClickAsync(new());
        cut.Render(parameters => parameters.Add(list => list.Value, Changes[^1]));
        await cut.FindAll("[data-part=chosen] [role=option]").Single(option => option.TextContent.Trim() == "Casey")
            .DoubleClickAsync(new());

        Assert.Equal([["Blake", "Casey"], ["Blake"]], Changes.Select(Names));
    }

    [Fact]
    public async Task Option_ShouldStayInItsList_WhenDoubleClickedInAReadOnlyList()
    {
        var cut = RenderCrew([Hikers[2]], parameters => parameters.Add(list => list.ReadOnly, true));

        await cut.Find("[data-part=available] [role=option]").DoubleClickAsync(new());

        Assert.Empty(Changes);
    }

    [Fact]
    public async Task MoveBackAsync_ShouldReturnTheMarkedItems_WhenCalled()
    {
        var cut = RenderCrew([Hikers[0], Hikers[2]]);

        await cut.Tb().TransferList("Crew").MoveBackAsync("Avery");

        Assert.Equal(["Casey"], Names(Changes.Single()));
    }

    [Fact]
    public async Task MoveAll_ShouldMoveOnlyTheItemsTheFilterShows_WhenTheListIsFiltered()
    {
        var cut = RenderCrew([], parameters => parameters.Add(list => list.Filterable, true));

        await cut.Find("input[type=text]").InputAsync(new ChangeEventArgs { Value = "a" });
        await cut.Find("[data-part=move-all-chosen]").ClickAsync(new());

        Assert.Equal(["Avery", "Blake", "Casey"], Names(Changes.Single()));
    }

    [Fact]
    public async Task Filter_ShouldIgnoreAccents_WhenTyped()
    {
        var cut = Render<TbTransferList<string>>(parameters => parameters.Add(list => list.Label, "Crew")
            .Add(list => list.Items, ["José", "Ana"]).Add(list => list.Filterable, true));

        await cut.Find("input[type=text]").InputAsync(new ChangeEventArgs { Value = "jose" });

        Assert.Equal(["José"], cut.Tb().ListBox("Available").Options);
    }

    [Fact]
    public void Value_ShouldMatchItsItems_WhenTheComparerSaysTheyAreTheSame()
    {
        var cut = RenderCrew([new Hiker(2, "Blake (loaded again)")], parameters => parameters.Add(list => list.Comparer,
            EqualityComparer<Hiker>.Create((one, other) => one?.Id == other?.Id, hiker => hiker.Id)));

        Assert.Equal(["Blake"], cut.Tb().TransferList("Crew").Chosen);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Render_ShouldOfferNoMoves_WhenDisabledOrReadOnly(bool disabled, bool readOnly)
    {
        var cut = RenderCrew([Hikers[0]], parameters => parameters.Add(list => list.Disabled, disabled)
            .Add(list => list.ReadOnly, readOnly));

        Assert.All(cut.FindAll("[data-part=moves] button"), button => Assert.True(
            button.HasAttribute("disabled") || button.GetAttribute("aria-disabled") == "true"));
    }

    [Fact]
    public async Task MoveAsync_ShouldKeepAChosenValueThatMatchesNoItem_WhenOtherItemsMove()
    {
        var gone = new Hiker(9, "Morgan");
        var cut = RenderCrew([gone]);

        await cut.Tb().TransferList("Crew").MoveAsync("Avery");

        Assert.Equal(["Avery", "Morgan"], Names(Changes.Single()));
    }

    [Fact]
    public async Task MoveButton_ShouldHaveNothingToMove_WhenTheFilterHidesTheMarkedItem()
    {
        var cut = RenderCrew([], parameters => parameters.Add(list => list.Filterable, true));
        await cut.Tb().ListBox("Available").ChooseAsync("Avery");

        await cut.Find("input[type=text]").InputAsync(new ChangeEventArgs { Value = "lake" });

        Assert.Equal("true", cut.Find("[data-part=move-chosen]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task MoveAllBack_ShouldReturnEveryChosenItem_WhenClicked()
    {
        var cut = RenderCrew([Hikers[0], Hikers[3]]);

        await cut.Find("[data-part=move-all-available]").ClickAsync(new());

        Assert.Empty(Changes.Single());
    }
}
