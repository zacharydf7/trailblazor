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
    private static ILocator PageTitle(IPage page) => page.Locator(":is(.docs-main, .docs-article__body) > h1");

    // A section's code shows only once asked for.
    private static async Task ShowCodeAsync(IPage page, string section)
    {
        var frame = page.Locator(".docs-example").Filter(new() { Has = page.Locator($"#code-{section}") });
        await frame.GetByRole(AriaRole.Button, new() { Name = "Show code" }).ClickAsync();
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
            "", "components/dialog", "components/select", "components/tree-view", "components/data-grid/api",
            "preview/tree-view/many-rows", "guides/getting-started", "guides/theming", "guides/palettes",
            "guides/spacing", "guides/browser", "guides/testing",
            "guides/announcements", "guides/icons", "guides/localization", "guides/telemetry", "guides/analyzers",
            "search?q=dialog",
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
    public async Task ThemingGuide_ShouldRestyleThePreviewAndItsCss_WhenABrandColorIsPicked(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/theming");
        var color = page.GetByRole(AriaRole.Textbox, new() { Name = "Brand color" });
        var save = page.Locator(".docs-brand").GetByRole(AriaRole.Button, new() { Name = "Save" });
        var before = await save.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");

        await color.FillAsync("#b91c1c");
        await color.PressAsync("Enter");

        await Expect(page.GetByText("from #b91c1c min(l, 0.55)").First).ToBeVisibleAsync();
        await Expect(save).Not.ToHaveCSSAsync("background-color", before);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LookSwitcher_ShouldApplyAndRememberTheThemePaletteAndDensity_WhenChosen(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/button");

        var dark = page.GetByRole(AriaRole.Group, new() { Name = "Theme" })
            .GetByRole(AriaRole.Button, new() { Name = "Dark" });
        await dark.ClickAsync();
        await page.Tb().Select("Palette").ChooseAsync("Cobalt");
        await page.Tb().Select("Density").ChooseAsync("Compact");

        var html = page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(html).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await Expect(html).ToHaveAttributeAsync("data-tb-density", "compact");
        await page.ReloadAsync();
        await page.Locator(".docs-main h1").WaitForAsync();
        await Expect(html).ToHaveAttributeAsync("data-tb-theme", "dark");
        await Expect(html).ToHaveAttributeAsync("data-tb-palette", "cobalt");
        await Expect(html).ToHaveAttributeAsync("data-tb-density", "compact");
        await Expect(dark).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Palette" })).ToHaveTextAsync("Cobalt");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Density" })).ToHaveTextAsync("Compact");
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
    public async Task Home_ShouldEndEachCardWithItsLinkAtOneHeightAndFitItsCode_WhenWide(string browser)
    {
        var page = await OpenDocsAsync(browser, "", new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        var cards = page.Locator(".docs-pillar");

        var links = await cards.EvaluateAllAsync<double[]>(
            "cards => cards.map(card => card.lastElementChild.getBoundingClientRect().bottom)");
        var starts = await cards.EvaluateAllAsync<double[]>("cards => cards.map(card => card.lastElementChild"
            + ".getBoundingClientRect().left - card.querySelector('h3').getBoundingClientRect().left)");

        Assert.Equal(3, links.Length);
        Assert.All(links, bottom => Assert.Equal(links[0], bottom, 1));
        Assert.All(starts, start => Assert.Equal(0, start, 1));
        Assert.True(await cards.Locator("pre").EvaluateAllAsync<bool>(
            "blocks => blocks.every(pre => pre.scrollWidth <= pre.clientWidth)"), "A card's code should fit it.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Home_ShouldFitEachCardsCode_WhenTheScreenIsANarrowPhone(string browser)
    {
        var page = await OpenDocsAsync(browser, "", new() { ViewportSize = new() { Width = 360, Height = 800 } });

        Assert.True(await page.Locator(".docs-pillar pre").EvaluateAllAsync<bool>(
            "blocks => blocks.every(pre => pre.scrollWidth <= pre.clientWidth)"), "A card's code should fit it.");
    }

    public static TheoryData<string, string> Guides => new(
        from browser in AllBrowsers.Names
        from guide in GuideCatalog.All
        select (browser, guide.Path));

    [Theory, MemberData(nameof(Guides))]
    public async Task Guide_ShouldFitThePageWidth_WhenTheScreenIsAPhone(string browser, string path)
    {
        var page = await OpenDocsAsync(browser, path, new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await Expect(page.Locator(".docs-article__body > h2").First).ToBeVisibleAsync();

        Assert.True(await page.Locator("html").EvaluateAsync<bool>("html => html.scrollWidth <= html.clientWidth"),
            "Wide code and tables should scroll inside their own box, not the page.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldScrollLongCodeLinesWithoutWrapping_WhenTheyDontFit(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/tree-view",
            new() { ViewportSize = new() { Width = 1024, Height = 800 } });
        await ShowCodeAsync(page, "filter-and-select");
        var source = page.GetByRole(AriaRole.Region, new() { Name = "TreeViewExample.razor" });
        var code = await source.EvaluateAsync<string>("pre => pre.textContent");

        Assert.True(await source.EvaluateAsync<bool>("pre => pre.scrollWidth > pre.clientWidth"),
            "A line too long for the block should scroll it sideways.");
        // A line that doesn't wrap takes one row, so the code has a row for each line with text.
        Assert.True(await source.EvaluateAsync<bool>("""
            pre => {
                const code = pre.querySelector('code');
                const range = document.createRange();
                range.selectNodeContents(code);
                const rows = new Set([...range.getClientRects()].filter(rect => rect.width > 0)
                    .map(rect => Math.round(rect.top)));
                return rows.size === code.textContent.split('\n').filter(line => line.length > 0).length;
            }
            """), "No line should wrap onto a second row.");
        Assert.Contains("26rem\">\n    <TbTextField", code);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldScrollCodeSideways_WhenTheScreenIsAPhone(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/tree-view",
            new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await ShowCodeAsync(page, "filter-and-select");
        var source = page.GetByRole(AriaRole.Region, new() { Name = "TreeViewExample.razor" });

        Assert.True(await source.EvaluateAsync<bool>("pre => pre.scrollWidth > pre.clientWidth"),
            "On a phone, long lines should scroll rather than wrap.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldUseTheExtraWidth_WhenTheScreenIsWide(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/tree-view",
            new() { ViewportSize = new() { Width = 1900, Height = 1000 } });

        var main = await page.Locator(".docs-main").BoundingBoxAsync();
        var lead = await page.Locator(".docs-lead").BoundingBoxAsync();
        var notes = await page.Locator(".docs-notes li").First.BoundingBoxAsync();

        Assert.True(main!.Width > 1200, $"The content column should widen on a wide screen, but is {main.Width}px.");
        Assert.True(lead!.Width <= 44 * 16, $"A paragraph should keep a readable width, but is {lead.Width}px.");
        Assert.True(notes!.Width <= 44 * 16, $"A list should keep a readable width, but is {notes.Width}px.");
    }

    // CodeBlockTests covers the spans; this checks only that the stylesheet colors them.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CodeBlock_ShouldColorTokensApartFromPlainText_WhenItShowsRazor(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/getting-started");
        var code = page.GetByRole(AriaRole.Region, new() { Name = "A first component" });

        var colors = await code.EvaluateAsync<string[]>("""
            pre => ['tag', 'attribute', 'string', 'keyword'].map(kind => getComputedStyle(
                pre.querySelector(`.docs-code__${kind}`)).color).concat(getComputedStyle(pre).color)
            """);

        Assert.Equal(colors.Length, colors.Distinct().Count());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldListItsSectionsBesideIt_WhenTheScreenIsWide(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/tree-view",
            new() { ViewportSize = new() { Width = 1600, Height = 900 } });
        var contents = page.GetByRole(AriaRole.Navigation, new() { Name = "On this page" });
        await Expect(contents).ToHaveAttributeAsync("data-ready", "");

        await Expect(contents.GetByRole(AriaRole.Link)).ToHaveTextAsync(["Filter, select and open", "Checkboxes",
            "Move and insert", "Many rows", "Accessibility", "Telemetry", "Design tokens", "Testing"]);
        await contents.GetByRole(AriaRole.Link, new() { Name = "Many rows" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Many rows", Level = 2 })).ToBeInViewportAsync();
        await Expect(contents.GetByRole(AriaRole.Link, new() { Name = "Many rows" }))
            .ToHaveAttributeAsync("aria-current", "location");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ComponentPage_ShouldFoldItsSectionsUnderTheTitle_WhenTheScreenIsNarrow(string browser)
    {
        var page = await OpenDocsAsync(browser, "components/tree-view",
            new() { ViewportSize = new() { Width = 390, Height = 844 } });
        var contents = page.GetByRole(AriaRole.Navigation, new() { Name = "On this page" });

        await Expect(contents).ToBeHiddenAsync();
        await page.GetByText("On this page").ClickAsync();

        await Expect(contents.GetByRole(AriaRole.Link, new() { Name = "Checkboxes" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Preview_ShouldLoadTheIconPack_WhenTheIconExampleIsOpenedOnItsOwn(string browser)
    {
        var page = await OpenDocsAsync(browser, "preview/icon/example");

        await Expect(page.GetByRole(AriaRole.Main).Locator("svg").First).ToBeVisibleAsync();
        await Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Preview_ShouldShowTheExampleWithoutTheSite_WhenASectionIsOpenedOnItsOwn(string browser)
    {
        var page = await OpenDocsAsync(browser, "preview/tree-view/many-rows");

        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = "Trail atlas" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" })).ToHaveCountAsync(0);
        await Expect(page).ToHaveTitleAsync("Many rows · Tree view · Trailblazor");
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
            await Expect(page.Locator(".docs-example__demo > *")).Not.ToHaveCountAsync(0);
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

    // SiteTests covers the toggle's state; this checks only that the stylesheet hides and shows the list.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Navigation_ShouldStayHiddenUntilAskedFor_WhenTheScreenIsNarrow(string browser)
    {
        var page = await OpenDocsAsync(browser, "guides/getting-started",
            new() { ViewportSize = new() { Width = 390, Height = 844 } });
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Contents" });
        await Expect(nav).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Contents" }).ClickAsync();

        await Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Menu", Exact = true })).ToBeVisibleAsync();
    }
}
