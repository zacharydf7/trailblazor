using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbRichTextEditorTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;
    private readonly BunitJSModuleInterop _handle;

    public TbRichTextEditorTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/rich-text-editor.js");
        _handle = _module.SetupModule("attachRichTextEditor", _ => true);
        _handle.SetupVoid("setValue", _ => true).SetVoidResult();
        _handle.SetupVoid("setOptions", _ => true).SetVoidResult();
        _handle.SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Render_ShouldNameTheEditingAreaAsAMultiLineTextbox_WhenLabeled()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report"));

        var area = cut.Find("[data-part=input]");

        Assert.Equal("textbox", area.GetAttribute("role"));
        Assert.Equal("true", area.GetAttribute("aria-multiline"));
        Assert.Equal("true", area.GetAttribute("contenteditable"));
        Assert.Equal(cut.Find("[data-part=label]").Id, area.GetAttribute("aria-labelledby"));
        Assert.Null(cut.Find("[data-part=label]").GetAttribute("for"));
    }

    [Fact]
    public void Render_ShouldShowAButtonForEachAllowedFormat_WhenGivenFormats()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Formula")
            .Add(editor => editor.Formats, RichTextFormats.Bold | RichTextFormats.Superscript));

        var buttons = cut.FindAll("[role=toolbar] button[data-format]");

        Assert.Equal("Formatting", cut.Find("[role=toolbar]").GetAttribute("aria-label"));
        Assert.Equal(["bold", "superscript"], buttons.Select(button => button.GetAttribute("data-format")));
        Assert.Equal(["Bold", "Superscript"], buttons.Select(button => button.GetAttribute("aria-label")));
        Assert.All(buttons, button => Assert.Equal("false", button.GetAttribute("aria-pressed")));
        Assert.Equal("Control+B Meta+B", buttons[0].GetAttribute("aria-keyshortcuts"));
    }

    [Theory]
    [InlineData(false, RichTextFormats.Bold, RichTextBlocks.Quotes)]
    [InlineData(true, RichTextFormats.None, RichTextBlocks.None)]
    public void Render_ShouldLeaveOutTheToolbar_WhenItIsHiddenOrHasNoFormats(bool showToolbar, RichTextFormats formats,
        RichTextBlocks blocks)
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.ShowToolbar, showToolbar).Add(editor => editor.Formats, formats)
            .Add(editor => editor.Blocks, blocks));

        Assert.Empty(cut.FindAll("[role=toolbar]"));
    }

    [Fact]
    public void Render_ShouldShowABlockButtonForEachAllowedBlockAndIndentButtons_WhenAListIsAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.None)
            .Add(editor => editor.Blocks, RichTextBlocks.Headings | RichTextBlocks.NumberedLists));

        var blocks = cut.FindAll("button[data-block]");
        var commands = cut.FindAll("button[data-command]");

        Assert.Equal(["heading1", "heading2", "heading3", "number"],
            blocks.Select(button => button.GetAttribute("data-block")));
        Assert.Equal(["Heading 1", "Heading 2", "Heading 3", "Numbered list"],
            blocks.Select(button => button.GetAttribute("aria-label")));
        Assert.All(blocks, button => Assert.Equal("false", button.GetAttribute("aria-pressed")));
        Assert.Equal(["Control+Alt+1 Meta+Alt+1", "Control+Alt+2 Meta+Alt+2", "Control+Alt+3 Meta+Alt+3",
            "Control+Shift+7 Meta+Shift+7"], blocks.Select(button => button.GetAttribute("aria-keyshortcuts")));
        Assert.Equal(["outdent", "indent"], commands.Select(button => button.GetAttribute("data-command")));
        Assert.All(commands, button => Assert.Null(button.GetAttribute("aria-pressed")));
    }

    [Fact]
    public void Render_ShouldLeaveOutTheIndentButtons_WhenNoListIsAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Blocks, RichTextBlocks.Headings | RichTextBlocks.Quotes));

        Assert.Empty(cut.FindAll("button[data-command=outdent], button[data-command=indent]"));
    }

    [Theory]
    [InlineData(RichTextBlocks.Headings | RichTextBlocks.BulletedLists | RichTextBlocks.Rules,
        "<h2>Title</h2><ul><li><code>a</code></li></ul><hr><p><a href=\"https://example.com\">b</a></p>")]
    [InlineData(RichTextBlocks.None, "<p>Title</p><p><code>a</code></p><p><a href=\"https://example.com\">b</a></p>")]
    public void Render_ShouldHandTheScriptOnlyTheAllowedBlocks_WhenGivenBlocks(RichTextBlocks blocks, string value)
    {
        Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Blocks, blocks)
            .Add(editor => editor.Value, "<h2>Title</h2><ul><li><code>a</code></li></ul><hr>"
                + "<a href=\"https://example.com\">b</a>"));

        Assert.Equal(value, _module.Invocations["attachRichTextEditor"].Single().Arguments[1]);
    }

    [Fact]
    public void Render_ShouldNameTheToolbarAndButtonsFromTheLabels_WhenLabelsAreGiven()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notas")
            .Add(editor => editor.Formats, RichTextFormats.Bold)
            .Add(editor => editor.Labels, new RichTextEditorLabels { Toolbar = "Formato", Bold = "Negrita" }));

        Assert.Equal("Formato", cut.Find("[role=toolbar]").GetAttribute("aria-label"));
        Assert.Equal("Negrita", cut.Find("button[data-format=bold]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldKeepAReadOnlyEditorFocusableButItsToolbarDisabled_WhenReadOnly()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Approved")
            .Add(editor => editor.ReadOnly, true));

        var area = cut.Find("[data-part=input]");

        Assert.Equal("true", area.GetAttribute("aria-readonly"));
        Assert.Equal("0", area.GetAttribute("tabindex"));
        Assert.Equal("none", area.GetAttribute("inputmode"));
        Assert.All(cut.FindAll("button[data-format]"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void Render_ShouldTakeADisabledEditorOutOfTheTabOrder_WhenDisabled()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Archived")
            .Add(editor => editor.Disabled, true));

        var area = cut.Find("[data-part=input]");

        Assert.Equal("false", area.GetAttribute("contenteditable"));
        Assert.Equal("-1", area.GetAttribute("tabindex"));
        Assert.Equal("true", area.GetAttribute("aria-disabled"));
        Assert.All(cut.FindAll("button[data-format]"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void Attach_ShouldHandTheScriptTheCanonicalValueAndAllowedFormats_WhenRendered()
    {
        Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Value, "<b>Hi</b> <i>there</i><script>alert(1)</script>")
            .Add(editor => editor.Formats, RichTextFormats.Bold)
            .Add(editor => editor.Blocks, RichTextBlocks.BulletedLists | RichTextBlocks.Quotes)
            .Add(editor => editor.UpdateOnInput, true)
            .Add(editor => editor.UpdateDelay, TimeSpan.FromMilliseconds(250)));

        var arguments = _module.Invocations["attachRichTextEditor"].Single().Arguments;

        Assert.Equal("<p><strong>Hi</strong> there</p>", arguments[1]);
        Assert.Equal("""{"formats":"bold","blocks":"quote bullet","links":null,"transformsPaste":false,"""
            + "\"readOnly\":false,\"updateOnInput\":true,\"updateDelay\":250}", JsonSerializer.Serialize(arguments[2]));
    }

    [Fact]
    public void Render_ShouldShowRuleAndClearFormattingButtons_WhenRulesAndAFormatAreAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.Italic).Add(editor => editor.Blocks, RichTextBlocks.Rules));

        var rule = cut.Find("button[data-command=rule]");
        var clear = cut.Find("button[data-command=clear]");
        var options = JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]);

        Assert.Equal("Horizontal line", rule.GetAttribute("aria-label"));
        Assert.Equal(("Clear formatting", "Control+\\ Meta+\\"),
            (clear.GetAttribute("aria-label"), clear.GetAttribute("aria-keyshortcuts")));
        Assert.All([rule, clear], button => Assert.Null(button.GetAttribute("aria-pressed")));
        Assert.Contains("\"blocks\":\"rule\"", options);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheClearFormattingButton_WhenNoFormatIsAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.Link));

        Assert.Empty(cut.FindAll("button[data-command=clear]"));
    }

    [Fact]
    public void Render_ShouldShowALinkButtonThatOpensADialog_WhenLinksAreAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.Link).Add(editor => editor.LinkSchemes, ["https"]));

        var link = cut.Find("button[data-command=link]");
        var options = JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]);

        Assert.Equal("Link", link.GetAttribute("aria-label"));
        Assert.Equal("dialog", link.GetAttribute("aria-haspopup"));
        Assert.Equal("Control+K Meta+K", link.GetAttribute("aria-keyshortcuts"));
        Assert.Null(link.GetAttribute("aria-pressed"));
        Assert.Contains("\"links\":\"https\"", options);
    }

    [Theory]
    [InlineData("example.com/trail", "https://example.com/trail")]
    [InlineData("ada@example.com", "mailto:ada@example.com")]
    [InlineData(" /trails#map ", "/trails#map")]
    [InlineData("tel:+15550100", "tel:+15550100")]
    public async Task RequestLink_ShouldPutTheCompletedLinkOnTheText_WhenTheDialogIsApplied(string typed,
        string expected)
    {
        _handle.SetupVoid("setLink", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        var request = cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextlink", new RichTextLinkEventArgs());
        host.WaitForElement("dialog");
        Assert.Equal("Add link", host.Find("h2").TextContent);
        await host.Find("input").InputAsync(typed);
        await host.Find("form").SubmitAsync();
        await request;

        Assert.Equal([expected, null], _handle.Invocations["setLink"].Single().Arguments);
    }

    [Fact]
    public async Task RequestLink_ShouldAskForTheTextToo_WhenNothingIsSelected()
    {
        _handle.SetupVoid("setLink", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        var request = cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextlink",
            new RichTextLinkEventArgs { Insert = true });
        host.WaitForElement("dialog");
        await host.FindAll("input")[0].InputAsync("Trail map");
        await host.FindAll("input")[1].InputAsync("https://example.com/map");
        await host.Find("form").SubmitAsync();
        await request;

        Assert.Equal(["https://example.com/map", "Trail map"], _handle.Invocations["setLink"].Single().Arguments);
    }

    [Theory]
    [InlineData(new[] { "https", "javascript" }, "This kind of link isn't allowed here. Allowed: https:")]
    [InlineData(new string[0], "Use a link within this site.")]
    public async Task RequestLink_ShouldRefuseTheLink_WhenItsSchemeIsNotAllowed(string[] schemes, string message)
    {
        _handle.SetupVoid("setLink", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.LinkSchemes, schemes));

        _ = cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextlink", new RichTextLinkEventArgs());
        host.WaitForElement("dialog");
        await host.Find("input").InputAsync("javascript:alert(1)");
        await host.Find("form").SubmitAsync();

        Assert.Contains(message, host.Find("dialog").TextContent);
        Assert.Empty(_handle.Invocations["setLink"]);
    }

    [Fact]
    public async Task RequestLink_ShouldLeaveTheScriptAlone_WhenTheEditorIsGoneBeforeTheDialogCloses()
    {
        _handle.SetupVoid("setLink", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        var request = cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextlink", new RichTextLinkEventArgs());
        host.WaitForElement("dialog");
        await host.Find("input").InputAsync("https://example.com");
        await cut.Instance.DisposeAsync();
        await host.Find("form").SubmitAsync();
        await request;

        Assert.Empty(_handle.Invocations["setLink"]);
    }

    [Fact]
    public async Task RequestLink_ShouldOfferToOpenOrRemoveTheLink_WhenTheTextHasOne()
    {
        _handle.SetupVoid("setLink", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        var request = cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextlink",
            new RichTextLinkEventArgs { Link = "https://example.com" });
        host.WaitForElement("dialog");
        var open = host.Find("a.tb-link");
        Assert.Equal("Edit link", host.Find("h2").TextContent);
        Assert.Equal("https://example.com", host.Find("input").GetAttribute("value"));
        Assert.Equal(("https://example.com", "_blank"), (open.GetAttribute("href"), open.GetAttribute("target")));
        await host.FindAll("button").Single(button => button.TextContent.Trim() == "Remove link").ClickAsync();
        await request;

        Assert.Equal([null, null], _handle.Invocations["setLink"].Single().Arguments);
    }

    [Fact]
    public async Task Paste_ShouldHandTheScriptWhatTheTransformReturns_WhenAPasteArrivesInParts()
    {
        _handle.SetupVoid("insertPasted", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.TransformPaste, document => new RichTextDocument(document.Paragraphs
                .Select(paragraph => new RichTextParagraph([new RichTextRun(paragraph.Text.ToUpperInvariant())])))));
        var area = cut.Find("[data-part=input]");

        await area.TriggerEventAsync("ontbrichtextpaste",
            new RichTextChangeEventArgs { Html = "<p><b>a", Continues = true });
        await area.TriggerEventAsync("ontbrichtextpaste", new RichTextChangeEventArgs { Html = "b</b></p><p>c</p>" });

        Assert.Contains("\"transformsPaste\":true",
            JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]));
        Assert.Equal(["<p>AB</p><p>C</p>"], _handle.Invocations["insertPasted"].Single().Arguments);
    }

    [Fact]
    public async Task Paste_ShouldHandTheScriptThePasteAsItCame_WhenTheTransformIsGoneByTheTimeItArrives()
    {
        _handle.SetupVoid("insertPasted", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        await cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextpaste",
            new RichTextChangeEventArgs { Html = "<p><strong>a</strong></p>" });

        Assert.Equal(["<p><strong>a</strong></p>"], _handle.Invocations["insertPasted"].Single().Arguments);
    }

    [Fact]
    public async Task Change_ShouldSetTheValueToWhatTheEditorAllows_WhenTheScriptSendsHtml()
    {
        string? value = null;
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.Bold)
            .Add(editor => editor.ValueChanged, (string html) => value = html));

        await ChangeAsync(cut, "<p><strong>a</strong><em>b</em></p>");

        Assert.Equal("<p><strong>a</strong>b</p>", value);
        // What the browser sent held a format the editor doesn't allow, so the script gets the cleaned value back.
        Assert.Equal("<p><strong>a</strong>b</p>", Assert.Single(_handle.Invocations["setValue"]).Arguments[0]);
    }

    [Fact]
    public async Task Change_ShouldLeaveTheScriptAlone_WhenItsOwnValueComesBack()
    {
        var cut = Render<BoundEditor>();

        await ChangeAsync(cut, "<p>typed</p>");

        Assert.Equal("<p>typed</p>", cut.Instance.Value);
        Assert.Empty(_handle.Invocations["setValue"]);
    }

    [Fact]
    public async Task Value_ShouldNotReachTheScript_WhenASlowParentPassesBackAnOlderValueWhileTypingGoesOn()
    {
        var parent = new SlowParent();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.ValueChanged, parent.StoreAsync));
        var typed = new[] { ChangeAsync(cut, "<p>a</p>"), ChangeAsync(cut, "<p>ab</p>") };

        // The parent stores each value and passes it back while the next is typed.
        cut.Render(parameters => parameters.Add(editor => editor.Value, "<p>a</p>"));
        await cut.InvokeAsync(parent.FinishAll);
        cut.WaitForAssertion(() => Assert.Equal(["<p>a</p>", "<p>ab</p>"], parent.Stored));
        cut.Render(parameters => parameters.Add(editor => editor.Value, "<p>ab</p>"));
        await cut.InvokeAsync(parent.FinishAll);
        await Task.WhenAll(typed);

        Assert.Empty(_handle.Invocations["setValue"]);
    }

    [Fact]
    public async Task Value_ShouldReachTheScript_WhenAParentThatKeepsNoValueClearsTheEditor()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Value, "<p>init</p>").Add(editor => editor.ValueChanged, (string _) => { }));
        await ChangeAsync(cut, "");
        await ChangeAsync(cut, "<p>hello</p>");

        cut.Render(parameters => parameters.Add(editor => editor.Value, null));

        Assert.Equal("", Assert.Single(_handle.Invocations["setValue"]).Arguments[0]);
    }

    [Fact]
    public async Task Change_ShouldGiveTheParentOnlyTheNewestValue_WhenItsHandlerIsStillRunning()
    {
        var parent = new SlowParent();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.ValueChanged, parent.StoreAsync));

        var typed = new[]
        {
            ChangeAsync(cut, "<p>a</p>"), ChangeAsync(cut, "<p>ab</p>"), ChangeAsync(cut, "<p>abc</p>"),
        };
        Assert.Equal(["<p>a</p>"], parent.Stored);
        await cut.InvokeAsync(parent.FinishAll);

        // Handlers that overlap could finish out of order and leave the parent with an older value.
        cut.WaitForAssertion(() => Assert.Equal(["<p>a</p>", "<p>abc</p>"], parent.Stored));
        await cut.InvokeAsync(parent.FinishAll);
        await Task.WhenAll(typed);
    }

    [Theory]
    [InlineData("<p>a</p>", "<p>a</p>")]
    [InlineData(null, "")]
    public async Task Value_ShouldReachTheScript_WhenTheParentSetsAValueTheEditorSentEarlier(string? value,
        string shown)
    {
        var cut = Render<BoundEditor>();
        await ChangeAsync(cut, "<p>a</p>");
        await ChangeAsync(cut, "");
        await ChangeAsync(cut, "<p>ab</p>");

        await cut.InvokeAsync(() => cut.Instance.Set(value));

        Assert.Equal(shown, Assert.Single(_handle.Invocations["setValue"]).Arguments[0]);
    }

    [Fact]
    public void Value_ShouldReachTheScript_WhenTheParentChangesIt()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        cut.Render(parameters => parameters.Add(editor => editor.Value, "<p>From <b>code</b></p>"));

        Assert.Equal("<p>From <strong>code</strong></p>", Assert.Single(_handle.Invocations["setValue"]).Arguments[0]);
    }

    [Fact]
    public void Options_ShouldReachTheScript_WhenReadOnlyChanges()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        cut.Render(parameters => parameters.Add(editor => editor.ReadOnly, true));

        var options = Assert.Single(_handle.Invocations["setOptions"]).Arguments[0];
        Assert.Equal(true, options!.GetType().GetProperty("readOnly")!.GetValue(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("<p><br></p><p> </p>")]
    public async Task Change_ShouldShowTheRequiredMessage_WhenTheParagraphsHaveNoText(string html)
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Description")
            .Add(editor => editor.Value, "<p>Something</p>").Add(editor => editor.Required, true));

        await ChangeAsync(cut, html);

        Assert.Equal("Description is required.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task Change_ShouldTakeBackTheRequiredMessage_WhenTextComesBack()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Description")
            .Add(editor => editor.Value, "<p>Something</p>").Add(editor => editor.Required, true));
        await ChangeAsync(cut, "");

        await ChangeAsync(cut, "<p>Back</p>");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Render_ShouldShowTheValueBeforeThePageIsInteractive_WhenPrerendered()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Value, "<p>Saved <b>text</b></p><img src=x onerror=alert(1)>"));

        Assert.Equal("<p>Saved <strong>text</strong></p>", cut.Find("[data-part=input]").InnerHtml.Trim());
    }

    [Fact]
    public void Render_ShouldParseTheValueAgain_WhenTheFormatsBlocksOrSchemesChange()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        const string value = "<blockquote><p><b>Hi</b> <a href=\"ftp://example.com\">there</a></p></blockquote>";
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Value, value));
        string Shown() => cut.Find("[data-part=input]").InnerHtml.Trim();
        Assert.Equal("<blockquote><p><strong>Hi</strong> there</p></blockquote>", Shown());

        cut.Render(parameters => parameters.Add(editor => editor.LinkSchemes, ["ftp"]));
        Assert.Contains("<a href=\"ftp://example.com\"", Shown());
        cut.Render(parameters => parameters.Add(editor => editor.Formats, RichTextFormats.Link));
        Assert.DoesNotContain("<strong>", Shown());
        cut.Render(parameters => parameters.Add(editor => editor.Blocks, RichTextBlocks.None));

        Assert.DoesNotContain("<blockquote>", Shown());
    }

    [Fact]
    public void Render_ShouldNotBeEditableOrReady_WhenTheScriptHasntAttachedYet()
    {
        Services.AddSingleton(new JsModuleLoader(new ScriptStillLoading()));

        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        Assert.Equal("false", cut.Find("[data-part=input]").GetAttribute("contenteditable"));
        Assert.False(cut.Find("[data-part=input]").HasAttribute("data-ready"));
    }

    [Fact]
    public void Render_ShouldBeEditableAndReady_WhenTheScriptHasAttached()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-part=input]").GetAttribute("contenteditable")));
        Assert.True(cut.Find("[data-part=input]").HasAttribute("data-ready"));
    }

    [Fact]
    public async Task FocusAsync_ShouldFocusTheEditingArea_WhenCalled()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));

        await cut.InvokeAsync(() => cut.Instance.FocusAsync().AsTask());

        // The script attaches to the editing area, so focus goes where the typing does.
        Assert.Equal(_module.Invocations["attachRichTextEditor"].Single().Arguments[0],
            JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
    }

    [Fact]
    public async Task Change_ShouldJoinTheParts_WhenALongValueComesInSeveralEvents()
    {
        List<string> changes = [];
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.ValueChanged, (string html) => changes.Add(html)));

        await ChangeAsync(cut, "<p>first ", continues: true);
        Assert.Empty(changes);
        await ChangeAsync(cut, "half</p>");

        Assert.Equal(["<p>first half</p>"], changes);
    }

    [Fact]
    public async Task Dispose_ShouldWithdrawTheEditorsAnnouncedErrors_WhenTheEditorGoesAway()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<CascadingValue<int>>(parameters => parameters.Add(parent => parent.Value, 0)
            .AddChildContent<TbRichTextEditor>(editor => editor.Add(notes => notes.Label, "Notes")
                .Add(notes => notes.Required, true).Add(notes => notes.Value, "<p>x</p>")));
        await ChangeAsync(cut, "");
        Assert.Equal(["Notes is required."], Spoken(announcer));

        cut.Render(parameters => parameters.Add(parent => parent.Value, 0)
            .Add(parent => parent.ChildContent, (RenderFragment)(_ => { })));

        Assert.Empty(Spoken(announcer));
    }

    private static Task ChangeAsync<TComponent>(IRenderedComponent<TComponent> cut, string html,
        bool continues = false)
        where TComponent : IComponent =>
        cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextchange",
            new RichTextChangeEventArgs { Html = html, Continues = continues });

    private static IEnumerable<string> Spoken(IRenderedComponent<AnnouncerHost> announcer) =>
        announcer.FindAll("[role=status] p").Select(spoken => spoken.TextContent);

    // A parent whose ValueChanged handler stores each value and then waits until the test lets it finish.
    private sealed class SlowParent
    {
        private readonly List<TaskCompletionSource> _waiting = [];

        public List<string> Stored { get; } = [];

        public async Task StoreAsync(string html)
        {
            Stored.Add(html);
            var finished = new TaskCompletionSource();
            _waiting.Add(finished);
            await finished.Task;
        }

        public void FinishAll()
        {
            var waiting = _waiting.ToList();
            _waiting.Clear();
            foreach (var finished in waiting)
            {
                finished.SetResult();
            }
        }
    }

    private sealed class BoundEditor : ComponentBase
    {
        public string? Value { get; private set; }

        // What a Discard or Clear button does.
        public void Set(string? value)
        {
            Value = value;
            StateHasChanged();
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbRichTextEditor>(0);
            builder.AddComponentParameter(1, nameof(TbRichTextEditor.Label), "Notes");
            builder.AddComponentParameter(2, nameof(TbRichTextEditor.Value), Value);
            builder.AddComponentParameter(3, nameof(TbRichTextEditor.ValueChanged),
                EventCallback.Factory.Create<string>(this, html => Value = html));
            builder.CloseComponent();
        }
    }

    // Never answers, as while the editor's script is still on its way.
    private sealed class ScriptStillLoading : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            new(new TaskCompletionSource<TValue>().Task);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) => new(new TaskCompletionSource<TValue>().Task);
    }
}
