using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RichTextEditorTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    public static TheoryData<string> Chromium => new(AllBrowsers.Names.Where(name => name == "chromium"));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldLandAsShownAndTakeTheMarksBefore_WhenTypingInsideAndAfterAMarkedRun(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p><strong>bold</strong> plain</p>", "bold plain");

        await CaretAsync(notes, 0, 2);
        await page.Keyboard.TypeAsync("x");
        await CaretAsync(notes, 0, 5);
        await page.Keyboard.TypeAsync("er");
        await CaretAsync(notes, 0, 8);
        await page.Keyboard.TypeAsync("<&");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>boxlder</strong> &lt;&amp;plain</p>");
        await Expect(notes).ToHaveTextAsync("boxlder <&plain");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldPlaceTheTextAtTheCaret_WhenTheSameCharacterRepeatsAcrossAMarkBoundary(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p><strong>a</strong>a</p>", "aa");

        await CaretAsync(notes, 0, 1);
        await page.Keyboard.TypeAsync("a");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>aa</strong>a</p>");
        await page.Keyboard.PressAsync("Backspace");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>a</strong>a</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Formats_ShouldApplyAndRemove_WhenEachButtonIsPressedOverARange(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>one two</p>", "one two");

        foreach (var (name, tag) in new[]
        {
            ("Bold", "strong"), ("Italic", "em"), ("Underline", "u"), ("Strikethrough", "s"), ("Subscript", "sub"),
            ("Superscript", "sup"),
        })
        {
            await SelectAsync(notes, 0, 4, 0, 7);
            await Tool(notes, name).ClickAsync();
            await Expect(page.GetByTestId("notes")).ToHaveTextAsync($"<p>one <{tag}>two</{tag}></p>");
            await Expect(Tool(notes, name)).ToHaveAttributeAsync("aria-pressed", "true");
            await Tool(notes, name).ClickAsync();
            await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one two</p>");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Formats_ShouldShowMixedAndApplyToAll_WhenAPartlyFormattedSelectionIsMadeWithTheKeyboard(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p><strong>one</strong> two</p>", "one two");

        await CaretAsync(notes, 0, 0);
        await page.Keyboard.PressAsync("Shift+End");
        await Expect(Tool(notes, "Bold")).ToHaveAttributeAsync("aria-pressed", "mixed");
        await Tool(notes, "Bold").ClickAsync();

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>one two</strong></p>");
        await Expect(Tool(notes, "Bold")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcuts_ShouldToggleEachFormat_WhenPressedOverARangeOrBeforeTyping(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>word</p>", "word");

        await SelectAsync(notes, 0, 0, 0, 4);
        foreach (var key in new[] { "b", "i", "u", "Shift+X" })
        {
            await page.Keyboard.PressAsync($"ControlOrMeta+{key}");
        }

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong><em><u><s>word</s></u></em></strong></p>");
        await CaretAsync(notes, 0, 4);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await page.Keyboard.PressAsync("ControlOrMeta+.");
        await page.Keyboard.TypeAsync("2");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p><strong><em><u><s>word</s></u></em></strong><em><u><s><sup>2</sup></s></u></em></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Formats_ShouldOfferOnlyThoseAllowed_WhenTheEditorAllowsSome(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var formula = Editor(page, "Formula");

        await CaretAsync(formula, 0, 0);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await page.Keyboard.TypeAsync("H");
        await page.Keyboard.PressAsync("ControlOrMeta+,");
        await page.Keyboard.TypeAsync("2");
        await PasteAsync(formula, "<b>O</b>", "O");

        await Expect(page.GetByTestId("formula")).ToHaveTextAsync("<p>H<sub>2</sub>O</p>");
        await Expect(formula.Locator("xpath=ancestor::*[@data-part='editor']").GetByRole(AriaRole.Button))
            .ToHaveCountAsync(3);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paragraphs_ShouldSplitBreakAndJoin_WhenEnterShiftEnterBackspaceAndDeleteArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>onetwo</p>", "onetwo");

        await CaretAsync(notes, 0, 3);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one</p><p>two</p>");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>onetwo</p>");
        await page.Keyboard.PressAsync("Shift+Enter");
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one<br>xtwo</p>");
        await page.Keyboard.PressAsync("Enter");
        await CaretAsync(notes, 0, 5);
        await page.Keyboard.PressAsync("Delete");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one<br>xtwo</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Deleting_ShouldMergeParagraphsAndLeaveAnEmptyValue_WhenRangesAndEverythingAreDeleted(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>one</p><p>two</p><p><strong>three</strong></p>", "one");

        await SelectAsync(notes, 0, 1, 2, 2);
        await page.Keyboard.TypeAsync("X");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>oX<strong>ree</strong></p>");
        await page.Keyboard.PressAsync("ControlOrMeta+a");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("");
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>y</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Undo_ShouldUndoAndRedoInWordSizedStepsAndRestoreTheSelection_WhenItsShortcutsArePressed(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = Editor(page, "Notes");

        await CaretAsync(notes, 0, 0);
        await page.Keyboard.TypeAsync("one two");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one&nbsp;</p>");
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one two</p>");
        await SelectAsync(notes, 0, 0, 0, 3);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await CaretAsync(notes, 0, 7);
        await page.Keyboard.PressAsync("ControlOrMeta+z");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one two</p>");
        Assert.Equal("one", await page.EvaluateAsync<string>("() => getSelection().toString()"));
        await page.Keyboard.PressAsync("ControlOrMeta+y");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>one</strong> two</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldKeepOnlyTheFormats_WhenPastingFromWordAndHostileHtml(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = Editor(page, "Notes");

        await CaretAsync(notes, 0, 0);
        await PasteAsync(notes, "<p class=MsoNormal><b>Bold</b> <span style=\"font-style:italic\">it</span></p>"
            + "<img src=x onerror=\"window.pwned = 1\"><script>window.pwned = 1</script><h2>Next</h2>", "ignored");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p><strong>Bold</strong> <em>it</em></p><h2>Next</h2>");
        Assert.Equal(0, await notes.Locator("img, script").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => window.pwned === undefined"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldPastePlainText_WhenTheClipboardHasOnlyTextOrShiftIsHeld(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p><strong>ab</strong></p>", "ab");

        await CaretAsync(notes, 0, 1);
        await PasteAsync(notes, null, "one\ntwo");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p><strong>aone</strong></p><p><strong>twob</strong></p>");
        await CaretAsync(notes, 0, 0);
        await notes.DispatchEventAsync("keydown",
            new { key = "V", ctrlKey = true, metaKey = true, shiftKey = true, bubbles = true });
        await PasteAsync(notes, "<em>x</em>", "x");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p><strong>xaone</strong></p><p><strong>twob</strong></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drop_ShouldMoveDraggedTextAndCleanDroppedHtml_WhenTextIsDropped(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>alpha beta</p>", "alpha beta");

        await SelectAsync(notes, 0, 6, 0, 10);
        await notes.DispatchEventAsync("dragstart", new { bubbles = true });
        await DropAtStartAsync(notes, null);
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>betaalpha&nbsp;</p>");
        await DropAtStartAsync(notes, "<b>x</b><script>window.pwned = 1</script>");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>x</strong>betaalpha&nbsp;</p>");
        Assert.True(await page.EvaluateAsync<bool>("() => window.pwned === undefined"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keyboard_ShouldLeaveTheEditorOnTabAndMoveAlongTheToolbarOnArrows_WhenNavigating(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = Editor(page, "Notes");

        await CaretAsync(notes, 0, 0);
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Source" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Tool(notes, "Bold")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tool(notes, "Italic")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");

        await Expect(Tool(notes, "Clear formatting")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldFocusTheEditor_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Locator("label", new() { HasText = "Summary" }).ClickAsync();

        await Expect(Editor(page, "Summary")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Summary_ShouldSendTheValue_WhenFocusLeavesTheEditor(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var summary = Editor(page, "Summary");

        await CaretAsync(summary, 0, 0);
        await page.Keyboard.TypeAsync("Short");
        await Expect(page.GetByTestId("summary")).ToBeEmptyAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("summary")).ToHaveTextAsync("<p>Short</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnly_ShouldKeepItsTextButLetItBeSelected_WhenTheUserTriesToEditIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var approved = Editor(page, "Approved");

        await CaretAsync(approved, 0, 4);
        await page.Keyboard.TypeAsync("x");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Backspace");
        await PasteAsync(approved, null, "pasted");
        await page.Keyboard.PressAsync("ControlOrMeta+a");

        await Expect(approved).ToHaveTextAsync("Read only");
        Assert.Equal("Read only", await page.EvaluateAsync<string>("() => getSelection().toString()"));
        await Expect(approved.Locator("xpath=ancestor::*[@data-part='editor']").GetByRole(AriaRole.Button).First)
            .ToBeDisabledAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadBack_ShouldKeepEachWordsMarks_WhenTheBrowserRewritesSeveralTextNodesAtOnce(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>teh <strong>bgi</strong> cat</p>", "teh bgi cat");

        await notes.EvaluateAsync("""
            editor => {
                editor.querySelector('p').firstChild.data = 'the ';
                editor.querySelector('strong').firstChild.data = 'big';
            }
            """);

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>the <strong>big</strong> cat</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldKeepEveryCharacterAndTheCaret_WhenEachChangeIsSlowToComeBack(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var live = Editor(page, "Live");
        const string text = "The quick brown fox jumps over the lazy dog";

        await CaretAsync(live, 0, 0);
        await page.Keyboard.TypeAsync(text[..1]);
        // An older value sent back to the script would render the paragraphs again, replacing this one.
        await live.EvaluateAsync("editor => window.paragraph = editor.firstElementChild");
        await page.Keyboard.TypeAsync(text[1..]);

        await Expect(page.GetByTestId("live")).ToHaveTextAsync($"<p>{text}</p>", new() { Timeout = 15_000 });
        await Expect(live).ToHaveTextAsync(text);
        Assert.Equal(text.Length, await CaretOffsetAsync(live));
        Assert.True(await page.EvaluateAsync<bool>("() => window.paragraph.isConnected"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Value_ShouldRenderAndLeaveTheCaretAlone_WhenSetFromDotNetOrEchoedBack(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>Set <b>from</b> code</p>", "Set from code");

        await Expect(notes.Locator("strong")).ToHaveTextAsync("from");
        await CaretAsync(notes, 0, 13);
        await page.Keyboard.TypeAsync("!");
        await page.Keyboard.PressAsync("ArrowLeft");
        await notes.EvaluateAsync("editor => window.node = editor.querySelector('p').lastChild");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>Set <strong>from</strong> code!</p>");
        await page.WaitForTimeoutAsync(200);

        Assert.True(await page.EvaluateAsync<bool>("() => window.node.isConnected"));
        Assert.Equal(13, await CaretOffsetAsync(notes));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Save_ShouldGetTheWholeValue_WhenClickedStraightFromAnEditorHoldingTooMuchForOneMessage(
        string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var summary = Editor(page, "Summary");
        // Over 32 KB as UTF-8, with a character that takes two UTF-16 units, so it goes in several parts.
        var text = string.Concat(Enumerable.Repeat("Ünïcode 😀 text ", 3_000));

        await CaretAsync(summary, 0, 0);
        await PasteAsync(summary, null, text);
        await page.GetByRole(AriaRole.Button, new() { Name = "Save summary" }).ClickAsync();

        await Expect(page.GetByTestId("saved")).ToHaveTextAsync($"<p>{text.TrimEnd()}&nbsp;</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldTurnTabsIntoSpacesAndKeepUndo_WhenPlainTextHasTabs(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>start</p>", "start");

        await CaretAsync(notes, 0, 5);
        await PasteAsync(notes, null, "\ta\tb");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>start a b</p>");
        await page.Keyboard.PressAsync("ControlOrMeta+z");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>start</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EnterAsync_ShouldReplaceTheTextAndCommitIt_WhenTheTestingHelperEntersHtml(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var summary = page.Tb().RichTextEditor("Summary");

        await summary.EnterAsync("<p>Old</p>");
        await summary.EnterAsync("<p><b>Hi</b> <span style=\"color:red\">there</span></p>");
        await Expect(page.GetByTestId("summary")).ToHaveTextAsync("<p><strong>Hi</strong> there</p>");
        await Expect(summary.Input).ToHaveTextAsync("Hi there");
        await summary.EnterAsync("");

        await Expect(page.GetByTestId("summary")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldLeaveFocusAlone_WhenTheEditorIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var archived = Editor(page, "Archived");
        await Expect(archived).ToHaveAttributeAsync("data-ready", "");

        await page.Locator("label", new() { HasText = "Archived" }).ClickAsync();

        await Expect(archived).Not.ToBeFocusedAsync();
    }

    public static TheoryData<string, string> CorpusSamples => new(AllBrowsers.Names.SelectMany(browser =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "RichTextCorpus"), "*.html")
            .Where(path => !path.EndsWith(".expected.html", StringComparison.Ordinal))
            .Select(path => (browser, Path.GetFileNameWithoutExtension(path)))));

    // The engine reads pasted HTML itself, so it must read each sample as the .NET reader does.
    [Theory, MemberData(nameof(CorpusSamples))]
    public async Task Paste_ShouldReadTheSampleAsDotNetDoes_WhenACorpusSampleIsPasted(string browser, string sample)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = Editor(page, "Notes");
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "RichTextCorpus", $"{sample}.html"));
        var expected = RichTextDocument.Parse(html).ToHtml();

        await CaretAsync(notes, 0, 0);
        await notes.EvaluateAsync(
            "editor => editor.addEventListener('tb-rich-text-change', event => window.sent = event.detail.html)");
        await PasteAsync(notes, html, "ignored");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(expected);
        Assert.Equal(expected, await page.EvaluateAsync<string>("() => window.sent"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Blocks_ShouldApplyAndRemoveEachKind_WhenEachBlockButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>one</p>", "one");

        foreach (var (name, html) in new[]
        {
            ("Heading 1", "<h1>one</h1>"), ("Heading 2", "<h2>one</h2>"), ("Heading 3", "<h3>one</h3>"),
            ("Quote", "<blockquote><p>one</p></blockquote>"), ("Bulleted list", "<ul><li>one</li></ul>"),
            ("Numbered list", "<ol><li>one</li></ol>"),
        })
        {
            await CaretAsync(notes, 0, 1);
            await Tool(notes, name).ClickAsync();
            await Expect(page.GetByTestId("notes")).ToHaveTextAsync(html);
            await Expect(Tool(notes, name)).ToHaveAttributeAsync("aria-pressed", "true");
            await Tool(notes, name).ClickAsync();
            await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one</p>");
        }

        await Tool(notes, "Heading 2").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<h2>one</h2>");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lists_ShouldIndentOutdentAndLeave_WhenTheButtonsEnterAndBackspaceAreUsed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<ul><li>a</li><li>b</li></ul>", "ab");

        await CaretAsync(notes, 1, 1);
        await Tool(notes, "Indent").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a<ul><li>b</li></ul></li></ul>");
        await Tool(notes, "Outdent").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li><li>b</li></ul>");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li><li>b</li><li><br></li></ul>");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("c");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li><li>b</li></ul><p>c</p>");
        await CaretAsync(notes, 1, 0);
        await page.Keyboard.PressAsync("Backspace");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li></ul><p>b</p><p>c</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Enter_ShouldGoOnAsAParagraph_WhenPressedAtAHeadingsEndOrInAnEmptyQuoteParagraph(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<h2>Title</h2><blockquote><p>q</p></blockquote>", "Title");

        await CaretAsync(notes, 0, 5);
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<h2>Title</h2><p>x</p><blockquote><p>q</p></blockquote>");
        await CaretAsync(notes, 2, 1);
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<h2>Title</h2><p>x</p><blockquote><p>q</p></blockquote><p>y</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldLandInTheRightItem_WhenTypingInANestedListAndTheItemAroundIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<ul><li>a<ul><li>b</li></ul></li><li>c</li></ul>", "abc");

        await CaretAsync(notes, 1, 1);
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a<ul><li>bx</li></ul></li><li>c</li></ul>");
        await CaretAsync(notes, 0, 1);
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>ay<ul><li>bx</li></ul></li><li>c</li></ul>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldKeepTheBlock_WhenPlainParagraphsArePastedIntoAListItemOrHeading(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<ul><li>ab</li></ul><h2>Title</h2>", "abTitle");

        await CaretAsync(notes, 0, 1);
        await PasteAsync(notes, "<p>x</p><p>y</p>", "x\ny");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>ax</li><li>yb</li></ul><h2>Title</h2>");
        await CaretAsync(notes, 1, 2);
        await page.Keyboard.PressAsync("Enter");
        await PasteAsync(notes, "<span>z</span>", "z");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<ul><li>ax</li><li>yb</li><li>z</li></ul><h2>Title</h2>");
        await CaretAsync(notes, 3, 0);
        await PasteAsync(notes, "<span>A</span>", "A");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<ul><li>ax</li><li>yb</li><li>z</li></ul><h2>ATitle</h2>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Code_ShouldToggle_WhenItsButtonOrShortcutIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>run it</p>", "run it");

        await SelectAsync(notes, 0, 4, 0, 6);
        await Tool(notes, "Code").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>run <code>it</code></p>");
        await page.Keyboard.PressAsync("ControlOrMeta+e");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>run it</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Links_ShouldAddUndoEditAndRemoveALink_WhenTheLinkDialogIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>see the map</p>", "see the map");
        var dialog = page.GetByRole(AriaRole.Dialog);
        var address = dialog.GetByRole(AriaRole.Textbox, new() { Name = "Link", Exact = true });

        await SelectAsync(notes, 0, 4, 0, 11);
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await Expect(dialog).ToHaveAccessibleNameAsync("Add link");
        await Expect(address).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("example.com/map");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>see <a href=\"https://example.com/map\">the map</a></p>");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>see the map</p>");
        await page.Keyboard.PressAsync("ControlOrMeta+y");
        await CaretAsync(notes, 0, 6);
        await Tool(notes, "Link").ClickAsync();
        await Expect(dialog).ToHaveAccessibleNameAsync("Edit link");
        await Expect(address).ToHaveValueAsync("https://example.com/map");
        await address.FillAsync("https://example.com/trails");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>see <a href=\"https://example.com/trails\">the map</a></p>");
        await CaretAsync(notes, 0, 11);
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Remove link" }).ClickAsync();

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>see the map</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Links_ShouldInsertLinkedTextThatTypingJoinsOnlyInside_WhenNothingIsSelected(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>Go to now</p>", "Go to now");
        var dialog = page.GetByRole(AriaRole.Dialog);

        await CaretAsync(notes, 0, 6);
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await dialog.GetByRole(AriaRole.Textbox, new() { Name = "Text" }).FillAsync("the map");
        await dialog.GetByRole(AriaRole.Textbox, new() { Name = "Link", Exact = true })
            .FillAsync("https://example.com");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>Go to <a href=\"https://example.com\">the map</a>now</p>");
        await page.Keyboard.TypeAsync(" ");
        await CaretAsync(notes, 0, 9);
        await page.Keyboard.TypeAsync("x");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<p>Go to <a href=\"https://example.com\">thex map</a> now</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Links_ShouldPutTheCaretBack_WhenTheLinkDialogIsCanceled(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>see the map</p>", "see the map");

        await CaretAsync(notes, 0, 3);
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("s");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>sees the map</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldKeepOnlyTheAllowedLinks_WhenTheEditorLimitsSchemesOrHasNoLinks(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        const string html = "<a href=\"http://a.example\">a</a> <a href=\"https://b.example\">b</a> "
            + "<a href=\"javascript:alert(1)\">c</a> <a href=\"/d\">d</a>";
        // What the script sends, before .NET reads it again.
        await page.EvaluateAsync("() => document.addEventListener('tb-rich-text-change', event => "
            + "(window.sent ??= {})[event.target.getAttribute('aria-labelledby')] = event.detail.html)");

        foreach (var (label, expected) in new[]
        {
            ("Citations", "<p>a <a href=\"https://b.example\">b</a> c <a href=\"/d\">d</a></p>"),
            ("Formula", "<p>a b c d</p>"),
        })
        {
            var editor = Editor(page, label);
            await CaretAsync(editor, 0, 0);
            await PasteAsync(editor, html, "ignored");
            var sent = await page.WaitForFunctionAsync("label => window.sent?.[label]",
                await editor.GetAttributeAsync("aria-labelledby"));
            Assert.Equal(expected, await sent.JsonValueAsync<string>());
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Links_ShouldOpenInANewTab_WhenCtrlClickedButNotWhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p><a href=\"/rich-text-editor?opened\">site</a></p>", "site");
        var url = page.Url;

        await notes.GetByText("site").ClickAsync();
        var popup = await page.RunAndWaitForPopupAsync(() =>
            notes.GetByText("site").ClickAsync(new() { Modifiers = [KeyboardModifier.ControlOrMeta] }));

        await popup.WaitForURLAsync("**/rich-text-editor?opened");
        Assert.Equal(url, page.Url);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldGoInFromTheButtonOrDashesAndComeOut_WhenBackspaceOrDeleteMeetsThem(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>ab</p>", "ab");

        await CaretAsync(notes, 0, 1);
        await Tool(notes, "Horizontal line").ClickAsync();
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a</p><hr><p>xb</p>");
        await CaretAsync(notes, 2, 0);
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a</p><p>xb</p>");
        await CaretAsync(notes, 1, 2);
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("---");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("y");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a</p><p>xb</p><hr><p>y</p>");
        await CaretAsync(notes, 1, 2);
        await page.Keyboard.PressAsync("Delete");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a</p><p>xb</p><p>y</p>");
        await CaretAsync(notes, 0, 0);
        await Tool(notes, "Horizontal line").ClickAsync();
        await page.Keyboard.TypeAsync("w");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><br></p><hr><p>wa</p><p>xb</p><p>y</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldKeepTheCaretOffTheRule_WhenTheArrowKeysCrossIt(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>a</p><hr><p>b</p>", "b");

        await CaretAsync(notes, 0, 1);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a</p><hr><p>xb</p>");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>ay</p><hr><p>xb</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldLeaveRoomAboveAndAPlainParagraphBelow_WhenStartedFirstOrAtAHeadingsEnd(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = Editor(page, "Notes");

        await CaretAsync(notes, 0, 0);
        await page.Keyboard.TypeAsync("---");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("b");
        await CaretAsync(notes, 0, 0);
        await page.Keyboard.TypeAsync("y");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>y</p><hr><p>b</p>");
        await SetAsync(page, "<h1>Title</h1>", "Title");
        await CaretAsync(notes, 0, 5);
        await Tool(notes, "Horizontal line").ClickAsync();
        await page.Keyboard.TypeAsync("body");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<h1>Title</h1><hr><p>body</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldCrossBack_WhenLeftOrUpIsPressedAtTheStartAfterARule(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>a</p><hr><p>b</p>", "b");

        await CaretAsync(notes, 2, 0);
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>ax</p><hr><p>b</p>");
        await CaretAsync(notes, 2, 0);
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>axy</p><hr><p>b</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldLeaveRoomToTypeBeforeAndAfter_WhenTheValueStartsOrEndsWithARule(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<hr><p>a</p><hr>", "a");

        await Expect(Tool(notes, "Heading 1")).ToHaveAttributeAsync("aria-pressed", "false");
        await CaretAsync(notes, 4, 0);
        await page.Keyboard.TypeAsync("z");
        await CaretAsync(notes, 0, 0);
        await page.Keyboard.TypeAsync("y");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>y</p><hr><p>a</p><hr><p>z</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldStayRules_WhenABlockIsSetOrADeletionStartsOnOne(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>a</p><hr><p>b</p>", "b");

        await SelectAsync(notes, 0, 0, 2, 1);
        await Tool(notes, "Bulleted list").ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li></ul><hr><ul><li>b</li></ul>");
        await SelectAsync(notes, 1, 0, 2, 1);
        await page.Keyboard.PressAsync("Delete");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li><li><br></li></ul>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rules_ShouldTakeDroppedTextAfterThem_WhenTextIsDroppedOnOne(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>a</p><hr><p>b</p>", "b");

        await notes.EvaluateAsync("""
            editor => {
                const box = editor.querySelector('hr').getBoundingClientRect();
                const data = new DataTransfer();
                data.setData('text/html', '<b>x</b>');
                const event = new DragEvent('drop', { bubbles: true, cancelable: true, clientX: box.left + 10,
                    clientY: box.top + box.height / 2 });
                Object.defineProperty(event, 'dataTransfer', { value: data });
                editor.dispatchEvent(event);
            }
            """);

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>a</p><hr><p><strong>x</strong>b</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldKeepTextOutOfARule_WhenARuleIsPastedOrSelectedAndDeleted(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>ab</p>", "ab");

        await CaretAsync(notes, 0, 1);
        await PasteAsync(notes, "<p>x</p><hr><p>y</p>", "ignored");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>ax</p><hr><p>yb</p>");
        await CaretAsync(notes, 2, 2);
        await PasteAsync(notes, "<hr>", "ignored");
        await page.Keyboard.TypeAsync("z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>ax</p><hr><p>yb</p><hr><p>z</p>");
        await SetAsync(page, "<hr><p>q</p><hr>", "q");
        await notes.FocusAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+a");
        await page.Keyboard.PressAsync("Delete");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ClearFormatting_ShouldTakeOffTheFormatsButKeepLinksAndBlocks_WhenPressedOrTyped(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page,
            "<h2><strong>a</strong><em><a href=\"/x\">b</a></em>c</h2><p><strong>de</strong></p>", "abc");

        await SelectAsync(notes, 0, 0, 0, 3);
        await Tool(notes, "Clear formatting").ClickAsync();
        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<h2>a<a href=\"/x\">b</a>c</h2><p><strong>de</strong></p>");
        await CaretAsync(notes, 1, 1);
        await page.Keyboard.PressAsync("ControlOrMeta+Backslash");
        await page.Keyboard.TypeAsync("x");

        await Expect(page.GetByTestId("notes"))
            .ToHaveTextAsync("<h2>a<a href=\"/x\">b</a>c</h2><p><strong>d</strong>x<strong>e</strong></p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Blocks_ShouldApplyEachKind_WhenEachBlockShortcutIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>one</p>", "one");

        foreach (var (keys, html) in new[]
        {
            ("Alt+Digit1", "<h1>one</h1>"), ("Alt+Digit2", "<h2>one</h2>"), ("Alt+Digit3", "<h3>one</h3>"),
            ("Shift+Digit9", "<blockquote><p>one</p></blockquote>"), ("Shift+Digit8", "<ul><li>one</li></ul>"),
            ("Shift+Digit7", "<ol><li>one</li></ol>"),
        })
        {
            await CaretAsync(notes, 0, 1);
            await page.Keyboard.PressAsync($"ControlOrMeta+{keys}");
            await Expect(page.GetByTestId("notes")).ToHaveTextAsync(html);
            await page.Keyboard.PressAsync("ControlOrMeta+Alt+Digit0");
            await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one</p>");
        }

        await page.Keyboard.TypeAsync("x");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>oxne</p>");
        await page.Keyboard.PressAsync("ControlOrMeta+Alt+Digit0");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>one</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Markdown_ShouldStartEachBlockAndUndoToTheText_WhenAStartIsTypedAtAParagraphsStart(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>a</p><p>b</p><p>c</p><p>d</p><p>e</p><p>f</p><p>g</p>", "abcdefg");
        string[] starts = ["# ", "## ", "### ", "> ", "- ", "* ", "1. "];

        for (var i = 0; i < starts.Length; i++)
        {
            await CaretAsync(notes, i, 0);
            await page.Keyboard.TypeAsync(starts[i]);
        }
        const string started = "<h1>a</h1><h2>b</h2><h3>c</h3><blockquote><p>d</p></blockquote><ul><li>e</li><li>f</li></ul>";
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(started + "<ol><li>g</li></ol>");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(started + "<p>1. g</p>");
        await CaretAsync(notes, 4, 0);
        await page.Keyboard.TypeAsync("# ");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(started.Replace("<li>e", "<li># e") + "<p>1. g</p>");
        await Editor(page, "Formula").ClickAsync();
        await page.Keyboard.TypeAsync("# x");

        await Expect(page.GetByTestId("formula")).ToHaveTextAsync("<p># x</p>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldIndentAndOutdentAListItemAndLeaveAfterEscape_WhenPressedInAList(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<ul><li>a</li><li>b</li></ul>", "ab");

        await CaretAsync(notes, 1, 1);
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a<ul><li>b</li></ul></li></ul>");
        await Expect(notes).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li><li>b</li></ul>");
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a<ul><li>b</li></ul></li></ul>");
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Source" })).ToBeFocusedAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<ul><li>a</li><li>b</li></ul>");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldLetTabLeaveAndPassTheNextEscapeToTheDialog_WhenPressedInAListItemInADialog(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open draft" }).ClickAsync();
        var draft = Editor(page, "Draft");
        await CaretAsync(draft, 1, 1);
        // The dialog closes on any Escape not marked defaultPrevented, so record that flag for each Escape on its way up.
        await draft.EvaluateAsync("""
            editor => editor.parentElement.addEventListener('keydown', event => {
                if (event.key === 'Escape') (window.escapes ??= []).push(event.defaultPrevented);
            })
            """);

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true })).ToBeFocusedAsync();
        await CaretAsync(draft, 1, 1);
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
        var escapes = await page.EvaluateAsync<bool[]>("() => window.escapes");
        Assert.Equal([true, true, false], escapes);
    }

    [Theory, MemberData(nameof(Chromium))]
    public async Task Composition_ShouldCommitTheTextInsideTheMark_WhenAnInputMethodComposesInABoldRun(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p><strong>ab</strong></p>", "ab");
        await CaretAsync(notes, 0, 1);
        var session = await page.Context.NewCDPSessionAsync(page);

        await session.SendAsync("Input.imeSetComposition",
            new() { ["text"] = "に", ["selectionStart"] = 1, ["selectionEnd"] = 1 });
        await page.EvaluateAsync("() => window.composing = getSelection().anchorNode");
        await session.SendAsync("Input.imeSetComposition",
            new() { ["text"] = "にほ", ["selectionStart"] = 2, ["selectionEnd"] = 2 });
        Assert.True(await page.EvaluateAsync<bool>("() => window.composing.isConnected"));
        await session.SendAsync("Input.insertText", new() { ["text"] = "日本" });

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>a日本b</strong></p>");
    }

    [Theory, MemberData(nameof(Chromium))]
    public async Task Composition_ShouldMergeTheParagraphsFirst_WhenItStartsOverASelectionAcrossThem(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = await SetAsync(page, "<p>one</p><p>two</p>", "one");
        await SelectAsync(notes, 0, 2, 1, 1);
        var session = await page.Context.NewCDPSessionAsync(page);

        await session.SendAsync("Input.imeSetComposition",
            new() { ["text"] = "に", ["selectionStart"] = 1, ["selectionEnd"] = 1 });
        await session.SendAsync("Input.insertText", new() { ["text"] = "日" });

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p>on日wo</p>");
    }

    private static ILocator Editor(IPage page, string label) =>
        page.GetByRole(AriaRole.Textbox, new() { Name = label, Exact = true });

    private static ILocator Tool(ILocator editor, string name) =>
        editor.Locator("xpath=ancestor::*[@data-part='editor']")
            .GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // Sets the notes from .NET and waits for the editor to show their text.
    private static async Task<ILocator> SetAsync(IPage page, string html, string text)
    {
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Source" }).FillAsync(html);
        await page.GetByRole(AriaRole.Button, new() { Name = "Set notes" }).ClickAsync();
        var notes = Editor(page, "Notes");
        await Expect(notes).ToContainTextAsync(text);
        return notes;
    }

    private static Task CaretAsync(ILocator editor, int block, int offset) =>
        SelectAsync(editor, block, offset, block, offset);

    // Selects by paragraph and offset in its text, counting a line break as one character.
    private static async Task SelectAsync(ILocator editor, int anchorBlock, int anchorOffset, int headBlock,
        int headOffset)
    {
        await Expect(editor).ToHaveAttributeAsync("data-ready", "");
        await editor.EvaluateAsync("""
            (editor, [anchorBlock, anchorOffset, headBlock, headOffset]) => {
                const point = (block, offset) => {
                    // A list item's own text leaves out the lists nested in it, and a rule counts as a block.
                    const paragraph = editor.querySelectorAll('p, h1, h2, h3, li, hr')[block];
                    const walker = document.createTreeWalker(paragraph, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT,
                        { acceptNode: node => node !== paragraph && /^[uo]l$/.test(node.localName) ? 2
                            : node.nodeType === 3 || node.localName === 'br' ? 1 : 3 });
                    let count = 0;
                    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
                        if (node.nodeType === 3) {
                            if (offset <= count + node.data.length) return [node, offset - count];
                            count += node.data.length;
                        } else if (offset === count) {
                            return [node.parentNode, [...node.parentNode.childNodes].indexOf(node)];
                        } else {
                            count++;
                        }
                    }
                    return [paragraph, 0];
                };
                editor.focus();
                getSelection().setBaseAndExtent(...point(anchorBlock, anchorOffset), ...point(headBlock, headOffset));
            }
            """, new[] { anchorBlock, anchorOffset, headBlock, headOffset });
    }

    private static Task<int> CaretOffsetAsync(ILocator editor) => editor.EvaluateAsync<int>("""
        editor => {
            const selection = getSelection();
            const range = document.createRange();
            range.setStart(editor.firstChild, 0);
            range.setEnd(selection.focusNode, selection.focusOffset);
            return range.toString().length;
        }
        """);

    // Playwright can't write to the system clipboard in every browser, so the paste event carries its own data.
    private static Task PasteAsync(ILocator editor, string? html, string text) => editor.EvaluateAsync("""
        (editor, [html, text]) => {
            const data = new DataTransfer();
            if (html) data.setData('text/html', html);
            data.setData('text/plain', text);
            const event = new ClipboardEvent('paste', { bubbles: true, cancelable: true });
            Object.defineProperty(event, 'clipboardData', { value: data });
            editor.dispatchEvent(event);
        }
        """, new[] { html, text });

    // Drops at the left edge of the first character, with HTML from elsewhere, or none for text dragged inside.
    private static Task DropAtStartAsync(ILocator editor, string? html) => editor.EvaluateAsync("""
        (editor, html) => {
            const text = editor.querySelector('p').firstChild;
            const range = document.createRange();
            range.setStart(text, 0);
            range.setEnd(text, 1);
            const box = range.getBoundingClientRect();
            const data = new DataTransfer();
            if (html) data.setData('text/html', html);
            const event = new DragEvent('drop', { bubbles: true, cancelable: true, clientX: box.left + 1,
                clientY: box.top + box.height / 2 });
            Object.defineProperty(event, 'dataTransfer', { value: data });
            editor.dispatchEvent(event);
        }
        """, html);
}
