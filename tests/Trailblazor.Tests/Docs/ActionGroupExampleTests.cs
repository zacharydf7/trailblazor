using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The icon button, button group and toggle group pages' examples, as the docs show them.
public class ActionGroupExampleTests : DocsTest
{
    [Fact]
    public void IconButtonLinkExample_ShouldRenderANamedLink_WhenItHasAnHref()
    {
        var cut = Render<IconButtonLinkExample>();

        var link = cut.Find("a");

        Assert.Equal("guides/theming", link.GetAttribute("href"));
        Assert.Equal("Open the theming guide", link.GetAttribute("aria-label"));
    }

    [Fact]
    public void IconButtonLoadingExample_ShouldShowItsSpinnerThenStop_WhenPressed()
    {
        var cut = Render<IconButtonLoadingExample>();

        cut.Find("button").Click();

        Assert.NotNull(cut.Find("[data-part=spinner]"));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-part=spinner]")), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IconButtonDisabledExample_ShouldKeepOnlyTheFocusableOneInTheTabOrder_WhenRendered()
    {
        var cut = Render<IconButtonDisabledExample>();

        var focusable = cut.Find("button[aria-label='Confirm the route']");

        Assert.True(cut.Find("button[aria-label=Confirm]").HasAttribute("disabled"));
        Assert.False(focusable.HasAttribute("disabled"));
        Assert.Equal("true", focusable.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void ButtonGroupSharedExample_ShouldLetAButtonKeepItsOwnTone_WhenTheGroupSetsOne()
    {
        var cut = Render<ButtonGroupSharedExample>();

        Assert.Contains("danger", Button(cut, "Delete").ClassName);
    }

    [Fact]
    public void ButtonGroupSharedExample_ShouldTurnOffEveryButton_WhenTheGroupIsDisabled()
    {
        var cut = Render<ButtonGroupSharedExample>();

        var buttons = cut.FindAll("[aria-label='Archived trip actions'] button");

        Assert.Equal(2, buttons.Count);
        Assert.All(buttons, button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void ToggleGroupClearableExample_ShouldClearTheValue_WhenThePressedItemIsPressedAgain()
    {
        var cut = Render<ToggleGroupClearableExample>();

        Button(cut, "Miles").Click();
        Assert.Contains("Units: Miles", cut.Markup);

        Button(cut, "Miles").Click();

        Assert.Contains("Units: none", cut.Markup);
    }

    [Fact]
    public void ToggleGroupDisabledExample_ShouldTurnOffEveryItem_WhenTheGroupIsDisabled()
    {
        var cut = Render<ToggleGroupDisabledExample>();

        var items = cut.FindAll("[aria-label='Season on the old map'] button");

        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.True(item.HasAttribute("disabled")));
    }

    [Fact]
    public void ToggleGroupStyleExample_ShouldApplyTheSizeToEveryItem_WhenTheGroupSetsOne()
    {
        var cut = Render<ToggleGroupStyleExample>();

        var items = cut.FindAll("button");

        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Contains("tb-button--large", item.ClassName));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeys_WhenShowingTheToggleGroup()
    {
        var cut = ComponentPage("toggle-group");

        Assert.Contains("Home", cut.FindAll("kbd").Select(key => key.TextContent));
    }

    [Theory]
    [InlineData("toggle-group", "Up and Down"), InlineData("toggle-group", "Clearable")]
    [InlineData("button-group", "Disabled"), InlineData("icon-button", "ShowTooltip")]
    public void ComponentPage_ShouldKeepFeaturesOutOfAccessibility_WhenShowingAnActionGroup(string example,
        string feature)
    {
        var cut = ComponentPage(example);

        Assert.DoesNotContain(feature, cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
