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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconsGuide_ShouldShowAndCopyTheMatchingIcons_WhenSearched(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/icons");
        var icons = page.GetByRole(AriaRole.List, new() { Name = "Matching icons" }).GetByRole(AriaRole.Button);

        await page.GetByLabel("Icon name").FillAsync("hiking");
        await Expect(icons).ToHaveTextAsync(["Hiking"]);
        await icons.ClickAsync();

        // Whether the browser lets the page copy or not, the toast names the icon.
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Notifications" }))
            .ToContainTextAsync("MaterialSymbols.Outlined.Hiking");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconsGuide_ShouldLoadTheIconPack_WhenOpenedThroughAnyAddressThatRoutesToIt(string browser)
    {
        var page = await OpenDocsAsync(browser, "Guides/Icons?style=rounded#find");

        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Matching icons" }).Locator("svg").First)
            .ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconPack_ShouldLoadOnlyWithThePagesThatDrawIt_WhenNavigating(string browser)
    {
        await using var context = await Fixture.NewContextAsync(browser);
        var page = await context.NewPageAsync();
        var loads = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("Trailblazor.Icons.Material", StringComparison.Ordinal))
            {
                loads.Add(request.Url);
            }
        };

        await page.GotoAsync(new Uri(Fixture.DocsAddress, "").ToString());
        await page.Locator(".docs-main h1").WaitForAsync();
        Assert.Empty(loads);
        await page.GetByRole(AriaRole.Link, new() { Name = "Icon", Exact = true }).First.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Hiking" })).ToBeVisibleAsync();
        Assert.NotEmpty(loads);
    }

    public static TheoryData<string, string, string> PagesInBothThemes => new(
        from browser in AllBrowsers.Names
        from path in new[]
        {
            "", "components/dialog", "guides/getting-started", "guides/palettes", "guides/browser",
            "guides/announcements", "guides/icons", "guides/localization", "guides/telemetry", "search?q=dialog",
            "roadmap",
        }
        from theme in new[] { "light", "dark" }
        select (browser, path, theme));

    [Fact]
    public async Task Site_ShouldLinkTheStylesheetBeforeTheAppStarts_WhenItsPageIsServed()
    {
        using var client = new HttpClient { BaseAddress = Fixture.DocsAddress };

        var html = await client.GetStringAsync("", TestContext.Current.CancellationToken);

        Assert.Contains("""<link rel="stylesheet" href="_content/Trailblazor/trailblazor.css" />""", html);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LookSwitcher_ShouldApplyAndRememberTheThemeAndPalette_WhenChosen(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        var dark = page.GetByRole(AriaRole.Group, new() { Name = "Theme" })
            .GetByRole(AriaRole.Button, new() { Name = "Dark" });
        await dark.ClickAsync();
        await page.Tb().Select("Palette").ChooseAsync("Cobalt");

        var html = page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(html).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await page.ReloadAsync();
        await page.Locator(".docs-main h1").WaitForAsync();
        await Expect(html).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(html).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await Expect(dark).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Palette" })).ToHaveTextAsync("Cobalt");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Site_ShouldShowTheLogoInTheHeaderAndAsItsIcon_WhenAPageBelowTheRootIsOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        await page.WaitForFunctionAsync(
            "() => document.querySelector('.docs-brand img').complete",
            null, new() { Timeout = 5000 });
        Assert.True(await page.Locator(".docs-brand img").EvaluateAsync<bool>("logo => logo.naturalWidth > 0"),
            "The header's logo didn't load.");

        foreach (var icon in await page.Locator("link[rel=icon]").AllAsync())
        {
            // The address the browser resolved, against the page's base rather than its own path.
            var address = await icon.EvaluateAsync<string>("link => link.href");
            Assert.True((await page.APIRequest.GetAsync(address)).Ok, $"{address} didn't load.");
        }

        await Expect(page.Locator("link[rel=icon]")).ToHaveCountAsync(2);
        // A link before <base> may be fetched against the page's own path; reading href later can't show that.
        Assert.True(await page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('link[rel=icon]')].every(link =>
                document.querySelector('base').compareDocumentPosition(link) & Node.DOCUMENT_POSITION_FOLLOWING)
            """), "An icon link comes before <base>.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Navigation_ShouldListEveryComponentUnderItsCategory_WhenOpen(string browser)
    {
        // The home page spans the window without the sidebar, so the list is checked from a guide.
        var page = await OpenDocsAsync(browser, "guides/getting-started");

        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" });

        await Expect(nav.GetByRole(AriaRole.Heading)).ToHaveTextAsync(["Guides", .. Enum.GetNames<ExampleCategory>()]);
        await Expect(nav.GetByRole(AriaRole.Link)).ToHaveTextAsync(Links);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Home_ShouldSpanTheWindowWithoutTheSidebar_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Guides and components" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" })).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Contents" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HeaderLink_ShouldShowTheCatalogBelowTheHeader_WhenComponentsIsClicked(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button",
            new() { ViewportSize = new() { Width = 1440, Height = 900 } });

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Site" })
            .GetByRole(AriaRole.Link, new() { Name = "Components" }).ClickAsync();

        var heading = page.GetByRole(AriaRole.Heading, new() { Name = "Guides and components" });
        await Expect(heading).ToBeInViewportAsync();
        // The sticky header must not cover the heading the link scrolled to.
        var header = (await page.Locator(".docs-header").BoundingBoxAsync())!;
        var title = (await heading.BoundingBoxAsync())!;
        Assert.True(title.Y >= header.Y + header.Height, $"The heading is at {title.Y}, under the header.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Home_ShouldLinkEveryComponentWithItsSummary_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "");

        var catalog = page.GetByRole(AriaRole.Region, new() { Name = "Guides and components" });

        await Expect(catalog.GetByRole(AriaRole.Link)).ToHaveTextAsync(Links);
        await Expect(catalog.GetByText(ExampleCatalog.Find("menu")!.Summary)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldShowEveryFileOfTheExample_WhenItHasMoreThanOne(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/dialog");

        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "DialogExample.razor" }))
            .ToContainTextAsync("ShowAsync<RenameDialog, string>");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "RenameDialog.razor" }))
            .ToContainTextAsync("TbDialogContent<string>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldTableTheComponentsParameters_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        await Expect(page.Locator(".docs-api__summary").First).ToHaveTextAsync(new Regex("^A button with text"));
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

        var notes = page.GetByRole(AriaRole.List, new() { Name = "Accessibility" }).GetByRole(AriaRole.Listitem);

        await Expect(notes).ToHaveCountAsync(ExampleCatalog.Find("button")!.Accessibility.Count);
        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Telemetry" }).GetByRole(AriaRole.Listitem))
            .ToHaveCountAsync(ExampleCatalog.Find("button")!.Telemetry.Count);
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
        var page = await OpenDocsAsync(browser, "guides/getting-started");
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
        var page = await OpenDocsAsync(browser, "guides/getting-started");
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
    public async Task Browser_ShouldFollowTheViewportAndDownloadTheList_WhenUsed(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/browser");
        var viewport = page.GetByText("Your window is");

        await page.SetViewportSizeAsync(700, 500);
        await Expect(viewport).ToHaveTextAsync("Your window is 700 by 500 pixels, which is breakpoint Sm.");
        var download = await page.RunAndWaitForDownloadAsync(() =>
            page.GetByRole(AriaRole.Button, new() { Name = "Download the list" }).ClickAsync());

        Assert.Equal("packing-list.csv", download.SuggestedFilename);
        var methods = page.GetByRole(AriaRole.Region, new() { Name = "IBrowserService" });
        await Expect(methods.GetByRole(AriaRole.Columnheader)).ToHaveTextAsync(["Member", "Type", "Description"]);
        await Expect(methods.GetByRole(AriaRole.Rowheader, new() { Name = "PickColorAsync()", Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Telemetry_ShouldCountTheDemoButtonsShownAndUsed_WhenTheyAreClicked(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/telemetry");
        var counts = page.GetByTestId("telemetry-counts");
        var export = page.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true });

        await Expect(counts.Locator("tbody tr")).ToHaveCountAsync(5);
        await export.ClickAsync();
        await export.ClickAsync();
        // Playwright waits forever for a disabled button to enable unless the click is forced.
        await page.GetByRole(AriaRole.Button, new() { Name = "Archive", Exact = true })
            .ClickAsync(new() { Force = true });

        await Expect(Counts("export-csv")).ToHaveTextAsync(["1", "2"]);
        await Expect(Counts("archive")).ToHaveTextAsync(["1", "0"]);
        await Expect(Counts("print")).ToHaveTextAsync(["1", "0"]);

        ILocator Counts(string name) => counts.Locator("tbody tr").Filter(new() { HasText = name }).Locator("td");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Telemetry_ShouldCountTheDemoMenusOpensAndItsItemsChosen_WhenOpenedByPointerAndKey(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/telemetry");
        var counts = page.GetByTestId("telemetry-counts");
        var more = page.GetByRole(AriaRole.Button, new() { Name = "More", Exact = true });
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");

        // The menu reports an open from its toggle event, which an Escape pressed straight after could beat.
        await more.ClickAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Escape");
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");
        await more.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Duplicate" }).ClickAsync();

        await Expect(Counts("more-actions")).ToHaveTextAsync(["1", "2"]);
        await Expect(Counts("duplicate")).ToHaveTextAsync(["0", "1"]);
        await Expect(counts.Locator("tbody tr").Filter(new() { HasText = "rename" })).ToHaveCountAsync(0);

        ILocator Counts(string name) => counts.Locator("tbody tr").Filter(new() { HasText = name }).Locator("td");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Announcements_ShouldSpeakAndClearMessages_WhenTheButtonsArePressed(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/announcements");
        var polite = page.Locator("[data-part=announcer] [role=status] p");
        var assertive = page.Locator("[data-part=announcer] [role=alert] p");

        await page.GetByRole(AriaRole.Button, new() { Name = "Announce politely" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Announce now" }).ClickAsync();
        await Expect(polite).ToContainTextAsync("Trail saved");
        await Expect(assertive).ToHaveTextAsync("Couldn't reach the server");
        await page.GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();
        await Expect(polite).ToHaveCountAsync(0);
        await Expect(assertive).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Announce politely" }).ClickAsync();
        await Expect(polite).ToHaveCountAsync(1);
        await Expect(polite).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Theming_ShouldNameEveryPalette_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/theming");

        var text = await page.Locator(".docs-main").InnerTextAsync();

        Assert.All(PaletteCatalog.All,
            palette => Assert.Contains(palette.Name, text, StringComparison.OrdinalIgnoreCase));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Palettes_ShouldShowEachRampsStepsInItsColor_WhenOpened(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/palettes");

        var teal = page.GetByRole(AriaRole.Region, new() { Name = "teal ramp" }).GetByRole(AriaRole.Listitem);
        await Expect(teal).ToHaveTextAsync(["50", "100", "200", "300", "400", "500", "600", "700", "800", "900", "950"]);
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "graphite ramp" }).GetByRole(AriaRole.Listitem))
            .ToHaveCountAsync(15);
        await Expect(teal.Nth(6).Locator(".docs-ramp__swatch")).Not.ToHaveCSSAsync("background-color",
            "rgba(0, 0, 0, 0)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Navigation_ShouldStayClosedUntilAskedFor_WhenTheScreenIsNarrow(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/getting-started",
            new() { ViewportSize = new() { Width = 390, Height = 844 } });
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
