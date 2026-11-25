using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AsyncContentScenarioTests : BunitContext
{
    public AsyncContentScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailReports_ShouldShow_WhenRetriedAfterTheFirstLoadFails()
    {
        var cut = Render<AsyncContentFailureExample>();
        var reports = cut.Tb().AsyncContent("trail reports");
        await reports.WaitUntilLoadedAsync();
        Assert.Equal(AsyncContentState.Failed, reports.State);

        // Retry returns once the load it starts has finished.
        await reports.RetryAsync();

        Assert.Equal(AsyncContentState.Content, reports.State);
        Assert.Equal(2, cut.FindAll("li").Count);
    }
}
