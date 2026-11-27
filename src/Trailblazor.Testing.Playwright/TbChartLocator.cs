using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbChart</c>, found by its <c>Title</c> or <c>AriaLabel</c>, or the only one in the scope.</summary>
public sealed class TbChartLocator(ILocator scope, string? name)
{
    // Whether the plot draws this series.
    private const string Drawn = """
        (chart, series) => [...chart.querySelectorAll('.tb-chart__marks [data-series]')]
            .some(marks => marks.dataset.series === series)
        """;

    // Where a series' point at a place is in the plot, as "x,y" in shares of its box, or what the chart lacks:
    // "series", "place" or, where the series has nothing at the place, "value".
    private const string Spot = """
        (plot, [series, place]) => {
            const at = (plot.dataset.places ?? '').split('\n').indexOf(place);
            const marks = [...plot.querySelectorAll('.tb-chart__marks [data-series]')]
                .find(one => one.dataset.series === series);
            if (at < 0 || !marks) {
                return marks ? 'place' : 'series';
            }

            const point = marks.dataset.points.split(' ')[at];
            if (point === '-') {
                return 'value';
            }

            const [x, y] = point.split(',').map(Number);
            const mirrored = getComputedStyle(plot).direction === 'rtl';
            return `${(mirrored ? 10000 - x : x) / 10000},${y / 10000}`;
        }
        """;

    /// <summary>The chart's figure.</summary>
    public ILocator Chart => Scope.ByRole(scope, AriaRole.Figure, name).And(scope.Locator("figure.tb-chart"));

    private ILocator Plot => Chart.Locator(":scope > .tb-chart__body > [data-part=plot]");

    /// <summary>The names of the chart's series, in the order its legend and data table list them.</summary>
    public async Task<IReadOnlyList<string>> SeriesAsync()
    {
        await Chart.WaitForAsync();
        return await Chart.EvaluateAsync<string[]>("chart => [...chart.querySelectorAll("
            + "'[data-part=table] thead tr > :not(:first-child)')].map(heading => heading.textContent)");
    }

    /// <summary>What the chart's items are called along it, in order.</summary>
    public async Task<IReadOnlyList<string>> PlacesAsync()
    {
        await Chart.WaitForAsync();
        return await Chart.EvaluateAsync<string[]>("chart => [...chart.querySelectorAll("
            + "'[data-part=table] tbody th')].map(heading => heading.textContent)");
    }

    /// <summary>What the chart writes for each of its reference lines and bands, such as "Target: 60", in the order
    /// the chart lists them under its data table. A chart without any gives an empty list.</summary>
    public async Task<IReadOnlyList<string>> ReferencesAsync()
    {
        await Chart.WaitForAsync();
        return await Chart.EvaluateAsync<string[]>("chart => [...chart.querySelectorAll("
            + "'[data-part=references] li')].map(item => item.textContent)");
    }

    /// <summary>The value of this series at this place, as the chart's data table writes it, or an empty string
    /// where the series has no value. A series or a place the chart doesn't have throws.</summary>
    public async Task<string> ValueAsync(string series, string place) =>
        await (await CellAsync(series, place)).EvaluateAsync<string>("cell => cell.textContent");

    /// <summary>The tone of this series' mark at this place: the one the series' <c>ItemTone</c> gives the item, or
    /// else the series' own <c>Tone</c>, and null with neither or where the series has no value. A series or a
    /// place the chart doesn't have throws.</summary>
    public async Task<Tone?> ToneAsync(string series, string place) =>
        await (await CellAsync(series, place)).GetAttributeAsync("data-tone") is { } tone
            ? Enum.Parse<Tone>(tone, true) : null;

    // The data table's cell for this series at this place.
    private async Task<ILocator> CellAsync(string series, string place)
    {
        var column = (await SeriesAsync()).ToList().IndexOf(series);
        var row = (await PlacesAsync()).ToList().IndexOf(place);
        if (column < 0 || row < 0)
        {
            throw new InvalidOperationException(Missing(column < 0 ? "series" : "place", series, place));
        }

        return Chart.Locator("[data-part=table] tbody tr").Nth(row).Locator("td").Nth(column);
    }

    /// <summary>Whether this series is hidden: left out of the drawing, with its values still in the data table. A
    /// series the chart doesn't have throws.</summary>
    public async Task<bool> IsSeriesHiddenAsync(string series) => !(await SeriesAsync()).Contains(series)
        ? throw new InvalidOperationException(Missing("series", series, ""))
        : !await Chart.EvaluateAsync<bool>(Drawn, series);

    /// <summary>Hides this series, if it shows, by pressing its button in the legend, and waits for the app's
    /// <c>HiddenChanged</c> to run. A series the chart doesn't have throws, and so does a legend with no buttons: a
    /// chart with one series, or with <c>CanHideSeries</c> off. It waits for the page to be interactive first.
    /// </summary>
    public Task HideSeriesAsync(string series) => SetHiddenAsync(series, true);

    /// <summary>Shows this series, if it is hidden, by pressing its button in the legend, and waits for the app's
    /// <c>HiddenChanged</c> to run. It throws as <see cref="HideSeriesAsync"/> does.</summary>
    public Task ShowSeriesAsync(string series) => SetHiddenAsync(series, false);

