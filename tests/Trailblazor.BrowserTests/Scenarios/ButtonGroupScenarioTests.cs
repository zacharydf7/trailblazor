using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ButtonGroupScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ArchivedTripActions_ShouldBeOff_WhenTheGroupIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button-group/shared-settings");

        // A button group has no helper of its own; it is a group named by its aria-label, and its Disabled turns every
        // button in it off.
        var archived = page.GetByRole(AriaRole.Group, new() { Name = "Archived trip actions" });
        await Expect(archived.GetByRole(AriaRole.Button, new() { Name = "Share" })).ToBeDisabledAsync();
        await Expect(archived.GetByRole(AriaRole.Button, new() { Name = "Restore" })).ToBeDisabledAsync();
        var active = page.GetByRole(AriaRole.Group, new() { Name = "Trip actions", Exact = true });
        await Expect(active.GetByRole(AriaRole.Button, new() { Name = "Share" })).ToBeEnabledAsync();
        await Expect(active.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeEnabledAsync();
    }
}
