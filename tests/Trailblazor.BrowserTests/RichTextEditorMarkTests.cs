using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

public class RichTextEditorMarkTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Spoiler = "<span data-tb-mark=\"spoiler\">";

    private static ILocator Tool(ILocator notes, string name) =>
        notes.Locator("xpath=ancestor::*[@data-part='editor']").Locator("[data-part=toolbar]")
            .GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // Opens the page with the notes holding a sentence, and "Esc" in it selected.
    private async Task<(IPage Page, ILocator Notes)> StartAsync(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-marks");
        var notes = await SetAsync(page, "<p>Press Esc to leave</p>", "leave");
        await SelectAsync(notes, 0, 6, 0, 9);
        return (page, notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mark_ShouldGoOnAndOffTheSelection_WhenItsButtonOrItsShortcutIsPressed(string browser)
    {
        var (page, notes) = await StartAsync(browser);

        // A mark named for an element is written as that element, and its button says it is on.
        await Tool(notes, "Keyboard key").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Press <kbd>Esc</kbd> to leave</p>");
        await Expect(Tool(notes, "Keyboard key")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(notes.Locator("kbd")).ToHaveTextAsync("Esc");

        // Any other is a span that names it, inside the first, as .NET writes the two.
        await Tool(notes, "Spoiler").ClickAsync();
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"<p>Press <kbd>{Spoiler}Esc</span></kbd> to leave</p>");

        // The mark's own keys take it off again, and Clear formatting takes off the other.
        await page.Keyboard.PressAsync("Alt+Shift+K");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Press {Spoiler}Esc</span> to leave</p>");
        await Expect(Tool(notes, "Keyboard key")).ToHaveAttributeAsync("aria-pressed", "false");
        // Its other keys put it back: a key that isn't a letter, and one of them only with exactly its modifiers.
        await page.Keyboard.PressAsync("Meta+Alt+Shift+K");
        await page.Keyboard.PressAsync("Control+;");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"<p>Press <kbd>{Spoiler}Esc</span></kbd> to leave</p>");
        await page.Keyboard.PressAsync("ControlOrMeta+\\");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Press Esc to leave</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mark_ShouldBeWrittenAsItsSpan_WhenItsNameIsOneEveryScriptObjectHas(string browser)
    {
        var (page, notes) = await StartAsync(browser);

        await Tool(notes, "Builder").ClickAsync();

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>Press <span data-tb-mark=\"constructor\">Esc</span> to leave</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mark_ShouldGoOnTheTextTypedNext_WhenTurnedOnAtACaret(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-marks");
        var notes = await SetAsync(page, "<p>Press</p>", "Press");
        await CaretAsync(notes, 0, 5);
        await page.Keyboard.TypeAsync(" ");

        await Tool(notes, "Keyboard key").ClickAsync();
        await page.Keyboard.TypeAsync("Tab");
        await Tool(notes, "Keyboard key").ClickAsync();
        await page.Keyboard.TypeAsync(" twice");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Press <kbd>Tab</kbd> twice</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mark_ShouldBeReadAsDotNetReadsIt_WhenTextWithMarksIsPastedOrSetFromDotNet(string browser)
    {
        // The editor's marks are kept, also around other formats. These aren't marks: var, a span that names
        // another mark, an element named as a mark that is written as a span, and a span that names a mark that
        // is written as an element.
        const string Html = "<p>Press <kbd>Esc</kbd> or <span data-tb-mark=\"spoiler\">run <b>away</b></span>, "
            + "<var>x</var> <span data-tb-mark=\"other\">y</span> <constructor>z</constructor> "
            + "<span data-tb-mark=\"kbd\">w</span></p>";
        var schema = new RichTextSchema { Marks = ["kbd", "spoiler", "constructor"] };
        var expected = RichTextDocument.Parse(Html, schema).ToHtml();
        Assert.Equal($"<p>Press <kbd>Esc</kbd> or {Spoiler}run </span><strong>{Spoiler}away</span></strong>, "
            + "x y z w</p>", expected);
        var page = await OpenAsync(browser, "/rich-text-marks");
        var notes = await SetAsync(page, "<p>Old</p>", "Old");
        await notes.PressAsync("ControlOrMeta+a");

        await PasteAsync(notes, Html, "Press Esc or run away, x y z w");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(expected);

        // A value from .NET shows with its marks, and an edit sends them back as they were.
        await SetAsync(page, "<p><kbd>Tab</kbd></p>", "Tab");
        await Expect(notes.Locator("kbd")).ToHaveTextAsync("Tab");
        await CaretAsync(notes, 0, 3);
        await page.Keyboard.TypeAsync("!");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><kbd>Tab!</kbd></p>");
    }
}
