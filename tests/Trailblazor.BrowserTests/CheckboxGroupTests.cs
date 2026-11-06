using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class CheckboxGroupTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxGroup_ShouldBindTheCheckedOptionsInTheOrderChecked_WhenClickedOrToggledWithSpace(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox-group");
        var days = page.GetByRole(AriaRole.Group, new() { Name = "Trail days" });

        await days.GetByText("Monday").ClickAsync();
        await Expect(page.GetByText("Trail days value: Saturday, Monday", new() { Exact = true })).ToBeVisibleAsync();
        await days.GetByRole(AriaRole.Checkbox, new() { Name = "Wednesday" }).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByText("Trail days value: Saturday, Monday, Wednesday", new() { Exact = true }))
            .ToBeVisibleAsync();
        await days.GetByRole(AriaRole.Checkbox, new() { Name = "Saturday" }).UncheckAsync();

        await Expect(page.GetByText("Trail days value: Monday, Wednesday", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxGroup_ShouldShowItsErrorAndMarkTheGroupInvalid_WhenTheLastOptionIsUnchecked(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox-group");
        var ways = page.GetByRole(AriaRole.Group, new() { Name = "Ways to reach you" });
        await ways.GetByRole(AriaRole.Checkbox, new() { Name = "Email" }).CheckAsync();
        await Expect(ways).Not.ToHaveAttributeAsync("aria-invalid", "true");

        await ways.GetByRole(AriaRole.Checkbox, new() { Name = "Email" }).UncheckAsync();

        await Expect(ways).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(ways).ToHaveAccessibleDescriptionAsync("Pick at least one. Pick at least one way to reach you.");
        await Expect(ways.GetByRole(AriaRole.Checkbox, new() { Name = "Phone call" }))
            .ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxGroup_ShouldPostTheCheckedOptions_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-checkbox-group");

        await page.GetByLabel("Friday").CheckAsync();
        await page.GetByLabel("Monday").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Monday, Friday");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldOpenTheLinkWithoutChecking_WhenALinkInItsRichLabelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox");
        var agree = page.GetByRole(AriaRole.Checkbox, new() { Name = "I agree to the trail rules" });
        await Expect(agree).ToHaveAccessibleNameAsync("I agree to the trail rules (opens in a new tab)");

        var opened = await page.RunAndWaitForPopupAsync(
            () => page.GetByRole(AriaRole.Link, new() { Name = "trail rules" }).ClickAsync());
        await Expect(opened).ToHaveURLAsync(new Regex("guides/forms$"));
        await Expect(agree).Not.ToBeCheckedAsync();
        await page.GetByText("I agree to the").ClickAsync();

        await Expect(agree).ToBeCheckedAsync();
    }
}
