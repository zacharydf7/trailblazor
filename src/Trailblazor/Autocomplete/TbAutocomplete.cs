using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A text field that suggests values as the user types, from a list or an async search, and lets the user
/// choose one. Bind it with <c>@bind-Value</c>.</summary>
public partial class TbAutocomplete<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    : IAsyncDisposable
{
    private ElementReference _input;
    private ElementReference _listbox;
    private string _text = "";
    private TValue? _shownValue;
    private List<TValue> _results = [];
    private bool _searching;
    private bool _searched;
    private bool _failed;
    private CancellationTokenSource? _search;
    private CancellationTokenSource? _typing;
    private bool _typedSinceReport;
    private int _reports;
    private int? _announcedCount;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<AutocompleteEvents>? _events;
    private bool _open;
    private bool _lastOpen;
    private bool _openFromParent;
    private bool _disposed;
    private readonly ItemHandlers<TValue, Func<Task>> _choose;

    /// <summary>Creates the autocomplete.</summary>
    public TbAutocomplete() => _choose = new(item => () => ChooseAsync(item));

    /// <summary>Finds the suggestions for what the user typed. It runs once the user stops typing for
    /// <see cref="DebounceDelay"/>, and the token is canceled when the user types again.</summary>
    [Parameter]
    public Func<string, CancellationToken, Task<IEnumerable<TValue>>>? Search { get; set; }

    /// <summary>Suggestions to filter by what the user typed, when there is no <see cref="Search"/>.</summary>
    [Parameter]
    public IEnumerable<TValue>? Items { get; set; }

    /// <summary>Turns a suggestion into the text shown for it and put in the field when it is chosen. Defaults to
    /// the value's own text.</summary>
    [Parameter]
    public Func<TValue, string>? ItemText { get; set; }

    /// <summary>Draws a suggestion in the list instead of its text.</summary>
    [Parameter]
    public RenderFragment<TValue>? ItemTemplate { get; set; }

    /// <summary>Makes what the user typed the value when it isn't a suggestion, read as the value's type. Without
    /// it, leaving the field puts back the chosen value's text.</summary>
    [Parameter]
    public bool FreeText { get; set; }

    /// <summary>How long the user must stop typing before <see cref="Search"/> runs. Defaults to 200 milliseconds;
    /// zero searches at once. It is timed by the app's <see cref="TimeProvider"/>.</summary>
    [Parameter]
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>How many characters the user must type before suggestions show. Defaults to 0.</summary>
    [Parameter]
    public int MinCharacters { get; set; }

    /// <summary>The most suggestions the list shows.</summary>
    [Parameter]
    public int? MaxItems { get; set; }

    /// <summary>Text shown while the field is empty. It is not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Shows a button that clears the field.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>The words it shows and says, such as the clear button's name and the text while a search runs.
    /// </summary>
    [Parameter]
    public AutocompleteLabels Labels { get; set; } = new();

    /// <summary>Raised with the field's text once the user stops typing for <see cref="DebounceDelay"/>, whether or
    /// not a search runs. Choosing a suggestion or clearing the field raises it at once, and so does pressing Enter or
    /// leaving the field with text it hasn't reported yet. Use it to act on what is typed beyond suggesting, such as
    /// filtering a table. Setting <c>Value</c> doesn't raise it.</summary>
    [Parameter]
    public EventCallback<string> OnTextChanged { get; set; }

    /// <summary>Raised when <see cref="Search"/> fails. The field stays usable, and typing again searches again.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnSearchFailed { get; set; }

