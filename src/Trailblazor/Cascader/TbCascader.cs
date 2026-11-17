using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A field that chooses a path through nested choices, such as a country, then a region, then a city, from
/// lists side by side, one for each level. Typing searches every level at once. Bind it with <c>@bind-Value</c>.
/// </summary>
public partial class TbCascader<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    : IAsyncDisposable
{
    private ElementReference _input;
    private ElementReference _popup;
    private string _text = "";
    private TValue? _shownValue;
    // The choices whose lists are open, one for each level after the first.
    private readonly List<CascaderItem<TValue>> _trail = [];
    private List<IReadOnlyList<CascaderItem<TValue>>> _results = [];
    private int? _announcedCount;
    private IReadOnlyList<CascaderItem<TValue>>? _shownItems;
    private string _shownPathText = "";
    private int _handled;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<CascaderEvents>? _events;
    private bool _disposed;
    private readonly ItemHandlers<CascaderItem<TValue>, Func<Task>> _pick;
    private readonly ItemHandlers<IReadOnlyList<CascaderItem<TValue>>, Func<Task>> _pickPath;

    /// <summary>Creates the cascader.</summary>
    public TbCascader()
    {
        _pick = new(item => () => HandledAsync(() => PickAsync(item)));
        _pickPath = new(path => () => HandledAsync(() => ChooseAsync(path)));
    }

    /// <summary>The choices at the first level, each with the choices nested under it. Required.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<CascaderItem<TValue>> Items { get; set; } = [];

    /// <summary>Lets any choice be the value, not only one at the end of a path. Choosing one that has more under it
    /// also opens its list.</summary>
    [Parameter]
    public bool AllowParents { get; set; }

    /// <summary>Joins the path's choices in the field and in search results. <c>" / "</c> by default.</summary>
    [Parameter]
    public string Separator { get; set; } = " / ";

    /// <summary>Decides whether two values are the same, such as entities matched by id, so a value loaded again
    /// still finds its path. Defaults to the values' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>The most paths a search shows. Defaults to 50; set it to null to show them all.</summary>
    [Parameter]
    public int? MaxItems { get; set; } = 50;

    /// <summary>Text shown while the field is empty. It is not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Shows a button that clears the field.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>The words it shows and says, in place of the app's text for the <see cref="TbStrings"/> keys.
    /// </summary>
    [Parameter]
    public CascaderLabels Labels { get; set; } = new();

