using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbButtonTests : BunitContext
{
    private static readonly Icon Plus = new("<path d=\"M12 5v14M5 12h14\" />");
    private static readonly Icon Chevron = new("<path d=\"M6 9l6 6 6-6\" />");

    [Fact]
    public void Render_ShouldRenderAFilledPrimaryButton_WhenNoParametersAreSet()
    {
        var cut = Render<TbButton>(parameters => parameters.AddChildContent("Save"));

        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Contains("tb-button--filled", button.ClassList);
        Assert.Contains("tb-button--primary", button.ClassList);
        Assert.Equal("Save", cut.Find("[data-part=label]").TextContent);
    }

    [Fact]
    public void Click_ShouldInvokeOnClick_WhenEnabled()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.OnClick, () => clicks++));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Click_ShouldNotInvokeOnClick_WhenDisabled()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters
            .Add(button => button.Disabled, true)
            .Add(button => button.OnClick, () => clicks++));

        cut.Find("button").Click();

        Assert.Equal(0, clicks);
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Click_ShouldNotInvokeOnClickButStayFocusable_WhenLoading()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters
            .Add(button => button.Loading, true)
            .Add(button => button.OnClick, () => clicks++));

        cut.Find("button").Click();

        var button = cut.Find("button");
        Assert.Equal(0, clicks);
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.NotNull(cut.Find("[data-part=spinner]"));
    }

    [Fact]
    public void Render_ShouldRenderSlotsAndPartClasses_WhenTheyAreSet()
    {
        var cut = Render<TbButton>(parameters => parameters
            .Add(button => button.Start, (RenderFragment)(builder => builder.AddContent(0, "S")))
            .Add(button => button.End, (RenderFragment)(builder => builder.AddContent(0, "E")))
            .Add(button => button.Classes, new ButtonClasses { Label = "my-label", Start = "my-start" })
            .AddChildContent("Label"));

        Assert.Equal("S", cut.Find("[data-part=start]").TextContent);
        Assert.Equal("E", cut.Find("[data-part=end]").TextContent);
        Assert.Contains("my-label", cut.Find("[data-part=label]").ClassList);
        Assert.Contains("my-start", cut.Find("[data-part=start]").ClassList);
    }

    [Fact]
    public void Render_ShouldDrawTheIconsAroundTheLabel_WhenGivenStartAndEndIcons()
    {
        var cut = Render<TbButton>(parameters => parameters
            .Add(button => button.StartIcon, Plus)
            .Add(button => button.EndIcon, Chevron)
            .AddChildContent("Add"));

        Assert.Contains("M12 5v14", cut.Find("[data-part=start] svg").InnerHtml);
        Assert.Contains("M6 9l6 6", cut.Find("[data-part=end] svg").InnerHtml);
        Assert.Equal("Add", cut.Find("[data-part=label]").TextContent);
    }

    [Theory]
    [InlineData(Size.Small, "tb-icon--small")]
    [InlineData(Size.Large, "tb-icon--large")]
    public void Render_ShouldSizeTheIconsWithTheButton_WhenTheButtonHasASize(Size size, string iconClass)
    {
        var cut = Render<TbButton>(parameters => parameters
            .Add(button => button.Size, size)
            .Add(button => button.StartIcon, Plus)
            .Add(button => button.EndIcon, Chevron)
            .AddChildContent("Add"));

        Assert.Contains(iconClass, cut.Find("[data-part=start] svg").ClassList);
        Assert.Contains(iconClass, cut.Find("[data-part=end] svg").ClassList);
    }

    [Fact]
    public void Render_ShouldDrawNoIconParts_WhenNoIconsOrSlotsAreSet()
    {
        var cut = Render<TbButton>(parameters => parameters.AddChildContent("Add"));

        Assert.Empty(cut.FindAll("[data-part=start], [data-part=end]"));
    }

    [Fact]
    public void Render_ShouldSetTheNativeType_WhenTypeIsSubmit()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Type, ButtonType.Submit));

        Assert.Equal("submit", cut.Find("button").GetAttribute("type"));
    }

    [Fact]
    public void Render_ShouldKeepTheConsumersAttributes_WhenTheButtonDoesNotSetThem()
    {
        var cut = Render<TbButton>(parameters => parameters.AddUnmatched("aria-label", "Review changes, 3 unsaved")
            .AddUnmatched("aria-pressed", "mixed"));

        Assert.Equal("Review changes, 3 unsaved", cut.Find("button").GetAttribute("aria-label"));
        Assert.Equal("mixed", cut.Find("button").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_ShouldKeepTheConsumersRoleAndTabIndex_WhenTheLinkIsEnabled()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "guides")
            .AddUnmatched("role", "menuitem").AddUnmatched("tabindex", "-1"));

        Assert.Equal("menuitem", cut.Find("a").GetAttribute("role"));
        Assert.Equal("-1", cut.Find("a").GetAttribute("tabindex"));
    }

    [Fact]
    public void Render_ShouldKeepTheLinkFocusable_WhenDisabledButFocusable()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "guides")
            .Add(button => button.Disabled, true).Add(button => button.FocusableWhenDisabled, true));

        var link = cut.Find("a");
        Assert.Equal("0", link.GetAttribute("tabindex"));
        Assert.Equal("link", link.GetAttribute("role"));
        Assert.False(link.HasAttribute("href"));
    }

    [Fact]
    public void Click_ShouldIgnoreTheClick_WhenALinkIsLoading()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "guides")
            .Add(button => button.Loading, true).Add(button => button.OnClick, () => clicks++));

        cut.Find("a").Click();

        Assert.Equal(0, clicks);
        Assert.Equal("true", cut.Find("a").GetAttribute("aria-disabled"));
        Assert.Equal("guides", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Render_ShouldRenderALink_WhenGivenAnHref()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "guides")
            .AddChildContent("Guides"));

        var link = cut.Find("a.tb-button");
        Assert.Equal("guides", link.GetAttribute("href"));
        Assert.Null(link.GetAttribute("role"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Render_ShouldDropTheAddressButKeepTheLinkRole_WhenALinkIsDisabled()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "guides")
            .Add(button => button.Disabled, true));

        var link = cut.Find("a");
        Assert.False(link.HasAttribute("href"));
        Assert.Equal("link", link.GetAttribute("role"));
        Assert.Equal("true", link.GetAttribute("aria-disabled"));
        Assert.False(link.HasAttribute("tabindex"));
    }

    [Fact]
    public void Click_ShouldIgnoreTheClickButStayFocusable_WhenDisabledButFocusable()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Disabled, true)
            .Add(button => button.FocusableWhenDisabled, true).Add(button => button.OnClick, () => clicks++));

        var button = cut.Find("button");
        button.Click();

        Assert.Equal(0, clicks);
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Click_ShouldTogglePressed_WhenBoundToPressed()
    {
        var pressed = false;
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Pressed, pressed)
            .Add(button => button.PressedChanged, (bool value) => pressed = value));

        Assert.Equal("false", cut.Find("button").GetAttribute("aria-pressed"));
        cut.Find("button").Click();

        Assert.True(pressed);
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_ShouldLeaveOutAriaPressed_WhenNotAToggle()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Pressed, true));

        Assert.False(cut.Find("button").HasAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenALinkIsBoundToPressed()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbButton>(parameters => parameters
            .Add(button => button.Href, "guides").Add(button => button.PressedChanged, (bool _) => { })));
    }

    [Fact]
    public async Task FocusAsync_ShouldFocusTheButton_WhenCalled()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<TbButton>();

        await cut.InvokeAsync(() => cut.Instance.FocusAsync());

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(cut.Find("button"));
    }
}
