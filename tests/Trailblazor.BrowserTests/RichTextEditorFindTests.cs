using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

public class RichTextEditorFindTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Trails = "<p>The trail and the trailhead</p><p>THE end of the <strong>trail</strong></p>";

    private static ILocator Bar(IPage page, string label = "Notes") =>
        Editor(page, label).Locator("xpath=ancestor::*[@data-part='editor']")
            .GetByRole(AriaRole.Group, new() { Name = "Find and replace" });

    private static ILocator Count(IPage page, string label = "Notes") => Bar(page, label).GetByRole(AriaRole.Status);

    // The texts the page marks for this editor: the matches, and the current one. Null in a browser without highlights.
    private static Task<string[][]?> MarkedAsync(IPage page) => page.EvaluateAsync<string[][]?>("""
        () => CSS.highlights ? ['tb-find', 'tb-find-current']
            .map(name => [...(CSS.highlights.get(name) ?? [])].map(range => range.toString())) : null
        """);

    // Opens the page with the notes holding the trails text, the caret at its start and the find bar open.
    private async Task<(IPage Page, ILocator Notes)> StartAsync(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        var notes = await SetAsync(page, Trails, "trailhead");
        await CaretAsync(notes, 0, 0);
        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await Expect(Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Find" })).ToBeFocusedAsync();
        return (page, notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldMarkAndCountTheMatchesWithoutChangingTheValue_WhenTextIsTyped(string browser)
    {
        var (page, _) = await StartAsync(browser);

        await page.Keyboard.TypeAsync("the");

        // Capitals don't matter, a match may be part of a word, and the first one from the caret is the current one.
        await Expect(Count(page)).ToHaveTextAsync("1 of 4");
        if (await MarkedAsync(page) is { } marked)
        {
            Assert.Equal([["the", "THE", "the"], ["The"]], marked);
        }

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(Trails);
        await Bar(page).GetByRole(AriaRole.Button, new() { Name = "Match case" }).ClickAsync();
        await Expect(Count(page)).ToHaveTextAsync("1 of 2");
        await Expect(Bar(page).GetByRole(AriaRole.Button, new() { Name = "Match case" }))
            .ToHaveAttributeAsync("aria-pressed", "true");

        // A match crosses formats, here into bold text, and a whole word has no letter on either side.
        await Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Find" }).FillAsync("the trail");
        await Expect(Count(page)).ToHaveTextAsync("1 of 2");
        await Bar(page).GetByRole(AriaRole.Button, new() { Name = "Whole word" }).ClickAsync();
        await Expect(Count(page)).ToHaveTextAsync("1 of 1");
        await Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Find" }).FillAsync("ridge");
        await Expect(Count(page)).ToHaveTextAsync("No results");
        await Expect(Bar(page).GetByRole(AriaRole.Button, new() { Name = "Next match" })).ToBeDisabledAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldStepThroughTheMatchesAndSelectTheCurrentOne_WhenEnterAndEscapeArePressed(
        string browser)
    {
        var (page, notes) = await StartAsync(browser);
        await page.Keyboard.TypeAsync("trail");
        await Expect(Count(page)).ToHaveTextAsync("1 of 3");

        // Enter goes on and wraps around, Shift+Enter goes back, and the buttons do the same.
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Count(page)).ToHaveTextAsync("3 of 3");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Count(page)).ToHaveTextAsync("1 of 3");
        await page.Keyboard.PressAsync("Shift+Enter");
        await Expect(Count(page)).ToHaveTextAsync("3 of 3");
        await Bar(page).GetByRole(AriaRole.Button, new() { Name = "Previous match" }).ClickAsync();
        await Expect(Count(page)).ToHaveTextAsync("2 of 3");

        // Ctrl+F on a new selection in the text starts the search again from there.
        await SelectAsync(notes, 1, 15, 1, 20);
        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await Expect(Count(page)).ToHaveTextAsync("3 of 3");
        await page.Keyboard.PressAsync("Shift+Enter");
        await Expect(Count(page)).ToHaveTextAsync("2 of 3");

        // Escape closes the bar and hands the editor the current match, selected, so typing replaces it.
        await page.Keyboard.PressAsync("Escape");
        await Expect(Bar(page)).ToBeHiddenAsync();
        await Expect(notes).ToBeFocusedAsync();
        Assert.Equal([[], []], await MarkedAsync(page) ?? [[], []]);
        await page.Keyboard.TypeAsync("bridge");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>The trail and the bridgehead</p><p>THE end of the <strong>trail</strong></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Replace_ShouldReplaceOneMatchOrAllInOneStepToUndo_WhenItsButtonsArePressed(string browser)
    {
        var (page, notes) = await StartAsync(browser);
        await page.Keyboard.TypeAsync("trail");
        await Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Replace with" }).FillAsync("path");

        // The current match is replaced, the app has the value, and the search goes on to the next match.
        await Bar(page).GetByRole(AriaRole.Button, new() { Name = "Replace", Exact = true }).ClickAsync();
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>The path and the trailhead</p><p>THE end of the <strong>trail</strong></p>");
        await Expect(Count(page)).ToHaveTextAsync("1 of 2");

        // The new text takes the format of what it replaces.
        await Bar(page).GetByRole(AriaRole.Button, new() { Name = "Replace all" }).ClickAsync();
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>The path and the pathhead</p><p>THE end of the <strong>path</strong></p>");
        await Expect(Count(page)).ToHaveTextAsync("Replaced 2");
        await Expect(Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Find" })).ToBeFocusedAsync();

        // Replace all is one step to undo, and Replace was another.
        await page.Keyboard.PressAsync("Escape");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>The path and the trailhead</p><p>THE end of the <strong>trail</strong></p>");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(Trails);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Replace_ShouldGoOnAfterTheNewText_WhenTheNewTextHoldsWhatWasSought(string browser)
    {
        var (page, _) = await StartAsync(browser);
        await page.Keyboard.TypeAsync("trail");
        await Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Replace with" }).FillAsync("trails");

        await page.Keyboard.PressAsync("Enter");

        // The new text matches too, and the search goes on after it, so replacing again can't grow it.
        await Expect(Count(page)).ToHaveTextAsync("2 of 3");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>The trails and the trailhead</p><p>THE end of the <strong>trail</strong></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldOpenWithTheSelectedTextAndCloseAgain_WhenItsToolbarButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        var notes = await SetAsync(page, Trails, "trailhead");
        var button = notes.Locator("xpath=ancestor::*[@data-part='editor']")
            .GetByRole(AriaRole.Button, new() { Name = "Find and replace" });
        await Expect(Bar(page)).ToBeHiddenAsync();
        await SelectAsync(notes, 0, 18, 0, 27);

        await button.ClickAsync();

        // The search starts from the selection, so the match that was selected is the current one.
        await Expect(Bar(page).GetByRole(AriaRole.Textbox, new() { Name = "Find" })).ToHaveValueAsync("trailhead");
        await Expect(Count(page)).ToHaveTextAsync("1 of 1");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await button.ClickAsync();
        await Expect(Bar(page)).ToBeHiddenAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(notes).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldPutTheCaretBackWhereItWas_WhenTheBarClosesWithNoMatch(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        var notes = await SetAsync(page, Trails, "trailhead");
        await CaretAsync(notes, 0, 4);
        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await page.Keyboard.TypeAsync("ridge");
        await Expect(Count(page)).ToHaveTextAsync("No results");

        await page.Keyboard.PressAsync("Escape");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("old ");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>The old trail and the trailhead</p><p>THE end of the <strong>trail</strong></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldLeaveTheViewWhereItIs_WhenTheTextIsEditedWhileTheBarShows(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        var lines = string.Concat(Enumerable.Range(1, 80).Select(line => $"<p>Line {line}</p>"));
        var notes = await SetAsync(page, $"<p>trail</p>{lines}", "Line 80");
        await CaretAsync(notes, 0, 0);
        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await page.Keyboard.TypeAsync("trail");
        await Expect(Count(page)).ToHaveTextAsync("1 of 1");

        // An edit far below the current match marks the matches again, and must not bring the view back to it.
        // Typing scrolls to the caret, and Enter is an edit the editor makes itself, which nothing scrolls after.
        await CaretAsync(notes, 80, 7);
        await page.Keyboard.TypeAsync("!");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes")).ToContainTextAsync("<p>Line 80!</p><p><br></p>");

        Assert.True(await page.EvaluateAsync<double>("scrollY") > 500);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Replace_ShouldLeaveAMentionWholeAndSendTheValueAtOnce_WhenEnterIsPressedInItsField(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        await CaretAsync(Editor(page, "Summary"), 0, 0);
        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await page.Keyboard.TypeAsync("Ada");

        // A mention is one unit, so its text isn't searched.
        await Expect(Count(page, "Summary")).ToHaveTextAsync("1 of 1");
        await Bar(page, "Summary").GetByRole(AriaRole.Textbox, new() { Name = "Replace with" }).FillAsync("Bo");
        await page.Keyboard.PressAsync("Enter");

        // This editor sends its value as focus leaves it, which has happened already, so it sends the new one itself.
        await Expect(page.GetByTestId("summary")).ToHaveTextAsync(
            "<p>Ask <span data-mention=\"7\" data-trigger=\"@\">@Ada</span> about Bo's map</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldCloseAndTakeItsMarksAway_WhenTheHtmlSourceIsShown(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        var summary = Editor(page, "Summary");
        var tools = summary.Locator("xpath=ancestor::*[@data-part='editor']");
        await CaretAsync(summary, 0, 0);
        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await page.Keyboard.TypeAsync("map");
        await Expect(Count(page, "Summary")).ToHaveTextAsync("1 of 1");

        await tools.GetByRole(AriaRole.Button, new() { Name = "HTML source" }).ClickAsync();

        // The source isn't the text the bar searches, so the bar goes, with its marks and its button's pressed state.
        await Expect(tools.Locator("[data-part=find]")).ToHaveCountAsync(0);
        await Expect(tools.Locator("[data-part=find-toggle]")).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.Equal([[], []], await MarkedAsync(page) ?? [[], []]);
        await tools.GetByRole(AriaRole.Button, new() { Name = "HTML source" }).ClickAsync();
        await Expect(tools.Locator("[data-part=find]")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Find_ShouldFindAndNotReplace_WhenTheEditorIsReadOnly(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-find");
        var approved = Editor(page, "Approved");
        await CaretAsync(approved, 0, 0);

        await page.Keyboard.PressAsync("ControlOrMeta+f");
        await page.Keyboard.TypeAsync("the");

        await Expect(Count(page, "Approved")).ToHaveTextAsync("1 of 2");
        await Expect(Bar(page, "Approved").GetByRole(AriaRole.Textbox, new() { Name = "Replace with" }))
            .ToBeDisabledAsync();
        await Expect(Bar(page, "Approved").GetByRole(AriaRole.Button, new() { Name = "Replace all" }))
            .ToBeDisabledAsync();
    }
}
