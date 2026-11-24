using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SplitterScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailsPane_ShouldGiveTheAppItsSize_WhenResizedCollapsedAndRestored(string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter/panes");
        // A separator is named after the pane it resizes.
        var trails = page.Tb().Splitter("Resize Trails");
        Assert.Equal(25, await trails.SizeAsync());

        // ResizeAsync moves the separator as its keys do, to a percent of the splitter.
        await trails.ResizeAsync(35);
        await Expect(page.GetByText("Trails 35%, details 25%")).ToBeVisibleAsync();

        await trails.CollapseAsync();
        Assert.True(await trails.CollapsedAsync());
        await Expect(page.GetByText("Trails hidden, details 25%")).ToBeVisibleAsync();

        // The pane comes back at the size it had.
        await trails.RestoreAsync();
        await Expect(page.GetByText("Trails 35%, details 25%")).ToBeVisibleAsync();
    }
}
