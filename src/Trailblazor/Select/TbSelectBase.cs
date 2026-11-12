using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>What <see cref="TbSelect{TValue}"/> and <see cref="TbMultiSelect{TItem}"/> share: a combobox that opens
/// a list of <see cref="TbOption{TValue}"/>s, or a native select when rendered statically.</summary>
public abstract partial class TbSelectBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    : TbBoxedFieldBase<TValue>, ISelect, IAsyncDisposable
{
    private readonly List<ISelectOption> _options = [];
    // Past this many options from Items, only those in view are in the page.
    private const int VirtualizeAbove = 100;

    // A first guess at an option's height, in pixels, which Virtualize corrects as it measures them.
    private const float OptionSize = 36;

    // Options drawn beyond each end of the view, so a key press usually finds its option already there.
    private const int OptionsBeyondView = 10;

    private IReadOnlyList<ItemOption> _itemOptions = [];
    private List<ShownOption>? _shown;
    private string _query = "";
    private ElementReference _combobox;
    private ElementReference _listbox;
    private SelectDisplay? _display;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<SelectToggle>? _toggle;
    private bool _open;
    private bool _lastOpen;
    private bool _openFromParent;
    private bool _attaching;
    // Whether the script was attached to a filtered select, whose list sits in another element.
    private bool _attachedFiltered;
    private bool _disposed;

