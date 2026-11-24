using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PopconfirmScenarioTests : BunitContext
{
    public PopconfirmScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task DeleteTrail_ShouldKeepOrDeleteTheTrail_WhenTheQuestionIsAnswered()
    {
        var cut = Render<PopconfirmExample>();
        // A popconfirm is found by the name of the button that opens it.
        var delete = cut.Tb().Popconfirm("Delete trail");
        var status = cut.Find("output");

        // CancelAsync and ConfirmAsync press the question's buttons, as a user's answer does; bUnit doesn't need the
        // question opened first.
        await delete.CancelAsync();
        Assert.Equal("Kept it.", status.TextContent);

        await delete.ConfirmAsync();
        Assert.Equal("Deleted.", status.TextContent);
    }
}
