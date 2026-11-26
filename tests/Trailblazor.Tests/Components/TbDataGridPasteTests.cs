using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Cells pasted into a grid that edits in batches, such as rows copied from a spreadsheet.
public class TbDataGridPasteTests : BunitContext
{
    public TbDataGridPasteTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Hard,
    }

    public sealed class Hike(int id, string name, int lengthKm)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;

        public int LengthKm { get; set; } = lengthKm;

        public Difficulty Difficulty { get; set; }

        public string Region => "North";
    }

    private readonly List<Hike> _hikes = [new(1, "Pine Hollow", 6), new(2, "Eagle Ridge", 14), new(3, "Cedar Loop", 3)];

    private readonly List<GridBatchEdit<Hike>> _saved = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(GridEditMode mode = GridEditMode.Batch) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, _hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.EditMode, mode).Add(grid => grid.CellSelection, true)
            .Add(grid => grid.OnBatchSave, (GridBatchEdit<Hike> batch) => _saved.Add(batch))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                .Add(name => name.Editable, true))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(region => region.Field, hike => hike.Region))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(length => length.Field, hike => hike.LengthKm)
                .Add(length => length.Editable, true))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(level => level.Field, hike => hike.Difficulty)
                .Add(level => level.Editable, true)));

    private async Task<List<string>> SavedAsync(IRenderedComponent<TbDataGrid<Hike>> cut)
    {
        await cut.InvokeAsync(cut.Instance.SaveChangesAsync);
        return [.. _saved.SelectMany(batch => batch.Changes).Select(change => $"{change.Item.Id} {change.Column} "
            + $"{change.Value}")];
    }

    [Fact]
    public async Task PasteAsync_ShouldKeepEachCellAsAChangeAndSkipReadOnlyColumns_WhenRowsComeFromASpreadsheet()
    {
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").PasteAsync("Pine Hollow", "Name", "Pine Top\tSouth\t7\nEagle Bend\tEast\t15\r\n");

        Assert.Equal(["1 Name Pine Top", "1 LengthKm 7", "2 Name Eagle Bend", "2 LengthKm 15"], await SavedAsync(cut));
    }

    [Fact]
    public async Task PasteAsync_ShouldStopAtTheLastRowAndColumn_WhenThePasteRunsPastThem()
    {
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").PasteAsync("Cedar Loop", "Length km", "4\thard\tmore\n5\teasy");

        Assert.Equal(["3 LengthKm 4", "3 Difficulty Hard"], await SavedAsync(cut));
    }

    [Fact]
    public async Task PasteAsync_ShouldFillTheRange_WhenOneValueIsPastedOverIt()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.SelectCellsAsync("Pine Hollow", "Length km", "Eagle Ridge", "Length km");

        await grid.PasteAsync("Pine Hollow", "Length km", "9");

        Assert.Equal(["1 LengthKm 9", "2 LengthKm 9"], await SavedAsync(cut));
    }

    [Fact]
    public async Task PasteAsync_ShouldKeepNothingAndSaySo_WhenACellCantHoldItsValue()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").PasteAsync("Pine Hollow", "Name", "Pine Top\tSouth\tfar\nEagle Bend\tEast\t15");

        Assert.Empty(await SavedAsync(cut));
        host.WaitForAssertion(() => Assert.Contains("Nothing was pasted: Length km can't hold that value.",
            host.Find("[role=alert]").TextContent));
    }

    [Fact]
    public async Task PasteAsync_ShouldThrow_WhenTheGridDoesntEditInBatches()
    {
        var cut = RenderHikes(GridEditMode.Cell);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().Grid("Hikes").PasteAsync("Pine Hollow", "Name", "Pine Top"));
    }
}
