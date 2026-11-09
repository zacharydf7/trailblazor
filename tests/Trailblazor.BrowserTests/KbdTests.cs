namespace Trailblazor.BrowserTests;

public class KbdTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kbd_ShouldDrawEachKeyAsAKeycapButNotThePlus_WhenGivenACombination(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kbd");
        var combination = page.Locator(".tb-kbd").First;

        var borders = await combination.Locator("[data-part=key], .tb-kbd__plus")
            .EvaluateAllAsync<string[]>("parts => parts.map(part => getComputedStyle(part).borderBottomStyle)");

        Assert.Equal(["solid", "none", "solid"], borders);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kbd_ShouldShowASingleKeyInTheSameFontAsACombinationsKeys_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kbd");

        var fonts = await page.Locator(".tb-kbd--key, [data-part=key]").EvaluateAllAsync<string[]>(
            "keys => keys.map(key => { const style = getComputedStyle(key); "
            + "return `${style.fontFamily} ${style.fontSize} ${style.lineHeight}`; })");

        Assert.Single(fonts.Distinct());
        Assert.Contains("mono", fonts[0], StringComparison.OrdinalIgnoreCase);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kbd_ShouldKeepItsOwnKeycaps_WhenInsideATooltip(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kbd");
        var combination = page.Locator(".tb-kbd").First;

        // A tooltip styles a plain kbd itself, so the combination is moved into a tooltip's bubble to compare.
        var borders = await combination.EvaluateAsync<string[]>("""
            kbd => {
                const bubble = document.createElement('span');
                bubble.className = 'tb-tooltip';
                document.body.append(bubble);
                bubble.append(kbd);
                const key = kbd.querySelector('[data-part=key]');
                return [getComputedStyle(kbd).borderBottomStyle, getComputedStyle(key).borderBottomWidth];
            }
            """);

        Assert.Equal(["none", "2px"], borders);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kbd_ShouldKeepItsKeycapEdges_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kbd", new() { ForcedColors = ForcedColors.Active });

        var width = await page.Locator(".tb-kbd--key").First
            .EvaluateAsync<string>("key => getComputedStyle(key).borderBottomWidth");

        Assert.Equal("2px", width);
    }
}
