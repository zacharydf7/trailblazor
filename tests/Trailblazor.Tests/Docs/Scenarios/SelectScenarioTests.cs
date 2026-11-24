using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SelectScenarioTests : BunitContext
{
    public SelectScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Topics_ShouldGiveTheAppWhatIsChosen_WhenTopicsAreChosenTakenAwayAndCleared()
    {
        var cut = Render<SelectMultipleExample>();
        var topics = cut.Tb().Select("Notify me about");
        var value = cut.Find("output");

        await topics.ChooseAsync("Returns", "Offers");
        Assert.Equal(["Orders", "Returns", "Offers"], topics.Chosen);
        Assert.Equal("Notifications value: Orders, Returns, Offers", value.TextContent);

        await topics.UnchooseAsync("Orders");
        Assert.Equal("Notifications value: Returns, Offers", value.TextContent);

        await topics.ClearAsync();
        Assert.Equal("Notifications value:", value.TextContent.Trim());
    }
}
