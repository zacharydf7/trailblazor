using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerPopOutDialogTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Asking(IPage page) =>
        page.GetByRole(AriaRole.Alertdialog, new() { Name = "Remove the file?" });

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static Task<bool> IsInertAsync(ILocator element) =>
        element.EvaluateAsync<bool>("element => element.closest('[inert]') !== null");

    // Waits until the focus is in the dialog, on it or on something it holds.
    private static async Task FocusInsideAsync(ILocator dialog) => await dialog.Page.WaitForFunctionAsync(
        "dialog => dialog.contains(document.activeElement)", await dialog.ElementHandleAsync());

    // What covers a window while a modal is open in another of the app's windows.
    private static ILocator Scrim(IPage page) => page.Locator("[data-part=scrim]");

    private static ILocator Settings(IPage page) => page.GetByRole(AriaRole.Dialog, new() { Name = "File settings" });

    // How far along its window's row of minimized dialogs a dialog sits, and how many rows up.
    private static Task<string> PlaceInTheRowAsync(ILocator dialog) => dialog.EvaluateAsync<string>("""
        dialog => ['--tb-dialog-tray-inline', '--tb-dialog-tray-block']
            .map(name => dialog.style.getPropertyValue(name)).join(' ')
        """);

    // The marks that dialogs which opened in another window have left on the page.
    private static Task<int> MarksAsync(IPage page) => page.EvaluateAsync<int>("""
        () => {
            const comments = document.createTreeWalker(document.body, NodeFilter.SHOW_COMMENT);
            let marks = 0;
            while (comments.nextNode()) {
                marks += comments.currentNode.data === '' ? 1 : 0;
            }

            return marks;
        }
        """);

    private async Task<IPage> OpenDeskAsync(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-dialogs");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Desk" })).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // Opens the page with its Files panel in a window of its own.
    private async Task<(IPage Page, IPage Window)> OpenFilesInAWindowAsync(string browser)
    {
        var page = await OpenDeskAsync(browser);
        return (page, (await page.Tb().Dock("Desk").PopOutAsync("Files")).Page);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenInThePanelsWindowAndGiveTheFocusBackThere_WhenAskedForFromThePanel(
        string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        var ask = Button(window, "Ask");
        var marks = await MarksAsync(page);
        // WebKit doesn't focus a button that is clicked, so the keyboard presses it.
        await ask.FocusAsync();

        await window.Keyboard.PressAsync("Enter");

        await Expect(Asking(window)).ToBeVisibleAsync();
        await Expect(Asking(page)).ToHaveCountAsync(0);
        Assert.Equal(marks + 1, await MarksAsync(page));
        Assert.True(await Asking(window).EvaluateAsync<bool>(
            "dialog => dialog.matches(':modal') && dialog.contains(dialog.ownerDocument.activeElement)"));
        await window.Keyboard.PressAsync("Escape");

        await Expect(Asking(window)).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Kept");
        await FocusedAsync(ask);
        Assert.Equal(marks, await MarksAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenInThePanelsWindow_WhenItIsWrittenInsideThePanel(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        // A window that is covered or minimized draws no frames, and the page's own may be.
        await page.EvaluateAsync("() => window.requestAnimationFrame = () => 0");

        await Button(window, "Settings").ClickAsync();

        var settings = window.GetByRole(AriaRole.Dialog, new() { Name = "File settings" });
        await Expect(settings).ToBeVisibleAsync();
        await FocusedAsync(settings.GetByRole(AriaRole.Button, new() { Name = "Save settings" }));
        await settings.GetByRole(AriaRole.Button, new() { Name = "Save settings" }).ClickAsync();
        await Expect(settings.Locator("[data-part=toast]")).ToHaveTextAsync("Settings saved");
        // Its live regions come with the window's next frame, not the page's.
        await Expect(settings.Locator("[data-part=announcer] [role=status]")).ToHaveTextAsync("Settings saved");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldStayInItsDialog_WhenTheModalComesToAnotherWindow(string browser)
    {
        var page = await OpenDeskAsync(browser);
        var window = (await page.Tb().Dock("Desk").PopOutAsync("Other")).Page;
        await Button(page, "Settings").ClickAsync();
        await Settings(page).GetByRole(AriaRole.Button, new() { Name = "Save settings" }).ClickAsync();
        await Expect(Settings(page).Locator("[data-part=toast]")).ToHaveTextAsync("Settings saved");

        await window.Mouse.ClickAsync(100, 100);

        await Expect(Settings(window).Locator("[data-part=toast]")).ToHaveTextAsync("Settings saved");
        await Expect(window.Locator("[data-part=toast]")).ToHaveCountAsync(1);
        await Expect(page.Locator("[data-part=toast]")).ToHaveCountAsync(0);
        // F8 on the page brings the modal back with the focus where it starts, not on its toast.
        await page.Keyboard.PressAsync("F8");
        await FocusedAsync(Settings(page).GetByRole(AriaRole.Button, new() { Name = "Save settings" }));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldBlockTheAppsOtherWindows_WhenItIsOpenInOne(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await page.EvaluateAsync(
            "document.body.insertAdjacentHTML('beforeend', '<div id=blazor-error-ui hidden></div>')");

        await Button(window, "Ask").ClickAsync();

        await Expect(Asking(window)).ToBeVisibleAsync();
        await Expect(Scrim(page)).ToBeVisibleAsync();
        Assert.True(await IsInertAsync(Button(page, "Ask on the page")));
        Assert.False(await IsInertAsync(page.Locator("#blazor-error-ui")));
        await Expect(Scrim(window)).ToHaveCountAsync(0);
        await window.Keyboard.PressAsync("Escape");
        await Expect(Asking(window)).ToHaveCountAsync(0);
        await Expect(Scrim(page)).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(Button(page, "Ask on the page")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldComeToTheWindowTheUserWorksIn_WhenAPressOrAKeyIsInAnotherOne(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        var ask = Button(window, "Ask");
        var marks = await MarksAsync(page);
        await ask.FocusAsync();
        await window.Keyboard.PressAsync("Enter");
        await Expect(Asking(window)).ToBeVisibleAsync();

        // A press on the blocked page brings the dialog back to its mark there, and does nothing else there. The
        // toaster's mark goes too, since the toasts come with the user.
        await page.Mouse.ClickAsync(400, 300);

        await Expect(Asking(page)).ToBeVisibleAsync();
        Assert.Equal(marks - 1, await MarksAsync(page));
        Assert.True(await Asking(page).EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await FocusInsideAsync(Asking(page));
        await Expect(Asking(window)).ToHaveCountAsync(0);
        await Expect(Scrim(window)).ToBeVisibleAsync();
        await Expect(Scrim(page)).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("");

        // A key in the window brings it back with the focus where it starts, and its opener gets the focus when it
        // is answered there.
        await window.Keyboard.PressAsync("Shift");
        await Expect(Asking(window)).ToBeVisibleAsync();
        await FocusedAsync(Asking(window).GetByRole(AriaRole.Button, new() { Name = "Cancel" }));
        await Expect(Asking(page)).ToHaveCountAsync(0);
        Assert.Equal(marks + 1, await MarksAsync(page));
        await Asking(window).GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Removed");
        await FocusedAsync(ask);
        await Expect(Scrim(window)).ToHaveCountAsync(0);
        await Expect(Scrim(page)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Key_ShouldOnlyBringTheModal_WhenItIsPressedInABlockedWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Ask").ClickAsync();
        await Expect(Asking(window)).ToBeVisibleAsync();

        // Escape doesn't dismiss the dialog it brings, and Enter doesn't press the button the focus lands on.
        await page.Keyboard.PressAsync("Escape");
        await Expect(Asking(page)).ToBeVisibleAsync();
        await FocusedAsync(Asking(page).GetByRole(AriaRole.Button, new() { Name = "Cancel" }));
        await window.Keyboard.PressAsync("Enter");
        await Expect(Asking(window)).ToBeVisibleAsync();
        await FocusedAsync(Asking(window).GetByRole(AriaRole.Button, new() { Name = "Cancel" }));

        // It is still there to answer; either key would have answered it with Kept.
        await Asking(window).GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Removed");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldComeToTheUserWithTheDialogItIsWrittenIn_WhenItOpensWhileTheyWorkElsewhere(
        string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Notes").ClickAsync();
        await Expect(window.GetByRole(AriaRole.Dialog, new() { Name = "File notes" })).ToBeVisibleAsync();
        await page.Mouse.ClickAsync(400, 300);

        // The page's hotkey opens a modal written inside the modeless dialog in the panel's window.
        await page.Keyboard.PressAsync("Alt+J");

        var discard = page.GetByRole(AriaRole.Dialog, new() { Name = "File notes" })
            .GetByRole(AriaRole.Dialog, new() { Name = "Discard?" });
        await Expect(discard).ToBeVisibleAsync();
        Assert.True(await discard.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await FocusInsideAsync(discard);
        await Expect(Scrim(page)).ToHaveCountAsync(0);
        await Expect(Scrim(window)).ToBeVisibleAsync();
        // What the dialog it came with holds watches its size on the page now.
        await page.Keyboard.PressAsync("Escape");
        await Expect(discard).ToHaveCountAsync(0);
        var notes = page.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        var more = notes.GetByRole(AriaRole.Button, new() { Name = "More" });
        await Expect(more).ToBeHiddenAsync();
        await notes.GetByRole(AriaRole.Toolbar).EvaluateAsync("toolbar => toolbar.style.inlineSize = '4rem'");
        await Expect(more).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldLetTheOtherWindowsBeUsed_WhenItIsMinimized(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Settings").ClickAsync();
        await Expect(Scrim(page)).ToBeVisibleAsync();

        await Settings(window).GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();

        await Expect(Settings(window)).ToHaveAttributeAsync("data-window-state", "minimized");
        await Expect(Scrim(page)).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(Button(page, "Ask on the page")));
        await Settings(window).GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();
        await Expect(Scrim(page)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldComeToTheUser_WhenTheModalInFrontOfItClosesInAnotherWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Settings").ClickAsync();
        await Settings(window).GetByRole(AriaRole.Button, new() { Name = "Ask again" }).ClickAsync();
        await Expect(Asking(window)).ToBeVisibleAsync();
        // The one in front comes to the page, and the one behind it stays where it is.
        await page.Mouse.ClickAsync(400, 300);
        await Expect(Asking(page)).ToBeVisibleAsync();
        await Expect(Settings(window)).ToHaveCountAsync(1);

        await page.Keyboard.PressAsync("Escape");

        await Expect(Asking(page)).ToHaveCountAsync(0);
        await Expect(Settings(page)).ToBeVisibleAsync();
        await Expect(Settings(window)).ToHaveCountAsync(0);
        await Expect(Scrim(window)).ToBeVisibleAsync();
        // What it holds goes on watching its size there: a toolbar that narrows puts its buttons in its menu.
        var more = Settings(page).GetByRole(AriaRole.Button, new() { Name = "More" });
        await Expect(more).ToBeHiddenAsync();
        await Settings(page).GetByRole(AriaRole.Toolbar).EvaluateAsync("toolbar => toolbar.style.inlineSize = '4rem'");
        await Expect(more).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(Settings(page)).ToHaveCountAsync(0);
        await Expect(Scrim(window)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldBringTheDialogItIsWrittenIn_WhenItComesToAnotherWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Settings").ClickAsync();
        await Settings(window).GetByRole(AriaRole.Button, new() { Name = "Check" }).ClickAsync();
        await Expect(window.GetByRole(AriaRole.Dialog, new() { Name = "Sure?" })).ToBeVisibleAsync();

        await page.Mouse.ClickAsync(400, 300);

        var sure = Settings(page).GetByRole(AriaRole.Dialog, new() { Name = "Sure?" });
        await Expect(sure).ToBeVisibleAsync();
        Assert.True(await sure.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await FocusInsideAsync(sure);
        await Expect(window.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        // The dialog it came with is usable there once the modal inside it closes.
        await page.Keyboard.PressAsync("Escape");
        await Expect(sure).ToHaveCountAsync(0);
        await Settings(page).GetByRole(AriaRole.Button, new() { Name = "Save settings" }).ClickAsync();
        await Expect(Settings(page).Locator("[data-part=toast]")).ToHaveTextAsync("Settings saved");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenOnThePage_WhenAskedForFromThePageWhileAPanelIsInAWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);

        await Button(page, "Ask on the page").ClickAsync();

        await Expect(Asking(page)).ToBeVisibleAsync();
        await Expect(Asking(window)).ToHaveCountAsync(0);
        await Expect(Scrim(window)).ToBeVisibleAsync();
        Assert.True(await IsInertAsync(Button(window, "Ask")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldCloseOnlyTheDialogInItsWindow_WhenModelessDialogsAreOpenInTwoWindows(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(page, "Log").ClickAsync();
        var log = page.GetByRole(AriaRole.Dialog, new() { Name = "Page log" });
        await FocusInsideAsync(log);
        await Button(window, "Notes").ClickAsync();
        var notes = window.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        await FocusedAsync(notes.GetByLabel("Note"));

        await window.Keyboard.PressAsync("Escape");

        await Expect(notes).ToHaveCountAsync(0);
        // The page's dialog, which opened first and holds the page's focus, is still there to close.
        await log.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Expect(log).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldComeBackToThePageStillOpen_WhenTheWindowItShowsInCloses(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Ask").ClickAsync();
        await Expect(Asking(window)).ToBeVisibleAsync();

        await window.CloseAsync();

        await Expect(Asking(page)).ToBeVisibleAsync();
        Assert.True(await Asking(page).EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await FocusedAsync(Asking(page).GetByRole(AriaRole.Button, new() { Name = "Cancel" }));
        await Asking(page).GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Removed");
        await Expect(Asking(page)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldMakeThePageInert_WhenItComesBackFromAClosedWindowOnThePageLayer(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Rules").ClickAsync();
        var rules = window.GetByRole(AriaRole.Dialog, new() { Name = "File rules" });
        await Expect(rules).ToHaveAttributeAsync("aria-modal", "true");
        Assert.True(await IsInertAsync(Button(window, "Ask")));
        await Expect(Scrim(page)).ToBeVisibleAsync();

        await window.CloseAsync();

        rules = page.GetByRole(AriaRole.Dialog, new() { Name = "File rules" });
        await Expect(rules).ToBeVisibleAsync();
        await Expect(Scrim(page)).ToHaveCountAsync(0);
        Assert.True(await IsInertAsync(Button(page, "Ask")));
        Assert.True(await IsInertAsync(Button(page, "Ask on the page")));
        await page.Keyboard.PressAsync("Escape");
        await Expect(rules).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(Button(page, "Ask on the page")));
        Assert.False(await IsInertAsync(Button(page, "Ask")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldNotFire_WhenItIsPressedInAnotherWindowThanTheOneAModalIsIn(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Settings").ClickAsync();
        await Expect(Settings(window)).ToBeVisibleAsync();

        // The key only brings the modal to the page, where it stops the page's hotkey from then on.
        await page.Keyboard.PressAsync("Alt+K");

        await Expect(Settings(page)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(Settings(page)).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Alt+K");
        await Expect(page.GetByTestId("marked")).ToHaveTextAsync("1");
        // Closed on the page, the modal didn't send the focus back to its opener in the other window.
        Assert.Equal("body", await window.EvaluateAsync<string>("document.activeElement.localName"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldLeaveTheFocusWhereItIs_WhenAModelessOneComesBackFromAClosedWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Name").ClickAsync();
        await Expect(window.GetByRole(AriaRole.Dialog, new() { Name = "File name" })).ToBeVisibleAsync();
        var ask = Button(page, "Ask on the page");
        await ask.FocusAsync();

        await window.CloseAsync();

        // The dialog arrives on a page that is drawn, where showing it again would give it the focus.
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "File name" })).ToBeVisibleAsync();
        Assert.Equal("Ask on the page", await page.EvaluateAsync<string>(
            "() => document.activeElement.textContent.trim().slice(0, 40) || document.activeElement.localName"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldTakeTheFocusAgain_WhenItComesBackToThePageWithItsPanel(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Settings").ClickAsync();
        await Expect(window.GetByRole(AriaRole.Dialog, new() { Name = "File settings" })).ToBeVisibleAsync();

        await window.CloseAsync();

        var settings = page.GetByRole(AriaRole.Dialog, new() { Name = "File settings" });
        await Expect(settings).ToBeVisibleAsync();
        await FocusedAsync(settings.GetByRole(AriaRole.Button, new() { Name = "Save settings" }));
        // That happens once: a later change of its size leaves the focus where the user put it.
        var close = settings.GetByRole(AriaRole.Button, new() { Name = "Close" });
        await close.FocusAsync();
        await settings.EvaluateAsync("""
            dialog => {
                dialog.style.inlineSize = '20rem';
                return new Promise(drawn => requestAnimationFrame(() => requestAnimationFrame(drawn)));
            }
            """);
        await FocusedAsync(close);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldGetItsLiveRegionsOnThePage_WhenItsWindowClosesBeforeItDrawsAFrame(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        // A covered window draws no frames. Playwright's own waits need them, so a script presses the button.
        await window.EvaluateAsync("""
            () => {
                window.requestAnimationFrame = () => 0;
                const named = button => button.textContent.trim() === 'Settings';
                [...document.querySelectorAll('button')].find(named).click();
            }
            """);
        await window.WaitForFunctionAsync("() => document.querySelector('dialog[open]')", null,
            new() { PollingInterval = 50 });

        await window.CloseAsync();

        var settings = page.GetByRole(AriaRole.Dialog, new() { Name = "File settings" });
        await settings.GetByRole(AriaRole.Button, new() { Name = "Save settings" }).ClickAsync();
        // The dock's own message about the panel's return is said there too.
        await Expect(settings.Locator("[data-part=announcer] [role=status]")).ToContainTextAsync("Settings saved");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MinimizedDialogs_ShouldLineUpInTheirOwnWindow_WhenAPanelWithOneGoesToAWindow(string browser)
    {
        var page = await OpenDeskAsync(browser);
        await Button(page, "Log").ClickAsync();
        var log = page.GetByRole(AriaRole.Dialog, new() { Name = "Page log" });
        await log.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await Expect(log).ToHaveAttributeAsync("data-window-state", "minimized");
        await Button(page, "Notes").ClickAsync();
        var notes = page.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        await notes.GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await Expect(notes).ToHaveAttributeAsync("data-window-state", "minimized");
        Assert.NotEqual("0px 0px", await PlaceInTheRowAsync(notes));

        var window = (await page.Tb().Dock("Desk").PopOutAsync("Files")).Page;

        notes = window.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        await Expect(notes).ToBeVisibleAsync();
        Assert.Equal("0px 0px", await PlaceInTheRowAsync(notes));
        Assert.Equal("0px 0px", await PlaceInTheRowAsync(log));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldComeInsideItsNewWindow_WhenItWasMovedFurtherOverThanTheWindowIsWide(string browser)
    {
        var page = await OpenDeskAsync(browser);
        await Button(page, "Notes").ClickAsync();
        var notes = page.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        var title = (await notes.GetByRole(AriaRole.Heading).BoundingBoxAsync())!;
        var (x, y) = (title.X + title.Width / 2, title.Y + title.Height / 2);
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 400, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        var moved = (await notes.BoundingBoxAsync())!;

        var window = (await page.Tb().Dock("Desk").PopOutAsync("Files")).Page;

        // The panel's window opens narrower than the page, and the dialog was past its edge.
        notes = window.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        await Expect(notes).ToBeVisibleAsync();
        Assert.True((await notes.BoundingBoxAsync())!.X < moved.X);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayOpenWithItsPanel_WhenThePanelGoesToAWindowAndComesBack(string browser)
    {
        var page = await OpenDeskAsync(browser);
        await Button(page, "Notes").ClickAsync();
        await page.GetByRole(AriaRole.Dialog, new() { Name = "File notes" }).GetByLabel("Note").FillAsync("Keep");

        var window = (await page.Tb().Dock("Desk").PopOutAsync("Files")).Page;

        var notes = window.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        await Expect(notes).ToBeVisibleAsync();
        await Expect(notes.GetByLabel("Note")).ToHaveValueAsync("Keep");
        await notes.GetByLabel("Note").FillAsync("Keep this");
        await page.Tb().Dock("Desk").ReturnAsync("Files");

        notes = page.GetByRole(AriaRole.Dialog, new() { Name = "File notes" });
        await Expect(notes).ToBeVisibleAsync();
        await Expect(notes.GetByLabel("Note")).ToHaveValueAsync("Keep this");
        // Modeless, it doesn't take the focus by coming back.
        Assert.False(await notes.EvaluateAsync<bool>("dialog => dialog.contains(document.activeElement)"));
        await notes.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Expect(notes).ToHaveCountAsync(0);
    }
}
