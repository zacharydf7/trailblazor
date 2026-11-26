using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// What the chart hands its script, and what it does when the script says a point was activated.
public class TbChartScriptTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10, 2), new("Feb", 20, null), new("Mar", 40, 6)];

    private readonly List<ChartPoint<Month>> _clicked = [];

    public TbChartScriptTests() => this.AddTrailblazor();

    private sealed record Month(string Name, int Completed, int? Failed);

    [Fact]
    public void Render_ShouldWriteEachPlaceValueAndPointForItsScript_WhenDrawn()
    {
        var cut = RenderChart();

        var plot = cut.Find("[data-part=plot]");
        Assert.Equal("Jan\nFeb\nMar", plot.GetAttribute("data-places"));
        var bars = cut.Find("g[data-series='Completed']");
        Assert.Equal(("0", "10\n20\n40", "1667,7500 5000,5000 8333,0"), (bars.GetAttribute("data-number"),
            bars.GetAttribute("data-values"), bars.GetAttribute("data-points")));
        var line = cut.Find("path[data-series='Failed']");
        Assert.Equal(("1", "2\n\n6", "1667,9500 - 8333,8500"), (line.GetAttribute("data-number"),
            line.GetAttribute("data-values"), line.GetAttribute("data-points")));
    }

    [Fact]
    public void Render_ShouldAttachItsScriptWithHowAPointIsNamed_WhenInteractive()
    {
        var cut = RenderChart();

        var call = Assert.Single(JSInterop.Invocations["attachChart"]);
        Assert.Equal(("{0}, {1}: {2}, {3} of {4}", "No value", true),
            ((string)call.Arguments[2]!, (string)call.Arguments[3]!, (bool)call.Arguments[4]!));
        Assert.Equal(("", "0"), (cut.Find("[data-part=plot]").GetAttribute("data-activates"),
            cut.Find("[data-part=plot]").GetAttribute("data-commits")));
    }

    [Fact]
    public void Render_ShouldNotHearPointsActivated_WhenThereIsNoOnPointClick()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs")
            .AddChildContent<TbBarSeries<Month>>(series => series.Add(bar => bar.Name, "Completed")
                .Add(bar => bar.Value, month => month.Completed)));

        Assert.Equal(false, Assert.Single(JSInterop.Invocations["attachChart"]).Arguments[4]);
        Assert.False(cut.Find("[data-part=plot]").HasAttribute("data-activates"));
    }

    [Fact]
    public void Render_ShouldNotSayAPointCanBeClicked_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderChart();

        Assert.False(cut.Find("[data-part=plot]").HasAttribute("data-activates"));
    }

    [Fact]
    public void Render_ShouldTellItsScript_WhenHowAPointIsNamedChanges()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/chart.js").SetupModule("attachChart", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        var cut = RenderChart();

        cut.Render(parameters => parameters.Add(chart => chart.Title, "Jobs by month"));
        Assert.Empty(handle.Invocations["update"]);
        cut.Render(parameters => parameters.Add(chart => chart.Labels, new ChartLabels { Point = "{1} at {0}" }));

        Assert.Equal(["{1} at {0}", "No value", true], Assert.Single(handle.Invocations["update"]).Arguments);
    }

    [Fact]
    public async Task Activate_ShouldHandTheAppThePointAndCountIt_WhenTheScriptSaysSo()
    {
        var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderChart();

        await cut.InvokeAsync(() => EventsOf().OnActivate(1, 2, "keyboard"));

        Assert.Equal([new ChartPoint<Month>(Months[2], "Failed", 6)], _clicked);
        Assert.Equal("1", cut.Find("[data-part=plot]").GetAttribute("data-commits"));
        var measurement = Assert.Single(used.GetMeasurementSnapshot());
        Assert.Equal(("activate", "keyboard"),
            (measurement.Tags["trailblazor.action"], measurement.Tags["trailblazor.input"]));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(0, 3)]
    [InlineData(-1, 0)]
    public async Task Activate_ShouldPassOverAPointThatIsNotThere_WhenTheScriptNamesOne(int series, int place)
    {
        var cut = RenderChart();

        await cut.InvokeAsync(() => EventsOf().OnActivate(series, place, "pointer"));

        Assert.Empty(_clicked);
        Assert.Equal("0", cut.Find("[data-part=plot]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task Helper_ShouldReadAndClickPoints_WhenUsed()
    {
        var cut = RenderChart();
        var chart = cut.Tb().Chart("Jobs");

        Assert.Equal(["Completed", "Failed"], chart.Series);
        Assert.Equal(["Jan", "Feb", "Mar"], chart.Places);
        Assert.Equal(("40", ""), (chart.Value("Completed", "Mar"), chart.Value("Failed", "Feb")));
        await chart.ClickPointAsync("Failed", "Jan");

        Assert.Equal([new ChartPoint<Month>(Months[0], "Failed", 2)], _clicked);
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => chart.ClickPointAsync("Failed", "Feb"));
        Assert.Equal("\"Failed\" has no value at \"Feb\".", missing.Message);
        var unknown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => chart.ClickPointAsync("Lost", "Feb"));
        Assert.Equal("The chart has no series named \"Lost\".", unknown.Message);
    }

    [Fact]
    public async Task Helper_ShouldClickEachChartAgain_WhenTwoChartsHaveRenderedAgain()
    {
        List<string> picked = [];
        var cut = Render(builder =>
        {
            foreach (var title in new[] { "Jobs", "Retries" })
            {
                builder.OpenRegion(title.Length);
                builder.OpenComponent<TbChart<Month>>(0);
                builder.AddComponentParameter(1, nameof(TbChart<Month>.Items), Months);
                builder.AddComponentParameter(2, nameof(TbChart<Month>.X), (Func<Month, object?>)(month => month.Name));
                builder.AddComponentParameter(3, nameof(TbChart<Month>.Title), title);
                builder.AddComponentParameter(4, nameof(TbChart<Month>.OnPointClick),
                    EventCallback.Factory.Create<ChartPoint<Month>>(this,
                        point => picked.Add($"{title} {point.Item.Name}")));
                builder.AddComponentParameter(5, nameof(TbChart<Month>.ChildContent), (RenderFragment)AddBars);
                builder.CloseComponent();
                builder.CloseRegion();
            }
        });

        // Each click draws its chart again, and the next one still finds the script of the chart it names.
        await cut.Tb().Chart("Jobs").ClickPointAsync("Completed", "Jan");
        await cut.Tb().Chart("Retries").ClickPointAsync("Completed", "Feb");
        await cut.Tb().Chart("Jobs").ClickPointAsync("Completed", "Mar");
        await cut.Tb().Chart("Retries").ClickPointAsync("Completed", "Jan");

        Assert.Equal(["Jobs Jan", "Retries Feb", "Jobs Mar", "Retries Jan"], picked);
    }

    private static void AddBars(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbBarSeries<Month>>(0);
        builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Completed");
        builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
            (Func<Month, object?>)(month => month.Completed));
        builder.CloseComponent();
    }

    private ChartEvents EventsOf() => ((DotNetObjectReference<ChartEvents>)
        JSInterop.Invocations["attachChart"].Single().Arguments[1]!).Value;

    private IRenderedComponent<TbChart<Month>> RenderChart() =>
        Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs")
            .Add(chart => chart.TelemetryName, "jobs").Add(chart => chart.OnPointClick, _clicked.Add)
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbBarSeries<Month>>(0);
                builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Completed");
                builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
                    (Func<Month, object?>)(month => month.Completed));
                builder.CloseComponent();
                builder.OpenComponent<TbLineSeries<Month>>(3);
                builder.AddComponentParameter(4, nameof(TbLineSeries<Month>.Name), "Failed");
                builder.AddComponentParameter(5, nameof(TbLineSeries<Month>.Value),
                    (Func<Month, object?>)(month => month.Failed));
                builder.CloseComponent();
            }));
}
