using System.Diagnostics.Metrics;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// The legend's buttons, which hide and show a series, and a series' own Hidden.
public class TbChartLegendTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10, 2), new("Feb", 20, 4), new("Mar", 40, 6)];

    private readonly List<ChartPoint<Month>> _clicked = [];

    public TbChartLegendTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Month(string Name, int Completed, int Failed);

    [Fact]
    public async Task Legend_ShouldHideItsSeriesAndFitTheAxisToTheRest_WhenItsButtonIsPressed()
    {
        var cut = RenderChart();
        Assert.Equal(40, HighestMark(cut));

        await PressAsync(cut, "Completed");

        Assert.Empty(cut.FindAll("g[data-series='Completed']"));
        Assert.Equal(6, HighestMark(cut));
        // The series left keeps its color, and its bars take the room the hidden ones had.
        var failed = cut.Find("g[data-series='Failed']");
        Assert.Equal(("tb-chart__series--2", "23.333%"), (failed.ClassName, failed.Children[0].GetAttribute("width")));
        Assert.Equal("Bar chart: Failed", cut.Find("svg[role=img]").GetAttribute("aria-label"));
        var button = Button(cut, "Completed");
        Assert.Equal(("false", true),
            (button.GetAttribute("aria-pressed"), button.ParentElement!.HasAttribute("data-hidden")));
        // The data table keeps every series.
        Assert.Equal(["Completed", "Failed"], cut.Tb().Chart("Jobs").Series);
        Assert.Equal("40", cut.Tb().Chart("Jobs").Value("Completed", "Mar"));
    }

    [Fact]
    public async Task Legend_ShouldShowItsSeriesAgain_WhenItsButtonIsPressedTwice()
    {
        var cut = RenderChart();

        await PressAsync(cut, "Completed");
        await PressAsync(cut, "Completed");

        Assert.Single(cut.FindAll("g[data-series='Completed']"));
        var button = Button(cut, "Completed");
        Assert.Equal(("true", false),
            (button.GetAttribute("aria-pressed"), button.ParentElement!.HasAttribute("data-hidden")));
        Assert.Equal(("Bar chart: Completed, Failed", "2"), (cut.Find("svg[role=img]").GetAttribute("aria-label"),
            cut.Find("[data-part=plot]").GetAttribute("data-commits")));
    }

    [Fact]
    public async Task Plot_ShouldSayEverySeriesIsHidden_WhenNoneShows()
    {
        var cut = RenderChart();

        await PressAsync(cut, "Completed");
        await PressAsync(cut, "Failed");

        var plot = cut.Find("[data-part=plot]");
        Assert.Contains("Every series is hidden", plot.TextContent);
        Assert.False(plot.HasAttribute("data-places"));
        Assert.Empty(cut.FindAll("svg[role=img], .tb-chart__y, .tb-chart__x"));
        // The legend is still there to show them again, and the table still holds the values.
        Assert.Equal(2, cut.FindAll("[data-part=legend] button[aria-pressed=false]").Count);
        Assert.Equal("20", cut.Tb().Chart("Jobs").Value("Completed", "Feb"));
    }

    [Fact]
    public async Task Hidden_ShouldHideTheSeriesAndTellTheApp_WhenTheAppOnlyListens()
    {
        List<bool> told = [];
        var cut = RenderChart(hiddenChanged: told.Add);

        await PressAsync(cut, "Completed");

        // The app's own Hidden is still false, and the user's choice holds until the app gives a new value.
        Assert.Equal([true], told);
        Assert.Empty(cut.FindAll("g[data-series='Completed']"));
        Assert.Equal("false", Button(cut, "Completed").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Hidden_ShouldLeaveTheSeriesOutAndNotTellTheApp_WhenTheAppSetsIt()
    {
        List<bool> told = [];
        var cut = RenderChart(hiddenChanged: told.Add);

        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, Series(true, told.Add)));

        Assert.Empty(cut.FindAll("g[data-series='Completed']"));
        Assert.Equal(6, HighestMark(cut));
        Assert.Equal("false", Button(cut, "Completed").GetAttribute("aria-pressed"));
        Assert.Empty(told);
    }

    [Fact]
    public async Task Hidden_ShouldTakeTheAppsValueAgain_WhenTheAppSetsItAfterTheUserChoseAnother()
    {
        var cut = RenderChart();
        await PressAsync(cut, "Completed");

        // The app's Hidden was false all along, so only a new value from the app says anything.
        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, Series(false, null)));
        Assert.Empty(cut.FindAll("g[data-series='Completed']"));
        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, Series(true, null)));
        cut.Render(parameters => parameters.Add(chart => chart.ChildContent, Series(false, null)));

        Assert.Single(cut.FindAll("g[data-series='Completed']"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Legend_ShouldHaveNoButtons_WhenSeriesCannotBeHiddenOrThePageIsStatic(bool interactive, bool canHide)
    {
        if (!interactive)
        {
            SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        }

        var cut = RenderChart(canHide: canHide, hidden: true);

        Assert.Empty(cut.FindAll("[data-part=legend] button"));
        // A series the app hides is still marked in the legend, in words for a screen reader too.
        var entries = cut.FindAll("[data-part=legend] li");
        Assert.Equal([true, false], entries.Select(entry => entry.HasAttribute("data-hidden")));
        Assert.Equal(["Completed (hidden)", "Failed"],
            entries.Select(entry => string.Join(' ', entry.TextContent.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries))));
        Assert.Equal("(hidden)", entries[0].QuerySelector(".tb-visually-hidden")!.TextContent);
    }

    [Fact]
    public async Task Legend_ShouldCountAHideAndAShow_WhenTheChartHasATelemetryName()
    {
        var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderChart();

        await PressAsync(cut, "Failed");
        await PressAsync(cut, "Failed");

        Assert.Equal(["hide", "show"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Activate_ShouldPassOverAHiddenSeries_WhenTheScriptNamesOne()
    {
        var cut = RenderChart();
        var events = ((DotNetObjectReference<ChartEvents>)JSInterop.Invocations["attachChart"].Single().Arguments[1]!)
            .Value;
        await PressAsync(cut, "Completed");

        await cut.InvokeAsync(() => events.OnActivate(0, 1, "pointer"));
        await cut.InvokeAsync(() => events.OnActivate(1, 1, "pointer"));

        Assert.Equal([new ChartPoint<Month>(Months[1], "Failed", 4)], _clicked);
    }

    [Fact]
    public async Task Helper_ShouldHideAndShowASeries_WhenCalled()
    {
        var cut = RenderChart();
        var chart = cut.Tb().Chart("Jobs");

        Assert.False(chart.IsSeriesHidden("Completed"));
        await chart.HideSeriesAsync("Completed");
        await chart.HideSeriesAsync("Completed");

        Assert.Equal((true, false), (chart.IsSeriesHidden("Completed"), chart.IsSeriesHidden("Failed")));
        var hidden = await Assert.ThrowsAsync<InvalidOperationException>(
            () => chart.ClickPointAsync("Completed", "Jan"));
        Assert.Equal("\"Completed\" is hidden.", hidden.Message);
        await chart.ShowSeriesAsync("Completed");
        Assert.False(chart.IsSeriesHidden("Completed"));
        var unknown = Assert.Throws<InvalidOperationException>(() => chart.IsSeriesHidden("Lost"));
        Assert.Equal("The chart has no series named \"Lost\".", unknown.Message);
    }

    [Fact]
    public async Task Helper_ShouldSayTheLegendHasNoButton_WhenSeriesCannotBeHidden()
    {
        var cut = RenderChart(canHide: false);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Chart("Jobs").HideSeriesAsync("Failed"));

        Assert.Equal("The chart's legend has no button for \"Failed\".", thrown.Message);
    }

    private static Task PressAsync(IRenderedComponent<TbChart<Month>> cut, string series) =>
        Button(cut, series).ClickAsync(new MouseEventArgs());

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<TbChart<Month>> cut, string series) =>
        cut.FindAll("[data-part=legend] button").Single(button => button.TextContent.Trim() == series);

    // The value axis's highest mark.
    private static double HighestMark(IRenderedComponent<TbChart<Month>> cut) =>
        cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)")
            .Max(label => double.Parse(label.TextContent, CultureInfo.InvariantCulture));

    private IRenderedComponent<TbChart<Month>> RenderChart(bool canHide = true, bool hidden = false,
        Action<bool>? hiddenChanged = null) =>
        Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs")
            .Add(chart => chart.TelemetryName, "jobs").Add(chart => chart.CanHideSeries, canHide)
            .Add(chart => chart.OnPointClick, _clicked.Add)
            .Add(chart => chart.ChildContent, Series(hidden, hiddenChanged)));

    // Two bar series, the first with the Hidden and the HiddenChanged given.
    private RenderFragment Series(bool hidden, Action<bool>? hiddenChanged) => builder =>
    {
        builder.OpenComponent<TbBarSeries<Month>>(0);
        builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Completed");
        builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
            (Func<Month, object?>)(month => month.Completed));
        builder.AddComponentParameter(3, nameof(TbBarSeries<Month>.Hidden), hidden);
        if (hiddenChanged is not null)
        {
            builder.AddComponentParameter(4, nameof(TbBarSeries<Month>.HiddenChanged),
                EventCallback.Factory.Create(this, hiddenChanged));
        }

        builder.CloseComponent();
        builder.OpenComponent<TbBarSeries<Month>>(5);
        builder.AddComponentParameter(6, nameof(TbBarSeries<Month>.Name), "Failed");
        builder.AddComponentParameter(7, nameof(TbBarSeries<Month>.Value),
            (Func<Month, object?>)(month => month.Failed));
        builder.CloseComponent();
    };
}
