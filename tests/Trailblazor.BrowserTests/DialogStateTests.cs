using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

// Dialogs that change between modal and modeless, and change size, while they're open.
public class DialogStateTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLetThePageBeUsedUntilRestored_WhenAModalIsMinimized(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Editor", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Editor" });
        await Expect(dialog.GetByLabel("Name")).ToBeFocusedAsync();

        // Pressed with the keyboard, since WebKit doesn't focus a button that is clicked.
        await PressAsync(dialog, "Minimize");

        await Expect(dialog).ToHaveAttributeAsync("data-window-state", "minimized");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeFocusedAsync();
        Assert.False(await IsModalAsync(dialog));
        await page.GetByTestId("page-button").ClickAsync();
        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
        await PressAsync(dialog, "Restore");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Minimize" })).ToBeFocusedAsync();
        Assert.True(await IsModalAsync(dialog));
        await dialog.GetByLabel("Name").FillAsync("Pine Hollow");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(page.GetByTestId("result")).ToHaveTextAsync("editor saved");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldSwitchBetweenModalAndModelessAndKeepFocus_WhenItsContentAsks(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Editor", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Editor" });
        await Expect(dialog.GetByLabel("Name")).ToBeFocusedAsync();

        await PressAsync(dialog, "Pop out");

        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Pop in" })).ToBeFocusedAsync();
        Assert.False(await IsModalAsync(dialog));
        await page.GetByTestId("page-button").ClickAsync();
        await Expect(page.GetByTestId("page-clicks")).ToHaveTextAsync("1");
        await PressAsync(dialog, "Pop in");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Pop out" })).ToBeFocusedAsync();
        Assert.True(await IsModalAsync(dialog));
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("result")).ToHaveTextAsync("editor cancelled");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCloseTheModelessDialogWithFocus_WhenEscapeIsPressedInAnOlderOne(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Panel" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Panel" });
        await Expect(panel).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Docked" }).ClickAsync();
        var docked = page.GetByRole(AriaRole.Dialog, new() { Name = "Placed BottomEnd" });
        await Expect(docked).ToBeVisibleAsync();

        await panel.GetByLabel("Name").FocusAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("panel cancelled");
        await Expect(docked).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCloseOnlyTheInnerDialog_WhenEscapeIsPressedInOneWrittenInsideAModelessOne(
        string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Outer" }).ClickAsync();
        var outer = page.GetByRole(AriaRole.Dialog, new() { Name = "Outer" });
        await outer.GetByRole(AriaRole.Button, new() { Name = "Inner" }).ClickAsync();
        var inner = page.GetByRole(AriaRole.Dialog, new() { Name = "Inner" });

        await inner.GetByRole(AriaRole.Button, new() { Name = "Inside" }).FocusAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(inner).ToBeHiddenAsync();
        await Expect(outer).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldKeepEscapeFromClosingIt_WhenRestoredAboveADialogOpenedLater(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Locked editor" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Editor" });
        await PressAsync(dialog, "Minimize");
        await page.GetByRole(AriaRole.Button, new() { Name = "Panel" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Panel" })).ToBeVisibleAsync();
        await PressAsync(dialog, "Restore");

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");

        await Expect(dialog.GetByLabel("Name")).ToBeVisibleAsync();
        Assert.True(await IsModalAsync(dialog));
        await Expect(page.GetByTestId("result")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldShrink_WhenItsContentSetsItsSize(string browser)
    {
        var page = await OpenAsync(browser, "/dialogs");
        await page.GetByRole(AriaRole.Button, new() { Name = "Editor", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Editor" });
        var before = (await dialog.BoundingBoxAsync())!.Width;

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Shrink" }).ClickAsync();

        await Expect(dialog).ToHaveClassAsync(new Regex(@"\btb-dialog--small\b"));
        Assert.True((await dialog.BoundingBoxAsync())!.Width < before);
    }

    private static async Task PressAsync(ILocator dialog, string button)
    {
        await dialog.GetByRole(AriaRole.Button, new() { Name = button }).FocusAsync();
        await dialog.Page.Keyboard.PressAsync("Enter");
    }

    private static Task<bool> IsModalAsync(ILocator dialog) =>
        dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')");
}
