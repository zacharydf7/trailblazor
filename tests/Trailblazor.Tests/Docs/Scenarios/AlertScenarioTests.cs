using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AlertScenarioTests : BunitContext
{
    public AlertScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Warning_ShouldGoAndComeBack_WhenItIsClosedAndShownAgain()
    {
        var cut = Render<AlertCloseExample>();
        // An alert is found by its text.
        var warning = cut.Tb().Alert("Close this alert with its button.");

        // CloseAsync presses the alert's close button, and the app shows a button in its place.
        await warning.CloseAsync();
        Assert.Empty(cut.Tb().Alerts);

        // Alerts reads the text of every alert that shows.
        await cut.Tb().Button("Show the alert again").ClickAsync();
        Assert.Equal(["Close this alert with its button."], cut.Tb().Alerts);
    }
}
