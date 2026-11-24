using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AvatarScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Avatars_ShouldBeNamedForTheirPeople_WhenShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/avatar/basic");

        // An avatar has no helper of its own; it is an image named for its person.
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Ada Lovelace" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "A hiker who hasn't signed in" })).ToBeVisibleAsync();
        // Without a photo, it shows the person's initials.
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "Grace Hopper" })).ToHaveTextAsync("GH");
        // So it does once a photo fails to load, which drops the photo's part.
        var alan = page.GetByRole(AriaRole.Img, new() { Name = "Alan Turing" });
        await Expect(alan.Locator("[data-part=image]")).ToHaveCountAsync(0);
        await Expect(alan).ToHaveTextAsync("AT");
    }
}
