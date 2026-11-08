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
            .ToHaveCountAsync(2);
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

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("<p><strong>Bold</strong> <em>it</em></p><p>Next</p>");
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

        await Expect(Tool(notes, "Superscript")).ToBeFocusedAsync();
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
        var folder = Path.Combine(AppContext.BaseDirectory, "RichTextCorpus");
        var expected = File.ReadAllText(Path.Combine(folder, $"{sample}.expected.html")).TrimEnd('\r', '\n');

        await CaretAsync(notes, 0, 0);
        await notes.EvaluateAsync(
            "editor => editor.addEventListener('tb-rich-text-change', event => window.sent = event.detail.html)");
        await PasteAsync(notes, File.ReadAllText(Path.Combine(folder, $"{sample}.html")), "ignored");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync(expected);
        Assert.Equal(expected, await page.EvaluateAsync<string>("() => window.sent"));
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
                    const paragraph = editor.children[block];
                    const walker = document.createTreeWalker(paragraph, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT,
                        { acceptNode: node => node.nodeType === 3 || node.localName === 'br' ? 1 : 3 });
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
