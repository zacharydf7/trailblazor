using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TabsScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TripPlan_ShouldMarkTheTabsThatNeedAttentionAndSave_WhenEachPanelIsFilledIn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs/forms");
        var plan = page.Tb().Tabs("Trip plan");
        var save = page.Tb().Button("Save trip");

        // Saving an empty form marks each tab whose panel holds a problem. The click returns before the form has
        // handled its submit, so Expect waits for the first mark.
        await save.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Leader (needs attention)" })).ToBeVisibleAsync();
        Assert.Equal(["Leader (needs attention)", "Itinerary (needs attention)"], await plan.TabsAsync());

        // A tab loses its mark once its panel's field is filled in.
        await page.Tb().TextField("Leader's name").EnterAsync("Ada");
        Assert.Equal(["Leader", "Itinerary (needs attention)"], await plan.TabsAsync());

        await plan.SelectAsync("Itinerary (needs attention)");
        await page.Tb().TextField("Trail").EnterAsync("Eagle Ridge");
        await save.ClickAsync();

        await Expect(page.GetByText("Saved Ada's trip on Eagle Ridge.")).ToBeVisibleAsync();
        Assert.Equal(["Leader", "Itinerary"], await plan.TabsAsync());
        Assert.Equal("Itinerary", await plan.SelectedAsync());
    }
}
