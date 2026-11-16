using Trailblazor.Docs.Recipes;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Runs each Playwright cell of the Plain bUnit and Playwright guide, the plain way and with the helpers, and checks
// both leave TripPlanner the same way.
public class TestingComparisonTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    public static TheoryData<string, bool> BothWays()
    {
        var data = new TheoryData<string, bool>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, false);
            data.Add(browser, true);
        }

        return data;
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task ClickAButton_ShouldAddAStop_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.ClickAButton(page) : PlainPlaywright.ClickAButton(page));

        // No wait: each way has already waited for the app's handler.
        Assert.Equal("1", await page.GetByLabel("Stops", new() { Exact = true }).TextContentAsync());
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task TypeAndLeave_ShouldRenameTheTrip_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.TypeAndLeave(page) : PlainPlaywright.TypeAndLeave(page));

        Assert.StartsWith("Lake loop,", await page.GetByLabel("Trip", new() { Exact = true }).TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadAnError_ShouldFindTheRequiredMessage_WhenRunEitherWay(string browser)
    {
        var plain = await PlainPlaywright.ReadAnError(await OpenStaticAsync(browser, "/trip-planner"));
        var helped = await PlaywrightHelpers.ReadAnError(await OpenStaticAsync(browser, "/trip-planner"));

        Assert.Equal((true, "Trip name is required."), helped);
        Assert.Contains(helped.Error!, plain);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task ChooseAnOption_ShouldSetTheSeason_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.ChooseAnOption(page) : PlainPlaywright.ChooseAnOption(page));

        Assert.Contains("Summer", await page.GetByLabel("Trip", new() { Exact = true }).TextContentAsync());
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task AnswerADialog_ShouldDeleteTheTrip_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.AnswerADialog(page) : PlainPlaywright.AnswerADialog(page));

        Assert.Equal("yes", await page.GetByLabel("Deleted", new() { Exact = true }).TextContentAsync());
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task SelectATreeRow_ShouldSelectRidgeLoop_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.SelectATreeRow(page) : PlainPlaywright.SelectATreeRow(page));

        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Ridge loop", Exact = true }))
            .ToHaveAttributeAsync("aria-selected", "true", new() { Timeout = 1 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SelectAGridRow_ShouldSelectLakePathAndReadEveryRow_WhenRunEitherWay(string browser)
    {
        var plainPage = await OpenStaticAsync(browser, "/trip-planner");
        var helpedPage = await OpenStaticAsync(browser, "/trip-planner");

        var plain = await PlainPlaywright.SelectAGridRow(plainPage);
        var helped = await PlaywrightHelpers.SelectAGridRow(helpedPage);

        Assert.Equal(["Ridge loop", "Lake path", "Fern gully"], plain);
        Assert.Equal(plain, helped.Select(row => row[0]));
        Assert.Equal([["Lake path", "5"]], await helpedPage.Tb().Grid("Hikes").SelectedAsync());
        Assert.Equal([["Lake path", "5"]], await plainPage.Tb().Grid("Hikes").SelectedAsync());
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task ReadATabPanel_ShouldReadTheRoutePanel_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        var text = await (helpers ? PlaywrightHelpers.ReadATabPanel(page) : PlainPlaywright.ReadATabPanel(page));

        Assert.Equal("North along the ridge, back by the lake.", text);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task UploadAFile_ShouldGiveTheTripItsMap_WhenRunEitherWay(string browser, bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.UploadAFile(page) : PlainPlaywright.UploadAFile(page));

        Assert.EndsWith("route.gpx", await page.GetByLabel("Trip", new() { Exact = true }).TextContentAsync());
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task WaitUntilInteractive_ShouldReturnOnceThePageIsInteractive_WhenRunEitherWay(string browser,
        bool helpers)
    {
        var page = await OpenStaticAsync(browser, "/trip-planner");

        await (helpers ? PlaywrightHelpers.WaitUntilInteractive(page) : PlainPlaywright.WaitUntilInteractive(page));

        // A click now reaches the app, so the stop count moves without a retry.
        await page.GetByRole(AriaRole.Button, new() { Name = "Add a stop", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Stops", new() { Exact = true })).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadAControlWithNoHelper_ShouldFindTheProgressByRole_WhenRun(string browser)
    {
        await PlainPlaywright.ReadAControlWithNoHelper(await OpenStaticAsync(browser, "/trip-planner"));
    }
}
