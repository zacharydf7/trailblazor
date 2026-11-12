using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A list whose items the user puts in order, by dragging an item's handle or with the arrow keys on it.
/// The list doesn't move items itself: <see cref="OnMove"/> asks the app to, and <see cref="CanMove"/> can
/// refuse.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed partial class TbSortable<TItem> : TbComponentBase
{
    private readonly KeyIds _ids = new();
    private ElementReference _list;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<SortableEvents>? _events;
    private ItemMover<TItem>? _mover;
    private ControlTelemetry? _telemetry;
    private bool _disposed;

    /// <summary>The items, in the order they show. Apply each <see cref="OnMove"/> to them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>What each item shows beside its handle.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    /// <summary>The list's accessible name, such as "Rules".</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The item's text, which names its handle and is said after it moves. Defaults to
    /// <see cref="object.ToString"/>.</summary>
    [Parameter]
    public Func<TItem, string>? ItemText { get; set; }

    /// <summary>What tells the items apart as they move, such as an id. Defaults to the item itself.</summary>
    [Parameter]
    public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Raised when the user moves an item; apply it to <see cref="Items"/>, as
    /// <see cref="SortableMove{TItem}.ApplyTo"/> does.</summary>
    [Parameter]
    public EventCallback<SortableMove<TItem>> OnMove { get; set; }

    /// <summary>Asked while an item is dragged over a place and before a move is made. A refusal's reason shows beside
    /// the drop and is read out.</summary>
    [Parameter]
    public Func<SortableMove<TItem>, Task<MoveCheck>>? CanMove { get; set; }

    /// <summary>Turns off the handles, so the order can't change.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>A label the list's use is counted under, such as <c>"rule-order"</c>, through the <c>Trailblazor</c>
    /// meter: shown when it renders, and used with the action <c>move</c> and the input <c>pointer</c> or
    /// <c>keyboard</c> each time the user moves an item. Which item is never recorded. It names the list, never the
    /// user.</summary>
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

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private ItemMover<TItem> Mover => _mover ??= new(Announcer, Localizer);

    /// <summary>Moves the item at <paramref name="from"/> to <paramref name="to"/>, as the user would, for an app's own
    /// buttons such as "Move up": it asks <see cref="CanMove"/>, raises <see cref="OnMove"/> and says what moved.
    /// Returns whether the item moved.</summary>
    public async Task<bool> MoveAsync(int from, int to)
    {
        var moved = await Mover.MoveAsync(() => Items, from, to, CanMove, OnMove, TextOf);
        if (moved)
        {
            StateHasChanged();
        }

        return moved;
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
            var module = await Modules.ImportAsync("./_content/Trailblazor/sortable.js");
            if (_disposed)
            {
                return;
            }

            _events = DotNetObjectReference.Create(new SortableEvents(OnDragOverAsync, OnDropAsync, OnKeyAsync));
            var handle = await module.InvokeAsync<IJSObjectReference?>("attachSortable", _list, _events);
            if (handle is not null && _disposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the list went with it.
        }
    }

    private object KeyOf(TItem item) => ItemKey?.Invoke(item) ?? item!;

    private string IdOf(TItem item) => _ids.IdOf(KeyOf(item));

    private int IndexOf(string id)
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (IdOf(Items[index]) == id)
            {
                return index;
            }
        }

        return -1;
    }

    private string TextOf(TItem item) => ItemText?.Invoke(item) ?? item?.ToString() ?? "";

    private string HandleLabel(TItem item) =>
        string.Format(CultureInfo.CurrentCulture, Localizer[TbStrings.SortableHandle], TextOf(item));

    private Task<MoveCheck> OnDragOverAsync(string id, string targetId, bool before) =>
        !Disabled && ItemMover<TItem>.DropIndex(IndexOf(id), IndexOf(targetId), before) is { } to
            ? Mover.CheckAsync(Items, IndexOf(id), to, CanMove) : Task.FromResult(new MoveCheck(false));

    private Task OnDropAsync(string id, string targetId, bool before) =>
        ItemMover<TItem>.DropIndex(IndexOf(id), IndexOf(targetId), before) is { } to
            ? UserMoveAsync(IndexOf(id), to, "pointer") : Task.CompletedTask;

    private Task OnKeyAsync(string id, string key) =>
        IndexOf(id) is >= 0 and var index && ItemMover<TItem>.TargetOf(key, index, Items.Count) is { } to
            ? UserMoveAsync(index, to, "keyboard") : Task.CompletedTask;

    // A move chosen from the handle's menu names the item, which is found where it is now.
    private Task UserMoveAsync(TItem item, int to, string input) => UserMoveAsync(IndexOf(IdOf(item)), to, input);

    // Blazor may move the item's element to its new place, taking focus off its handle, so the handle gets it back.
    private async Task UserMoveAsync(int from, int to, string input)
    {
        if (Disabled || !await MoveAsync(from, to))
        {
            return;
        }

        Telemetry.Used(TelemetryName, "move", input);
        if (input == "keyboard" && _handle is not null)
        {
            try
            {
                await _handle.InvokeVoidAsync("focusHandle", IdOf(Items[to]));
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, and the list went with it.
            }
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

        _events?.Dispose();
    }
}
