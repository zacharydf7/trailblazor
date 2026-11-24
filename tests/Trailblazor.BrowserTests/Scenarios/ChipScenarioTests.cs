using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChipScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tags_ShouldLoseATag_WhenItsChipIsRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip/removable");
        var tags = page.Tb().ChipGroup("Selected tags");
        Assert.Equal(["Invoices", "Archive", "2026"], await tags.ChipsAsync());

        // RemoveAsync presses the chip's remove button and waits for the app's handler.
        await tags.Chip("Archive").RemoveAsync();

        Assert.Equal(["Invoices", "2026"], await tags.ChipsAsync());
    }
}
