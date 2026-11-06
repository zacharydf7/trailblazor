using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class FormTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Field(IPage page, string name) =>
        page.GetByRole(AriaRole.Textbox, new() { Name = name, Exact = true });

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldShowEveryErrorAndFocusTheFirst_WhenASubmitFails(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");

        await Button(page, "Sign up").ClickAsync();

        await Expect(Field(page, "Leader's name")).ToBeFocusedAsync();
        await Expect(Field(page, "Leader's name")).ToHaveAccessibleDescriptionAsync("Enter the leader's name.");
        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" }))
            .ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByText("3 to fix.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldFocusTheGroupsFirstRadio_WhenASubmitFailsOnARadioGroupFirst(string browser)
    {
        var page = await OpenAsync(browser, "/form-group-first");

        await page.GetByRole(AriaRole.Button, new() { Name = "Book" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Meadow" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldSubmit_WhenEnterIsPressedInASingleLineField(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");

        await Field(page, "Meeting point").FillAsync("South trailhead");
        await Field(page, "Meeting point").PressAsync("Enter");

        await Expect(page.GetByText("Saved: South trailhead.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FormExample_ShouldMoveFocusToTheErrorSummary_WhenSubmittedWithErrors(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");

        await Button(page, "Request permit").ClickAsync();

        var summary = page.GetByRole(AriaRole.Group, new() { Name = "There are 2 problems to fix." });
        await Expect(summary).ToBeFocusedAsync();
        await Expect(page).ToHaveTitleAsync("Error: Request a permit");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FormExample_ShouldMoveFocusToTheField_WhenAnErrorInTheSummaryIsFollowed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");
        await Button(page, "Request permit").ClickAsync();

        await Button(page, "Enter how many nights you'll stay.").ClickAsync();

        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Nights" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FormExample_ShouldDropTheErrorFromThePageTitle_WhenEveryErrorIsFixed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");
        await Button(page, "Request permit").ClickAsync();
        await Expect(page).ToHaveTitleAsync("Error: Request a permit");

        await Field(page, "Permit holder").FillAsync("Robin");
        await Field(page, "Permit holder").PressAsync("Tab");
        await page.Tb().NumberField("Nights").EnterAsync("2");

        await Expect(page).ToHaveTitleAsync("Form example");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FormExample_ShouldFocusTheFieldItIsAbout_WhenTheServerAnswersWithAnError(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");
        await Field(page, "Campsite").FillAsync("Ridge 12");

        await Button(page, "Reserve").ClickAsync();

        await Expect(Field(page, "Campsite")).ToBeFocusedAsync();
        await Expect(Field(page, "Campsite"))
            .ToHaveAccessibleDescriptionAsync(new Regex("Ridge 12 is already taken. Choose another campsite."));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldPostNumbersAndDatesIntact_WhenAStaticPageIsInAnotherCulture(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-culture?culture=de-DE");
        await Expect(page.GetByTestId("culture")).ToHaveTextAsync("de-DE");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("1.5|2026-07-02|1.5|1.5");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FormExample_ShouldValidateAndPost_WhenItsStaticExampleIsOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-tbform");
        var send = Button(page, "Send report");

        await send.ClickAsync();
        await Expect(page.GetByText("Enter the trail's name.")).ToBeVisibleAsync();
        await Expect(page.Locator("output")).ToBeEmptyAsync();
        await Field(page, "Trail").FillAsync("Ridge loop");
        await send.ClickAsync();

        await Expect(page.Locator("output")).ToHaveTextAsync("Sent a report on Ridge loop.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldFocusTheFieldWithAnError_WhenAFieldAboveItIsOnlyMarkedInvalid(string browser)
    {
        var page = await OpenAsync(browser, "/form-invalid-first");

        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Field(page, "Name")).ToBeFocusedAsync();
        await Expect(Field(page, "Nickname")).ToHaveAttributeAsync("aria-invalid", "true");
    }
}
