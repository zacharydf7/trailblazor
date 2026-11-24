using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class BadgeScenarioTests : BunitContext
{
    public BadgeScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Inbox_ShouldHideItsCount_WhenEverythingIsMarkedRead()
    {
        var cut = Render<BadgeVisibleExample>();
        // The badge is hidden from screen readers, so the button's AriaLabel says the count.
        Assert.Equal("5", cut.Find("[data-part=badge]").TextContent.Trim());
        Assert.Single(cut.FindAll("button[aria-label='Inbox, 5 unread']"));

        await cut.Tb().Button("Mark all read").ClickAsync();

        Assert.Empty(cut.FindAll("[data-part=badge]"));
        Assert.Single(cut.FindAll("button[aria-label='Inbox']"));
    }
}
