using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbPieChart</c>, found by its <c>Title</c> or <c>AriaLabel</c>, or the only one in the scope.
/// </summary>
public sealed class TbPieChartLocator(ILocator scope, string? name)
{
    // Where the slice of the item at this place is in the plot, as "x,y" in shares of its box, or an empty string
    // when the item has no slice.
    private const string Spot = """
        (plot, place) => {
            const point = plot.querySelector('.tb-chart__marks [data-series]')?.dataset.points.split(' ')[place];
            if (!point || point === '-') {
                return '';
            }

            const [x, y] = point.split(',').map(Number);
            const mirrored = getComputedStyle(plot).direction === 'rtl';
            return `${(mirrored ? 10000 - x : x) / 10000},${y / 10000}`;
        }
        """;

    /// <summary>The chart's figure.</summary>
    public ILocator Chart => Scope.ByRole(scope, AriaRole.Figure, name).And(scope.Locator("figure.tb-pie"));

    private ILocator Plot => Chart.Locator(":scope > .tb-pie__body > [data-part=plot]");

    /// <summary>The labels of the chart's items, in order, those that have no slice too.</summary>
    public async Task<IReadOnlyList<string>> SlicesAsync()
    {
        await Chart.WaitForAsync();
        return await Chart.EvaluateAsync<string[]>("chart => [...chart.querySelectorAll("
            + "'[data-part=table] tbody th')].map(heading => heading.textContent)");
    }

    /// <summary>This item's value, as the chart's data table writes it, or an empty string where it has none. A
    /// label the chart doesn't have throws.</summary>
    public async Task<string> ValueAsync(string slice) => await CellAsync(slice, 0);

    /// <summary>This item's share of the total, as the chart's data table writes it, such as "60%", or an empty
    /// string where the item has no slice. A label the chart doesn't have throws.</summary>
    public async Task<string> ShareAsync(string slice) => await CellAsync(slice, 1);

    /// <summary>Clicks this item's slice, and waits for the app's <c>OnSliceClick</c> to run. A label the chart
    /// doesn't have throws, and so do an item with no slice and a chart with no <c>OnSliceClick</c>.</summary>
    public async Task ClickSliceAsync(string slice)
    {
        await Assertions.Expect(Plot).ToHaveAttributeAsync("data-ready", "");
        if (await Plot.GetAttributeAsync("data-activates") is null)
        {
            throw new InvalidOperationException("The chart has no OnSliceClick, so a click on a slice does nothing.");
        }

        var spot = await Plot.EvaluateAsync<string>(Spot, await RowAsync(slice));
        if (spot.Length == 0)
        {
            throw new InvalidOperationException($"\"{slice}\" has no slice.");
        }

        var shares = spot.Split(',').Select(share => float.Parse(share, CultureInfo.InvariantCulture)).ToArray();
        var box = await Plot.BoundingBoxAsync() ?? throw new InvalidOperationException("The chart isn't shown.");
        await Commit.CountedAsync(Plot, "data-commits", async () =>
        {
            await Plot.ClickAsync(new() { Position = new() { X = box.Width * shares[0], Y = box.Height * shares[1] } });
            return true;
        });
    }

    // The text of one of the two cells the data table has for this item: its value, then its share.
    private async Task<string> CellAsync(string slice, int column) =>
        await Chart.Locator("[data-part=table] tbody tr").Nth(await RowAsync(slice)).Locator("td").Nth(column)
            .EvaluateAsync<string>("cell => cell.textContent");

    // Which of the data table's rows, and so which of the items, has this label.
    private async Task<int> RowAsync(string slice) => (await SlicesAsync()).ToList().IndexOf(slice) is >= 0 and var row
        ? row : throw new InvalidOperationException($"The chart has no item labeled \"{slice}\".");
}
