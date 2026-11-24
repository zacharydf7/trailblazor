using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ImageScenarioTests : BunitContext
{
    public ImageScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailPhotos_ShouldTellTheAppWhichLoadedAndWhichFailed_WhenTheyLoad()
    {
        var cut = Render<ImageEventsExample>();

        // An image has no helper of its own, and bUnit loads no images, so the test raises the events a browser would.
        await cut.Find("img[alt='The ridge at sunrise']").LoadAsync();
        await cut.Find("img[alt='The lookout over the river valley']").ErrorAsync();

        Assert.Equal("Loaded: True. Failed: True.", cut.Find("output").TextContent);
    }
}
