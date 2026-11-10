using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests;

public class FakeAnnouncerTests : BunitContext
{
    public FakeAnnouncerTests() => this.AddTrailblazor();

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    [Fact]
    public void AddTrailblazor_ShouldMakeTheFakeTheAnnouncer_WhenCalled()
    {
        Assert.Same(Announcer, Services.GetRequiredService<IAnnouncer>());
    }

    [Fact]
    public async Task Change_ShouldRecordTheErrorAFieldReadsOut_WhenItsCheckFails()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail name")
            .Add(field => field.Validation,
                (string? name) => name is { Length: < 3 } ? ["Use at least 3 letters."] : []));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Ox" });

        Assert.Equal(["Use at least 3 letters."], Announcer.Polite);
        Assert.Empty(Announcer.Assertive);
    }

    [Fact]
    public void Announce_ShouldRecordEveryMessageInOrderAndStillReachTheLiveRegions_WhenOneIsWithdrawn()
    {
        var root = Render<TbRoot>();
        IAnnouncer announcer = Announcer;

        var saved = announcer.Polite("Saved");
        announcer.Assertive("Connection lost");
        announcer.Polite("3 results");
        announcer.Withdraw(saved);

        Assert.Equal(["Saved", "3 results"], Announcer.Polite);
        Assert.Equal(["Connection lost"], Announcer.Assertive);
        root.WaitForAssertion(() => Assert.Equal(["3 results"],
            root.FindAll("[data-part=polite] p").Select(message => message.TextContent)));
        Assert.Equal("Connection lost", root.Find("[data-part=assertive] p").TextContent);
    }
}
