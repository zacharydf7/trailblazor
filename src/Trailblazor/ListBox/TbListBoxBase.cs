using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>What <see cref="TbListBox{TValue}"/> and <see cref="TbMultiListBox{TItem}"/> share: a list of
/// <see cref="TbOption{TValue}"/>s that is always shown, or a native list when rendered statically.</summary>
public abstract partial class TbListBoxBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    : TbFieldBase<TValue>, ISelect, IAsyncDisposable
{
    private ElementReference _listbox;
    private readonly List<ISelectOption> _options = [];
    private IJSObjectReference? _handle;
    private readonly HashSet<string> _range = [];
    private DotNetObjectReference<ListBoxEvents>? _events;
    private bool _disposed;

    /// <summary>
    /// The height of the options and the size of their text. Defaults to <see cref="Trailblazor.Size.Medium"/>.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>The <see cref="TbOption{TValue}"/>s to choose from.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <summary>How many options show before the list scrolls. Defaults to 5.</summary>
    [Parameter]
    public int Rows { get; set; } = 5;

    /// <summary>The words it shows and says, such as the message for an empty list.</summary>
    [Parameter]
    public ListBoxLabels Labels { get; set; } = new();

    private string EmptyText => Labels.Empty ?? Localizer[TbStrings.ListBoxEmpty];

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

    // The field's value after the user chooses several options at once, which only multiple selection can do.
    private protected virtual TValue? ChooseMany(IReadOnlyList<object?> items) => throw new NotSupportedException();

    private protected abstract string? FormatItem(object? item);

    // A disabled listbox stays focusable so screen readers can find it; FocusAsync leaves it alone.
    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) =>
        Disabled ? ValueTask.CompletedTask : _listbox.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
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

            _events = Multiple ? DotNetObjectReference.Create(new ListBoxEvents(ChooseAllAsync, ChooseRangeAsync)) : null;
            var handle = await module.InvokeAsync<IJSObjectReference?>("attachListBox", _listbox, _events);
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

    bool ISelect.ShowsCheck => false;

    bool ISelect.ShowsBox => Multiple;

    string? ISelect.Format(object? value) => FormatItem(value);

    async Task<bool> ISelect.ChooseAsync(object? value, string input)
    {
        // Unlike a select's, a disabled or read-only listbox's options are still on the page to be clicked.
        if (Disabled || ReadOnly)
        {
            return false;
        }

        Telemetry.Used(TelemetryName, input: input);
        await SetValueAsync(Choose(value));
        StateHasChanged();
        return true;
    }

    private Task ChooseAllAsync() => ChooseManyAsync(_options, "select_all", "keyboard");

    // A long range arrives in batches, and they make one change to the value once the last is in. A second range can
    // arrive alongside the first, so an id may come twice; more ids than options can only come from a bad client.
    private Task ChooseRangeAsync(string[] ids, bool last)
    {
        _range.UnionWith(ids);
        if (_range.Count > _options.Count)
        {
            _range.Clear();
            return Task.CompletedTask;
        }

        if (!last)
        {
            return Task.CompletedTask;
        }

        var options = _options.Where(option => _range.Contains(option.Id)).ToList();
        _range.Clear();
        return ChooseManyAsync(options, "select_range", "pointer");
    }

    private async Task ChooseManyAsync(IEnumerable<ISelectOption> options, string action, string input)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        var items = options.Where(option => !option.Disabled && !IsChosen(option.Value))
            .Select(option => option.Value).ToList();
        if (items.Count > 0)
        {
            Telemetry.Used(TelemetryName, action, input);
            await SetValueAsync(ChooseMany(items));
            StateHasChanged();
        }
    }

    void ISelect.Add(ISelectOption option)
    {
        if (option.Value is not null && !ItemType.IsInstanceOfType(option.Value))
        {
            throw new InvalidOperationException($"A TbOption's value must be a {ItemType.Name}, the type of its " +
                $"listbox, but {option.Value} is a {option.Value.GetType().Name}.");
        }

        _options.Add(option);
        // The empty list's message goes once the first option comes, and comes back once the last one goes.
        if (_options.Count == 1)
        {
            StateHasChanged();
        }
    }

    void ISelect.Remove(ISelectOption option)
    {
        if (_options.Remove(option) && _options.Count == 0)
        {
            StateHasChanged();
        }
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

        _events?.Dispose();
    }
}
