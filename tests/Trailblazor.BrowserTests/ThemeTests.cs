namespace Trailblazor.BrowserTests;

public class ThemeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemeHead_ShouldStartThePageInTheAppsDefaults_WhenNothingIsRemembered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/theme");

        Assert.Equal(["system", "graphite"], await ThemeAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemePicker_ShouldShowAndRememberTheChosenMode_WhenAnItemIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/theme");
        var dark = page.GetByRole(AriaRole.Button, new() { Name = "Dark" });

        await dark.ClickAsync();

        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(dark).ToHaveAttributeAsync("aria-pressed", "true");
        // The head script puts the choice back before the page paints, so it holds before the page is interactive.
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.Equal("dark", (await ThemeAsync(page))[0]);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Dark" }))
            .ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemeService_ShouldShowAndRememberThePalette_WhenOneIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/theme");

        await page.GetByRole(AriaRole.Button, new() { Name = "Use cobalt" }).ClickAsync();

        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.Equal("cobalt", (await ThemeAsync(page))[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemeHead_ShouldKeepTheRememberedMode_WhenAnEnhancedNavigationReplacesThePage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-theme");
        await page.EvaluateAsync("() => localStorage.setItem('trailblazor.tbTheme', 'dark')");
        await page.ReloadAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // A full page load would forget this, so its surviving shows the navigation was enhanced.
        await page.EvaluateAsync("() => window.stayedOnThePage = true");

        await page.GetByRole(AriaRole.Link, new() { Name = "Read-only fields" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "Agree" })).ToBeVisibleAsync();

        Assert.True(await page.EvaluateAsync<bool>("() => window.stayedOnThePage === true"));
        Assert.Equal("dark", (await ThemeAsync(page))[0]);
    }

    private static Task<string[]> ThemeAsync(IPage page) => page.EvaluateAsync<string[]>(
        "() => [document.documentElement.dataset.tbTheme, document.documentElement.dataset.tbPalette]");
}
