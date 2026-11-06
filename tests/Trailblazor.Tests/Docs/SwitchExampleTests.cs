using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class SwitchExampleTests : DocsTest
{
    [Fact]
    public async Task SwitchExample_ShouldShowTheBoundValue_WhenToggled()
    {
        var cut = Render<SwitchExample>();
        Assert.Contains("Auto-save value: True", cut.Markup);

        await cut.Find("input[role=switch]").ChangeAsync(new() { Value = false });

        Assert.Contains("Auto-save value: False", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<SwitchRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Turn on location sharing to join a guided trip.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void StatesExample_ShouldMarkOneSwitchReadOnlySoItPostsAndDisableTheOther_WhenRendered()
    {
        var cut = Render<SwitchStatesExample>();
        var switches = cut.FindAll("input[role=switch]");

        Assert.Equal("true", switches[0].GetAttribute("aria-readonly"));
        Assert.False(string.IsNullOrEmpty(switches[0].GetAttribute("name")));
        Assert.True(switches[1].HasAttribute("disabled"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheSwitch()
    {
        var cut = ComponentPage("switch");

        Assert.Equal(["Space"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
