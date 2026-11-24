using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AppShellScenarioTests : BunitContext
{
    public AppShellScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task AccountMenu_ShouldOpenTheProfile_WhenItIsFollowedFromSettings()
    {
        var cut = Render<AppShellNavMenuExample>();
        var account = cut.Tb().NavMenu("Account");
        Assert.False(account.Expanded("Settings"));

        // FollowAsync opens each group on the way, then clicks the link; bUnit's NavigationManager goes to its address.
        await account.FollowAsync("Settings", "Profile");

        Assert.True(account.Expanded("Settings"));
        Assert.Equal("Profile", account.Current);
        Assert.Equal("http://localhost/components/text-field", Services.GetRequiredService<NavigationManager>().Uri);
    }
}
