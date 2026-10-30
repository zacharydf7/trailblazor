namespace Trailblazor.BrowserTests;

public class TbRootTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Fact]
    public async Task TbRoot_ShouldPutStylesheetInHead_WhenPagePrerenders()
    {
        using var client = new HttpClient { BaseAddress = Fixture.BaseAddress };

        var html = await client.GetStringAsync("/tokens", TestContext.Current.CancellationToken);

        var head = html[..html.IndexOf("</head>", StringComparison.Ordinal)];
        Assert.Matches("""<link rel="stylesheet" href="_content/Trailblazor/trailblazor\.[a-z0-9]+\.css" """, head);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TbRoot_ShouldShowWhatThePageOpens_WhenOnlyThePageIsInteractive(string browser)
    {
        var page = await OpenStaticAsync(browser, "/per-page");
        // Only the buttons are interactive, so the layout's main never gets the attribute.
        await page.Locator("main [data-interactive]").WaitForAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Toast" }).ClickAsync();
        await Expect(page.Locator("[data-part=toast]")).ToContainTextAsync("Saved");
        await page.GetByRole(AriaRole.Button, new() { Name = "Announce" }).ClickAsync();
        await Expect(page.Locator("[data-part=announcer] [role=alert]")).ToHaveTextAsync("Connection lost");
        await page.GetByRole(AriaRole.Button, new() { Name = "Dialog" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Toasting" })).ToBeVisibleAsync();
    }
}
