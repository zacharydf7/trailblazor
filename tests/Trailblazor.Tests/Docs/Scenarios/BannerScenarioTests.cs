using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BannerScenarioTests : BunitContext
{
    public BannerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Notice_ShouldGoAndComeBack_WhenItIsClosedAndShownAgain()
    {
        var cut = Render<BannerExample>();
        var notice = cut.Tb().Banner("Maintenance tonight");
        Assert.Equal("Trail maps are offline from 10 PM to midnight.", notice.Text);

        // CloseAsync presses the banner's close button, and the app shows a button in its place.
        await notice.CloseAsync();
        Assert.DoesNotContain("Maintenance tonight", cut.Markup);

        await cut.Tb().Button("Show the banner again").ClickAsync();
        Assert.Equal("Trail maps are offline from 10 PM to midnight.", notice.Text);
    }
}
