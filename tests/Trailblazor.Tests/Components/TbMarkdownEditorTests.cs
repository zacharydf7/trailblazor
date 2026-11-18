using Bunit;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbMarkdownEditorTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;
    private string? _value;

    public TbMarkdownEditorTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/markdown-editor.js");
        _module.SetupModule("createMarkdownEditor", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Render_ShouldShowALabeledTextAreaAndAToolbarOfMarks_WhenCalled()
    {
        var cut = RenderEditor();

        var area = cut.Find("textarea[data-part=input]");
        var tools = cut.FindAll("[role=toolbar] button[data-command]");
        Assert.Equal(area.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal("Formatting", cut.Find("[role=toolbar]").GetAttribute("aria-label"));
        Assert.Equal(["Bold", "Italic", "Strikethrough", "Code", "Link", "Heading 1", "Heading 2", "Heading 3", "Quote",
            "Bulleted list", "Numbered list", "Horizontal line"],
            tools.Select(tool => tool.GetAttribute("aria-label")));
        Assert.Equal("Control+B Meta+B", tools[0].GetAttribute("aria-keyshortcuts"));
        Assert.All(tools, tool => Assert.False(tool.HasAttribute("disabled")));
        Assert.Equal("", cut.Find("[data-part=editor]").GetAttribute("data-ready"));
    }

    [Fact]
    public void Render_ShouldOfferOnlyTheMarksThePreviewKeeps_WhenGivenASchema()
    {
        var cut = Render<TbMarkdownEditor>(parameters => parameters.Add(editor => editor.Label, "Trail notes")
            .Add(editor => editor.Schema, new RichTextSchema
            {
                Formats = RichTextFormats.Bold | RichTextFormats.Link,
                Blocks = RichTextBlocks.BulletedLists | RichTextBlocks.Rules,
            }));

        var tools = cut.FindAll("button[data-command]");
        Assert.Equal(["bold", "link", "bullet", "rule"], tools.Select(tool => tool.GetAttribute("data-command")));
        Assert.Null(tools[^1].GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public async Task EnterAsync_ShouldSetTheBoundMarkdown_WhenTyped()
    {
        var cut = RenderEditor();

        await cut.Tb().MarkdownEditor("Trail notes").EnterAsync("# Pine Hollow");

        Assert.Equal("# Pine Hollow", _value);
        Assert.Equal("# Pine Hollow", cut.Tb().MarkdownEditor("Trail notes").Text);
    }

    [Fact]
    public async Task ShowPreviewAsync_ShouldShowTheMarkdownAsHtmlInPlaceOfTheText_WhenPressed()
    {
        var cut = RenderEditor("**Steep** climb, then [the falls](/falls)");
        var editor = cut.Tb().MarkdownEditor("Trail notes");
        Assert.Null(editor.PreviewHtml);

        await editor.ShowPreviewAsync();

        var preview = cut.Find("[data-part=preview]");
        Assert.Equal("<p><strong>Steep</strong> climb, then <a href=\"/falls\">the falls</a></p>", editor.PreviewHtml);
        Assert.Equal(("region", "Preview"), (preview.GetAttribute("role"), preview.GetAttribute("aria-label")));
        Assert.Equal(preview.Id, cut.Find("[data-part=preview-toggle]").GetAttribute("aria-controls"));
        Assert.True(cut.Find("textarea").HasAttribute("hidden"));
        Assert.All(cut.FindAll("button[data-command]"), tool => Assert.True(tool.HasAttribute("disabled")));
        await editor.HidePreviewAsync();
        Assert.Null(editor.PreviewHtml);
        Assert.False(cut.Find("textarea").HasAttribute("hidden"));
    }

    [Fact]
    public async Task EnterAsync_ShouldUpdateThePreviewBesideTheText_WhenPreviewIsBeside()
    {
        var cut = RenderEditor(preview: MarkdownPreview.Beside);
        var editor = cut.Tb().MarkdownEditor("Trail notes");

        await editor.EnterAsync("- Water\n- Map");

        Assert.Equal("<ul><li>Water</li><li>Map</li></ul>", editor.PreviewHtml);
        Assert.False(cut.Find("textarea").HasAttribute("hidden"));
        Assert.Empty(cut.FindAll("[data-part=preview-toggle]"));
        Assert.Contains("tb-markdown-editor--beside", cut.Find("[data-part=editor]").ClassList);
    }

    [Fact]
    public async Task ShowPreviewAsync_ShouldThrow_WhenTheEditorHasNoPreview()
    {
        var cut = RenderEditor(preview: MarkdownPreview.None, showToolbar: false);

        await Assert.ThrowsAsync<InvalidOperationException>(cut.Tb().MarkdownEditor("Trail notes").ShowPreviewAsync);

        Assert.Empty(cut.FindAll("[role=toolbar], [data-part=preview]"));
    }

    [Fact]
    public void Render_ShouldKeepOnlyThePreviewButton_WhenTheToolbarIsHidden()
    {
        var cut = RenderEditor(showToolbar: false);

        Assert.Empty(cut.FindAll("button[data-command]"));
        Assert.Equal("Preview", cut.Find("[role=toolbar] [data-part=preview-toggle]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task ShowPreviewAsync_ShouldShowLinksAndHtmlThatRunScriptAsText_WhenTheMarkdownHasThem()
    {
        var cut = RenderEditor("[x](javascript:alert(1)) <script>alert(2)</script>");

        await cut.Tb().MarkdownEditor("Trail notes").ShowPreviewAsync();

        Assert.Equal("<p>x &lt;script&gt;alert(2)&lt;/script&gt;</p>",
            cut.Tb().MarkdownEditor("Trail notes").PreviewHtml);
    }

    [Fact]
    public async Task ShowPreviewAsync_ShouldShowAPipeTableAsATable_WhenTheSchemaAllowsTables()
    {
        var cut = Render<TbMarkdownEditor>(parameters => parameters.Add(editor => editor.Label, "Trail notes")
            .Add(editor => editor.Value, "| Trail | Miles |\n| --- | --- |\n| Pine | 3 |")
            .Add(editor => editor.Schema, new RichTextSchema
            {
                Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Tables,
            }));

        await cut.Tb().MarkdownEditor("Trail notes").ShowPreviewAsync();

        Assert.Equal("<table><thead><tr><th scope=\"col\">Trail</th><th scope=\"col\">Miles</th></tr></thead>"
            + "<tbody><tr><td>Pine</td><td>3</td></tr></tbody></table>",
            cut.Tb().MarkdownEditor("Trail notes").PreviewHtml);
    }

    [Fact]
    public void Labels_ShouldNameTheToolbarButtonsAndPreview_WhenGiven()
    {
        var cut = Render<TbMarkdownEditor>(parameters => parameters.Add(editor => editor.Label, "Trail notes")
            .Add(editor => editor.Labels, new MarkdownEditorLabels { Bold = "Strong", Preview = "Look" }));

        Assert.Equal("Strong", cut.Find("[data-command=bold]").GetAttribute("aria-label"));
        Assert.Equal("Look", cut.Find("[data-part=preview-toggle]").GetAttribute("aria-label"));
        Assert.Equal("Italic", cut.Find("[data-command=italic]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task EnterAsync_ShouldThrowAndLeaveTheToolbarOff_WhenTheEditorIsReadOnly()
    {
        var cut = Render<TbMarkdownEditor>(parameters => parameters.Add(editor => editor.Label, "Trail notes")
            .Add(editor => editor.ReadOnly, true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().MarkdownEditor("Trail notes").EnterAsync("text"));

        Assert.All(cut.FindAll("button[data-command]"), tool => Assert.True(tool.HasAttribute("disabled")));
    }

    private IRenderedComponent<TbMarkdownEditor> RenderEditor(string? value = null,
        MarkdownPreview preview = MarkdownPreview.Toggle, bool showToolbar = true)
    {
        _value = value;
        return Render<TbMarkdownEditor>(parameters => parameters.Add(editor => editor.Label, "Trail notes")
            .Add(editor => editor.Value, value).Add(editor => editor.ValueChanged, text => _value = text)
            .Add(editor => editor.Preview, preview).Add(editor => editor.ShowToolbar, showToolbar));
    }
}
