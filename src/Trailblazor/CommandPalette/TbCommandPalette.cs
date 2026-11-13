using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A dialog that finds a command or a page as the user types, opened from anywhere with a shortcut such as
/// Ctrl+K. Its search matches the letters in order, so "nwtb" finds "New tab", and puts the best matches first.
/// </summary>
/// <typeparam name="TItem">The type of the commands.</typeparam>
public sealed partial class TbCommandPalette<TItem> : TbComponentBase, IAsyncDisposable
{
    private readonly string _id = $"tb-command-palette-{Guid.NewGuid():N}";
    private readonly ItemHandlers<TItem, Func<MouseEventArgs, Task>> _choose;
    private ElementReference _marker;
    private string _query = "";
    private List<Result> _results = [];
    private List<Group> _groups = [];
    private bool _open;
    private bool _openParameter;
    private int? _announcedCount;
    private int _chosen;
    private IJSObjectReference? _module;
    private IJSObjectReference? _shortcut;
    private string? _attachedShortcut;
    private int _attaches;
    private DotNetObjectReference<CommandPaletteEvents>? _events;
    private ControlTelemetry? _telemetry;
    private bool _disposed;

    /// <summary>Creates the command palette.</summary>
    public TbCommandPalette() => _choose = new(item => args => ChooseAsync(item, args));

    /// <summary>The commands and pages to search, in the order they show before the user types.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>A command's name, which the search matches. Defaults to <see cref="object.ToString"/>.</summary>
    [Parameter]
    public Func<TItem, string>? ItemText { get; set; }

    /// <summary>The heading a command shows under, such as "Pages" or "Actions". Commands with the same group show
    /// together.</summary>
    [Parameter]
    public Func<TItem, string?>? ItemGroup { get; set; }

    /// <summary>Other words that find a command, such as "settings" for "Preferences". A match on these ranks below one
    /// on the name.</summary>
    [Parameter]
    public Func<TItem, IEnumerable<string>>? ItemKeywords { get; set; }

    /// <summary>A line shown under a command's name.</summary>
    [Parameter]
    public Func<TItem, string?>? ItemDescription { get; set; }

    /// <summary>An icon shown before a command's name.</summary>
    [Parameter]
    public Func<TItem, Icon?>? ItemIcon { get; set; }

    /// <summary>The keys that run a command elsewhere in the app, such as <c>"Ctrl+S"</c>, shown beside it as a hint.
    /// </summary>
    [Parameter]
    public Func<TItem, string?>? ItemShortcut { get; set; }

    /// <summary>Whether a command shows but can't be chosen.</summary>
    [Parameter]
    public Func<TItem, bool>? ItemDisabled { get; set; }

    /// <summary>A page that choosing the command goes to, after <see cref="OnChoose"/> has run.</summary>
    [Parameter]
    public Func<TItem, string?>? ItemHref { get; set; }

    /// <summary>Draws a command in place of its icon, name, description and shortcut.</summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Raised when the user chooses a command, once the palette has closed.</summary>
    [Parameter]
    public EventCallback<TItem> OnChoose { get; set; }

    /// <summary>Whether the palette is open. Bind it with <c>@bind-Open</c> to open it from a button as well as the
    /// shortcut; it turns false when the user chooses a command or dismisses the palette.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the user opens or closes the palette: with the shortcut, by choosing a command or by
    /// dismissing it.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The keys that open and close the palette anywhere on the page, such as <c>"Mod+K"</c>, the default:
    /// Mod is ⌘ on Apple devices and Ctrl elsewhere, and Ctrl, Alt, Shift and Meta can be named too. Null leaves the
    /// palette to <see cref="Open"/>.</summary>
    [Parameter]
    public string? Shortcut { get; set; } = "Mod+K";

    /// <summary>The palette's accessible name. Defaults to the app's text for <see
    /// cref="TbStrings.CommandPaletteLabel"/>.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The search box's placeholder. Defaults to the app's text for <see
    /// cref="TbStrings.CommandPalettePlaceholder"/>.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The words the palette shows and says to screen readers. Each one left null uses the app's text.
    /// </summary>
    [Parameter]
    public CommandPaletteLabels Labels { get; set; } = new();

    /// <summary>A label the palette's use is counted under, such as <c>"app-commands"</c>, through the
    /// <c>Trailblazor</c> meter: shown when it renders, and used with the action <c>choose</c> and the input
    /// <c>pointer</c> or <c>keyboard</c> each time the user chooses a command. Which command is never recorded. It
    /// names the palette, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private string LabelText => Label ?? Localizer[TbStrings.CommandPaletteLabel];

    private string InputId => $"{_id}-input";

    private string ListboxId => $"{_id}-listbox";

    private static readonly HashSet<string> Modifiers =
        new(["Mod", "Ctrl", "Alt", "Shift", "Meta"], StringComparer.OrdinalIgnoreCase);

