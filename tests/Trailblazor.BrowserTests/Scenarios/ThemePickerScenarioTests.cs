using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ThemePickerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Theme_ShouldShowTheChosenMode_WhenAModeIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/theme-picker/modes");
        // The picker is a toggle group named "Theme".
        var theme = page.Tb().ToggleGroup("Theme");
        Assert.Equal(["Match the device"], await theme.ChosenAsync());

        await theme.ChooseAsync("Dark");

        await Expect(page.GetByLabel("Current theme")).ToContainTextAsync("The page is in Dark mode");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-theme", "dark");
    }
}
