using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Zooming into part of a chart's continuous scale and moving along it.
public class TbChartZoomTests : BunitContext
{
    // A point at every whole number from 0 to 20.
    private static readonly int[] Numbers = [.. Enumerable.Range(0, 21)];

    public TbChartZoomTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void Buttons_ShouldShowTheWholeScaleAndOnlyZoomingInCanBeDone_WhenNothingIsZoomed()
    {
        var cut = RenderChart();

        Assert.Equal("Showing 0 to 20", Status(cut));
        Assert.Equal([("Move back", true), ("Zoom out", true), ("Zoom in", false), ("Move forward", true)],
            cut.FindAll("[data-part=zoom] [aria-label]").Select(button => (button.GetAttribute("aria-label")!,
                button.GetAttribute("aria-disabled") == "true")));
        // A button that can't act keeps its tab stop.
        Assert.False(Button(cut, "Zoom out").HasAttribute("disabled"));
        Assert.Equal(21, cut.Tb().Chart("Readings").Places.Count);
        Assert.Equal("", cut.Find("[data-part=plot]").GetAttribute("data-zoomable"));
        Assert.False(cut.Find("[data-part=plot]").HasAttribute("data-zoomed"));
    }

    [Fact]
    public void ZoomIn_ShouldShowHalfTheScaleAroundItsMiddleAndOnlyItsItems_WhenClicked()
    {
        ChartView? bound = null;
        var cut = RenderChart(view => bound = view);

        Click(cut, "Zoom in");

        // Half of 0 to 20 around its middle is 5 to 15, and the chart has those items and no others.
        Assert.Equal(new ChartView(5L, 15L), bound);
        Assert.Equal("Showing 5 to 15", Status(cut));
        Assert.Equal([.. Enumerable.Range(5, 11).Select(number => number.ToString(CultureInfo.CurrentCulture))],
            cut.Tb().Chart("Readings").Places);
        Assert.Equal("", cut.Find("[data-part=plot]").GetAttribute("data-zoomed"));
    }

