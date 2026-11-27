using System.Globalization;
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
        bool zoomable = true, string? format = null) =>
        Render<TbChart<T>>(parameters => parameters.Add(chart => chart.Items, items).Add(chart => chart.X, x)
            .Add(chart => chart.Title, "Readings")
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
