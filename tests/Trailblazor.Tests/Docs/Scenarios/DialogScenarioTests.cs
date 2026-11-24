using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DialogScenarioTests : BunitContext
{
    public DialogScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task DeleteReport_ShouldKeepOrDeleteTheReport_WhenTheConfirmationIsAnswered()
    {
        // The dialog shows in the app's TbRoot, which a layout renders, so the test renders one too.
        var root = Render<TbRoot>();
        var cut = Render<DialogConfirmExample>();
        var delete = cut.Tb().Button("Delete report");

        // The click's handler waits for the answer, so the test answers before it awaits the click.
        var click = delete.ClickAsync();
        await root.Tb().Dialog("Delete report?").ChooseAsync("Cancel");
        await click;
        Assert.Contains("Kept", cut.Markup);

        click = delete.ClickAsync();
        await root.Tb().Dialog("Delete report?").ChooseAsync("Delete");
        await click;
        Assert.Contains("Deleted", cut.Markup);
        Assert.False(root.Tb().Dialog("Delete report?").IsOpen);
    }
}
