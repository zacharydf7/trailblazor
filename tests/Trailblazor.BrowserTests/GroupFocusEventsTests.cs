namespace Trailblazor.BrowserTests;

public class GroupFocusEventsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Readout(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxGroup_ShouldRaiseNoBlurOrFocus_WhenAnotherOptionsLabelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/group-focus-events");
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Tent" }).FocusAsync();
        await Expect(Readout(page, "Focuses")).ToHaveTextAsync("1");

        await page.GetByText("Stove", new() { Exact = true }).ClickAsync();

        // The label's click checks its box, so once that renders, earlier focus events have rendered too.
        await Expect(Readout(page, "Gear")).ToHaveTextAsync("Stove");
        await Expect(Readout(page, "Focuses")).ToHaveTextAsync("1");
        await Expect(Readout(page, "Blurs")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldRaiseNoBlurOrFocus_WhenAnotherOptionsLabelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/group-focus-events");
        await page.GetByRole(AriaRole.Radio, new() { Name = "Slow" }).FocusAsync();
        await Expect(Readout(page, "Pace focuses")).ToHaveTextAsync("1");

        await page.GetByText("Fast", new() { Exact = true }).ClickAsync();

        await Expect(Readout(page, "Pace")).ToHaveTextAsync("Fast");
        await Expect(Readout(page, "Pace focuses")).ToHaveTextAsync("1");
        await Expect(Readout(page, "Pace blurs")).ToHaveTextAsync("0");
    }

    // WebKit doesn't focus a link on a click.
    [Theory, InlineData("chromium"), InlineData("firefox")]
    public async Task CheckboxGroup_ShouldLetALinkInALabelTakeFocus_WhenItIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/group-focus-events");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "guide" });
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Tent" }).FocusAsync();
        await Expect(Readout(page, "Focuses")).ToHaveTextAsync("1");

        await link.ClickAsync();

        await Expect(link).ToBeFocusedAsync();
    }
}
