using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BreadcrumbsScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trail_ShouldTakeTheUserUpALevel_WhenALinkIsFollowed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/breadcrumbs/basic");
        var trail = page.Tb().Breadcrumbs();
        Assert.Equal(["Home", "Guides", "Right-to-left"], await trail.ItemsAsync());
        Assert.Equal("Right-to-left", await trail.CurrentAsync());

        // FollowAsync clicks an item's link, and the browser goes to its address.
        await trail.FollowAsync("Guides");

        await Expect(page).ToHaveURLAsync(new Regex("/guides/getting-started$"));
    }
}
