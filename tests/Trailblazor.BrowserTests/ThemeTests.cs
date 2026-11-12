namespace Trailblazor.BrowserTests;

public class ThemeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemeHead_ShouldStartThePageInTheAppsDefaults_WhenNothingIsRemembered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/theme");

        Assert.Equal(["system", "graphite", "comfortable"], await ThemeAsync(page));
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
    public async Task ThemeService_ShouldShowAndRememberACustomPalette_WhenOneIsSetByName(string browser)
    {
        var page = await OpenAsync(browser, "/theme");

        await page.GetByRole(AriaRole.Button, new() { Name = "Use forest" }).ClickAsync();

        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-palette", "forest");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.Equal("forest", (await ThemeAsync(page))[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemeService_ShouldShowAndRememberTheDensity_WhenOneIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/theme");

        await page.GetByRole(AriaRole.Button, new() { Name = "Use compact" }).ClickAsync();

        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-density", "compact");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.Equal("compact", (await ThemeAsync(page))[2]);
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

    private static Task<string[]> ThemeAsync(IPage page) => page.EvaluateAsync<string[]>("""
        () => [document.documentElement.dataset.tbTheme, document.documentElement.dataset.tbPalette,
            document.documentElement.dataset.tbDensity]
        """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Theme_ShouldPaintARegionWithItsThemesBackgroundAndText_WhenTheRegionSetsATheme(string browser)
    {
        var page = await OpenAsync(browser, "/theme");

        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const region = document.createElement('section');
                region.dataset.tbTheme = 'dark';
                region.textContent = 'Night trails';
                const probe = document.createElement('span');
                probe.style.color = 'var(--tb-color-background)';
                region.append(probe);
                document.body.append(region);
                const style = getComputedStyle(region);
                return [style.backgroundColor, getComputedStyle(probe).color, style.color];
            }
            """);

        Assert.Equal(colors[1], colors[0]);
        Assert.NotEqual("rgba(0, 0, 0, 0)", colors[0]);
        Assert.NotEqual(colors[0], colors[2]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemedPage_ShouldTakeTheThemesTypeUnlessTheAppSetsItsOwn_WhenTbThemeHeadMarksIt(string browser)
    {
        var page = await OpenAsync(browser, "/theme");
        const string Type = "() => { const html = getComputedStyle(document.documentElement); "
            + "return [html.fontFamily, html.lineHeight, html.fontSize, getComputedStyle(document.body).fontSize]; }";
        const string Token = "() => getComputedStyle(document.documentElement).getPropertyValue('--tb-font-family')";

        var themed = await page.EvaluateAsync<string[]>(Type);
        // WebKit drops the quotes around a font's name.
        Assert.Equal((await page.EvaluateAsync<string>(Token)).Trim().Replace("\"", ""), themed[0].Replace("\"", ""));
        Assert.Equal("24px", themed[1]);
        await page.AddStyleTagAsync(new() { Content = ":root { --tb-font-size-md: 1.25rem; }" });
        // The body takes the size, and rem stays the browser's.
        Assert.Equal(["16px", "20px"], (await page.EvaluateAsync<string[]>(Type))[2..]);
        await page.AddStyleTagAsync(new() { Content = "html { font-family: serif; line-height: 2; }" });

        Assert.Equal(["serif", "32px"], (await page.EvaluateAsync<string[]>(Type))[..2]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GraphiteStrongBorder_ShouldLighten_WhenTheThemeIsDark(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");

        // The strong border and graphite 400, each resolved inside a dark graphite region.
        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const host = document.body.appendChild(document.createElement('div'));
                host.dataset.tbPalette = 'graphite';
                host.dataset.tbTheme = 'dark';
                const color = token => {
                    const probe = host.appendChild(document.createElement('div'));
                    probe.style.color = token;
                    return getComputedStyle(probe).color;
                };
                const colors = [color('var(--tb-border-strong)'), color('var(--tb-graphite-400)')];
                host.remove();
                return colors;
            }
            """);

        Assert.Equal(colors[1], colors[0]);
    }
}
