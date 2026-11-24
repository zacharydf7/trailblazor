using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class KanbanScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailWork_ShouldMoveACardAndRefuseOneOverTheLimit_WhenCardsAreMovedToAColumn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/basic");
        var work = page.Tb().Kanban("Trail work");

        // MoveToColumnAsync uses the card's handle menu, as a user who can't drag does.
        await work.MoveToColumnAsync("Clear fallen trees", "Doing");
        Assert.Equal(["Fix the footbridge", "Clear fallen trees"], await work.CardsAsync("Doing"));
        Assert.Equal(["Repaint blazes"], await work.CardsAsync("To do"));

        // Doing takes two cards at most, so a third is refused, and screen readers hear why.
        await work.MoveToColumnAsync("Repaint blazes", "Doing");
        Assert.Equal(["Repaint blazes"], await work.CardsAsync("To do"));
        await page.Tb().Announcements.ExpectAnnouncedAsync("Doing is full: its limit is 2.");
    }
}
