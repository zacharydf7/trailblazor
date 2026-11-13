using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridExportTests : BunitContext
{
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public TbDataGridExportTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, double LengthKm, bool Done, DateOnly Walked);

    private static readonly Hike[] Hikes =
    [
        new(1, "Pine Hollow", 6.4, true, new(2026, 5, 2)),
        new(2, "Eagle Ridge", 14.2, false, new(2026, 6, 14)),
        new(3, "Cedar Flats", 3.1, true, new(2026, 7, 9)),
    ];

    private FakeBrowserService Browser => Services.GetRequiredService<FakeBrowserService>();

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null,
        IReadOnlyList<Hike>? items = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, items ?? Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.PageSize, 2)
                .Add(grid => grid.ChildContent, (RenderFragment)Columns);
            configure?.Invoke(parameters);
        });

    private static void Columns(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), (System.Linq.Expressions.Expression<Func<Hike, object?>>)
            (hike => hike.Name));
        builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(3);
        builder.AddAttribute(4, nameof(TbColumn<Hike>.Field), (System.Linq.Expressions.Expression<Func<Hike, object?>>)
            (hike => hike.LengthKm));
        builder.AddAttribute(5, nameof(TbColumn<Hike>.Format), "N1");
        builder.AddAttribute(6, nameof(TbColumn<Hike>.Sortable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(7);
        builder.AddAttribute(8, nameof(TbColumn<Hike>.Field), (System.Linq.Expressions.Expression<Func<Hike, object?>>)
            (hike => hike.Done));
        builder.AddAttribute(15, nameof(TbColumn<Hike>.Groupable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(9);
        builder.AddAttribute(10, nameof(TbColumn<Hike>.Field), (System.Linq.Expressions.Expression<Func<Hike, object?>>)
            (hike => hike.Walked));
        builder.AddAttribute(11, nameof(TbColumn<Hike>.Format), "yyyy-MM-dd");
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(12);
        builder.AddAttribute(13, nameof(TbColumn<Hike>.Title), "Actions");
        builder.AddAttribute(14, nameof(TbColumn<Hike>.CellTemplate), (RenderFragment<Hike>)(_ => _ => { }));
        builder.CloseComponent();
    }

    private static string[] Lines(string csv) => csv.Split("\r\n");

    private static Task<string> CsvAsync(IRenderedComponent<TbDataGrid<Hike>> cut,
        GridExportRows rows = GridExportRows.Shown) =>
        InEnglishAsync(() => cut.InvokeAsync(() => cut.Instance.ExportCsvAsync(rows)));

    private static Task<byte[]> ExcelAsync(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        InEnglishAsync(() => cut.InvokeAsync(() => cut.Instance.ExportExcelAsync()));

    // The cells' text follows the culture, so these tests write it in English wherever they run.
    private static async Task<T> InEnglishAsync<T>(Func<Task<T>> export)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            return await export();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    private static XDocument PartOf(byte[] workbook, string part)
    {
        using var zip = new ZipArchive(new MemoryStream(workbook));
        return XDocument.Load(zip.GetEntry(part)!.Open());
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldWriteTheFilteredRowsOfEveryPageInTheirSort_WhenShown()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.HideableColumns, true)
            .Add(grid => grid.State, new GridState
            {
                Sorts = [new GridSort("LengthKm", SortDirection.Ascending)],
                Filters = [new GridFilter("Name", FilterOperator.Contains, "a")],
                HiddenColumns = ["Done"],
                PageSize = 1,
            }));

        var csv = await CsvAsync(cut);

        Assert.Equal(["Name,Length km,Walked", "Cedar Flats,3.1,2026-07-09", "Eagle Ridge,14.2,2026-06-14", ""],
            Lines(csv));
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldWriteEveryRowAndColumn_WhenAskedForAll()
    {
        var cut = RenderHikes(parameters => parameters
            .Add(grid => grid.Filters, [new GridFilter("Name", FilterOperator.Contains, "Pine")])
            .Add(grid => grid.HideableColumns, true)
            .Add(grid => grid.State, new GridState { HiddenColumns = ["Done"] }));

        var csv = await CsvAsync(cut, GridExportRows.All);

        Assert.Equal("Name,Length km,Done,Walked", Lines(csv)[0]);
        Assert.Equal(5, Lines(csv).Length);
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldQuoteFieldsAndKeepFormulasFromRunning_WhenTheTextNeedsIt()
    {
        var cut = RenderHikes(items:
        [
            new(1, "Ridge, north", -1.5, true, default), new(2, "The \"Gate\"", 2, true, default),
            new(3, "=HYPERLINK(\"x\")", 3, true, default), new(4, "Two\nlines", 4, true, default),
        ]);

        var csv = await CsvAsync(cut);

        Assert.StartsWith("\"Ridge, north\",-1.5,", Lines(csv)[1]);
        Assert.StartsWith("\"The \"\"Gate\"\"\",2.0,", Lines(csv)[2]);
        Assert.StartsWith("\"'=HYPERLINK(\"\"x\"\")\",3.0,", Lines(csv)[3]);
        Assert.StartsWith("\"Two\nlines\",4.0,", Lines(csv)[4]);
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldAskTheProviderForEveryFilteredRow_WhenTheGridHasADataProvider()
    {
        GridRequest? asked = null;
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.PageSize, 2)
            .Add(grid => grid.Filters, [new GridFilter("Name", FilterOperator.Contains, "e")])
            .Add(grid => grid.DataProvider, (request, _) =>
            {
                asked = request;
                return Task.FromResult(new GridResult<Hike>(request.Count is null ? Hikes : Hikes[..2], 3));
            })
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

        var csv = await CsvAsync(cut);

        Assert.Equal((0, (int?)null), (asked!.StartIndex, asked.Count));
        Assert.Single(asked.Filters);
        Assert.Equal(5, Lines(csv).Length);
    }

    [Fact]
    public async Task ExportExcelAsync_ShouldKeepNumbersDatesAndYesOrNoAsValues_WhenWritingTheWorkbook()
    {
        var cut = RenderHikes();

        var workbook = await ExcelAsync(cut);

        using var zip = new ZipArchive(new MemoryStream(workbook));
        var sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var rows = sheet.Descendants(Sheet + "row").ToList();
        Assert.Equal(4, rows.Count);
        var header = rows[0].Elements(Sheet + "c").ToList();
        Assert.Equal(["Name", "Length km", "Done", "Walked"], header.Select(cell => cell.Value));
        Assert.All(header, cell => Assert.Equal("1", (string?)cell.Attribute("s")));
        var cells = rows[1].Elements(Sheet + "c").ToList();
        Assert.Equal(("inlineStr", "Pine Hollow"), ((string?)cells[0].Attribute("t"), cells[0].Value));
        Assert.Equal((null, "6.4"), ((string?)cells[1].Attribute("t"), cells[1].Value));
        Assert.Equal(("b", "1"), ((string?)cells[2].Attribute("t"), cells[2].Value));
        Assert.Equal(("2", "46144"), ((string?)cells[3].Attribute("s"), cells[3].Value));
        Assert.Equal("frozen", (string?)sheet.Descendants(Sheet + "pane").Single().Attribute("state"));
        var book = XDocument.Load(zip.GetEntry("xl/workbook.xml")!.Open());
        Assert.Equal("Hikes", (string?)book.Descendants(Sheet + "sheet").Single().Attribute("name"));
    }

    [Fact]
    public async Task ExportExcelAsync_ShouldKeepEmoji_WhenTheTextOrTheSheetNameHasThem()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.ExportFileName, "Hikes 🌲"),
            items: [new(1, "Pine 🌲 Hollow", 6.4, true, new(2026, 5, 2))]);

        var workbook = await ExcelAsync(cut);

        var sheet = PartOf(workbook, "xl/worksheets/sheet1.xml");
        Assert.Equal("Pine 🌲 Hollow", sheet.Descendants(Sheet + "row").ElementAt(1).Elements().First().Value);
        var book = PartOf(workbook, "xl/workbook.xml");
        Assert.Equal("Hikes 🌲", (string?)book.Descendants(Sheet + "sheet").Single().Attribute("name"));
    }

    [Fact]
    public async Task ExportExcelAsync_ShouldWriteADateAsText_WhenExcelCantHoldIt()
    {
        var cut = RenderHikes(items: [new(1, "Pine Hollow", 6.4, true, default)]);

        var workbook = await ExcelAsync(cut);

        var walked = PartOf(workbook, "xl/worksheets/sheet1.xml").Descendants(Sheet + "row").ElementAt(1)
            .Elements(Sheet + "c").ElementAt(3);
        Assert.Equal(("inlineStr", "0001-01-01"), ((string?)walked.Attribute("t"), walked.Value));
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldListTheRowsGroupByGroup_WhenTheGridIsGrouped()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Done"]));

        var csv = await CsvAsync(cut);

        Assert.Equal(["Eagle Ridge", "Pine Hollow", "Cedar Flats"],
            Lines(csv)[1..^1].Select(line => line.Split(',')[0]));
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldAskTheProviderForEveryRowInItsSort_WhenAskedForAll()
    {
        GridRequest? asked = null;
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.State, new GridState
            {
                Sorts = [new GridSort("LengthKm", SortDirection.Ascending)],
                Filters = [new GridFilter("Name", FilterOperator.Contains, "e")],
            })
            .Add(grid => grid.DataProvider, (request, _) =>
            {
                asked = request;
                return Task.FromResult(new GridResult<Hike>(Hikes, 3));
            })
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

        await CsvAsync(cut, GridExportRows.All);

        Assert.Empty(asked!.Filters);
        Assert.Equal([new GridSort("LengthKm", SortDirection.Ascending)], asked.Sorts);
    }

    [Fact]
    public async Task ExportMenu_ShouldReportTheFailureAndDownloadNothing_WhenTheProviderFails()
    {
        Exception? failed = null;
        var loads = 0;
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.Exportable, true)
            .Add(grid => grid.OnLoadFailed, (Exception exception) => failed = exception)
            .Add(grid => grid.DataProvider, (_, _) => ++loads == 1
                ? Task.FromResult(new GridResult<Hike>(Hikes, 3))
                : throw new InvalidOperationException("The trail service is down."))
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

        await cut.Tb().Menu("Export").ChooseAsync("CSV");

        Assert.Equal("The trail service is down.", failed?.Message);
        Assert.Empty(Browser.Downloads);
        Assert.Equal(3, cut.FindAll("tr[data-part=row]").Count);
    }

    [Fact]
    public async Task ExportMenu_ShouldReportTheFailure_WhenTheProvidersOwnTimeoutCancelsTheLoad()
    {
        Exception? failed = null;
        var loads = 0;
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.Exportable, true)
            .Add(grid => grid.OnLoadFailed, (Exception exception) => failed = exception)
            .Add(grid => grid.DataProvider, (_, _) => ++loads == 1
                ? Task.FromResult(new GridResult<Hike>(Hikes, 3))
                : throw new TaskCanceledException("The request timed out."))
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

        await cut.Tb().Menu("Export").ChooseAsync("CSV");

        Assert.IsType<TaskCanceledException>(failed);
        Assert.Empty(Browser.Downloads);
    }

    [Fact]
    public async Task ExportMenu_ShouldReportNothing_WhenTheGridGoesAwayWhileItLoads()
    {
        Exception? failed = null;
        var loads = 0;
        var load = new TaskCompletionSource<GridResult<Hike>>();
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.Exportable, true)
            .Add(grid => grid.OnLoadFailed, (Exception exception) => failed = exception)
            .Add(grid => grid.DataProvider, (_, _) => ++loads == 1
                ? Task.FromResult(new GridResult<Hike>(Hikes, 3)) : load.Task)
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));

        var choosing = cut.Tb().Menu("Export").ChooseAsync("CSV");
        await DisposeComponentsAsync();
        load.SetException(new InvalidOperationException("The trail service hung up."));
        await choosing;

        Assert.Null(failed);
        Assert.Empty(Browser.Downloads);
    }

    [Theory]
    [InlineData("History", "Sheet1")]
    [InlineData("A list of thirty characters ab'cd", "A list of thirty characters ab")]
    public async Task ExportExcelAsync_ShouldNameTheSheetAsExcelAllows_WhenTheGridsNameBreaksItsRules(string name,
        string expected)
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.ExportFileName, name));

        var workbook = await ExcelAsync(cut);

        var book = PartOf(workbook, "xl/workbook.xml");
        Assert.Equal(expected, (string?)book.Descendants(Sheet + "sheet").Single().Attribute("name"));
    }

    [Fact]
    public async Task ExportMenu_ShouldDownloadEachFile_WhenItsItemIsChosen()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Exportable, true)
            .Add(grid => grid.ExportFileName, "Hikes: May").Add(grid => grid.TelemetryName, "hikes"));

        await cut.Tb().Menu("Export").ChooseAsync("CSV");
        await cut.Tb().Menu("Export").ChooseAsync("Excel");
        await cut.Tb().Menu("Export").ChooseAsync("PDF");

        var (csv, excel, pdf) = (Browser.Downloads[0], Browser.Downloads[1], Browser.Downloads[2]);
        Assert.Equal(("Hikes- May.csv", "text/csv"), (csv.FileName, csv.ContentType));
        Assert.Equal([0xEF, 0xBB, 0xBF], csv.Content[..3]);
        Assert.StartsWith("\uFEFFName,Length km,Done,Walked", csv.Text);
        Assert.Equal("Hikes- May.xlsx", excel.FileName);
        Assert.Equal("PK"u8.ToArray(), excel.Content[..2]);
        Assert.Equal(("Hikes- May.pdf", "application/pdf"), (pdf.FileName, pdf.ContentType));
        Assert.Equal("%PDF"u8.ToArray(), pdf.Content[..4]);
        Assert.Equal(["export_csv", "export_excel", "export_pdf"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }
}
