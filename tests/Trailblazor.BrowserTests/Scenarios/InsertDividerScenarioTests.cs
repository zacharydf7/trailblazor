using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class InsertDividerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checklist_ShouldTakeANewStepWhereItIsInserted_WhenADividersButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider/between-items");

        // A divider is found by its label, and InsertAsync presses its insert button.
        await page.Tb().InsertDivider("Add a step after Pack the tent").InsertAsync();

        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Trip checklist" }).GetByRole(AriaRole.Listitem))
            .ToHaveTextAsync(["Pack the tent", "New step 1", "Fill the water bottles", "Check the trail map"]);
    }
}