    /// <summary>Content before the input, usually an icon.</summary>
    [Parameter]
    public RenderFragment? Start { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <summary>Whether the list is open. Bind it with <c>@bind-Open</c> to open or close it from code.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the user opens or closes the list.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    private string ListboxId => $"{FieldId}-listbox";

    private string SearchFailedLabel => Labels.SearchFailed ?? Localizer[TbStrings.AutocompleteSearchFailed];

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    private protected override Task OnResetAsync()
    {
        StopSearch();
        _shownValue = Value;
        _text = Value is null ? "" : TextOf(Value);
        _results = [];
        _searched = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A value from the parent, or a choice the user made, puts its text in the field.
        if (!EqualityComparer<TValue>.Default.Equals(Value, _shownValue))
        {
            _shownValue = Value;
            _text = Value is null ? "" : TextOf(Value);
        }

        // Only a new value from the parent opens or closes the list, as in TbSelect.
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
            if (firstRender)
            {
                var module = await Modules.ImportAsync("./_content/Trailblazor/autocomplete.js");
                if (_disposed)
                {
                    return;
                }

                _events = DotNetObjectReference.Create(new AutocompleteEvents(OnToggleAsync, BrowseAsync));
                var handle = await module.InvokeAsync<IJSObjectReference?>("createAutocomplete", _input, _listbox,
                    _events);
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
                var opened = await _handle.InvokeAsync<bool>("setOpen", _open);
                if (opened != _open)
                {
                    _open = opened;
                    await OpenChanged.InvokeAsync(opened);
                }

                if (opened)
                {
                    await BrowseAsync();
                }
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the field went with it.
        }
    }

    private string TextOf(TValue item) => ItemText?.Invoke(item) ?? FormatValue(item) ?? "";

    private bool IsChosen(TValue item) => EqualityComparer<TValue>.Default.Equals(item, Value);

    // Each keystroke cancels the search before it, so only the latest text's suggestions are shown.
    private async Task OnTypedAsync(string? text)
    {
        _text = text ?? "";
        _typedSinceReport = true;
        // The report runs beside the search, so a slow listener doesn't hold back the suggestions.
        var report = ReportTypedAsync();
        await SearchAsync();
        StateHasChanged();
        await report;
    }

    // Each keystroke restarts the wait, so only text the user paused on is reported.
    private async Task ReportTypedAsync()
    {
        if (!OnTextChanged.HasDelegate)
        {
            return;
        }

        _typing?.Cancel();
        _typing?.Dispose();
        _typing = new CancellationTokenSource();
        try
        {
            await Task.Delay(DebounceDelay, Time, _typing.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await ReportTextAsync();
    }

    private async Task ReportTextAsync()
    {
        // A parent can remove the field while its value change is handled.
        if (_disposed)
        {
            return;
        }

        _typing?.Cancel();
        _typedSinceReport = false;
        _reports++;
        await OnTextChanged.InvokeAsync(_text);
    }

    // A search the user has moved on from, by choosing, leaving or clearing, is dropped with its results.
    private void StopSearch()
    {
        _search?.Cancel();
        _searching = _failed = false;
    }

    private async Task SearchAsync()
    {
        _search?.Cancel();
        _search?.Dispose();
        _search = new CancellationTokenSource();
        var token = _search.Token;
        _failed = false;
        if (_text.Length < MinCharacters)
        {
            _results = [];
            _searched = _searching = false;
            return;
        }

        _searching = true;
        try
        {
            IEnumerable<TValue> found;
            if (Search is not null)
            {
                await Task.Delay(DebounceDelay, Time, token);
                found = await Search(_text, token);
            }
            else
            {
                found = Items?.Where(item => TextOf(item).Contains(_text, StringComparison.CurrentCultureIgnoreCase))
                    ?? [];
            }

            token.ThrowIfCancellationRequested();
            _results = [.. MaxItems is int most ? found.Take(most) : found];
            _choose.KeepOnly(_results);
            _searching = false;
            _searched = true;
            // Only a new count is spoken, so typing doesn't bring an announcement with every key.
            if (_results.Count != _announcedCount)
            {
                _announcedCount = _results.Count;
                Announcer.Polite(string.Format(CultureInfo.CurrentCulture,
                    Labels.Results ?? Localizer[TbStrings.AutocompleteResults], _results.Count));
            }
        }
        // A search the user moved on from ends quietly, even when it reports the cancel its own way.
        catch (Exception) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _results = [];
            _searching = _searched = false;
            _failed = true;
            _announcedCount = null;
            Announcer.Polite(SearchFailedLabel);
            await OnSearchFailed.InvokeAsync(exception);
        }
    }

    private async Task ChooseAsync(TValue item)
    {
        // A list that was open before the field turned read-only or disabled still shows its options.
        if (ReadOnly || Disabled)
        {
            return;
        }

        StopSearch();
        _text = TextOf(item);
        _shownValue = item;
        await SetValueAsync(item);
        await ReportTextAsync();
    }

    // Leaving the field, or Enter with no option active, settles what was typed.
    // It then reports text not reported yet (typed since the last report, or changed by settling), unless settling
    // chose a match, which reports it itself.
    private async Task CommitAsync()
    {
        _typing?.Cancel();
        var reports = _reports;
        var before = _text;
        await SettleTextAsync();
        if (_reports == reports && (_typedSinceReport || _text != before))
        {
            await ReportTextAsync();
        }
    }

    private async Task SettleTextAsync()
    {
        StopSearch();
        // Text typed before the field turned read-only or disabled isn't committed.
        if (ReadOnly || Disabled)
        {
            _text = Value is null ? "" : TextOf(Value);
            _results = [];
            _searched = false;
            return;
        }

        if (string.IsNullOrEmpty(_text))
        {
            _shownValue = default;
            await SetValueAsync(default);
            return;
        }

        // Typing the value's own text back keeps the value, and drops an error left by text that didn't parse.
        if (Value is not null && _text == TextOf(Value))
        {
            await SetValueAsync(Value);
            return;
        }

        if (_results.Find(item => string.Equals(TextOf(item), _text, StringComparison.CurrentCultureIgnoreCase))
            is { } match)
        {
            await ChooseAsync(match);
        }
        else if (FreeText)
        {
            await SetTextAsync(_text);
            _shownValue = Value;
        }
        else
        {
            _text = Value is null ? "" : TextOf(Value);
            _results = [];
            _searched = false;
        }
    }

    // The clear button goes away with the text, so focus moves to the input instead of the page.
    private async Task ClearAsync()
    {
        StopSearch();
        _text = "";
        _results = [];
        _searched = false;
        _shownValue = default;
        await SetValueAsync(default);
        await ReportTextAsync();
        if (!_disposed)
        {
            await _input.FocusAsync();
        }
    }

    private async Task OnToggleAsync(bool open)
    {
        if (!open)
        {
            _announcedCount = null;
        }

        if (open == _open)
        {
            return;
        }

        _open = open;
        await OpenChanged.InvokeAsync(open);
    }

    // Opening the list without typing, by the keys or from code, shows suggestions for the text already there. The
    // browser calls this from outside an event, so the list is rendered as it starts searching and again at the end.
    private async Task BrowseAsync()
    {
        if (_searched || _searching)
        {
            return;
        }

        var search = SearchAsync();
        StateHasChanged();
        await search;
        StateHasChanged();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        _disposed = true;
        _search?.Cancel();
        _search?.Dispose();
        _typing?.Cancel();
        _typing?.Dispose();
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
