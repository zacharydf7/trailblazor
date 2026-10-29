using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Trailblazor.BrowserTests;

public class ToastTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Toast(IPage page, string message) =>
        page.Locator("[data-part=toast]").Filter(new() { HasText = message });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldAppearAnnouncedWithoutTakingFocusAndCloseByItself_WhenShown(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        var quick = page.GetByRole(AriaRole.Button, new() { Name = "Quick" });
        await quick.FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(Toast(page, "Quick note")).ToBeVisibleAsync();
        await Expect(page.Locator("[data-part=announcer] [role=status]")).ToHaveTextAsync("Quick note");
        await Expect(quick).ToBeFocusedAsync();
        await Expect(Toast(page, "Quick note")).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldStayOpen_WhenThePointerRestsOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Quick" }).ClickAsync();

        await Toast(page, "Quick note").HoverAsync();
        await page.WaitForTimeoutAsync(4000);
        await Expect(Toast(page, "Quick note")).ToBeVisibleAsync();

        await page.Mouse.MoveAsync(5, 300);
        await Expect(Toast(page, "Quick note")).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Action_ShouldRunAndCloseTheToast_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Toast(page, "Deleted").GetByRole(AriaRole.Button, new() { Name = "Undo" }).ClickAsync();

        await Expect(page.GetByTestId("result")).ToHaveTextAsync("undone");
        await Expect(Toast(page, "Deleted")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldTakeFocusOnF8AndGiveItBackOnEscape_WhenAToastIsOpen(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Fail" }).ClickAsync();
        await delete.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var undo = Toast(page, "Deleted").GetByRole(AriaRole.Button, new() { Name = "Undo" });
        await Expect(undo).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("F8");
        await Expect(undo).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(delete).ToBeFocusedAsync();
        await Expect(Toast(page, "Deleted")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldGiveFocusBack_WhenTheFocusedToastIsDismissed(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        var fail = page.GetByRole(AriaRole.Button, new() { Name = "Fail" });
        await fail.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var dismiss = Toast(page, "Upload failed").GetByRole(AriaRole.Button, new() { Name = "Dismiss" });
        await Expect(dismiss).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("F8");
        await Expect(dismiss).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Toast(page, "Upload failed")).ToHaveCountAsync(0);
        await Expect(fail).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldBeUsableAboveAModalDialog_WhenShownFromIt(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Dialog" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Toasting" });

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Copy" }).ClickAsync();

        var toast = Toast(page, "Copied");
        await Expect(toast).ToBeVisibleAsync();
        Assert.True(await toast.EvaluateAsync<bool>("""
            toast => {
                const box = toast.getBoundingClientRect();
                return toast.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2));
            }
            """), "The toast is covered by the dialog or can't be clicked.");
        await toast.GetByRole(AriaRole.Button, new() { Name = "Dismiss" }).ClickAsync();
        await Expect(toast).ToHaveCountAsync(0);
        await Expect(dialog).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldAnnounceAndGiveFocusBackInsideTheDialog_WhenShownFromAModal(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Dialog" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Toasting" });
        var copy = dialog.GetByRole(AriaRole.Button, new() { Name = "Copy" });
        await copy.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(dialog.Locator("[data-part=announcer] [role=status]")).ToHaveTextAsync("Copied");
        await Expect(Toast(page, "Copied")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("F8");
        await Expect(Toast(page, "Copied").GetByRole(AriaRole.Button, new() { Name = "Dismiss" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(copy).ToBeFocusedAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Toast(page, "Copied")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldHandFocusToTheDialog_WhenEscapeIsPressedOnAToastReachedWithoutF8(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Dialog" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Toasting" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Copy" }).ClickAsync();
        var dismiss = Toast(page, "Copied").GetByRole(AriaRole.Button, new() { Name = "Dismiss" });
        await dismiss.FocusAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(dialog).ToBeFocusedAsync();
        await Expect(Toast(page, "Copied")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldHaveNoAccessibilityViolations_WhenToastsAreOpenInEitherTheme(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fail" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(Toast(page, "Deleted")).ToBeVisibleAsync();

        foreach (var theme in new[] { "light", "dark" })
        {
            await SetThemeAsync(page, theme);
            var result = await page.RunAxe(new AxeRunOptions
            {
                RunOnly = new RunOnlyOptions
                {
                    Type = "tag",
                    Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"],
                },
            });

            Assert.Empty(result.Violations.Select(violation =>
                $"{theme} {violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
        }
    }
}
