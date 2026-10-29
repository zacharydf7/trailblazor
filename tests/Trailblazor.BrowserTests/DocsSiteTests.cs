using System.Globalization;
using System.Text.RegularExpressions;
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

    // The guides come before the components, in the sidebar and on the home page.
    private static string[] Links => [.. GuideCatalog.All.Select(guide => guide.Title), .. Titles];

    // The page's own heading; an example, such as the one for text, can show headings of its own.
    private static ILocator PageTitle(IPage page) => page.Locator(".docs-main > h1");

    public static TheoryData<string, string, string> PagesInBothThemes => new(
        from browser in AllBrowsers.Names
        from path in new[] { "", "components/dialog", "guides/getting-started", "search?q=dialog", "roadmap" }
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

        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" });

        await Expect(nav.GetByRole(AriaRole.Heading)).ToHaveTextAsync(["Guides", .. Enum.GetNames<ExampleCategory>()]);
        await Expect(nav.GetByRole(AriaRole.Link)).ToHaveTextAsync(Links);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Home_ShouldLinkEveryComponentWithItsSummary_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "");

        var main = page.GetByRole(AriaRole.Main);

        await Expect(main.GetByRole(AriaRole.Link)).ToHaveTextAsync(Links);
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
    public async Task ComponentPage_ShouldTableTheComponentsParameters_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        var table = page.GetByRole(AriaRole.Region, new() { Name = "TbButton" });
        var variant = table.GetByRole(AriaRole.Row)
            .Filter(new() { Has = page.GetByRole(AriaRole.Rowheader, new() { Name = "Variant" }) });

        var cells = variant.GetByRole(AriaRole.Cell);
        await Expect(cells.Nth(0)).ToHaveTextAsync("Variant");
        await Expect(cells.Nth(1)).ToHaveTextAsync("Variant.Filled");
        await Expect(cells.Nth(2)).ToHaveTextAsync(new Regex("^How strongly the button is drawn"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldListTheAccessibilityNotesAndTheLocator_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        var notes = page.Locator(".docs-notes > li");

        await Expect(notes).ToHaveCountAsync(ExampleCatalog.Find("button")!.Accessibility.Count);
        await Expect(notes.First).ToHaveTextAsync(new Regex("^A native <button>, so Enter and Space press it."));
        await Expect(notes.First.Locator("code").First).ToHaveTextAsync("<button>");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Finding it in Playwright" }))
            .ToHaveTextAsync("""page.GetByRole(AriaRole.Button, new() { Name = "Add item" })""");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldListTheThemeTokensTheComponentReads_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/dialog");

        var tokens = page.GetByRole(AriaRole.List, new() { Name = "Design tokens" }).GetByRole(AriaRole.Listitem);

        await Expect(tokens).ToHaveTextAsync([.. DesignTokens.Read(["tb-dialog"])]);
        await Expect(tokens.Filter(new() { HasText = "--tb-shadow-lg" })).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldSayItReadsNoThemeTokens_WhenTheComponentReadsNone(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/stack");

        await Expect(page.GetByText("Its styles read no theme tokens.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Design tokens" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldShowTheStepThatOpensTheComponent_WhenItOnlyShowsAfterOne(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/dialog");

        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Finding it in Playwright" })).ToHaveTextAsync(
            """
            await page.GetByRole(AriaRole.Button, new() { Name = "Rename report" }).ClickAsync();
            page.GetByRole(AriaRole.Dialog, new() { Name = "Rename report" })
            """);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldSayThereIsNoRoleToFindItBy_WhenTheComponentHasNone(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/stack");

        await Expect(page.GetByText("It has no role and name a test can single out")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Finding it in Playwright" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Search_ShouldListTheComponentsWithAParameter_WhenItsNameIsSearched(string browser)
    {
        var page = await OpenDocsAsync(browser, "");

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" }).FillAsync("placeholder");
        await page.Keyboard.PressAsync("Enter");

        var status = page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Status);
        await Expect(status).ToHaveTextAsync($"{DocsSearch.Find("placeholder").Count} results for “placeholder”.");
        var select = page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Picks one or several options" });
        await Expect(select).ToContainTextAsync("Has Placeholder");
        await select.GetByRole(AriaRole.Link, new() { Name = "Select" }).ClickAsync();
        await Expect(PageTitle(page)).ToHaveTextAsync("Select");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SearchBox_ShouldShowTheQuery_WhenTheResultsPageIsOpenedFromALink(string browser)
    {
        var page = await OpenDocsAsync(browser, "search?q=dialog%20options");

        var box = page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" });
        await Expect(box).ToHaveValueAsync("dialog options");
        await box.FillAsync("menu");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Status)).ToContainTextAsync("“menu”");
        await page.GoBackAsync();

        await Expect(box).ToHaveValueAsync("dialog options");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Search_ShouldSaySoAndListNothing_WhenNothingMatches(string browser)
    {
        var page = await OpenDocsAsync(browser, "search?q=zebra");

        var status = page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Status);
        await Expect(status).ToHaveTextAsync("Nothing matches “zebra”.");
        await Expect(page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Site_ShouldOpenEveryGuide_WhenEachIsChosenFromTheList(string browser)
    {
        var page = await OpenDocsAsync(browser, "");
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" });

        foreach (var guide in GuideCatalog.All)
        {
            await nav.GetByRole(AriaRole.Link, new() { Name = guide.Title, Exact = true }).ClickAsync();

            await Expect(PageTitle(page)).ToHaveTextAsync(guide.Title);
            await Expect(page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Heading, new() { Level = 2 }).First)
                .ToBeVisibleAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Roadmap_ShouldShowEachPhasesProgressAndComponents_WhenOpened(string browser)
    {
        var roadmap = Roadmap.Current;
        var page = await OpenDocsAsync(browser, "roadmap");

        var bars = page.GetByRole(AriaRole.Progressbar);

        await Expect(bars).ToHaveCountAsync(roadmap.Phases.Count);
        var first = roadmap.Phases[0];
        await Expect(page.GetByRole(AriaRole.Progressbar, new() { Name = first.Title }))
            .ToHaveAttributeAsync("aria-valuenow", first.Shipped.ToString(CultureInfo.InvariantCulture));
        var shipped = roadmap.Phases.SelectMany(phase => phase.Components)
            .First(component => component.Status == ComponentStatus.Shipped);
        var status = page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Listitem)
            .Filter(new() { HasText = shipped.Title }).Locator("[data-status]");
        await Expect(status).ToHaveTextAsync("Shipped");
        await Expect(status).ToHaveAttributeAsync("data-status", "shipped");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Site_ShouldOpenEveryComponentPage_WhenEachIsChosenFromTheList(string browser)
    {
        var page = await OpenDocsAsync(browser, "");
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" });

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
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Contents" });
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" });
        await Expect(nav).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await nav.GetByRole(AriaRole.Link, new() { Name = "Menu", Exact = true }).ClickAsync();

        await Expect(PageTitle(page)).ToHaveTextAsync("Menu");
        await Expect(nav).ToBeHiddenAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
