namespace Trailblazor.BrowserTests;

public class TooltipPopupTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowWithoutAnError_WhenEscapeClosesTheMenuOnItsTrigger(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-popups");
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "More actions" });
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "More actions" })).ToBeVisibleAsync();
        Assert.Empty(errors);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowWithoutAnError_WhenEscapeClosesThePopoverOnItsTrigger(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-popups");
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Share" });
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Copy the link" })).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Share" })).ToBeVisibleAsync();
        Assert.Empty(errors);
    }
}
