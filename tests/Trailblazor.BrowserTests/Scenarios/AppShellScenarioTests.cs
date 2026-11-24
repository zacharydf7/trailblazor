using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AppShellScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccountMenu_ShouldOpenTheProfile_WhenItIsFollowedFromSettings(string browser)
    {
        var page = await OpenAsync(browser, "/examples/app-shell/nav-menu");
        var account = page.Tb().NavMenu("Account");
        Assert.False(await account.ExpandedAsync("Settings"));

        // FollowAsync opens each group on the way, then clicks the link, and the browser goes to its address.
        await account.FollowAsync("Settings", "Profile");

        await Expect(page).ToHaveURLAsync(new Regex("/components/text-field$"));
    }
}
