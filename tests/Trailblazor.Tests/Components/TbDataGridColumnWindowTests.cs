using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Internal.Events;

namespace Trailblazor.Tests.Components;

public class TbDataGridColumnWindowTests : BunitContext
{
    public TbDataGridColumnWindowTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Reading(int Id, double[] Values);

    private static readonly Reading[] Readings =
    [
        new(1, [.. Enumerable.Range(0, 40).Select(at => (double)at)]),
        new(2, [.. Enumerable.Range(100, 40).Select(at => (double)at)]),
    ];

    private IRenderedComponent<TbDataGrid<Reading>> RenderReadings(bool virtualize = true,
        RenderFragment? columns = null) =>
        Render<TbDataGrid<Reading>>(parameters => parameters.Add(grid => grid.Items, Readings)
            .Add(grid => grid.AriaLabel, "Readings").Add(grid => grid.KeySelector, reading => reading.Id)
            .Add(grid => grid.VirtualizeColumns, virtualize).Add(grid => grid.ChildContent, columns ?? Columns));

    // A pinned Id column, then forty sensors; the first two have widths in px and rem.
    private static void Columns(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbColumn<Reading>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Reading>.Field), (Expression<Func<Reading, object?>>)(reading => reading.Id));
        builder.AddAttribute(2, nameof(TbColumn<Reading>.Pinned), ColumnPin.Start);
        builder.CloseComponent();
        for (var at = 0; at < 40; at++)
        {
            var sensor = at;
            builder.OpenComponent<TbColumn<Reading>>(3);
            builder.SetKey(sensor);
            builder.AddAttribute(4, nameof(TbColumn<Reading>.Id), $"s{sensor}");
            builder.AddAttribute(5, nameof(TbColumn<Reading>.Title), $"Sensor {sensor}");
            builder.AddAttribute(6, nameof(TbColumn<Reading>.Width), sensor switch { 0 => "120px", 1 => "5rem", _ => null });
            builder.AddAttribute(7, nameof(TbColumn<Reading>.CellTemplate),
                (RenderFragment<Reading>)(reading => cell => cell.AddContent(0, reading.Values[sensor])));
            builder.CloseComponent();
        }
    }

    private static List<string?> HeaderKeys(IRenderedComponent<TbDataGrid<Reading>> cut) =>
        [.. cut.FindAll("thead tr:last-child > *").Select(cell =>
            cell.GetAttribute("data-column") ?? cell.GetAttribute("data-part"))];

    [Fact]
    public void Grid_ShouldDrawThePinnedAndTheFirstColumns_WhenItDrawsOnlyTheColumnsInView()
    {
        var cut = RenderReadings();

        Assert.Equal(["Id", "column-spacer", .. Enumerable.Range(0, 12).Select(at => $"s{at}"), "column-spacer"],
            HeaderKeys(cut));
        Assert.Equal("41", cut.Find("table").GetAttribute("aria-colcount"));
        Assert.Equal("3", cut.Find("th[data-column=s1]").GetAttribute("aria-colindex"));
        Assert.Equal(["width: 0px", $"width: {28 * 160}px"],
            cut.FindAll("col:not([data-column])").Select(col => col.GetAttribute("style")));
    }

    [Fact]
    public async Task Grid_ShouldDrawTheColumnsTheScriptReports_WhenTheBoxScrolls()
    {
        var cut = RenderReadings();

        await cut.Find("table").TriggerEventAsync("ontbcolumnwindow", new ColumnWindowEventArgs { First = 20, Last = 25 });

        Assert.Equal(["Id", "column-spacer", .. Enumerable.Range(18, 10).Select(at => $"s{at}"), "column-spacer"],
            HeaderKeys(cut));
        Assert.Equal($"width: {120 + 80 + 16 * 160}px", cut.Find("col:not([data-column])").GetAttribute("style"));
        var cell = cut.Find("tbody tr[data-key='1'] td[data-column=s18]");
        Assert.Equal(("20", "18"), (cell.GetAttribute("aria-colindex"), cell.TextContent.Trim()));
    }

    [Fact]
    public void Grid_ShouldGiveTheScriptEachUnpinnedColumnsKeyAndWidth_WhenItDrawsOnlyTheColumnsInView()
    {
        var table = RenderReadings().Find("table");

        Assert.StartsWith("[\"s0\",\"s1\",\"s2\"", table.GetAttribute("data-column-keys"));
        Assert.StartsWith("120,80,160,", table.GetAttribute("data-column-widths"));
    }

    [Fact]
    public void Cells_ShouldCountTheSelectionColumnInTheirPlace_WhenRowsCanBeSelected()
    {
        var cut = Render<TbDataGrid<Reading>>(parameters => parameters.Add(grid => grid.Items, Readings)
            .Add(grid => grid.AriaLabel, "Readings").Add(grid => grid.KeySelector, reading => reading.Id)
            .Add(grid => grid.VirtualizeColumns, true).Add(grid => grid.SelectionMode, SelectionMode.Single)
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

        Assert.Equal("42", cut.Find("table").GetAttribute("aria-colcount"));
        Assert.Equal("4", cut.Find("tbody tr[data-key='1'] td[data-column=s1]").GetAttribute("aria-colindex"));
    }

    [Fact]
    public async Task Resize_ShouldKeepTheWidthsOfColumnsNotDrawn_WhenTheScriptReportsOnlyTheDrawnOnes()
    {
        var cut = RenderReadings();
        var table = cut.Find("table");
        await table.TriggerEventAsync("ontbcolumnwidths", new ColumnWidthsEventArgs { Widths = new() { ["s2"] = 200 } });

        await cut.Find("table").TriggerEventAsync("ontbcolumnwidths",
            new ColumnWidthsEventArgs { Widths = new() { ["s30"] = 90 } });

        var widths = cut.Find("table").GetAttribute("data-column-widths")!.Split(',');
        Assert.Equal(("200", "90"), (widths[2], widths[30]));
    }

    [Fact]
    public async Task Reorder_ShouldKeepThePlacesOfColumnsNotDrawn_WhenTheScriptReportsOnlyTheDrawnOnes()
    {
        var cut = RenderReadings();
        await cut.Find("table").TriggerEventAsync("ontbcolumnwindow", new ColumnWindowEventArgs { First = 20, Last = 25 });

        await cut.Find("table").TriggerEventAsync("ontbcolumnorder",
            new ColumnOrderEventArgs { Order = ["Id", .. Enumerable.Range(18, 10).Select(at => $"s{at}").Reverse()] });

        Assert.StartsWith("[\"s0\",\"s1\",", cut.Find("table").GetAttribute("data-column-keys"));
        Assert.Contains("\"s17\",\"s27\",\"s26\"", cut.Find("table").GetAttribute("data-column-keys"));
    }

    [Fact]
    public async Task Widths_ShouldFollowThePagesFontSize_WhenAColumnsWidthIsInRem()
    {
        var cut = RenderReadings();

        await cut.Find("table").TriggerEventAsync("ontbcolumnwindow",
            new ColumnWindowEventArgs { First = 0, Last = 9, RemPixels = 20 });

        Assert.StartsWith("120,100,160,", cut.Find("table").GetAttribute("data-column-widths"));
    }

    [Fact]
    public void Grid_ShouldDrawEveryColumn_WhenItDrawsAllOfThem()
    {
        var cut = RenderReadings(virtualize: false);

        Assert.Equal(41, cut.FindAll("thead th[data-column]").Count);
        Assert.Empty(cut.FindAll("[data-part=column-spacer]"));
        Assert.Null(cut.Find("table").GetAttribute("aria-colcount"));
    }

    [Fact]
    public void Grid_ShouldThrow_WhenItDrawsOnlyTheColumnsInViewWithColumnGroups()
    {
        RenderFragment grouped = builder =>
        {
            builder.OpenComponent<TbColumnGroup<Reading>>(0);
            builder.AddAttribute(1, nameof(TbColumnGroup<Reading>.Title), "Sensors");
            builder.AddAttribute(2, nameof(TbColumnGroup<Reading>.ChildContent), (RenderFragment)Columns);
            builder.CloseComponent();
        };

        Assert.Throws<InvalidOperationException>(() => RenderReadings(columns: grouped));
    }
}
