using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Moves between the pages of a long list, with numbered page buttons or, in compact mode, a box for the
/// page number.</summary>
public partial class TbPagination
{
    private ElementReference _pager;
    private ElementReference _list;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<PaginationFit>? _fit;
    private bool _disposed;
    // How many buttons the script found room for on one row, or null where nothing limits the pager's width.
    private int? _slots;
    private bool _fitted;

    // The pages to show: the ones asked for, or fewer where there is less room, giving up the current page's
    // siblings before the pages at the ends. Null means the compact form: asked for with Compact, or used when even
    // the first, current and last pages don't fit.
    private IReadOnlyList<int?>? ShownPages()
    {
        if (Compact)
        {
            return null;
        }

        var steps = (ShowFirstLast ? 2 : 0) + (ShowPreviousNext ? 2 : 0);
        var (siblings, boundaries) = (SiblingCount, BoundaryCount);
        while (true)
        {
            var pages = PageRange.For(CurrentPage, Count, siblings, boundaries);
            if (_slots is not { } slots || pages.Count + steps <= slots)
            {
                return pages;
            }

            if (siblings > 0)
            {
                siblings--;
            }
            else if (boundaries > 1)
            {
                boundaries--;
            }
            else
            {
                return null;
            }
        }
    }

    internal void OnFit(int? slots)
    {
        if (_slots != slots)
        {
            _slots = slots;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        if (!firstRender)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/pagination.js");
            if (_disposed)
            {
                return;
            }

            _fit = DotNetObjectReference.Create(new PaginationFit(DispatchExceptionAsync, OnFit));
            var handle = await module.InvokeAsync<IJSObjectReference?>("fitPagination", _pager, _list, _fit);
            if (handle is not null && _disposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
            _slots = handle is null ? null : await handle.InvokeAsync<int?>("slots");
            _fitted = true;
            StateHasChanged();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the pager went with it.
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

        _fit?.Dispose();
    }
}
