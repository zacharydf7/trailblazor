namespace Trailblazor.BrowserTests;

public class TextFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldPostItsValue_WhenSubmittedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByLabel("Name").FillAsync("Ada");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Ada, False, Free");
        await Expect(page.GetByLabel("Name")).ToHaveValueAsync("Ada");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowItsValidationMessage_WhenAStaticFormIsSubmittedEmpty(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByText("Enter a name.")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Name")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldUpdateWhileTyping_WhenUpdateOnInputIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");

        await page.GetByLabel("Search").PressSequentiallyAsync("tea");

        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("tea");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepEveryKeystroke_WhenEachUpdateIsSlowToComeBack(string browser)
    {
        // A lost keystroke depends on timing, so a few page loads make a regression fail reliably.
        const string text = "abcdefghijklmnopqrst";
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var page = await OpenAsync(browser, "/slow-typing");
            var search = page.GetByLabel("Search");

            await search.PressSequentiallyAsync(text);
            await Expect(page.GetByTestId("completed")).ToHaveTextAsync(text.Length.ToString());

            Assert.Equal(text, await search.InputValueAsync());
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepTheTextAndExplain_WhenTheTextDoesNotParse(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await age.PressAsync("Tab");

        await Expect(age).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(age).ToHaveValueAsync("thirty");
        await Expect(age).ToHaveAccessibleDescriptionAsync("Age isn't valid.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheParentsErrorUntilItClears_WhenTheParentSetsOne(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var handle = page.GetByLabel("Handle");

        await handle.PressSequentiallyAsync("robin");
        await page.GetByRole(AriaRole.Button, new() { Name = "Check the handle" }).ClickAsync();
        await Expect(handle).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(handle).ToHaveAccessibleDescriptionAsync(
            "Try robin, who already has an account. That handle is taken.");
        await Expect(page.Locator("[data-part=announcer] [role=status]")).ToContainTextAsync("That handle is taken.");

        await handle.PressSequentiallyAsync("2");

        await Expect(handle).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }
}
