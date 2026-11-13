using System.Text;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

public partial class TbDataGrid<TItem>
{
    private const string CsvType = "text/csv";
    private const string ExcelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly CancellationTokenSource _exporting = new();

    /// <summary>Adds an Export menu to the bar above the rows. It downloads the rows the filters leave, from every
    /// page, as a CSV file or an Excel workbook with the content <see cref="ExportCsvAsync"/> and
    /// <see cref="ExportExcelAsync"/> return. A <see cref="DataProvider"/> that fails raises
    /// <see cref="OnLoadFailed"/> and leaves the rows shown as they were.</summary>
    [Parameter]
    public bool Exportable { get; set; }

    /// <summary>The name, without its extension, of the file the Export menu downloads. Defaults to the grid's
    /// <see cref="AriaLabel"/>, or "Export" without one. Characters a file name can't hold become hyphens.</summary>
    [Parameter]
    public string? ExportFileName { get; set; }

    /// <summary>The rows as CSV text, one line for the headings and one for each row, with each cell's text as the
    /// grid shows it. Only columns with a <see cref="TbColumn{TItem}.Field"/> are exported, and text a spreadsheet
    /// would run as a formula, such as "=SUM(A1:A9)", gets an apostrophe in front. With
    /// <see cref="GridExportRows.Shown"/>, rows grouped by <see cref="Groups"/> come group by group, as they show. A
    /// grid with a <see cref="DataProvider"/> asks it for every row.</summary>
    /// <param name="rows">Whether to export the rows and columns as shown, or every one.</param>
    public async Task<string> ExportCsvAsync(GridExportRows rows = GridExportRows.Shown)
    {
        var (columns, items) = await ExportedAsync(rows);
        return CsvExport.Write(columns.Select(column => column.Heading), CellsOf(columns, items));
    }

    /// <summary>The rows as an Excel workbook (.xlsx) of one sheet, with a bold header row that stays in view. Numbers,
    /// dates and yes-or-no values stay values a spreadsheet can sum and sort; other cells hold the grid's text. Only
    /// columns with a <see cref="TbColumn{TItem}.Field"/> are exported, shown rows grouped by <see cref="Groups"/>
    /// come group by group, and a grid with a <see cref="DataProvider"/> asks it for every row.</summary>
    /// <param name="rows">Whether to export the rows and columns as shown, or every one.</param>
    public async Task<byte[]> ExportExcelAsync(GridExportRows rows = GridExportRows.Shown)
    {
        var (columns, items) = await ExportedAsync(rows);
        return XlsxExport.Write(ExportName, [.. columns.Select(column => column.Heading)], CellsOf(columns, items));
    }

    // A fixed set, not Path.GetInvalidFileNameChars(), which follows the server's system rather than the user's.
    private string ExportName => string.Concat((ExportFileName ?? AriaLabel ?? "Export")
        .Select(letter => char.IsControl(letter) || "\\/:*?\"<>|".Contains(letter) ? '-' : letter));

    private static IEnumerable<IEnumerable<ExportCell>> CellsOf(List<TbColumn<TItem>> columns,
        IReadOnlyList<TItem> items) =>
        items.Select(item => columns.Select(column =>
        {
            var value = column.ValueOf(item);
            return new ExportCell(value, column.TextOf(value));
        }));

    private async Task<(List<TbColumn<TItem>> Columns, IReadOnlyList<TItem> Items)> ExportedAsync(GridExportRows rows)
    {
        var shown = rows == GridExportRows.Shown;
        List<TbColumn<TItem>> columns =
            [.. (shown ? _shownColumns : _arrangedColumns).Where(column => column.HasField)];
        if (ChildrenSelector is not null)
        {
            return (columns, [.. Flattened(TreeOf(Matcher(shown), []))]);
        }

        if (DataProvider is null)
        {
            List<TItem> arranged = [.. Arranged(filtered: shown)];
            return (columns, shown ? [.. GroupedOrder(arranged, AppliedGroups.ToList(), 0)] : arranged);
        }

        TypeFilters();
        var request = new GridRequest(0, null, [.. _sorts]) { Filters = shown ? [.. _filters] : [] };
        return (columns, (await DataProvider(request, _exporting.Token)).Items);
    }

    // A provider's failure is the grid's to report, as when it loads a page, but the rows shown stay as they were.
    private async Task ExportFromMenuAsync(bool excel)
    {
        Telemetry.Used(TelemetryName, excel ? "export_excel" : "export_csv");
        byte[] content;
        try
        {
            content = excel ? await ExportExcelAsync() : Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(await ExportCsvAsync())).ToArray();
        }
        catch (Exception) when (_exporting.IsCancellationRequested)
        {
            // The grid went away while it was loading the rows, so whatever the provider threw goes unreported.
            return;
        }
        catch (Exception exception)
        {
            Announcer.Polite(LoadFailedLabel);
            await OnLoadFailed.InvokeAsync(exception);
            return;
        }

        using var stream = new MemoryStream(content);
        await Browser.DownloadAsync(stream, $"{ExportName}.{(excel ? "xlsx" : "csv")}", excel ? ExcelType : CsvType);
    }
}
