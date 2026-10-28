using Bunit;

namespace Trailblazor.Tests.Components;

public class TbContainerTests : BunitContext
{
    [Fact]
    public void Render_ShouldUseTheLargeWidth_WhenNoParametersAreSet()
    {
        var cut = Render<TbContainer>(parameters => parameters.AddChildContent("Content"));

        var container = cut.Find(".tb-container");
        Assert.Contains("tb-container--large", container.ClassList);
        Assert.Equal("Content", container.TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldAddTheWidthClass_WhenMaxWidthIsSet()
    {
        var cut = Render<TbContainer>(parameters => parameters.Add(container => container.MaxWidth, ContainerWidth.Full));

        Assert.Contains("tb-container--full", cut.Find(".tb-container").ClassList);
    }
}
