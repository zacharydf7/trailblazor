using Deque.AxeCore.Playwright;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The published docs site, served as GitHub Pages serves it.
public class DocsSiteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The titles in the order the site lists them: by category, then as the catalog has them.
    private static string[] Titles => [.. ExampleCatalog.All.OrderBy(example => example.Category)
        .Select(example => example.Title)];

    // The page's own heading; an example, such as the one for text, can show headings of its own.
    private static ILocator PageTitle(IPage page) => page.Locator(".docs-main > h1");

    public static TheoryData<string, string, string> PagesInBothThemes => new(
        from browser in AllBrowsers.Names
        from path in new[] { "", "components/dialog" }
        from theme in new[] { "light", "dark" }
        select (browser, path, theme));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LookSwitcher_ShouldApplyAndRememberTheThemeAndPalette_WhenChosen(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        await page.Tb().Select("Theme").ChooseAsync("Dark");
        await page.Tb().Select("Palette").ChooseAsync("Cobalt");

        var html = page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(html).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await page.ReloadAsync();
        await page.Locator(".docs-main h1").WaitForAsync();
        await Expect(html).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(html).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Theme" })).ToHaveTextAsync("Dark");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Navigation_ShouldListEveryComponentUnderItsCategory_WhenOpen(string browser)
    {
        var page = await OpenDocsAsync(browser, "");

        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Components" });

        await Expect(nav.GetByRole(AriaRole.Heading)).ToHaveTextAsync(Enum.GetNames<ExampleCategory>());
        await Expect(nav.GetByRole(AriaRole.Link)).ToHaveTextAsync(Titles);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Home_ShouldLinkEveryComponentWithItsSummary_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "");

        var main = page.GetByRole(AriaRole.Main);

        await Expect(main.GetByRole(AriaRole.Link)).ToHaveTextAsync(Titles);
        await Expect(main.GetByText(ExampleCatalog.Find("menu")!.Summary)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldShowEveryFileOfTheExample_WhenItHasMoreThanOne(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/dialog");

        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "DialogExample.razor" }))
            .ToContainTextAsync("ShowAsync<RenameDialog, string>");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "RenameDialog.razor" }))
            .ToContainTextAsync("TbDialog<string>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Site_ShouldOpenEveryComponentPage_WhenEachIsChosenFromTheList(string browser)
    {
        var page = await OpenDocsAsync(browser, "");
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Components" });

        foreach (var title in Titles)
        {
            await nav.GetByRole(AriaRole.Link, new() { Name = title, Exact = true }).ClickAsync();

            await Expect(PageTitle(page)).ToHaveTextAsync(title);
            // Not text: an example can be all icons.
            await Expect(page.Locator(".docs-example > *")).Not.ToHaveCountAsync(0);
        }

        await Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [Theory, MemberData(nameof(PagesInBothThemes))]
    public async Task Site_ShouldHaveNoAccessibilityViolations_WhenOpenedInTheme(string browser, string path,
        string theme)
    {
        var page = await OpenDocsAsync(browser, path);
        await SetThemeAsync(page, theme);

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Navigation_ShouldStayClosedUntilAskedFor_WhenTheScreenIsNarrow(string browser)
    {
        var page = await OpenDocsAsync(browser, "", new() { ViewportSize = new() { Width = 390, Height = 844 } });
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Components" });
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Components" });
        await Expect(nav).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await nav.GetByRole(AriaRole.Link, new() { Name = "Menu", Exact = true }).ClickAsync();

        await Expect(PageTitle(page)).ToHaveTextAsync("Menu");
        await Expect(nav).ToBeHiddenAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
