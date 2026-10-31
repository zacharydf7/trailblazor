using Bunit;

namespace Trailblazor.Tests.Components;

public class TbGridTests : BunitContext
{
    private const string Twelve = "0 0 calc(12 * var(--tb-grid-column) + 11 * var(--tb-grid-gap))";

    [Fact]
    public void Render_ShouldSetTheGap_WhenGapIsSet()
    {
        var cut = Render<TbGrid>(parameters => parameters.Add(grid => grid.Gap, 2));

        Assert.Equal("--tb-grid-gap: var(--tb-space-2)", cut.Find(".tb-grid").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldTakeTheWholeRowWithNoOffsetAtEveryBreakpoint_WhenNoSpanIsSet()
    {
        var cut = Render<TbGridItem>();

        var style = Declarations(cut);
        Assert.All(["", "-sm", "-md", "-lg", "-xl"], suffix =>
        {
            Assert.Equal(Twelve, style[$"--tb-grid-flex{suffix}"]);
            Assert.Equal("0", style[$"--tb-grid-offset{suffix}"]);
        });
    }

    [Fact]
    public void Render_ShouldCarryEachSpanAndOffsetUpToLargerBreakpoints_WhenOnlySomeAreSet()
    {
        var cut = Render<TbGridItem>(parameters => parameters.Add(item => item.Span, 6).Add(item => item.SpanMd, 4)
            .Add(item => item.SpanXl, 3).Add(item => item.Offset, 2).Add(item => item.OffsetLg, 8));

        var style = Declarations(cut);
        Assert.Equal(["6", "6", "4", "4", "3"], new[] { "", "-sm", "-md", "-lg", "-xl" }
            .Select(suffix => style[$"--tb-grid-flex{suffix}"].Split('(')[1].Split(' ')[0]));
        Assert.Equal(["2", "2", "2", "8", "8"], new[] { "", "-sm", "-md", "-lg", "-xl" }
            .Select(suffix => style[$"--tb-grid-offset{suffix}"]));
    }

    [Fact]
    public void Render_ShouldGrowIntoTheRowOrSizeToItsContent_WhenTheSpanIsGrowOrAuto()
    {
        var cut = Render<TbGridItem>(parameters => parameters.Add(item => item.Span, GridSpan.Auto)
            .Add(item => item.SpanMd, GridSpan.Grow));

        var style = Declarations(cut);
        Assert.Equal(["0 1 auto", "0 1 auto", "1 1 0%", "1 1 0%", "1 1 0%"],
            new[] { "", "-sm", "-md", "-lg", "-xl" }.Select(suffix => style[$"--tb-grid-flex{suffix}"]));
    }

    [Theory]
    [InlineData(8, 12, null, nameof(TbGridItem.Offset))]
    [InlineData(8, 4, 6, nameof(TbGridItem.OffsetXl))]
    public void Render_ShouldThrow_WhenGivenAnOffsetThatRunsPastTheLastColumn(int offset, int span, int? spanXl,
        string parameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbGridItem>(parameters => parameters
            .Add(item => item.Offset, offset).Add(item => item.Span, span)
            .Add(item => item.SpanXl, spanXl is { } xl ? (GridSpan)xl : null)));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Theory]
    [InlineData(nameof(TbGridItem.Span), 0)]
    [InlineData(nameof(TbGridItem.Span), -1)]
    [InlineData(nameof(TbGridItem.SpanSm), 13)]
    [InlineData(nameof(TbGridItem.SpanMd), 0)]
    [InlineData(nameof(TbGridItem.SpanLg), 13)]
    [InlineData(nameof(TbGridItem.SpanXl), 0)]
    [InlineData(nameof(TbGridItem.Offset), 12)]
    [InlineData(nameof(TbGridItem.OffsetMd), -1)]
    public void Render_ShouldThrow_WhenGivenASpanOrOffsetOffTheTwelveColumns(string parameter, int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render(builder =>
        {
            builder.OpenComponent<TbGridItem>(0);
            builder.AddAttribute(1, parameter, parameter.StartsWith("Span", StringComparison.Ordinal)
                ? (object)(GridSpan)value : value);
            // A whole-row span would also trip on an offset, so the other parameters keep a one-column span.
            if (parameter != nameof(TbGridItem.Span))
            {
                builder.AddAttribute(2, nameof(TbGridItem.Span), (GridSpan)1);
            }

            builder.CloseComponent();
        }));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Render_ShouldRenderAListOfListItems_WhenTheGridAndItsItemsAreRenderedAsThem()
    {
        var cut = Render<TbGrid>(parameters => parameters.Add(grid => grid.As, LayoutElement.Ul)
            .Add(grid => grid.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(builder =>
            {
                builder.OpenComponent<TbGridItem>(0);
                builder.AddComponentParameter(1, nameof(TbGridItem.As), LayoutElement.Li);
                builder.CloseComponent();
            })));

        var list = cut.Find("ul.tb-grid");
        Assert.Equal("list", list.GetAttribute("role"));
        Assert.NotNull(list.QuerySelector(":scope > li.tb-grid-item"));
    }

    private static Dictionary<string, string> Declarations(IRenderedComponent<TbGridItem> cut) =>
        cut.Find(".tb-grid-item").GetAttribute("style")!.Split("; ").Select(declaration => declaration.Split(": ", 2))
            .ToDictionary(pair => pair[0], pair => pair[1]);
}
