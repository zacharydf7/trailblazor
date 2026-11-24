using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BreadcrumbsScenarioTests : BunitContext
{
    public BreadcrumbsScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trail_ShouldTakeTheUserUpALevel_WhenALinkIsFollowed()
    {
        var cut = Render<BreadcrumbsExample>();
        var trail = cut.Tb().Breadcrumbs();
        Assert.Equal(["Home", "Guides", "Right-to-left"], trail.Items);
        Assert.Equal("Right-to-left", trail.Current);

        // FollowAsync clicks an item's link; bUnit's NavigationManager goes to its address.
        await trail.FollowAsync("Guides");

        Assert.Equal("http://localhost/guides/getting-started", Services.GetRequiredService<NavigationManager>().Uri);
    }
}
