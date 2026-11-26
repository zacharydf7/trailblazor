using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbChartTests : BunitContext
{
    private static readonly Month[] Months = [new("Jan", 10, 2, 4.5), new("Feb", 20, 0, 5.25), new("Mar", 40, 6, null)];

    public TbChartTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private sealed record Month(string Name, int Completed, int Failed, double? Hours);

    private enum Stage { NotStarted, InProgress }

    [Fact]
    public void Render_ShouldDrawABarFromZeroForEachItem_WhenGivenABarSeries()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed));

        var bars = cut.FindAll("g[data-series='Completed'] rect");
        Assert.Equal(3, bars.Count);
        Assert.Equal(("5%", "75%", "23.333%", "25%"), (bars[0].GetAttribute("x"), bars[0].GetAttribute("y"),
            bars[0].GetAttribute("width"), bars[0].GetAttribute("height")));
        Assert.Equal(["tb-chart__bar tb-chart__bar--top"], bars.Select(bar => bar.ClassName).Distinct());
    }

    [Fact]
    public void Render_ShouldDrawALineThroughTheValuesWithAMarkerOnEach_WhenGivenALineSeries()
    {
        var cut = RenderChart(Line("Completed", month => month.Completed));

        // Lines alone don't have to reach zero, so the axis runs from 10 to 40.
        Assert.Equal("M1667 10000l3333 -3333l3333 -6667",
            cut.Find("path[data-series='Completed']").GetAttribute("d"));
        Assert.Equal(["16.667% 100%", "50% 66.667%", "83.333% 0%"], cut.FindAll("circle.tb-chart__marker")
            .Select(marker => $"{marker.GetAttribute("cx")} {marker.GetAttribute("cy")}"));
    }

    [Fact]
    public void Render_ShouldLeaveAGapAndNoMarker_WhenAValueIsMissing()
    {
        var cut = RenderChart(Line("Hours", month => month.Hours));

        Assert.Equal(2, cut.FindAll("circle.tb-chart__marker").Count);
        Assert.Equal(["4.5", "5.25", ""], cut.FindAll("[data-part=table] tbody td").Select(cell => cell.TextContent));
    }

    [Theory]
    [InlineData(null, 30, true)]
    [InlineData(null, 31, false)]
    [InlineData(true, 31, true)]
    [InlineData(false, 3, false)]
    public void Render_ShouldDrawMarkersOnlyWhileTheyHaveRoom_WhenALineHasManyPoints(bool? markers, int points,
        bool drawn)
    {
        var cut = Render<TbChart<int>>(parameters => parameters.Add(chart => chart.Items, Enumerable.Range(0, points))
            .Add(chart => chart.X, number => number).Add(chart => chart.AriaLabel, "Numbers")
            .AddChildContent<TbLineSeries<int>>(series => series.Add(line => line.Name, "Value")
                .Add(line => line.Value, number => number).Add(line => line.Markers, markers)));

        Assert.Equal(drawn ? points : 0, cut.FindAll("circle.tb-chart__marker").Count);
    }

    [Fact]
    public void Render_ShouldNameTheFigureByItsTitleAndTheDrawingByItsKindAndSeries_WhenGivenATitle()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed), Line("Failed", month => month.Failed));

        Assert.Equal("Jobs by month", cut.Find("figure > figcaption[data-part=title]").TextContent);
        var drawing = cut.Find("svg.tb-chart__marks");
        Assert.Equal(("img", "Bar and line chart: Completed, Failed"),
            (drawing.GetAttribute("role"), drawing.GetAttribute("aria-label")));
    }

    [Theory]
    [InlineData(true, "Bar chart: Completed")]
    [InlineData(false, "Line chart: Completed")]
    public void Render_ShouldSayTheKindOfChart_WhenEverySeriesIsTheSameKind(bool bars, string summary)
    {
        var cut = RenderChart(bars ? Bars("Completed", month => month.Completed)
            : Line("Completed", month => month.Completed));

        Assert.Equal(summary, cut.Find("svg.tb-chart__marks").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldNameTheFigureForScreenReaders_WhenGivenAnAriaLabelAndNoTitle()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.AriaLabel, "Jobs by month"));

        Assert.Equal("Jobs by month", cut.Find("figure").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("figcaption"));
    }

    [Fact]
    public void Render_ShouldListTheSeriesInALegend_WhenThereAreTwoOrMore()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed), Line("Failed", month => month.Failed));

        var entries = cut.FindAll("ul[data-part=legend] > li");
        Assert.Equal(["Completed", "Failed"], entries.Select(entry => entry.TextContent.Trim()));
        // Each key is its series' own mark: a bar for the first and a stroke for the second.
        Assert.Equal("tb-chart__key tb-chart__series--1", entries[0].QuerySelector("svg")!.ClassName);
        Assert.NotNull(entries[0].QuerySelector("svg > rect.tb-chart__bar"));
        Assert.NotNull(entries[1].QuerySelector("svg.tb-chart__key--line > line.tb-chart__line.tb-chart__series--2"));
    }

    [Fact]
    public void Render_ShouldHaveNoLegend_WhenThereIsOneSeries()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed));

        Assert.Empty(cut.FindAll("[data-part=legend]"));
    }

    [Fact]
    public void Render_ShouldHoldEveryValueInATableUnderTheChart_WhenDrawn()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed), Bars("Failed", month => month.Failed));

        var table = cut.Find("details[data-part=table]");
        Assert.Equal("Data table", table.QuerySelector("summary")!.TextContent);
        Assert.Equal("Jobs by month", table.QuerySelector("table")!.GetAttribute("aria-label"));
        Assert.Equal(["Completed", "Failed"], table.QuerySelectorAll("thead th").Select(cell => cell.TextContent));
        Assert.Equal(["Jan 10 2", "Feb 20 0", "Mar 40 6"], table.QuerySelectorAll("tbody tr").Select(row =>
            string.Join(' ', row.Children.Select(cell => cell.TextContent))));
        Assert.Equal(["row", "row", "row"],
            table.QuerySelectorAll("tbody th").Select(cell => cell.GetAttribute("scope")));
    }

    [Fact]
    public void Render_ShouldLabelEachTickAndPlace_WhenDrawn()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed));

        var ticks = cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)");
        Assert.Equal(["0 100%", "10 75%", "20 50%", "30 25%", "40 0%"],
            ticks.Select(tick => $"{tick.TextContent} {tick.GetAttribute("style")!["top: ".Length..]}"));
        Assert.Equal(5, cut.FindAll("line.tb-chart__grid").Count);
        Assert.Single(cut.FindAll("line.tb-chart__grid--zero"));
        var places = cut.FindAll(".tb-chart__x > span");
        Assert.Equal(["Jan", "Feb", "Mar"], places.Select(place => place.TextContent.Trim()));
        Assert.Equal("inset-inline-start: 50%; --_tb-chart-stride: 1", places[1].GetAttribute("style"));
        Assert.Equal("--_tb-chart-count: 3; --_tb-chart-chars: 3", cut.Find(".tb-chart__x").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldStackBarsAndRoundOnlyTheTop_WhenStacked()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Stacked),
            Bars("Completed", month => month.Completed), Bars("Failed", month => month.Failed));

        // February has no failed jobs, so its completed bar is the top of its stack.
        Assert.Equal(["tb-chart__bar", "tb-chart__bar tb-chart__bar--top", "tb-chart__bar"],
            cut.FindAll("g[data-series='Completed'] rect").Select(bar => bar.ClassName));
        Assert.Equal(["4%", "0%", "12%"],
            cut.FindAll("g[data-series='Failed'] rect").Select(bar => bar.GetAttribute("height")));
        // A line parts each completed bar from the failed one on top of it: at 80% and 20% from the top.
        Assert.Equal(["80%", "20%"], cut.FindAll("line.tb-chart__gap").Select(gap => gap.GetAttribute("y1")));
        Assert.Equal(["0", "10", "20", "30", "40", "50"],
            cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)").Select(tick => tick.TextContent));
    }

    [Fact]
    public void Render_ShouldShowSharesOfEachTotal_WhenStackedAsPercent()
    {
        var cut = RenderChart(parameters => parameters.Add(chart => chart.Stacking, ChartStacking.Percent),
            Bars("Completed", month => month.Completed), Bars("Failed", month => month.Failed));

        Assert.Equal(["0%", "20%", "40%", "60%", "80%", "100%"],
            cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)").Select(tick => tick.TextContent));
        Assert.Equal("100%", cut.Find("g[data-series='Completed'] rect:nth-child(2)").GetAttribute("height"));
    }

    [Fact]
    public void Render_ShouldSayThereIsNoData_WhenThereAreNoItems()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, [])
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, Bars("Completed", month => month.Completed)));

        Assert.Equal("No data", cut.Find("[data-part=plot].tb-chart__plot--empty").TextContent.Trim());
        Assert.Empty(cut.FindAll("svg, [data-part=table]"));
    }

    [Fact]
    public void Render_ShouldSayThereIsNoDataAndShowNoAxisTitles_WhenThereAreNoSeries()
    {
        var cut = RenderChart(builder =>
        {
            builder.OpenComponent<TbYAxis>(0);
            builder.AddComponentParameter(1, nameof(TbYAxis.Title), "Jobs");
            builder.CloseComponent();
            builder.OpenComponent<TbXAxis>(2);
            builder.AddComponentParameter(3, nameof(TbXAxis.Title), "Month");
            builder.CloseComponent();
        });

        Assert.Equal("No data", cut.Find("[data-part=plot]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=y-title], [data-part=x-title]"));
    }

    [Fact]
    public void Render_ShouldDrawWithoutMarksOnTheAxis_WhenNoMultipleOfItsStepIsBetweenItsEnds()
    {
        var cut = RenderChart(Line("Hours", month => month.Hours), builder =>
        {
            builder.OpenComponent<TbYAxis>(0);
            builder.AddComponentParameter(1, nameof(TbYAxis.Min), 4.1);
            builder.AddComponentParameter(2, nameof(TbYAxis.Max), 5.9);
            builder.AddComponentParameter(3, nameof(TbYAxis.Step), 10d);
            builder.CloseComponent();
        });

        Assert.Empty(cut.FindAll("line.tb-chart__grid"));
        Assert.NotNull(cut.Find("path[data-series='Hours']"));
    }

    [Fact]
    public void Render_ShouldUseTheWordsGiven_WhenLabelsAreSet()
    {
        var labels = new ChartLabels { Table = "Zahlen", Summary = "{1} ({0})", Bars = "Balken", Empty = "Leer" };

        var cut = RenderChart(parameters => parameters.Add(chart => chart.Labels, labels),
            Bars("Completed", month => month.Completed));

        Assert.Equal("Zahlen", cut.Find("summary").TextContent);
        Assert.Equal("Completed (Balken)", cut.Find("svg.tb-chart__marks").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldDrawWhatASeriesNowSays_WhenItsParametersChange()
    {
        var name = "Completed";
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbBarSeries<Month>>(0);
                builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), name);
                builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
                    (Func<Month, object?>)(month => month.Completed));
                builder.CloseComponent();
            }));

        name = "Done";
        cut.Render();

        Assert.Equal("Bar chart: Done", cut.Find("svg.tb-chart__marks").GetAttribute("aria-label"));
        Assert.Equal(["Done"], cut.FindAll("thead th").Select(cell => cell.TextContent));
    }

    // The function is a new one on each render, with the same code and name, so only what it reads tells them apart.
    [Fact]
    public void Render_ShouldDrawWhatASeriesFunctionNowReads_WhenALocalItReadsChanges()
    {
        var showFailed = false;
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                var failed = showFailed;
                builder.OpenComponent<TbBarSeries<Month>>(0);
                builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Jobs");
                builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
                    (Func<Month, object?>)(month => failed ? month.Failed : month.Completed));
                builder.CloseComponent();
            }));
        Assert.Equal(["10", "20", "40"], cut.FindAll("tbody td").Select(cell => cell.TextContent));

        showFailed = true;
        cut.Render();

        Assert.Equal(["2", "0", "6"], cut.FindAll("tbody td").Select(cell => cell.TextContent));
    }

    [Fact]
    public async Task Render_ShouldDrawWhatASeriesFunctionNowReads_WhenAComponentAroundTheSeriesRendersAlone()
    {
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .AddChildContent<SwitchingSeries>());
        Assert.Equal(["10", "20", "40"], cut.FindAll("tbody td").Select(cell => cell.TextContent));
        var around = cut.FindComponent<SwitchingSeries>();

        await around.InvokeAsync(around.Instance.Switch);

        Assert.Equal(["2", "0", "6"], cut.FindAll("tbody td").Select(cell => cell.TextContent));
    }

    [Fact]
    public void Render_ShouldDropASeries_WhenItIsTakenOut()
    {
        var failed = true;
        var cut = Render<TbChart<Month>>(parameters => parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                Bars("Completed", month => month.Completed)(builder);
                if (failed)
                {
                    Bars("Failed", month => month.Failed)(builder);
                }
            }));
        Assert.Equal(2, cut.FindAll("g[data-series]").Count);

        failed = false;
        cut.Render();

        Assert.Equal(["Completed"], cut.FindAll("g[data-series]").Select(series => series.GetAttribute("data-series")));
        Assert.Empty(cut.FindAll("[data-part=legend]"));
    }

    [Fact]
    public void Render_ShouldFollowTheAxesSettings_WhenTheyAreWrittenInside()
    {
        var cut = RenderChart(Bars("Completed", month => month.Completed), builder =>
        {
            builder.OpenComponent<TbYAxis>(0);
            builder.AddComponentParameter(1, nameof(TbYAxis.Title), "Jobs");
            builder.AddComponentParameter(2, nameof(TbYAxis.Format), "0 jobs");
            builder.AddComponentParameter(3, nameof(TbYAxis.Max), 80d);
            builder.AddComponentParameter(4, nameof(TbYAxis.Step), 40d);
            builder.CloseComponent();
            builder.OpenComponent<TbXAxis>(5);
            builder.AddComponentParameter(6, nameof(TbXAxis.Title), "Month");
            builder.CloseComponent();
        });

        Assert.Equal(["0 jobs", "40 jobs", "80 jobs"],
            cut.FindAll(".tb-chart__y > span:not(.tb-chart__y-sizer)").Select(tick => tick.TextContent));
        Assert.Equal(("Jobs", "Month"),
            (cut.Find("[data-part=y-title]").TextContent, cut.Find("[data-part=x-title]").TextContent));
        Assert.Equal(["Month", "Completed"], cut.FindAll("thead th").Select(cell => cell.TextContent));
        Assert.Equal("10 jobs", cut.Find("tbody td").TextContent);
    }

    [Fact]
    public void Render_ShouldWriteDatesAndEnumsAsPeopleReadThem_WhenXIsNotText()
    {
        var days = new[] { new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2) };
        var byDay = Render<TbChart<DateOnly>>(parameters => parameters.Add(chart => chart.Items, days)
            .Add(chart => chart.X, day => day).Add(chart => chart.AriaLabel, "Days")
            .Add(chart => chart.ChildContent, builder =>
            {
                builder.OpenComponent<TbXAxis>(0);
                builder.AddComponentParameter(1, nameof(TbXAxis.Format), "MMM d");
                builder.CloseComponent();
                builder.OpenComponent<TbBarSeries<DateOnly>>(2);
                builder.AddComponentParameter(3, nameof(TbBarSeries<DateOnly>.Name), "Day");
                builder.AddComponentParameter(4, nameof(TbBarSeries<DateOnly>.Value),
                    (Func<DateOnly, object?>)(day => day.Day));
                builder.CloseComponent();
            }));
        var byStage = Render<TbChart<Stage>>(parameters => parameters.Add(chart => chart.Items, Enum.GetValues<Stage>())
            .Add(chart => chart.X, stage => stage).Add(chart => chart.AriaLabel, "Stages")
            .AddChildContent<TbBarSeries<Stage>>(series => series.Add(bar => bar.Name, "Order")
                .Add(bar => bar.Value, stage => (int)stage)));

        Assert.Equal(["Mar 1", "Mar 2"],
            byDay.FindAll(".tb-chart__x > span").Select(place => place.TextContent.Trim()));
        Assert.Equal(["Not started", "In progress"],
            byStage.FindAll(".tb-chart__x > span").Select(place => place.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldWriteNumbersInTheCurrentCulture_WhenDrawn()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        var cut = RenderChart(Line("Hours", month => month.Hours * 1000));

        Assert.Equal("4.500", cut.Find("tbody td").TextContent);
        Assert.Contains("5.400", cut.FindAll(".tb-chart__y > span").Select(tick => tick.TextContent));
    }

    [Fact]
    public void Render_ShouldGiveASeriesItsOwnColor_WhenColorIsSet()
    {
        var cut = RenderChart(builder =>
        {
            builder.OpenComponent<TbBarSeries<Month>>(0);
            builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Failed");
            builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
                (Func<Month, object?>)(month => month.Failed));
            builder.AddComponentParameter(3, nameof(TbBarSeries<Month>.Color), "var(--tb-color-danger)");
            builder.CloseComponent();
        });

        Assert.Equal("--_tb-chart-series: var(--tb-color-danger)",
            cut.Find("g[data-series='Failed']").GetAttribute("style"));
    }

    // A key's mark has the series' own class, which sets the color it would take, so the color goes on the mark.
    [Fact]
    public void Render_ShouldGiveALegendKeyItsSeriesOwnColor_WhenColorIsSet()
    {
        RenderFragment Colored<TSeries>(string name) where TSeries : TbChartSeries<Month> => builder =>
        {
            builder.OpenComponent<TSeries>(0);
            builder.SetKey(name);
            builder.AddComponentParameter(1, nameof(TbChartSeries<Month>.Name), name);
            builder.AddComponentParameter(2, nameof(TbChartSeries<Month>.Value),
                (Func<Month, object?>)(month => month.Failed));
            builder.AddComponentParameter(3, nameof(TbChartSeries<Month>.Color), "var(--tb-color-danger)");
            builder.CloseComponent();
        };

        var cut = RenderChart(Colored<TbBarSeries<Month>>("Bars"), Colored<TbLineSeries<Month>>("Line"));

        var keys = cut.FindAll("[data-part=legend] li");
        const string colored = "--_tb-chart-series: var(--tb-color-danger)";
        Assert.Equal(colored, keys[0].QuerySelector("svg")!.GetAttribute("style"));
        Assert.Equal(colored, keys[1].QuerySelector("line")!.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldStartOverAtTheFirstColor_WhenThereAreMoreThanEightSeries()
    {
        var cut = RenderChart([.. Enumerable.Range(1, 9).Select(number =>
            Bars($"Series {number}", month => month.Completed))]);

        Assert.Equal(["tb-chart__series--8", "tb-chart__series--1"],
            cut.FindAll("g[data-series]").Skip(7).Select(series => series.ClassName));
    }

    [Fact]
    public void Render_ShouldSetTheHeightAndPartClasses_WhenTheyAreGiven()
    {
        var classes = new ChartClasses
        {
            Title = "my-title", Legend = "my-legend", Plot = "my-plot", Table = "my-table",
        };

        var cut = RenderChart(parameters => parameters.Add(chart => chart.Height, "12rem")
            .Add(chart => chart.Classes, classes).Add(chart => chart.Style, "margin: 0"),
            Bars("Completed", month => month.Completed), Bars("Failed", month => month.Failed));

        Assert.Equal("--tb-chart-height: 12rem; margin: 0", cut.Find("figure").GetAttribute("style"));
        Assert.Equal(["tb-chart__title my-title", "tb-chart__legend my-legend", "tb-chart__plot my-plot",
            "tb-chart__table my-table"], new[] { "title", "legend", "plot", "table" }.Select(part =>
            cut.Find($"[data-part={part}]").ClassName));
    }

    [Fact]
    public void Render_ShouldThrow_WhenASeriesValueIsNotANumber()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            RenderChart(Bars("Name", month => month.Name)));

        Assert.Equal("The chart series \"Name\" got a value of type String, which isn't a number.", thrown.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenASeriesIsOutsideAChart()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbBarSeries<Month>>(parameters =>
            parameters.Add(series => series.Name, "Completed").Add(series => series.Value, month => month.Completed)));

        Assert.Equal("TbBarSeries must be inside a TbChart.", thrown.Message);
    }

    [Theory]
    [InlineData(0d, null, null)]
    [InlineData(null, 5d, 5d)]
    public void Render_ShouldThrow_WhenTheValueAxisCannotBeDrawn(double? step, double? min, double? max)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderChart(builder =>
        {
            builder.OpenComponent<TbYAxis>(0);
            builder.AddComponentParameter(1, nameof(TbYAxis.Step), step);
            builder.AddComponentParameter(2, nameof(TbYAxis.Min), min);
            builder.AddComponentParameter(3, nameof(TbYAxis.Max), max);
            builder.CloseComponent();
        }));

        Assert.Equal("TbYAxis needs a Step above zero and a Min below its Max.", thrown.Message);
    }

    // An app's own component around a series, which renders again without the chart's parent doing so.
    private sealed class SwitchingSeries : ComponentBase
    {
        private bool _failed;

        public void Switch()
        {
            _failed = !_failed;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var failed = _failed;
            builder.OpenComponent<TbBarSeries<Month>>(0);
            builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), "Jobs");
            builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value),
                (Func<Month, object?>)(month => failed ? month.Failed : month.Completed));
            builder.CloseComponent();
        }
    }

    private IRenderedComponent<TbChart<Month>> RenderChart(params RenderFragment[] content) =>
        RenderChart(parameters => parameters, content);

    private IRenderedComponent<TbChart<Month>> RenderChart(
        Func<ComponentParameterCollectionBuilder<TbChart<Month>>, ComponentParameterCollectionBuilder<TbChart<Month>>>
            more, params RenderFragment[] content) =>
        Render<TbChart<Month>>(parameters => more(parameters.Add(chart => chart.Items, Months)
            .Add(chart => chart.X, month => month.Name).Add(chart => chart.Title, "Jobs by month")
            .Add(chart => chart.ChildContent, builder =>
            {
                foreach (var one in content)
                {
                    one(builder);
                }
            })));

    private static RenderFragment Bars(string name, Func<Month, object?> value) => builder =>
    {
        builder.OpenComponent<TbBarSeries<Month>>(0);
        builder.SetKey(name);
        builder.AddComponentParameter(1, nameof(TbBarSeries<Month>.Name), name);
        builder.AddComponentParameter(2, nameof(TbBarSeries<Month>.Value), value);
        builder.CloseComponent();
    };

    private static RenderFragment Line(string name, Func<Month, object?> value) => builder =>
    {
        builder.OpenComponent<TbLineSeries<Month>>(0);
        builder.SetKey(name);
        builder.AddComponentParameter(1, nameof(TbLineSeries<Month>.Name), name);
        builder.AddComponentParameter(2, nameof(TbLineSeries<Month>.Value), value);
        builder.CloseComponent();
    };
}
