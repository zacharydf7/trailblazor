using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CarouselScenarioTests : BunitContext
{
    public CarouselScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task FeaturedTrails_ShouldGiveTheAppTheSlide_WhenTheSlidesAreStepped()
    {
        var cut = Render<CarouselExample>();
        var featured = cut.Tb().Carousel("Featured trails");

        await featured.NextAsync();
        Assert.Equal(1, featured.ActiveIndex);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing Cedar Flats.");

        // GoToAsync shows a slide by its place, counted from 0.
        await featured.GoToAsync(2);
        Assert.Equal(2, featured.ActiveIndex);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing Eagle Ridge traverse.");
    }
}
