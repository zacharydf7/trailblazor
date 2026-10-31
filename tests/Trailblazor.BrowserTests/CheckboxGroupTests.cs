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
        var echo = page.GetByTestId("days-echo");

        await days.GetByText("Monday").ClickAsync();
        await Expect(echo).ToHaveTextAsync("Saturday, Monday");
        await days.GetByRole(AriaRole.Checkbox, new() { Name = "Wednesday" }).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(echo).ToHaveTextAsync("Saturday, Monday, Wednesday");
        await days.GetByRole(AriaRole.Checkbox, new() { Name = "Saturday" }).UncheckAsync();

        await Expect(echo).ToHaveTextAsync("Monday, Wednesday");
        await Expect(days.GetByRole(AriaRole.Checkbox, new() { Name = "Sunday" })).ToBeDisabledAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxGroup_ShouldShowItsErrorAndMarkTheGroupInvalid_WhenTheLastOptionIsUnchecked(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox-group");
        var days = page.GetByRole(AriaRole.Group, new() { Name = "Trail days" });

        await days.GetByRole(AriaRole.Checkbox, new() { Name = "Saturday" }).UncheckAsync();

        await Expect(days).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(days).ToHaveAccessibleDescriptionAsync("Pick at least one. Pick at least one day.");
        await Expect(days.GetByRole(AriaRole.Checkbox, new() { Name = "Monday" }))
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
    public async Task Checkbox_ShouldOpenTheLinkWithoutTicking_WhenALinkInItsRichLabelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox");
        var agree = page.GetByRole(AriaRole.Checkbox, new() { Name = "I agree to the trail rules" });

        await page.GetByRole(AriaRole.Link, new() { Name = "trail rules" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("#terms$"));
        await Expect(agree).Not.ToBeCheckedAsync();
        await page.GetByText("I agree to the").ClickAsync();

        await Expect(agree).ToBeCheckedAsync();
    }
}
