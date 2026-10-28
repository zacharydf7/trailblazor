using Bunit;

namespace Trailblazor.Tests.Components;

public class TbStackTests : BunitContext
{
    [Fact]
    public void Render_ShouldBeAColumnWithTheDefaultGap_WhenNoParametersAreSet()
    {
        var cut = Render<TbStack>(parameters => parameters.AddChildContent("Item"));

        var stack = cut.Find(".tb-stack");
        Assert.DoesNotContain("tb-stack--row", stack.ClassList);
        Assert.Contains("tb-stack--align-stretch", stack.ClassList);
        Assert.Contains("tb-stack--justify-start", stack.ClassList);
        Assert.Equal("--tb-stack-gap: var(--tb-space-3)", stack.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldAddLayoutClasses_WhenRowAlignJustifyAndWrapAreSet()
    {
        var cut = Render<TbStack>(parameters => parameters
            .Add(stack => stack.Row, true)
            .Add(stack => stack.Align, Align.Center)
            .Add(stack => stack.Justify, Justify.SpaceBetween)
            .Add(stack => stack.Wrap, true));

        var classes = cut.Find(".tb-stack").ClassList;
        Assert.Contains("tb-stack--row", classes);
        Assert.Contains("tb-stack--align-center", classes);
        Assert.Contains("tb-stack--justify-spacebetween", classes);
        Assert.Contains("tb-stack--wrap", classes);
    }

    [Fact]
    public void Render_ShouldPutTheGapBeforeTheConsumerStyle_WhenBothAreSet()
    {
        var cut = Render<TbStack>(parameters => parameters
            .Add(stack => stack.Gap, 0)
            .Add(stack => stack.Style, "padding: 1rem"));

        Assert.Equal("--tb-stack-gap: var(--tb-space-0); padding: 1rem", cut.Find(".tb-stack").GetAttribute("style"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Render_ShouldThrow_WhenGivenAGapOffTheSpacingScale(int gap)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbStack>(parameters => parameters
            .Add(stack => stack.Gap, gap)));

        Assert.Equal(nameof(TbStack.Gap), exception.ParamName);
    }
}
