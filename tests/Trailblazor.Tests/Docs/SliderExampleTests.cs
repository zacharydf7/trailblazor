using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class SliderExampleTests : DocsTest
{
    [Fact]
    public async Task SliderExample_ShouldFollowTheThumbAndSaveOnlyOnCommit_WhenItMoves()
    {
        var cut = Render<SliderExample>();
        var thumb = cut.Find("input[type=range]");

        await thumb.InputAsync(new() { Value = "75" });

        Assert.Contains("Map brightness value: 75%", cut.Markup);
        Assert.Contains("Saved at 60%", cut.Markup);

        await cut.Find("input[type=range]").ChangeAsync(new() { Value = "75" });

        Assert.Contains("Saved at 75%", cut.Markup);
    }

    [Fact]
    public void ValidationExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<SliderValidationExample>();

        cut.WaitForAssertion(() => Assert.Equal("Groups over 8 need a permit.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void StatesExample_ShouldMarkOneReadOnlySoItPostsAndDisableTheOther_WhenRendered()
    {
        var cut = Render<SliderStatesExample>();
        var difficulty = Input(cut.Find(".tb-stack"), "Trail difficulty");

        Assert.Equal("true", difficulty.GetAttribute("aria-readonly"));
        Assert.False(string.IsNullOrEmpty(difficulty.GetAttribute("name")));
        Assert.True(Input(cut.Find(".tb-stack"), "Campfire level").HasAttribute("disabled"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheSlider()
    {
        var cut = ComponentPage("slider");

        Assert.Equal(["Right", "Up", "Left", "Down", "Page Up", "Page Down", "Home", "End", "Tab"],
            cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("FocusAsync", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
