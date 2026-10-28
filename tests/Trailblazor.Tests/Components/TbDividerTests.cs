using Bunit;

namespace Trailblazor.Tests.Components;

public class TbDividerTests : BunitContext
{
    [Fact]
    public void Render_ShouldRenderAHorizontalRule_WhenNotVertical()
    {
        var cut = Render<TbDivider>();

        Assert.Equal("HR", cut.Find(".tb-divider").TagName);
    }

    [Fact]
    public void Render_ShouldRenderAVerticalSeparator_WhenVertical()
    {
        var cut = Render<TbDivider>(parameters => parameters.Add(divider => divider.Vertical, true));

        var separator = cut.Find("[role=separator]");
        Assert.Equal("vertical", separator.GetAttribute("aria-orientation"));
    }

    [Fact]
    public void Render_ShouldPutClassStyleAndAttributesOnTheSeparator_WhenVertical()
    {
        var cut = Render<TbDivider>(parameters => parameters
            .Add(divider => divider.Vertical, true)
            .Add(divider => divider.Class, "custom")
            .Add(divider => divider.Style, "margin: 0")
            .AddUnmatched("data-test", "yes"));

        var separator = cut.Find("[role=separator]");
        Assert.Contains("custom", separator.ClassList);
        Assert.Equal("margin: 0", separator.GetAttribute("style"));
        Assert.Equal("yes", separator.GetAttribute("data-test"));
    }

    [Fact]
    public void Render_ShouldLetTheConsumerOverrideTheRole_WhenVerticalAndDecorative()
    {
        var cut = Render<TbDivider>(parameters => parameters
            .Add(divider => divider.Vertical, true)
            .AddUnmatched("role", "none"));

        var divider = cut.Find(".tb-divider");
        Assert.Equal("none", divider.GetAttribute("role"));
        Assert.False(divider.HasAttribute("aria-orientation"));
    }
}
