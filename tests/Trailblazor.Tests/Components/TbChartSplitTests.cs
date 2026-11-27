using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A series with a SplitBy, which makes a series for each value of a field of the items.
public class TbChartSplitTests : BunitContext
{
    // Hikers a day on two trails, a row for each day and trail; the lake trail has no count on the 2nd, and one row
    // has no trail.
    private static readonly Hike[] Hikes =
    [
        new(1, "Ridge", 12), new(1, "Lake", 30), new(2, "Ridge", 15), new(3, "Lake", 28), new(3, "Ridge", 9),
        new(3, null, 99),
    ];

    public TbChartSplitTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void Split_ShouldMakeASeriesForEachValueInTheOrderTheyAppear_WhenGivenAField()
    {
        var chart = RenderChart().Tb().Chart("Hikers");

        Assert.Equal(["Ridge", "Lake", "Total"], chart.Series);
        Assert.Equal(["1", "2", "3"], chart.Places);
        Assert.Equal([("12", "30"), ("15", ""), ("9", "28")],
            chart.Places.Select(day => (chart.Value("Ridge", day), chart.Value("Lake", day))));
    }

    [Fact]
    public void Series_ShouldShowTheFirstItemAtEachPlace_WhenItHasNoSplitByBesideOneThatHas()
    {
        var chart = RenderChart().Tb().Chart("Hikers");

        Assert.Equal(["12", "15", "28"], chart.Places.Select(day => chart.Value("Total", day)));
    }

    [Fact]
    public async Task Legend_ShouldHideOneValuesSeriesAndLeaveItsHiddenAsItIs_WhenItsButtonIsPressed()
    {
        var raised = new List<bool>();
        var chart = RenderChart(hiddenChanged: raised.Add).Tb().Chart("Hikers");

        await chart.HideSeriesAsync("Lake");

        Assert.True(chart.IsSeriesHidden("Lake"));
        Assert.False(chart.IsSeriesHidden("Ridge"));
        Assert.Empty(raised);
        await chart.ShowSeriesAsync("Lake");
        Assert.False(chart.IsSeriesHidden("Lake"));
    }

    [Fact]
    public void Hidden_ShouldHideEveryValuesSeries_WhenTheSplitSeriesIsHidden()
    {
        var chart = RenderChart(hidden: () => true).Tb().Chart("Hikers");

        Assert.True(chart.IsSeriesHidden("Ridge"));
        Assert.True(chart.IsSeriesHidden("Lake"));
        Assert.False(chart.IsSeriesHidden("Total"));
    }

    [Fact]
    public async Task Point_ShouldBeTheItemWithItsSeriesValue_WhenClicked()
    {
        ChartPoint<Hike>? clicked = null;
        var chart = RenderChart(clicked: point => clicked = point).Tb().Chart("Hikers");

        // Ridge's item on the 3rd isn't the first there.
        await chart.ClickPointAsync("Ridge", "3");

        Assert.Equal((new Hike(3, "Ridge", 9), "Ridge", 9d), (clicked!.Item, clicked.Series, clicked.Value));
    }

    [Fact]
    public void ItemTone_ShouldBeTheToneOfEachSeriesOwnItem_WhenSplit()
    {
        var chart = RenderChart(tone: hike => hike.Count > 20 ? Tone.Danger : null).Tb().Chart("Hikers");

        Assert.Equal((null, Tone.Danger), (chart.Tone("Ridge", "1"), chart.Tone("Lake", "1")));
    }

    [Fact]
    public async Task Legend_ShouldShowTheSplitSeriesWithOnlyThatValue_WhenOneIsShownWhileItIsHidden()
    {
        // The app binds Hidden, so the series hears the user's choice back.
        var hidden = true;
        var cut = RenderChart(hidden: () => hidden, hiddenChanged: value => hidden = value);
        var chart = cut.Tb().Chart("Hikers");

        await chart.ShowSeriesAsync("Lake");
        cut.Render(parameters => parameters.Add(chart => chart.Title, "Hikers"));

        Assert.False(hidden);
        Assert.Equal((false, true), (chart.IsSeriesHidden("Lake"), chart.IsSeriesHidden("Ridge")));

        // A new Hidden from the app replaces what the user chose.
        hidden = true;
        cut.Render(parameters => parameters.Add(chart => chart.Title, "Hikers"));
        hidden = false;
        cut.Render(parameters => parameters.Add(chart => chart.Title, "Hikers"));
        Assert.Equal((false, false), (chart.IsSeriesHidden("Lake"), chart.IsSeriesHidden("Ridge")));
    }

