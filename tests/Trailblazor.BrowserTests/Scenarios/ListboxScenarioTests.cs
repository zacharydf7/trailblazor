using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ListboxScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Gear_ShouldGiveTheAppWhatIsChosen_WhenOptionsAreChosenAndTakenAway(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox/multiple");
        var gear = page.Tb().ListBox("Gear to pack");
        Assert.Equal(["Tent"], await gear.ChosenAsync());

        await gear.ChooseAsync("Stove", "Headlamp");
        Assert.Equal(["Tent", "Stove", "Headlamp"], await gear.ChosenAsync());
        await Expect(page.GetByText("Packing: Tent, Stove, Headlamp")).ToBeVisibleAsync();

        await gear.UnchooseAsync("Tent");
        Assert.Equal(["Stove", "Headlamp"], await gear.ChosenAsync());
        await Expect(page.GetByText("Packing: Stove, Headlamp")).ToBeVisibleAsync();
    }
}
