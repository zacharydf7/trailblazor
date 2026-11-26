using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Lays out cards of different heights in columns without gaps: each one goes under the shortest column, in
/// the order they're given, which is also the order they're read and tabbed through.</summary>
public sealed class TbMasonry : TbComponentBase, IAsyncDisposable
{
    private ElementReference _root;
    private IJSObjectReference? _handle;
    private bool _disposed;

    /// <summary>The narrowest a column gets, as a CSS length, such as <c>12rem</c>; the masonry fits as many columns
    /// as its width allows. Defaults to <c>16rem</c>.</summary>
    [Parameter]
    public string MinColumnWidth { get; set; } = "16rem";

    /// <summary>The space between cards, across and down, as a step of the spacing scale (0, 1, 2, 3, 4, 5, 6, 8, 10,
    /// 12 or 16). Defaults to 4.</summary>
    [Parameter]
    public int Gap { get; set; } = 4;

    /// <summary>The element to render, such as <see cref="LayoutElement.Ul"/> for a list of cards whose items are
    /// <c>li</c> elements. Defaults to <see cref="LayoutElement.Div"/>.</summary>
    [Parameter]
    public LayoutElement As { get; set; }

    /// <summary>The cards, each one element.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, LayoutElements.Tag(As));
        builder.AddAttribute(1, "role", LayoutElements.Role(As));
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "class", ClassNames.Join("tb-masonry", Class));
        builder.AddAttribute(4, "style", Styles.Join($"--_tb-masonry-gap: {Spacing.Token(Gap, nameof(Gap))}; "
            + $"--_tb-masonry-column: {MinColumnWidth}", Style));
        builder.AddElementReferenceCapture(5, element => _root = element);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
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
            var module = await Modules.ImportAsync("./_content/Trailblazor/masonry.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("attachMasonry", _root);
            if (_disposed && handle is not null)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the masonry went with it.
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
