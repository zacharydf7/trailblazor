namespace Trailblazor.BrowserTests;

public class SeededPaletteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    public static TheoryData<string, string> BrowsersAndModes => new(
        from browser in AllBrowsers.Names from mode in new[] { "light", "dark" } select (browser, mode));

    [Theory, MemberData(nameof(BrowsersAndModes))]
    public async Task Palette_ShouldDrawComponentsInTheSeededColors_WhenInEachMode(string browser, string mode)
    {
        var page = await OpenAsync(browser, "/seeded-palette");
        await page.EvaluateAsync("() => document.documentElement.dataset.tbPalette = 'brand'");
        await SetThemeAsync(page, mode);
        var brand = page.GetByTestId("brand");
        var save = brand.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await Expect(save).ToHaveCSSAsync("background-color", await SwatchAsync(page, "--tb-color-primary", mode));
        await Expect(save).ToHaveCSSAsync("color", await SwatchAsync(page, "--tb-color-on-primary", mode));
        var body = page.Locator("body");
        await Expect(body).ToHaveCSSAsync("background-color", await SwatchAsync(page, "--tb-color-background", mode));
        await Expect(body).ToHaveCSSAsync("color", await SwatchAsync(page, "--tb-color-text", mode));
        var surface = page.GetByTestId("surface");
        await Expect(surface).ToHaveCSSAsync("background-color", await SwatchAsync(page, "--tb-color-surface", mode));
        await Expect(surface).ToHaveCSSAsync("color", await SwatchAsync(page, "--tb-color-text", mode));
        await Expect(brand.GetByRole(AriaRole.Button, new() { Name = "Cancel" }))
            .ToHaveCSSAsync("color", await SwatchAsync(page, "--tb-color-neutral-text", mode));
    }

    // The seeded token's color as this browser computes it.
    private static Task<string> SwatchAsync(IPage page, string token, string mode) =>
        page.GetByTestId($"{token}-{mode}").EvaluateAsync<string>("swatch => getComputedStyle(swatch).color");
}
