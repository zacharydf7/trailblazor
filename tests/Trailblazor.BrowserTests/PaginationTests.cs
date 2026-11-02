using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class PaginationTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Navigation(IPage page, string name) =>
        page.GetByRole(AriaRole.Navigation, new() { Name = name, Exact = true });

    private static ILocator Page(IPage page, string pagination, int number) =>
        Navigation(page, pagination).GetByRole(AriaRole.Button, new() { Name = $"Page {number}", Exact = true });

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
    public async Task Pagination_ShouldFillTheCurrentPageWithTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/pagination", new() { ForcedColors = ForcedColors.Active });

        await Expect(Page(page, "Trail reports", 1))
            .ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Highlight"));
    }
}
