namespace Trailblazor.BrowserTests;

public class RadioGroupTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldBeNamedByItsLegend_WhenLabelIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group");

        await Expect(page.GetByRole(AriaRole.Radiogroup, new() { Name = "Paper size" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldMoveTheSelection_WhenAnArrowKeyIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group");
        await page.GetByRole(AriaRole.Radio, new() { Name = "Letter" }).FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByText("Paper size value: A4", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "A4" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldBeOneTabStopOnTheCheckedOption_WhenTabbedInto(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group");
        await page.GetByRole(AriaRole.Radio, new() { Name = "Portrait" }).FocusAsync();

        // Letter is checked; separate tab stops would land on A4, the group's last enabled option.
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Letter" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldWrapALongOption_WhenTheOptionIsLongerThanTheGroupIsWide(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group");

        var longOption = await page.GetByText("Best fit for the map").BoundingBoxAsync();
        var shortOption = await page.GetByText("Portrait", new() { Exact = true }).BoundingBoxAsync();

        Assert.True(longOption!.Height > shortOption!.Height * 1.5, $"The option is {longOption.Height}px tall.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldNameAnOptionByAllItsContent_WhenItHoldsABadge(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group");

        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Season pass Popular" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldPostItsValue_WhenChosenInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByLabel("Name").FillAsync("Ada");
        await page.GetByRole(AriaRole.Radio, new() { Name = "Pro" }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Ada, False, Pro");
        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Pro" })).ToBeCheckedAsync();
    }
}
