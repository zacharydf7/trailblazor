using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RichTextEditorScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CrewUpdate_ShouldTellTheAppWhomItMentions_WhenAPersonIsMentioned(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rich-text-editor/mentions");
        var update = page.Tb().RichTextEditor("Crew update");

        // MentionAsync types at the end of the editor's text, ending with the trigger and the start of a name,
        // chooses a suggestion, and waits until the app has the text.
        await update.MentionAsync(" Thanks @da", "Dan Ng");

        await Expect(update.Input).ToContainTextAsync("The bridge at Pine Hollow is out. Thanks @Dan Ng");
        await Expect(page.Locator("output")).ToHaveTextAsync("Mentioned: @Dan Ng (u2)");
    }
}
