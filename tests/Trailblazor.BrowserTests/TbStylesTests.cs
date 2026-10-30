namespace Trailblazor.BrowserTests;

public class TbStylesTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Fact]
    public async Task TbStyles_ShouldPutTheStylesheetInTheHead_WhenThePagePrerenders()
    {
        using var client = new HttpClient { BaseAddress = Fixture.BaseAddress };

        var html = await client.GetStringAsync("/tokens", TestContext.Current.CancellationToken);

        var head = html[..html.IndexOf("</head>", StringComparison.Ordinal)];
        Assert.Matches("""<link rel="stylesheet" href="_content/Trailblazor/trailblazor\.[a-z0-9]+\.css" """, head);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TbStyles_ShouldKeepTheStylesheetLinked_WhenThePageBecomesInteractive(string browser)
    {
        // An empty page first, so the observer is installed before the real page starts to parse.
        var page = await OpenStaticAsync(browser, "about:blank");
        // Records every stylesheet link the page loses.
        await page.AddInitScriptAsync("""
            window.lostLinks = [];
            new MutationObserver(records => records.forEach(record => record.removedNodes.forEach(node => {
                if (node.nodeName === 'LINK') {
                    lostLinks.push(node.getAttribute('href'));
                }
            }))).observe(document, { childList: true, subtree: true });
            """);

        await page.GotoAsync(new Uri(Fixture.BaseAddress, "/tokens").ToString());
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });

        Assert.Empty(await page.EvaluateAsync<string[]>("() => window.lostLinks"));
    }
}