    /// <summary>The <see cref="TbOption{TValue}"/>s to choose from.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Sets up the select's script when it is first focused or pointed at, or its list is opened from code,
    /// not when the select first renders. That saves a call to the browser for each select on a page with many of them,
    /// such as a select on every row.</summary>
    [Parameter]
    public bool AttachOnFirstUse { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <summary>Text shown while no option is chosen. It is not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Shows a button that clears the choice. A single select bound to a value type clears to its default,
    /// such as <c>Guid.Empty</c> or an enum's <c>None</c>, which shows as no choice; bind the nullable form instead
    /// when the default is one of the options.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>Puts a text box above the options that narrows them to those containing what is typed, for long
    /// lists. The options are all on the page; to search a server as the user types, use
    /// <see cref="TbAutocomplete{TValue}"/>.</summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>The words it shows and says, such as the filter box's name and the clear button's.</summary>
    [Parameter]
    public SelectLabels Labels { get; set; } = new();

    private string FilterText => Labels.Filter ?? Localizer[TbStrings.SelectFilter];

    /// <summary>Whether the list is open. Bind it with <c>@bind-Open</c> to open or close the list from code, or to
    /// load options when it opens.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the user opens or closes the list, or when it can't open as asked because the select is
    /// disabled or read only.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    // Whether several options can be chosen at once.
    private protected abstract bool Multiple { get; }

    // The type every option's value must have.
    private protected abstract Type ItemType { get; }

    private protected abstract bool HasChoice { get; }

    // Whether the chosen options show as removable chips, and what is said when one is removed, with {0} for it.
    private protected virtual bool ShowChips => false;

    private string RemovedFormat => Labels.Removed ?? Localizer[TbStrings.MultiSelectRemoved];

    // Whether the list starts with an option that chooses every enabled option, or clears them once all are chosen.
    private protected virtual bool ShowSelectAll => false;

    private string SelectAllText => Labels.SelectAll ?? Localizer[TbStrings.MultiSelectSelectAll];

    private bool HasEnabledOptions => _options.Any(option => !option.Disabled);

    private bool AllChosen => HasEnabledOptions
        && _options.Where(option => !option.Disabled).All(option => IsChosen(option.Value));

    // The text shown for a chosen value that no option has, or null when an option has it or nothing is chosen.
    private protected virtual string? UnmatchedValueText => null;

    // The field's value once nothing is chosen.
    private protected abstract TValue? Cleared { get; }

    private protected IReadOnlyList<ISelectOption> SelectedOptions =>
        _options.Where(option => IsChosen(option.Value)).ToList();

    private string ListboxId => $"{FieldId}-listbox";

    private string PopupId => $"{FieldId}-popup";

    // Razor only finds public components by tag, so the internal display is added here.
    private RenderFragment Display => builder =>
    {
        builder.OpenComponent<SelectDisplay>(0);
        builder.AddComponentParameter(1, nameof(SelectDisplay.Selected),
            (Func<IReadOnlyList<ISelectOption>>)(() => SelectedOptions));
        builder.AddComponentParameter(2, nameof(SelectDisplay.Placeholder), Placeholder);
        builder.AddComponentParameter(3, nameof(SelectDisplay.Chips), ShowChips);
        builder.AddComponentParameter(4, nameof(SelectDisplay.Disabled), Disabled || ReadOnly);
        builder.AddComponentParameter(5, nameof(SelectDisplay.Unmatched), (Func<string?>)(() => UnmatchedValueText));
        builder.AddComponentReferenceCapture(6, display => _display = (SelectDisplay)display);
        builder.CloseComponent();
    };

    // A label's for attribute can't point at the interactive combobox, which names itself with aria-labelledby.
    private protected override bool LabelTargetsInput => !RendererInfo.IsInteractive;

    private protected abstract bool IsChosen(object? item);

    // The field's value after the user chooses this option.
    private protected abstract TValue? Choose(object? item);

    private protected abstract string? FormatItem(object? item);

    // The field's value after choosing these options, or taking them out when choose is false.
    private protected virtual TValue? ChooseAll(IEnumerable<object?> items, bool choose) =>
        throw new NotSupportedException();

    // A disabled combobox stays focusable so screen readers can find it; FocusAsync leaves it alone.
    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) =>
        Disabled ? ValueTask.CompletedTask : _combobox.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UseItems();
        // Only a new value from the parent opens or closes the list, so re-rendering with the old one keeps the
        // list as the user left it.
        if (Open != _lastOpen)
        {
            _lastOpen = Open;
            if (Open != _open)
            {
                _open = Open;
                _openFromParent = true;
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        try
        {
            if ((!AttachOnFirstUse || _openFromParent) && !_attaching)
            {
                _attaching = true;
                await AttachAsync();
            }
            else if (_handle is not null && _attachedFiltered != Filterable)
            {
                // Taken out first, so a render during the awaits neither disposes it twice nor skips the new one.
                var old = _handle;
                _handle = null;
                await old.DisposeHandleAsync();
                _toggle?.Dispose();
                await AttachAsync();
                // A list that was open opens again through the new script.
                _openFromParent = _open;
            }

            if (_handle is not null && _openFromParent)
            {
                _openFromParent = false;
                // A disabled or read-only select stays closed, and the parent hears so rather than keeping a value
                // that didn't apply.
                var opened = await _handle.InvokeAsync<bool>("setOpen", _open);
                if (opened != _open)
                {
                    _open = opened;
                    await OpenChanged.InvokeAsync(opened);
                }
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the select went with it.
        }
    }

    // A select that waits for its first use listens on its combobox and marks it, so tests know to focus it first. One
    // opened from code isn't marked: it attaches right after this render, and no later render would remove the mark.
    private bool WaitsForUse => AttachOnFirstUse && !_attaching && !_openFromParent;

    private EventCallback AttachOnUse => WaitsForUse ? EventCallback.Factory.Create(this, AttachOnUseAsync) : default;

    private async Task AttachOnUseAsync()
    {
        if (_attaching)
        {
            return;
        }

        _attaching = true;
        try
        {
            await AttachAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the select went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/select.js");
        if (_disposed)
        {
            return;
        }

        _toggle = DotNetObjectReference.Create(new SelectToggle(OnToggleAsync, OnRemoveChipAsync, OnFilterAsync,
            OnFindAsync));
        _attachedFiltered = Filterable;
        var handle = await module.InvokeAsync<IJSObjectReference?>("createSelect", _combobox, _listbox, _toggle);
        if (handle is null)
        {
            return;
        }

        if (_disposed)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
        // A Filterable that changed during the awaits takes effect on the next render.
        if (_attachedFiltered != Filterable)
        {
            StateHasChanged();
        }
    }

    bool ISelect.IsSelected(object? value) => IsChosen(value);

    // TbSelect and TbMultiSelect hand over their Items here.
    private protected abstract void UseItems();

    // Options are made again on each render, as TbOption children are, so items added to a list in place,
    // and an ItemDisabled that reads the page's state, take effect.
    private protected void UseItems<TItem>(IEnumerable<TItem>? items, Func<TItem, string>? itemText,
        RenderFragment<TItem>? itemTemplate, Func<TItem, bool>? itemDisabled)
    {
        _options.RemoveAll(option => option is ItemOption);

        _itemOptions = items?.Select((item, index) =>
        {
            var text = itemText?.Invoke(item) ?? FormatItem(item) ?? "";
            return new ItemOption($"{FieldId}-item-{index}", item, text, itemDisabled?.Invoke(item) ?? false,
                itemTemplate?.Invoke(item) ?? (builder => builder.AddContent(0, text)),
                itemTemplate is null ? null : text);
        }).ToList() ?? [];
        _options.AddRange(_itemOptions);
        Show();
    }

    // A long list is virtualized, so its filter runs here: most of its options aren't in the page for the script.
    private void Show()
    {
        if (!RendererInfo.IsInteractive || _itemOptions.Count <= VirtualizeAbove)
        {
            _shown = null;
            return;
        }

        var query = TextFold.Fold(_query);
        _shown = _itemOptions.Where(option => query == ""
                || TextFold.Fold(option.Text).Contains(query, StringComparison.Ordinal))
            .Select((option, index) => new ShownOption(index, option)).ToList();
    }

    // Where the list opens: the chosen option's place, which may not be in the page.
    private int? ChosenIndex => _shown?.FindIndex(shown => IsChosen(shown.Option.Value)) is >= 0 and var index
        ? index : null;

    private Task OnFilterAsync(string query)
    {
        _query = query;
        Show();
        StateHasChanged();
        return Task.CompletedTask;
    }

    // The script calls this only for a virtualized list, so _shown is never null.
    private Task<int> OnFindAsync(int from, string typed) => Task.FromResult(Typeahead.Find(_shown!.Count,
        place => _shown[place].Option.Text, place => _shown[place].Option.Disabled, from, typed));

    private readonly record struct ShownOption(int Index, ItemOption Option);

    // A choice the script makes for a key, such as Enter, is a click with no count of presses.
    private async Task ChooseItemAsync(ItemOption option, MouseEventArgs args)
    {
        if (!option.Disabled)
        {
            await ((ISelect)this).ChooseAsync(option.Value, args.Detail > 0 ? "pointer" : "keyboard");
        }
    }

    bool ISelect.ShowsCheck => !Multiple;

    bool ISelect.ShowsBox => Multiple;

    string? ISelect.Format(object? value) => FormatItem(value);

    async Task<bool> ISelect.ChooseAsync(object? value, string input)
    {
        if (Disabled || ReadOnly)
        {
            return false;
        }

        Telemetry.Used(TelemetryName, input: input);
        await SetValueAsync(Choose(value));
        StateHasChanged();
        return true;
    }

    void ISelect.Add(ISelectOption option)
    {
        if (option.Value is not null && !ItemType.IsInstanceOfType(option.Value))
        {
            throw new InvalidOperationException($"A TbOption's value must be a {ItemType.Name}, the type of its " +
                $"select, but {option.Value} is a {option.Value.GetType().Name}.");
        }

        _options.Add(option);
        StateHasChanged();
    }

    void ISelect.Remove(ISelectOption option)
    {
        _options.Remove(option);
        StateHasChanged();
    }

    void ISelect.Refresh() => _display?.Refresh();

    private async Task OnToggleAsync(bool open)
    {
        if (open == _open)
        {
            return;
        }

        _open = open;
        await OpenChanged.InvokeAsync(open);
    }

    // The script reports a chip removed by its button or by Backspace, with the text the chip shows, which is said
    // aloud unless its option is named by its Text; focus returns to the combobox, since the button went with its chip.
    // A second removal can arrive before the first reaches the page, so a chip whose option is no longer chosen is
    // ignored rather than removing another.
    private async Task OnRemoveChipAsync(string optionId, string text)
    {
        var option = _options.FirstOrDefault(each => each.Id == optionId);
        if (!ShowChips || Disabled || ReadOnly || option is null || option.Disabled || !IsChosen(option.Value))
        {
            return;
        }

        Telemetry.Used(TelemetryName, "remove");
        await SetValueAsync(Choose(option.Value));
        StateHasChanged();
        Announcer.Polite(string.Format(CultureInfo.CurrentCulture, RemovedFormat, option.Name ?? text));
        if (!_disposed)
        {
            await _combobox.FocusAsync();
        }
    }

    // Disabled options keep their state, as they do when chosen one at a time.
    private async Task SelectAllAsync(MouseEventArgs args)
    {
        if (Disabled || ReadOnly || !HasEnabledOptions)
        {
            return;
        }

        Telemetry.Used(TelemetryName, "select_all", args.Detail > 0 ? "pointer" : "keyboard");
        var enabled = _options.Where(option => !option.Disabled).Select(option => option.Value);
        await SetValueAsync(ChooseAll(enabled, !AllChosen));
        StateHasChanged();
    }

    // The clear button goes away with the value, so focus moves to the combobox instead of the page.
    private async Task ClearAsync()
    {
        await SetValueAsync(Cleared);
        StateHasChanged();
        // An async ValueChanged handler may have removed the select, and a removed element can't take focus.
        if (!_disposed)
        {
            await _combobox.FocusAsync();
        }
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

        _toggle?.Dispose();
    }
}
