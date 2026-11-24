using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class MarkdownEditorScenarioTests : BunitContext
{
    public MarkdownEditorScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailNotes_ShouldPreviewTheMarkdown_WhenThePreviewIsShown()
    {
        var cut = Render<MarkdownEditorExample>();
        var notes = cut.Tb().MarkdownEditor("Trail notes");

        // EnterAsync replaces the editor's markdown, and the app's bound value follows.
        await notes.EnterAsync("**Icy** at dawn; carry spikes.");
        Assert.Equal("**Icy** at dawn; carry spikes.", cut.Find("output code").TextContent);

        // ShowPreviewAsync presses the Preview button, and PreviewHtml reads what the preview draws.
        await notes.ShowPreviewAsync();
        Assert.Contains("<strong>Icy</strong> at dawn; carry spikes.", notes.PreviewHtml);
    }
}
