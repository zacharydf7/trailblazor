using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridExportTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static async Task<IDownload> ExportAsync(IPage page, string item) =>
        await page.RunAndWaitForDownloadAsync(() => page.Tb().Menu("Export").ChooseAsync(item));

    private static async Task<byte[]> ContentOf(IDownload download)
    {
        await using var stream = await download.CreateReadStreamAsync();
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ExportMenu_ShouldDownloadTheFilteredRowsOfEveryPage_WhenCsvIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        await page.Tb().Grid("Trail list").FilterAsync("Region", "valley");

        var download = await ExportAsync(page, "CSV");

        Assert.Equal("Trail list.csv", download.SuggestedFilename);
        var lines = System.Text.Encoding.UTF8.GetString(await ContentOf(download)).TrimStart('\uFEFF').Split("\r\n");
        Assert.Equal("Name,Region,Length (km),Last hiked", lines[0]);
        Assert.Equal(["Cedar Flats", "Fern Gully"], lines[1..^1].Select(line => line.Split(',')[0]));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ExportMenu_ShouldDownloadAWorkbook_WhenExcelIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");

        var download = await ExportAsync(page, "Excel");

        Assert.Equal("Trail list.xlsx", download.SuggestedFilename);
        Assert.Equal("PK"u8.ToArray(), (await ContentOf(download))[..2]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ExportMenu_ShouldDownloadAPdf_WhenPdfIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");

        var download = await ExportAsync(page, "PDF");

        Assert.Equal("Trail list.pdf", download.SuggestedFilename);
        Assert.Equal("%PDF-1.4"u8.ToArray(), (await ContentOf(download))[..8]);
    }
}
