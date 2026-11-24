using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TabsScenarioTests : BunitContext
{
    public TabsScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TripPlan_ShouldMarkTheTabsThatNeedAttentionAndSave_WhenEachPanelIsFilledIn()
    {
        var cut = Render<TabsFormExample>();
        var plan = cut.Tb().Tabs("Trip plan");
        var save = cut.Tb().Button("Save trip");

        // Saving an empty form marks each tab whose panel holds a problem.
        await save.ClickAsync();
        Assert.Equal(["Leader (needs attention)", "Itinerary (needs attention)"], plan.Tabs);

        // A tab loses its mark once its panel's field is filled in.
        await cut.Tb().TextField("Leader's name").EnterAsync("Ada");
        Assert.Equal(["Leader", "Itinerary (needs attention)"], plan.Tabs);

        await plan.SelectAsync("Itinerary (needs attention)");
        await cut.Tb().TextField("Trail").EnterAsync("Eagle Ridge");
        await save.ClickAsync();

        Assert.Contains("Saved Ada's trip on Eagle Ridge.", cut.Markup);
        Assert.Equal(["Leader", "Itinerary"], plan.Tabs);
        Assert.Equal("Itinerary", plan.Selected);
    }
}
