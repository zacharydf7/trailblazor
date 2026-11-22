using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

public class RichTextEditorFloatingToolbarTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Trails = "<p>The trail and the trailhead</p>";

    private static ILocator Floating(IPage page, string label = "Notes") =>
        Editor(page, label).Locator("xpath=ancestor::*[@data-part='editor']")
            .GetByRole(AriaRole.Toolbar, new() { Name = "Selection formatting" });

    private static ILocator Tool(IPage page, string name) =>
        Floating(page).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // The top and bottom of the selected text in the window.
    private static Task<double[]> SelectionEdgesAsync(IPage page) => page.EvaluateAsync<double[]>("""
        () => {
            const box = getSelection().getRangeAt(0).getBoundingClientRect();
            return [box.top, box.bottom];
        }
        """);

    // The toolbar is placed once a frame, so what a test did has reached it two frames on.
    private static Task FramesAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

    // Opens the page with "trail", the second word of the notes, selected.
    private async Task<(IPage Page, ILocator Notes)> StartAsync(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-floating");
        var notes = await SetAsync(page, Trails, "trailhead");
        await SelectAsync(notes, 0, 4, 0, 9);
        return (page, notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldShowAboveTheSelectionAndApplyItsFormats_WhenTextIsSelected(string browser)
    {
        var (page, notes) = await StartAsync(browser);

        await Expect(Floating(page)).ToBeVisibleAsync();
        var (bar, edges) = ((await Floating(page).BoundingBoxAsync())!, await SelectionEdgesAsync(page));
        Assert.True(bar.Y + bar.Height <= edges[0], $"The toolbar ends at {bar.Y + bar.Height}, past {edges[0]}.");
        Assert.True(bar.X >= 0);

        await Tool(page, "Bold").ClickAsync();

        // The format goes on, the button says so, and the text keeps the focus and its selection.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>The <strong>trail</strong> and the trailhead</p>");
        await Expect(Tool(page, "Bold")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(notes).ToBeFocusedAsync();
        await Expect(Floating(page)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Floating(page)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldGoBelowTheSelection_WhenThereIsNoRoomAboveIt(string browser)
    {
        var (page, _) = await StartAsync(browser);
        await Expect(Floating(page)).ToBeVisibleAsync();

        // The page scrolls until the selected line is at the window's top edge.
        await page.EvaluateAsync("() => scrollBy(0, getSelection().getRangeAt(0).getBoundingClientRect().top - 4)");
        await FramesAsync(page);

        var (bar, edges) = ((await Floating(page).BoundingBoxAsync())!, await SelectionEdgesAsync(page));
        Assert.True(bar.Y >= edges[1], $"The toolbar starts at {bar.Y}, above the selection's end at {edges[1]}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldTakeTheFocusAndGiveItBack_WhenAltF10AndEscapeArePressed(string browser)
    {
        var (page, notes) = await StartAsync(browser);
        await Expect(Floating(page)).ToBeVisibleAsync();

        // Its buttons are out of the tab order; Alt+F10 goes to the first, and the arrows, Home and End move in it.
        await page.Keyboard.PressAsync("Alt+F10");
        await Expect(Tool(page, "Bold")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Tool(page, "Link")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tool(page, "Bold")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tool(page, "Italic")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        // A button applies its format and hands the focus back to the text, with the selection as it was.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>The <em>trail</em> and the trailhead</p>");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Alt+F10");
        await Expect(Tool(page, "Bold")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("path");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>The <em>path</em> and the trailhead</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldHide_WhenEscapeIsPressedInTheTextOrTheFocusLeavesTheEditor(string browser)
    {
        var (page, _) = await StartAsync(browser);
        await Expect(Floating(page)).ToBeVisibleAsync();

        // Escape hides it until the selection changes.
        await page.Keyboard.PressAsync("Escape");
        await Expect(Floating(page)).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(Floating(page)).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Textbox, new() { Name = "Source" }).FocusAsync();
        await Expect(Floating(page)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldStayHidden_WhenTheSelectionIsMadeByTouch(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-floating");
        var notes = await SetAsync(page, Trails, "trailhead");

        // A finger's selection gets the system's own menu, which sits where the toolbar would.
        await notes.DispatchEventAsync("pointerdown", new { pointerType = "touch", bubbles = true });
        await SelectAsync(notes, 0, 4, 0, 9);
        await notes.DispatchEventAsync("pointerup", new { pointerType = "touch", bubbles = true });
        await FramesAsync(page);
        await Expect(Floating(page)).ToBeHiddenAsync();

        // The keyboard changing that selection brings the toolbar.
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(Floating(page)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldWaitUntilThePointerLifts_WhenTheSelectionIsBeingDragged(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-floating");
        var notes = await SetAsync(page, Trails, "trailhead");

        // While the button is down the selection is still being made, and a toolbar would follow it around.
        await notes.DispatchEventAsync("pointerdown", new { pointerType = "mouse", bubbles = true });
        await SelectAsync(notes, 0, 4, 0, 9);
        await FramesAsync(page);
        await Expect(Floating(page)).ToBeHiddenAsync();

        await notes.DispatchEventAsync("pointerup", new { pointerType = "mouse", bubbles = true });
        await Expect(Floating(page)).ToBeVisibleAsync();

        // A press on one of the main toolbar's buttons makes no selection, so the floating toolbar doesn't blink.
        await notes.Locator("xpath=ancestor::*[@data-part='editor']").Locator("[data-part=toolbar] [data-format=bold]")
            .DispatchEventAsync("pointerdown", new { pointerType = "mouse", bubbles = true });
        await FramesAsync(page);
        await Expect(Floating(page)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldWorkWithoutAMainToolbar_WhenTheEditorHidesIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-floating");
        var brief = Editor(page, "Brief");
        await SelectAsync(brief, 0, 0, 0, 5);

        await Expect(Floating(page, "Brief").GetByRole(AriaRole.Button)).ToHaveCountAsync(2);
        await Floating(page, "Brief").GetByRole(AriaRole.Button, new() { Name = "Bold" }).ClickAsync();
        await brief.BlurAsync();

        await Expect(page.GetByTestId("brief")).ToHaveTextAsync("<p><strong>Short</strong> and plain</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldNotSendTheValue_WhenAltF10MovesTheFocusToIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-floating");
        var brief = Editor(page, "Brief");
        await CaretAsync(brief, 0, 15);
        await page.Keyboard.TypeAsync("!");
        await SelectAsync(brief, 0, 0, 0, 5);
        await Expect(Floating(page, "Brief")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Alt+F10");
        await Expect(Floating(page, "Brief").GetByRole(AriaRole.Button, new() { Name = "Bold" })).ToBeFocusedAsync();

        // This editor sends its value as focus leaves it, and its own toolbar isn't outside it. The editor counts
        // the values it has sent in data-sends.
        Assert.Null(await brief.GetAttributeAsync("data-sends"));
        await page.Keyboard.PressAsync("Escape");
        await brief.BlurAsync();
        await Expect(page.GetByTestId("brief")).ToHaveTextAsync("<p>Short and plain!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldHide_WhenTheSelectionIsScrolledOutOfView(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-floating");

        // The toolbar is drawn over everything, so it must go with the selection: in an editor that scrolls its
        // own text, and in one that a box around it scrolls.
        var log = Editor(page, "Log");
        await SelectAsync(log, 0, 0, 0, 4);
        await Expect(Floating(page, "Log")).ToBeVisibleAsync();
        await log.EvaluateAsync("editor => editor.scrollTop = editor.scrollHeight");
        await Expect(Floating(page, "Log")).ToBeHiddenAsync();

        await SelectAsync(Editor(page, "Boxed"), 0, 0, 0, 4);
        await Expect(Floating(page, "Boxed")).ToBeVisibleAsync();
        await page.GetByTestId("box").EvaluateAsync("box => box.scrollTop = box.scrollHeight");
        await Expect(Floating(page, "Boxed")).ToBeHiddenAsync();
    }
}
