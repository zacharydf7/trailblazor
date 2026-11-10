using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbSliderTests : BunitContext
{
    private int Brightness { get; set; }

    private SliderRange<int> Elevation { get; set; }

    public TbSliderTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldMarkEveryThumbReadOnlyButNotDisabled_WhenReadOnly()
    {
        var slider = Render<TbSlider<int>>(parameters => parameters.Add(field => field.ReadOnly, true));
        var range = Render<TbRangeSlider<int>>(parameters => parameters.Add(field => field.ReadOnly, true));

        var thumbs = slider.FindAll("input").Concat(range.FindAll("input")).ToList();
        Assert.Equal(3, thumbs.Count);
        Assert.All(thumbs, thumb => Assert.Equal("true", thumb.GetAttribute("aria-readonly")));
        Assert.All(thumbs, thumb => Assert.False(thumb.HasAttribute("disabled")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkEveryThumbReadyOrStaticWithItsValue_WhenTheRendererIsOrIsntInteractive(
        bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
        List<AngleSharp.Dom.IElement> thumbs;
        try
        {
            var slider = Render<TbSlider<double>>(parameters => parameters.Add(field => field.Value, 2.5)
                .Add(field => field.Step, 0.5m));
            var range = Render<TbRangeSlider<int>>(parameters => parameters.Add(field => field.Value, new(20, 70)));
            thumbs = [.. slider.FindAll("input").Concat(range.FindAll("input"))];
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }

        Assert.Equal(["2.5", "20", "70"], thumbs.Select(thumb => thumb.GetAttribute("data-value")));
        Assert.All(thumbs, thumb => Assert.Equal([interactive, !interactive],
            [thumb.HasAttribute("data-ready"), thumb.HasAttribute("data-static")]));
    }

    [Fact]
    public void Input_ShouldKeepTheValueAndCommitNothing_WhenReadOnly()
    {
        var committed = new List<int>();
        var single = RenderSlider(40, onCommit: committed.Add);
        single.Render(parameters => parameters.Add(slider => slider.ReadOnly, true));
        var changes = new List<SliderRange<int>>();
        var range = Render<TbRangeSlider<int>>(parameters => parameters.Add(slider => slider.ReadOnly, true)
            .Add(slider => slider.Value, new(20, 60))
            .Add(slider => slider.ValueChanged, (SliderRange<int> next) => changes.Add(next)));

        single.Find("input").Input("75");
        single.Find("input").Change("75");
        range.Find("[data-part=start-thumb]").Input("30");
        range.Find("[data-part=end-thumb]").Input("90");

        Assert.Equal(40, Brightness);
        Assert.Empty(committed);
        Assert.Empty(changes);
    }

    [Fact]
    public void Render_ShouldBeARangeInputNamedByItsLabel_WhenCalled()
    {
        var cut = RenderSlider(40);

        var input = cut.Find("input[type=range]");
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal(["0", "100", "5", "40"],
            new[] { "min", "max", "step", "value" }.Select(name => input.GetAttribute(name)));
        Assert.Equal("Brightness", input.GetAttribute("name"));
    }

    [Fact]
    public void Input_ShouldSetTheValueAsTheThumbMoves_WhenItStaysInRange()
    {
        var cut = RenderSlider(40);

        cut.Find("input").Input("75");

        Assert.Equal(75, Brightness);
        Assert.Contains("--tb-slider-to: 0.75", cut.Find(".tb-slider__control").GetAttribute("style"));
    }

    [Fact]
    public void Change_ShouldRaiseValueCommitted_WhenTheUserLetsGo()
    {
        var committed = new List<int>();
        var cut = RenderSlider(40, onCommit: committed.Add);

        cut.Find("input").Input("60");
        cut.Find("input").Change("60");

        Assert.Equal([60], committed);
    }

    [Fact]
    public void Render_ShouldShowAndReadTheFormattedValue_WhenGivenAFormatter()
    {
        var cut = RenderSlider(40, formatter: value => $"{value}%");

        Assert.Equal("40%", cut.Find("input").GetAttribute("aria-valuetext"));
        Assert.Equal("40%", cut.Find("[data-part=value]").TextContent);
    }

    [Fact]
    public void Render_ShouldPlaceAnEmptyValueAtTheStart_WhenTheValueIsNull()
    {
        var cut = Render<TbSlider<int?>>(parameters => parameters.Add(slider => slider.Min, 10)
            .Add(slider => slider.Value, null));

        Assert.Equal("10", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldPutAMarkUnderTheTrack_WhenGivenMarks()
    {
        var cut = Render<TbSlider<int>>(parameters => parameters.Add(slider => slider.Max, 30)
            .Add(slider => slider.Marks,
                [new SliderMark { Value = 0, Label = "Flat" }, new SliderMark { Value = 30, Label = "Scramble" }]));

        var marks = cut.FindAll("[data-part=mark]");
        Assert.Equal(["Flat", "Scramble"], marks.Select(mark => mark.TextContent));
        Assert.Contains("--tb-slider-at: 1", marks[1].GetAttribute("style"));
    }

    [Theory]
    [InlineData(10, 10, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(0, 10, 0.5)]
    [InlineData(0.5, 10, 1)]
    public void Render_ShouldThrow_WhenTheLimitsOrStepDontFitTheType(decimal min, decimal max, decimal step)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbSlider<int>>(parameters => parameters
            .Add(slider => slider.Min, min).Add(slider => slider.Max, max).Add(slider => slider.Step, step)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheMaxIsPastTheTypesRange()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbSlider<byte>>(parameters => parameters
            .Add(slider => slider.Max, 300)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheValueIsntANumber()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbSlider<string>>());
    }

    [Fact]
    public void Render_ShouldDescribeTheValueTheThumbShows_WhenTheValueIsOutOfRangeOrEmpty()
    {
        var past = RenderSlider(150, formatter: value => $"{value}%");
        var empty = Render<TbSlider<int?>>(parameters => parameters.Add(slider => slider.Min, 10)
            .Add(slider => slider.ValueFormatter, value => $"{value}%"));

        Assert.Equal("100%", past.Find("input").GetAttribute("aria-valuetext"));
        Assert.Equal("10%", empty.Find("input").GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void Render_ShouldDescribeEachThumbWithTheFormatter_WhenARangeSliderHasOne()
    {
        var cut = Render<TbRangeSlider<int>>(parameters => parameters.Add(slider => slider.Max, 4000)
            .Add(slider => slider.Value, new SliderRange<int>(800, 2400))
            .Add(slider => slider.ValueFormatter, value => $"{value} m"));

        Assert.Equal(["800 m", "2400 m"], cut.FindAll("input").Select(input => input.GetAttribute("aria-valuetext")));
    }

    [Fact]
    public void Change_ShouldRaiseValueCommittedWithTheWholeRange_WhenAThumbIsLetGo()
    {
        var committed = new List<SliderRange<int>>();
        var cut = Render<TbRangeSlider<int>>(parameters => parameters.Add(slider => slider.Max, 4000)
            .Add(slider => slider.Value, new SliderRange<int>(800, 2400))
            .Add(slider => slider.ValueCommitted, (SliderRange<int> range) => committed.Add(range)));

        cut.Find("[data-part=end-thumb]").Input("3000");
        cut.Find("[data-part=end-thumb]").Change("3000");

        Assert.Equal([new SliderRange<int>(800, 3000)], committed);
    }

    [Fact]
    public void Render_ShouldNameEachThumbByTheLabelAndItsEnd_WhenARangeSlider()
    {
        var cut = RenderRange(new(800, 2400));

        var start = cut.Find("[data-part=start-thumb]");
        var end = cut.Find("[data-part=end-thumb]");
        Assert.Equal(["800", "2400"], new[] { start, end }.Select(input => input.GetAttribute("value")));
        Assert.Equal(["Elevation", "Minimum"], start.GetAttribute("aria-labelledby")!.Split(' ')
            .Select(id => cut.Find($"#{id}").TextContent.Trim()));
        Assert.Equal(["Elevation.Start", "Elevation.End"],
            new[] { start, end }.Select(input => input.GetAttribute("name")));
    }

    [Fact]
    public void Input_ShouldStopAThumbAtTheOther_WhenItIsDraggedPastIt()
    {
        var cut = RenderRange(new(800, 2400));

        cut.Find("[data-part=start-thumb]").Input("3000");
        Assert.Equal(new SliderRange<int>(2400, 2400), Elevation);
        cut.Find("[data-part=end-thumb]").Input("100");

        Assert.Equal(new SliderRange<int>(2400, 2400), Elevation);
    }

    [Fact]
    public void Render_ShouldLeaveThePointerToTheScript_WhenBothThumbsShareAValue()
    {
        var cut = RenderRange(new(2000, 2000));

        Assert.Contains("tb-slider__input--shared", cut.Find("[data-part=start-thumb]").ClassList);
        Assert.Contains("tb-slider__input--shared", cut.Find("[data-part=end-thumb]").ClassList);
    }

    private IRenderedComponent<TbSlider<int>> RenderSlider(int value, Action<int>? onCommit = null,
        Func<int, string>? formatter = null)
    {
        Brightness = value;
        return Render<TbSlider<int>>(parameters => parameters
            .Add(slider => slider.Label, "Brightness")
            .Add(slider => slider.Step, 5)
            .Add(slider => slider.ShowValue, true)
            .Add(slider => slider.ValueFormatter, formatter)
            .Add(slider => slider.Value, value)
            .Add(slider => slider.ValueChanged, (int next) => Brightness = next)
            .Add(slider => slider.ValueCommitted, (int next) => onCommit?.Invoke(next))
            .Add(slider => slider.ValueExpression, (Expression<Func<int>>)(() => Brightness)));
    }

    private IRenderedComponent<TbRangeSlider<int>> RenderRange(SliderRange<int> value)
    {
        Elevation = value;
        return Render<TbRangeSlider<int>>(parameters => parameters
            .Add(slider => slider.Label, "Elevation")
            .Add(slider => slider.Max, 4000)
            .Add(slider => slider.Step, 100)
            .Add(slider => slider.Value, value)
            .Add(slider => slider.ValueChanged, (SliderRange<int> next) => Elevation = next)
            .Add(slider => slider.ValueExpression, (Expression<Func<SliderRange<int>>>)(() => Elevation)));
    }
}
