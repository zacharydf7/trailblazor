using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;

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
    [InlineData(true, RichTextFormats.Link, RichTextBlocks.Rules)]
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
        Assert.Equal(["outdent", "indent"], commands.Select(button => button.GetAttribute("data-command")));
        Assert.All(commands, button => Assert.Null(button.GetAttribute("aria-pressed")));
    }

    [Fact]
    public void Render_ShouldLeaveOutTheIndentButtons_WhenNoListIsAllowed()
    {
        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Blocks, RichTextBlocks.Headings | RichTextBlocks.Quotes));

        Assert.Empty(cut.FindAll("button[data-command]"));
    }

    [Theory]
    [InlineData(RichTextBlocks.Headings | RichTextBlocks.BulletedLists | RichTextBlocks.Rules,
        "<h2>Title</h2><ul><li><code>a</code></li></ul><p>b</p>")]
    [InlineData(RichTextBlocks.None, "<p>Title</p><p><code>a</code></p><p>b</p>")]
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
        Assert.Equal("""{"formats":"bold","blocks":"quote bullet","readOnly":false,"updateOnInput":true,"""
            + "\"updateDelay\":250}", JsonSerializer.Serialize(arguments[2]));
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
    public void Render_ShouldShowTheValueBeforeThePageIsInteractive_WhenPrerendered()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Notes")
            .Add(editor => editor.Value, "<p>Saved <b>text</b></p><img src=x onerror=alert(1)>"));

        Assert.Equal("<p>Saved <strong>text</strong></p>", cut.Find("[data-part=input]").InnerHtml.Trim());
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
}