    private static readonly DialogOptions DialogSettings =
        new() { Position = DialogPosition.Top, ShowHeader = false, Size = DialogSize.Medium };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Shortcut?.Split('+') is [.. var modifiers, var key]
            && (key.Trim().Length == 0 || modifiers.Any(modifier => !Modifiers.Contains(modifier.Trim()))))
        {
            throw new InvalidOperationException($"{nameof(TbCommandPalette<TItem>)} can't read the shortcut "
                + $"\"{Shortcut}\": end it with one key, after any of Mod, Ctrl, Alt, Shift and Meta, as in "
                + "\"Mod+K\" or \"Ctrl+Shift+P\".");
        }

        if (Open != _openParameter)
        {
            _openParameter = Open;
            if (Open != _open)
            {
                SetOpen(Open);
            }
        }

        if (_open)
        {
            Find();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        if (Shortcut == _attachedShortcut)
        {
            return;
        }

        // The app can change the shortcut, such as to keys the user set, so the old listener is replaced. A render
        // during the wait may change it again, even back, and then only the latest render's handle is kept.
        var wanted = _attachedShortcut = Shortcut;
        var attach = ++_attaches;
        try
        {
            if (_shortcut is { } previous)
            {
                _shortcut = null;
                await previous.DisposeHandleAsync();
            }

            if (wanted is null)
            {
                return;
            }

            _module ??= await Modules.ImportAsync("./_content/Trailblazor/command-palette.js");
            // Nothing waits between here and the call, so attaches reach the browser in the order they were counted.
            if (_disposed || attach != _attaches)
            {
                return;
            }

            _events ??= DotNetObjectReference.Create(new CommandPaletteEvents(OnShortcutAsync));
            var handle = await _module.InvokeAsync<IJSObjectReference?>("attachShortcut", _marker, wanted, _events);
            if (handle is not null && (_disposed || attach != _attaches))
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _shortcut = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the palette went with it.
        }
    }

    private string TextOf(TItem item) => ItemText?.Invoke(item) ?? item?.ToString() ?? "";

    private bool IsDisabled(TItem item) => ItemDisabled?.Invoke(item) == true;

    private async Task OnShortcutAsync()
    {
        SetOpen(!_open);
        StateHasChanged();
        await OpenChanged.InvokeAsync(_open);
    }

    private async Task OnDialogOpenChangedAsync(bool open)
    {
        if (!open && _open)
        {
            SetOpen(false);
            await OpenChanged.InvokeAsync(false);
        }
    }

    // Each opening starts from an empty search, and says nothing about the results until the user types.
    private void SetOpen(bool open)
    {
        _open = open;
        if (open)
        {
            _query = "";
            Find();
            _announcedCount = _results.Count;
        }
    }

    private void SetQuery(string? query)
    {
        _query = query ?? "";
        Find();
        // Only a new count is spoken, so typing doesn't bring an announcement with every key.
        if (_results.Count != _announcedCount)
        {
            _announcedCount = _results.Count;
            Announcer.Polite(_results.Count == 0 ? NoResultsText : string.Format(CultureInfo.CurrentCulture,
                Labels.Results ?? Localizer[TbStrings.CommandPaletteResults], _results.Count));
        }
    }

    private string NoResultsText => Labels.NoResults ?? Localizer[TbStrings.CommandPaletteNoResults];

    // With no search the commands keep their order. Otherwise the best matches come first, ties keeping their order,
    // and each group shows where its best match ranks.
    private void Find()
    {
        var found = new List<Result>();
        var searching = !string.IsNullOrWhiteSpace(_query);
        foreach (var item in Items)
        {
            if (!searching)
            {
                found.Add(new(item, [], 0));
                continue;
            }

            var match = FuzzyMatch.Find(TextOf(item), _query);
            var keywords = ItemKeywords?.Invoke(item) is { } words ? FuzzyMatch.Find(string.Join(' ', words), _query)
                : null;
            if (match is null && keywords is null)
            {
                continue;
            }

            var score = Math.Max(match?.Score ?? int.MinValue, keywords is { } other ? other.Score / 2 : int.MinValue);
            found.Add(new(item, match?.Places ?? [], score));
        }

        _groups = [.. found.OrderByDescending(result => result.Score).GroupBy(result => ItemGroup?.Invoke(result.Item))
            .Select(group => new Group(group.Key, [.. group]))];
        _results = [.. _groups.SelectMany(group => group.Results)];
        _choose.KeepOnly(_results.Select(result => result.Item));
    }

    // The palette closes first, so a dialog the command opens returns focus to where the user was, not to the closed
    // palette.
    private async Task ChooseAsync(TItem item, MouseEventArgs args)
    {
        if (!_open || IsDisabled(item))
        {
            return;
        }

        SetOpen(false);
        try
        {
            await OpenChanged.InvokeAsync(false);
            Telemetry.Used(TelemetryName, "choose", args.Detail > 0 ? "pointer" : "keyboard");
            await OnChoose.InvokeAsync(item);
            if (ItemHref?.Invoke(item) is { } href)
            {
                Navigation.NavigateTo(href);
            }
        }
        finally
        {
            _chosen++;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_shortcut is not null)
        {
            await _shortcut.DisposeHandleAsync();
        }

        _events?.Dispose();
    }

    private sealed record Result(TItem Item, int[] Places, int Score);

    private sealed record Group(string? Name, List<Result> Results);
}
