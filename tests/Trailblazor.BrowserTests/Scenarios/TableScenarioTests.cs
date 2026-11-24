using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TableScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OpeningHours_ShouldListTheCentersAndTheirHours_WhenRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/table/basic");
        var hours = page.GetByRole(AriaRole.Table, new() { Name = "Opening hours" });

        // A table has no helper of its own; Playwright's roles read it as a screen reader does.
        await Expect(hours.GetByRole(AriaRole.Rowheader))
            .ToHaveTextAsync(["North ridge", "River valley", "High peaks"]);
        await Expect(hours.GetByRole(AriaRole.Row, new() { Name = "High peaks" }).GetByRole(AriaRole.Cell))
            .ToHaveTextAsync(["Closed", "9–15"]);
    }
}
