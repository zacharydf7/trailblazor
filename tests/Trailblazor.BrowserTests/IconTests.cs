namespace Trailblazor.BrowserTests;

public class IconTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Icon_ShouldTakeItsTonesTextColor_WhenGivenATone(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon");

        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.createElement('span');
                probe.style.color = 'var(--tb-color-danger-text)';
                document.body.append(probe);
                const icon = document.querySelector('[aria-label="Failed"]');
                return [getComputedStyle(icon).color, getComputedStyle(probe).color,
                    getComputedStyle(icon.parentElement).color];
            }
            """);

        Assert.Equal(colors[1], colors[0]);
        Assert.NotEqual(colors[2], colors[0]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Icon_ShouldTurnAroundOnlyRightToLeft_WhenItMirrorsInRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon");

        var transforms = await page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('.tb-icon--mirrored')].map(icon => getComputedStyle(icon).transform)");

        Assert.Equal(["none", "matrix(-1, 0, 0, 1, 0, 0)"], transforms);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MaterialSymbol_ShouldDrawItsShape_WhenGivenToTbIcon(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon");
        var hiking = page.GetByRole(AriaRole.Img, new() { Name = "Hiking" });

        var path = (await hiking.Locator("path").BoundingBoxAsync())!;
        var icon = (await hiking.BoundingBoxAsync())!;

        Assert.True(path.Width > icon.Width / 2 && path.Height > icon.Height / 2,
            $"The path is {path.Width}x{path.Height} in a {icon.Width}x{icon.Height} icon.");
        Assert.True(path.X >= icon.X - 1 && path.Y >= icon.Y - 1 && path.X + path.Width <= icon.X + icon.Width + 1
            && path.Y + path.Height <= icon.Y + icon.Height + 1, "The path is drawn outside the icon.");
    }
}
