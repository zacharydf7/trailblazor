using System.ComponentModel.DataAnnotations;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridBatchTests : BunitContext
{
    public TbDataGridBatchTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Hard,
    }

    // A class whose values the grid can set in place.
    public sealed class Hike(int id, string name, int lengthKm)
    {
        public Difficulty Difficulty { get; set; }

        public int Id { get; } = id;

        public string Name { get; set; } = name;

        [Range(1, 100)]
        public int LengthKm { get; set; } = lengthKm;
    }

    private readonly List<Hike> _hikes = [new(1, "Pine Hollow", 6), new(2, "Eagle Ridge", 14)];

    private List<GridBatchEdit<Hike>> Saved { get; } = [];

    // When set, the save handler waits on it, as one sending the changes to a server would.
    private TaskCompletionSource? _server;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(Action<GridBatchEdit<Hike>>? save = null,
        bool handler = true) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, _hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.EditMode, GridEditMode.Batch)
                .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                    .Add(name => name.Editable, true))
                .AddChildContent<TbColumn<Hike>>(column => column.Add(length => length.Field, hike => hike.LengthKm)
                    .Add(length => length.Editable, true))
                .AddChildContent<TbColumn<Hike>>(column => column.Add(level => level.Field, hike => hike.Difficulty)
                    .Add(level => level.Editable, true));
            if (handler)
            {
                parameters.Add(grid => grid.OnBatchSave, async (GridBatchEdit<Hike> batch) =>
                {
                    Saved.Add(batch);
                    if (_server is not null)
                    {
                        await _server.Task;
                    }

                    save?.Invoke(batch);
                });
            }
        });

    private static IElement Cell(IRenderedComponent<TbDataGrid<Hike>> cut, int id, string column) =>
        cut.Find($"tr[data-key='{id}'] td[data-column={column}]");

    private static async Task EditAsync(IRenderedComponent<TbDataGrid<Hike>> cut, int id, string column, string value)
    {
        await Cell(cut, id, column).DoubleClickAsync(new());
        if (column == "LengthKm")
        {
            await cut.Tb().Within(() => Cell(cut, id, column)).NumberField("Length km").EnterAsync(value);
        }
        else
        {
            await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = value });
        }

        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey", new GridEditKeyEventArgs { Key = "Enter" });
    }

    private static string Count(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        cut.Find("[data-part=change-count]").TextContent.Trim();

    [Fact]
    public void Bar_ShouldShowNoChangesWithItsButtonsOff_WhenNothingIsEdited()
    {
        var cut = RenderHikes();

        Assert.Equal("Unsaved changes: 0", Count(cut));
        Assert.Equal(["true", "true"], cut.FindAll("[data-part=batch-bar] button")
            .Select(button => button.GetAttribute("aria-disabled")));
    }

    [Fact]
    public async Task Edit_ShouldKeepTheChangeMarkedOnItsCellWithoutSavingIt_WhenTheGridEditsInBatches()
    {
        var cut = RenderHikes();

        await EditAsync(cut, 1, "Name", "Pine loop");

        var cell = Cell(cut, 1, "Name");
        Assert.True(cell.HasAttribute("data-changed"));
        Assert.Contains("tb-data-grid__cell--changed", cell.ClassList);
        Assert.Equal("Pine loop", cell.TextContent.Trim());
        Assert.Equal("Changed", cut.Find($"#{cell.GetAttribute("aria-describedby")}").TextContent);
        Assert.Equal("Pine Hollow", _hikes[0].Name);
        Assert.Empty(Saved);
        Assert.Equal("Unsaved changes: 1", Count(cut));
        Assert.True(cut.Instance.HasChanges);
    }

    [Fact]
    public async Task Edit_ShouldKeepTheChoice_WhenAValueIsChosenFromAList()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Difficulty").DoubleClickAsync(new());

        await cut.Tb().Within(() => Cell(cut, 1, "Difficulty")).Select("Difficulty").ChooseAsync("Hard");

        Assert.True(Cell(cut, 1, "Difficulty").HasAttribute("data-changed"));
        Assert.Equal("Unsaved changes: 1", Count(cut));
    }

    [Fact]
    public async Task Edit_ShouldStartFromTheChange_WhenAChangedCellIsEditedAgain()
    {
        var cut = RenderHikes();
        await EditAsync(cut, 1, "Name", "Pine loop");

        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        Assert.Equal("Pine loop", cut.Find("[data-part=editor] input").GetAttribute("value"));
    }

    [Fact]
    public async Task Edit_ShouldDropTheChange_WhenTheCellIsEditedBackToItsValue()
    {
        var cut = RenderHikes();
        await EditAsync(cut, 1, "Name", "Pine loop");

        await EditAsync(cut, 1, "Name", "Pine Hollow");

        Assert.False(Cell(cut, 1, "Name").HasAttribute("data-changed"));
        Assert.Equal("Unsaved changes: 0", Count(cut));
    }

    [Fact]
    public async Task Edit_ShouldKeepNoChange_WhenTheValueFailsItsCheck()
    {
        var cut = RenderHikes();

        await EditAsync(cut, 1, "LengthKm", "500");

        Assert.NotEmpty(cut.FindAll("[data-part=editor]"));
        Assert.Equal("Unsaved changes: 0", Count(cut));
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRaiseEveryChangeInOrderAndClearTheMarks_WhenSaveChangesIsClicked()
    {
        var cut = RenderHikes();
        await EditAsync(cut, 2, "LengthKm", "9");
        await EditAsync(cut, 1, "Name", "Pine loop");

        await cut.Tb().Grid("Hikes").SaveChangesAsync();

        Assert.Equal([(2, "LengthKm", (object?)9), (1, "Name", "Pine loop")],
            Saved.Single().Changes.Select(change => (change.Item.Id, change.Column, change.Value)));
        Assert.Empty(cut.FindAll("td[data-changed]"));
        Assert.Equal("Unsaved changes: 0", Count(cut));
        Assert.Equal(14, _hikes[1].LengthKm);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRaiseTheChangesInTheOrderMade_WhenAChangeWasDroppedInBetween()
    {
        var cut = RenderHikes();
        await EditAsync(cut, 1, "Name", "Pine loop");
        await EditAsync(cut, 2, "Name", "Eagle loop");
        await EditAsync(cut, 1, "Name", "Pine Hollow");
        await EditAsync(cut, 1, "LengthKm", "9");

        await cut.Tb().Grid("Hikes").SaveChangesAsync();

        Assert.Equal([(2, "Name"), (1, "LengthKm")],
            Saved.Single().Changes.Select(change => (change.Item.Id, change.Column)));
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveOnceAndKeepNewerChanges_WhenUsedWhileASaveIsOnItsWay()
    {
        _server = new();
        var cut = RenderHikes(batch => _hikes.Find(hike => hike.Id == 2)!.LengthKm = 9);
        var grid = cut.Tb().Grid("Hikes");
        await EditAsync(cut, 1, "Name", "Pine loop");
        await EditAsync(cut, 2, "LengthKm", "9");

        var saving = grid.SaveChangesAsync();
        await EditAsync(cut, 2, "Name", "Eagle loop");
        await EditAsync(cut, 1, "Name", "Pine trail");
        await EditAsync(cut, 2, "LengthKm", "14");
        var again = grid.SaveChangesAsync();
        _server.SetResult();
        await Task.WhenAll(saving, again);

        Assert.Equal(["Pine loop", (object?)9], Saved.Single().Changes.Select(change => change.Value));
        Assert.Equal("Pine trail", Cell(cut, 1, "Name").TextContent.Trim());
        Assert.True(Cell(cut, 2, "Name").HasAttribute("data-changed"));
        Assert.Equal("14", Cell(cut, 2, "LengthKm").TextContent.Trim());
        Assert.True(Cell(cut, 2, "LengthKm").HasAttribute("data-changed"));
        Assert.Equal("Unsaved changes: 3", Count(cut));
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetTheValuesOnTheItems_WhenNoHandlerTakesThem()
    {
        var cut = RenderHikes(handler: false);
        await EditAsync(cut, 1, "Name", "Pine loop");

        await cut.Tb().Grid("Hikes").SaveChangesAsync();

        Assert.Equal("Pine loop", _hikes[0].Name);
        Assert.Equal("Pine loop", Cell(cut, 1, "Name").TextContent.Trim());
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldKeepTheChangesAndSayWhy_WhenTheHandlerTurnsThemDown()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderHikes(batch => batch.Error = "The server is down.");
        await EditAsync(cut, 1, "Name", "Pine loop");

        await cut.Tb().Grid("Hikes").SaveChangesAsync();

        Assert.Equal("The server is down.", cut.Find("[data-part=batch-error]").TextContent);
        Assert.True(Cell(cut, 1, "Name").HasAttribute("data-changed"));
        Assert.Equal("Unsaved changes: 1", Count(cut));
        announcer.WaitForAssertion(() => Assert.Equal("The server is down.",
            announcer.FindAll("[role=alert] p").Last().TextContent));
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveAnOpenEditWithTheRest_WhenCalledFromCode()
    {
        var cut = RenderHikes();
        await EditAsync(cut, 1, "Name", "Pine loop");
        await Cell(cut, 2, "Name").DoubleClickAsync(new());
        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Eagle loop" });

        await cut.InvokeAsync(() => cut.Instance.SaveChangesAsync());

        Assert.Equal(["Pine loop", "Eagle loop"], Saved.Single().Changes.Select(change => change.Value));
    }

    [Fact]
    public async Task DiscardChangesAsync_ShouldShowTheItemsValuesAgain_WhenDiscardChangesIsClicked()
    {
        var cut = RenderHikes();
        await EditAsync(cut, 1, "Name", "Pine loop");

        await cut.Tb().Grid("Hikes").DiscardChangesAsync();

        Assert.Equal("Pine Hollow", Cell(cut, 1, "Name").TextContent.Trim());
        Assert.Empty(cut.FindAll("td[data-changed]"));
        Assert.False(cut.Instance.HasChanges);
        Assert.Empty(Saved);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrow_WhenTheGridHoldsNoChanges()
    {
        var cut = RenderHikes();

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Grid("Hikes").SaveChangesAsync());
    }
}
