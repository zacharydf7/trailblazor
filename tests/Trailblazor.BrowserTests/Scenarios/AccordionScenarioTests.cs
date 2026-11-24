using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AccordionScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Sizing_ShouldShowItsAnswerAndCloseShipping_WhenExpanded(string browser)
    {
        var page = await OpenAsync(browser, "/examples/accordion/items");
        var sizing = page.Tb().AccordionItem("Sizing");
        var shipping = page.Tb().AccordionItem("Shipping");
        await Expect(shipping.Trigger).ToHaveAttributeAsync("aria-expanded", "true");

        // ExpandAsync gives back the item's panel, to check what it shows.
        var answer = await sizing.ExpandAsync();

        await Expect(answer).ToHaveTextAsync("Sizes run true, so order your usual one.");
        // One item is open at a time unless the accordion is Multiple.
        await Expect(shipping.Trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
