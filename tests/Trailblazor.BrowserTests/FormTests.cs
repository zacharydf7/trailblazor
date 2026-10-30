namespace Trailblazor.BrowserTests;

public class FormTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Field(IPage page, string name) =>
        page.GetByRole(AriaRole.Textbox, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldShowEveryErrorAndFocusTheFirst_WhenASubmitFails(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");

        await page.GetByRole(AriaRole.Button, new() { Name = "Check the form" }).ClickAsync();

        await Expect(Field(page, "Leader's name")).ToBeFocusedAsync();
        await Expect(Field(page, "Leader's name")).ToHaveAccessibleDescriptionAsync("Enter the leader's name.");
        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" }))
            .ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByText("3 to fix.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldTurnOnTheButtonAndSubmit_WhenEveryFieldPasses(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");
        var signUp = page.GetByRole(AriaRole.Button, new() { Name = "Sign up" });
        await Expect(signUp).ToBeDisabledAsync();
        await Expect(page.Locator("[data-part=error]")).ToHaveCountAsync(0);

        await Field(page, "Leader's name").FillAsync("Robin");
        await Field(page, "Leader's name").PressAsync("Tab");
        await page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" }).FillAsync("4");
        await page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" }).PressAsync("Tab");
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "I've read the safety notes" }).CheckAsync();
        await signUp.ClickAsync();

        await Expect(page.GetByText("Signed up Robin's party of 4.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldValidateAndPost_WhenUsedAsAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-tbform");
        var send = page.GetByRole(AriaRole.Button, new() { Name = "Send" });

        await send.ClickAsync();
        await Expect(page.GetByText("Enter a leader.")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
        await Field(page, "Leader").FillAsync("Robin");
        await send.ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Robin");
    }
}
