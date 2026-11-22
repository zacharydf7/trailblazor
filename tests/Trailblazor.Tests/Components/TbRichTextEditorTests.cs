using System.Linq.Expressions;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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

    // .NET reads a color for every tone, so the script and the style sheet have to know each one too: a tone only
    // .NET knew would make the editor load its value again, and lose its undo history, each time it came up.
    [Fact]
    public void Colors_ShouldBeTheTonesInTheScriptAndTheStyleSheet_WhenComparedWithTheToneEnum()
    {
        var tones = Enum.GetNames<Tone>().Select(name => name.ToLowerInvariant()).ToList();
        var assets = Path.Combine(Trailblazor.Tests.Docs.Repository.Root, "src", "Trailblazor", "wwwroot");
        var script = System.Text.RegularExpressions.Regex.Match(
            File.ReadAllText(Path.Combine(assets, "rich-text-editor.js")), @"const COLORS = \[([^\]]*)\]");
        var styled = System.Text.RegularExpressions.Regex.Matches(
            File.ReadAllText(Path.Combine(assets, "trailblazor.css")), "\\[data-tb-color=\"(\\w+)\"\\] \\{");

        Assert.Equal(tones, script.Groups[1].Value.Split(',').Select(name => name.Trim().Trim('\'')));
        Assert.Equal(tones, styled.Select(rule => rule.Groups[1].Value));
    }

    [Fact]
    public void Render_ShouldOfferATextColorMenuWithTheAppsNames_WhenColorIsAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.Color)
            .Add(editor => editor.Labels, new RichTextEditorLabels { TextColor = "Ink", ColorDanger = "Brick" }));

        Assert.Equal("Ink", cut.Find("[data-part=color-menu] button").GetAttribute("aria-label"));
        // Color is a format to clear too.
        Assert.Single(cut.FindAll("button[data-command=clear]"));
        cut.Find("[data-part=color-menu] button").Click();
        cut.WaitForAssertion(() => Assert.Equal(
            ["Default", "Primary", "Secondary", "Gray", "Green", "Amber", "Brick", "Blue"],
            cut.FindAll("[data-part=color-menu] [role=menuitemradio]").Select(item => item.TextContent.Trim())));
        Assert.Equal(["", "primary", "secondary", "neutral", "success", "warning", "danger", "info"],
            cut.FindAll("[data-part=color-menu] [role=menuitemradio]")
                .Select(item => item.GetAttribute("data-color-command")));

        cut.Render(parameters => parameters.Add(editor => editor.Formats, RichTextFormats.Bold));

        Assert.Empty(cut.FindAll("[data-part=color-menu]"));
    }

    [Fact]
    public void Render_ShouldShowAHighlightButtonWithItsShortcutAndTheAppsName_WhenHighlightIsAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Formats, RichTextFormats.Highlight)
            .Add(editor => editor.Labels, new RichTextEditorLabels { Highlight = "Marker" }));

        var button = Assert.Single(cut.FindAll("[role=toolbar] button[data-format]"));

        Assert.Equal("highlight", button.GetAttribute("data-format"));
        Assert.Equal("Marker", button.GetAttribute("aria-label"));
        Assert.Equal("Control+Shift+H Meta+Shift+H", button.GetAttribute("aria-keyshortcuts"));
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
        Assert.Equal("""{"formats":"bold","blocks":"quote bullet","links":null,"images":null,"""
            + "\"mentions\":null,\"slash\":false,\"transformsPaste\":false,"
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
    public async Task RequestLink_ShouldNameTheDialogsCancelButtonFromTheLabels_WhenGiven()
    {
        var host = Render<DialogHost>();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Labels, new RichTextEditorLabels { CancelLink = "Keep the text" }));

        var request = cut.Find("[data-part=input]").TriggerEventAsync("ontbrichtextlink", new RichTextLinkEventArgs());
        host.WaitForElement("dialog");
        var cancel = host.FindAll("button").Single(button => button.TextContent.Trim() == "Keep the text");
        await cancel.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { Detail = 1 });
        await request;

        Assert.Empty(host.FindAll("dialog"));
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
    public async Task Change_ShouldKeepAMentionAndLeaveTheScriptAlone_WhenTheEditorHasMentionTriggers()
    {
        const string Html = "<p>Ask <span data-mention=\"42\" data-trigger=\"@\">@Ada</span> today</p>";
        string? value = null;
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.MentionTriggers, "@").Add(editor => editor.ValueChanged, (string html) => value = html));

        await ChangeAsync(cut, Html);

        // The server reads the mention back as the script wrote it, so it has nothing to correct.
        Assert.Equal(Html, value);
        Assert.Empty(_handle.Invocations["setValue"]);
        var options = JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]);
        Assert.Contains("\"mentions\":\"@\"", options);
    }

    [Fact]
    public async Task Change_ShouldTakeAMentionAsItsText_WhenTheEditorHasNoMentionTriggers()
    {
        string? value = null;
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.ValueChanged, (string html) => value = html));

        await ChangeAsync(cut, "<p>Ask <span data-mention=\"42\" data-trigger=\"@\">@Ada</span> today</p>");

        Assert.Equal("<p>Ask @Ada today</p>", value);
        Assert.Equal("<p>Ask @Ada today</p>", Assert.Single(_handle.Invocations["setValue"]).Arguments[0]);
        var options = JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]);
        Assert.Contains("\"mentions\":null", options);
    }

    [Fact]
    public void Render_ShouldHandTheScriptItsListOfSuggestions_WhenTheEditorHasTriggersAndItemsToSuggest()
    {
        var cut = RenderSuggesting(parameters => parameters.Add(editor => editor.MentionItems, [new("Ada")]));

        var (editor, list) = (cut.Find("[data-part=input]"), cut.Find("[data-part=mention-list]"));
        Assert.Equal((list.Id, "list", "@#"), (editor.GetAttribute("aria-controls"),
            editor.GetAttribute("aria-autocomplete"), editor.GetAttribute("data-triggers")));
        Assert.Equal(("listbox", cut.Find("label").Id),
            (list.GetAttribute("role"), list.GetAttribute("aria-labelledby")));
        Assert.NotNull(Assert.Single(_handle.Invocations["setMentions"]).Arguments[0]);
        var options = JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]);
        Assert.Contains("\"mentions\":\"@#\"", options);
    }

    [Fact]
    public void Render_ShouldLeaveTheSuggestionsOut_WhenTheEditorHasTriggersButNothingToSuggest()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.MentionTriggers, "@"));

        Assert.Empty(cut.FindAll("[data-part=mention-list]"));
        Assert.Null(cut.Find("[data-part=input]").GetAttribute("aria-controls"));
        Assert.Empty(_handle.Invocations["setMentions"]);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheMentionTriggersHaveWhitespace()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbRichTextEditor>(parameters =>
            parameters.Add(editor => editor.Label, "Notes").Add(editor => editor.MentionTriggers, "@ #")));

        Assert.Contains("MentionTriggers", thrown.Message);
    }

    [Fact]
    public async Task Query_ShouldListTheSuggestionsWithTheirIdsAndSayHowMany_WhenTheScriptAsks()
    {
        var announcer = Render<AnnouncerHost>();
        var queries = new List<MentionQuery>();
        var cut = RenderSuggesting(parameters => parameters.Add(editor => editor.MaxMentionItems, 2)
            .Add(editor => editor.MentionSearch, (query, _) =>
            {
                queries.Add(query);
                return Task.FromResult<IEnumerable<MentionItem>>(
                    [new("Ada") { Id = "42", Description = "Math" }, new("Adam"), new("Addie")]);
            }));

        await cut.InvokeAsync(() => MentionEvents().OnQuery("@", "ad", 1));

        Assert.Equal([new MentionQuery("@", "ad")], queries);
        var list = cut.Find("[data-part=mention-list]");
        Assert.Equal("1", list.GetAttribute("data-results-for"));
        Assert.Equal([("@Ada", "42", "Ada"), ("@Adam", null, "Adam")], list.QuerySelectorAll("[role=option]").Select(
            option => (option.GetAttribute("data-insert"), option.GetAttribute("data-mention-id"),
                option.GetAttribute("aria-label"))));
        Assert.Contains("Suggestions: 2", Spoken(announcer));
        await cut.InvokeAsync(() => MentionEvents().OnChosen(0, 1));

        // A choice empties the list, which the script closed as it put the mention in.
        Assert.Empty(cut.FindAll("[data-part=mention-list] [role=option]"));
    }

    private static readonly RichTextCommand Signature = new("Signature")
    {
        Description = "Your name", Keywords = "sign off", Html = "<p><b>Ana</b> <i>Lopez</i><script>x</script></p>",
    };

    private static IEnumerable<(string? Id, string? Name)> Commands(IRenderedComponent<TbRichTextEditor> cut) =>
        cut.FindAll("[data-part=mention-list] [role=option]")
            .Select(option => (option.GetAttribute("data-mention-id"), option.GetAttribute("aria-label")));

    [Fact]
    public async Task SlashCommands_ShouldListTheBlocksAllowedThenTheAppsCommands_WhenTheScriptAsksForASlash()
    {
        _handle.SetupVoid("setMentions", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.SlashCommands, true)
            .Add(editor => editor.Blocks, RichTextBlocks.Headings | RichTextBlocks.Rules)
            .Add(editor => editor.Commands, [Signature]));

        Assert.Contains("\"slash\":true",
            JsonSerializer.Serialize(_module.Invocations["attachRichTextEditor"].Single().Arguments[2]));
        await cut.InvokeAsync(() => MentionEvents().OnQuery("/", "", 1));

        Assert.Equal([("heading1", "Heading 1"), ("heading2", "Heading 2"), ("heading3", "Heading 3"),
            ("rule", "Horizontal line"), ("app:0", "Signature")], Commands(cut));
        // What is typed narrows them by name, or by the keywords of one of the app's commands.
        await cut.InvokeAsync(() => MentionEvents().OnQuery("/", "3", 2));
        Assert.Equal([("heading3", "Heading 3")], Commands(cut));
        await cut.InvokeAsync(() => MentionEvents().OnQuery("/", "off", 3));
        Assert.Equal([("app:0", "Signature")], Commands(cut));
    }

    [Fact]
    public async Task SlashCommands_ShouldPutInTheAppsHtmlAsTheEditorAllowsAndCallItBack_WhenItsCommandIsChosen()
    {
        var called = 0;
        var stamp = new RichTextCommand("Stamp") { OnChosen = () => Task.FromResult(called++) };
        _handle.SetupVoid("setMentions", _ => true).SetVoidResult();
        _handle.SetupVoid("insertPasted", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.SlashCommands, true).Add(editor => editor.Formats, RichTextFormats.Bold)
            .Add(editor => editor.Commands, [Signature, stamp]));

        // The script names one of the app's commands once the slash is out. Choosing any command only empties the
        // list, and one of the editor's own the script runs itself.
        await cut.InvokeAsync(() => MentionEvents().OnQuery("/", "", 1));
        await cut.InvokeAsync(() => MentionEvents().OnChosen(1, 1));
        await cut.InvokeAsync(() => MentionEvents().OnCommand("heading1"));
        Assert.Empty(_handle.Invocations["insertPasted"]);
        await cut.InvokeAsync(() => MentionEvents().OnCommand("app:0"));

        Assert.Equal("<p><strong>Ana</strong> Lopez</p>", _handle.Invocations["insertPasted"].Single().Arguments[0]);
        Assert.Equal(0, called);
        await cut.InvokeAsync(() => MentionEvents().OnCommand("app:1"));

        Assert.Equal(1, called);
        Assert.Single(_handle.Invocations["insertPasted"]);
    }

    [Fact]
    public async Task SlashCommands_ShouldRunTheCommandThatWasListed_WhenTheAppsCommandsChangeBeforeTheChoice()
    {
        var called = 0;
        _handle.SetupVoid("setMentions", _ => true).SetVoidResult();
        _handle.SetupVoid("insertPasted", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.SlashCommands, true).Add(editor => editor.Commands, [Signature]));
        await cut.InvokeAsync(() => MentionEvents().OnQuery("/", "", 1));

        cut.Render(parameters => parameters.Add(editor => editor.Commands,
            [new RichTextCommand("Stamp") { OnChosen = () => Task.FromResult(called++) }]));
        await cut.InvokeAsync(() => MentionEvents().OnCommand("app:0"));

        Assert.Single(_handle.Invocations["insertPasted"]);
        Assert.Equal(0, called);
    }

    [Fact]
    public async Task SlashCommands_ShouldSayNothingAtAMentionTrigger_WhenTheEditorHasNoMentionsToSuggest()
    {
        var announcer = Render<AnnouncerHost>();
        _handle.SetupVoid("setMentions", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.SlashCommands, true).Add(editor => editor.MentionTriggers, "@"));

        // The script asks, since the list is there for the commands, but the editor has no mentions to look for.
        await cut.InvokeAsync(() => MentionEvents().OnQuery("@", "ad", 1));

        Assert.Empty(Spoken(announcer));
        Assert.Equal("1", cut.Find("[data-part=mention-list]").GetAttribute("data-results-for"));
    }

    [Fact]
    public void SlashCommands_ShouldRejectASlashAmongTheMentionTriggers_WhenBothAreSet() =>
        Assert.Throws<InvalidOperationException>(() => Render<TbRichTextEditor>(parameters => parameters
            .Add(editor => editor.Label, "Notes").Add(editor => editor.SlashCommands, true)
            .Add(editor => editor.MentionTriggers, "@/")));

    [Fact]
    public async Task Query_ShouldEmptyTheListAndTellTheApp_WhenTheSearchFails()
    {
        var announcer = Render<AnnouncerHost>();
        Exception? failure = null;
        var cut = RenderSuggesting(parameters => parameters
            .Add(editor => editor.MentionSearch, (_, _) => throw new IOException("Down"))
            .Add(editor => editor.OnMentionSearchFailed, (Exception exception) => failure = exception)
            .Add(editor => editor.Labels, new RichTextEditorLabels { MentionSearchFailed = "No people today." }));

        await cut.InvokeAsync(() => MentionEvents().OnQuery("@", "a", 3));

        Assert.IsType<IOException>(failure);
        Assert.Equal("3", cut.Find("[data-part=mention-list]").GetAttribute("data-results-for"));
        Assert.Contains("No people today.", Spoken(announcer));
    }

    [Fact]
    public async Task OnMentionsChanged_ShouldReportOnlyWhatTheUserChanges_WhenTheValueGainsAndLosesMentions()
    {
        const string Ada = "<span data-mention=\"42\" data-trigger=\"@\">@Ada</span>";
        var reported = new List<IReadOnlyList<Mention>>();
        var cut = RenderSuggesting(parameters => parameters.Add(editor => editor.Value, $"<p>{Ada}</p>")
            .Add(editor => editor.OnMentionsChanged, reported.Add));

        // The app set the first value itself, and an edit that keeps the same mentions is no news either.
        await ChangeAsync(cut, $"<p>{Ada} and more</p>");
        Assert.Empty(reported);
        await ChangeAsync(cut, $"<p>{Ada} and <span data-mention=\"7\" data-trigger=\"#\">#trails</span></p>");
        await ChangeAsync(cut, "<p>and</p>");

        Assert.Equal(2, reported.Count);
        Assert.Equal([new Mention("@", new MentionItem("Ada") { Id = "42" }),
            new Mention("#", new MentionItem("trails") { Id = "7" })], reported[0]);
        Assert.Empty(reported[1]);
    }

    [Fact]
    public async Task OnMentionsChanged_ShouldReportAMentionOnce_WhenTheAppNeverStoresTheValueItIsGiven()
    {
        const string Ada = "<span data-mention=\"42\" data-trigger=\"@\">@Ada</span>";
        var reported = new List<IReadOnlyList<Mention>>();
        var cut = RenderSuggesting(parameters => parameters.Add(editor => editor.Value, "<p>x</p>")
            .Add(editor => editor.OnMentionsChanged, reported.Add));

        // The parent renders again with the value it had, as one that never stores the value it is given does.
        await ChangeAsync(cut, $"<p>{Ada}</p>");
        cut.Render(parameters => parameters.Add(editor => editor.Value, "<p>x</p>"));
        await ChangeAsync(cut, $"<p>{Ada} and more</p>");
        Assert.Single(reported);

        // Taking the mention out again gives the value the parent still holds and has just passed in again, so the
        // value itself is no news to it, but the app hears that the mention has gone.
        cut.Render(parameters => parameters.Add(editor => editor.Value, "<p>x</p>"));
        await ChangeAsync(cut, "<p>x</p>");

        Assert.Equal(2, reported.Count);
        Assert.Empty(reported[1]);
    }

    [Fact]
    public async Task Commit_ShouldCountOnlyOnceTheAppHasHeardOfTheMentions_WhenItsHandlerTakesAWhile()
    {
        var heard = new TaskCompletionSource();
        var cut = RenderSuggesting(parameters => parameters.Add(editor => editor.OnMentionsChanged, _ => heard.Task));
        string? Commits() => cut.Find("[data-commits]").GetAttribute("data-commits");
        var before = Commits();

        // A test helper that waits for the commit must not return while the app is still handling the mentions.
        var change = ChangeAsync(cut, "<p><span data-mention=\"42\" data-trigger=\"@\">@Ada</span></p>");
        cut.Render();
        Assert.Equal(before, Commits());
        heard.SetResult();
        await change;

        cut.WaitForAssertion(() => Assert.NotEqual(before, Commits()));
    }

    private IRenderedComponent<TbRichTextEditor> RenderSuggesting(
        Action<ComponentParameterCollectionBuilder<TbRichTextEditor>> configure)
    {
        _handle.SetupVoid("setMentions", _ => true).SetVoidResult();
        return Render<TbRichTextEditor>(parameters => configure(parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.MentionTriggers, "@#")));
    }

    // The .NET object the script calls back as a trigger is typed and a suggestion chosen.
    private MentionInputEvents MentionEvents() =>
        ((DotNetObjectReference<MentionInputEvents>)_handle.Invocations["setMentions"].Single().Arguments[1]!).Value;

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

    [Theory]
    [InlineData(null, "4", "4 characters")]
    [InlineData(10, "4 / 10", "4 of 10 characters")]
    public void Render_ShouldCountTheTextWithoutItsBreaksAndDescribeTheEditorWithIt_WhenShowCount(int? maxLength,
        string shown, string said)
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Value, "<p>ab</p><p>c<br>d</p>").Add(editor => editor.ShowCount, true)
            .Add(editor => editor.MaxLength, maxLength));

        var count = cut.Find("[data-part=count]");

        Assert.Equal((shown, said), (count.QuerySelector("[data-part=count-shown]")!.TextContent,
            count.QuerySelector("[data-part=count-said]")!.TextContent));
        Assert.Contains(count.Id, cut.Find("[data-part=input]").GetAttribute("aria-describedby")!.Split(' '));
        Assert.Equal(count.Id, cut.Find("[data-part=input]").GetAttribute("data-count"));
    }

    [Theory]
    [InlineData("<p>Pine <b>Hollow</b></p>", false)]
    [InlineData("<p>Pine</p><p>Hollow</p>", true)]
    public void Submit_ShouldBlockTheFormOnlyForTextPastMaxLength_WhenItsBreaksArentCounted(string report, bool sent)
    {
        var trip = new Trip { Report = report };
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters.Add(form => form.Model, trip)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => builder =>
            {
                builder.OpenComponent<TbRichTextEditor>(0);
                builder.AddAttribute(1, "Value", trip.Report);
                builder.AddAttribute(2, "ValueExpression", (Expression<Func<string>>)(() => trip.Report!));
                builder.AddAttribute(3, "Label", "Trail report");
                builder.AddAttribute(4, "MaxLength", 10);
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        Assert.Equal(sent, submitted);
        Assert.Equal(sent ? [] : ["Trail report can be at most 10 characters."],
            cut.FindAll("[data-part=error]").Select(error => error.TextContent.Trim()));
    }

    private sealed class Trip
    {
        public string? Report { get; set; }
    }

    [Fact]
    public async Task ShowSource_ShouldFlushTheEditorAndSwapItForItsHtml_WhenTheSourceButtonIsPressed()
    {
        _handle.SetupVoid("flush", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Value, "<p>Pine <b>Hollow</b></p>").Add(editor => editor.ShowSource, true));
        var toggle = cut.Find("[data-part=source-toggle]");
        Assert.Equal(("HTML source", "false"),
            (toggle.GetAttribute("aria-label"), toggle.GetAttribute("aria-pressed")));

        await toggle.ClickAsync(new());

        var source = cut.Find("[data-part=source]");
        Assert.Single(_handle.Invocations["flush"]);
        Assert.Equal("<p>Pine <strong>Hollow</strong></p>", source.GetAttribute("value"));
        Assert.Equal(cut.Find("[data-part=label]").Id, source.GetAttribute("aria-labelledby"));
        Assert.True(cut.Find("[data-part=input]").HasAttribute("hidden"));
        Assert.Equal("true", cut.Find("[data-part=source-toggle]").GetAttribute("aria-pressed"));
        Assert.All(cut.FindAll("[role=toolbar] button[data-format]"),
            button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public async Task Source_ShouldKeepOnlyWhatTheEditorAllows_WhenItsHtmlIsChanged()
    {
        _handle.SetupVoid("flush", _ => true).SetVoidResult();
        List<string> changes = [];
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Formats, RichTextFormats.Bold).Add(editor => editor.ShowSource, true)
            .Add(editor => editor.ValueChanged, (string html) => changes.Add(html)));
        await cut.Find("[data-part=source-toggle]").ClickAsync(new());

        await cut.Find("[data-part=source]")
            .ChangeAsync(new() { Value = "<p><b>Pine</b> <i>Hollow</i><script>x()</script></p>" });

        Assert.Equal(["<p><strong>Pine</strong> Hollow</p>"], changes);
        Assert.Equal("<p><strong>Pine</strong> Hollow</p>", _handle.Invocations["setValue"].Last().Arguments[0]);
    }

    [Fact]
    public async Task ShowSource_ShouldBringBackTheEditingArea_WhenTheParentTakesTheSourceButtonAway()
    {
        _handle.SetupVoid("flush", _ => true).SetVoidResult();
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.ShowSource, true));
        await cut.Find("[data-part=source-toggle]").ClickAsync(new());

        cut.Render(parameters => parameters.Add(editor => editor.ShowSource, false));
        var hidden = cut.FindAll("[data-part=source]").Count;
        var enabled = cut.FindAll("[role=toolbar] button[data-format]").All(button => !button.HasAttribute("disabled"));
        // Shown again, the button starts unpressed, so the next press sends the editor's text first.
        cut.Render(parameters => parameters.Add(editor => editor.ShowSource, true));

        Assert.Equal((0, true), (hidden, enabled));
        Assert.Empty(cut.FindAll("[data-part=source]"));
        Assert.False(cut.Find("[data-part=input]").HasAttribute("hidden"));
        Assert.Equal("false", cut.Find("[data-part=source-toggle]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task Source_ShouldTakeTheFieldsInvalidStateAndFocus_WhenItShowsAndTheFormFailsToSubmit()
    {
        _handle.SetupVoid("flush", _ => true).SetVoidResult();
        var trip = new Trip();
        var cut = Render<EditForm>(parameters => parameters.Add(form => form.Model, trip)
            .Add(form => form.ChildContent, (EditContext _) => builder =>
            {
                builder.OpenComponent<TbRichTextEditor>(0);
                builder.AddAttribute(1, "Value", trip.Report);
                builder.AddAttribute(2, "ValueExpression", (Expression<Func<string>>)(() => trip.Report!));
                builder.AddAttribute(3, "Label", "Trail report");
                builder.AddAttribute(4, "Required", true);
                builder.AddAttribute(5, "ShowSource", true);
                builder.CloseComponent();
            }));
        var editor = cut.FindComponent<TbRichTextEditor>();
        await cut.Find("[data-part=source-toggle]").ClickAsync(new());
        // bUnit drops an element's reference mark once the submit renders again, so read it first.
        var reference = cut.Find("[data-part=source]").GetAttribute("blazor:elementReference");

        cut.Find("form").Submit();
        await cut.InvokeAsync(() => editor.Instance.FocusAsync().AsTask());

        var source = cut.Find("[data-part=source]");
        Assert.Equal(("true", "true"), (source.GetAttribute("aria-invalid"), source.GetAttribute("aria-required")));
        Assert.Null(cut.Find("[data-part=input]").GetAttribute("aria-invalid"));
        Assert.Equal(reference, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public void Render_ShouldKeepImagesAndTellTheScriptTheirSchemes_WhenTheBlocksAllowThem()
    {
        Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Blocks, RichTextBlocks.Images)
            .Add(editor => editor.Value, "<p>Pine</p><img src=\"https://example.com/pine.png\" alt=\"Pine\">"));

        var arguments = _module.Invocations["attachRichTextEditor"].Single().Arguments;
        Assert.Equal("<p>Pine</p><img src=\"https://example.com/pine.png\" alt=\"Pine\">", arguments[1]);
        Assert.Contains("\"images\":\"https http\"", JsonSerializer.Serialize(arguments[2]));
    }

    [Fact]
    public void Render_ShouldDropAnImage_WhenItsSchemeIsntAllowed()
    {
        Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Blocks, RichTextBlocks.Images).Add(editor => editor.ImageSchemes, ["https"])
            .Add(editor => editor.Value, "<p>Pine</p><img src=\"http://example.com/pine.png\" alt=\"Pine\">"));

        var arguments = _module.Invocations["attachRichTextEditor"].Single().Arguments;
        Assert.Equal("<p>Pine</p>", arguments[1]);
        Assert.Contains("\"images\":\"https\"", JsonSerializer.Serialize(arguments[2]));
    }

    private static InputFileContent Photo =>
        InputFileContent.CreateFromBinary([1, 2, 3], "trail.png", contentType: "image/png");

    // An editor that allows images and stores each through upload, which records the file it got.
    private IRenderedComponent<TbRichTextEditor> RenderWithImages(
        Func<IBrowserFile, CancellationToken, Task<string>> upload,
        Action<ComponentParameterCollectionBuilder<TbRichTextEditor>>? more = null) =>
        Render<TbRichTextEditor>(parameters =>
        {
            parameters.Add(editor => editor.Label, "Trail report").Add(editor => editor.Blocks, RichTextBlocks.Images)
                .Add(editor => editor.UploadImage, upload);
            more?.Invoke(parameters);
        });

    private static Task<string> Stored(string url) => Task.FromResult(url);

    [Fact]
    public void Render_ShouldShowAnImageButton_WhenImagesAndAnUploadAreGiven()
    {
        var cut = RenderWithImages((_, _) => Stored("https://example.com/trail.png"));
        var withoutUpload = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Blocks, RichTextBlocks.Images));
        var withoutImages = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.UploadImage, (_, _) => Stored("https://example.com/trail.png")));

        var button = cut.Find("button[data-part=image]");

        Assert.Equal(("Image", "dialog"), (button.GetAttribute("aria-label"), button.GetAttribute("aria-haspopup")));
        Assert.Empty(withoutUpload.FindAll("button[data-part=image]"));
        Assert.Empty(withoutImages.FindAll("button[data-part=image]"));
    }

    [Fact]
    public void Render_ShouldShowTheImageButton_WhenNoOtherToolIsAllowed()
    {
        var cut = RenderWithImages((_, _) => Stored("https://example.com/trail.png"),
            parameters => parameters.Add(editor => editor.Formats, RichTextFormats.None));

        Assert.Single(cut.FindAll("[data-part=toolbar]"));
        Assert.Single(cut.FindAll("button[data-part=image]"));
    }

    [Fact]
    public void Render_ShouldTurnTheImageButtonOff_WhenTheEditorIsReadOnly()
    {
        var cut = RenderWithImages((_, _) => Stored("https://example.com/trail.png"),
            parameters => parameters.Add(editor => editor.ReadOnly, true));

        Assert.True(cut.Find("button[data-part=image]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task InsertImage_ShouldRefuseTheFile_WhenItsLargerThanMaxImageSize()
    {
        var host = Render<DialogHost>();
        var uploads = 0;
        var cut = RenderWithImages((_, _) =>
        {
            uploads++;
            return Stored("https://example.com/trail.png");
        }, parameters => parameters.Add(editor => editor.MaxImageSize, 2));

        _ = cut.Find("button[data-part=image]").ClickAsync(new() { Detail = 1 });
        host.WaitForElement("dialog");
        host.FindComponent<InputFile>().UploadFiles(Photo);
        await host.Tb().TextField("Alt text").EnterAsync("Pine trail");
        await host.Find("form").SubmitAsync();

        Assert.Equal(0, uploads);
        Assert.Single(host.FindAll("dialog"));
    }

    [Fact]
    public async Task InsertImage_ShouldCancelTheUploadAndPutNothingIn_WhenTheDialogIsCanceledWhileItStores()
    {
        var host = Render<DialogHost>();
        var token = CancellationToken.None;
        var cut = RenderWithImages(async (_, given) =>
        {
            token = given;
            await Task.Delay(Timeout.Infinite, given);
            return "https://example.com/trail.png";
        });

        var inserting = cut.Find("button[data-part=image]").ClickAsync(new() { Detail = 1 });
        host.WaitForElement("dialog");
        host.FindComponent<InputFile>().UploadFiles(Photo);
        await host.Tb().TextField("Alt text").EnterAsync("Pine trail");
        _ = host.Find("form").SubmitAsync();
        await host.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel")
            .ClickAsync(new() { Detail = 1 });
        await inserting;

        Assert.True(token.IsCancellationRequested);
        Assert.Empty(_handle.Invocations["insertImage"]);
    }

    [Fact]
    public async Task InsertImage_ShouldStoreTheFileAndPutTheImageIn_WhenTheDialogIsFilled()
    {
        _handle.SetupVoid("insertImage", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        string? stored = null;
        var cut = RenderWithImages((file, _) =>
        {
            stored = file.Name;
            return Stored(" https://example.com/trail.png ");
        });

        var inserting = cut.Find("button[data-part=image]").ClickAsync(new() { Detail = 1 });
        host.WaitForElement("dialog");
        Assert.Equal("Insert image", host.Find("h2").TextContent);
        host.FindComponent<InputFile>().UploadFiles(Photo);
        await host.Tb().TextField("Alt text").EnterAsync("Pine trail at dawn");
        await host.Find("form").SubmitAsync();
        await inserting;

        Assert.Equal("trail.png", stored);
        Assert.Equal(["https://example.com/trail.png", "Pine trail at dawn"],
            _handle.Invocations["insertImage"].Single().Arguments);
    }

    [Fact]
    public async Task InsertImage_ShouldGiveNoAltText_WhenTheImageIsDecorative()
    {
        _handle.SetupVoid("insertImage", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        var cut = RenderWithImages((_, _) => Stored("/images/divider.png"));

        var inserting = cut.Find("button[data-part=image]").ClickAsync(new() { Detail = 1 });
        host.WaitForElement("dialog");
        host.FindComponent<InputFile>().UploadFiles(Photo);
        await host.Tb().TextField("Alt text").EnterAsync("Divider");
        await host.Tb().Checkbox("Decorative").CheckAsync();
        await host.Find("form").SubmitAsync();
        await inserting;

        Assert.Equal(["/images/divider.png", ""], _handle.Invocations["insertImage"].Single().Arguments);
    }

    [Fact]
    public async Task InsertImage_ShouldAskForAltText_WhenTheImageIsntDecorative()
    {
        var host = Render<DialogHost>();
        var uploads = 0;
        var cut = RenderWithImages((_, _) =>
        {
            uploads++;
            return Stored("https://example.com/trail.png");
        });

        _ = cut.Find("button[data-part=image]").ClickAsync(new() { Detail = 1 });
        host.WaitForElement("dialog");
        host.FindComponent<InputFile>().UploadFiles(Photo);
        await host.Find("form").SubmitAsync();

        Assert.Equal(0, uploads);
        Assert.Single(host.FindAll("dialog"));
        Assert.NotNull(host.Tb().TextField("Alt text").Error);
    }

    [Theory]
    [InlineData("http://example.com/trail.png", "The image's address isn't one this editor allows.")]
    [InlineData(null, "The image couldn't be stored. Try again.")]
    public async Task InsertImage_ShouldSayWhyAndStayOpen_WhenTheUploadFailsOrGivesAnAddressNotAllowed(string? url,
        string message)
    {
        var host = Render<DialogHost>();
        var cut = RenderWithImages((_, _) => url is null ? throw new IOException("The disk is full.") : Stored(url),
            parameters => parameters.Add(editor => editor.ImageSchemes, ["https"]));

        _ = cut.Find("button[data-part=image]").ClickAsync(new() { Detail = 1 });
        host.WaitForElement("dialog");
        host.FindComponent<InputFile>().UploadFiles(Photo);
        await host.Tb().TextField("Alt text").EnterAsync("Pine trail");
        await host.Find("form").SubmitAsync();

        Assert.Equal(message, host.Find("[role=alert], .tb-alert").TextContent.Trim());
        Assert.Single(host.FindAll("dialog"));
        Assert.Empty(_handle.Invocations["insertImage"]);
    }

    private IRenderedComponent<TbRichTextEditor> RenderWithTables(RichTextTableState state)
    {
        _handle.Setup<RichTextTableState>("tableState", _ => true).SetResult(state);
        return Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Blocks, RichTextBlocks.Tables));
    }

    private static string? Disabled(IRenderedComponent<TbRichTextEditor> cut, string item) =>
        cut.FindAll("[role=menuitem]").Single(element => element.TextContent.Trim() == item)
            .GetAttribute("aria-disabled");

    [Fact]
    public void Render_ShouldOfferATableButtonAndATableMenu_WhenTheEditorAllowsTables()
    {
        var cut = RenderWithTables(new(false, false, false));
        var without = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes"));
        var named = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Blocks, RichTextBlocks.Tables)
            .Add(editor => editor.Labels, new RichTextEditorLabels { Table = "Grid", TableMenu = "Grid tools" }));

        Assert.Equal("Table", cut.Find("button[data-command=table]").GetAttribute("aria-label"));
        Assert.Equal("Table options", cut.Find("[data-part=table-menu] button").GetAttribute("aria-label"));
        Assert.Empty(without.FindAll("button[data-command=table], [data-part=table-menu]"));
        Assert.Equal("Grid", named.Find("button[data-command=table]").GetAttribute("aria-label"));
        Assert.Equal("Grid tools", named.Find("[data-part=table-menu] button").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task TableMenu_ShouldOfferTheCommandsThatFit_WhenTheCaretIsInATablesHeaderRow()
    {
        var cut = RenderWithTables(new(InCell: true, InHeaderRow: true, HasHeaderRow: true));
        var menu = cut.Tb().Menu("Table options");

        await menu.OpenAsync();
        cut.WaitForAssertion(() => Assert.True(menu.Checked("Header row")));

        Assert.Equal("true", Disabled(cut, "Add row above"));
        Assert.Null(Disabled(cut, "Add row below"));
        Assert.Equal(["rowAbove", "rowBelow", "columnLeft", "columnRight", "header", "deleteRow", "deleteColumn",
            "deleteTable"],
            cut.FindAll("[data-table-command]").Select(item => item.GetAttribute("data-table-command")));
    }

    [Fact]
    public async Task TableMenu_ShouldTurnItsCommandsOff_WhenItOpensAgainWithTheCaretOutsideATable()
    {
        var cut = RenderWithTables(new(InCell: true, InHeaderRow: false, HasHeaderRow: true));
        var menu = cut.Tb().Menu("Table options");
        await menu.OpenAsync();
        cut.WaitForAssertion(() => Assert.Null(Disabled(cut, "Delete table")));
        await menu.CloseAsync();
        _handle.Setup<RichTextTableState>("tableState", _ => true)
            .SetResult(new(InCell: false, InHeaderRow: false, HasHeaderRow: false));

        await menu.OpenAsync();

        cut.WaitForAssertion(() => Assert.All(cut.FindAll("[data-part=table-menu] [role^=menuitem]"),
            item => Assert.Equal("true", item.GetAttribute("aria-disabled"))));
    }

    [Fact]
    public void Render_ShouldTurnTheTableMenuOff_WhenTheEditorIsReadOnly()
    {
        _handle.Setup<RichTextTableState>("tableState", _ => true).SetResult(new(false, false, false));
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Trail report")
            .Add(editor => editor.Blocks, RichTextBlocks.Tables).Add(editor => editor.ReadOnly, true));

        Assert.True(cut.Find("[data-part=table-menu] button").HasAttribute("disabled"));
        Assert.True(cut.Find("button[data-command=table]").HasAttribute("disabled"));
    }
}
