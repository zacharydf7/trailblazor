using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class MarkdownEditorTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Opens the editor with this text in it, all of it selected, once its toolbar works.
    private async Task<(IPage Page, ILocator Input)> OpenSelectedAsync(string browser, string text)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor");
        var editor = page.Tb().MarkdownEditor("Trail notes");
        await Expect(editor.Input.Locator("xpath=ancestor::*[@data-part='editor'][1]"))
            .ToHaveAttributeAsync("data-ready", "");
        await editor.Input.FillAsync(text);
        await editor.Input.SelectTextAsync();
        return (page, editor.Input);
    }

    private static Task<int[]> SelectionAsync(ILocator input) =>
        input.EvaluateAsync<int[]>("input => [input.selectionStart, input.selectionEnd]");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcuts_ShouldPutMarksAroundTheSelectionAndTakeThemOff_WhenPressed(string browser)
    {
        var (page, input) = await OpenSelectedAsync(browser, "steep");

        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await Expect(input).ToHaveValueAsync("**steep**");
        var selection = await SelectionAsync(input);
        Assert.Equal((2, 7), (selection[0], selection[1]));
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await Expect(input).ToHaveValueAsync("steep");
        await page.Keyboard.PressAsync("ControlOrMeta+i");
        await Expect(input).ToHaveValueAsync("*steep*");
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+x");
        await Expect(input).ToHaveValueAsync("*~~steep~~*");
        await page.Keyboard.PressAsync("ControlOrMeta+e");

        await Expect(input).ToHaveValueAsync("*~~`steep`~~*");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcuts_ShouldPrefixTheSelectedLines_WhenPressed(string browser)
    {
        var (page, input) = await OpenSelectedAsync(browser, "Water\nMap");

        await page.Keyboard.PressAsync("ControlOrMeta+Shift+Digit8");
        await Expect(input).ToHaveValueAsync("- Water\n- Map");
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+Digit7");
        await Expect(input).ToHaveValueAsync("1. Water\n2. Map");
        await page.Keyboard.PressAsync("ControlOrMeta+Alt+Digit2");
        await Expect(input).ToHaveValueAsync("## Water\n## Map");
        await page.Keyboard.PressAsync("ControlOrMeta+Alt+Digit2");
        await Expect(input).ToHaveValueAsync("Water\nMap");
        await page.Keyboard.PressAsync("ControlOrMeta+Shift+Digit9");

        await Expect(input).ToHaveValueAsync("> Water\n> Map");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcuts_ShouldKeepIndentationAndPutFencesOnTheirOwnLines_WhenPressed(string browser)
    {
        var (page, input) = await OpenSelectedAsync(browser, "- a\n    - b\n- c\n    - d");

        await page.Keyboard.PressAsync("ControlOrMeta+Shift+Digit7");
        await Expect(input).ToHaveValueAsync("1. a\n    1. b\n2. c\n    1. d");
        await input.FillAsync("see a\nb\n");
        await input.EvaluateAsync("input => input.setSelectionRange(4, input.value.length)");
        await page.Keyboard.PressAsync("ControlOrMeta+e");

        await Expect(input).ToHaveValueAsync("see \n```\na\nb\n```\n");
    }

    // AltGr types characters such as # and ~ on many keyboards, and Windows reports it as Ctrl+Alt.
    [Fact]
    public async Task Shortcuts_ShouldLeaveAltGrAlone_WhenItTypesACharacter()
    {
        var (_, input) = await OpenSelectedAsync("chromium", "Water");

        var prevented = await input.EvaluateAsync<bool>("""
            input => !input.dispatchEvent(new KeyboardEvent('keydown', { key: '~', code: 'Digit2', ctrlKey: true,
                altKey: true, modifierAltGraph: true, bubbles: true, cancelable: true }))
            """);

        Assert.False(prevented);
        await Expect(input).ToHaveValueAsync("Water");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldKeepBracketsAndWaitForALabel_WhenPressed(string browser)
    {
        var (page, input) = await OpenSelectedAsync(browser, "a]b");

        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await Expect(input).ToHaveValueAsync("[a\\]b](https://)");
        await input.FillAsync("the map here");
        await input.EvaluateAsync("input => input.setSelectionRange(4, 8)");
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await Expect(input).ToHaveValueAsync("the [map](https://) here");
        await input.FillAsync("");
        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await page.Keyboard.TypeAsync("map");

        await Expect(input).ToHaveValueAsync("[map](https://)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcuts_ShouldDoNothing_WhenTheSchemaLeavesTheirMarkOut(string browser)
    {
        var page = await OpenAsync(browser, "/markdown-editor-schema");
        var input = page.Tb().MarkdownEditor("Summary").Input;
        await Expect(page.Locator("[data-part=editor]")).ToHaveAttributeAsync("data-ready", "");
        await input.FillAsync("steep");
        await input.SelectTextAsync();

        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await page.Keyboard.PressAsync("ControlOrMeta+b");

        await Expect(input).ToHaveValueAsync("**steep**");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldSelectTheAddress_WhenPressed(string browser)
    {
        var (page, input) = await OpenSelectedAsync(browser, "the map");

        await page.Keyboard.PressAsync("ControlOrMeta+k");
        await page.Keyboard.TypeAsync("example.com/map");

        await Expect(input).ToHaveValueAsync("[the map](example.com/map)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Undo_ShouldTakeAMarkOff_WhenPressedAfterIt(string browser)
    {
        var (page, input) = await OpenSelectedAsync(browser, "steep");
        await page.Keyboard.PressAsync("ControlOrMeta+b");
        await Expect(input).ToHaveValueAsync("**steep**");

        await page.Keyboard.PressAsync("ControlOrMeta+z");

        await Expect(input).ToHaveValueAsync("steep");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Editor_ShouldFillItsFieldsBox_WhenShownWithOrWithoutThePreviewBeside(string browser)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor");

        foreach (var label in new[] { "Trail notes", "Trail description" })
        {
            var input = page.Tb().MarkdownEditor(label).Input;
            var editor = input.Locator("xpath=ancestor::*[@data-part='editor'][1]");
            // The editor's width, its box's width, and the tops of the first and last visible toolbar buttons.
            var sizes = await editor.EvaluateAsync<double[]>("""
                editor => {
                    const buttons = [...editor.querySelectorAll('[data-part=toolbar] button')]
                        .filter(button => button.offsetWidth > 0).map(button => button.getBoundingClientRect().top);
                    return [editor.getBoundingClientRect().width, editor.parentElement.clientWidth, buttons[0],
                        buttons.at(-1)];
                }
                """);

            // The box is far wider than the toolbar, so its buttons share one row.
            Assert.InRange(sizes[1], 400, 2000);
            Assert.Equal(sizes[1], sizes[0], 1.0);
            Assert.Equal(sizes[2], sizes[3], 1.0);
            Assert.InRange((await input.BoundingBoxAsync())!.Width, sizes[0] / 2 - 20, sizes[0]);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Editor_ShouldKeepItsToolbarOnOneRow_WhenItsFieldIsCentered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor");

        // The editor with the preview behind a button, then the one with the preview beside the text.
        foreach (var label in new[] { "Trail notes", "Trail description" })
        {
            var input = page.Tb().MarkdownEditor(label).Input;
            await CenterFieldAsync(input);
            // The tops of the first and last toolbar buttons.
            var tops = await input.Locator("xpath=ancestor::*[@data-part='editor'][1]").EvaluateAsync<double[]>("""
                editor => {
                    const buttons = [...editor.querySelectorAll('[data-part=toolbar] button')]
                        .filter(button => button.offsetWidth > 0);
                    return [buttons[0], buttons.at(-1)].map(button => button.getBoundingClientRect().top);
                }
                """);

            Assert.Equal(tops[0], tops[1], 1.0);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Editor_ShouldFitItsParent_WhenACenteredColumnOrARowIsNarrowerThanItAsksFor(string browser)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor");
        var input = page.Tb().MarkdownEditor("Trail notes").Input;

        foreach (var layout in new object[]
        {
            new { display = "grid", justifyItems = "center", inlineSize = "20rem" },
            new { display = "flex", flexDirection = "row", inlineSize = "20rem" },
        })
        {
            await StyleFieldsParentAsync(input, layout);
            // The right edges of the field and of its parent.
            var edges = await input.EvaluateAsync<double[]>("""
                input => {
                    const field = input.closest('.tb-field');
                    return [field.getBoundingClientRect().right, field.parentElement.getBoundingClientRect().right];
                }
                """);

            Assert.True(edges[0] <= edges[1] + 1, $"The field ends {edges[0] - edges[1]}px past its parent.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Preview_ShouldKeepTheEditorsWidth_WhenShownInACenteredField(string browser)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor");
        var editor = page.Tb().MarkdownEditor("Trail notes");
        await CenterFieldAsync(editor.Input);
        var paragraph = string.Join(' ', Enumerable.Repeat("A long day on the ridge with water every hour.", 8));
        await editor.EnterAsync(paragraph);
        var field = editor.Input.Locator("xpath=ancestor::*[contains(@class, 'tb-markdown-editor-field')][1]");
        var width = (await field.BoundingBoxAsync())!.Width;

        await editor.ShowPreviewAsync();

        Assert.Equal(width, (await field.BoundingBoxAsync())!.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldPressAToolbarButtonAndReadThePreview_WhenFoundByLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor");
        var editor = page.Tb().MarkdownEditor("Trail notes");

        await editor.EnterAsync("steep");
        await editor.Input.SelectTextAsync();
        await editor.PressAsync("Bold");
        await editor.ShowPreviewAsync();

        Assert.Equal("**steep**", await editor.TextAsync());
        Assert.Equal("<p><strong>steep</strong></p>", await editor.PreviewHtmlAsync());
        await Expect(page.GetByText("Value: **steep**")).ToBeVisibleAsync();
        await editor.HidePreviewAsync();
        Assert.Null(await editor.PreviewHtmlAsync());
    }
}
