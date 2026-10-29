namespace Trailblazor.BrowserTests;

public class AccordionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldOpenOneItemAtATimeFromTheKeyboard_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        var shipping = page.GetByRole(AriaRole.Button, new() { Name = "Shipping options" });
        var returns = page.GetByRole(AriaRole.Button, new() { Name = "Return policy" });
        await shipping.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Shipping options" })).ToBeVisibleAsync();

        await returns.FocusAsync();
        await page.Keyboard.PressAsync(" ");
        await Expect(returns).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Return policy" })).ToHaveTextAsync("Returns content");
        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByLabel("Notes")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldKeepWhatWasTyped_WhenAnItemIsClosedAndOpenedAgain(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Shipping options" });
        await trigger.ClickAsync();
        await page.GetByLabel("Notes").FillAsync("leave at the door");

        await trigger.ClickAsync();
        await Expect(page.GetByLabel("Notes")).ToBeHiddenAsync();
        await trigger.ClickAsync();

        await Expect(page.GetByLabel("Notes")).ToHaveValueAsync("leave at the door");
    }
}
