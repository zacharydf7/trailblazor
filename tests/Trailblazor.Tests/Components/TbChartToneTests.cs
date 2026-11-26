using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A tone for a whole series, and for single items' marks.
public class TbChartToneTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10, 2), new("Feb", 20, 0), new("Mar", 40, 6)];

    public TbChartToneTests() => this.AddTrailblazor();

    private sealed record Month(string Name, int Completed, int Failed);

    // Warning for a month with a failure, and danger for one with more than four.
    private static Tone? ByFailures(Month month) =>
        month.Failed > 4 ? Tone.Danger : month.Failed > 0 ? Tone.Warning : null;

    [Fact]
    public void Render_ShouldGiveASeriesItsTonesColor_WhenGivenATone()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Completed", month => month.Completed),
            Series<TbLineSeries<Month>>("Failed", month => month.Failed, Tone.Danger));

        // The tone's class follows the class of the series' position, and wins over it in the stylesheet.
        const string toned = "tb-chart__series--2 tb-chart__tone--danger";
        Assert.Equal($"tb-chart__line {toned}", cut.Find("path[data-series='Failed']").ClassName);
        Assert.Equal(toned, cut.Find("svg.tb-chart__marks > g:not([data-series])").ClassName);
        Assert.Equal($"tb-chart__line {toned}", cut.Find("[data-part=legend] li:nth-child(2) line").ClassName);
        Assert.Equal("tb-chart__series--1", cut.Find("g[data-series='Completed']").ClassName);
        // No mark has a tone of its own, so the script is told of none.
        Assert.Empty(cut.FindAll("[data-tones]"));
        var chart = cut.Tb().Chart();
        Assert.Equal((null, Tone.Danger), (chart.Tone("Completed", "Feb"), chart.Tone("Failed", "Feb")));
    }

    [Fact]
    public void Render_ShouldGiveSingleMarksTheirItemsTone_WhenGivenAnItemTone()
    {
        var cut = RenderChart(Series<TbBarSeries<Month>>("Bars", month => month.Completed, itemTone: ByFailures),
            Series<TbScatterSeries<Month>>("Marks", month => month.Completed, itemTone: ByFailures),
            Series<TbLineSeries<Month>>("Line", month => month.Completed, Tone.Info, ByFailures));

        const string bar = "tb-chart__bar tb-chart__bar--top";
        Assert.Equal([$"{bar} tb-chart__tone--warning", bar, $"{bar} tb-chart__tone--danger"],
            cut.FindAll("g[data-series='Bars'] rect").Select(mark => mark.ClassName));
        Assert.Equal(["tb-chart__marker tb-chart__tone--warning", "tb-chart__marker",
            "tb-chart__marker tb-chart__tone--danger"],
            cut.FindAll("g[data-series='Marks'] circle").Select(mark => mark.ClassName));
        Assert.Equal(["tb-chart__marker tb-chart__tone--warning", "tb-chart__marker",
            "tb-chart__marker tb-chart__tone--danger"],
            cut.FindAll("svg.tb-chart__marks > g:not([data-series]) circle").Select(mark => mark.ClassName));
        // The series itself and its legend key keep the series' color, and the script hears each place's tone.
        Assert.Equal(("tb-chart__series--1", "warning - danger"), (cut.Find("g[data-series='Bars']").ClassName,
            cut.Find("g[data-series='Bars']").GetAttribute("data-tones")));
        Assert.Equal("warning - danger", cut.Find("path[data-series='Line']").GetAttribute("data-tones"));
        Assert.Empty(cut.FindAll("[data-part=legend] :is([class*=tone--warning], [class*=tone--danger])"));
        // An item's tone wins over its series', which the marks without one keep.
        var chart = cut.Tb().Chart();
        Assert.Equal([Tone.Warning, null, Tone.Danger], Months.Select(month => chart.Tone("Bars", month.Name)));
        Assert.Equal([Tone.Warning, Tone.Info, Tone.Danger],
            Months.Select(month => chart.Tone("Line", month.Name)));
    }

    [Fact]
    public void Render_ShouldToneAMarkByItsItem_WhenAnEarlierItemHasNoValue()
    {
        var cut = RenderChart(
            Series<TbBarSeries<Month>>("Completed", month => month.Name == "Feb" ? null : month.Completed,
                Tone.Info, month => month.Name == "Jan" ? null : Tone.Success),
            Series<TbBarSeries<Month>>("Failed", month => month.Failed, itemTone: _ => null));

        Assert.Equal(["tb-chart__bar tb-chart__bar--top", "tb-chart__bar tb-chart__bar--top tb-chart__tone--success"],
            cut.FindAll("g[data-series='Completed'] rect").Select(mark => mark.ClassName));
        // February has no value, so it has no mark and no tone: not its item's and not its series'.
        Assert.Equal("- - success", cut.Find("g[data-series='Completed']").GetAttribute("data-tones"));
        var chart = cut.Tb().Chart();
        Assert.Equal([Tone.Info, null, Tone.Success],
            Months.Select(month => chart.Tone("Completed", month.Name)));
        // With no mark given a tone, the script is told of none.
        Assert.False(cut.Find("g[data-series='Failed']").HasAttribute("data-tones"));
    }

    [Fact]
    public void Render_ShouldShowNoItemsTone_WhenALineHasNoMarkers()
    {
        var cut = RenderChart(builder =>
        {
            builder.OpenComponent<TbLineSeries<Month>>(0);
            builder.AddComponentParameter(1, nameof(TbLineSeries<Month>.Name), "Failed");
            builder.AddComponentParameter(2, nameof(TbLineSeries<Month>.Value),
                (Func<Month, object?>)(month => month.Failed));
            builder.AddComponentParameter(3, nameof(TbLineSeries<Month>.ItemTone), (Func<Month, Tone?>)ByFailures);
            builder.AddComponentParameter(4, nameof(TbLineSeries<Month>.Markers), false);
            builder.CloseComponent();
        });

        // The line has no mark to color, so nothing says a tone: not the script's data and not the table.
        Assert.Empty(cut.FindAll("[data-tones], [data-tone], [class*=tb-chart__tone]"));
        Assert.Null(cut.Tb().Chart().Tone("Failed", "Mar"));
    }

    [Fact]
    public async Task Render_ShouldDrawWhatAnItemToneNowReads_WhenAComponentAroundTheSeriesRendersAlone()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .AddChildContent<LimitSeries>());
        Assert.Equal("- - danger", cut.Find("g[data-series='Failed']").GetAttribute("data-tones"));
        var around = cut.FindComponent<LimitSeries>();

        await around.InvokeAsync(() => around.Instance.SetLimit(1));

        Assert.Equal("danger - danger", cut.Find("g[data-series='Failed']").GetAttribute("data-tones"));
    }

    [Fact]
    public async Task Render_ShouldDrawASeriesNewTone_WhenAComponentAroundTheSeriesRendersAlone()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .AddChildContent<WarningSeries>());
        Assert.Equal("tb-chart__series--1", cut.Find("g[data-series='Failed']").ClassName);
        var around = cut.FindComponent<WarningSeries>();

        await around.InvokeAsync(around.Instance.Warn);

        Assert.Equal("tb-chart__series--1 tb-chart__tone--warning", cut.Find("g[data-series='Failed']").ClassName);
    }

    // An app's own component around a series, which gives it a tone without the chart's parent rendering.
    private sealed class WarningSeries : ComponentBase
    {
        // The same function on every render, so only the tone is new.
        private static readonly Func<Month, object?> Failed = month => month.Failed;

        private Tone? _tone;

        public void Warn()
        {
            _tone = Tone.Warning;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbBarSeries<Month>>(0);
            builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Failed");
            builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value), Failed);
            builder.AddComponentParameter(3, nameof(TbBarSeries<Month>.Tone), _tone);
            builder.CloseComponent();
        }
    }

    // An app's own component around a series, which renders again without the chart's parent doing so.
    private sealed class LimitSeries : ComponentBase
    {
        // The same function on every render, so only the tone's function is new.
        private static readonly Func<Month, object?> Failed = month => month.Failed;

        private int _limit = 4;

        public void SetLimit(int limit)
        {
            _limit = limit;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var limit = _limit;
            builder.OpenComponent<TbBarSeries<Month>>(0);
            builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Failed");
            builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value), Failed);
            builder.AddComponentParameter(3, nameof(TbBarSeries<Month>.ItemTone),
                (Func<Month, Tone?>)(month => month.Failed > limit ? Tone.Danger : null));
            builder.CloseComponent();
        }
    }

    private IRenderedComponent<TbChart<Month>> RenderChart(params RenderFragment[] content) =>
        Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                foreach (var one in content)
                {
                    one(builder);
                }
            }));

    private static RenderFragment Series<TSeries>(string name, Func<Month, object?> value, Tone? tone = null,
        Func<Month, Tone?>? itemTone = null) where TSeries : TbChartSeries<Month> => builder =>
    {
        builder.OpenComponent<TSeries>(0);
        builder.SetKey(name);
        builder.AddComponentParameter(1, nameof(TbChartSeries<Month>.Name), name);
        builder.AddComponentParameter(2, nameof(TbChartSeries<Month>.Value), value);
        builder.AddComponentParameter(3, nameof(TbChartSeries<Month>.Tone), tone);
        builder.AddComponentParameter(4, nameof(TbChartSeries<Month>.ItemTone), itemTone);
        builder.CloseComponent();
    };
}
