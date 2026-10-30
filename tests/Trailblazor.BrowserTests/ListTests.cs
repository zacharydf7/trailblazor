using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class ListTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Campsites(IPage page) => page.GetByRole(AriaRole.List, new() { Name = "Campsites" });

    // An item's button is named by its text and then its description.
    private static ILocator Site(IPage page, string name) =>
        Campsites(page).GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^{name}") });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldMarkTheChosenItemCurrent_WhenItIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");
        var pine = Site(page, "Pine Hollow");
        var eagle = Site(page, "Eagle Ridge");

        await eagle.ClickAsync();

        await Expect(eagle).ToHaveAttributeAsync("aria-current", "true");
        await Expect(pine).Not.ToHaveAttributeAsync("aria-current", "true");
        await Expect(page.GetByText("Showing: Eagle Ridge")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldNameEachItemByItsTextThenItsDescription_WhenItHasOne(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");

        await Expect(Site(page, "Pine Hollow")).ToHaveAccessibleNameAsync("Pine Hollow 4 sites, water on site");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldReachEachItemThenItsOwnButton_WhenTabbing(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");
        await Site(page, "Pine Hollow").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Remove Pine Hollow" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Site(page, "Eagle Ridge")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldRunTheItemsOwnButtonAlone_WhenItIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");

        await page.GetByRole(AriaRole.Button, new() { Name = "Remove Eagle Ridge" }).ClickAsync();

        await Expect(Campsites(page).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Expect(page.GetByText("Showing: Pine Hollow")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldSkipTheItem_WhenItIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");

        await Expect(Site(page, "Cedar Flats")).ToBeDisabledAsync();
    }
}