    private async Task SetHiddenAsync(string series, bool hidden)
    {
        // A prerendered legend has no buttons until the page is interactive.
        await Assertions.Expect(Plot).ToHaveAttributeAsync("data-ready", "");
        if (await IsSeriesHiddenAsync(series) == hidden)
        {
            return;
        }

        var button = Chart.Locator("[data-part=legend] button")
            .And(Chart.GetByRole(AriaRole.Button, new() { Name = series, Exact = true }));
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The chart's legend has no button for \"{series}\".");
        }

        await Commit.CountedAsync(Plot, "data-commits", async () =>
        {
            await button.ClickAsync();
            return true;
        });
    }

    /// <summary>Clicks this series' point at this place, and waits for the app's <c>OnPointClick</c> to run. A
    /// series or a place the chart doesn't have throws, and so do a hidden series, a place where the series has no
    /// value and a chart with no <c>OnPointClick</c>.</summary>
    public async Task ClickPointAsync(string series, string place)
    {
        await Assertions.Expect(Plot).ToHaveAttributeAsync("data-ready", "");
        if (await Plot.GetAttributeAsync("data-activates") is null)
        {
            throw new InvalidOperationException("The chart has no OnPointClick, so a click on a point does nothing.");
        }

        var spot = await Plot.EvaluateAsync<string>(Spot, new[] { series, place });
        if (!spot.Contains(','))
        {
            // The plot has no marks for a hidden series, which the data table still lists.
            var hidden = spot == "series" && (await SeriesAsync()).Contains(series);
            throw new InvalidOperationException(Missing(hidden ? "hidden" : spot, series, place));
        }

        var shares = spot.Split(',').Select(share => float.Parse(share, CultureInfo.InvariantCulture)).ToArray();
        var box = await Plot.BoundingBoxAsync() ?? throw new InvalidOperationException("The chart isn't shown.");
        await Commit.CountedAsync(Plot, "data-commits", async () =>
        {
            await Plot.ClickAsync(new() { Position = new() { X = box.Width * shares[0], Y = box.Height * shares[1] } });
            return true;
        });
    }

    /// <summary>What a zoomable chart says it shows, such as "Showing Jun 3 to Jun 7", or null for a chart without
    /// zoom buttons: one whose <c>TbXAxis</c> isn't <c>Zoomable</c>, or on a static page. It waits for the page to be
    /// interactive first.</summary>
    public async Task<string?> ViewAsync()
    {
        await Chart.WaitForAsync();
        // A prerendered chart has no zoom buttons until the page is interactive.
        if (await Plot.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Plot).ToHaveAttributeAsync("data-ready", "");
        }

        return await Chart.EvaluateAsync<string?>(
            "chart => chart.querySelector('[data-part=zoom] [data-part=view]')?.textContent ?? null");
    }

    /// <summary>Whether a zoomable chart shows only part of its scale.</summary>
    public async Task<bool> IsZoomedAsync()
    {
        await Chart.WaitForAsync();
        return await Plot.GetAttributeAsync("data-zoomed") is not null;
    }

    /// <summary>Zooms in around the middle of what is shown by pressing Zoom in, and waits for the app's
    /// <c>ViewChanged</c> to run. A chart without zoom buttons throws, and so does one zoomed in as far as it goes. It
    /// waits for the page to be interactive first.</summary>
    public Task ZoomInAsync() => PressZoomAsync("zoom-in", "zoom in any further");

    /// <summary>Zooms out around the middle of what is shown by pressing Zoom out, and waits for the app's
    /// <c>ViewChanged</c> to run. A chart without zoom buttons throws, and so does one that shows all of its scale. It
    /// waits for the page to be interactive first.</summary>
    public Task ZoomOutAsync() => PressZoomAsync("zoom-out", "zoom out any further");

    /// <summary>Moves back along the scale by a quarter of what is shown by pressing Move back, and waits for the
    /// app's <c>ViewChanged</c> to run. A chart without zoom buttons throws, and so does one that shows the start of
    /// its scale. It waits for the page to be interactive first.</summary>
    public Task MoveBackAsync() => PressZoomAsync("move-back", "move back any further");

    /// <summary>Moves forward along the scale by a quarter of what is shown by pressing Move forward, and waits for
    /// the app's <c>ViewChanged</c> to run. A chart without zoom buttons throws, and so does one that shows the end of
    /// its scale. It waits for the page to be interactive first.</summary>
    public Task MoveForwardAsync() => PressZoomAsync("move-forward", "move forward any further");

    /// <summary>Shows all of the scale by pressing Show all, and waits for the app's <c>ViewChanged</c> to run. A
    /// chart without zoom buttons throws, and so does one that already shows all of its scale. It waits for the page
    /// to be interactive first.</summary>
    public Task ShowAllAsync() => PressZoomAsync("show-all", "show any more of its scale");

    private async Task PressZoomAsync(string part, string cannot)
    {
        await Chart.WaitForAsync();
        if (await Plot.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("The chart has no zoom buttons: the page is static.");
        }

        // A prerendered chart has no zoom buttons until the page is interactive.
        await Assertions.Expect(Plot).ToHaveAttributeAsync("data-ready", "");
        var button = Chart.Locator($"[data-part=zoom] [data-part={part}]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException("The chart has no zoom buttons: its TbXAxis isn't Zoomable.");
        }

        if (await button.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The chart can't {cannot}.");
        }

        await Commit.ClickedAsync(button);
    }

    private static string Missing(string what, string series, string place) => what switch
    {
        "series" => $"The chart has no series named \"{series}\".",
        "place" => $"The chart has no place named \"{place}\".",
        "hidden" => $"\"{series}\" is hidden.",
        _ => $"\"{series}\" has no value at \"{place}\".",
    };
}
