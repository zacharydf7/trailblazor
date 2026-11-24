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
    /// <see cref="KeySelector"/>, or use items that compare by value. Items that don't come back as they went in,
    /// as when a property has a private setter, aren't kept, and the tree logs a warning and loads the children
    /// again. What is kept holds every public property of each parent that was open and of its children, and with
    /// WebAssembly or Auto rendering it is readable in the page. It does nothing without
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
            if (Persisted?.TryTakeFromJson<List<LoadedChildren>>(PrerenderKey, out var kept) is not true
                || kept is null)
            {
                return;
            }

            // Parents that came back sharing a key, as when the key's property was lost on the way, would each
            // show the last one's children.
            if (kept.Select(loaded => KeyOf(loaded.Parent)).Distinct().Count() != kept.Count)
            {
                Logger?.LogWarning("The tree {Tree} couldn't take the children kept under PrerenderKey {Key}, so "
                    + "it loads them again: two of their parents have the same key. The item type must come back "
                    + "from JSON with what its KeySelector reads.", Name, PrerenderKey);
                return;
            }

            foreach (var loaded in kept)
            {
                _loaded[KeyOf(loaded.Parent)] = loaded.Children;
            }

            _tookPrerendered = true;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            Logger?.LogWarning(exception, "The tree {Tree} couldn't "
                + "read the children kept under PrerenderKey {Key}, so it loads them again. The item type must "
                + "serialize to JSON and back.", Name, PrerenderKey);
        }
    }

    private ILogger? Logger => Services.GetService<ILogger<TbTreeView<TItem, TKey>>>();

    private string Name => AriaLabel ?? nameof(TbTreeView<TItem, TKey>);

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
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The items go through the serializer that the "
        + "framework's own PersistAsJson uses on the next line, on the server that prerenders.")]
    private Task PersistChildrenAsync()
    {
        List<LoadedChildren> kept = [.. _loaded.Where(loaded => _nodes.ContainsKey(loaded.Key))
            .Select(loaded => new LoadedChildren(_nodes[loaded.Key].Item, loaded.Value))];
        if (kept.Count == 0 || PrerenderKey is null)
        {
            return Task.CompletedTask;
        }

        if (PrerenderedState.ComesBack(kept, out var failure))
        {
            Persisted?.PersistAsJson(PrerenderKey, kept);
        }
        else
        {
            // Items that lost a property would show blank rows, or file children under the wrong parent.
            Logger?.LogWarning(failure, "The tree {Tree} didn't keep its loaded children under PrerenderKey {Key}, "
                + "so it loads them again once the page is interactive: items of type {ItemType} {Why}", Name,
                PrerenderKey, typeof(TItem), PrerenderedState.Why(failure));
        }

        return Task.CompletedTask;
    }

    private sealed record LoadedChildren(TItem Parent, List<TItem> Children);
}
