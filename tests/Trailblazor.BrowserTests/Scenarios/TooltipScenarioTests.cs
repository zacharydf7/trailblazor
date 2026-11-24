using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TooltipScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AddButton_ShouldBeDescribedByItsTooltip_WhenThePointerRestsOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip/description");

        // TextAsync rests the pointer on the button named "Add" and reads the tooltip once it shows.
        var tooltip = page.Tb().Tooltip("Add");
        Assert.Equal("Add an item", await tooltip.TextAsync());
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        await Expect(add).ToHaveAccessibleDescriptionAsync("Add an item");

        // Escape hides a tooltip, as WCAG 1.4.13 asks.
        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip.Bubble).ToBeHiddenAsync();
    }
}
