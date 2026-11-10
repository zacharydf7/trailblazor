using System.Linq.Expressions;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridColumnOrderTests : BunitContext
{
    public TbDataGridColumnOrderTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, string Region, double LengthKm, string Notes);

    private static IReadOnlyList<Hike> Hikes => [new(1, "Pine Hollow", "North ridge", 6.4, "Shaded")];

    private List<GridState> States { get; } = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(GridState? state = null,
        SelectionMode selection = SelectionMode.None) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.SelectionMode, selection)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.ReorderableColumns, true).Add(grid => grid.State, state)
            .Add(grid => grid.StateChanged, (GridState next) => States.Add(next))
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

    private static void Columns(RenderTreeBuilder builder)
    {
        Expression<Func<Hike, object?>>[] fields =
            [hike => hike.Name, hike => hike.Region, hike => hike.LengthKm, hike => hike.Notes];
        for (var at = 0; at < fields.Length; at++)
        {
            builder.OpenComponent<TbColumn<Hike>>(at * 3);
            builder.AddAttribute(at * 3 + 1, nameof(TbColumn<Hike>.Field), fields[at]);
            // Notes is pinned at the end, so it moves only among the columns pinned there.
            builder.AddAttribute(at * 3 + 2, nameof(TbColumn<Hike>.Pinned), at == 3 ? ColumnPin.End : ColumnPin.None);
            builder.CloseComponent();
        }
    }

    private static IReadOnlyList<string?> Headers(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("thead th[data-column]").Select(header => header.GetAttribute("data-column"))];

    private static IReadOnlyList<string?> Cells(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody td[data-column]").Select(cell => cell.GetAttribute("data-column"))];

    [Fact]
    public async Task ColumnMenu_ShouldMoveTheColumnAndItsCellsAndKeepTheOrder_WhenMoveEarlierIsChosen()
    {
        var cut = RenderHikes();

        await cut.Tb().Menu("Options for Region").ChooseAsync("Move Region earlier");

        cut.WaitForAssertion(() => Assert.Equal(["Region", "Name", "LengthKm", "Notes"], Headers(cut)));
        Assert.Equal(["Region", "Name", "LengthKm", "Notes"], Cells(cut));
        Assert.Equal(["Region", "Name", "LengthKm", "Notes"], States.Single().ColumnOrder);
        Assert.Equal("Region", cut.Tb().Grid("Hikes").Columns[0]);
    }

    [Fact]
    public void ColumnMenu_ShouldTurnOffAMoveOffItsSide_WhenAColumnIsAtTheEdgeOfIt()
    {
        var cut = RenderHikes();
        var length = cut.Find("th[data-column=LengthKm]").GetAttribute("data-title");

        Assert.Equal("true", MenuItem(cut, "Move Name earlier").GetAttribute("aria-disabled"));
        Assert.Equal("true", MenuItem(cut, $"Move {length} later").GetAttribute("aria-disabled"));
        Assert.Null(MenuItem(cut, "Move Region later").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldGiveNoMenu_WhenAColumnIsAloneOnItsPinnedSide()
    {
        var cut = RenderHikes();

        Assert.Empty(cut.FindAll("th[data-column=Notes] [data-part=column-menu]"));
        Assert.NotEmpty(cut.FindAll("th[data-column=Name] [data-part=column-menu]"));
    }

    [Fact]
    public async Task ColumnMenu_ShouldReadOutTheColumnsNewPlace_WhenItMovesTheColumn()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();

        await cut.Tb().Menu("Options for Name").ChooseAsync("Move Name later");

        host.WaitForAssertion(() => Assert.Equal(["Name moved to column 2 of 4"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public async Task ColumnMenu_ShouldCountTheSelectionColumnInThePlaceItReadsOut_WhenRowsCanBeSelected()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(selection: SelectionMode.Multiple);

        await cut.Tb().Menu("Options for Name").ChooseAsync("Move Name later");

        host.WaitForAssertion(() => Assert.Equal(["Name moved to column 3 of 5"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void GridState_ShouldKeepItsColumnOrderAndCompareIt_WhenReadBackFromJson()
    {
        var state = new GridState { ColumnOrder = ["Region", "Name"] };

        var read = JsonSerializer.Deserialize<GridState>(JsonSerializer.Serialize(state));

        Assert.Equal(state, read);
        Assert.NotEqual(state, read! with { ColumnOrder = ["Name", "Region"] });
        Assert.Empty(JsonSerializer.Deserialize<GridState>("""{"ColumnOrder":null}""")!.ColumnOrder);
    }

    [Fact]
    public void State_ShouldPutTheNamedColumnsFirstAndTheOthersAfter_WhenItHasAColumnOrder()
    {
        var cut = RenderHikes(new() { ColumnOrder = ["LengthKm", "Name"] });

        Assert.Equal(["LengthKm", "Name", "Region", "Notes"], Headers(cut));
    }

    // A menu's items render hidden in its popup, so they can be read without opening it.
    private static AngleSharp.Dom.IElement MenuItem(IRenderedComponent<TbDataGrid<Hike>> cut, string name) =>
        cut.FindAll("[role=menuitem]").Single(item => item.TextContent.Trim() == name);
}
