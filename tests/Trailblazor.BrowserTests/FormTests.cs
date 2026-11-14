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
    public async Task Form_ShouldFocusTheFirstErrorAfterThePageReloads_WhenAStaticPostFails(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form-submit");

        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        var ranger = page.GetByLabel("Ranger");
        await Expect(ranger).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(ranger).ToBeFocusedAsync();
        await Expect(page.GetByTestId("saved")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldFocusTheFirstErrorEachTime_WhenAnEnhancedStaticPostFails(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form-enhanced");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });
        var ranger = page.GetByLabel("Ranger");

        await save.ClickAsync();
        await Expect(ranger).ToBeFocusedAsync();
        await save.ClickAsync();

        await Expect(save).Not.ToBeFocusedAsync();
        await Expect(ranger).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldMarkItsSubmitButtonBusyAndSendOnce_WhenAStaticPostIsUnderWay(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form-submit");
        var posts = 0;
        await page.RouteAsync("**/static-form-submit", async route =>
        {
            // The first post is held long enough for a second press to land while it is on its way.
            if (route.Request.Method == "POST" && ++posts == 1)
            {
                await Task.Delay(1500, TestContext.Current.CancellationToken);
            }

            await route.ContinueAsync();
        });

        // Playwright waits for a navigation to finish before it reads the page, so the page itself records the button
        // at each submit and makes the second press. Importing the script first gets the same module the startup
        // script loads, so its listener is in place by then.
        await page.EvaluateAsync("""
            async () => {
                await import(new URL('_content/Trailblazor/static-form.js', document.baseURI));
                window.addEventListener('submit', event => sessionStorage.setItem('busy', [
                    event.submitter.getAttribute('aria-busy'),
                    !!event.submitter.querySelector('[data-part=spinner]')].join(' ')));
                const form = document.querySelector('form');
                const save = form.querySelector('button[type=submit]');
                form.requestSubmit(save);
                setTimeout(() => form.requestSubmit(save), 500);
            }
            """);

        await Expect(page.GetByLabel("Ranger")).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.Equal("true true", await page.EvaluateAsync<string>("() => sessionStorage.getItem('busy')"));
        Assert.Equal(1, posts);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldLetItsSubmitButtonGoAfterTenSeconds_WhenAStaticPostLeavesThePageInPlace(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form-submit");
        // An answer with no content keeps the page, as a stopped post or a download would. Playwright waits on the
        // navigation that never finishes, so the page is read with script.
        await page.RouteAsync("**/static-form-submit", route => route.Request.Method == "POST"
            ? route.FulfillAsync(new() { Status = 204 }) : route.ContinueAsync());
        const string Busy = "() => [document.querySelector('button[type=submit]').getAttribute('aria-busy'), "
            + "'tbSubmitting' in document.querySelector('form').dataset].join(' ')";
        await page.EvaluateAsync("""
            async () => {
                await import(new URL('_content/Trailblazor/static-form.js', document.baseURI));
                document.querySelector('form').requestSubmit(document.querySelector('button[type=submit]'));
            }
            """);
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve, 50))");
        Assert.Equal("true true", await page.EvaluateAsync<string>(Busy));

        // Waited out for real: after an answer like this, Chromium no longer runs Playwright's fake timers.
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve, 10500))");

        Assert.Equal(" false", await page.EvaluateAsync<string>(Busy));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldLeaveItsSubmitButtonAlone_WhenAnotherListenerCancelsTheStaticPost(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form-submit");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        // A listener after the library's, on the window, cancels the post once.
        await page.EvaluateAsync("""
            async () => {
                await import(new URL('_content/Trailblazor/static-form.js', document.baseURI));
                window.addEventListener('submit', event => event.preventDefault(), { once: true });
                document.querySelector('form').requestSubmit(document.querySelector('button[type=submit]'));
            }
            """);
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve, 50))");
        Assert.Null(await save.GetAttributeAsync("aria-busy"));
        await save.ClickAsync();

        await Expect(page.GetByLabel("Ranger")).ToHaveAttributeAsync("aria-invalid", "true");
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
    public async Task Form_ShouldPutFieldsAndTheirValuesBack_WhenItsResetButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/form-reset");
        var size = page.GetByRole(AriaRole.Combobox, new() { Name = "Size" });
        await Expect(size).ToHaveAttributeAsync("data-ready", "");
        var bound = page.Locator("output");
        var first = await bound.TextContentAsync();
        await Field(page, "Name").FillAsync("");
        await page.GetByLabel("Count").FillAsync("9");
        await Field(page, "Age").FillAsync("40");
        await size.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Large" }).ClickAsync();
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Guide" }).UncheckAsync();
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" });
        await trail.FillAsync("Riv");
        await page.GetByRole(AriaRole.Option, new() { Name = "River" }).ClickAsync();
        await page.GetByLabel("Color").First.FillAsync("#112233");
        await Button(page, "Send").ClickAsync();
        await Expect(Field(page, "Name")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Field(page, "Name")).ToBeFocusedAsync();
        var reset = Button(page, "Start over");
        await reset.FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(bound).ToHaveTextAsync(first!);
        await Expect(reset).ToBeFocusedAsync();
        await Expect(Field(page, "Name")).ToHaveValueAsync("Robin");
        await Expect(Field(page, "Name")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByLabel("Count")).ToHaveValueAsync("3");
        await Expect(size).ToContainTextAsync("Small");
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "Guide" })).ToBeCheckedAsync();
        await Expect(trail).ToHaveValueAsync("Ridge");
        await Expect(page.GetByLabel("Color").First).ToHaveValueAsync("#336699");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldClearUnreadableText_WhenItsResetButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/form-reset");
        var start = page.Locator("[data-part=start-input]");
        var age = Field(page, "Age");
        var firstDate = await start.InputValueAsync();
        await age.FillAsync("old");
        await age.BlurAsync();
        await Expect(age).ToHaveAttributeAsync("aria-invalid", "true");
        await start.FillAsync("not a date");
        await start.BlurAsync();
        await Expect(start).ToHaveAttributeAsync("aria-invalid", "true");

        await Button(page, "Start over").ClickAsync();

        await Expect(age).ToHaveValueAsync("30");
        await Expect(age).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(start).ToHaveValueAsync(firstDate);
        await Expect(start).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldLeaveTheResetToTheBrowser_WhenTheFormIsOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form-reset");
        await Field(page, "Name").FillAsync("Sam");

        await Button(page, "Start over").ClickAsync();

        await Expect(Field(page, "Name")).ToHaveValueAsync("Robin");
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
