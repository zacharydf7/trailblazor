using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AiPromptScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailQuestion_ShouldAddTheAnswerToTheNotes_WhenItIsInserted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/ai-prompt/basic");
        var prompt = page.Tb().AiPrompt("Ask about the ridge trail");

        // PromptAsync writes the prompt, presses Enter and waits until the response is done.
        await prompt.PromptAsync("What should I pack?");
        Assert.Equal([AiPromptStatus.Done], await prompt.StatusesAsync());
        var answer = "Two liters of water, a rain shell and the map. The ridge has no shade after noon, so add a hat.";
        Assert.Equal([answer], await prompt.ResponsesAsync());

        // InsertAsync presses the newest response's Insert button, and the app adds it to its trip notes, so the answer
        // shows twice.
        await prompt.InsertAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Trip notes" })).ToBeVisibleAsync();
        await Expect(page.GetByText(answer)).ToHaveCountAsync(2);
    }
}
