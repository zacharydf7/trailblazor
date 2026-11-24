using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AvatarScenarioTests : BunitContext
{
    public AvatarScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Avatars_ShouldBeNamedForTheirPeople_WhenShown()
    {
        var cut = Render<AvatarExample>();

        // An avatar has no helper of its own; it is an image named for its person.
        Assert.Equal(["Ada Lovelace", "Grace Hopper", "Alan Turing", "A hiker who hasn't signed in"],
            cut.FindAll("[data-part=avatar]").Select(avatar => avatar.GetAttribute("aria-label")));
        // Without a photo, it shows the person's initials.
        var grace = cut.Find("[data-part=avatar][aria-label='Grace Hopper']");
        Assert.Equal("GH", grace.QuerySelector("[data-part=initials]")!.TextContent);
    }
}
