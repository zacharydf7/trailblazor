using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class PaginationTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Navigation(IPage page, string name) =>
        page.GetByRole(AriaRole.Navigation, new() { Name = name, Exact = true });

    private static ILocator Page(IPage page, string pagination, int number) =>
        Navigation(page, pagination).GetByRole(AriaRole.Button, new() { Name = $"Page {number}", Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PageBox_ShouldGoToTheTypedPageWithoutSubmittingTheForm_WhenEnterIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/pagination");
        var box = Navigation(page, "In a form").Locator("input");
        await box.FillAsync("12");

        await box.PressAsync("Enter");

        await Expect(page.GetByTestId("box-page")).ToHaveTextAsync("12");
        // Events reach the server in order, so once this shows, a submit from Enter would have shown too.
        await Page(page, "Lean", 2).ClickAsync();
        await Expect(page.GetByTestId("page")).ToHaveTextAsync("2");
        await Expect(page.GetByTestId("submits")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldShowFewerPagesOnOneRow_WhenItHasLittleRoom(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        var reports = Navigation(page, "Trail reports").Locator("[data-part=page]");
        var camps = Navigation(page, "Campsites");
        // On its first page, Trail reports shows 1 to 5 and 20.
        await Expect(reports).ToHaveCountAsync(6);
        await Expect(camps.Locator("[data-part=page]")).Not.ToHaveCountAsync(0);
        // No control of any pager starts below its first one.
        const string OneRowEach = """
            () => [...document.querySelectorAll('.tb-pagination__list')].every(list =>
                [...list.children].every(item => item.offsetTop < list.children[0].offsetTop + list.children[0].offsetHeight))
            """;

        await page.SetViewportSizeAsync(390, 800);

        // Trail reports gives up the current page's siblings, and Campsites, with more to show, turns to its page box.
        await Expect(reports).ToHaveCountAsync(4);
        await Expect(camps.Locator("[data-part=page-box]")).ToBeVisibleAsync();
        await page.WaitForFunctionAsync(OneRowEach);

        await page.SetViewportSizeAsync(1280, 800);
        await Expect(reports).ToHaveCountAsync(6);
        await Expect(camps.Locator("[data-part=page-box]")).ToHaveCountAsync(0);
    }

    private static ILocator Pages(IPage page, string pagination) =>
        Navigation(page, pagination).Locator("[data-part=page]");

    private static Task WidenAsync(IPage page, string frame, string width) =>
        page.GetByTestId(frame).EvaluateAsync("(frame, width) => frame.style.inlineSize = width", width);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldGetItsPagesBack_WhenTheRowItSharesOrTheBoxAroundItsWrapperWidens(string browser)
    {
        var page = await OpenAsync(browser, "/pagination-fit");
        await Expect(Navigation(page, "Wrapped")).ToHaveAttributeAsync("data-ready", "");
        await Expect(Pages(page, "Wrapped")).ToHaveCountAsync(6);

        // The wrapper is as wide as what it holds, so only the frame around it changes size when there is room again.
        await WidenAsync(page, "wrapped", "18rem");
        await Expect(Pages(page, "Wrapped")).Not.ToHaveCountAsync(6);
        await WidenAsync(page, "wrapped", "");
        await Expect(Pages(page, "Wrapped")).ToHaveCountAsync(6);

        // The note beside the pager takes room from it, and gives it back when it gets shorter.
        await WidenAsync(page, "shared", "34rem");
        await Expect(Pages(page, "Shared row")).ToHaveCountAsync(4);
        await page.GetByRole(AriaRole.Button, new() { Name = "Shorten the note" }).ClickAsync();
        await Expect(Pages(page, "Shared row")).ToHaveCountAsync(6);

        // So does a chip that wasn't in the row when the pager first measured it.
        var chip = page.GetByRole(AriaRole.Button, new() { Name = "Toggle the chip" });
        await chip.ClickAsync();
        await Expect(Pages(page, "Shared row")).Not.ToHaveCountAsync(6);
        await chip.ClickAsync();
        await Expect(Pages(page, "Shared row")).ToHaveCountAsync(6);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldGetItsPagesBack_WhenItsPageBoxWasAllItShowed(string browser)
    {
        var page = await OpenAsync(browser, "/pagination-fit");
        var alone = Navigation(page, "Pages alone");
        await Expect(alone).ToHaveAttributeAsync("data-ready", "");

        await WidenAsync(page, "alone", "9rem");
        await Expect(alone.Locator("[data-part=page-box]")).ToBeVisibleAsync();
        await WidenAsync(page, "alone", "");

        await Expect(Pages(page, "Pages alone")).ToHaveCountAsync(6);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldKeepItsPages_WhenItIsHidden(string browser)
    {
        var page = await OpenAsync(browser, "/pagination-fit");
        // Found by its element, since a hidden pager has no role to find it by.
        var hidden = page.GetByTestId("hidden").Locator("nav");
        await Expect(hidden).ToHaveAttributeAsync("data-ready", "");

        await page.GetByTestId("hidden").EvaluateAsync("frame => frame.hidden = true");
        // The fit a size change brings is marked, then finishes; a hidden pager has no width to fit to.
        await hidden.EvaluateAsync("pager => new Promise(done => requestAnimationFrame(() => setTimeout(done)))");
        await Expect(hidden).Not.ToHaveAttributeAsync("data-fitting", "");

        await Expect(hidden.Locator("[data-part=page]")).ToHaveCountAsync(6);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldKeepFocusInside_WhenTheFocusedPageGoesForLackOfRoom(string browser)
    {
        var page = await OpenAsync(browser, "/pagination-fit");
        await Expect(Navigation(page, "Wrapped")).ToHaveAttributeAsync("data-ready", "");
        await Page(page, "Wrapped", 5).FocusAsync();

        // Page 5 is a sibling the pager gives up, and then every page goes for the page box.
        await WidenAsync(page, "wrapped", "24rem");
        await Expect(Page(page, "Wrapped", 1)).ToBeFocusedAsync();
        await WidenAsync(page, "wrapped", "12rem");
        await Expect(Navigation(page, "Wrapped").Locator("[data-part=page-box] input")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GoToPageAsync_ShouldReachThePage_WhenThePaginationStartsWithLittleRoom(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination",
            new() { ViewportSize = new() { Width = 390, Height = 800 } });

        // Campsites turns to its page box here, and Trail reports to fewer pages, after the page is interactive.
        await page.Tb().Pagination("Campsites").GoToPageAsync(9);
        await page.Tb().Pagination("Trail reports").GoToPageAsync(7);

        Assert.Equal(9, await page.Tb().Pagination("Campsites").PageAsync());
        Assert.Equal(7, await page.Tb().Pagination("Trail reports").PageAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldGoToThePageAndKeepFocusOnIt_WhenAPageIsChosenWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        await Page(page, "Trail reports", 5).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(Page(page, "Trail reports", 5)).ToHaveAttributeAsync("aria-current", "page");
        await Expect(page.GetByText("Showing report page")).ToHaveTextAsync("Showing report page 5 of 20");
        await Expect(Page(page, "Trail reports", 6)).ToBeVisibleAsync();
        await Expect(Page(page, "Trail reports", 2)).ToHaveCountAsync(0);
        await Expect(page.Locator(":focus")).ToHaveAttributeAsync("aria-current", "page");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldStepStopAndKeepFocusAtTheEnds_WhenFirstPreviousNextAndLastAreUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        var camps = Navigation(page, "Campsites");
        var text = page.GetByText("Showing campsite page");

        await camps.GetByRole(AriaRole.Button, new() { Name = "Last page" }).ClickAsync();
        await Expect(text).ToHaveTextAsync("Showing campsite page 12 of 12");
        await Expect(camps.GetByRole(AriaRole.Button, new() { Name = "Next page" })).ToBeDisabledAsync();
        await camps.GetByRole(AriaRole.Button, new() { Name = "Previous page" }).ClickAsync();
        await Expect(text).ToHaveTextAsync("Showing campsite page 11 of 12");
        var next = camps.GetByRole(AriaRole.Button, new() { Name = "Next page" });
        await next.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(text).ToHaveTextAsync("Showing campsite page 12 of 12");
        await Expect(next).ToBeDisabledAsync();
        await Expect(next).ToBeFocusedAsync();
        await camps.GetByRole(AriaRole.Button, new() { Name = "First page" }).ClickAsync();

        await Expect(text).ToHaveTextAsync("Showing campsite page 1 of 12");
        await Expect(camps.GetByRole(AriaRole.Button, new() { Name = "Previous page" })).ToBeDisabledAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldGoToTheTypedPage_WhenAPageIsTypedInCompactMode(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        var box = Navigation(page, "Photos").GetByRole(AriaRole.Spinbutton, new() { Name = "Page" });
        await Expect(Navigation(page, "Photos")).ToContainTextAsync("of 140");

        await box.FillAsync("42");
        await box.PressAsync("Enter");
        await Expect(page.GetByText("Showing photo")).ToHaveTextAsync("Showing photo 42 of 140");
        await box.FillAsync("999");
        await box.PressAsync("Tab");

        await Expect(page.GetByText("Showing photo")).ToHaveTextAsync("Showing photo 140 of 140");
        await Expect(box).ToHaveValueAsync("140");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PageBox_ShouldShowTheCurrentPageAgain_WhenClearedOrTypedPastTheEndWhileThere(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        var box = Navigation(page, "Photos").GetByRole(AriaRole.Spinbutton, new() { Name = "Page" });
        await Expect(Navigation(page, "Photos")).ToContainTextAsync("of 140");
        await box.FillAsync("140");
        await box.PressAsync("Enter");
        await Expect(page.GetByText("Showing photo")).ToHaveTextAsync("Showing photo 140 of 140");

        await box.FillAsync("999");
        await box.PressAsync("Enter");
        await Expect(box).ToHaveValueAsync("140");
        await box.FillAsync("");
        await box.PressAsync("Tab");

        await Expect(box).ToHaveValueAsync("140");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldGoToTheNextPage_WhenUpIsPressedInCompactMode(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");

        await Navigation(page, "Photos").GetByRole(AriaRole.Spinbutton).PressAsync("ArrowUp");

        await Expect(page.GetByText("Showing photo")).ToHaveTextAsync("Showing photo 2 of 140");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldTurnItsArrowsAround_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        var next = Navigation(page, "Trail reports").GetByRole(AriaRole.Button, new() { Name = "Next page" });

        await Navigation(page, "Trail reports").EvaluateAsync("nav => nav.dir = 'rtl'");

        await Expect(next.Locator("svg")).ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GoToPageAsync_ShouldReachAPageThatIsntShown_WhenSteppingThroughTheNeighbors(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");

        await page.Tb().Pagination("Trail reports").GoToPageAsync(11);
        await Expect(page.GetByText("Showing report page")).ToHaveTextAsync("Showing report page 11 of 20");
        await page.Tb().Pagination("Trail reports").GoToPageAsync(3);
        await Expect(page.GetByText("Showing report page")).ToHaveTextAsync("Showing report page 3 of 20");
        await page.Tb().Pagination("Photos").GoToPageAsync(77);

        await Expect(page.GetByText("Showing photo")).ToHaveTextAsync("Showing photo 77 of 140");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GoToPageAsync_ShouldThrow_WhenThePageIsPastTheEnd(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => page.Tb().Pagination("Campsites").GoToPageAsync(13));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => page.Tb().Pagination("Photos").GoToPageAsync(141));
        await Expect(page.GetByText("Showing campsite page")).ToHaveTextAsync("Showing campsite page 6 of 12");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GoToPageAsync_ShouldStepWithNext_WhenNoPageShowsBetween(string browser)
    {
        var page = await OpenAsync(browser, "/pagination");

        await page.Tb().Pagination("Lean").GoToPageAsync(9);

        await Expect(page.GetByTestId("page")).ToHaveTextAsync("9");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GoToPageAsync_ShouldWaitForTheTypedPage_WhenTheCompactBoxIsUsedBeforeThePageIsInteractive(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/examples/pagination");

        await page.Tb().Pagination("Photos").GoToPageAsync(42);

        await Expect(page.GetByText("Showing photo")).ToHaveTextAsync("Showing photo 42 of 140");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldLinkEachPage_WhenGivenPageHref(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination");
        var chapters = Navigation(page, "Guide chapters");

        await chapters.GetByRole(AriaRole.Link, new() { Name = "Page 3", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("chapter=3$"));
        await Expect(chapters.GetByRole(AriaRole.Link, new() { Name = "Page 3", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pagination_ShouldFillTheCurrentPageWithTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination", new() { ForcedColors = ForcedColors.Active });

        await Expect(Page(page, "Trail reports", 1))
            .ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Highlight"));
    }
}
