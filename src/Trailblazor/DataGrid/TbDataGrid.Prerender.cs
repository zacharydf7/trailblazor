using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trailblazor;

public partial class TbDataGrid<TItem>
{
    private PersistingComponentStateSubscription? _keepsPrerendered;
    private bool _lookedForPrerendered;

    /// <summary>Keeps the rows a <see cref="DataProvider"/> gave while the page prerendered, under this key, and
    /// shows them when the page turns interactive, so the provider isn't asked a second time and the rows don't give
    /// way to the loading row. The key must differ from every other key the page persists, and the rows must
    /// serialize to JSON and back: rows that don't come back as they went in, as when a property has a private
    /// setter, aren't kept, and the grid logs a warning and asks the provider again. What is kept holds every
    /// public property of each row the grid shows, and with WebAssembly or Auto rendering it is readable in the
    /// page. The provider is still asked when the interactive grid's first request differs from the prerendered
    /// one, as it does when a column has an <see cref="TbColumn{TItem}.Aggregate"/>, whose totals aren't kept; the
    /// kept rows stay in view meanwhile. It does nothing for <see cref="Items"/> or with <see cref="Virtualize"/>.
    /// </summary>
    [Parameter]
    public string? PrerenderKey { get; set; }

    private PersistentComponentState? Persisted => Services.GetService<PersistentComponentState>();

    // The framework reads the callback's target to decide which render mode gets the state, so it must be this
    // component's own method.
    private void KeepPrerenderedRows()
    {
        if (_keepsPrerendered is null && PrerenderKey is not null && !RendererInfo.IsInteractive)
        {
            _keepsPrerendered = Persisted?.RegisterOnPersisting(PersistRowsAsync);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Getting Started tells a trimmed app that "
        + "sets PrerenderKey to keep the row type's constructors and properties.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The rows go through the serializer that the "
        + "framework's own PersistAsJson uses on the next line, on the server that prerenders.")]
    private Task PersistRowsAsync()
    {
        // Totals are untyped objects that come back from JSON as JsonElement, so a load with totals is kept without
        // its request and the interactive grid asks again.
        if (PrerenderKey is null || _loaded is null || _loading || _failed)
        {
            return Task.CompletedTask;
        }

        var kept = new PrerenderedRows(_totals.Count == 0 ? _loaded : null, _rows, _total, CountUnknown, _uncounted);
        if (PrerenderedState.ComesBack(kept, out var failure))
        {
            Persisted?.PersistAsJson(PrerenderKey, kept);
        }
        else
        {
            // Rows that lost a property would show blank cells, or share one key, with nothing to say why.
            Logger?.LogWarning(failure, "The grid {Grid} didn't keep its rows under PrerenderKey {Key}, so it asks "
                + "its DataProvider again once the page is interactive: rows of type {RowType} {Why}", Name,
                PrerenderKey, typeof(TItem), PrerenderedState.Why(failure));
        }

        return Task.CompletedTask;
    }

    // Shows the rows kept while prerendering, once, as the interactive grid starts. Their request becomes the loaded
    // one, so the provider is asked only if the grid's first request differs.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Getting Started tells a trimmed app that "
        + "sets PrerenderKey to keep the row type's constructors and properties.")]
    private bool TryShowPrerenderedRows()
    {
        if (_lookedForPrerendered || PrerenderKey is null)
        {
            return false;
        }

        _lookedForPrerendered = true;
        PrerenderedRows? kept;
        try
        {
            if (Persisted?.TryTakeFromJson(PrerenderKey, out kept) is not true || kept is null)
            {
                return false;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            Logger?.LogWarning(exception, "The grid {Grid} couldn't read "
                + "the rows kept under PrerenderKey {Key}, so it asks its DataProvider again. The row type must "
                + "serialize to JSON and back.", Name, PrerenderKey);
            return false;
        }

        // Rows that came back sharing a key, as when the key's property was lost on the way, can't be shown.
        if (kept.Items.Select(KeyOf).Distinct().Count() != kept.Items.Count)
        {
            Logger?.LogWarning("The grid {Grid} couldn't show the rows kept under PrerenderKey {Key}, so it asks its "
                + "DataProvider again: two of them have the same key. The row type must come back from JSON with "
                + "what its KeySelector reads.", Name, PrerenderKey);
            return false;
        }

        ShowRows(kept.Items);
        _total = kept.TotalCount;
        // An uncounted grid goes on from the rows the prerender reached, and the count it learned if any.
        (_uncounted, _reached, _ended) = (kept.Uncounted, kept.TotalCount, kept.Uncounted && !kept.HasMore);
        _countedFor = kept.Uncounted ? CountKey : null;
        _loaded = kept.Asked;
        return true;
    }

    private ILogger? Logger => Services.GetService<ILogger<TbDataGrid<TItem>>>();

    private string Name => AriaLabel ?? nameof(TbDataGrid<TItem>);

    private sealed record PrerenderedRows(string? Asked, List<TItem> Items, int TotalCount, bool HasMore,
        bool Uncounted);
}
