namespace Trailblazor.BrowserTests;

public class OffTriggerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator OpenPanels(IPage page) => page.Locator(":popover-open");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldStayClosed_WhenTheMouseRestsOnACustomTriggerThatIsOff(string browser)
    {
        var page = await OpenAsync(browser, "/off-triggers");

        foreach (var name in new[] { "Profile", "Details" })
        {
            var trigger = Button(page, name);
            await Expect(trigger).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
            await trigger.HoverAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(800);
        }

        await Expect(OpenPanels(page)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldStayClosed_WhenAnArrowIsPressedOnACustomTriggerThatIsOff(string browser)
    {
        var page = await OpenAsync(browser, "/off-triggers");

        foreach (var name in new[] { "Export", "More" })
        {
            var trigger = Button(page, name);
            await Expect(trigger).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
            await trigger.FocusAsync();
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("ArrowUp");
        }

        // The script opens a menu itself, with no server round trip, so two frames after the keys are enough for
        // one to show.
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        await Expect(OpenPanels(page)).ToHaveCountAsync(0);
    }
}
