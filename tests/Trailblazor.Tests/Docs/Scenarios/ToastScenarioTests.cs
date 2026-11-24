using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ToastScenarioTests : BunitContext
{
    public ToastScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Archive_ShouldRestoreTheReport_WhenTheToastsUndoIsPressed()
    {
        // A toast shows in the app's TbRoot, which a layout renders, so the test renders one too.
        var root = Render<TbRoot>();
        var cut = Render<ToastActionExample>();

        await cut.Tb().Button("Archive report").ClickAsync();
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Archived");

        // ChooseAsync waits for the toast with this text and presses its action.
        await root.Tb().Toasts.ChooseAsync("Report archived", "Undo");

        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Restored");
    }
}
