using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

public class RichTextEditorSlashTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator List(IPage page) => page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });

    // Opens the page with the notes holding this text and the caret at its end, once slash commands work.
    private async Task<(IPage Page, ILocator Notes)> StartAsync(string browser, string text = "Plan",
        string mode = "")
    {
        var page = await OpenAsync(browser, $"/rich-text-slash{mode}");
        var notes = await SetAsync(page, $"<p>{text}</p>", text);
        await Expect(page.Locator("[data-part=mention-list][data-ready]")).ToHaveCountAsync(3);
        await CaretAsync(notes, 0, text.Length);
        return (page, notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slash_ShouldListTheCommandsAndTurnTheParagraphIntoTheOneChosen_WhenTypedAndEnterIsPressed(
        string browser)
    {
        var (page, notes) = await StartAsync(browser);

        await page.Keyboard.TypeAsync(" /");

        // The editor's own commands follow its blocks, under their buttons' names, and the app's come after.
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Heading 1", "Heading 2", "Heading 3",
            "Bulleted list", "Horizontal line", "Table", "SignatureYour name and role", "Stamp"]);
        await page.Keyboard.TypeAsync("hea");
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Heading 1", "Heading 2", "Heading 3"]);
        // The list is busy until the answer for the last key is in, and starts again from its first item then.
        await Expect(List(page)).Not.ToHaveAttributeAsync("aria-busy", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(List(page).GetByRole(AriaRole.Option, new() { Name = "Heading 2" }))
            .ToHaveAttributeAsync("data-active", "");
        await page.Keyboard.PressAsync("Enter");

        // The slash and what was typed after it are gone, and the paragraph is the heading.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<h2>Plan&nbsp;</h2>");
        await Expect(List(page)).ToBeHiddenAsync();
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("B");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<h2>Plan B</h2>");

        // The command for the kind the block already is leaves it so, where that kind's button would turn it back.
        await page.Keyboard.TypeAsync(" /2");
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Heading 2"]);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<h2>Plan B&nbsp;</h2>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slash_ShouldPutInARuleAListOrATable_WhenThatCommandIsChosen(string browser)
    {
        var (page, _) = await StartAsync(browser);

        await page.Keyboard.TypeAsync(" /");
        await List(page).GetByRole(AriaRole.Option, new() { Name = "Horizontal line" }).ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Plan&nbsp;</p><hr><p><br></p>");

        await page.Keyboard.TypeAsync("/bul");
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Bulleted list"]);
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("pack");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Plan&nbsp;</p><hr><ul><li>pack</li></ul>");

        // Tab runs a command as Enter does, and a table goes in with the caret in its first cell.
        await page.Keyboard.TypeAsync(" /tab");
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Table"]);
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToContainTextAsync("</ul><table><thead><tr><th scope=\"col\">x</th>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public Task Slash_ShouldRunTheAppsCommand_WhenItIsFoundByItsNameOrItsKeywords(string browser) =>
        RunTheAppsCommandsAsync(browser, "");

    // On WebAssembly .NET hears of a choice before the script has taken the slash out; on a server, after.
    [Theory, ClassData(typeof(AllBrowsers))]
    public Task Slash_ShouldRunTheAppsCommand_WhenThePageRunsOnWebAssembly(string browser) =>
        RunTheAppsCommandsAsync(browser, "?mode=webassembly");

    private async Task RunTheAppsCommandsAsync(string browser, string mode)
    {
        var (page, _) = await StartAsync(browser, mode: mode);

        // A command is found by a keyword too, and its HTML goes in at the caret, read as pasted text is.
        await page.Keyboard.TypeAsync(" /off");
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["SignatureYour name and role"]);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>Plan <strong>Ana Lopez</strong>, trail crew</p>");

        // A command with nothing to put in still takes its slash out, and the app hears of it.
        await page.Keyboard.TypeAsync(" /stamp");
        await Expect(List(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Stamp"]);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("stamped")).ToHaveTextAsync("Stamped: 1");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>Plan <strong>Ana Lopez</strong>, trail crew&nbsp;</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slash_ShouldStayAsText_WhenEscapeIsPressedOrNoCommandMatches(string browser)
    {
        var (page, _) = await StartAsync(browser);

        await page.Keyboard.TypeAsync(" /");
        await Expect(List(page)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(List(page)).ToBeHiddenAsync();
        await page.Keyboard.TypeAsync("xyz");
        await page.Keyboard.PressAsync("Enter");

        // With no command to run, Enter starts a paragraph as usual.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Plan /xyz</p><p><br></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slash_ShouldBePlainText_WhenItIsTypedInsideAWordOrWhereSlashCommandsAreOff(string browser)
    {
        var (page, _) = await StartAsync(browser);

        await page.Keyboard.TypeAsync("/b");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Plan/b</p>");
        await Expect(List(page)).ToBeHiddenAsync();

        // An editor that suggests mentions, with slash commands off, asks for nothing at a slash.
        await Editor(page, "Places").ClickAsync();
        await page.Keyboard.TypeAsync("/hea @hea");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Places" }).GetByRole(AriaRole.Option))
            .ToHaveTextAsync(["Hearth Lake"]);
        await Expect(page.GetByTestId("asked")).ToHaveTextAsync("Asked: @");
        await Expect(page.GetByTestId("places")).ToHaveTextAsync("<p>/hea @hea</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slash_ShouldSuggestMentionsAsAnyTriggerDoes_WhenItIsAMentionTriggerAndSlashCommandsAreOff(
        string browser)
    {
        var (page, _) = await StartAsync(browser);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Paths" });

        await Editor(page, "Paths").ClickAsync();
        await page.Keyboard.TypeAsync("/hea");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Hearth Lake"]);
        await Expect(list).Not.ToHaveAttributeAsync("aria-busy", "true");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("paths")).ToHaveTextAsync(
            "<p><span data-mention=\"9\" data-trigger=\"/\">/Hearth Lake</span>&nbsp;</p>");
    }
}
