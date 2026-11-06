using AngleSharp.Dom;
using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class CheckboxGroupExampleTests : DocsTest
{
    [Fact]
    public async Task CheckboxGroupExample_ShouldListTheValuesInTheOrderChecked_WhenAnOptionIsChecked()
    {
        var cut = Render<CheckboxGroupExample>();
        Assert.Contains("Trail days value: Saturday", cut.Markup);

        await Option(cut.Find("fieldset"), "Monday").ChangeAsync(new() { Value = true });

        Assert.Contains("Trail days value: Saturday, Monday", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<CheckboxGroupRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Pick at least one way to reach you.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ComparerExample_ShouldCheckTheSavedTrail_WhenItsValueIsAnotherInstance()
    {
        var cut = Render<CheckboxGroupComparerExample>();
        var group = cut.Find("fieldset");

        Assert.True(Option(group, "Lake").HasAttribute("checked"));
        Assert.False(Option(group, "Ridge").HasAttribute("checked"));
        Assert.False(Option(group, "Pass").HasAttribute("checked"));
    }

    [Fact]
    public void StatesExample_ShouldMarkAGroupReadOnlyAndDisableAGroupOrOneOption_WhenRendered()
    {
        var cut = Render<CheckboxGroupStatesExample>();
        var groups = cut.FindAll("fieldset");
        IElement Group(string legend) => groups.Single(group => group.QuerySelector("legend")!.TextContent.Contains(legend));

        Assert.All(Group("Booked days").QuerySelectorAll("input"), box =>
        {
            Assert.Equal("true", box.GetAttribute("aria-readonly"));
            Assert.False(string.IsNullOrEmpty(box.GetAttribute("name")));
            Assert.False(box.HasAttribute("disabled"));
        });
        Assert.True(Group("Closed days").HasAttribute("disabled"));
        Assert.False(Option(Group("Shuttle days"), "Saturday").HasAttribute("disabled"));
        Assert.True(Option(Group("Shuttle days"), "Sunday").HasAttribute("disabled"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheGroup()
    {
        var cut = ComponentPage("checkbox-group");

        Assert.Equal(["Tab", "Space"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("FocusAsync", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }

    private static IElement Option(IElement group, string text) =>
        group.QuerySelectorAll("label").Single(label => label.TextContent.Contains(text)).QuerySelector("input")!;
}