    [Fact]
    public void MoveButtons_ShouldMoveByAQuarterInWholeNumbersAndStopAtTheEnds_WhenClicked()
    {
        ChartView? bound = null;
        var cut = RenderChart(view => bound = view);
        Click(cut, "Zoom in");

        // A quarter of 10 is 2.5, which moves a scale of whole numbers by 3 either way.
        Click(cut, "Move forward");
        Assert.Equal(new ChartView(8L, 18L), bound);
        Click(cut, "Move back");
        Assert.Equal(new ChartView(5L, 15L), bound);
        Click(cut, "Move forward");
        Click(cut, "Move forward");
        Assert.Equal(new ChartView(10L, 20L), bound);
        Assert.Equal("true", Button(cut, "Move forward").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void ZoomOutAndShowAll_ShouldWidenTheViewAndClearIt_WhenClicked()
    {
        ChartView? bound = null;
        var cut = RenderChart(view => bound = view);
        Click(cut, "Zoom in");
        Click(cut, "Zoom in");
        Assert.Equal(new ChartView(8L, 13L), bound);

        Click(cut, "Zoom out");
        Assert.Equal(new ChartView(6L, 16L), bound);

        ShowAll(cut).Click();
        Assert.Null(bound);
        Assert.Equal(21, cut.Tb().Chart("Readings").Places.Count);
    }

    [Fact]
    public void ZoomIn_ShouldStopAtFourWholeNumbers_WhenTheItemsAreWholeNumbersOneApart()
    {
        ChartView? bound = null;
        var cut = RenderChart(view => bound = view);

        for (var time = 0; time < 6; time++)
        {
            Click(cut, "Zoom in");
        }

        // A quarter of a view four wide moves it by one.
        Assert.Equal(new ChartView(9L, 13L), bound);
        Assert.Equal("true", Button(cut, "Zoom in").GetAttribute("aria-disabled"));
        Assert.Equal(5, cut.Tb().Chart("Readings").Places.Count);
    }

    [Fact]
    public void ZoomIn_ShouldStopAtTwiceTheRoomBetweenTheClosestItems_WhenTheItemsAreNotWholeNumbers()
    {
        ChartView? bound = null;
        var cut = Render(Numbers.Select(number => (double)number), number => number, view => bound = view);

        for (var time = 0; time < 6; time++)
        {
            Click(cut, "Zoom in");
        }

        // The items are 1 apart, so a view is at least 2 wide, with three items in it.
        Assert.Equal(new ChartView(9d, 11d), bound);
        Assert.Equal(3, cut.Tb().Chart("Readings").Places.Count);
    }

    [Fact]
    public void Buttons_ShouldStayToShowAllAgain_WhenTheViewFallsBetweenItems()
    {
        var cut = Render([0, 1, 100], number => number);

        Click(cut, "Zoom in");

        Assert.Equal("Showing 25 to 75", Status(cut));
        Assert.Empty(cut.Tb().Chart("Readings").Places);
        ShowAll(cut).Click();
        Assert.Equal(3, cut.Tb().Chart("Readings").Places.Count);
    }

    [Fact]
    public void Status_ShouldBeWrittenWithTheAxisFormat_WhenTheFormatSuitsWholeNumbersOnly()
    {
        var cut = Render(Numbers, number => number, format: "D3");

        Assert.Equal("Showing 000 to 020", Status(cut));
        Click(cut, "Zoom in");
        Click(cut, "Move forward");
        Assert.Equal("Showing 008 to 018", Status(cut));
    }

    [Fact]
    public void View_ShouldBeTheAppsUntilPeopleChangeIt_WhenTheAppSetsIt()
    {
        var raised = 0;
        var given = new ChartView(2, 6);
        var cut = Render<TbChart<int>>(parameters => parameters.Add(chart => chart.Items, Numbers)
            .Add(chart => chart.X, number => number).Add(chart => chart.Title, "Readings")
            .Add(chart => chart.ChildContent, builder => Content<int>(builder, given, _ => raised++, null,
                number => number % 5)));

        Assert.Equal("Showing 2 to 6", Status(cut));
        Assert.Equal(5, cut.Tb().Chart("Readings").Places.Count);
        Assert.Equal(0, raised);

        // What people choose holds while the app gives the same view again, and a new one from the app replaces it.
        Click(cut, "Zoom out");
        Assert.Equal("Showing 0 to 8", Status(cut));
        cut.Render(parameters => parameters.Add(chart => chart.Title, "Readings"));
        Assert.Equal("Showing 0 to 8", Status(cut));
        given = new ChartView(10, 14);
        cut.Render(parameters => parameters.Add(chart => chart.Title, "Readings"));
        Assert.Equal("Showing 10 to 14", Status(cut));
    }

    [Fact]
    public void View_ShouldBeWholeDays_WhenTheScaleIsOfDateOnlyItems()
    {
        ChartView? bound = null;
        var days = Enumerable.Range(0, 9).Select(day => new DateOnly(2026, 6, 1).AddDays(day)).ToArray();
        var cut = Render(days, day => day, view => bound = view, format: "MMM d");

        Click(cut, "Zoom in");
        Assert.Equal(new ChartView(new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 7)), bound);
        Assert.Equal("Showing Jun 3 to Jun 7", Status(cut));
        Assert.Equal("true", Button(cut, "Zoom in").GetAttribute("aria-disabled"));

        // At its narrowest, four days, a view moves a day at a time either way.
        Click(cut, "Move forward");
        Assert.Equal(new ChartView(new DateOnly(2026, 6, 4), new DateOnly(2026, 6, 8)), bound);
        Click(cut, "Move back");
        Assert.Equal(new ChartView(new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 7)), bound);
    }

    [Fact]
    public void Buttons_ShouldBeLeftOutAndTheAppsViewShown_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderChart(view: new ChartView(2, 6));

