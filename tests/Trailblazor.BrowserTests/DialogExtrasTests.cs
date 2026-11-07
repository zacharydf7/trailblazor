using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class DialogExtrasTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenTheFormChosenFromAMenuByItsType_WhenAnItemIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open a form" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename report" }).ClickAsync();

        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename report" });
        await Expect(rename.GetByLabel("Name")).ToHaveValueAsync("Quarterly report");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldAskBeforeDiscardingTheNewName_WhenEscapeTheBackdropCancelOrTheCloseButtonLeaveIt(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename report" }).ClickAsync();
        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename report" });
        await rename.GetByLabel("Name").PressSequentiallyAsync(" draft");
        var discard = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Discard the new name?" });

        await page.Keyboard.PressAsync("Escape");
        await discard.GetByRole(AriaRole.Button, new() { Name = "Keep editing" }).ClickAsync();
        await Expect(discard).ToHaveCountAsync(0);
        await page.Mouse.ClickAsync(5, 5);
        await discard.GetByRole(AriaRole.Button, new() { Name = "Keep editing" }).ClickAsync();
        await Expect(discard).ToHaveCountAsync(0);
        await rename.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await discard.GetByRole(AriaRole.Button, new() { Name = "Keep editing" }).ClickAsync();
        await Expect(discard).ToHaveCountAsync(0);
        await Expect(rename.GetByLabel("Name")).ToHaveValueAsync("Quarterly report draft");
        await rename.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await discard.GetByRole(AriaRole.Button, new() { Name = "Discard" }).ClickAsync();

        await Expect(rename).ToHaveCountAsync(0);
        await Expect(page.GetByText("Renamed to")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldCloseWithoutAsking_WhenTheNameIsUnchanged(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename report" }).ClickAsync();
        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename report" });
        await Expect(rename.GetByLabel("Name")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(rename).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Alertdialog)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldShowProgressThenCloseAndAlert_WhenThePageUpdatesAndClosesItsReference(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var export = page.GetByRole(AriaRole.Button, new() { Name = "Export report" });

        await export.ClickAsync();
        var exporting = page.GetByRole(AriaRole.Dialog, new() { Name = "Exporting report", Exact = true });
        await Expect(exporting.Locator("[data-part=header] h2 svg")).ToBeVisibleAsync();
        await Expect(exporting.GetByRole(AriaRole.Progressbar))
            .ToHaveAttributeAsync("aria-valuenow", new Regex("^[1-9]"));

        var alert = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Report exported" });
        await Expect(alert).ToHaveAccessibleDescriptionAsync("The PDF is in your downloads.");
        await Expect(exporting).ToHaveCountAsync(0);
        await alert.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
        await Expect(page.GetByText("Exported", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(export).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStopTheExport_WhenItsStopButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");

        await page.GetByRole(AriaRole.Button, new() { Name = "Export report" }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog, new() { Name = "Exporting report" })
            .GetByRole(AriaRole.Button, new() { Name = "Stop" }).ClickAsync();

        await Expect(page.GetByText("Export stopped")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Alertdialog)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenFromMarkupDescribedAndCloseThroughItsBinding_WhenShared(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var share = page.GetByRole(AriaRole.Button, new() { Name = "Share" });
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Share report" });

        await share.ClickAsync();
        await Expect(dialog).ToHaveAccessibleDescriptionAsync("Anyone with the link can view Quarterly report.");
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(share).ToBeFocusedAsync();
        await share.ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();

        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(share).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStartOnTheSafeChoice_WhenAButtonIsMarkedAutofocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");

        await page.GetByRole(AriaRole.Button, new() { Name = "Start at the safe choice" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Discard the draft?" });
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Keep the draft" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStartOnItsFirstButton_WhenConfirming(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");

        await page.GetByRole(AriaRole.Button, new() { Name = "Delete report" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete report?" });
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldScrollItsBodyAndKeepItsHeaderInView_WhenItsContentIsLong(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        await page.GetByRole(AriaRole.Button, new() { Name = "Read the trail rules" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail rules" });
        var close = dialog.GetByRole(AriaRole.Button, new() { Name = "Close" });
        var done = dialog.GetByRole(AriaRole.Button, new() { Name = "Done" });
        await Expect(close).ToBeInViewportAsync();
        await Expect(done).Not.ToBeInViewportAsync();

        await done.ScrollIntoViewIfNeededAsync();

        await Expect(done).ToBeInViewportAsync();
        await Expect(close).ToBeInViewportAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayOpenWithItsErrorThenClose_WhenSavingFailsThenSucceeds(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        await page.GetByRole(AriaRole.Button, new() { Name = "Book the hut" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Book the hut" });
        var book = dialog.GetByRole(AriaRole.Button, new() { Name = "Book", Exact = true });

        await book.ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Try again" })).ToBeVisibleAsync();
        await Expect(dialog.GetByLabel("Guest name")).ToHaveValueAsync("Ada Lovelace");
        await book.ClickAsync();

        await Expect(dialog).ToBeHiddenAsync();
        await Expect(page.GetByText("Booked for Ada Lovelace")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldAskFirstOnlyAfterSomethingIsWritten_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var open = page.GetByRole(AriaRole.Button, new() { Name = "Ask before closing" });
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Write a trail note" });

        await open.ClickAsync();
        await dialog.GetByLabel("Trail note").FocusAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();

        await open.ClickAsync();
        await dialog.GetByLabel("Trail note").PressSequentiallyAsync("Muddy");
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Alertdialog, new() { Name = "Discard the note?" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldFadeOutWhileStillOpen_WhenItCloses(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        await page.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Share report" });
        await Expect(dialog).ToHaveCSSAsync("opacity", "1");
        await WatchLeavingAsync(page);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();

        Assert.True(await page.EvaluateAsync<bool>("() => window.leaving"));
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenAndCloseWithoutFading_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog", new() { ReducedMotion = ReducedMotion.Reduce });

        await page.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Share report" });
        await Expect(dialog).ToHaveCSSAsync("transition-duration", "0s");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldFocusTheMarkedFieldKeepTabInsideAndGiveFocusBack_WhenOpenedAndClosed(
        string browser)
    {
        var page = await OpenAsync(browser, "/tb-focus-scope");
        var close = page.GetByRole(AriaRole.Button, new() { Name = "Close" });

        await page.GetByTestId("opener").ClickAsync();
        await Expect(page.GetByLabel("Second")).ToBeFocusedAsync();
        await close.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByLabel("First")).ToBeFocusedAsync();
        await close.ClickAsync();

        await Expect(page.GetByTestId("opener")).ToBeFocusedAsync();
    }

    // Records into window.leaving whether the dialog was still open when it began to fade out.
    private static Task WatchLeavingAsync(IPage page) => page.EvaluateAsync("""
        () => {
            window.leaving = new Promise(resolve => {
                setTimeout(() => resolve('never faded'), 5000);
                new MutationObserver((_, observer) => {
                    const leaving = document.querySelector('dialog[data-leaving]');
                    if (leaving) {
                        observer.disconnect();
                        resolve(leaving.open && getComputedStyle(leaving).transitionDuration !== '0s');
                    }
                }).observe(document.body, { subtree: true, attributes: true, attributeFilter: ['data-leaving'] });
            });
        }
        """);
}
