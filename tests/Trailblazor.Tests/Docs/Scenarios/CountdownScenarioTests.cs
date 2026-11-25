using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CountdownScenarioTests : BunitContext
{
    public CountdownScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task ResetRedirect_ShouldStop_WhenTheUserStays()
    {
        var cut = Render<CountdownExample>();

        // A countdown has no helper of its own; its buttons are TbButtons, found by their names.
        await cut.Tb().Button("Stay on this page").ClickAsync();

        Assert.Equal("Stayed on this page", cut.Find(".tb-countdown").TextContent.Trim());
        JSInterop.VerifyFocusAsyncInvoke();
    }
}
