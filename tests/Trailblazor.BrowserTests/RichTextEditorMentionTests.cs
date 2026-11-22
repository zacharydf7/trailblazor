using System.Text.RegularExpressions;
using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

// Offsets count a mention's text character by character, though the editor treats it as one unit: "Hi " is 3
// characters, and "@Ada" after it runs from 3 to 7.
public class RichTextEditorMentionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Ada = "<span data-mention=\"42\" data-trigger=\"@\">@Ada</span>";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldStayWhole_WhenTextIsTypedOnEachSideOfIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        await CaretAsync(notes, 0, 3);
        await page.Keyboard.TypeAsync("x");
        await CaretAsync(notes, 0, 8);
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi x{Ada}y!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldTakeTextBesideIt_WhenItIsAllItsParagraphHolds(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>{Ada}</p>", "@Ada");

        // Backspace takes a mention that is all its paragraph holds, and what is typed next is plain text.
        await CaretAsync(notes, 0, 4);
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.TypeAsync("b");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>b</p>");
        await SetAsync(page, $"<p>{Ada}</p>", "@Ada");
        await CaretAsync(notes, 0, 4);
        await page.Keyboard.TypeAsync(" ok");
        await CaretAsync(notes, 0, 0);
        await page.Keyboard.TypeAsync("a ");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>a {Ada} ok</p>");
        await CaretAsync(notes, 0, 6);
        await page.Keyboard.PressAsync("Enter");

        // Enter right after the mention keeps it in the first paragraph.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>a {Ada}</p><p>&nbsp;ok</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldGoWhole_WhenBackspaceOrDeleteMeetsIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        await CaretAsync(notes, 0, 7);
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi !</p>");
        // Undo brings it back with its id.
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi {Ada}!</p>");
        await CaretAsync(notes, 0, 3);
        await page.Keyboard.PressAsync("Delete");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi !</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldGoWithTheRange_WhenASelectionThatCoversItIsDeletedOrTypedOver(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        // Selected with the keys, as a user would: a browser takes a selection set by script out of a mention's text.
        await CaretAsync(notes, 0, 1);
        await PressAsync(page, "Shift+ArrowRight", times: 3);
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>H!</p>");
        await SetAsync(page, $"<p>Hi {Ada} there</p>", "Hi @Ada there");
        await CaretAsync(notes, 0, 2);
        await PressAsync(page, "Shift+ArrowRight", times: 3);
        await page.Keyboard.TypeAsync("-");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi-there</p>");
    }

    private static async Task PressAsync(IPage page, string key, int times)
    {
        for (var press = 0; press < times; press++)
        {
            await page.Keyboard.PressAsync(key);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldBeCrossedInOneStep_WhenAnArrowKeyMeetsIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        await CaretAsync(notes, 0, 3);
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi {Ada}x!</p>");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi y{Ada}x!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldKeepItsIdAndTakeTheFormat_WhenTheTextAroundItIsMadeBold(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        await SelectAsync(notes, 0, 0, 0, 8);
        await page.Keyboard.PressAsync("ControlOrMeta+b");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(
            $"<p><strong>Hi </strong><strong>{Ada}</strong><strong>!</strong></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldStayOutOfTheLink_WhenTheTextAroundItIsLinked(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        await SelectAsync(notes, 0, 0, 0, 8);
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveAccessibleNameAsync("Add link");
        await page.Keyboard.TypeAsync("example.com");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(
            $"<p><a href=\"https://example.com\">Hi </a>{Ada}<a href=\"https://example.com\">!</a></p>");
        // The editor wrote that itself: had the server taken a link off the mention and sent the value back, the
        // editor would have loaded it and lost the step that undo takes back.
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi {Ada}!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldTakeTheLetterOnItsNearerSide_WhenTheCaretIsSetInsideItsText(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");
        // A script or an assistive tool can put the caret where the keys and the pointer can't.
        const string CaretInside = """
            (mention, offset) => {
                mention.closest('[contenteditable=true]').focus();
                getSelection().setBaseAndExtent(mention.firstChild, offset, mention.firstChild, offset);
            }
            """;

        await notes.Locator("[data-mention]").EvaluateAsync(CaretInside, 1);
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi x{Ada}!</p>");
        await notes.Locator("[data-mention]").EvaluateAsync(CaretInside, 3);
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi x{Ada}y!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldBecomePlainText_WhenSomethingElseChangesItsText(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada}!</p>", "Hi @Ada!");

        // An extension or an input method can write into the page past the editor's own keys.
        await notes.Locator("[data-mention]").EvaluateAsync("mention => mention.firstChild.data = '@Ad'");
        await CaretAsync(notes, 0, 0);
        await page.Keyboard.TypeAsync("x");

        // What is left is no longer the item the id names.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>xHi @Ad!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldArriveWhole_WhenItsHtmlIsPasted(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, "<p>ab</p>", "ab");

        await CaretAsync(notes, 0, 1);
        // As the editor's own copy writes one, with the attribute that keeps it from being edited in place.
        await PasteAsync(notes, "<span data-mention=\"7\" data-trigger=\"#\" contenteditable=\"false\">#trails</span>",
            "#trails");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(
            "<p>a<span data-mention=\"7\" data-trigger=\"#\">#trails</span>b</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldArriveAsItsText_WhenItIsPastedWhereMentionsAreNotKept(string browser)
    {
        // This page's editor has no MentionTriggers, as an app that doesn't use mentions.
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>ab</p>", "ab");

        await CaretAsync(notes, 0, 1);
        await PasteAsync(notes, "<span data-mention=\"7\" data-trigger=\"#\">#trails</span>", "#trails");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a#trailsb</p>");
        await page.Keyboard.PressAsync("Backspace");

        // It is text like any other: Backspace takes one letter of it.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a#trailb</p>");
    }

    // The engine reads pasted mentions itself, so it must read the sample as the .NET reader does.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldReadMentionsAsDotNetDoes_WhenTheCorpusSampleIsPastedWhereMentionsAreKept(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = Editor(page, "Notes");
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "RichTextCorpus", "mentions.html"));
        var expected = RichTextDocument.Parse(html, RichTextSchema.Default with { Mentions = true }).ToHtml();
        Assert.Contains("data-mention=\"7\"", expected);

        await CaretAsync(notes, 0, 0);
        await notes.EvaluateAsync(
            "editor => editor.addEventListener('tb-rich-text-change', event => window.sent = event.detail.html)");
        await PasteAsync(notes, html, "ignored");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(expected);
        Assert.Equal(expected, await page.EvaluateAsync<string>("() => window.sent"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mention_ShouldKeepTheCaretOutOfIt_WhenItIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await SetAsync(page, $"<p>Hi {Ada} there</p>", "Hi @Ada there");

        await notes.Locator("[data-mention]").ClickAsync();
        await page.Keyboard.TypeAsync("x");

        // The letter lands on one side of the mention or the other, and the mention is still whole.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(new Regex($"^<p>Hi (x{Ada}|{Ada}x) there</p>$"));
    }
}
