namespace Trailblazor.BrowserTests;

// Dialogs kept out of the browser's top layer, so popups other libraries add to the page's body open above them.
public class DialogLayerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static Task<bool> IsInertAsync(ILocator element) =>
        element.EvaluateAsync<bool>("element => element.closest('[inert]') !== null");

    private static Task<string> RootOverflowAsync(IPage page) =>
        page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).overflow");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLetAnotherLibrarysPopupAboveItBeUsed_WhenItIsOnThePageLayer(string browser)
    {
        var page = await OpenAsync(browser, "/foreign-popups");
        await Button(page, "Note").ClickAsync();
        var note = page.GetByRole(AriaRole.Dialog, new() { Name = "Note" });

        await note.GetByRole(AriaRole.Button, new() { Name = "Pick a color" }).ClickAsync();
        await Button(page, "Red").FocusAsync();
        await page.Keyboard.PressAsync("Escape");
        await Button(page, "Red").ClickAsync();

        await Expect(note.GetByTestId("color")).ToHaveTextAsync("Red");
        // A note that Escape had dismissed would be fading out, inert, and couldn't open its details.
        await note.GetByRole(AriaRole.Button, new() { Name = "Details" }).ClickAsync();
        var details = page.GetByRole(AriaRole.Dialog, new() { Name = "Details" });
        await Expect(details.GetByLabel("Name")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLeaveAPopupMadeBeforeItAndBlazorsErrorBarUsable_WhenItIsOnThePageLayer(
        string browser)
    {
        var page = await OpenAsync(browser, "/foreign-popups");
        await Expect(page.Locator(".reused-popup")).ToHaveCountAsync(1);
        await Button(page, "Note").ClickAsync();
        var note = page.GetByRole(AriaRole.Dialog, new() { Name = "Note" });

        await note.GetByRole(AriaRole.Button, new() { Name = "Pick a size" }).ClickAsync();
        await Button(page, "Large").ClickAsync();

        await Expect(note.GetByTestId("size")).ToHaveTextAsync("Large");
        Assert.False(await IsInertAsync(page.Locator("#blazor-error-ui")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldKeepThePageInertAndCloseOnEscapeOrTheBackdrop_WhenItIsOnThePageLayer(string browser)
    {
        var page = await OpenAsync(browser, "/foreign-popups");
        var note = page.GetByRole(AriaRole.Dialog, new() { Name = "Note" });
        var pageButton = page.GetByTestId("page-button");

        await Button(page, "Note").ClickAsync();
        await Expect(note).ToHaveAttributeAsync("aria-modal", "true");
        Assert.True(await IsInertAsync(pageButton));
        Assert.Equal("hidden", await RootOverflowAsync(page));
        await page.Keyboard.PressAsync("Escape");
        await Expect(note).ToHaveCountAsync(0);
        await Expect(Button(page, "Note")).ToBeFocusedAsync();
        await Button(page, "Note").ClickAsync();
        await Expect(note).ToBeVisibleAsync();
        await page.Mouse.ClickAsync(5, 5);

        await Expect(note).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(pageButton));
        Assert.Equal("visible", await RootOverflowAsync(page));
        await pageButton.ClickAsync();
        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLetThePageBeUsedUntilItIsRestored_WhenItIsMinimizedOnThePageLayer(string browser)
    {
        var page = await OpenAsync(browser, "/foreign-popups");
        var note = page.GetByRole(AriaRole.Dialog, new() { Name = "Note" });
        var pageButton = page.GetByTestId("page-button");
        await Button(page, "Note").ClickAsync();

        await note.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await Expect(note).Not.ToHaveAttributeAsync("aria-modal", "true");
        Assert.False(await IsInertAsync(pageButton));
        Assert.Equal("visible", await RootOverflowAsync(page));
        await pageButton.ClickAsync();
        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
        await note.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();

        await Expect(note).ToHaveAttributeAsync("aria-modal", "true");
        Assert.True(await IsInertAsync(pageButton));
        Assert.Equal("hidden", await RootOverflowAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCloseTheNewestFirstAndKeepThePageInert_WhenOneOnThePageLayerOpensAnother(
        string browser)
    {
        var page = await OpenAsync(browser, "/foreign-popups");
        var note = page.GetByRole(AriaRole.Dialog, new() { Name = "Note" });
        var details = page.GetByRole(AriaRole.Dialog, new() { Name = "Details" });
        await Button(page, "Note").ClickAsync();

        await note.GetByRole(AriaRole.Button, new() { Name = "Details" }).ClickAsync();
        await Expect(details.GetByLabel("Name")).ToBeFocusedAsync();
        Assert.True(await IsInertAsync(note));
        await page.Keyboard.PressAsync("Escape");

        await Expect(details).ToHaveCountAsync(0);
        await Expect(note).ToBeVisibleAsync();
        Assert.False(await IsInertAsync(note));
        Assert.True(await IsInertAsync(page.GetByTestId("page-button")));
        await page.Keyboard.PressAsync("Escape");
        await Expect(note).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(page.GetByTestId("page-button")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenInTheTopLayer_WhenAModalInTheTopLayerOpensOneMeantForThePage(string browser)
    {
        var page = await OpenAsync(browser, "/foreign-popups");
        await Button(page, "Top note").ClickAsync();
        var details = page.GetByRole(AriaRole.Dialog, new() { Name = "Details" });

        await page.GetByRole(AriaRole.Dialog, new() { Name = "Top note" })
            .GetByRole(AriaRole.Button, new() { Name = "Details" }).ClickAsync();
        await details.GetByLabel("Name").FillAsync("Ridge");

        Assert.True(await details.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await details.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(details).ToHaveCountAsync(0);
    }
}
