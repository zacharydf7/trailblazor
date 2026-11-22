using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

public class RichTextEditorBlockTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Callout = "<p data-tb-block=\"callout\">";

    private static ILocator Tool(ILocator notes, string name) =>
        notes.Locator("xpath=ancestor::*[@data-part='editor']").Locator("[data-part=toolbar]")
            .GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // Opens the page with two paragraphs in the notes and the caret at the end of the first.
    private async Task<(IPage Page, ILocator Notes)> StartAsync(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-blocks");
        var notes = await SetAsync(page, "<p>Mind the gap</p><p>Next</p>", "Next");
        await CaretAsync(notes, 0, 12);
        return (page, notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Block_ShouldTurnTheParagraphIntoItsKindAndBack_WhenItsButtonOrItsKeysArePressed(string browser)
    {
        var (page, notes) = await StartAsync(browser);

        // The paragraph the caret is in becomes a paragraph that names the kind, and the button says it is on.
        await Tool(notes, "Callout").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"{Callout}Mind the gap</p><p>Next</p>");
        await Expect(Tool(notes, "Callout")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(notes.Locator("p[data-tb-block=callout]")).ToHaveTextAsync("Mind the gap");

        // A format rewrites its text in place, as in a plain paragraph: the same element still names the kind.
        var paragraph = await notes.Locator("p[data-tb-block=callout]").ElementHandleAsync();
        await SelectAsync(notes, 0, 0, 0, 12);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"{Callout}<strong>Mind the gap</strong></p><p>Next</p>");
        await Expect(notes.Locator("p[data-tb-block=callout] strong")).ToHaveTextAsync("Mind the gap");
        Assert.True(await paragraph.EvaluateAsync<bool>("element => element.isConnected"));
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"{Callout}Mind the gap</p><p>Next</p>");
        await CaretAsync(notes, 0, 12);

        // Typing in it keeps the kind, and its keys turn it back into a plain paragraph.
        await page.Keyboard.TypeAsync("!");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"{Callout}Mind the gap!</p><p>Next</p>");
        await page.Keyboard.PressAsync("Alt+Shift+C");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Mind the gap!</p><p>Next</p>");
        await Expect(Tool(notes, "Callout")).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Block_ShouldGoOnToTheNextParagraphAndEndAtAnEmptyOne_WhenEnterAndBackspaceArePressed(
        string browser)
    {
        var (page, notes) = await StartAsync(browser);
        await Tool(notes, "Callout").ClickAsync();

        // Enter goes on in the same kind, as in a quote, and Enter in an empty one ends it.
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("More");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("After");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"{Callout}Mind the gap</p>{Callout}More</p><p>After</p><p>Next</p>");

        // Backspace at the start of one takes the kind away before it joins the paragraph above.
        await CaretAsync(notes, 1, 0);
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"{Callout}Mind the gap</p><p>More</p><p>After</p><p>Next</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Block_ShouldBeReadAsDotNetReadsIt_WhenPastedOrSetFromDotNet(string browser)
    {
        // The editor's kind is kept. A p that names another kind and a div that names this one are plain paragraphs,
        // and a p that names another kind is still the list item a word processor made it.
        const string Html = "<p data-tb-block=\"callout\">Mind the <b>gap</b></p><p data-tb-block=\"other\">x</p>"
            + "<div data-tb-block=\"callout\">y</div>"
            + "<p data-tb-block=\"other\" style=\"mso-list:l0 level1 lfo1\">z</p>";
        var expected = RichTextDocument.Parse(Html, new RichTextSchema { CustomBlocks = ["callout"] }).ToHtml();
        Assert.Equal($"{Callout}Mind the <strong>gap</strong></p><p>x</p><p>y</p><ul><li>z</li></ul>", expected);
        var page = await OpenAsync(browser, "/rich-text-blocks");
        var notes = await SetAsync(page, "<p>Old</p>", "Old");
        await notes.PressAsync("ControlOrMeta+a");

        await PasteAsync(notes, Html, "Mind the gap x y z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(expected);

        // A value from .NET shows as its kind, and an edit sends it back as it was.
        await SetAsync(page, $"{Callout}Stop</p>", "Stop");
        await Expect(notes.Locator("p[data-tb-block=callout]")).ToHaveTextAsync("Stop");
        await CaretAsync(notes, 0, 4);
        await page.Keyboard.TypeAsync("!");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"{Callout}Stop!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Block_ShouldBeOneOfTheSlashCommands_WhenTheEditorHasThem(string browser)
    {
        var (page, _) = await StartAsync(browser);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });

        await page.Keyboard.TypeAsync(" /cal");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Callout"]);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"{Callout}Mind the gap&nbsp;</p><p>Next</p>");

        // In a callout the list offers Paragraph, and not the callout again.
        await page.Keyboard.TypeAsync("/");
        await Expect(list.GetByRole(AriaRole.Option).First).ToHaveTextAsync("Paragraph");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Callout" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Block_ShouldBePutInUnderTheLine_WhenAnAppsSlashCommandHasOneInItsHtml(string browser)
    {
        var (page, _) = await StartAsync(browser);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });

        await page.Keyboard.TypeAsync(" /warn");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Warning"]);
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"<p>Mind the gap&nbsp;</p>{Callout}Careful</p><p>Next</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Block_ShouldBeWrittenAsItsParagraph_WhenItsNameIsOneEveryScriptObjectHas(string browser)
    {
        const string Named = "<p data-tb-block=\"constructor\">";
        var (page, notes) = await StartAsync(browser);

        await Tool(notes, "Constructor").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"{Named}Mind the gap</p><p>Next</p>");
        await Expect(Tool(notes, "Constructor")).ToHaveAttributeAsync("aria-pressed", "true");

        // Enter goes on in the kind, as in any block of the app's own, and doesn't take it for a heading.
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("More");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync($"{Named}Mind the gap</p>{Named}More</p><p>Next</p>");
    }
}
