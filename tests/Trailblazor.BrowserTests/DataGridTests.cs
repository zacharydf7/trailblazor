using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DataGridTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSortByAColumnAndSaySo_WhenItsHeaderIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");

        await trails.SortAsync("Name");
        await Expect(Names(page)).ToHaveTextAsync(["Cedar Flats", "Eagle Ridge traverse", "Fern Gully", "Lake camp",
            "Pine Hollow loop"]);
        await trails.SortAsync("Name", descending: true);

        await Expect(Names(page).First).ToHaveTextAsync("Waterfall spur");
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("Sorted by Name, descending.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSortByOneColumnAlone_WhenAskedWhileSortedByTwo(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");
        await trails.SortAsync("Region");
        await Table(page, "Trails").Locator("th[data-title=Name] [data-part=sort]")
            .ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Table(page, "Trails").Locator(".tb-data-grid__sort-order")).ToHaveCountAsync(2);

        await trails.SortAsync("Region", descending: true);

        await Expect(Table(page, "Trails").Locator(".tb-data-grid__sort-order")).ToHaveCountAsync(0);
        await Expect(Table(page, "Trails").Locator("th[data-title=Region]")).ToHaveAttributeAsync("aria-sort",
            "descending");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSortFromTheKeyboard_WhenEnterIsPressedOnAHeader(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var header = Table(page, "Trails").Locator("th[data-title='Length (km)']");
        await Expect(page.Locator(".tb-data-grid").First).ToHaveAttributeAsync("data-ready", "");

        await header.GetByRole(AriaRole.Button).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Names(page).First).ToHaveTextAsync("Waterfall spur");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldShowEachPageAndItsRange_WhenPagedOrGivenAnotherPageSize(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var summary = Table(page, "Trails").Locator("xpath=ancestor::*[contains(@class, 'tb-data-grid ')][1]")
            .Locator("[data-part=summary]");
        await Expect(summary).ToHaveTextAsync("1–5 of 7");

        await page.Tb().Grid("Trails").GoToPageAsync(2);
        await Expect(Names(page)).ToHaveTextAsync(["Summit Gate", "Lake camp"]);
        await Expect(summary).ToHaveTextAsync("6–7 of 7");
        await page.Tb().Select("Rows per page").ChooseAsync("10");

        await Expect(Names(page)).ToHaveCountAsync(7);
        await Expect(summary).ToHaveTextAsync("1–7 of 7");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldSelectByBoxRowAndHeaderButNotByACellsButton_WhenEachIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var trails = page.Tb().Grid("Trails");
        var chosen = page.GetByTestId("chosen");

        await trails.SelectAsync("Cedar Flats");
        await (await trails.CellAsync("Fern Gully", "Region")).ClickAsync();
        await Expect(chosen).ToContainTextAsync("Chosen: Cedar Flats, Fern Gully.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Plan Pine Hollow loop" }).ClickAsync();
        await Expect(chosen).ToContainTextAsync("Planning Pine Hollow loop.");
        await Expect(chosen).ToContainTextAsync("Chosen: Cedar Flats, Fern Gully.");
        var all = page.GetByRole(AriaRole.Checkbox, new() { Name = "Select all rows" });
        Assert.True(await all.EvaluateAsync<bool>("box => box.indeterminate"));
        await all.CheckAsync();

        await Expect(page.Locator("tr[data-part=row][data-selected]")).ToHaveCountAsync(5);
        Assert.False(await all.EvaluateAsync<bool>("box => box.indeterminate"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldPressOnlyThePlannedTrailsButton_WhenAnotherTrailIsPlanned(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var pine = page.GetByRole(AriaRole.Button, new() { Name = "Plan Pine Hollow loop" });
        await pine.ClickAsync();
        await Expect(pine).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByRole(AriaRole.Button, new() { Name = "Plan Cedar Flats" }).ClickAsync();

        await Expect(page.GetByTestId("chosen")).ToContainTextAsync("Planning Cedar Flats.");
        await Expect(pine).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldLoadEachSortedPageFromTheProvider_WhenSortedPagedAndAClickedRowIsChosen(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var log = page.Tb().Grid("Hike log");
        var hours = Table(page, "Hike log").Locator("tbody td[data-column=Hours]");
        await Expect(hours).ToHaveCountAsync(4);

        // Read once, without retrying, so the helpers must have waited for the provider's rows.
        await log.SortAsync("Hours", descending: true);
        Assert.Equal(["7.0", "7.0", "7.0", "5.5"], await hours.AllTextContentsAsync());
        await log.GoToPageAsync(2);
        Assert.Equal("5.5", await hours.First.TextContentAsync());
        await (await log.CellAsync("Lake camp", "Hours")).ClickAsync();

        await Expect(log.Row("Lake camp")).ToHaveAttributeAsync("data-selected", "");
        await Expect(page.GetByTestId("hike")).ToContainTextAsync("chosen");
    }

    private static ILocator Table(IPage page, string name) =>
        page.GetByRole(AriaRole.Table, new() { Name = name, Exact = true });

    private static ILocator Names(IPage page) => Table(page, "Trails").Locator("tbody td[data-column=Name]");
}
