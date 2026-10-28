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
}
