using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SwitchScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoSave_ShouldGiveTheAppItsState_WhenTurnedOffAndOn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/switch/label-and-help");
        var autoSave = page.Tb().Switch("Auto-save");
        var value = page.Locator("output");
        Assert.True(await autoSave.CheckedAsync());

        await autoSave.UncheckAsync();
        await Expect(value).ToHaveTextAsync("Auto-save value: False");

        await autoSave.CheckAsync();
        await Expect(value).ToHaveTextAsync("Auto-save value: True");
    }
}
