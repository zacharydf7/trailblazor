using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Internal.Events;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridCellRangeTests : BunitContext
{
    public TbDataGridCellRangeTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, string Region, double LengthKm);

    private readonly List<Hike> _hikes =
    [
        new(1, "Pine Hollow", "North ridge", 6.5),
        new(2, "Eagle Ridge", "North ridge", 14),
        new(3, "Cedar Flats", "River valley", 3.25),
        new(4, "Birch Bend", "River valley", 9),
    ];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(bool cellSelection = true) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, _hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.CellSelection, cellSelection)
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(region => region.Field, hike => hike.Region))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(length => length.Field, hike => hike.LengthKm)
                .Add(length => length.Format, "0.0").Add(length => length.Sortable, true)));

    private static List<string> InRange(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("td[aria-selected=true]").Select(cell =>
            $"{cell.ParentElement!.GetAttribute("data-key")}:{cell.GetAttribute("data-column")}")];

    [Fact]
    public void Grid_ShouldBeAGridWhoseCellsCanBeSelected_WhenCellSelectionIsOn()
    {
        var cut = RenderHikes();

        var table = cut.Find("table");
        Assert.Equal("grid", table.GetAttribute("role"));
        Assert.Equal("true", table.GetAttribute("aria-multiselectable"));
        Assert.All(cut.FindAll("tbody td"), cell => Assert.Equal("false", cell.GetAttribute("aria-selected")));
    }

    [Fact]
    public void Grid_ShouldLeaveCellsUnselectable_WhenCellSelectionIsOff()
    {
        var cut = RenderHikes(cellSelection: false);

        Assert.Null(cut.Find("table").GetAttribute("role"));
        Assert.Empty(cut.FindAll("td[aria-selected]"));
    }

    [Fact]
    public async Task SelectCellsAsync_ShouldMarkTheCellsBetweenTheTwo_WhenARangeIsSelected()
    {
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").SelectCellsAsync("Eagle Ridge", "Length km", "Pine Hollow", "Region");

        Assert.Equal(["1:Region", "1:LengthKm", "2:Region", "2:LengthKm"], InRange(cut));
        Assert.All(cut.FindAll("td[aria-selected=true]"), cell =>
        {
            Assert.True(cell.HasAttribute("data-in-range"));
            Assert.Contains("tb-data-grid__cell--in-range", cell.ClassList);
        });
        Assert.True(cut.Find("table").HasAttribute("data-cell-range"));
    }

    [Fact]
    public async Task SelectedCells_ShouldHoldTheRangeTextInTheOrderShown_WhenARangeIsSelected()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");

        await grid.SelectCellsAsync("Cedar Flats", "Length km", "Pine Hollow", "Name");

        Assert.Equal([["Pine Hollow", "North ridge", 6.5.ToString("0.0")],
            ["Eagle Ridge", "North ridge", 14.0.ToString("0.0")], ["Cedar Flats", "River valley", 3.25.ToString("0.0")]],
            grid.SelectedCells);
    }

    [Fact]
    public async Task SelectCellsAsync_ShouldClearTheRange_WhenOneCellIsSelected()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Region");

        await grid.SelectCellsAsync("Pine Hollow", "Name", "Pine Hollow", "Name");

        Assert.Empty(InRange(cut));
        Assert.Empty(grid.SelectedCells);
        Assert.False(cut.Find("table").HasAttribute("data-cell-range"));
    }

    [Fact]
    public async Task Range_ShouldClear_WhenTheScriptSendsNoCells()
    {
        var cut = RenderHikes();
        await cut.Tb().Grid("Hikes").SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Region");

        await cut.Find("table").TriggerEventAsync("ontbcellrange", new CellRangeEventArgs());

        Assert.Empty(InRange(cut));
    }

    [Fact]
    public async Task Range_ShouldRunBetweenItsEndsInTheNewOrder_WhenTheRowsAreSorted()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Name");

        await grid.SortAsync("Length km");

        Assert.Equal([["Pine Hollow"], ["Birch Bend"], ["Eagle Ridge"]], grid.SelectedCells);
        Assert.Equal(["1:Name", "4:Name", "2:Name"], InRange(cut));
    }

    [Fact]
    public async Task Range_ShouldBeGone_WhenCellSelectionIsTurnedOff()
    {
        var cut = RenderHikes();
        await cut.Tb().Grid("Hikes").SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Region");

        cut.Render(parameters => parameters.Add(grid => grid.CellSelection, false));

        Assert.Empty(cut.FindAll("td[data-in-range]"));
        Assert.Empty(cut.Tb().Grid("Hikes").SelectedCells);
    }

    [Fact]
    public void Cells_ShouldStopBeingSelectable_WhenCellSelectionIsTurnedOff()
    {
        var cut = RenderHikes();

        cut.Render(parameters => parameters.Add(grid => grid.CellSelection, false));

        Assert.Empty(cut.FindAll("td[aria-selected]"));
    }

    [Fact]
    public async Task SelectedCells_ShouldHoldAChangeAsItShows_WhenTheGridEditsInBatches()
    {
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, _hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.CellSelection, true).Add(grid => grid.EditMode, GridEditMode.Batch)
            .Add(grid => grid.OnBatchSave, (GridBatchEdit<Hike> _) => { })
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                .Add(name => name.Editable, true).Add(name => name.CellTemplate, hike => $"{hike.Name}!"))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(region => region.Field, hike => hike.Region)));
        await cut.Find("tr[data-key='1'] td[data-column=Name]").DoubleClickAsync(new());
        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Pine loop" });
        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey", new GridEditKeyEventArgs { Key = "Enter" });

        await cut.Tb().Grid("Hikes").SelectCellsAsync("Pine loop", "Name", "Eagle Ridge!", "Region");

        Assert.Equal([["Pine loop", "North ridge"], ["Eagle Ridge", "North ridge"]],
            cut.Tb().Grid("Hikes").SelectedCells);
        Assert.Equal(["Pine loop", "Eagle Ridge"], cut.FindAll("tr[data-key='1'] td[data-column=Name], "
            + "tr[data-key='2'] td[data-column=Name]").Select(cell => cell.GetAttribute("data-copy")));
    }

    [Fact]
    public async Task SelectCellsAsync_ShouldThrow_WhenCellSelectionIsOff()
    {
        var cut = RenderHikes(cellSelection: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().Grid("Hikes").SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Region"));
    }
}
