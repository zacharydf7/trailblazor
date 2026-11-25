using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridLabelsTests : BunitContext
{
    public TbDataGridLabelsTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, int LengthKm, bool Loop, IReadOnlyList<Hike>? Spurs = null);

    private static readonly DataGridLabels Words = new()
    {
        GroupBy = "Cluster by", Columns = "Show fields", Export = "Download", ExportCsv = "As CSV",
        ExportExcel = "As XLSX", ExportPdf = "As PDF file", Pages = "{0} pages", Order = "Place", Details = "More",
        DetailsFor = "More on {0}", Select = "Pick", SelectRow = "Pick {0}", RowsUnder = "Spurs of {0}",
        Changed = "Edited", UnsavedChanges = "Pending: {0}", SaveChanges = "Store all", DiscardChanges = "Drop all",
        ChangesSaved = "All stored", Save = "Keep", SaveRow = "Keep {0}", Cancel = "Undo", CancelRow = "Undo {0}",
        EditInvalid = "{0} won't take that", EditRequired = "{0} needs a value", SortedAscending = "{0} going up",
        SortedDescending = "{0} going down", Unsorted = "{0} in no order", SortPosition = "order {0}/{1} {2}",
        Ascending = "up", Descending = "down", ColumnMenu = "Menu for {0}", Earlier = "{0} left",
        Later = "{0} right", Wider = "{0} wider please", Narrower = "{0} narrower please", ResetWidths = "Even out",
        Hide = "Drop {0}", ColumnHidden = "{0} gone", ColumnMoved = "{0} now {1} of {2}", Filter = "Narrow {0}",
        FilterOn = "Narrow {0}, active", FilterCondition = "Rule", FilterValue = "Target", FilterAny = "Whatever",
        FilterYes = "Sure", FilterNo = "Nah", FilterClear = "Reset", FilterApply = "Use", RowsShown = "Showing {0}",
        Totals = "Grand", GroupTotals = "Subtotal {0}", GroupHeading = "{0} ~ {1}", GroupNone = "(none)",
        GroupCount = "[{0}]", Sum = "Added", Average = "Mean", Count = "Tally", Min = "Least", Max = "Most",
        RowHandles = new() { Handle = "Drag {0}", Hint = "Arrows shift it", Moved = "{0} sits at {1} of {2}" },
        Comparisons = new() { GreaterThanOrEqualTo = "from", LessThan = "below" },
    };

    private readonly List<Hike> _hikes =
    [
        new(1, "Pine Hollow", 6, true, [new(3, "Pine Spur", 2, false)]),
        new(2, "Eagle Ridge", 14, false),
    ];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null,
        ColumnAggregate aggregate = ColumnAggregate.Sum) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, _hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.Labels, Words)
                .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                    .Add(name => name.Sortable, true).Add(name => name.Filterable, true)
                    .Add(name => name.Editable, true))
                .AddChildContent<TbColumn<Hike>>(column => column.Add(length => length.Field, hike => hike.LengthKm)
                    .Add(length => length.Sortable, true).Add(length => length.Filterable, true)
                    .Add(length => length.Editable, true).Add(length => length.Aggregate, aggregate))
                .AddChildContent<TbColumn<Hike>>(column => column.Add(loop => loop.Field, hike => hike.Loop)
                    .Add(loop => loop.Filterable, true).Add(loop => loop.Groupable, true));
            configure?.Invoke(parameters);
        });

    private static IReadOnlyList<string> Spoken(IRenderedComponent<AnnouncerHost> host) =>
        [.. host.FindAll("[role=status] p").Select(p => p.TextContent)];

    [Fact]
    public async Task Labels_ShouldWordTheToolbarHeadersAndRowControls_WhenGiven()
    {
        RenderFragment<Hike> detail = hike => builder => builder.AddContent(0, hike.Name);
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.SelectionMode, SelectionMode.Multiple)
            .Add(grid => grid.DetailTemplate, detail).Add(grid => grid.OnMove, (SortableMove<Hike> _) => { })
            .Add(grid => grid.HideableColumns, true).Add(grid => grid.Exportable, true)
            .Add(grid => grid.PageSize, 1));

        var row = cut.Tb().Grid("Hikes").Row("Pine Hollow");
        Assert.Equal("Pick Pine Hollow", row.QuerySelector("[data-part=select] input")!.GetAttribute("aria-label"));
        Assert.Equal("More on Pine Hollow",
            row.QuerySelector(".tb-data-grid__detail-button")!.GetAttribute("aria-label"));
        Assert.Equal("Drag Pine Hollow",
            row.QuerySelector(".tb-data-grid__handle [aria-label]")!.GetAttribute("aria-label"));
        Assert.Contains("Arrows shift it", row.QuerySelector(".tb-data-grid__handle")!.OuterHtml);
        var headers = cut.FindAll("thead .tb-visually-hidden").Take(2).Select(header => header.TextContent.Trim());
        Assert.Equal(["Place", "More"], headers);
        Assert.Equal("Hikes pages", cut.Find("nav[aria-label]").GetAttribute("aria-label"));
        Assert.Contains("Cluster by", cut.Markup);
        Assert.Contains("Show fields", cut.Markup);
        await cut.Tb().Menu("Download").OpenAsync();
        Assert.Equal(["As CSV", "As XLSX", "As PDF file"],
            cut.FindAll(".tb-data-grid__export [role=menuitem]").Select(item => item.TextContent.Trim()));
        Assert.Equal("Menu for Name", cut.Find("th[data-column=Name] [data-part=column-menu] > button")
            .GetAttribute("aria-label"));
        var single = RenderHikes(parameters => parameters.Add(grid => grid.SelectionMode, SelectionMode.Single));
        Assert.Equal("Pick", single.Find("thead .tb-visually-hidden").TextContent.Trim());
    }

    [Fact]
    public async Task Labels_ShouldWordTheColumnMenuAndWhatItReadsOut_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.ReorderableColumns, true)
            .Add(grid => grid.ResizableColumns, true).Add(grid => grid.HideableColumns, true));
        var menu = cut.Tb().Menu("Menu for Name");

        await menu.OpenAsync();
        Assert.Equal(["Name left", "Name right", "Name wider please", "Name narrower please", "Even out", "Drop Name"],
            cut.FindAll("[role=menuitem]").Take(6).Select(item => item.TextContent.Trim()));
        await menu.ChooseAsync("Name right");
        await cut.Tb().Menu("Menu for Name").ChooseAsync("Drop Name");

        host.WaitForAssertion(() => Assert.Equal(["Name now 2 of 3", "Name gone"], Spoken(host)));
    }

    [Fact]
    public async Task Labels_ShouldWordTheSortAnnouncementsAndPositions_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Sorts,
            [new GridSort("Name", SortDirection.Ascending), new GridSort("LengthKm", SortDirection.Descending)]));

        Assert.Equal(["order 1/2 up", "order 2/2 down"],
            cut.FindAll("thead th .tb-visually-hidden").Select(text => text.TextContent.Trim()));
        var plain = RenderHikes();
        await plain.Tb().Grid("Hikes").SortAsync("Name");
        host.WaitForAssertion(() => Assert.Contains("Name going up", Spoken(host)));
        await plain.Tb().Grid("Hikes").SortAsync("Name", descending: true);
        host.WaitForAssertion(() => Assert.Contains("Name going down", Spoken(host)));
        await plain.Find("th[data-column=Name] [data-part=sort]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Contains("Name in no order", Spoken(host)));
    }

    [Fact]
    public async Task Labels_ShouldWordTheFilterMenuItsComparisonsAndTheCount_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));

        await cut.Tb().Grid("Hikes").FilterAsync("Length km", "from", "8");

        Assert.Equal("Narrow Length km, active",
            cut.Find("th[data-column=LengthKm] [data-part=filter-menu] > button").GetAttribute("aria-label"));
        Assert.Equal("Narrow Name", cut.Find("th[data-column=Name] [data-part=filter-menu] > button")
            .GetAttribute("aria-label"));
        host.WaitForAssertion(() => Assert.Contains("Showing 1", Spoken(host)));
        var menu = cut.Find("th[data-column=LengthKm] [data-part=filter-menu]").TextContent;
        Assert.All(new[] { "Rule", "Target", "Reset", "Use", "below" }, word => Assert.Contains(word, menu));
    }

    [Fact]
    public void Labels_ShouldWordAYesNoFiltersChoices_WhenGiven()
    {
        var cut = RenderHikes();

        Assert.Equal(["Sure", "Nah"], cut.Tb().Select("Narrow Loop").Options);
        Assert.Contains("Whatever", cut.Find("[data-part=filter][data-column=Loop]").OuterHtml);
    }

    [Fact]
    public void Labels_ShouldWordTheGroupsAndTotals_WhenGiven()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Loop"])
            .Add(grid => grid.GroupTotals, true));

        var headings = cut.FindAll("[data-part=group-toggle]").Select(heading => heading.TextContent.Trim()).ToList();
        Assert.Equal(2, headings.Count);
        Assert.All(headings, heading => Assert.Matches(@"^Loop ~ .+\s+\[1\]$", heading));
        Assert.Contains("Subtotal Loop ~", cut.Markup);
        Assert.Contains("Grand", cut.Markup);
        Assert.Contains("Added", cut.Markup);
    }

    [Fact]
    public async Task Labels_ShouldWordTheGroupsAndTotalsInAPdf_WhenGiven()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Loop"])
            .Add(grid => grid.GroupTotals, true));

        var text = PdfText.TextOf(await cut.InvokeAsync(() => cut.Instance.ExportPdfAsync())).Single();

        Assert.Equal(2, text.Count(run =>
            run.StartsWith("Loop ~ ", StringComparison.Ordinal) && run.EndsWith(" · [1]", StringComparison.Ordinal)));
        Assert.Equal(2, text.Count(run => run.StartsWith("Subtotal Loop ~ ", StringComparison.Ordinal)));
        Assert.Contains("Grand", text);
        Assert.Contains(text, run => run.StartsWith("Added ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ColumnAggregate.Average, "Mean")]
    [InlineData(ColumnAggregate.Count, "Tally")]
    [InlineData(ColumnAggregate.Min, "Least")]
    [InlineData(ColumnAggregate.Max, "Most")]
    public void Labels_ShouldNameEachKindOfTotal_WhenGiven(ColumnAggregate aggregate, string name)
    {
        var cut = RenderHikes(aggregate: aggregate);

        Assert.Contains(name, cut.Find("tfoot").TextContent);
    }

    [Fact]
    public async Task Labels_ShouldWordTheRowEditButtonsAndMessages_WhenGiven()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.EditMode, GridEditMode.Row)
            .Add(grid => grid.OnRowEdit, (GridRowEdit<Hike> _) => { }));
        await cut.Find("tr[data-key='1'] td[data-column=LengthKm]").DoubleClickAsync(new());

        Assert.Equal([("Keep Pine Hollow", "Keep"), ("Undo Pine Hollow", "Undo")],
            cut.FindAll("[data-part=row-actions] button")
                .Select(button => (button.GetAttribute("aria-label"), button.TextContent.Trim())));
    }

    [Theory]
    [InlineData("6.5", "Length km won't take that")]
    [InlineData("", "Length km needs a value")]
    public async Task Labels_ShouldWordACellEditsMessages_WhenGiven(string typed, string message)
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.OnCellEdit, (GridCellEdit<Hike> _) => { }));
        await cut.Find("tr[data-key='1'] td[data-column=LengthKm]").DoubleClickAsync(new());

        await cut.Tb().NumberField("Length km").EnterAsync(typed);
        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Enter" });

        Assert.Contains(message, cut.Find("[data-part=editor]").TextContent);
    }

    [Fact]
    public async Task Labels_ShouldWordTheBatchBarAndItsAnnouncement_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.EditMode, GridEditMode.Batch)
            .Add(grid => grid.OnBatchSave, (GridBatchEdit<Hike> _) => { }));
        await cut.Find("tr[data-key='1'] td[data-column=Name]").DoubleClickAsync(new());
        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Pine loop" });
        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Enter" });

        var cell = cut.Find("tr[data-key='1'] td[data-column=Name]");
        Assert.Equal("Edited", cut.Find($"#{cell.GetAttribute("aria-describedby")}").TextContent);
        Assert.Equal("Pending: 1", cut.Find("[data-part=change-count]").TextContent.Trim());
        Assert.Equal(["Store all", "Drop all"],
            cut.FindAll("[data-part=batch-bar] button").Select(button => button.TextContent.Trim()));
        await cut.Tb().Grid("Hikes").SaveChangesAsync();

        host.WaitForAssertion(() => Assert.Contains("All stored", Spoken(host)));
    }

    [Fact]
    public void Labels_ShouldWordTheYesOrNoCells_WhenGiven()
    {
        var cut = RenderHikes();

        Assert.Equal(["Sure", "Nah"], cut.FindAll("tr[data-part=row] td[data-column=Loop]")
            .Select(cell => cell.TextContent.Trim()));
    }

    [Fact]
    public async Task Labels_ShouldWordATreeRowsToggleAndAMovedRow_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var tree = RenderHikes(parameters => parameters.Add(grid => grid.ChildrenSelector, hike => hike.Spurs));
        Assert.Equal("Spurs of Pine Hollow",
            tree.Find("tr[data-key='1'] [data-part=tree-toggle]").GetAttribute("aria-label"));

        var cut = RenderHikes(parameters => parameters.Add(grid => grid.OnMove, (SortableMove<Hike> _) => { }));
        await cut.Tb().Grid("Hikes").MoveRowAsync("Pine Hollow", 1);

        host.WaitForAssertion(() => Assert.Contains("Pine Hollow sits at 2 of 2", Spoken(host)));
    }
}
