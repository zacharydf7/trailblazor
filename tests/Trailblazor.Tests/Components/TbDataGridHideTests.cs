using System.Linq.Expressions;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridHideTests : BunitContext
{
    public TbDataGridHideTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, string Region, double LengthKm, string Notes);

    private static IReadOnlyList<Hike> Hikes => [new(1, "Pine Hollow", "North ridge", 6.4, "Shaded")];

    private List<GridState> States { get; } = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(GridState? state = null, bool hideable = true,
        bool nameHideable = true, bool resizable = false) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.HideableColumns, hideable).Add(grid => grid.State, state)
            .Add(grid => grid.ResizableColumns, resizable)
            .Add(grid => grid.StateChanged, (GridState next) => States.Add(next))
            .Add(grid => grid.ChildContent, (RenderFragment)(builder => Columns(builder, nameHideable))));

    private static void Columns(RenderTreeBuilder builder, bool nameHideable)
    {
        Expression<Func<Hike, object?>>[] fields =
            [hike => hike.Name, hike => hike.Region, hike => hike.LengthKm, hike => hike.Notes];
        for (var at = 0; at < fields.Length; at++)
        {
            builder.OpenComponent<TbColumn<Hike>>(at * 3);
            builder.AddAttribute(at * 3 + 1, nameof(TbColumn<Hike>.Field), fields[at]);
            builder.AddAttribute(at * 3 + 2, nameof(TbColumn<Hike>.Hideable), at != 0 || nameHideable);
            builder.CloseComponent();
        }
    }

    private static IReadOnlyList<string?> Headers(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("thead th[data-column]").Select(header => header.GetAttribute("data-column"))];

    private static IReadOnlyList<string?> Cells(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody td[data-column]").Select(cell => cell.GetAttribute("data-column"))];

    private static IReadOnlyList<string> Choices(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("[data-part=column-chooser] [role=menuitemcheckbox]").Select(item => item.TextContent.Trim())];

    [Fact]
    public async Task Chooser_ShouldHideTheColumnAndItsCellsAndKeepItInTheState_WhenItsItemIsUnchecked()
    {
        var cut = RenderHikes();

        await cut.Tb().Menu("Columns").ChooseAsync("Region");

        cut.WaitForAssertion(() => Assert.Equal(["Name", "LengthKm", "Notes"], Headers(cut)));
        Assert.Equal(["Name", "LengthKm", "Notes"], Cells(cut));
        Assert.Equal(["Region"], States.Single().HiddenColumns);
        Assert.Equal("false", cut.FindAll("[data-part=column-chooser] [role=menuitemcheckbox]")[1]
            .GetAttribute("aria-checked"));
    }

    [Fact]
    public async Task Chooser_ShouldShowTheColumnInItsPlaceAgain_WhenItsItemIsChecked()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Region"] });
        Assert.Equal(["Name", "LengthKm", "Notes"], Headers(cut));

        await cut.Tb().Menu("Columns").ChooseAsync("Region");

        cut.WaitForAssertion(() => Assert.Equal(["Name", "Region", "LengthKm", "Notes"], Headers(cut)));
        Assert.Empty(States.Single().HiddenColumns);
    }

    [Fact]
    public void Chooser_ShouldListEveryColumnInOrder_WhenSomeAreHidden()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Region"], ColumnOrder = ["Notes"] });

        Assert.Equal(["Notes", "Name", "Region", "Length km"], Choices(cut));
    }

    [Fact]
    public async Task Chooser_ShouldKeepTheLastColumnShown_WhenEveryOtherIsHidden()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Name", "Region", "LengthKm"] });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Menu("Columns").ChooseAsync("Notes"));

        Assert.Contains("disabled", error.Message);
        Assert.Empty(cut.FindAll("[data-part=column-menu]"));
    }

    [Fact]
    public void Chooser_ShouldTurnOffAColumnThatCantBeHidden_WhenItsHideableIsOff()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Name"] }, nameHideable: false);

        var name = cut.FindAll("[data-part=column-chooser] [role=menuitemcheckbox]")[0];
        Assert.Equal(("true", "true"), (name.GetAttribute("aria-checked"), name.GetAttribute("aria-disabled")));
        Assert.Equal(["Name", "Region", "LengthKm", "Notes"], Headers(cut));
    }

    [Fact]
    public async Task ColumnMenu_ShouldHideTheColumnAndReadItOut_WhenHideIsChosen()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();

        await cut.Tb().Menu("Options for Region").ChooseAsync("Hide Region");

        cut.WaitForAssertion(() => Assert.Equal(["Name", "LengthKm", "Notes"], Headers(cut)));
        Assert.Equal(["Region"], States.Single().HiddenColumns);
        host.WaitForAssertion(() => Assert.Equal(["Region hidden"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void State_ShouldShowEveryColumn_WhenItHidesThemAll()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Name", "Region", "LengthKm", "Notes"] });

        Assert.Equal(["Name", "Region", "LengthKm", "Notes"], Headers(cut));
    }

    [Fact]
    public async Task Chooser_ShouldHideTheColumn_WhenTheStateHidEveryColumn()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Name", "Region", "LengthKm", "Notes"] });

        await cut.Tb().Menu("Columns").ChooseAsync("Region");

        cut.WaitForAssertion(() => Assert.Equal(["Name", "LengthKm", "Notes"], Headers(cut)));
        Assert.Equal(["Region"], States.Single().HiddenColumns);
    }

    [Fact]
    public async Task ColumnWidthsEvent_ShouldKeepAHiddenColumnsWidth_WhenTheShownColumnsAreResized()
    {
        var cut = RenderHikes(new GridState
        {
            HiddenColumns = ["Region"],
            ColumnWidths = new Dictionary<string, double> { ["Name"] = 160, ["Region"] = 140, ["LengthKm"] = 100 },
        });

        await cut.Find("table").TriggerEventAsync("ontbcolumnwidths", new ColumnWidthsEventArgs
        {
            Widths = new() { ["Name"] = 200, ["LengthKm"] = 100, ["Notes"] = 120 },
        });

        Assert.Equal(
            new Dictionary<string, double> { ["Name"] = 200, ["Region"] = 140, ["LengthKm"] = 100, ["Notes"] = 120 },
            States.Single().ColumnWidths);
    }

    [Fact]
    public void State_ShouldMeasureTheColumnsAgain_WhenItShowsAColumnThatHasNoWidth()
    {
        var script = JSInterop.SetupModule("./_content/Trailblazor/data-grid.js")
            .SetupModule("createDataGrid", _ => true);
        script.Mode = JSRuntimeMode.Loose;
        var widths = new Dictionary<string, double> { ["Name"] = 160, ["LengthKm"] = 100, ["Notes"] = 120 };
        // A key from an older saved state that no column has any more can't be measured.
        var cut = RenderHikes(new GridState { HiddenColumns = ["Gone", "Region"], ColumnWidths = widths },
            resizable: true);
        Assert.Empty(script.Invocations["resizeColumn"]);

        cut.Render(parameters => parameters.Add(grid => grid.State, new GridState { ColumnWidths = widths }));

        cut.WaitForAssertion(() => Assert.Equal(["Region", 0],
            script.Invocations["resizeColumn"].Single().Arguments));
    }

    [Fact]
    public void GridState_ShouldKeepItsHiddenColumnsAndCompareThem_WhenReadBackFromJson()
    {
        var state = new GridState { HiddenColumns = ["Region", "Notes"] };

        var read = JsonSerializer.Deserialize<GridState>(JsonSerializer.Serialize(state));

        Assert.Equal(state, read);
        Assert.NotEqual(state, read! with { HiddenColumns = ["Notes"] });
        Assert.Empty(JsonSerializer.Deserialize<GridState>("""{"HiddenColumns":null}""")!.HiddenColumns);
    }

    [Fact]
    public void State_ShouldHideNothingAndShowNoChooser_WhenTheGridDoesntLetColumnsHide()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Region"] }, hideable: false);

        Assert.Equal(["Name", "Region", "LengthKm", "Notes"], Headers(cut));
        Assert.Empty(cut.FindAll("[data-part=toolbar]"));
    }

    [Fact]
    public async Task ColumnOrderEvent_ShouldKeepAHiddenColumnInItsPlace_WhenTheShownColumnsMove()
    {
        var cut = RenderHikes(new GridState { HiddenColumns = ["Region"] });

        await cut.Find("table").TriggerEventAsync("ontbcolumnorder",
            new ColumnOrderEventArgs { Order = ["LengthKm", "Name", "Notes"] });

        Assert.Equal(["LengthKm", "Region", "Name", "Notes"], States.Single().ColumnOrder);
        Assert.Equal(["LengthKm", "Name", "Notes"], Headers(cut));
    }
}
