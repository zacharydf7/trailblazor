using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class MarkdownEditorScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailNotes_ShouldPreviewTheMarkdown_WhenThePreviewIsShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/markdown-editor/basic");
        var notes = page.Tb().MarkdownEditor("Trail notes");

        // EnterAsync replaces the editor's markdown, and the app's bound value follows.
        await notes.EnterAsync("**Icy** at dawn; carry spikes.");
        await Expect(page.Locator("output code")).ToHaveTextAsync("**Icy** at dawn; carry spikes.");

        // ShowPreviewAsync presses the Preview button and waits until the preview shows.
        await notes.ShowPreviewAsync();
        Assert.Contains("<strong>Icy</strong> at dawn; carry spikes.", await notes.PreviewHtmlAsync());
    }
}
