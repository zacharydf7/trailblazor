using AngleSharp.Dom;
using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class RadioGroupExampleTests : DocsTest
{
    [Fact]
    public async Task RadioGroupExample_ShouldShowTheChosenValue_WhenAnOptionIsChosen()
    {
        var cut = Render<RadioGroupExample>();
        Assert.Contains("Paper size value: Letter", cut.Markup);

        await Option(cut.Find("fieldset"), "A4").ChangeAsync(new() { Value = true });

        Assert.Contains("Paper size value: A4", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<RadioGroupRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Choose where to meet the group.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public async Task ValidationExample_ShouldShowItsMessageUntilAnotherOptionIsChosen_WhenItAppears()
    {
        var cut = Render<RadioGroupValidationExample>();
        cut.WaitForAssertion(() => Assert.Equal("The lake site is full this week.",
            cut.Find("[data-part=error]").TextContent.Trim()));

        await Option(cut.Find("fieldset"), "Ridge").ChangeAsync(new() { Value = true });

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task ErrorExample_ShouldShowTheErrorUntilTheDayChanges_WhenTheCheckFindsAProblem()
    {
        var cut = Render<RadioGroupErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        await Button(cut, "Check the day").ClickAsync(new());
        Assert.Equal("Saturday is fully booked.", cut.Find("[data-part=error]").TextContent.Trim());

        await Option(cut.Find("fieldset"), "Friday").ChangeAsync(new() { Value = true });

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void StatesExample_ShouldHoldTheChosenOptionAndDisableTheOtherGroup_WhenRendered()
    {
        var cut = Render<RadioGroupStatesExample>();
        var groups = cut.FindAll("fieldset");
        IElement Group(string legend) => groups.Single(group => group.QuerySelector("legend")!.TextContent.Contains(legend));
        var plan = Group("Current plan");

        Assert.Equal("true", plan.GetAttribute("aria-readonly"));
        Assert.True(Option(plan, "Free").HasAttribute("disabled"));
        Assert.False(Option(plan, "Hiker").HasAttribute("disabled"));
        Assert.True(Group("Shipping").HasAttribute("disabled"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheRadioGroup()
    {
        var cut = ComponentPage("radio-group");

        Assert.Equal(["Tab", "Down", "Right", "Up", "Left", "Space"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("FocusAsync", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
