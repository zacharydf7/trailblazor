using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>What <see cref="TbSelect{TValue}"/> and <see cref="TbMultiSelect{TItem}"/> share: a combobox that opens
/// a list of <see cref="TbOption{TValue}"/>s, or a native select when rendered statically.</summary>
public abstract partial class TbSelectBase<TValue> : TbFieldBase<TValue>, ISelect, IAsyncDisposable
{
    private readonly List<ISelectOption> _options = [];
    private ElementReference _combobox;
    private ElementReference _listbox;
    private SelectDisplay? _display;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<SelectToggle>? _toggle;
    private bool _open;
    private bool _lastOpen;
    private bool _openFromParent;
    private bool _disposed;

    /// <summary>The <see cref="TbOption{TValue}"/>s to choose from.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <summary>Text shown while no option is chosen. It is not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Shows a button that clears the choice. A single select bound to a value type, such as an enum, needs
    /// its nullable form, since the type's default is one of its values.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>Puts a text box above the options that narrows them to those containing what is typed, for long
    /// lists. The options are all on the page; to search a server as the user types, use
    /// <see cref="TbAutocomplete{TValue}"/>. Read on the first render.</summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>Names the filter box and shows in it while it's empty. Defaults to the app's text for <see
    /// cref="TbStrings.SelectFilter"/>, "Filter" in English.</summary>
    [Parameter]
    public string? FilterLabel { get; set; }

    /// <summary>Shown and announced when no option contains the filter's text.
    /// Defaults to the app's text for <see cref="TbStrings.SelectNoResults"/>, "No matches" in English.</summary>
    [Parameter]
    public string? NoResultsText { get; set; }

    private string FilterText => FilterLabel ?? Localizer[TbStrings.SelectFilter];

    /// <summary>Whether the list is open. Bind it with <c>@bind-Open</c> to open or close the list from code, or to
    /// load options when it opens.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the user opens or closes the list, or when it can't open as asked because the select is
    /// disabled.</summary>
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

    private protected virtual string RemovedFormat => Localizer[TbStrings.MultiSelectRemoved];

    // The field's value once nothing is chosen.
    private protected abstract TValue? Cleared { get; }

    private protected IReadOnlyList<ISelectOption> SelectedOptions =>
        _options.Where(option => IsChosen(option.Value)).ToList();

    private string ListboxId => $"{FieldId}-listbox";

    // Razor only finds public components by tag, so the internal display is added here.
    private RenderFragment Display => builder =>
    {
        builder.OpenComponent<SelectDisplay>(0);
        builder.AddComponentParameter(1, nameof(SelectDisplay.Selected),
            (Func<IReadOnlyList<ISelectOption>>)(() => SelectedOptions));
        builder.AddComponentParameter(2, nameof(SelectDisplay.Placeholder), Placeholder);
        builder.AddComponentParameter(3, nameof(SelectDisplay.Chips), ShowChips);
        builder.AddComponentParameter(4, nameof(SelectDisplay.Disabled), Disabled);
        builder.AddComponentReferenceCapture(5, display => _display = (SelectDisplay)display);
        builder.CloseComponent();
    };

    // A label's for attribute can't point at the interactive combobox, which names itself with aria-labelledby.
    private protected override bool LabelTargetsInput => !RendererInfo.IsInteractive;

    private protected abstract bool IsChosen(object? item);

    // The field's value after the user chooses this option.
    private protected abstract TValue? Choose(object? item);

    private protected abstract string? FormatItem(object? item);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
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
        try
        {
            if (firstRender)
            {
                var module = await Modules.ImportAsync("./_content/Trailblazor/select.js");
                if (_disposed)
                {
                    return;
                }

                _toggle = DotNetObjectReference.Create(new SelectToggle(OnToggleAsync, OnRemoveChipAsync));
                var handle = await module.InvokeAsync<IJSObjectReference?>("createSelect", _combobox, _listbox,
                    _toggle);
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
            }

            if (_handle is not null && _openFromParent)
            {
                _openFromParent = false;
                // A disabled select stays closed, and the parent hears so rather than keeping a value that didn't apply.
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

    bool ISelect.IsSelected(object? value) => IsChosen(value);

    string? ISelect.Format(object? value) => FormatItem(value);

    async Task ISelect.ChooseAsync(object? value)
    {
        await SetValueAsync(Choose(value));
        StateHasChanged();
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
    // aloud; focus returns to the combobox, since the button went with its chip.
    // A second removal can arrive before the first reaches the page, so a chip whose option is no longer chosen is
    // ignored rather than removing another.
    private async Task OnRemoveChipAsync(string optionId, string text)
    {
        var option = _options.FirstOrDefault(each => each.Id == optionId);
        if (!ShowChips || Disabled || option is null || option.Disabled || !IsChosen(option.Value))
        {
            return;
        }

        await SetValueAsync(Choose(option.Value));
        StateHasChanged();
        Announcer.Polite(string.Format(CultureInfo.CurrentCulture, RemovedFormat, text));
        if (!_disposed)
        {
            await _combobox.FocusAsync();
        }
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
