using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ButtonScenarioTests : BunitContext
{
    public ButtonScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task SendReport_ShouldSendOnlyANamedTrail_WhenClicked()
    {
        var cut = Render<ButtonSubmitExample>();
        var send = cut.Tb().Button("Send report");
        var trail = cut.Tb().TextField("Trail name");

        // Without a trail name the form isn't sent, and the field says why.
        await send.ClickAsync();
        Assert.Equal("Trail name is required.", trail.Error);
        Assert.Equal("Reports sent: 0", cut.Find("output").TextContent);

        await trail.EnterAsync("Pine Hollow loop");
        await send.ClickAsync();

        Assert.Null(trail.Error);
        Assert.Equal("Reports sent: 1", cut.Find("output").TextContent);
    }
}
