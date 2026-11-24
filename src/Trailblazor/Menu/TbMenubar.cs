using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A row of menus, such as File, Edit and View, following the WAI-ARIA menubar pattern: Tab reaches the bar
/// once, Left and Right move along it, Down opens a menu, and Left and Right in an open menu move to the menu beside
/// it. Put <see cref="TbMenu"/> components in it.</summary>
public partial class TbMenubar
{
    private ElementReference _root;
    private IJSObjectReference? _handle;
    private bool _disposed;

    /// <summary>Names the bar for screen readers, such as "Trail editor". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The bar's menus, each a <see cref="TbMenu"/> with a <see cref="TbMenu.Label"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>How big the menus' triggers on the bar are, as a toolbar's <c>Size</c> sizes its buttons. A menu with
    /// a <see cref="TbMenu.Size"/> of its own keeps it. Defaults to <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>How big the items inside each of the bar's menus are, as <see cref="TbMenu.ItemSize"/> sets for one
    /// menu. A menu with an <see cref="TbMenu.ItemSize"/> of its own keeps it. Defaults to
    /// <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size ItemSize { get; set; } = Size.Medium;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbMenubar)} needs an {nameof(AriaLabel)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/menubar.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("createMenubar", _root);
            if (_disposed && handle is not null)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
            StateHasChanged();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the bar went with it.
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
