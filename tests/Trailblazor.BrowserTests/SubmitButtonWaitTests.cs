using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// A click on a form's submit button returns once the form has handled the submit. The forms' handlers are slow, and
// each test reads the page once, without retrying, right after the helper returns.
public class SubmitButtonWaitTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldReturnOnceTheFormHasHandledItsSubmit_WhenItIsAFormsSubmitButton(string browser)
    {
        var page = await OpenAsync(browser, "/slow-submit");
        var save = page.Tb().Button("Save the trip");

        // Each read is made once, with no retry: the click has waited for the form's slow handler.
        await save.ClickAsync();
        Assert.Equal("Refused", await page.GetByTestId("trip").TextContentAsync());
        Assert.Equal("Trail is required.", await page.Tb().TextField("Trail").ErrorAsync());

        await page.Tb().TextField("Trail").EnterAsync("Pine Hollow");
        await save.ClickAsync();
        Assert.Equal("Saved Pine Hollow", await page.GetByTestId("trip").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldReturnOnceBothItsOwnHandlerAndTheFormsHaveRun_WhenASubmitButtonHasASlowOnClick(
        string browser)
    {
        var page = await OpenAsync(browser, "/slow-submit");

        // The button's handler takes twice as long as the form's.
        await page.Tb().Button("Save and note").ClickAsync();

        Assert.Equal("Refused", await page.GetByTestId("trip").TextContentAsync());
        Assert.Equal("Noted", await page.GetByTestId("noted").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldWaitForTheFormItSubmits_WhenItsFormAttributeNamesAFormItIsntIn(string browser)
    {
        var page = await OpenAsync(browser, "/slow-submit");

        // The button sits in another form, whose own count never moves.
        await page.Tb().Button("Save the trip from the report").ClickAsync();
        Assert.Equal("Refused", await page.GetByTestId("trip").TextContentAsync());

        // The dialog was open before the click and stays open, and its button is outside the form it submits.
        await page.Tb().Button("Book the hut").ClickAsync();
        await page.Tb().Dialog("Book the hut").WaitForAsync();
        await page.Tb().Button("Book").ClickAsync();
        Assert.Equal("Turned down", await page.GetByTestId("booking").TextContentAsync());
        Assert.True(await page.Tb().Dialog("Book the hut").IsOpenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldReturn_WhenItsFormsSubmitHandlerAwaitsADialogOrTakesTheFormAway(string browser)
    {
        var page = await OpenAsync(browser, "/slow-submit");

        // The handler finishes only once the dialog is answered, so the click returns when the dialog opens.
        await page.Tb().Button("Delete the report").ClickAsync();
        await page.Tb().Dialog("Delete the report?").ChooseAsync("Delete");
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("Deleted");

        // The form, and the count the click waited on, are gone once the handler has run.
        await page.Tb().Button("Send the message").ClickAsync();
        Assert.Equal("Sent", await page.GetByTestId("message").TextContentAsync());
    }
}
