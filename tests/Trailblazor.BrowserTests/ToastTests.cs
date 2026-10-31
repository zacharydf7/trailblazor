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
    public async Task Toast_ShouldFadeOutOfReachBeforeItGoes_WhenClosed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toast");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save report" }).ClickAsync();
        var toast = Toast(page, "Report saved");
        await Expect(toast).ToHaveCSSAsync("opacity", "1");

        await toast.GetByRole(AriaRole.Button, new() { Name = "Dismiss" }).ClickAsync();

        await Expect(toast).ToHaveAttributeAsync("data-leaving", "");
        await Expect(toast).ToHaveAttributeAsync("inert", "");
        await Expect(toast).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldAppearAndGoWithoutFading_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toast", new() { ReducedMotion = ReducedMotion.Reduce });

        await page.GetByRole(AriaRole.Button, new() { Name = "Save report" }).ClickAsync();
        await Expect(Toast(page, "Report saved")).ToHaveCSSAsync("transition-duration", "0s, 0s");
        await WatchLeavingAsync(page, "getComputedStyle(leaving).display");
        await Toast(page, "Report saved").GetByRole(AriaRole.Button, new() { Name = "Dismiss" }).ClickAsync();

        Assert.Equal("none", await page.EvaluateAsync<string>("() => window.leaving"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldShowItsTitleIconAndContentAndSayWhyItClosed_WhenShownWithThem(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toast");

        await page.GetByRole(AriaRole.Button, new() { Name = "Fail to sync" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Export map" }).ClickAsync();
        var failed = Toast(page, "Check your connection");
        await Expect(failed.Locator("[data-part=title]")).ToHaveTextAsync("Couldn't reach the server");
        await Expect(failed.Locator(".tb-toast__icon")).ToBeVisibleAsync();
        await Expect(Toast(page, "Trail map exported").GetByRole(AriaRole.Link, new() { Name = "Open it" }))
            .ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Dismiss all" }).ClickAsync();

        await Expect(page.GetByText("The export toast closed (App).")).ToBeVisibleAsync();
        await Expect(page.Locator("[data-part=toast]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldStayOpen_WhenThePointerRestsOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.GetByRole(AriaRole.Button, new() { Name = "Quick" }).ClickAsync();
        await Expect(Toast(page, "Quick note")).ToBeVisibleAsync();

        // Force skips the wait for the toast to hold still, which on a slow runner can outlast it.
        await Toast(page, "Quick note").HoverAsync(new() { Force = true });
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
        await WatchLeavingAsync(page, "document.activeElement?.textContent.trim()", delay: 100);
        await page.Keyboard.PressAsync("Enter");

        Assert.Equal("Fail", await page.EvaluateAsync<string>("() => window.leaving"));
        await Expect(Toast(page, "Upload failed")).ToHaveCountAsync(0);
        await Expect(fail).ToBeFocusedAsync();
    }

    // Records the expression, in milliseconds after a toast starts fading out, into window.leaving; the fade lasts 200.
    private static Task WatchLeavingAsync(IPage page, string expression, int delay = 0) => page.EvaluateAsync($$"""
        () => {
            window.leaving = new Promise(resolve => {
                setTimeout(() => resolve('no toast left'), 5000);
                new MutationObserver((_, observer) => {
                    const leaving = document.querySelector('[data-leaving]');
                    if (leaving) {
                        observer.disconnect();
                        setTimeout(() => resolve({{expression}}), {{delay}});
                    }
                }).observe(document.body, { subtree: true, attributes: true, attributeFilter: ['data-leaving'] });
            });
        }
        """);

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
    public async Task Announcer_ShouldOpenTheDialogWithAnEmptyRegion_WhenAMessageComesAsItOpens(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        // Records each dialog's polite region in the frame after it opens, when it joins the accessibility tree.
        await page.EvaluateAsync("""
            () => {
                window.politeAtOpen = [];
                new MutationObserver(records => records
                    .filter(record => record.target.open)
                    .forEach(record => requestAnimationFrame(() => window.politeAtOpen.push(
                        record.target.querySelector('[data-part=announcer] [role=status]')?.textContent ?? null))))
                    .observe(document.body, { attributeFilter: ['open'], subtree: true });
            }
            """);

        await page.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Toasting" });
        await Expect(dialog.Locator("[data-part=announcer] [role=status]")).ToHaveTextAsync("Draft restored");
        Assert.Equal([""], await page.EvaluateAsync<string?[]>("() => window.politeAtOpen"));
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
