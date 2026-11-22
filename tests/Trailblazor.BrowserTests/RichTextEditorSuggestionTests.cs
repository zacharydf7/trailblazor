using Trailblazor.Testing.Playwright;
using static Trailblazor.BrowserTests.RichTextEditorTests;

namespace Trailblazor.BrowserTests;

public class RichTextEditorSuggestionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Ada = "<span data-mention=\"42\" data-trigger=\"@\">@Ada Lovelace</span>";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldOpenUnderTheTriggerAndPutTheChoiceIn_WhenAMentionIsTypedAndEnterIsPressed(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });

        await page.Keyboard.TypeAsync(" @ad");

        await Expect(list.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Ada LovelaceMathematics", "Adam Ant"]);
        var first = list.GetByRole(AriaRole.Option, new() { Name = "Ada Lovelace" });
        await Expect(first).ToHaveAttributeAsync("data-active", "");
        Assert.Equal(await first.GetAttributeAsync("id"), await notes.GetAttributeAsync("aria-activedescendant"));
        // The list opens under the line the trigger is on, and starts near the trigger.
        var (typed, shown) = ((await notes.BoundingBoxAsync())!, (await list.BoundingBoxAsync())!);
        Assert.InRange(shown.Y, typed.Y + 10, typed.Y + typed.Height + 10);
        Assert.InRange(shown.X, typed.X, typed.X + 80);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Adam Ant" }))
            .ToHaveAttributeAsync("data-active", "");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("Enter");

        // The mention goes in for what was typed, with a space after it to type on, and Enter started no paragraph.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi {Ada}&nbsp;</p>");
        await Expect(list).ToBeHiddenAsync();
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Ada Lovelace=42");
        Assert.Null(await notes.GetAttributeAsync("aria-activedescendant"));
        await page.Keyboard.TypeAsync("ok");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>Hi {Ada} ok</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldPutTheChoiceIn_WhenASuggestionIsClickedOrTabIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        var notes = await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });

        await page.Keyboard.TypeAsync(" @a");
        var adam = list.GetByRole(AriaRole.Option, new() { Name = "Adam Ant" });
        // The pointer makes the suggestion under it the active one, as the arrow keys do.
        await adam.HoverAsync();
        await Expect(adam).ToHaveAttributeAsync("data-active", "");
        await adam.ClickAsync();
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Adam Ant=7");
        // The click left the focus in the editor, and a tag's trigger suggests tags.
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("#tr");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveTextAsync(["trails", "trees"]);
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Adam Ant=7, #trails=t1");
        await Expect(notes).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldStayClosedUntilTheTextChanges_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(list).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowRight");

        // Moving the caret away and back doesn't bring the list back, however long a search would take; more text
        // does.
        await page.WaitForTimeoutAsync(300);
        await Expect(list).ToBeHiddenAsync();
        await page.Keyboard.TypeAsync("d");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
        // With the list open, Enter chooses; it starts a paragraph again once the list has gone.
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi @ad</p><p><br></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldNotOpen_WhenTheTriggerIsInsideAWordOrTheCaretLeavesIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });

        await page.Keyboard.TypeAsync("@a");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi@a</p>");
        await page.WaitForTimeoutAsync(300);
        await Expect(list).ToBeHiddenAsync();
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(list).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldClose_WhenTheFocusLeavesTheEditor(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Textbox, new() { Name = "Source" }).FocusAsync();

        await Expect(list).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldPutInTheFirstOfTheLatest_WhenEnterIsPressedBeforeTheyLand(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Ada Lovelace" }))
            .ToHaveAttributeAsync("data-active", "");

        // The search for "adam" is slow, so Enter comes while the list still shows the suggestions for a shorter
        // query.
        await page.Keyboard.TypeAsync("dam");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Adam Ant=7");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldPutTheChoiceInBeforeWhatIsTypedNext_WhenTextFollowsEnterBeforeTheyLand(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Ada Lovelace" }))
            .ToHaveAttributeAsync("data-active", "");

        // The search for "adam" is slow. Keys count in the order pressed, so what is typed after Enter comes after
        // the mention, and the mention's own space stands in for the space typed first.
        await page.Keyboard.TypeAsync("dam");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync(" said");
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Adam Ant=7");
        await page.Keyboard.TypeAsync("!");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(
            "<p>Hi <span data-mention=\"7\" data-trigger=\"@\">@Adam Ant</span> said!</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldDropTheWaitingChoice_WhenTheQueryIsEditedTheCaretMovesBackOrAParagraphIsStarted(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Ada Lovelace" }))
            .ToHaveAttributeAsync("data-active", "");

        // The search for "adam" is slow. A letter of the query deleted while Enter waits drops the choice, and the
        // suggestions for "ada" just show.
        await page.Keyboard.TypeAsync("dam");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);

        // The caret moved back into the query drops it too.
        await page.Keyboard.TypeAsync("m");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
        await page.WaitForTimeoutAsync(700);
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi @adam</p>");

        // So does the caret leaving the paragraph: the list closes when " x" is typed, so the second Enter isn't the
        // list's and starts a new paragraph.
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync(" x");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForTimeoutAsync(700);

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi @adam x</p><p><br></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldNotOpen_WhenTextIsTypedRightAfterAMention(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @bo");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Enter");

        // The mention's own text starts with a trigger, but it isn't a mention being typed: with the caret right
        // after it, nothing is searched for, where a search for "Bo" would find Bo again.
        await page.Keyboard.PressAsync("Backspace");
        await page.WaitForTimeoutAsync(300);
        await Expect(list).ToBeHiddenAsync();
        await page.Keyboard.TypeAsync("x");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(
            "<p>Hi <span data-mention=\"2\" data-trigger=\"@\">@Bo</span>x</p>");
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Bo=2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mentions_ShouldBeReportedAgain_WhenAChosenMentionIsDeletedAndTheChoiceUndone(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @bo");
        await Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Bo=2");

        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Backspace");

        // The space goes, then the mention whole, and the app hears that it has none.
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Hi&nbsp;</p>");
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("mentions")).ToHaveTextAsync("@Bo=2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestions_ShouldCloseAndTellTheApp_WhenTheSearchFails(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @bo");
        await Expect(list).ToBeVisibleAsync();

        await page.Keyboard.TypeAsync("om");

        await Expect(page.GetByTestId("failed")).ToHaveTextAsync("1");
        await Expect(list).ToBeHiddenAsync();
        await page.Tb().Announcements.ExpectAnnouncedAsync("Couldn't load suggestions.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldWaitForTheLatestSuggestions_WhenTheListAlreadyShowsTheItemForLessOfTheText(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-mentions");
        await StartAsync(page);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Notes" });
        await page.Keyboard.TypeAsync(" @a");
        await Expect(list.GetByRole(AriaRole.Option, new() { Name = "Adam Ant" })).ToBeVisibleAsync();

        // The search for "adam" is slow, so as the helper finishes typing the list still shows Adam Ant for "a".
        await page.Tb().RichTextEditor("Notes").MentionAsync("dam", "Adam Ant");

        Assert.Equal("@Adam Ant=7", await page.GetByTestId("mentions").TextContentAsync());
    }

    public static TheoryData<string, string> Paces => new(
        from browser in AllBrowsers.Names from query in new[] { "", "?slow=1" } select (browser, query));

    // With slow in the address, the app takes a while over each value and each change of mentions, and the editor
    // sends a value for each key typed, so a wait for one commit would end too soon.
    [Theory, MemberData(nameof(Paces))]
    public async Task Helper_ShouldMentionTheItemAndWaitForTheApp_WhenMentionAsyncIsCalled(string browser, string query)
    {
        var page = await OpenAsync(browser, $"/rich-text-mentions{query}");
        await SetAsync(page, "<p>First line</p><p>Hi</p>", "Hi");

        await page.Tb().RichTextEditor("Notes").MentionAsync(" @ad", "Adam Ant");

        // The helper returns once the app has the value and has heard of its mentions, so both read at once.
        Assert.Equal("<p>First line</p><p>Hi <span data-mention=\"7\" data-trigger=\"@\">@Adam Ant</span>&nbsp;</p>",
            await page.GetByTestId("notes").TextContentAsync());
        Assert.Equal("@Adam Ant=7", await page.GetByTestId("mentions").TextContentAsync());
    }

    // The notes hold "Hi", with the caret after it and the suggestions ready.
    private static async Task<ILocator> StartAsync(IPage page)
    {
        var notes = await SetAsync(page, "<p>Hi</p>", "Hi");
        await Expect(page.Locator("[data-part=mention-list]")).ToHaveAttributeAsync("data-ready", "");
        await CaretAsync(notes, 0, 2);
        return notes;
    }
}
