using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class NumberFieldExampleTests : DocsTest
{
    [Fact]
    public async Task NumberFieldExample_ShouldShowTheBoundValue_WhenANumberIsEntered()
    {
        var cut = Render<NumberFieldExample>();

        await cut.Tb().NumberField("Quantity").EnterAsync("7");

        Assert.Contains("Quantity value: 7", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<NumberFieldRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Enter how many are going.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void CheckedExample_ShouldShowItsMessage_WhenAValueSetByCodeIsOutsideTheLimits()
    {
        var cut = Render<NumberFieldCheckedExample>();

        cut.WaitForAssertion(() => Assert.Equal("Enter a group of 1 to 12 hikers.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void EmptyExample_ShouldBindNull_WhenNothingIsEntered()
    {
        var cut = Render<NumberFieldEmptyExample>();

        Assert.Contains("Copies value: null", cut.Markup);
    }

    [Fact]
    public void NoButtonsExample_ShouldShowNoStepButtons_WhenHidden()
    {
        var cut = Render<NumberFieldNoButtonsExample>();

        Assert.Empty(cut.FindAll("[data-part=increment]"));
    }

    [Fact]
    public async Task LiveExample_ShouldFollowTheValue_WhenTyped()
    {
        var cut = Render<NumberFieldLiveExample>();

        await Input(cut.Find(".tb-stack"), "Nights")
            .TriggerEventAsync("ontbnumberinput", new NumberChangeEventArgs { Value = "3" });

        Assert.Contains("Permit fee: 36 dollars", cut.Markup);
    }

    [Fact]
    public void StatesExample_ShouldNameTheInputSoItPosts_WhenReadOnly()
    {
        var cut = Render<NumberFieldStatesExample>();

        Assert.False(string.IsNullOrEmpty(Input(cut.Find(".tb-stack"), "Permit limit").GetAttribute("name")));
    }

    [Fact]
    public void StatesExample_ShouldMarkEachField_WhenRendered()
    {
        var cut = Render<NumberFieldStatesExample>();

        Assert.True(Input(cut.Find(".tb-stack"), "Permit limit").HasAttribute("readonly"));
        Assert.True(Input(cut.Find(".tb-stack"), "Spare tents").HasAttribute("disabled"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheNumberField()
    {
        var cut = ComponentPage("number-field");

        Assert.Contains(cut.FindAll("kbd"), key => key.TextContent == "Up");
        Assert.DoesNotContain("UpdateOnInput", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("ClampToRange", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
