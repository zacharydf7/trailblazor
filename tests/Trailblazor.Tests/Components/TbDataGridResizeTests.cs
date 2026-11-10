using System.Linq.Expressions;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridResizeTests : BunitContext
{
    public TbDataGridResizeTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, string Region, string Notes);

    private static IReadOnlyList<Hike> Hikes =>
    [
        new(1, "Pine Hollow", "North ridge", "Shaded"),
        new(2, "Eagle Ridge", "High peaks", "Exposed"),
    ];

    private List<GridState> States { get; } = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(bool resizable = true, GridState? state = null) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.ResizableColumns, resizable).Add(grid => grid.State, state)
            .Add(grid => grid.StateChanged, (GridState next) => States.Add(next))
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

    private static void Columns(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Name));
        builder.AddAttribute(2, nameof(TbColumn<Hike>.MinWidth), "8rem");
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(3);
        builder.AddAttribute(4, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Region));
        builder.AddAttribute(5, nameof(TbColumn<Hike>.Width), "10rem");
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(6);
        builder.AddAttribute(7, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Notes));
        builder.AddAttribute(8, nameof(TbColumn<Hike>.Resizable), false);
        builder.CloseComponent();
    }

    private static string? StyleOf(IRenderedComponent<TbDataGrid<Hike>> cut, string column) =>
        cut.Find($"th[data-column={column}]").GetAttribute("style");

    [Fact]
    public void Render_ShouldGiveEachResizableColumnAMenuAndAnEdge_WhenColumnsAreResizable()
    {
        var cut = RenderHikes();

        Assert.Equal(2, cut.FindAll("[data-part=resize]").Count);
        var menus = cut.FindAll("[data-part=column-menu] button[aria-label]");
        Assert.Equal(["Options for Name", "Options for Region"],
            menus.Select(button => button.GetAttribute("aria-label")));
        Assert.Contains("min-inline-size: 8rem", StyleOf(cut, "Name"));
        Assert.DoesNotContain("tb-data-grid__table--fixed", cut.Find("table").ClassList);
    }

    [Fact]
    public void Render_ShouldOfferNoResizing_WhenColumnsArentResizable()
    {
        var cut = RenderHikes(resizable: false);

        Assert.Empty(cut.FindAll("[data-part=resize]"));
        Assert.Empty(cut.FindAll("[data-part=column-menu]"));
        Assert.Equal("width: 10rem", StyleOf(cut, "Region"));
    }

    [Fact]
    public async Task ColumnWidthsEvent_ShouldFixEveryColumnAndKeepTheWidthsInTheState_WhenRaised()
    {
        var cut = RenderHikes();

        await cut.Find("table").TriggerEventAsync("ontbcolumnwidths", new ColumnWidthsEventArgs
        {
            Widths = new() { ["Name"] = 180.4, ["Region"] = 160, ["Notes"] = 120, ["Gone"] = 90 },
        });

        Assert.Equal(new Dictionary<string, double> { ["Name"] = 180, ["Region"] = 160, ["Notes"] = 120 },
            States.Single().ColumnWidths);
        Assert.Contains("tb-data-grid__table--fixed", cut.Find("table").ClassList);
        Assert.StartsWith("width: 180px", StyleOf(cut, "Name"));
        Assert.Equal("160", cut.Find("th[data-column=Region]").GetAttribute("data-width"));
    }

    [Fact]
    public void Render_ShouldPreferTheWidthsRatherThanFixThem_WhenAColumnHasNoWidth()
    {
        var cut = RenderHikes(state: new() { ColumnWidths = new Dictionary<string, double>
        {
            ["Name"] = 150, ["Region"] = 160,
        } });

        Assert.DoesNotContain("tb-data-grid__table--fixed", cut.Find("table").ClassList);
        Assert.StartsWith("width: 150px", StyleOf(cut, "Name"));
        Assert.Equal("150", cut.Find("th[data-column=Name]").GetAttribute("data-width"));
    }

    [Fact]
    public async Task ResizeColumnAsync_ShouldKeepTheOtherColumnsWidths_WhenCalled()
    {
        var cut = RenderHikes(state: new() { ColumnWidths = new Dictionary<string, double>
        {
            ["Name"] = 150, ["Region"] = 160, ["Notes"] = 120,
        } });

        await cut.Tb().Grid("Hikes").ResizeColumnAsync("Region", 200);

        Assert.Equal(new Dictionary<string, double> { ["Name"] = 150, ["Region"] = 200, ["Notes"] = 120 },
            States.Single().ColumnWidths);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Grid("Hikes").ResizeColumnAsync("Notes", 90));
    }

    [Fact]
    public async Task ColumnMenu_ShouldGiveTheColumnsBackTheirWidths_WhenResetIsChosen()
    {
        var cut = RenderHikes(state: new() { ColumnWidths = new Dictionary<string, double> { ["Name"] = 150 } });

        await cut.Tb().Menu("Options for Name").ChooseAsync("Reset column widths");

        Assert.Empty(States.Single().ColumnWidths);
        Assert.DoesNotContain("tb-data-grid__table--fixed", cut.Find("table").ClassList);
        Assert.Equal("width: 10rem; min-inline-size: 8rem", StyleOf(cut, "Region"));
    }

    [Fact]
    public void GridState_ShouldKeepItsColumnWidthsAndCompareThem_WhenReadBackFromJson()
    {
        var state = new GridState { ColumnWidths = new Dictionary<string, double> { ["Name"] = 150, ["Region"] = 90 } };

        var read = JsonSerializer.Deserialize<GridState>(JsonSerializer.Serialize(state));

        Assert.Equal(state, read);
        Assert.NotEqual(state, read! with { ColumnWidths = new Dictionary<string, double> { ["Name"] = 150 } });
        Assert.Empty(JsonSerializer.Deserialize<GridState>("""{"ColumnWidths":null}""")!.ColumnWidths);
    }
}
