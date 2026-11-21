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
    public async Task Toaster_ShouldStayInItsDialog_WhenTheUserGoesToWorkInAnotherWindow(string browser)
    {
        var page = await OpenDeskAsync(browser);
        var window = (await page.Tb().Dock("Desk").PopOutAsync("Other")).Page;
        await Button(page, "Settings").ClickAsync();
        var settings = page.GetByRole(AriaRole.Dialog, new() { Name = "File settings" });
        await settings.GetByRole(AriaRole.Button, new() { Name = "Save settings" }).ClickAsync();
        await Expect(settings.Locator("[data-part=toast]")).ToHaveTextAsync("Settings saved");

        // The page's modal doesn't block the other panel's window.
        await window.GetByText("Other notes").ClickAsync();

        await Expect(settings.Locator("[data-part=toast]")).ToHaveCountAsync(1);
        await Expect(window.Locator("[data-part=toast]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenOnThePage_WhenAskedForFromThePageWhileAPanelIsInAWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);

        await Button(page, "Ask on the page").ClickAsync();

        await Expect(Asking(page)).ToBeVisibleAsync();
        await Expect(Asking(window)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldCloseOnlyTheDialogInItsWindow_WhenDialogsAreOpenInTwoWindows(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(page, "Ask on the page").ClickAsync();
        await Expect(Asking(page)).ToBeVisibleAsync();
        await Button(window, "Rules").ClickAsync();
        var rules = window.GetByRole(AriaRole.Dialog, new() { Name = "File rules" });
        await Expect(rules).ToBeVisibleAsync();

        await window.Keyboard.PressAsync("Escape");

        await Expect(rules).ToHaveCountAsync(0);
        // The page's dialog is still there to answer; Escape would have answered it with Kept.
        await Asking(page).GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Removed");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldCloseTheDialogInItsWindow_WhenADialogOpenedLaterInAnotherWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Rules").ClickAsync();
        var rules = window.GetByRole(AriaRole.Dialog, new() { Name = "File rules" });
        await Expect(rules).ToBeVisibleAsync();
        await Button(page, "Ask on the page").ClickAsync();
        await Expect(Asking(page)).ToBeVisibleAsync();

        await window.Keyboard.PressAsync("Escape");

        await Expect(rules).ToHaveCountAsync(0);
        await Asking(page).GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Removed");
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
        // A modal in the page's top layer doesn't pull a dialog in another window into the top layer with it.
        await Button(page, "Ask on the page").ClickAsync();
        await Expect(Asking(page)).ToBeVisibleAsync();
        await Button(window, "Rules").ClickAsync();
        var rules = window.GetByRole(AriaRole.Dialog, new() { Name = "File rules" });
        await Expect(rules).ToHaveAttributeAsync("aria-modal", "true");
        Assert.True(await IsInertAsync(Button(window, "Ask")));
        await page.Keyboard.PressAsync("Escape");
        await Expect(Asking(page)).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(Button(page, "Ask on the page")));

        await window.CloseAsync();

        rules = page.GetByRole(AriaRole.Dialog, new() { Name = "File rules" });
        await Expect(rules).ToBeVisibleAsync();
        Assert.True(await IsInertAsync(Button(page, "Ask")));
        Assert.True(await IsInertAsync(Button(page, "Ask on the page")));
        await page.Keyboard.PressAsync("Escape");
        await Expect(rules).ToHaveCountAsync(0);
        Assert.False(await IsInertAsync(Button(page, "Ask on the page")));
        Assert.False(await IsInertAsync(Button(page, "Ask")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldStopFiringOnlyInTheWindowTheModalIsIn_WhenAModalIsOpenInAPanelsWindow(string browser)
    {
        var (page, window) = await OpenFilesInAWindowAsync(browser);
        await Button(window, "Settings").ClickAsync();
        await Expect(window.GetByRole(AriaRole.Dialog, new() { Name = "File settings" })).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Alt+K");
        await Expect(page.GetByTestId("marked")).ToHaveTextAsync("1");
        await window.Keyboard.PressAsync("Alt+K");
        await page.Keyboard.PressAsync("Alt+K");

        // The press in the modal's window didn't count.
        await Expect(page.GetByTestId("marked")).ToHaveTextAsync("2");
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
