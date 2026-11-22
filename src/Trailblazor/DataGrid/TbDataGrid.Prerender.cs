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
    /// serialize to JSON and back. The provider is still asked when the interactive grid's first request differs from
    /// the prerendered one, as it does when a column has an <see cref="TbColumn{TItem}.Aggregate"/>, whose totals
    /// aren't kept; the kept rows stay in view meanwhile. It does nothing for <see cref="Items"/> or with
    /// <see cref="Virtualize"/>.</summary>
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
    private Task PersistRowsAsync()
    {
        // Totals are untyped objects that come back from JSON as JsonElement, so a load with totals is kept without
        // its request and the interactive grid asks again.
        if (PrerenderKey is not null && _loaded is not null && !_loading && !_failed)
        {
            Persisted?.PersistAsJson(PrerenderKey, new PrerenderedRows(_totals.Count == 0 ? _loaded : null, _rows, _total));
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
            Services.GetService<ILogger<TbDataGrid<TItem>>>()?.LogWarning(exception, "The grid {Grid} couldn't read "
                + "the rows kept under PrerenderKey {Key}, so it asks its DataProvider again. The row type must "
                + "serialize to JSON and back.", AriaLabel ?? nameof(TbDataGrid<TItem>), PrerenderKey);
            return false;
        }

        ShowRows(kept.Items);
        _total = kept.TotalCount;
        _loaded = kept.Asked;
        return true;
    }

    private sealed record PrerenderedRows(string? Asked, List<TItem> Items, int TotalCount);
}
