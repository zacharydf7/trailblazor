using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TimelineScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TripHistory_ShouldListItsEventsInOrder_WhenRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/timeline/basic");
        var history = page.GetByRole(AriaRole.List, new() { Name = "Trip history" });

        // A timeline has no helper of its own; it is a list, and each event's title is its item's title part.
        await Expect(history.Locator("[data-part=title]"))
            .ToHaveTextAsync(["Trip booked", "Gear rented", "Trail closed for snow, so the date moved"]);
        // The time element holds the moment in a form that doesn't depend on the culture.
        await Expect(history.Locator("time").First).ToHaveAttributeAsync("datetime", "2026-03-03T09:30:00.000+00:00");
    }
}