    [Fact]
    public void SplitLine_ShouldRunOnToThePlotsEdgeFromItsOwnItems_WhenZoomed()
    {
        // Ridge stays at 10, and on the 2nd the lake trail's item comes first.
        Hike[] hikes = [new(1, "Lake", 10), new(1, "Ridge", 10), new(2, "Lake", 30), new(2, "Ridge", 10)];
        var cut = Render<TbChart<Hike>>(parameters => parameters.Add(chart => chart.Items, hikes)
            .Add(chart => chart.X, hike => hike.Day).Add(chart => chart.Title, "Hikers")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                builder.AddComponentParameter(2, nameof(TbXAxis.Zoomable), true);
                builder.AddComponentParameter(3, nameof(TbXAxis.View), new ChartView(1.5, 2d));
                builder.CloseComponent();
                builder.OpenComponent<TbLineSeries<Hike>>(4);
                builder.AddComponentParameter(5, nameof(TbLineSeries<Hike>.SplitBy),
                    (Func<Hike, object?>)(hike => hike.Trail));
                builder.AddComponentParameter(6, nameof(TbLineSeries<Hike>.Value),
                    (Func<Hike, object?>)(hike => hike.Count));
                builder.CloseComponent();
            }));

        // Ridge's line runs in level from the plot's start, between its own items on the 1st and the 2nd.
        Assert.Matches(@"^M0 \d+l10000 0$",cut.Find("path[data-series='Ridge']").GetAttribute("d")!);
    }

    [Fact]
    public void Chart_ShouldDrawAgain_WhenTheSplitByChanges()
    {
        Func<Hike, object?> split = hike => hike.Trail;
        var cut = Render<TbChart<Hike>>(parameters => parameters.Add(chart => chart.Items, Hikes)
            .Add(chart => chart.X, hike => hike.Day).Add(chart => chart.Title, "Hikers")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbLineSeries<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbLineSeries<Hike>.SplitBy), split);
                builder.AddComponentParameter(2, nameof(TbLineSeries<Hike>.Value),
                    (Func<Hike, object?>)(hike => hike.Count));
                builder.CloseComponent();
            }));
        Assert.Equal(["Ridge", "Lake"], cut.Tb().Chart("Hikers").Series);

        split = hike => hike.Count > 20 ? "Busy" : "Quiet";
        cut.Render(parameters => parameters.Add(chart => chart.Title, "Hikers"));

        Assert.Equal(["Quiet", "Busy"], cut.Tb().Chart("Hikers").Series);
    }

    [Fact]
    public void Series_ShouldThrow_WhenItHasNeitherANameNorASplitBy()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbChart<Hike>>(parameters => parameters
            .Add(chart => chart.Items, Hikes).Add(chart => chart.X, hike => hike.Day)
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbLineSeries<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbLineSeries<Hike>.Value), (Func<Hike, object?>)(h => 1));
                builder.CloseComponent();
            })));

        Assert.Equal("TbLineSeries needs a Name, or a SplitBy, and a Value.", thrown.Message);
    }

    private IRenderedComponent<TbChart<Hike>> RenderChart(Func<bool>? hidden = null, Action<bool>? hiddenChanged = null,
        Action<ChartPoint<Hike>>? clicked = null, Func<Hike, Tone?>? tone = null) =>
        Render<TbChart<Hike>>(parameters => parameters.Add(chart => chart.Items, Hikes)
            .Add(chart => chart.X, hike => hike.Day).Add(chart => chart.Title, "Hikers")
            .Add(chart => chart.OnPointClick, clicked ?? (_ => { }))
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbBarSeries<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbBarSeries<Hike>.SplitBy),
                    (Func<Hike, object?>)(hike => hike.Trail));
                builder.AddComponentParameter(2, nameof(TbBarSeries<Hike>.Value),
                    (Func<Hike, object?>)(hike => hike.Count));
                builder.AddComponentParameter(3, nameof(TbBarSeries<Hike>.Hidden), hidden?.Invoke() ?? false);
                builder.AddComponentParameter(4, nameof(TbBarSeries<Hike>.HiddenChanged),
                    EventCallback.Factory.Create<bool>(this, hiddenChanged ?? (_ => { })));
                builder.AddComponentParameter(5, nameof(TbBarSeries<Hike>.ItemTone), tone);
                builder.CloseComponent();
                Total(builder);
            }));

    private static void Total(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbLineSeries<Hike>>(10);
        builder.AddComponentParameter(11, nameof(TbLineSeries<Hike>.Name), "Total");
        builder.AddComponentParameter(12, nameof(TbLineSeries<Hike>.Value), (Func<Hike, object?>)(hike => hike.Count));
        builder.CloseComponent();
    }

    public sealed record Hike(int Day, string? Trail, int Count);
}
