using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbButtonTests : BunitContext
{
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
    public void Render_ShouldSetTheNativeType_WhenTypeIsSubmit()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Type, ButtonType.Submit));

        Assert.Equal("submit", cut.Find("button").GetAttribute("type"));
    }
}
