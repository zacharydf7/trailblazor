namespace Trailblazor.BrowserTests;

public class SpacingTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Classes_ShouldSpaceOnTheTokenScale_WhenApplied(string browser)
    {
        var page = await OpenAsync(browser, "/spacing");

        await Expect(page.GetByTestId("padded")).ToHaveCSSAsync("padding-left", "16px");
        await Expect(page.GetByTestId("padded")).ToHaveCSSAsync("margin-top", "8px");
        await Expect(page.GetByTestId("row")).ToHaveCSSAsync("column-gap", "24px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StartMargin_ShouldBeOnTheRight_WhenThePageRunsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/spacing");

        await Expect(page.GetByTestId("start-margin")).ToHaveCSSAsync("margin-right", "8px");
        await Expect(page.GetByTestId("start-margin")).ToHaveCSSAsync("margin-left", "0px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Classes_ShouldBeatAComponentsOwnSpacing_WhenPutOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/spacing");

        await Expect(page.GetByTestId("tight")).ToHaveCSSAsync("padding-top", "0px");
    }
}
