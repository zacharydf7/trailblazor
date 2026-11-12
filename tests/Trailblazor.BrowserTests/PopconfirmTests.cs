using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class PopconfirmTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Trigger(IPage page) =>
        page.GetByRole(AriaRole.Button, new() { Name = "Delete trail", Exact = true });

    private static ILocator Question(IPage page) =>
        page.GetByRole(AriaRole.Dialog, new() { Name = "Delete Pine Hollow loop?" });

    private static ILocator Status(IPage page) => page.Locator("output").First;

    // The panel shows before its toggle event runs, and closing it before then merges both into one event, which no
    // real user is fast enough to cause; the trigger's aria-expanded says the toggle has run.
    private static async Task OpenedAsync(IPage page)
    {
        await Expect(Question(page)).ToBeVisibleAsync();
        await Expect(Trigger(page)).ToHaveAttributeAsync("aria-expanded", "true");
    }

    private async Task<IPage> OpenFocusedAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm");
        await Expect(Trigger(page)).ToHaveAttributeAsync("data-ready", "");
        await Trigger(page).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await OpenedAsync(page);
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldStartOnCancelAndConfirmWithTheKeyboard_WhenOpenedWithEnter(string browser)
    {
        var page = await OpenFocusedAsync(browser);
        await Expect(Question(page).GetByRole(AriaRole.Button, new() { Name = "Cancel" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Question(page).GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Status(page)).ToHaveTextAsync("Deleted.");
        await Expect(Question(page)).ToBeHiddenAsync();
        await Expect(Trigger(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldCancelAndGiveFocusBack_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenFocusedAsync(browser);

        await page.Keyboard.PressAsync("Escape");

        await Expect(Status(page)).ToHaveTextAsync("Kept it.");
        await Expect(Question(page)).ToBeHiddenAsync();
        await Expect(Trigger(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldCancel_WhenTabLeavesThePanel(string browser)
    {
        var page = await OpenFocusedAsync(browser);

        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");

        await Expect(Status(page)).ToHaveTextAsync("Kept it.");
        await Expect(Question(page)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldCancel_WhenAClickLandsElsewhere(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm");
        await Expect(Trigger(page)).ToHaveAttributeAsync("data-ready", "");
        await Trigger(page).ClickAsync();
        await OpenedAsync(page);

        await page.Mouse.ClickAsync(5, 5);

        await Expect(Status(page)).ToHaveTextAsync("Kept it.");
        await Expect(Question(page)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldStayOpenAndBusyUntilASlowConfirmFinishes_WhenConfirmed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm");
        var archive = page.GetByRole(AriaRole.Dialog, new() { Name = "Archive Cedar Flats?" });

        // The helper waits until OnConfirm finishes, so this presses the confirm button itself to see the loading state
        // while it runs.
        var panel = await page.Tb().Popover("Archive Cedar Flats").OpenAsync();
        await panel.Locator("[data-part=confirm]").ClickAsync();

        await Expect(archive.Locator("[data-part=confirm]")).ToHaveClassAsync(new Regex("tb-button--loading"));
        await Expect(archive).ToBeVisibleAsync();
        await Expect(page.GetByText("Archived.")).ToBeVisibleAsync();
        await Expect(archive).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Archive Cedar Flats" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldCancel_WhenCancelAsyncIsCalled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm");

        await page.Tb().Popconfirm("Delete trail").CancelAsync();

        await Expect(Status(page)).ToHaveTextAsync("Kept it.");
    }
}
