using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class AppShellTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly BrowserNewContextOptions Narrow = new()
    {
        ViewportSize = new() { Width = 600, Height = 800 }, ReducedMotion = ReducedMotion.Reduce,
    };

    private static ILocator Beside(IPage page) => page.Locator("nav[data-part=navigation]");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AppShell_ShouldMarkTheLinkToThePageItIsOn_WhenALinkIsFollowed(string browser)
    {
        var page = await OpenAsync(browser, "/app-shell");
        var home = Beside(page).GetByRole(AriaRole.Link, new() { Name = "Home" });
        var trips = Beside(page).GetByRole(AriaRole.Link, new() { Name = "Trips" });
        await Expect(home).ToHaveAttributeAsync("aria-current", "page");

        await trips.ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/app-shell/trips$"));
        await Expect(trips).ToHaveAttributeAsync("aria-current", "page");
        await Expect(home).Not.ToHaveAttributeAsync("aria-current", "page");
    }

    // Rendered on the server before the page is interactive, so the group is already open as it arrives.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AppShell_ShouldOpenTheGroupHoldingThePage_WhenThePageLoads(string browser)
    {
        var page = await OpenStaticAsync(browser, "/app-shell/settings/profile");

        await Expect(Beside(page).GetByRole(AriaRole.Button, new() { Name = "Settings" }))
            .ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Beside(page).GetByRole(AriaRole.Link, new() { Name = "Profile" }))
            .ToHaveAttributeAsync("aria-current", "page");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SkipLink_ShouldShowAndMoveFocusToTheContent_WhenItIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/app-shell");
        var skip = page.GetByRole(AriaRole.Link, new() { Name = "Skip to content" });

        await skip.FocusAsync();
        Assert.True((await skip.BoundingBoxAsync())!.Width > 20, "The focused skip link is still hidden.");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator("main[data-part=main]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "After the heading" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AppShell_ShouldKeepTheBarAndNavigationInView_WhenThePageScrolls(string browser)
    {
        var page = await OpenAsync(browser, "/app-shell");

        await page.EvaluateAsync("() => scrollTo(0, 1500)");
        await page.WaitForFunctionAsync("() => scrollY > 1000");

        var bar = (await page.Locator("[data-part=app-bar]").BoundingBoxAsync())!;
        var navigation = (await Beside(page).BoundingBoxAsync())!;
        Assert.Equal((0, Math.Round(bar.Height)), (Math.Round(bar.Y), Math.Round(navigation.Y)));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MenuButton_ShouldNarrowTheNavigationToARail_WhenTheWindowIsWide(string browser)
    {
        var page = await OpenAsync(browser, "/app-shell");
        var wide = (await Beside(page).BoundingBoxAsync())!.Width;

        await page.GetByRole(AriaRole.Button, new() { Name = "Navigation" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Navigation" }))
            .ToHaveAttributeAsync("aria-expanded", "false");
        var rail = (await Beside(page).BoundingBoxAsync())!.Width;
        Assert.True(rail < wide / 2, $"The rail is {rail} wide, against {wide} open.");
        await Expect(Beside(page).GetByRole(AriaRole.Link, new() { Name = "Trips" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MenuButton_ShouldOpenTheNavigationInADrawer_WhenTheWindowIsNarrow(string browser)
    {
        var page = await OpenAsync(browser, "/app-shell", Narrow);
        await Expect(Beside(page)).ToBeHiddenAsync();
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Navigation" });

        await button.ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Navigation" });
        await Expect(drawer.GetByRole(AriaRole.Navigation, new() { Name = "Main" })).ToBeVisibleAsync();
        await drawer.GetByRole(AriaRole.Link, new() { Name = "Trips" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/app-shell/trips$"));
        await Expect(drawer).ToBeHiddenAsync();
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AppShell_ShouldHighlightTheCurrentLink_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/app-shell", new() { ForcedColors = ForcedColors.Active });
        var highlight = await page.EvaluateAsync<string>(@"() => {
            const probe = document.body.appendChild(document.createElement('div'));
            probe.style.cssText = 'background: Highlight; forced-color-adjust: none';
            return getComputedStyle(probe).backgroundColor; }");

        await Expect(Beside(page).GetByRole(AriaRole.Link, new() { Name = "Home" }))
            .ToHaveCSSAsync("background-color", highlight);
    }
}
