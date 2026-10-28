using Bunit;

namespace Trailblazor.Tests.Components;

public class TbGridTests : BunitContext
{
    [Fact]
    public void Render_ShouldSetTheGap_WhenGapIsSet()
    {
        var cut = Render<TbGrid>(parameters => parameters.Add(grid => grid.Gap, 2));

        Assert.Equal("--tb-grid-gap: var(--tb-space-2)", cut.Find(".tb-grid").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldSpanTheFullWidthAtEveryBreakpoint_WhenNoSpanIsSet()
    {
        var cut = Render<TbGridItem>();

        Assert.Equal("--tb-grid-start: auto; --tb-grid-span: 12; --tb-grid-span-sm: 12; --tb-grid-span-md: 12; "
            + "--tb-grid-span-lg: 12; --tb-grid-span-xl: 12", cut.Find(".tb-grid-item").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldCarryEachSpanUpToLargerBreakpoints_WhenOnlySomeAreSet()
    {
        var cut = Render<TbGridItem>(parameters => parameters
            .Add(item => item.Span, 6)
            .Add(item => item.SpanMd, 4)
            .Add(item => item.SpanXl, 3)
            .Add(item => item.Start, 4));

        Assert.Equal("--tb-grid-start: 4; --tb-grid-span: 6; --tb-grid-span-sm: 6; --tb-grid-span-md: 4; "
            + "--tb-grid-span-lg: 4; --tb-grid-span-xl: 3", cut.Find(".tb-grid-item").GetAttribute("style"));
    }

    [Theory]
    [InlineData(9, 12, null)]
    [InlineData(9, 4, 6)]
    public void Render_ShouldThrow_WhenGivenAStartThatRunsPastTheLastColumn(int start, int span, int? spanXl)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbGridItem>(parameters => parameters
            .Add(item => item.Start, start)
            .Add(item => item.Span, span)
            .Add(item => item.SpanXl, spanXl)));

        Assert.Equal(nameof(TbGridItem.Start), exception.ParamName);
    }

    [Theory]
    [InlineData(nameof(TbGridItem.Span), 0)]
    [InlineData(nameof(TbGridItem.SpanSm), 13)]
    [InlineData(nameof(TbGridItem.SpanMd), 0)]
    [InlineData(nameof(TbGridItem.SpanLg), 13)]
    [InlineData(nameof(TbGridItem.SpanXl), 0)]
    [InlineData(nameof(TbGridItem.Start), 13)]
    public void Render_ShouldThrow_WhenGivenAColumnOutsideOneToTwelve(string parameter, int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render(builder =>
        {
            builder.OpenComponent<TbGridItem>(0);
            builder.AddAttribute(1, parameter, value);
            builder.CloseComponent();
        }));

        Assert.Equal(parameter, exception.ParamName);
    }
}
