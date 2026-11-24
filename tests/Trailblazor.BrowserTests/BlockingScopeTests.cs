using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class BlockingScopeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string IsInert = "element => element.closest('[inert]') !== null";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldEverythingOutsideItAndGiveItBack_WhenItBlocksAndThenGoes(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");
        var (outside, sidebar, edit) = (Button(page, "Outside"), Button(page, "Sidebar"), Button(page, "Edit"));

        await edit.ClickAsync();

        // The scope takes the focus, and what is outside it is inert and looks held.
        await Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeFocusedAsync();
        await Expect(page.Locator("[data-tb-blocked]").First).ToBeAttachedAsync();
        Assert.All(await Task.WhenAll(outside.EvaluateAsync<bool>(IsInert), sidebar.EvaluateAsync<bool>(IsInert),
            edit.EvaluateAsync<bool>(IsInert)), Assert.True);
        Assert.False(await Button(page, "Done").EvaluateAsync<bool>(IsInert));
        Assert.Equal("0.5", await outside.EvaluateAsync<string>(
            "element => getComputedStyle(element.closest('[data-tb-blocked]')).opacity"));

        // Tab never reaches what is outside: after more presses than the page has controls, the focus has only been
        // in the scope or out of the page.
        var held = page.GetByRole(AriaRole.Region, new() { Name = "Editor" });
        for (var press = 0; press < 12; press++)
        {
            await page.Keyboard.PressAsync("Tab");
            Assert.True(await held.EvaluateAsync<bool>(
                "scope => scope.contains(document.activeElement) || document.activeElement === document.body"));
        }

        // A press on a button outside does nothing, and the count the outside buttons raise stays where it was.
        var box = (await outside.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Button(page, "Done").ClickAsync();

        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
        await Expect(edit).ToBeFocusedAsync();
        await Expect(page.GetByTestId("outside")).ToHaveTextAsync("0");
        await outside.ClickAsync();
        await Expect(page.GetByTestId("outside")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldGiveTheFocusBackToWhatOpenedIt_WhenItIsOpenedAndEndedFromTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");

        // No pointer is used, so the scope can only know its opener from the focus it had, which the hold takes away.
        await Button(page, "Edit").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeFocusedAsync();
        await Button(page, "Done").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Button(page, "Edit")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldNothingUntilItShows_WhenItStartsInsideSomethingHidden(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");

        // A scope nobody can get to would leave nothing in the app usable, so it holds nothing yet.
        await Button(page, "Start stowed").ClickAsync();
        await Expect(page.Locator("[data-part=focus-scope][data-ready]")).ToBeAttachedAsync();
        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
        await Button(page, "Outside").ClickAsync();
        await Expect(page.GetByTestId("outside")).ToHaveTextAsync("1");

        await Button(page, "Show").ClickAsync();

        // Once it shows, it holds the page and takes the focus.
        await Expect(Button(page, "Put away")).ToBeFocusedAsync();
        Assert.True(await Button(page, "Outside").EvaluateAsync<bool>(IsInert));
        await Button(page, "Put away").ClickAsync();
        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldThePage_WhenItHasNoBoxOfItsOwn(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");

        await Button(page, "Start boxless").ClickAsync();

        // The scope takes the focus as it starts, a moment before its block holds the page, which the mark follows.
        await Expect(Button(page, "End boxless")).ToBeFocusedAsync();
        await Expect(page.Locator("[data-part=focus-scope][data-ready]")).ToBeAttachedAsync();
        Assert.True(await Button(page, "Outside").EvaluateAsync<bool>(IsInert));
        await Button(page, "End boxless").ClickAsync();
        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldNothingUntilItsTabShows_WhenItStartsInADockPanelBehindAnotherTab(string browser)
    {
        var page = await OpenAsync(browser, "/dock-blocking");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Workspace" })).ToHaveAttributeAsync("data-ready", "");
        var dock = page.Tb().Dock("Workspace");
        // A panel's content first renders when its tab first shows, so the draft is shown once and left.
        await dock.SelectAsync("Draft");
        await dock.SelectAsync("Notes");

        await Button(page, "Draft").ClickAsync();
        await Expect(page.Locator("[data-part=focus-scope][data-ready]")).ToBeAttachedAsync();

        // The scope is behind the notes' tab: the dock, with the tab that shows the draft, stays usable.
        Assert.False(await Button(page, "Start").EvaluateAsync<bool>(IsInert));
        await dock.SelectAsync("Draft");

        await Expect(Button(page, "Close draft")).ToBeFocusedAsync();
        Assert.True(await Button(page, "Start").EvaluateAsync<bool>(IsInert));
        await Button(page, "Close draft").ClickAsync();
        await Expect(page.Locator("[data-tb-blocked]")).ToHaveCountAsync(0);
        Assert.False(await Button(page, "Start").EvaluateAsync<bool>(IsInert));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldLeaveADialogItsContentOpensUsable_WhenItBlocks(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");
        await Button(page, "Edit").ClickAsync();

        await Button(page, "Ask").ClickAsync();
        // The editor's trap stands down behind the dialog, so Tab moves between the dialog's own buttons.
        await Expect(Button(page, "OK")).ToBeVisibleAsync();
        const string Focused = "() => document.activeElement.textContent.trim()";
        await page.WaitForFunctionAsync($"() => ['OK', 'Cancel'].includes(({Focused})())");
        var first = await page.EvaluateAsync<string>(Focused);
        await page.Keyboard.PressAsync("Tab");
        Assert.Equal(first == "OK" ? "Cancel" : "OK", await page.EvaluateAsync<string>(Focused));
        await Button(page, "OK").ClickAsync();

        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("Kept");
        // The scope goes on holding the page once the dialog has gone.
        Assert.True(await Button(page, "Outside").EvaluateAsync<bool>(IsInert));
        Assert.False(await Button(page, "Ask").EvaluateAsync<bool>(IsInert));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldAnOlderScopeToo_WhenASecondOneBlocksAfterIt(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");
        await Button(page, "Edit").ClickAsync();

        await Button(page, "More").ClickAsync();

        // The newer scope is the one in force: the older one is outside it, and its trap stands down with it, so
        // Tab moves on in the newer one.
        await Expect(Button(page, "Close details")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Button(page, "Second detail")).ToBeFocusedAsync();
        Assert.True(await Button(page, "Done").EvaluateAsync<bool>(IsInert));
        Assert.True(await Button(page, "Outside").EvaluateAsync<bool>(IsInert));
        Assert.False(await Button(page, "Close details").EvaluateAsync<bool>(IsInert));

        await Button(page, "Close details").ClickAsync();

        // The older scope is in force again, with the focus back where it was in it.
        await Expect(Button(page, "More")).ToBeFocusedAsync();
        Assert.False(await Button(page, "Done").EvaluateAsync<bool>(IsInert));
        Assert.True(await Button(page, "Outside").EvaluateAsync<bool>(IsInert));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldWhatIsAddedBesideItLater_WhenItIsBlockingAlready(string browser)
    {
        var page = await OpenAsync(browser, "/blocking-scope");
        await Button(page, "Edit").ClickAsync();

        await Button(page, "Add").ClickAsync();

        // The button arrives beside the scope's section, and is held like the rest.
        await Expect(Button(page, "Late")).ToHaveAttributeAsync("data-tb-blocked", "");
        Assert.True(await Button(page, "Late").EvaluateAsync<bool>(IsInert));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldTheAppsOtherWindows_WhenItBlocksOnThePageOrInAPanelsWindow(string browser)
    {
        var page = await OpenAsync(browser, "/dock-blocking");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Workspace" })).ToHaveAttributeAsync("data-ready", "");
        var dock = page.Tb().Dock("Workspace");
        var notes = await dock.PopOutAsync("Notes");

        // The scope starts on the page: the notes' window is held, and dims, as under a modal dialog.
        await Button(page, "Start").ClickAsync();
        await Expect(Button(page, "Finish")).ToBeFocusedAsync();
        await Expect(notes.Page.Locator("[data-part=scrim]")).ToBeAttachedAsync();
        Assert.True(await notes.GetByRole(AriaRole.Button, new() { Name = "Note" }).EvaluateAsync<bool>(IsInert));
        await Button(page, "Finish").ClickAsync();
        await Expect(notes.Page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
        await notes.GetByRole(AriaRole.Button, new() { Name = "Note" }).ClickAsync();
        await Expect(notes.GetByTestId("notes")).ToHaveTextAsync("1");

        // The scope starts in the editor's own window: the page is held, and so is the notes' window.
        var editor = await dock.PopOutAsync("Editor");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Start" }).ClickAsync();
        await Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Finish" })).ToBeFocusedAsync();
        await Expect(page.Locator("[data-part=scrim]")).ToBeAttachedAsync();
        await Expect(notes.Page.Locator("[data-part=scrim]")).ToBeAttachedAsync();
        Assert.True(await page.GetByRole(AriaRole.Group, new() { Name = "Workspace" }).EvaluateAsync<bool>(IsInert));

        await editor.GetByRole(AriaRole.Button, new() { Name = "Finish" }).ClickAsync();

        await Expect(page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
        await Expect(notes.Page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
    }

    private async Task<IPage> OpenAllowingDockAsync(string browser)
    {
        var page = await OpenAsync(browser, "/dock-blocking-allow");
        var workspace = page.GetByRole(AriaRole.Group, new() { Name = "Workspace" });
        await Expect(workspace).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldLeaveTheRegionItAllowsUsable_WhenItBlocksBesideIt(string browser)
    {
        var page = await OpenAllowingDockAsync(browser);
        var (mark, note, outside) = (Button(page, "Mark"), Button(page, "Note"), Button(page, "Outside"));

        await Button(page, "Start").ClickAsync();
        await Expect(Button(page, "Finish")).ToBeFocusedAsync();
        await Expect(page.Locator("[data-tb-blocked]").First).ToBeAttachedAsync();

        // The viewer is left as it was for the pointer and for screen readers, while the rest is held.
        Assert.False(await mark.EvaluateAsync<bool>(IsInert));
        Assert.All(await Task.WhenAll(note.EvaluateAsync<bool>(IsInert), outside.EvaluateAsync<bool>(IsInert)),
            Assert.True);
        await mark.ClickAsync();
        await Expect(page.GetByTestId("marks")).ToHaveTextAsync("1");
        // Tab goes on from the scope into the viewer, and its button works from the keyboard.
        await Button(page, "Ask").FocusAsync();
        for (var press = 0; press < 4 && !await mark.EvaluateAsync<bool>("mark => mark === document.activeElement");
            press++)
        {
            await page.Keyboard.PressAsync("Tab");
        }

        await Expect(mark).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("marks")).ToHaveTextAsync("2");

        await Button(page, "Finish").ClickAsync();

        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldAWindowWithAnAllowedRegionPieceByPiece_WhenItBlocksInAnotherWindow(string browser)
    {
        var page = await OpenAllowingDockAsync(browser);
        var dock = page.Tb().Dock("Workspace");
        var viewer = await dock.PopOutAsync("Viewer");
        var notes = await dock.PopOutAsync("Notes");
        var mark = viewer.GetByRole(AriaRole.Button, new() { Name = "Mark" });

        // The scope starts on the page. The viewer's window has no scrim and its button works; the notes' window
        // is held whole.
        await Button(page, "Start").ClickAsync();
        await Expect(Button(page, "Finish")).ToBeFocusedAsync();
        await Expect(notes.Page.Locator("[data-part=scrim]")).ToBeAttachedAsync();
        await Expect(viewer.Page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
        Assert.False(await mark.EvaluateAsync<bool>(IsInert));
        await mark.ClickAsync();
        await Expect(viewer.GetByTestId("marks")).ToHaveTextAsync("1");

        // A modal dialog holds every other window whole, whatever the scope allows.
        await Button(page, "Ask").ClickAsync();
        await Expect(viewer.Page.Locator("[data-part=scrim]")).ToBeAttachedAsync();
        Assert.True(await mark.EvaluateAsync<bool>(IsInert));
        await Button(page, "OK").ClickAsync();
        await Expect(viewer.Page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
        Assert.False(await mark.EvaluateAsync<bool>(IsInert));

        await Button(page, "Finish").ClickAsync();
        await Expect(notes.Page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
        await dock.ReturnAsync("Viewer");

        // The scope starts in the editor's own window. The page holds the viewer, so the page is held piece by
        // piece: no scrim, the viewer usable, the rest inert.
        var editor = await dock.PopOutAsync("Editor");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Start" }).ClickAsync();
        await Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Finish" })).ToBeFocusedAsync();
        await Expect(page.Locator("[data-tb-blocked]").First).ToBeAttachedAsync();
        await Expect(page.Locator("[data-part=scrim]")).ToHaveCountAsync(0);
        Assert.False(await Button(page, "Mark").EvaluateAsync<bool>(IsInert));
        Assert.True(await Button(page, "Outside").EvaluateAsync<bool>(IsInert));
        await Button(page, "Mark").ClickAsync();
        await Expect(page.GetByTestId("marks")).ToHaveTextAsync("2");
        // A button that joins the page beside the dock after the hold started is held too.
        await editor.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
        await Expect(Button(page, "Late")).ToHaveAttributeAsync("inert", "");

        await editor.GetByRole(AriaRole.Button, new() { Name = "Finish" }).ClickAsync();

        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
    }

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
}
