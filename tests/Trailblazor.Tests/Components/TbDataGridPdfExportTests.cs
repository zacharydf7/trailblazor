using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridPdfExportTests : BunitContext
{
    public TbDataGridPdfExportTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, string Region, double LengthKm);

    private static readonly Hike[] Hikes =
    [
        new(1, "Pine Hollow", "North ridge", 6.4),
        new(2, "Eagle Ridge", "North ridge", 14.2),
        new(3, "Cedar Flats", "River valley", 3.1),
    ];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(IReadOnlyList<Hike>? items = null,
        IReadOnlyList<string>? groups = null, bool totals = false, bool groupTotals = false,
        ColumnAggregate aggregate = ColumnAggregate.Sum, string? fileName = null) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, items ?? Hikes)
            .Add(grid => grid.ExportFileName, fileName)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.PageSize, 2).Add(grid => grid.Groups, groups ?? [])
            .Add(grid => grid.GroupTotals, groupTotals)
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(region => region.Field, hike => hike.Region)
                .Add(region => region.Groupable, true))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(length => length.Field, hike => hike.LengthKm)
                .Add(length => length.Format, "N1")
                .Add(length => length.Aggregate, totals ? aggregate : ColumnAggregate.None)));

    // The cells' text follows the culture, so these tests write it in English wherever they run.
    private static async Task<byte[]> PdfAsync(IRenderedComponent<TbDataGrid<Hike>> cut)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            return await cut.InvokeAsync(() => cut.Instance.ExportPdfAsync());
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldPointTheCrossReferenceAtEachObject_WhenCalled()
    {
        var pdf = await PdfAsync(RenderHikes());

        var text = Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF-1.4\n", text);
        Assert.EndsWith("%%EOF\n", text);
        var xref = int.Parse(Regex.Match(text, @"startxref\n(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.StartsWith("xref\n", text[xref..]);
        var offsets = Regex.Matches(text[xref..], @"(\d{10}) 00000 n \n").Select(match => int.Parse(match.Groups[1].Value,
            CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(7, offsets.Count);
        Assert.All(offsets.Select((offset, at) => (offset, at)), entry =>
            Assert.StartsWith($"{entry.at + 1} 0 obj\n", text[entry.offset..]));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldWriteTheNameHeadingsAndEveryPagesRows_WhenTheRowsAreShown()
    {
        var pdf = await PdfAsync(RenderHikes());

        Assert.Equal([["Hikes", "Name", "Region", "Length km", "Pine Hollow", "North ridge", "6.4", "Eagle Ridge",
            "North ridge", "14.2", "Cedar Flats", "River valley", "3.1", "1 / 1"]], PdfText.TextOf(pdf));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldShowTheNameAndHeadingsInBold_WhenCalled()
    {
        var page = PdfText.PagesOf(await PdfAsync(RenderHikes()))[0];

        Assert.Equal(["Hikes", "Name", "Region", "Length km"], page.Where(run => run.Bold).Select(run => run.Text));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldLineNumbersUpOnTheRight_WhenAColumnHoldsThem()
    {
        var page = PdfText.PagesOf(await PdfAsync(RenderHikes()))[0];

        var x = page.GroupBy(run => run.Text).ToDictionary(runs => runs.Key, runs => runs.First().X);
        Assert.True(x["14.2"] < x["6.4"]);
        Assert.Equal(x["Pine Hollow"], x["Eagle Ridge"]);
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldPutEachGroupUnderItsHeadingWithItsTotals_WhenTheGridIsGrouped()
    {
        var pdf = await PdfAsync(RenderHikes(groups: ["Region"], totals: true, groupTotals: true));

        Assert.Equal(["Hikes", "Name", "Region", "Length km", "Region: North ridge · Rows: 2", "Pine Hollow",
            "North ridge", "6.4", "Eagle Ridge", "North ridge", "14.2", "Totals for Region: North ridge", "Sum 20.6",
            "Region: River valley · Rows: 1", "Cedar Flats", "River valley", "3.1", "Totals for Region: River valley",
            "Sum 3.1", "Totals", "Sum 23.7", "1 / 1"], PdfText.TextOf(pdf).Single());
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldEndWithTheTotalsInBold_WhenAColumnHasAnAggregate()
    {
        var page = PdfText.PagesOf(await PdfAsync(RenderHikes(totals: true)))[0];

        Assert.Equal(["Totals", "Sum 23.7"], page.SkipLast(1).TakeLast(2).Select(run => run.Text));
        Assert.All(page.SkipLast(1).TakeLast(2), run => Assert.True(run.Bold));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldNameEachTotalsAggregate_WhenItIsNotASum()
    {
        var page = PdfText.TextOf(await PdfAsync(RenderHikes(totals: true, aggregate: ColumnAggregate.Average)))[0];

        Assert.Equal(["Totals", "Average 7.9"], page.SkipLast(1).TakeLast(2));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldPrintTheExportsNameAsGiven_WhenItHoldsCharactersAFileNameCannot()
    {
        var page = PdfText.TextOf(await PdfAsync(RenderHikes(fileName: "Hikes: May")))[0];

        Assert.Equal("Hikes: May", page[0]);
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldWriteTheWindows1252Extras_WhenTheTextHoldsThem()
    {
        var page = PdfText.TextOf(await PdfAsync(RenderHikes([new(1, "“Quoted” – €5", "North ridge", 5)])))[0];

        var codes = page.Single(text => text.Length == 13).Select(letter => ((int)letter).ToString("X2"));
        Assert.Equal("9351756F746564942096208035", string.Concat(codes));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldRepeatTheNameAndHeadingsOnEachPage_WhenTheRowsFillAPage()
    {
        Hike[] many = [.. Enumerable.Range(1, 70).Select(at => new Hike(at, $"Trail {at}", "North ridge", at))];

        var pages = PdfText.TextOf(await PdfAsync(RenderHikes(many)));

        Assert.Equal(3, pages.Count);
        Assert.All(pages, page => Assert.Equal(["Hikes", "Name", "Region", "Length km"], page[..4]));
        Assert.Equal(["1 / 3", "2 / 3", "3 / 3"], pages.Select(page => page[^1]));
        Assert.Equal(many.Select(hike => hike.Name), pages.SelectMany(page => page.Where(text => text.StartsWith("Trail "))));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldCutTextShortWithAnEllipsis_WhenItIsTooWideForItsColumn()
    {
        var name = string.Concat(Enumerable.Repeat("Switchback ", 40));

        var page = PdfText.TextOf(await PdfAsync(RenderHikes([new(1, name, "North ridge", 5)])))[0];

        // The widest column is 240pt, and the ellipsis takes its own width out of what's left for the text.
        Assert.Equal(string.Concat(Enumerable.Repeat("Switchback ", 4)) + "Switch…",
            page.Single(text => text.StartsWith("Switchback")));
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldShowAQuestionMark_WhenALetterIsOutsideWindows1252()
    {
        var page = PdfText.TextOf(await PdfAsync(RenderHikes([new(1, "Café 富士", "Côte\0Nord", 5)])))[0];

        Assert.Contains("Café ??", page);
        Assert.Contains("Côte?Nord", page);
    }
}
