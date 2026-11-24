using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AiPromptScenarioTests : BunitContext
{
    public AiPromptScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailQuestion_ShouldAddTheAnswerToTheNotes_WhenItIsInserted()
    {
        var cut = Render<AiPromptExample>();
        var prompt = cut.Tb().AiPrompt("Ask about the ridge trail");

        // PromptAsync writes the prompt and generates, as Enter does. The example's answer streams in a word at a time,
        // so the test waits until it is done.
        await prompt.PromptAsync("What should I pack?");
        cut.WaitForAssertion(() => Assert.Equal([AiPromptStatus.Done], prompt.Statuses), TimeSpan.FromSeconds(5));
        var answer = "Two liters of water, a rain shell and the map. The ridge has no shade after noon, so add a hat.";
        Assert.Equal([answer], prompt.Responses);

        // InsertAsync presses the newest response's Insert button, and the app adds it to its trip notes.
        await prompt.InsertAsync();
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == answer);
    }
}
