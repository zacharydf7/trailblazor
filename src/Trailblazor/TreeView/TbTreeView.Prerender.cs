using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trailblazor;

public partial class TbTreeView<TItem, TKey>
{
    private PersistingComponentStateSubscription? _keepsPrerendered;
    private bool _lookedForPrerendered;
    private bool _tookPrerendered;

    /// <summary>Keeps the children that <see cref="LoadChildren"/> gave while the page prerendered, under this key, and
    /// takes them as loaded when the page turns interactive, so rows that start open aren't loaded a second time and
    /// their children don't give way to loading rows. The key must differ from every other key the page persists.
    /// The items must serialize to JSON and back, and a parent's key must come out the same afterward: set a
    /// <see cref="KeySelector"/>, or use items that compare by value. It does nothing without
    /// <see cref="LoadChildren"/>.</summary>
    [Parameter]
    public string? PrerenderKey { get; set; }

    private PersistentComponentState? Persisted => Services.GetService<PersistentComponentState>();

    // The framework reads the callback's target to decide which render mode gets the state, so it must be this
    // component's own method.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Getting Started tells a trimmed app that "
        + "sets PrerenderKey to keep the item type's constructors and properties.")]
    private void TakePrerenderedChildren()
    {
        if (PrerenderKey is null || LoadChildren is null)
        {
            return;
        }

        if (!RendererInfo.IsInteractive)
        {
            _keepsPrerendered ??= Persisted?.RegisterOnPersisting(PersistChildrenAsync);
            return;
        }

        if (_lookedForPrerendered)
        {
            return;
        }

        _lookedForPrerendered = true;
        try
        {
            if (Persisted?.TryTakeFromJson<List<LoadedChildren>>(PrerenderKey, out var kept) is true && kept is not null)
            {
                foreach (var loaded in kept)
                {
                    _loaded[KeyOf(loaded.Parent)] = loaded.Children;
                }

                _tookPrerendered = true;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            Services.GetService<ILogger<TbTreeView<TItem, TKey>>>()?.LogWarning(exception, "The tree {Tree} couldn't "
                + "read the children kept under PrerenderKey {Key}, so it loads them again. The item type must "
                + "serialize to JSON and back.", AriaLabel ?? nameof(TbTreeView<TItem, TKey>), PrerenderKey);
        }
    }

    // Runs once the rows are built and before they first draw, so a checked row's kept children show checked.
    private Task CheckKeptChildrenAsync()
    {
        if (!_tookPrerendered)
        {
            return Task.CompletedTask;
        }

        _tookPrerendered = false;
        var cascaded = false;
        foreach (var key in _loaded.Keys)
        {
            cascaded |= CheckLoadedChildren(key);
        }

        if (!cascaded)
        {
            return Task.CompletedTask;
        }

        Flatten();
        return CheckedKeysChanged.InvokeAsync(Reported(_checked));
    }

    // Each parent item is kept, not its key, since the key may be any object; it's worked out again from the item.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Getting Started tells a trimmed app that "
        + "sets PrerenderKey to keep the item type's constructors and properties.")]
    private Task PersistChildrenAsync()
    {
        List<LoadedChildren> kept = [.. _loaded.Where(loaded => _nodes.ContainsKey(loaded.Key))
            .Select(loaded => new LoadedChildren(_nodes[loaded.Key].Item, loaded.Value))];
        if (kept.Count > 0 && PrerenderKey is not null)
        {
            Persisted?.PersistAsJson(PrerenderKey, kept);
        }

        return Task.CompletedTask;
    }

    private sealed record LoadedChildren(TItem Parent, List<TItem> Children);
}
