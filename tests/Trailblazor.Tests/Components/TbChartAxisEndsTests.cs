using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

// The ends and the step an app gives a continuous TbXAxis, and the round ends a scatter's axis takes by itself.
public class TbChartAxisEndsTests : BunitContext
{
    private static readonly Trail[] Trails = [new(2.4, 120), new(7.9, 780), new(14.2, 1180)];

    public TbChartAxisEndsTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Trail(double Km, int Climb);

    [Fact]
    public void Render_ShouldRunFromTheFirstItemToTheLast_WhenALineHasNoEndsGiven()
    {
        var cut = RenderChart([typeof(TbLineSeries<Trail>)]);

        Assert.Equal("0 4661 10000", cut.Find("[data-part=plot]").GetAttribute("data-at"));
    }

    [Fact]
    public void Render_ShouldRunBetweenRoundValuesThatHoldEveryMark_WhenAScatterIsShown()
    {
        var cut = RenderChart([typeof(TbScatterSeries<Trail>)]);

        // The axis's marks are five apart, so it runs from 0 to 15, and no trail is on the plot's edge.
        Assert.Equal("1600 5267 9467", cut.Find("[data-part=plot]").GetAttribute("data-at"));
        Assert.Equal(["0", "5", "10", "15"], Marks(cut));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldLeaveTheEndsAsTheyAre_WhenTheScatterIsBesideBarsOrHidden(bool bars)
    {
        var cut = bars ? RenderChart([typeof(TbBarSeries<Trail>), typeof(TbScatterSeries<Trail>)])
            : RenderChart([typeof(TbLineSeries<Trail>), typeof(TbScatterSeries<Trail>)], hideLast: true);

        // Bars have half the smallest gap between two trails, 2.75 km, as room at each end. A line with the scatter
        // hidden runs from the first trail to the last.
        Assert.Equal(bars ? "1590 4769 8410" : "0 4661 10000",
            cut.Find("[data-part=plot]").GetAttribute("data-at"));
    }

    [Fact]
    public void Render_ShouldRunBetweenTheAppsEndsAndMarkItsStep_WhenTheAxisIsGivenThem()
    {
        var cut = RenderChart([typeof(TbLineSeries<Trail>)], min: 0, max: 16, step: 4);

        Assert.Equal("1500 4938 8875", cut.Find("[data-part=plot]").GetAttribute("data-at"));
        Assert.Equal(["0", "4", "8", "12", "16"], Marks(cut));
    }

    [Fact]
    public void Render_ShouldKeepTheEndTheAppGave_WhenAScattersOtherEndIsRounded()
    {
        var cut = RenderChart([typeof(TbScatterSeries<Trail>)], min: 2);

        // From 2, as given, to 15, the mark that holds the last trail.
        Assert.Equal("308 4538 9385", cut.Find("[data-part=plot]").GetAttribute("data-at"));
        Assert.Equal(["5", "10", "15"], Marks(cut));
    }

    [Theory]
    [InlineData(null, null, 0d, true)]
    [InlineData(5, 5, null, true)]
    [InlineData(9, 4, null, false)]
    public void Render_ShouldSayWhatTheAxisNeeds_WhenItsStepOrItsEndsCannotBeDrawn(int? min, int? max, double? step,
        bool items)
    {
        // The axis says so as it takes its parameters, whether or not the chart has items yet.
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            RenderChart([typeof(TbLineSeries<Trail>)], min, max, step, items: items));

        Assert.Equal("TbXAxis needs a Step above zero and a Min below its Max.", thrown.Message);
    }

    private static string[] Marks(IRenderedComponent<TbChart<Trail>> cut) =>
        [.. cut.FindAll(".tb-chart__x > span").Select(mark => mark.TextContent.Trim())];

    // A chart of these kinds of series over the trails, on a continuous scale with these ends and this step.
    private IRenderedComponent<TbChart<Trail>> RenderChart(Type[] series, object? min = null, object? max = null,
        double? step = null, bool hideLast = false, bool items = true) =>
        Render<TbChart<Trail>>(parameters => parameters.Add(chart => chart.Items, items ? Trails : [])
            .Add(chart => chart.X, trail => trail.Km).Add(chart => chart.Title, "Trail length and climb")
            .Add(chart => chart.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                builder.AddComponentParameter(2, nameof(TbXAxis.Min), min);
                builder.AddComponentParameter(3, nameof(TbXAxis.Max), max);
                builder.AddComponentParameter(4, nameof(TbXAxis.Step), step);
                builder.CloseComponent();
                foreach (var kind in series)
                {
                    builder.OpenComponent(10, kind);
                    builder.SetKey(kind);
                    builder.AddComponentParameter(11, nameof(TbChartSeries<Trail>.Name), kind.Name);
                    builder.AddComponentParameter(12, nameof(TbChartSeries<Trail>.Value),
                        (Func<Trail, object?>)(trail => trail.Climb));
                    builder.AddComponentParameter(13, nameof(TbChartSeries<Trail>.Hidden),
                        hideLast && kind == series[^1]);
                    builder.CloseComponent();
                }
            })));
}
