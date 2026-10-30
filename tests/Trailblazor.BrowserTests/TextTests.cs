namespace Trailblazor.BrowserTests;

public class TextTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The lines the text shows, whether it holds more than it shows, and how it clips the rest.
    private static Task<string[]> FitAsync(IPage page, string testId) =>
        page.GetByTestId(testId).EvaluateAsync<string[]>("""
            text => {
                const style = getComputedStyle(text);
                return [String(Math.round(text.clientHeight / parseFloat(style.lineHeight))),
                    String(text.scrollWidth > text.clientWidth || text.scrollHeight > text.clientHeight),
                    style.overflow, style.textOverflow];
            }
            """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldShowOnlyItsLinesWithAnEllipsis_WhenGivenMaxLines(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text");

        Assert.Equal(["1", "true", "hidden", "ellipsis"], await FitAsync(page, "truncated"));
        Assert.Equal(["1", "true", "hidden", "ellipsis"], await FitAsync(page, "truncated-span"));
        Assert.Equal(["2", "true", "hidden"], (await FitAsync(page, "clamped"))[..3]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldUseTheMutedColor_WhenMuted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text");

        var colors = await page.GetByTestId("muted").EvaluateAsync<string[]>("""
            text => {
                const probe = text.parentElement.appendChild(document.createElement('span'));
                probe.style.color = 'var(--tb-color-text-muted)';
                return [getComputedStyle(text).color, getComputedStyle(probe).color, getComputedStyle(document.body).color];
            }
            """);

        Assert.Equal(colors[1], colors[0]);
        Assert.NotEqual(colors[2], colors[0]);
    }
}
