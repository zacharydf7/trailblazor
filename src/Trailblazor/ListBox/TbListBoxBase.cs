using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>What <see cref="TbListBox{TValue}"/> and <see cref="TbMultiListBox{TItem}"/> share: a list of
/// <see cref="TbOption{TValue}"/>s that is always shown, or a native list when rendered statically.</summary>
public abstract partial class TbListBoxBase<TValue> : TbFieldBase<TValue>, ISelect, IAsyncDisposable
{
    private ElementReference _listbox;
    private IJSObjectReference? _handle;
    private bool _disposed;

    /// <summary>The <see cref="TbOption{TValue}"/>s to choose from.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <summary>How many options show before the list scrolls. Defaults to 5.</summary>
    [Parameter]
    public int Rows { get; set; } = 5;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    // Whether several options can be chosen at once.
    private protected abstract bool Multiple { get; }

    // The type every option's value must have.
    private protected abstract Type ItemType { get; }

    // A label's for attribute can't point at the interactive listbox, which names itself with aria-labelledby.
    private protected override bool LabelTargetsInput => !RendererInfo.IsInteractive;

    private protected abstract bool IsChosen(object? item);

    // The field's value after the user chooses this option.
    private protected abstract TValue? Choose(object? item);

    private protected abstract string? FormatItem(object? item);

    // Disabled, it is still a focusable element, kept so screen readers find it, so it refuses focus here.
    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) =>
        Disabled ? ValueTask.CompletedTask : _listbox.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/listbox.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("attachListBox", _listbox);
            if (handle is not null && _disposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the listbox went with it.
        }
    }

    bool ISelect.IsSelected(object? value) => IsChosen(value);

    string? ISelect.Format(object? value) => FormatItem(value);

    async Task<bool> ISelect.ChooseAsync(object? value)
    {
        // Unlike a select's, a disabled or read-only listbox's options are still on the page to be clicked.
        if (Disabled || ReadOnly)
        {
            return false;
        }

        await SetValueAsync(Choose(value));
        StateHasChanged();
        return true;
    }

    void ISelect.Add(ISelectOption option)
    {
        if (option.Value is not null && !ItemType.IsInstanceOfType(option.Value))
        {
            throw new InvalidOperationException($"A TbOption's value must be a {ItemType.Name}, the type of its " +
                $"listbox, but {option.Value} is a {option.Value.GetType().Name}.");
        }
    }

    void ISelect.Remove(ISelectOption option)
    {
    }

    // The options show themselves, so there is no chosen option to show elsewhere.
    void ISelect.Refresh()
    {
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
