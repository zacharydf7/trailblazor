using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class KanbanScenarioTests : BunitContext
{
    public KanbanScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailWork_ShouldMoveACardAndRefuseOneOverTheLimit_WhenCardsAreMovedToAColumn()
    {
        var cut = Render<KanbanExample>();
        var work = cut.Tb().Kanban("Trail work");

        // MoveToColumnAsync uses the card's handle menu, as a user who can't drag does.
        await work.MoveToColumnAsync("Clear fallen trees", "Doing");
        Assert.Equal(["Fix the footbridge", "Clear fallen trees"], work.Cards("Doing"));
        Assert.Equal(["Repaint blazes"], work.Cards("To do"));

        // Doing takes two cards at most, so a third is refused, and screen readers hear why.
        await work.MoveToColumnAsync("Repaint blazes", "Doing");
        Assert.Equal(["Repaint blazes"], work.Cards("To do"));
        Assert.Contains("Doing is full: its limit is 2.", Services.GetRequiredService<FakeAnnouncer>().Polite);
    }
}
