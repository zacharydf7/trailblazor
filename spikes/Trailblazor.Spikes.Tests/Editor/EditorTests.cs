using System.Text.RegularExpressions;

namespace Trailblazor.Spikes.Tests.Editor;

// Spike D, criteria 1 to 9 and 11. See spikes/rich-text-editor/README.md.
public class EditorTests(SpikeFixture fixture) : SpikeTest(fixture)
{
    private const string Editor = "document.querySelector('[data-testid=editor]').spikeEditor";

    private int _serverErrorsBefore;

    public static TheoryData<string> Browsers => new(BrowsersAndModes.Browsers);

    public static TheoryData<string> Modes => new(BrowsersAndModes.Modes);

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Typing_ShouldLandInTheModelAsShown_WhenTypingAtTheStartMiddleAndEnd(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>bd</p>");

        await CaretAsync(page, 0, 1);
        await page.Keyboard.TypeAsync("c");
        await CaretAsync(page, 0, 0);
        await page.Keyboard.TypeAsync("<a");
        await CaretAsync(page, 0, 5);
        await page.Keyboard.TypeAsync(" & e");

        Assert.Equal("<p>&lt;abcd &amp; e</p>", await ValueAsync(page));
        Assert.Equal("<abcd & e", await page.GetByTestId("editor").TextContentAsync());
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Typing_ShouldTakeTheMarksOfTheTextBefore_WhenTypingInsideOrAtTheEndOfAMarkedRun(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p><strong>bold</strong> plain</p>");

        await CaretAsync(page, 0, 2);
        await page.Keyboard.TypeAsync("x");
        await CaretAsync(page, 0, 5);
        await page.Keyboard.TypeAsync("er");
        await CaretAsync(page, 0, 8);
        await page.Keyboard.TypeAsync("y");

        Assert.Equal("<p><strong>boxlder</strong> yplain</p>", await ValueAsync(page));
        Assert.Equal("<strong>boxlder</strong> yplain", await page.Locator("[data-testid=editor] p").InnerHTMLAsync());
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Typing_ShouldPlaceTheTextAtTheCaret_WhenTheSameCharacterRepeatsAcrossAMarkBoundary(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p><strong>a</strong>a</p>");

        await CaretAsync(page, 0, 1);
        await page.Keyboard.TypeAsync("a");
        Assert.Equal("<p><strong>aa</strong>a</p>", await ValueAsync(page));
        await page.Keyboard.PressAsync("Backspace");

        Assert.Equal("<p><strong>a</strong>a</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Marks_ShouldApplyAndRemove_WhenEachIsToggledOverARange(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>one two</p>");

        foreach (var (label, tag) in new[]
        {
            ("Bold", "strong"), ("Italic", "em"), ("Underline", "u"), ("Strikethrough", "s"), ("Subscript", "sub"),
            ("Superscript", "sup"),
        })
        {
            await SelectAsync(page, 0, 4, 0, 7);
            await Tool(page, label).ClickAsync();
            Assert.Equal($"<p>one <{tag}>two</{tag}></p>", await ValueAsync(page));
            await Tool(page, label).ClickAsync();
            Assert.Equal("<p>one two</p>", await ValueAsync(page));
        }

        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Marks_ShouldShowMixedAndApplyToAll_WhenTheRangeIsPartlyMarked(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p><strong>one</strong> two</p>");

        // A selection the user makes, so the engine maps it from the DOM.
        await page.Keyboard.PressAsync("Shift+End");
        await Expect(Tool(page, "Bold")).ToHaveAttributeAsync("aria-pressed", "mixed");
        await Tool(page, "Bold").ClickAsync();

        Assert.Equal("<p><strong>one two</strong></p>", await ValueAsync(page));
        await Expect(Tool(page, "Bold")).ToHaveAttributeAsync("aria-pressed", "true");
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Marks_ShouldApplyToTheNextTypedText_WhenToggledWithACollapsedSelection(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>one</p>");

        await CaretAsync(page, 0, 3);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await page.Keyboard.TypeAsync("two");
        await Tool(page, "Italic").ClickAsync();
        await page.Keyboard.TypeAsync("3");

        Assert.Equal("<p>one<strong>two</strong><strong><em>3</em></strong></p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Marks_ShouldExcludeEachOther_WhenSubscriptAndSuperscriptAreBothApplied(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>x2</p>");

        await SelectAsync(page, 0, 1, 0, 2);
        await Tool(page, "Subscript").ClickAsync();
        Assert.Equal("<p>x<sub>2</sub></p>", await ValueAsync(page));
        await Tool(page, "Superscript").ClickAsync();

        Assert.Equal("<p>x<sup>2</sup></p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Marks_ShouldToggle_WhenTheirShortcutsArePressed(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>word</p>");

        await SelectAsync(page, 0, 0, 0, 4);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await page.Keyboard.PressAsync("ControlOrMeta+i");
        await page.Keyboard.PressAsync("ControlOrMeta+u");
        Assert.Equal("<p><strong><em><u>word</u></em></strong></p>", await ValueAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await page.Keyboard.PressAsync("ControlOrMeta+i");
        await page.Keyboard.PressAsync("ControlOrMeta+u");

        Assert.Equal("<p>word</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Toolbar_ShouldFollowTheSelection_WhenTheCaretMoves(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>plain <em>it</em></p>");

        await CaretAsync(page, 0, 2);
        await Expect(Tool(page, "Italic")).ToHaveAttributeAsync("aria-pressed", "false");
        await page.Keyboard.PressAsync("End");

        await Expect(Tool(page, "Italic")).ToHaveAttributeAsync("aria-pressed", "true");
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Enter_ShouldSplitTheParagraph_AndShiftEnterBreakTheLine_WhenPressed(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>onetwo</p>");

        await CaretAsync(page, 0, 3);
        await page.Keyboard.PressAsync("Enter");
        Assert.Equal("<p>one</p><p>two</p>", await ValueAsync(page));
        await CaretAsync(page, 1, 1);
        await page.Keyboard.PressAsync("Shift+Enter");
        await page.Keyboard.TypeAsync("x");

        Assert.Equal("<p>one</p><p>t<br>xwo</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task BackspaceAndDelete_ShouldJoinParagraphs_WhenPressedAtAParagraphsEdge(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>one</p><p>two</p>");

        await CaretAsync(page, 1, 0);
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.TypeAsync("_");
        Assert.Equal("<p>one_two</p>", await ValueAsync(page));
        await page.Keyboard.PressAsync("Enter");
        await CaretAsync(page, 0, 4);
        await page.Keyboard.PressAsync("Delete");

        Assert.Equal("<p>one_two</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Deleting_ShouldMergeParagraphs_WhenTheRangeSpansThem(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>one</p><p>two</p><p>three</p>");

        await SelectAsync(page, 0, 1, 2, 2);
        await page.Keyboard.PressAsync("Backspace");
        Assert.Equal("<p>oree</p>", await ValueAsync(page));
        await SetValueAsync(page, "<p>ab</p><p>cd</p>");
        await SelectAsync(page, 0, 1, 1, 1);
        await page.Keyboard.TypeAsync("X");

        Assert.Equal("<p>aXd</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Deleting_ShouldLeaveAnEmptyValueAndTheCaret_WhenEverythingIsSelected(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>one</p><p><strong>two</strong></p>");

        await page.Keyboard.PressAsync("ControlOrMeta+a");
        await page.Keyboard.PressAsync("Backspace");
        Assert.Equal("", await ValueAsync(page));
        Assert.Equal(1, await page.Locator("[data-testid=editor] p").CountAsync());
        await page.Keyboard.TypeAsync("x");

        Assert.Equal("<p>x</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Undo_ShouldUndoAndRedoTypingInWordSizedSteps_WhenItsShortcutsArePressed(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode);

        await page.Keyboard.TypeAsync("one two");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        Assert.Equal("<p>one&nbsp;</p>", await ValueAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        Assert.Equal("", await ValueAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+z");
        Assert.Equal("<p>one&nbsp;</p>", await ValueAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+y");

        Assert.Equal("<p>one two</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Undo_ShouldRestoreTheTextAndSelection_WhenUndoingAMarkASplitAndAPaste(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>onetwo</p>");

        await SelectAsync(page, 0, 0, 0, 3);
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        Assert.Equal("<p>onetwo</p>", await ValueAsync(page));
        Assert.Equal("0:0-0:3", await SelectionAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+z");
        Assert.Equal("<p><strong>one</strong>two</p>", await ValueAsync(page));
        Assert.Equal("0:0-0:3", await SelectionAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+z");

        await CaretAsync(page, 0, 3);
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("ControlOrMeta+z");
        Assert.Equal("<p>onetwo</p>", await ValueAsync(page));
        Assert.Equal("0:3-0:3", await SelectionAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+z");
        Assert.Equal("<p>one</p><p>two</p>", await ValueAsync(page));
        Assert.Equal("1:0-1:0", await SelectionAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+z");

        await CaretAsync(page, 0, 6);
        await PasteAsync(page, "<b>x</b>", "x");
        Assert.Equal("<p>onetwo<strong>x</strong></p>", await ValueAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+z");

        Assert.Equal("<p>onetwo</p>", await ValueAsync(page));
        Assert.Equal("0:6-0:6", await SelectionAsync(page));
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+z");
        Assert.Equal("<p>onetwo<strong>x</strong></p>", await ValueAsync(page));
        Assert.Equal("0:7-0:7", await SelectionAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Undo_ShouldFollowTheBrowsersHistoryInput_WhenItAsksToUndoOrRedo(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode);

        await page.Keyboard.TypeAsync("abc");
        await DispatchHistoryAsync(page, "historyUndo");
        Assert.Equal("", await ValueAsync(page));
        await DispatchHistoryAsync(page, "historyRedo");

        Assert.Equal("<p>abc</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, MemberData(nameof(Modes))]
    public async Task Composition_ShouldCommitTheTextInsideTheMark_WhenAnInputMethodComposesInABoldRun(string mode)
    {
        var page = await OpenEditorAsync("chromium", mode, "<p><strong>ab</strong></p>");
        await CaretAsync(page, 0, 1);
        var session = await page.Context.NewCDPSessionAsync(page);

        await session.SendAsync("Input.imeSetComposition",
            new() { ["text"] = "に", ["selectionStart"] = 1, ["selectionEnd"] = 1 });
        await page.EvaluateAsync("() => window.composingNode = getSelection().anchorNode");
        await session.SendAsync("Input.imeSetComposition",
            new() { ["text"] = "にほ", ["selectionStart"] = 2, ["selectionEnd"] = 2 });
        Assert.True(await page.EvaluateAsync<bool>("() => window.composingNode.isConnected"));
        Assert.Equal("<p><strong>ab</strong></p>", await ValueAsync(page));
        await session.SendAsync("Input.insertText", new() { ["text"] = "日本" });

        Assert.Equal("<p><strong>a日本b</strong></p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task ReadBack_ShouldTakeChangesTheEngineDidNotMake_WhenTheBrowserRewritesText(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>teh <strong>bgi</strong> cat</p>");

        // As spellcheck does it: one word at a time, straight in the text node.
        await page.EvaluateAsync("() => document.querySelector('[data-testid=editor] p').firstChild.data = 'the '");
        await page.EvaluateAsync("() => document.querySelector('[data-testid=editor] strong').firstChild.data = 'big'");

        Assert.Equal("<p>the <strong>big</strong> cat</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Paste_ShouldKeepTheMarks_WhenPastingFromWordGoogleDocsAndAWebPage(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode);

        foreach (var (html, expected) in PasteSamples)
        {
            await SetValueAsync(page, "");
            await CaretAsync(page, 0, 0);
            await PasteAsync(page, html, "ignored");
            Assert.Equal(expected, await ValueAsync(page));
        }

        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Paste_ShouldRunNoScript_WhenTheHtmlIsHostile(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode);
        const string hostile = """
            <img src="x" onerror="window.pwned = 1"><script>window.pwned = 1</script>
            <a href="javascript:window.pwned = 1" onclick="window.pwned = 1">click</a>
            <svg onload="window.pwned = 1"><text>svg</text></svg>
            <iframe srcdoc="<script>parent.pwned = 1</script>"></iframe>
            <p style="background: url(javascript:window.pwned = 1)" onmouseover="window.pwned = 1">hover</p>
            """;

        await PasteAsync(page, hostile, "ignored");
        Assert.Equal("<p>click</p><p>hover</p>", await ValueAsync(page));
        await page.GetByTestId("source").FillAsync(hostile);
        await page.GetByTestId("set-value").ClickAsync();
        await page.WaitForFunctionAsync($"() => {Editor}.value() === '<p>click</p><p>hover</p>'");
        await page.GetByTestId("editor").GetByText("hover").HoverAsync();
        await page.GetByTestId("editor").GetByText("click").ClickAsync();
        await page.WaitForTimeoutAsync(300);

        const string forbidden = "img, script, a, svg, iframe, [onerror], [onclick], [onmouseover], [style]";
        Assert.Equal(0, await page.Locator($"[data-testid=editor] :is({forbidden})").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => window.pwned === undefined"));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Paste_ShouldPastePlainText_WhenTheClipboardHasOnlyTextOrShiftIsHeld(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p><strong>ab</strong></p>");

        await CaretAsync(page, 0, 1);
        await PasteAsync(page, null, "one\ntwo");
        Assert.Equal("<p><strong>aone</strong></p><p><strong>twob</strong></p>", await ValueAsync(page));
        await SetValueAsync(page, "<p>ab</p>");
        await CaretAsync(page, 0, 1);
        await page.EvaluateAsync("""
            () => document.querySelector('[data-testid=editor]').dispatchEvent(new KeyboardEvent('keydown',
                { key: 'V', ctrlKey: true, metaKey: true, shiftKey: true, bubbles: true }))
            """);
        await PasteAsync(page, "<em>x</em>", "x");

        Assert.Equal("<p>axb</p>", await ValueAsync(page));
        AssertNoErrors();
    }

    [Theory, MemberData(nameof(Browsers))]
    public async Task Binding_ShouldKeepEveryCharacterAndTheCaret_WhenTypingFastOverASlowServer(string browser)
    {
        await using var proxy = new LatencyProxy(Fixture.BaseAddress, TimeSpan.FromMilliseconds(200));
        var page = await OpenEditorAsync(browser, "server", baseAddress: proxy.Address);
        const string text = "The quick brown fox jumps over the lazy dog 123456";

        // Pauses longer than the send delay put values in flight while typing goes on.
        foreach (var chunk in text.Chunk(10))
        {
            await page.Keyboard.TypeAsync(new string(chunk));
            await page.WaitForTimeoutAsync(350);
        }

        Assert.Equal($"<p>{text}</p>", await ValueAsync(page));
        await Expect(page.GetByTestId("value")).ToHaveTextAsync($"<p>{text}</p>", new() { Timeout = 5000 });
        await page.WaitForTimeoutAsync(500);
        Assert.Equal($"0:{text.Length}-0:{text.Length}", await SelectionAsync(page));
        AssertNoErrors();
    }

    // What a consumer's own Playwright test would do: find the editor by its label and fill it.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Fill_ShouldReplaceTheText_WhenAPlaywrightTestFillsTheLabeledTextbox(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode, "<p>old</p><p>text</p>");

        await page.GetByRole(AriaRole.Textbox, new() { Name = "Description" }).FillAsync("new text");

        Assert.Equal("<p>new text</p>", await ValueAsync(page));
        await Expect(page.GetByTestId("value")).ToHaveTextAsync("<p>new text</p>");
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Binding_ShouldRenderTheValue_WhenDotNetSetsIt(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode);

        await SetValueAsync(page, "<p>Set <strong>from</strong> .NET</p>");

        Assert.Equal("Set <strong>from</strong> .NET", await page.Locator("[data-testid=editor] p").InnerHTMLAsync());
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Binding_ShouldRenderTheValue_WhenDotNetSetsItWhileTheEngineIsLoading(string browser, string mode)
    {
        _serverErrorsBefore = Fixture.ServerErrors.Count;
        var engineHeld = new TaskCompletionSource();
        var page = await OpenAsync(browser, mode, "/editor", beforeNavigation: page => page.RouteAsync("**/editor*.js",
            async route =>
            {
                await engineHeld.Task;
                await route.ContinueAsync();
            }));

        await page.GetByTestId("source").FillAsync("<p>Loaded</p>");
        await page.GetByTestId("set-value").ClickAsync();
        await Expect(page.GetByTestId("value")).ToHaveTextAsync("<p>Loaded</p>");
        engineHeld.SetResult();

        await page.WaitForFunctionAsync($"() => {Editor}?.value() === '<p>Loaded</p>'");
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Binding_ShouldLeaveTheDomAndCaretAlone_WhenTheEditorsOwnValueComesBack(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode);

        await page.Keyboard.TypeAsync("abc");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.EvaluateAsync("() => window.textNode = document.querySelector('[data-testid=editor] p').firstChild");
        await Expect(page.GetByTestId("value")).ToHaveTextAsync("<p>abc</p>");
        await page.WaitForTimeoutAsync(300);

        Assert.True(await page.EvaluateAsync<bool>("() => window.textNode.isConnected"));
        Assert.Equal("0:1-0:1", await SelectionAsync(page));
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Binding_ShouldSendTheValue_WhenTypingPausesOrFocusLeaves(string browser, string mode)
    {
        var page = await OpenEditorAsync(browser, mode);

        await page.Keyboard.TypeAsync("a");
        var pauseToValue = await MillisecondsUntilTheValueChangesAsync(page, blur: false);
        await Expect(page.GetByTestId("value")).ToHaveTextAsync("<p>a</p>");
        await page.Keyboard.TypeAsync("b");
        var blurToValue = await MillisecondsUntilTheValueChangesAsync(page, blur: true);

        RecordMeasurement($"editor pause-to-value {browser} {mode}: {pauseToValue:F0} ms");
        RecordMeasurement($"editor blur-to-value {browser} {mode}: {blurToValue:F0} ms");
        // The engine waits 300 ms after typing stops.
        Assert.InRange(pauseToValue, 250, 450);
        Assert.InRange(blurToValue, 0, 100);
        await Expect(page.GetByTestId("value")).ToHaveTextAsync("<p>ab</p>");
        AssertNoErrors();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Accessibility_ShouldExposeATextboxAToolbarAndSemanticMarks_WhenRendered(string browser,
        string mode)
    {
        var page = await OpenEditorAsync(browser, mode,
            "<p><strong>a</strong><em>b</em><u>c</u><s>d</s><sub>e</sub><sup>f</sup></p>");

        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Description" }))
            .ToHaveAttributeAsync("aria-multiline", "true");
        await Expect(page.GetByRole(AriaRole.Toolbar, new() { Name = "Formatting" })).ToBeVisibleAsync();
        foreach (var label in new[] { "Bold", "Italic", "Underline", "Strikethrough", "Subscript", "Superscript" })
        {
            await Expect(Tool(page, label)).ToHaveAttributeAsync("aria-pressed", new Regex("^(true|false|mixed)$"));
        }

        Assert.Equal(6, await page.Locator("[data-testid=editor] p > :is(strong, em, u, s, sub, sup)").CountAsync());
        AssertNoErrors();
    }

    private static readonly (string Html, string Expected)[] PasteSamples =
    [
        (
            """
            <html xmlns:o="urn:schemas-microsoft-com:office:office">
            <head><style>p.MsoNormal { margin: 0 }</style></head>
            <body lang=EN-US><!--StartFragment--><p class=MsoNormal><b>Bold</b>
            <i>italic</i> <u>under</u> <s>strike</s> H<sub>2</sub>O x<sup>2</sup><o:p></o:p></p><!--EndFragment-->
            </body></html>
            """,
            "<p><strong>Bold</strong> <em>italic</em> <u>under</u> <s>strike</s> H<sub>2</sub>O x<sup>2</sup></p>"
        ),
        (
            """
            <p class=MsoListParagraphCxSpFirst style='text-indent:-.25in;mso-list:l0 level1 lfo1'>
            <![if !supportLists]><span style='font-family:Symbol'><span style='mso-list:Ignore'>·<span
            style='font:7.0pt "Times New Roman"'>&nbsp;&nbsp;&nbsp; </span></span></span><![endif]>One<o:p></o:p></p>
            <p class=MsoListParagraphCxSpLast style='text-indent:-.25in;mso-list:l0 level1 lfo1'>
            <![if !supportLists]><span style='mso-list:Ignore'>2.<span
            style='font:7.0pt "Times New Roman"'>&nbsp;&nbsp; </span></span><![endif]><b>Two</b></p>
            """,
            "<p>One</p><p><strong>Two</strong></p>"
        ),
        (
            """
            <meta charset="utf-8"><b style="font-weight:normal;" id="docs-internal-guid-1a2b3c"><p dir="ltr"
            style="line-height:1.38;margin-top:0pt;margin-bottom:0pt;"><span
            style="font-size:11pt;font-weight:700;">Bold</span><span
            style="font-size:11pt;font-weight:400;"> plain </span><span style="font-style:italic;">italic</span><span
            style="text-decoration:underline;">under</span><span style="vertical-align:super;">sup</span></p><br><p
            dir="ltr"><span style="text-decoration:line-through;">gone</span></p></b>
            """,
            "<p><strong>Bold</strong> plain <em>italic</em><u>under</u><sup>sup</sup></p><p><s>gone</s></p>"
        ),
        (
            """
            <h2>Title</h2>
            <ul>
              <li>One</li>
              <li><strong>Two</strong></li>
            </ul>
            <p>Some <a href="https://example.com/">link</a> text</p><table><tbody><tr><td>Cell</td></tr></tbody></table>
            """,
            "<p>Title</p><p>One</p><p><strong>Two</strong></p><p>Some link text</p><p>Cell</p>"
        ),
    ];

    private async Task<IPage> OpenEditorAsync(string browser, string mode, string value = "", Uri? baseAddress = null)
    {
        _serverErrorsBefore = Fixture.ServerErrors.Count;
        var page = await OpenAsync(browser, mode, "/editor", baseAddress);
        await page.Locator("[data-testid=editor][data-ready]").WaitForAsync();
        if (value != "")
        {
            await SetValueAsync(page, value);
        }

        await CaretAsync(page, 0, 0);
        return page;
    }

    private static async Task SetValueAsync(IPage page, string value)
    {
        await page.GetByTestId("source").FillAsync(value);
        await page.GetByTestId("set-value").ClickAsync();
        await page.WaitForFunctionAsync($"value => {Editor}.value() === value", value);
    }

    private static Task<double> MillisecondsUntilTheValueChangesAsync(IPage page, bool blur) =>
        page.EvaluateAsync<double>("""
            blur => new Promise(resolve => {
                const start = performance.now();
                const output = document.querySelector('[data-testid=value]');
                new MutationObserver(() => resolve(performance.now() - start))
                    .observe(output, { childList: true, characterData: true, subtree: true });
                if (blur) document.querySelector('[data-testid=source]').focus();
            })
            """, blur);

    private static Task<string> ValueAsync(IPage page) => page.EvaluateAsync<string>($"() => {Editor}.value()");

    private static ILocator Tool(IPage page, string label) =>
        page.GetByRole(AriaRole.Toolbar).GetByRole(AriaRole.Button, new() { Name = label, Exact = true });

    private static Task CaretAsync(IPage page, int block, int offset) =>
        SelectAsync(page, block, offset, block, offset);

    private static Task SelectAsync(IPage page, int anchorBlock, int anchorOffset, int headBlock, int headOffset) =>
        page.EvaluateAsync($$"""
            ([anchorBlock, anchorOffset, headBlock, headOffset]) => {
                const editor = {{Editor}};
                editor.root.focus();
                editor.selection = {
                    anchor: { block: anchorBlock, offset: anchorOffset },
                    head: { block: headBlock, offset: headOffset },
                };
                editor.writeSelection();
            }
            """, new[] { anchorBlock, anchorOffset, headBlock, headOffset });

    private static Task<string> SelectionAsync(IPage page) => page.EvaluateAsync<string>($$"""
        () => {
            const { anchor, head } = {{Editor}}.readSelection();
            return `${anchor.block}:${anchor.offset}-${head.block}:${head.offset}`;
        }
        """);

    // Playwright can't write to the system clipboard in every browser, so the paste event carries its own data.
    private static Task PasteAsync(IPage page, string? html, string text) => page.EvaluateAsync("""
        ([html, text]) => {
            const data = new DataTransfer();
            if (html) data.setData('text/html', html);
            data.setData('text/plain', text);
            const event = new ClipboardEvent('paste', { bubbles: true, cancelable: true });
            Object.defineProperty(event, 'clipboardData', { value: data });
            document.querySelector('[data-testid=editor]').dispatchEvent(event);
        }
        """, new[] { html, text });

    private static Task DispatchHistoryAsync(IPage page, string inputType) => page.EvaluateAsync("""
        inputType => document.querySelector('[data-testid=editor]').dispatchEvent(
            new InputEvent('beforeinput', { inputType, bubbles: true, cancelable: true }))
        """, inputType);

    private void AssertNoErrors()
    {
        Assert.Empty(ScriptErrors);
        Assert.Equal(_serverErrorsBefore, Fixture.ServerErrors.Count);
    }
}
