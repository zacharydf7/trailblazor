using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class CheckboxExampleTests : DocsTest
{
    [Fact]
    public async Task CheckboxExample_ShouldShowTheBoundValue_WhenToggled()
    {
        var cut = Render<CheckboxExample>();
        Assert.Contains("Updates value: False", cut.Markup);

        await cut.FindAll("input[type=checkbox]")[0].ChangeAsync(new() { Value = true });

        Assert.Contains("Updates value: True", cut.Markup);
    }

    [Fact]
    public void MixedExample_ShouldSayItsValueIsNull_WhenItAppears()
    {
        var cut = Render<CheckboxMixedExample>();

        Assert.Contains("All formats value: null, shown as mixed", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<CheckboxRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Read the safety notes first.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void StatesExample_ShouldNameTheReadOnlyBoxSoItPostsAndDisableTheOther_WhenRendered()
    {
        var cut = Render<CheckboxStatesExample>();
        var boxes = cut.FindAll("input[type=checkbox]");

        Assert.Equal("true", boxes[0].GetAttribute("aria-readonly"));
        Assert.False(string.IsNullOrEmpty(boxes[0].GetAttribute("name")));
        Assert.True(boxes[1].HasAttribute("disabled"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheCheckbox()
    {
        var cut = ComponentPage("checkbox");

        Assert.Contains(cut.FindAll("kbd"), key => key.TextContent == "Space");
        Assert.DoesNotContain("ChildContent", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
