using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerUnsavedTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepThePanelAndTheFocus_WhenItsCanCloseRefusesAKeyOrAMiddleClick(string browser)
    {
        var page = await OpenUnsavedAsync(browser);
        var draft = Tab(page, "Draft");
        await draft.FocusAsync();

        await page.Keyboard.PressAsync("Delete");
        await Expect(page.GetByTestId("asked")).ToHaveTextAsync("1");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("asked")).ToHaveTextAsync("2");
        await draft.ClickAsync(new() { Button = MouseButton.Middle });
        await Expect(page.GetByTestId("asked")).ToHaveTextAsync("3");

        await Expect(draft).ToBeFocusedAsync();
        await Expect(page.GetByText("Day one: the ridge.")).ToBeVisibleAsync();

        // Once the app agrees, the same key closes the panel.
        await page.GetByRole(AriaRole.Button, new() { Name = "Allow", Exact = true }).ClickAsync();
        await draft.FocusAsync();
        await page.Keyboard.PressAsync("Delete");
        await Expect(draft).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldShowADotUntilTheTabIsHoveredOrFocused_WhenItsPanelIsModified(string browser)
    {
        var page = await OpenUnsavedAsync(browser);
        var (draft, pinned) = (Tab(page, "Draft"), Tab(page, "Pinned"));
        await Expect(draft).ToHaveAccessibleDescriptionAsync("Unsaved changes Press Delete to close");
        await Expect(pinned).ToHaveAccessibleDescriptionAsync("Unsaved changes");
        await Expect(Tab(page, "Map")).ToHaveAccessibleDescriptionAsync("Press Delete to close");

        // The dot has the close mark's place, and the two trade without changing the tab's width.
        await Expect(draft.Locator("[data-part=modified]")).ToBeVisibleAsync();
        await Expect(draft.Locator("[data-part=close]")).ToBeHiddenAsync();
        var width = (await draft.BoundingBoxAsync())!.Width;
        await draft.HoverAsync();
        await Expect(draft.Locator("[data-part=close]")).ToBeVisibleAsync();
        await Expect(draft.Locator("[data-part=modified]")).ToBeHiddenAsync();
        Assert.Equal(width, (await draft.BoundingBoxAsync())!.Width);

        await pinned.HoverAsync();
        await Expect(draft.Locator("[data-part=modified]")).ToBeVisibleAsync();
        // Focus moved by an arrow key is keyboard focus, which makes the tab show its close mark.
        await draft.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(draft).ToBeFocusedAsync();
        await Expect(draft.Locator("[data-part=close]")).ToBeVisibleAsync();

        // A tab that can't close keeps its dot under the pointer.
        await Expect(pinned.Locator("[data-part=modified]")).ToBeVisibleAsync();
        var result = await page.Locator(".tb-dock").RunAxe(AxeTests.WcagAA);
        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturnWithThePanelKeptOrGone_WhenItsCanCloseAnswers(string browser)
    {
        var page = await OpenUnsavedAsync(browser);
        var dock = page.Tb().Dock();

        await dock.CloseAsync("Draft");

        await Expect(page.GetByTestId("asked")).ToHaveTextAsync("1");
        await Expect(Tab(page, "Draft")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Allow", Exact = true }).ClickAsync();
        await dock.CloseAsync("Draft");
        // The tab is gone by the time the helper returns, with nothing more to wait for.
        Assert.Equal(0, await Tab(page, "Draft").CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturnOnceTheQuestionIsOpen_WhenCanCloseAsksInADialog(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dock-manager");
        var dock = page.Tb().Dock("Trip journal");
        // The page's other examples have fields and tabs with the same names.
        var journal = page.GetByRole(AriaRole.Group, new() { Name = "Trip journal", Exact = true });
        var entry = journal.GetByRole(AriaRole.Tab, new() { Name = "Entry", Exact = true });
        var text = journal.GetByRole(AriaRole.Textbox, new() { Name = "Today's entry" });
        await text.FillAsync("Rain all day.");
        await Expect(entry).ToHaveAccessibleDescriptionAsync("Unsaved changes Press Delete to close");

        var handled = await journal.GetAttributeAsync("data-commits");
        await dock.CloseAsync("Entry");
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await Expect(text).ToHaveValueAsync("Rain all day.");
        // The dock drops a close asked for while the first is still being answered, so the test waits for that one.
        await Expect(journal).Not.ToHaveAttributeAsync("data-commits", handled!);

        await dock.CloseAsync("Entry");
        await page.GetByRole(AriaRole.Button, new() { Name = "Discard", Exact = true }).ClickAsync();
        await Expect(entry).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Open Entry" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepTheModifiedDot_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/dock-unsaved", new() { ForcedColors = ForcedColors.Active });
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");

        // The dot is a border in the tab's text color, which forced colors keep.
        var dot = await Tab(page, "Pinned").Locator("[data-part=modified]").EvaluateAsync<string[]>("""
            mark => {
                const dot = getComputedStyle(mark, '::before');
                return [dot.borderTopWidth, dot.borderTopStyle, dot.borderTopColor, getComputedStyle(mark).color];
            }
            """);

        Assert.Equal(["4px", "solid"], dot[..2]);
        Assert.Equal(dot[3], dot[2]);
    }

    private async Task<IPage> OpenUnsavedAsync(string browser)
    {
        var page = await OpenAsync(browser, "/dock-unsaved");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }
}