    /// <summary>Content before the input, usually an icon.</summary>
    [Parameter]
    public RenderFragment? Start { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private string PopupId => $"{FieldId}-popup";

    // Any text other than the value's own path starts a search; that path, or no text, shows the lists.
    private bool Searching => _text.Length > 0 && _text != PathText(Value);

    private IEnumerable<IReadOnlyList<CascaderItem<TValue>>> Columns =>
        [Items, .. _trail.Select(item => item.Children)];

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    private protected override Task OnResetAsync()
    {
        _shownValue = Value;
        _text = PathText(Value);
        _results = [];
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // New items, such as a reloaded tree, reopen the value's lists and drop the old choices' handlers; if the field
        // showed the value's path, it shows that path in the new items.
        if (!ReferenceEquals(Items, _shownItems))
        {
            var shownPath = _text == _shownPathText;
            _shownItems = Items;
            _pick.KeepOnly(Paths(Items, []).Select(path => path[^1]));
            OpenTrailTo(Value);
            if (shownPath)
            {
                _text = PathText(Value);
            }

            // A search typed before the items arrived, or before they changed, looks again among the new ones.
            Search();
        }

        // A new value from the parent or the user puts its path in the field, and opens its lists.
        if (!SameValue(Value, _shownValue))
        {
            _shownValue = Value;
            _text = PathText(Value);
            OpenTrailTo(Value);
        }

        _shownPathText = PathText(Value);
    }

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
            var module = await Modules.ImportAsync("./_content/Trailblazor/cascader.js");
            if (_disposed)
            {
                return;
            }

            _events = DotNetObjectReference.Create(new CascaderEvents(OnToggleAsync, OpenFromKeysAsync));
            var handle = await module.InvokeAsync<IJSObjectReference?>("createCascader", _input, _popup, _events);
            if (_disposed && handle is not null)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the field went with it.
        }
    }

    private protected override bool SameValue(TValue? value, TValue? other) =>
        OptionValues.Same(Comparer, value, other);

    private protected override string FieldModifiers => ClassNames.Join(base.FieldModifiers, "tb-cascader");

    private bool Ends(CascaderItem<TValue> item) => item.Children.Count == 0;

    private bool Choosable(CascaderItem<TValue> item) => !item.Disabled && (AllowParents || Ends(item));

    // Every step of the path is enabled, and its end may be the value.
    private bool ChoosablePath(IReadOnlyList<CascaderItem<TValue>> path) =>
        path.All(item => !item.Disabled) && (AllowParents || Ends(path[^1]));

    private bool IsChosen(CascaderItem<TValue> item) => OptionValues.Same(Comparer, item.Value, Value);

    private string ItemLabel(CascaderItem<TValue> item) => Ends(item) ? item.Text
        : string.Format(CultureInfo.CurrentCulture, Labels.More ?? Localizer[TbStrings.CascaderMore], item.Text);

    private string PathText(IEnumerable<CascaderItem<TValue>> path) =>
        string.Join(Separator, path.Select(item => item.Text));

    private string PathText(TValue? value) => value is null || PathTo(value) is not { } path ? "" : PathText(path);

    // The choices from the first level down to the one holding this value, or null when no choice holds it.
    private List<CascaderItem<TValue>>? PathTo(TValue value)
    {
        foreach (var path in Paths(Items, []))
        {
            if (OptionValues.Same(Comparer, path[^1].Value, value))
            {
                return path;
            }
        }

        return null;
    }

    // Every path from the first level, depth first, each ending at one choice.
    private static IEnumerable<List<CascaderItem<TValue>>> Paths(IReadOnlyList<CascaderItem<TValue>> items,
        List<CascaderItem<TValue>> above)
    {
        foreach (var item in items)
        {
            List<CascaderItem<TValue>> path = [.. above, item];
            yield return path;
            foreach (var below in Paths(item.Children, path))
            {
                yield return below;
            }
        }
    }

    private void OpenTrailTo(TValue? value)
    {
        _trail.Clear();
        if (value is not null && PathTo(value) is { } path)
        {
            _trail.AddRange(path.Where(item => !Ends(item)));
        }
    }

    private Task OnTypedInputAsync(ChangeEventArgs args) => HandledAsync(() =>
    {
        _text = args.Value as string ?? "";
        Search();
        return Task.CompletedTask;
    });

    private void Search()
    {
        if (!Searching)
        {
            _results = [];
            _announcedCount = null;
            return;
        }

        var found = Paths(Items, []).Where(path => ChoosablePath(path) && TextFold.Contains(PathText(path), _text));
        _results = [.. MaxItems is int most ? found.Take(most) : found];
        _pickPath.KeepOnly(_results);
        // Only a new count is spoken, so typing doesn't bring an announcement with every key.
        if (_results.Count != _announcedCount)
        {
            _announcedCount = _results.Count;
            Announcer.Polite(string.Format(CultureInfo.CurrentCulture,
                Labels.Results ?? Localizer[TbStrings.CascaderResults], _results.Count));
        }
    }

    // A choice with more under it opens its list, and may also become the value; one at the end of a path is chosen.
    private async Task PickAsync(CascaderItem<TValue> item)
    {
        if (ReadOnly || Disabled || item.Disabled)
        {
            return;
        }

        // A click can land on a list that another click has just replaced, so the list is gone.
        var level = Columns.TakeWhile(column => !column.Contains(item)).Count();
        if (level > _trail.Count)
        {
            return;
        }

        _trail.RemoveRange(level, _trail.Count - level);
        if (!Ends(item))
        {
            _trail.Add(item);
        }

        if (Choosable(item))
        {
            await ChooseAsync([.. _trail.Take(level), item]);
        }
    }

    private async Task ChooseAsync(IReadOnlyList<CascaderItem<TValue>> path)
    {
        if (ReadOnly || Disabled)
        {
            return;
        }

        var item = path[^1];
        _text = PathText(path);
        _shownValue = item.Value;
        _results = [];
        _trail.Clear();
        _trail.AddRange(path.Where(step => !Ends(step)));
        await SetValueAsync(item.Value);
    }

    // Leaving the field, or Enter with nothing active, settles what was typed: text that matches a path chooses it,
    // and anything else gives way to the value's own path.
    private Task OnChangedAsync() => HandledAsync(async () =>
    {
        if (ReadOnly || Disabled)
        {
            _text = PathText(Value);
            return;
        }

        if (_text.Length == 0)
        {
            _shownValue = default;
            _trail.Clear();
            await SetValueAsync(default);
            return;
        }

        var match = Paths(Items, []).FirstOrDefault(path => ChoosablePath(path)
            && string.Equals(PathText(path), _text, StringComparison.CurrentCultureIgnoreCase));
        if (match is not null)
        {
            await ChooseAsync(match);
        }
        else
        {
            _text = PathText(Value);
            _results = [];
            await SetValueAsync(Value);
        }
    });

    // The clear button goes away with the text, so focus moves to the input instead of the page.
    private Task OnClearedAsync() => HandledAsync(async () =>
    {
        _text = "";
        _results = [];
        _trail.Clear();
        _shownValue = default;
        await SetValueAsync(default);
        if (!_disposed)
        {
            await _input.FocusAsync();
        }
    });

    // Each user action counts once its handlers have finished, so a test can wait for them.
    private async Task HandledAsync(Func<Task> handle)
    {
        try
        {
            await handle();
        }
        finally
        {
            _handled++;
        }
    }

    // The keys open a choice's list without choosing it, even with AllowParents; only Enter or a click chooses.
    private Task OpenFromKeysAsync(int level, int index)
    {
        var column = Columns.ElementAtOrDefault(level);
        if (column?.ElementAtOrDefault(index) is not { Disabled: false } item || Ends(item))
        {
            return Task.CompletedTask;
        }

        _trail.RemoveRange(level, _trail.Count - level);
        _trail.Add(item);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnToggleAsync(bool open)
    {
        if (!open)
        {
            _announcedCount = null;
        }

        return Task.CompletedTask;
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
