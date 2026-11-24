using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TransferListScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Crew_ShouldGiveTheAppWhoIsOnTheTrip_WhenHikersAreMovedAcrossAndBack(string browser)
    {
        var page = await OpenAsync(browser, "/examples/transfer-list/move-items");
        var crew = page.Tb().TransferList("Trip crew");
        var onTheTrip = page.GetByLabel("Crew", new() { Exact = true });
        Assert.Equal(["Blake", "Emery"], await crew.ChosenAsync());

        // MoveAsync marks the hikers in the available list and presses the button that moves them across. The app gets
        // them in the order of Items, not the order they were moved in.
        await crew.MoveAsync("Casey", "Avery");
        Assert.Equal(["Avery", "Blake", "Casey", "Emery"], await crew.ChosenAsync());
        await Expect(onTheTrip).ToHaveTextAsync("Avery, Blake, Casey, Emery");

        await crew.MoveBackAsync("Blake");
        Assert.Equal(["Avery", "Casey", "Emery"], await crew.ChosenAsync());
        await Expect(onTheTrip).ToHaveTextAsync("Avery, Casey, Emery");
    }
}
