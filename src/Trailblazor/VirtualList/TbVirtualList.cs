using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A long list of items that draws only those near the view, such as a feed of trail reports. Items can be any
/// height. Screen readers and keyboards get the WAI-ARIA feed pattern: Page Down and Page Up move between items.
/// </summary>
public partial class TbVirtualList<TItem>
{
    private ElementReference _feed;
    private Virtualize<VirtualItem<TItem>>? _virtualize;
    // Set once the script has attached; the feed's keys and ScrollToAsync work from then on, and the script's
    // data-ready says so.
    private IJSObjectReference? _handle;
    private int _total;
    private int _loads;
    private bool _disposed;

    /// <summary>Names the list for screen readers, such as "Trail reports". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The items, all in memory. Set this or <see cref="ItemsProvider"/>. Before the page is interactive,
    /// including while it prerenders, every item is drawn, so for a very long list use <see cref="ItemsProvider"/> or
    /// turn prerendering off.</summary>
    [Parameter]
    public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Loads the items in view as the list scrolls, such as from a database, and gives their total. Set this or
    /// <see cref="Items"/>.</summary>
    [Parameter]
    public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>Draws one item.</summary>
    [Parameter]
    public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>Drawn in place of an item while <see cref="ItemsProvider"/> loads it.</summary>
    [Parameter]
    public RenderFragment<PlaceholderContext>? Placeholder { get; set; }

    /// <summary>Shown when there are no items.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Names an item for screen readers, such as by its title. Without it, an item has no name, though screen
    /// readers still read its content.</summary>
    [Parameter]
    public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>The height an item is expected to have, in pixels, used until items are measured. Items can still be
    /// any height. Defaults to 50.</summary>
    [Parameter]
    public float ItemSize { get; set; } = 50;

    /// <summary>How many items to draw beyond each end of the view, so a quick scroll doesn't show gaps. Defaults to 3.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 3;

    /// <summary>The list's height, such as <c>24rem</c>, inside which it scrolls. Leave it null to let the page
    /// scroll.</summary>
    [Parameter]
    public string? Height { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private bool Loading => _loads > 0;

    // A feed holds only articles, so a list with nothing to show, not even items on their way, is a group holding its
    // empty content.
    private string Role => Total > 0 || Loading ? "feed" : "group";

    // In-memory items are counted before Virtualize asks for them, so a static page has the total too.
    private int Total => Items?.Count ?? _total;

    private string ItemSizeText => ItemSize.ToString(CultureInfo.InvariantCulture);

    /// <summary>Scrolls the item at this position, from 0, into view, drawing it first if needed. It does nothing
    /// before the page is interactive and the list's script has attached, which the list marks with
    /// <c>data-ready</c>, or before <see cref="ItemsProvider"/> has given the total.</summary>
    /// <param name="index">The item's position.</param>
    /// <param name="focus">Whether to move focus to it too.</param>
    /// <exception cref="ArgumentOutOfRangeException">The position is negative, or past the last item.</exception>
    public async Task ScrollToAsync(int index, bool focus = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (Items is not null || _total > 0)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Total);
        }

        if (_handle is not null)
        {
            await _handle.InvokeVoidAsync("scrollToIndex", index, focus);
        }
    }

    /// <summary>Loads the items again, such as after the data behind <see cref="ItemsProvider"/> changed.</summary>
    public Task RefreshDataAsync() => _virtualize?.RefreshDataAsync() ?? Task.CompletedTask;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbVirtualList<TItem>)} needs an {nameof(AriaLabel)}.");
        }

        if ((Items is null) == (ItemsProvider is null))
        {
            throw new InvalidOperationException(
                $"{nameof(TbVirtualList<TItem>)} needs {nameof(Items)} or an {nameof(ItemsProvider)}, not both.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // Items given in memory may have changed in place, so the drawn ones are read again on every render.
        if (Items is not null && _virtualize is not null)
        {
            await _virtualize.RefreshDataAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/virtual-list.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("createVirtualList", _feed);
            if (_disposed && handle is not null)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the list went with it.
        }
    }

    // Virtualize gives an item without its place, so the items go through a provider that adds it, for aria-posinset.
    private async ValueTask<ItemsProviderResult<VirtualItem<TItem>>> ProvideAsync(ItemsProviderRequest request)
    {
        if (Items is { } items)
        {
            SetTotal(items.Count);
            var shown = Math.Max(0, Math.Min(request.Count, items.Count - request.StartIndex));
            return new(Enumerable.Range(request.StartIndex, shown).Select(index => new VirtualItem<TItem>(index,
                items[index])), items.Count);
        }

        _loads++;
        StateHasChanged();
        try
        {
            var result = await ItemsProvider!(request);
            SetTotal(result.TotalItemCount);
            return new(result.Items.Select((item, at) => new VirtualItem<TItem>(request.StartIndex + at, item)),
                result.TotalItemCount);
        }
        finally
        {
            _loads--;
            StateHasChanged();
        }
    }

    private void SetTotal(int total)
    {
        if (total != _total)
        {
            _total = total;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}

// An item with its place in the list.
internal readonly record struct VirtualItem<TItem>(int Index, TItem Item);