        Assert.Empty(cut.FindAll("[data-part=zoom]"));
        Assert.False(cut.Find("[data-part=plot]").HasAttribute("data-zoomable"));
        Assert.Equal(5, cut.Tb().Chart("Readings").Places.Count);
    }

    [Theory]
    [InlineData(ChartScale.Even, null, "A Zoomable TbXAxis needs a Continuous Scale.")]
    [InlineData(ChartScale.Continuous, 4, "TbXAxis needs a View whose From is before its To.")]
    public void Axis_ShouldThrow_WhenZoomableOnAnEvenScaleOrGivenABackwardView(ChartScale scale, int? to,
        string message)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart(scale: scale,
            view: to is { } end ? new ChartView(6, end) : null));

        Assert.Equal(message, thrown.Message);
    }

    [Fact]
    public void Chart_ShouldThrow_WhenTheViewIsOfDatesAndTheItemsAreNumbers()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            RenderChart(view: new ChartView(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5))));

        Assert.Equal("TbXAxis needs a View of the kind its items' X values are: numbers for numbers, dates for dates.",
            thrown.Message);
    }

    [Fact]
    public void Buttons_ShouldBeLeftOut_WhenTheChartIsNotZoomable()
    {
        var cut = RenderChart(zoomable: false);

        Assert.Empty(cut.FindAll("[data-part=zoom]"));
        Assert.False(cut.Find("[data-part=plot]").HasAttribute("data-zoomable"));
    }

    [Fact]
    public void Line_ShouldRunOnToThePlotsEdgesAlongItself_WhenTheViewCutsItBetweenItems()
    {
        var cut = RenderEdged<TbLineSeries<double>>(Numbers.Select(number => (double)number), new ChartView(4.25, 14.5));

        // From the plot's start to its end, with the first and last stretches as steep as their neighbors, while only
        // the items in view have places.
        var path = Path(cut, "path.tb-chart__line");
        Assert.Equal(0, path.Start.X);
        Assert.Equal(ChartPath.Grid, path.Steps.Sum(step => step.X));
        Assert.Equal(Slope(path.Steps[1]), Slope(path.Steps[0]), 0.01);
        Assert.Equal(Slope(path.Steps[^2]), Slope(path.Steps[^1]), 0.01);
        Assert.Equal([.. Enumerable.Range(5, 10).Select(number => number.ToString(CultureInfo.CurrentCulture))],
            cut.Tb().Chart("Readings").Places);
        Assert.Equal(10, cut.Find("path.tb-chart__line").GetAttribute("data-points")!.Split(' ').Length);
    }

    [Fact]
    public void SteppedLine_ShouldStartAtTheValueBeforeThePlot_WhenTheViewCutsItBetweenItems()
    {
        var cut = RenderEdged<TbLineSeries<double>>(Numbers.Select(number => (double)number), new ChartView(4.25, 14.5),
            stepped: true);

        // Across at 4, the value before the plot, then up to 5 at the first item: a step as high as the next one. At
        // the end it stays level at 14, the last item's value, to the plot's edge.
        var steps = Regex.Matches(cut.Find("path.tb-chart__line").GetAttribute("d")!, @"v(-?\d+)")
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(steps[1], steps[0]);
        Assert.Equal(0, steps[^1]);
    }

    [Fact]
    public void SmoothLine_ShouldKeepItsSlopeAtTheEdges_WhenTheViewEndsOnItems()
    {
        var cut = RenderEdged<TbLineSeries<double>>(Numbers.Select(number => (double)number), new ChartView(5d, 15d),
            curve: ChartCurve.Smooth);

        // The first item is at the plot's start, and the curve leaves it rising, as it does unzoomed.
        var d = cut.Find("path.tb-chart__line").GetAttribute("d")!;
        var first = Regex.Match(d, @"c(-?\d+) (-?\d+)");
        Assert.StartsWith("M0 ", d);
        Assert.NotEqual(0, Whole(first.Groups[1].Value));
        Assert.True(Whole(first.Groups[2].Value) < 0, d);
    }

    [Fact]
    public void Area_ShouldBeFilledToThePlotsEdges_WhenTheViewCutsItBetweenItems()
    {
        var cut = RenderEdged<TbAreaSeries<double>>(Numbers.Select(number => (double)number), new ChartView(4.5, 14.5));

        var fill = Path(cut, "path.tb-chart__area");
        Assert.Equal(0, fill.Start.X);
        Assert.Equal(ChartPath.Grid, fill.Steps.TakeWhile(step => step.X > 0).Sum(step => step.X));
    }

    [Fact]
    public void Line_ShouldStartAtItsFirstItemInView_WhenTheItemsAreNotInOrder()
    {
        // 2 and 10 swap places, so the line runs from 1 to 10 and back.
        var swapped = Numbers.Select(number => number switch { 2 => 10d, 10 => 2d, _ => number });
        var cut = RenderEdged<TbLineSeries<double>>(swapped, new ChartView(4.5, 14.5));

        Assert.NotEqual(0, Path(cut, "path.tb-chart__line").Start.X);
    }

    [Theory]
    [InlineData(ChartStacking.Stacked, 500, ChartPath.Grid)]
    [InlineData(ChartStacking.Percent, 500, 9500)]
    public void StackedAreas_ShouldRunOnOnlyWhereEveryAreaHasAValue_WhenTheViewCutsThem(ChartStacking stacking,
        int start, int end)
    {
        // The lower area has no value at 4, just before the view, so the stack starts at its first item, 5.
        var cut = Render<TbChart<double>>(parameters => parameters
            .Add(chart => chart.Items, Numbers.Select(number => (double)number))
            .Add(chart => chart.X, number => number).Add(chart => chart.Title, "Readings")
            .Add(chart => chart.Stacking, stacking)
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                builder.AddComponentParameter(2, nameof(TbXAxis.Zoomable), true);
                builder.AddComponentParameter(3, nameof(TbXAxis.View), new ChartView(4.5, 14.5));
                builder.CloseComponent();
                builder.OpenComponent<TbAreaSeries<double>>(4);
                builder.AddComponentParameter(5, nameof(TbAreaSeries<double>.Name), "Low");
                builder.AddComponentParameter(6, nameof(TbAreaSeries<double>.Value),
                    (Func<double, object?>)(number => number == 4 ? null : 2d));
                builder.CloseComponent();
                builder.OpenComponent<TbAreaSeries<double>>(7);
                builder.AddComponentParameter(8, nameof(TbAreaSeries<double>.Name), "High");
                builder.AddComponentParameter(9, nameof(TbAreaSeries<double>.Value), (Func<double, object?>)(n => n));
                builder.CloseComponent();
            }));

        // After the view, both areas have values, so a stack runs on to the end; a stack to percent doesn't.
        foreach (var area in cut.FindAll("path.tb-chart__line"))
        {
            var d = area.GetAttribute("d")!;
            var started = Whole(Regex.Match(d, @"^M(-?\d+)").Groups[1].Value);
            var reached = started + Regex.Matches(d, @"l(-?\d+) ").Sum(step => Whole(step.Groups[1].Value));
            Assert.Equal((start, end), (started, reached));
        }
    }

    [Fact]
    public void StackedAreas_ShouldStartAtTheFirstItemInView_WhenAnAreaChangesSignJustBeforeIt()
    {
        // The lower area goes from below zero at 4 to above it at 5, so the stack has no edge before 5.
        var cut = Render<TbChart<double>>(parameters => parameters
            .Add(chart => chart.Items, Numbers.Select(number => (double)number))
            .Add(chart => chart.X, number => number).Add(chart => chart.Title, "Readings")
            .Add(chart => chart.Stacking, ChartStacking.Stacked)
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                builder.AddComponentParameter(2, nameof(TbXAxis.Zoomable), true);
                builder.AddComponentParameter(3, nameof(TbXAxis.View), new ChartView(4.5, 14.5));
                builder.CloseComponent();
                builder.OpenComponent<TbAreaSeries<double>>(4);
                builder.AddComponentParameter(5, nameof(TbAreaSeries<double>.Name), "Low");
                builder.AddComponentParameter(6, nameof(TbAreaSeries<double>.Value),
                    (Func<double, object?>)(number => number - 4.6));
                builder.CloseComponent();
                builder.OpenComponent<TbAreaSeries<double>>(7);
                builder.AddComponentParameter(8, nameof(TbAreaSeries<double>.Name), "High");
                builder.AddComponentParameter(9, nameof(TbAreaSeries<double>.Value), (Func<double, object?>)(_ => 1d));
                builder.CloseComponent();
            }));

        Assert.All(cut.FindAll("path.tb-chart__line"),
            area => Assert.StartsWith("M500 ", area.GetAttribute("d")!));
    }

    private static double Slope((int X, int Y) step) => (double)step.Y / step.X;

    // A path's start and its straight steps, as whole offsets on the path's grid.
    private static ((int X, int Y) Start, List<(int X, int Y)> Steps) Path<T>(IRenderedComponent<TbChart<T>> cut,
        string selector)
    {
        var d = cut.Find(selector).GetAttribute("d")!;
        var start = Regex.Match(d, @"^M(-?\d+) (-?\d+)");
        return ((Whole(start.Groups[1].Value), Whole(start.Groups[2].Value)), [.. Regex.Matches(d, @"l(-?\d+) (-?\d+)")
            .Select(step => (Whole(step.Groups[1].Value), Whole(step.Groups[2].Value)))]);
    }

    private static int Whole(string text) => int.Parse(text, CultureInfo.InvariantCulture);

    // A chart of the values along the items, with one series of this kind, showing the view the app gives.
    private IRenderedComponent<TbChart<double>> RenderEdged<TSeries>(IEnumerable<double> items, ChartView view,
        bool stepped = false, ChartCurve? curve = null) where TSeries : TbChartSeries<double> =>
        Render<TbChart<double>>(parameters => parameters.Add(chart => chart.Items, items)
            .Add(chart => chart.X, number => number).Add(chart => chart.Title, "Readings")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Scale), ChartScale.Continuous);
                builder.AddComponentParameter(2, nameof(TbXAxis.Zoomable), true);
                builder.AddComponentParameter(3, nameof(TbXAxis.View), view);
                builder.CloseComponent();
                builder.OpenComponent<TSeries>(4);
                builder.AddComponentParameter(5, nameof(TbLineSeries<double>.Name), "Level");
                builder.AddComponentParameter(6, nameof(TbLineSeries<double>.Value), (Func<double, object?>)(n => n));
                if (stepped || curve is not null)
                {
                    builder.AddComponentParameter(7, nameof(TbLineSeries<double>.Curve), curve ?? ChartCurve.Stepped);
                }

                builder.CloseComponent();
            }));

    [Fact]
    public async Task Helpers_ShouldZoomMoveAndShowAll_WhenTheChartIsZoomable()
    {
        ChartView? bound = null;
        // Buttons the app has reworded are still found.
        var chart = Render(Numbers, number => number, view => bound = view,
            labels: new ChartLabels { ZoomIn = "Closer", ZoomOut = "Farther", ShowAll = "Everything" })
            .Tb().Chart("Readings");

        Assert.Equal("Showing 0 to 20", chart.View);
        Assert.False(chart.IsZoomed);
        await chart.ZoomInAsync();
        Assert.Equal(("Showing 5 to 15", true, new ChartView(5L, 15L)), (chart.View, chart.IsZoomed, bound));
        await chart.MoveForwardAsync();
        Assert.Equal(new ChartView(8L, 18L), bound);
        await chart.MoveBackAsync();
        Assert.Equal(new ChartView(5L, 15L), bound);
        await chart.ZoomInAsync();
        await chart.ZoomOutAsync();
        Assert.Equal(new ChartView(6L, 16L), bound);

        // From a view a quarter of the scale wide, only Show all shows all of it.
        await chart.ZoomInAsync();
        await chart.ShowAllAsync();
        Assert.Equal(("Showing 0 to 20", false, (ChartView?)null), (chart.View, chart.IsZoomed, bound));
    }

    [Fact]
    public async Task Helpers_ShouldSayWhatTheChartCannotDo_WhenItsButtonIsOffOrMissing()
    {
        var chart = RenderChart().Tb().Chart("Readings");

        Assert.Equal("The chart can't zoom out any further.",
            (await Assert.ThrowsAsync<InvalidOperationException>(chart.ZoomOutAsync)).Message);
        Assert.Equal("The chart can't show any more of its scale.",
            (await Assert.ThrowsAsync<InvalidOperationException>(chart.ShowAllAsync)).Message);
        Assert.Equal("The chart can't move back any further.",
            (await Assert.ThrowsAsync<InvalidOperationException>(chart.MoveBackAsync)).Message);

        var plain = Render(Numbers, number => number, zoomable: false).Tb().Chart("Readings");
        Assert.Null(plain.View);
        Assert.Equal("The chart has no zoom buttons: its TbXAxis isn't Zoomable, or the page is static.",
            (await Assert.ThrowsAsync<InvalidOperationException>(plain.ZoomInAsync)).Message);
    }

    private static string Status<T>(IRenderedComponent<TbChart<T>> cut) =>
        cut.Find("[data-part=zoom] [role=status]").TextContent;

    private static void Click<T>(IRenderedComponent<TbChart<T>> cut, string name) => Button(cut, name).Click();

    private static AngleSharp.Dom.IElement Button<T>(IRenderedComponent<TbChart<T>> cut, string name) =>
        cut.Find($"[data-part=zoom] [aria-label='{name}']");

    private static AngleSharp.Dom.IElement ShowAll<T>(IRenderedComponent<TbChart<T>> cut) =>
        cut.FindAll("[data-part=zoom] button").Single(button => button.TextContent.Trim() == "Show all");

    private IRenderedComponent<TbChart<int>> RenderChart(Action<ChartView?>? changed = null, ChartView? view = null,
        ChartScale scale = ChartScale.Continuous, bool zoomable = true) =>
        Render(Numbers, number => number, changed, view, scale, zoomable);

    private IRenderedComponent<TbChart<T>> Render<T>(IEnumerable<T> items, Func<T, object?> x,
        Action<ChartView?>? changed = null, ChartView? view = null, ChartScale scale = ChartScale.Continuous,
        bool zoomable = true, string? format = null, ChartLabels? labels = null) =>
        Render<TbChart<T>>(parameters => parameters.Add(chart => chart.Items, items).Add(chart => chart.X, x)
            .Add(chart => chart.Title, "Readings").Add(chart => chart.Labels, labels ?? new())
            .Add(chart => chart.ChildContent, builder => Content<T>(builder, view, changed, format, _ => 1, scale,
                zoomable)));

    private void Content<T>(RenderTreeBuilder builder, ChartView? view, Action<ChartView?>? changed, string? format,
        Func<T, object?> value, ChartScale scale = ChartScale.Continuous, bool zoomable = true)
    {
        builder.OpenComponent<TbXAxis>(0);
        builder.AddComponentParameter(1, nameof(TbXAxis.Scale), scale);
        builder.AddComponentParameter(2, nameof(TbXAxis.Zoomable), zoomable);
        builder.AddComponentParameter(3, nameof(TbXAxis.View), view);
        builder.AddComponentParameter(4, nameof(TbXAxis.Format), format);
        builder.AddComponentParameter(5, nameof(TbXAxis.ViewChanged),
            EventCallback.Factory.Create<ChartView?>(this, changed ?? (_ => { })));
        builder.CloseComponent();
        builder.OpenComponent<TbLineSeries<T>>(10);
        builder.AddComponentParameter(11, nameof(TbLineSeries<T>.Name), "Level");
        builder.AddComponentParameter(12, nameof(TbLineSeries<T>.Value), value);
        builder.CloseComponent();
    }
}
