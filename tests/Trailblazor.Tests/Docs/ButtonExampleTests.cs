using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class ButtonExampleTests : DocsTest
{
    [Fact]
    public void ButtonExample_ShouldShowEveryVariantAndTone_WhenRendered()
    {
        var cut = Render<ButtonExample>();

        Assert.Equal(Enum.GetValues<Variant>().Length * Enum.GetValues<Tone>().Length, cut.FindAll("button").Count);
    }

    [Fact]
    public void LoadingExample_ShouldShowItsSpinnerThenStop_WhenPressed()
    {
        var cut = Render<ButtonLoadingExample>();

        cut.Find("button").Click();

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-busy"));
        Assert.NotNull(cut.Find("[data-part=spinner]"));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-part=spinner]")), TimeSpan.FromSeconds(5));
        Assert.Equal("Save the trip", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void DisabledExample_ShouldKeepOnlyTheFocusableOneInTheTabOrder_WhenRendered()
    {
        var cut = Render<ButtonDisabledExample>();

        Assert.True(Button(cut, "Disabled").HasAttribute("disabled"));
        Assert.False(Button(cut, "Publish").HasAttribute("disabled"));
        Assert.Equal("true", Button(cut, "Publish").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void LinkExample_ShouldRenderALink_WhenItHasAnHref()
    {
        var cut = Render<ButtonLinkExample>();

        Assert.Equal("guides/theming", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void ToggleExample_ShouldStayPressed_WhenPressed()
    {
        var cut = Render<ButtonToggleExample>();

        cut.Find("button").Click();

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void SubmitExample_ShouldSendOnlyAValidForm_WhenSubmitted()
    {
        var cut = Render<ButtonSubmitExample>();
        Assert.Equal("submit", Button(cut, "Send report").GetAttribute("type"));

        cut.Find("form").Submit();
        Assert.Contains("Reports sent: 0", cut.Find("output").TextContent);

        cut.Find("input").Change("Pine Hollow loop");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Reports sent: 1", cut.Find("output").TextContent));
    }

    [Fact]
    public void FocusExample_ShouldMoveFocusToRetry_WhenItsButtonIsPressed()
    {
        var cut = Render<ButtonFocusExample>();

        Button(cut, "Move focus to Retry").Click();

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(Button(cut, "Retry"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheButton()
    {
        var cut = ComponentPage("button");
        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.Contains("Space", cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("Href", accessibility);
        Assert.DoesNotContain("type=", accessibility);
    }
}
