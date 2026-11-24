using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FormScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SignUp_ShouldListEveryErrorThenSave_WhenSubmittedEmptyAndThenFilledIn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form/complete-form");
        var form = page.Tb().Form("Trip sign-up");
        var signUp = page.Tb().Button("Sign up");
        var status = page.Locator("output");

        // Clicking Sign up waits until the form has handled the submit, so each field shows its message, in the
        // fields' order.
        await signUp.ClickAsync();
        Assert.Equal(["Enter the leader's name.", "Enter how many are going.", "Read the safety notes first."],
            await form.ErrorsAsync());
        await Expect(status).ToHaveTextAsync("3 to fix.");

        // Each field is found by its label and filled in with the helper from its own page.
        await page.Tb().TextField("Leader's name").EnterAsync("Ada");
        await page.Tb().NumberField("Party size").EnterAsync("4");
        await page.Tb().Checkbox("I've read the safety notes").CheckAsync();
        Assert.Empty(await form.ErrorsAsync());
        await Expect(status).ToHaveTextAsync("Ready to sign up.");

        await signUp.ClickAsync();
        await Expect(status).ToHaveTextAsync("Signed up Ada's party of 4.");
    }
}
