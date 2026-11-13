using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A list whose items the user puts in order, by dragging an item's handle or with the arrow keys on it.
/// The list doesn't move items itself: <see cref="OnMove"/> asks the app to, and <see cref="CanMove"/> can
/// refuse.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed partial class TbSortable<TItem> : TbComponentBase, ISortableGroupMember
{
    private readonly KeyIds _ids = new();
    private ElementReference _list;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<SortableEvents>? _events;
    private ItemMover<TItem>? _mover;
    private ControlTelemetry? _telemetry;
    private bool _disposed;
    // Counts the moves the user asked for that the app has answered, made or refused, so a test can wait for its own.
    private int _answered;

    /// <summary>The items, in the order they show. Apply each <see cref="OnMove"/> to them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>What each item shows beside its handle.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    /// <summary>The list's accessible name, such as "Rules".</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The item's name for its handle's label, which is also said after it moves. Defaults to
    /// <see cref="object.ToString"/>. When it's set, the test helpers find the item by it; otherwise they use the
    /// item's text.</summary>
    [Parameter]
    public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>What tells the items apart as they move, such as an id. Defaults to the item itself.</summary>
    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Raised when the user moves an item; apply it to <see cref="Items"/>, as
    /// <see cref="SortableMove{TItem}.ApplyTo"/> does. A move from another list of the <see cref="Group"/> is raised
    /// here, on the list it goes to, and not on the list it leaves; apply it to both with
    /// <see cref="SortableMove{TItem}.ApplyBetween"/>.</summary>
    [Parameter]
    public EventCallback<SortableMove<TItem>> OnMove { get; set; }

    /// <summary>Asked while an item is dragged over a place and before a move is made. A refusal's reason shows beside
    /// the drop and is read out. A move from another list of the <see cref="Group"/> is checked here, on the list it goes
    /// to, and not on the list it leaves.</summary>
    [Parameter]
    public Func<SortableMove<TItem>, Task<MoveCheck>>? CanMove { get; set; }

    /// <summary>A name shared by the lists an item can move between, such as a board's columns. An item moves to another
    /// list in its group by a drag, by Left or Right on its handle, or from its handle's menu, and
    /// <see cref="ListId"/> tells the lists apart in <see cref="OnMove"/>. The lists must have the same item type.
    /// </summary>
    [Parameter]
    public string? Group { get; set; }

    /// <summary>Names this list in a move's <see cref="SortableMove{TItem}.FromList"/> and
    /// <see cref="SortableMove{TItem}.ToList"/>, such as a column's id. Required with a <see cref="Group"/>, and unique
    /// within it.</summary>
    [Parameter]
    public string? ListId { get; set; }

    /// <summary>Turns off the handles, so the order can't change.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words the move handles show and say to screen readers: their names, hint and menu, and what is
    /// read out after a move. Each one left null uses the app's text.</summary>
    [Parameter]
    public SortableLabels Labels { get; set; } = new();

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

    [Inject]
    private SortableGroups Groups { get; set; } = default!;

    // The group this list last joined, so a new Group leaves the old one.
    private string? _joined;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private ItemMover<TItem> Mover => _mover ??= new(Announcer, Localizer);

    /// <summary>Moves the item at <paramref name="from"/> to <paramref name="to"/>, as the user would, for an app's own
    /// buttons such as "Move up": it asks <see cref="CanMove"/>, raises <see cref="OnMove"/> and says what moved.
    /// Returns whether the item moved.</summary>
    public async Task<bool> MoveAsync(int from, int to)
    {
        var moved = await Mover.MoveAsync(() => Items, from, to, CanMove, OnMove, TextOf, Labels.Moved, ListId);
        if (moved)
        {
            StateHasChanged();
        }

        return moved;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ItemTemplate is null)
        {
            throw new InvalidOperationException($"{nameof(TbSortable<TItem>)} needs an {nameof(ItemTemplate)}.");
        }

        if (Group is not null && string.IsNullOrEmpty(ListId))
        {
            throw new InvalidOperationException($"{nameof(TbSortable<TItem>)} in a {nameof(Group)} needs a "
                + $"{nameof(ListId)}, which names it in moves between the group's lists.");
        }

        if (Group is not null
            && Groups.Lists<ISortableGroupMember>(Group).Any(list => list != this && list.ListId == ListId))
        {
            throw new InvalidOperationException($"{nameof(TbSortable<TItem>)} \"{ListId}\" shares its {nameof(ListId)} "
                + $"with another list of the group \"{Group}\"; give each list its own.");
        }

        if (Group != _joined)
        {
            if (_joined is not null)
            {
                Groups.Leave(_joined, this);
            }

            if (Group is not null)
            {
                Groups.Join(Group, this);
            }

            _joined = Group;
        }
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

            _events = DotNetObjectReference.Create(new SortableEvents(OnDragOverAsync, OnDropAsync, OnKeyAsync,
                OnDragOverListAsync, OnDropListAsync, OnListKeyAsync));
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

    private object KeyOf(TItem item) => KeySelector?.Invoke(item) ?? item!;

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

    private string TextOf(TItem item) => ItemLabel?.Invoke(item) ?? item?.ToString() ?? "";

    // The other lists of this list's group that take its items.
    private IEnumerable<TbSortable<TItem>> Peers =>
        Group is null ? [] : Groups.Lists<TbSortable<TItem>>(Group).Where(list => list != this);

    private TbSortable<TItem>? Peer(string listId) => Peers.FirstOrDefault(list => list.ListId == listId);

    // Where an item dropped before or after the target lands in this list, or at its end with no target.
    private int? SlotOf(string? targetId, bool before) =>
        targetId is null ? Items.Count : IndexOf(targetId) is >= 0 and var index ? index + (before ? 0 : 1) : null;

    // The handle's menu offers each other list of the group, and choosing one moves the item to the end of that list.
    private IReadOnlyList<MoveTarget> MoveTargets(TItem item) => [.. Peers.Select(peer => new MoveTarget(
        string.Format(CultureInfo.CurrentCulture, Labels.MoveToList ?? Localizer[TbStrings.SortableMoveToList],
            peer.Label), peer.Label, peer.Disabled,
        args => UserTransferAsync(IndexOf(IdOf(item)), peer, peer.Items.Count,
            args.Detail > 0 ? "pointer" : "keyboard")))];

    private string? GroupHint => Group is null ? null : Localizer[TbStrings.SortableGroupHint];

    private string HandleLabel(TItem item) =>
        string.Format(CultureInfo.CurrentCulture, Labels.Handle ?? Localizer[TbStrings.SortableHandle], TextOf(item));

    private Task<MoveCheck> OnDragOverAsync(string id, string targetId, bool before) =>
        !Disabled && ItemMover<TItem>.DropIndex(IndexOf(id), IndexOf(targetId), before) is { } to
            ? Mover.CheckAsync(Items, IndexOf(id), to, CanMove, ListId) : Task.FromResult(new MoveCheck(false));

    private Task OnDropAsync(string id, string targetId, bool before) =>
        ItemMover<TItem>.DropIndex(IndexOf(id), IndexOf(targetId), before) is { } to
            ? UserMoveAsync(IndexOf(id), to, "pointer") : Task.CompletedTask;

    private Task OnKeyAsync(string id, string key) =>
        IndexOf(id) is >= 0 and var index && ItemMover<TItem>.TargetOf(key, index, Items.Count) is { } to
            ? UserMoveAsync(index, to, "keyboard") : Task.CompletedTask;

    private Task<MoveCheck> OnDragOverListAsync(string id, string listId, string? targetId, bool before) =>
        !Disabled && Peer(listId) is { Disabled: false } peer && peer.SlotOf(targetId, before) is { } to
            ? ItemMover<TItem>.CheckAsync(Items, IndexOf(id), ListId, peer.Items, to, peer.ListId, peer.CanMove)
            : Task.FromResult(new MoveCheck(false));

    private Task OnDropListAsync(string id, string listId, string? targetId, bool before) =>
        Peer(listId) is { } peer && peer.SlotOf(targetId, before) is { } to
            ? UserTransferAsync(IndexOf(id), peer, to, "pointer") : Task.CompletedTask;

    // Left and Right keep the item at its place in the previous or next list, or put it at the end of a shorter one.
    private Task OnListKeyAsync(string id, string listId) =>
        IndexOf(id) is >= 0 and var index && Peer(listId) is { } peer
            ? UserTransferAsync(index, peer, Math.Min(index, peer.Items.Count), "keyboard") : Task.CompletedTask;

    private async Task UserTransferAsync(int from, TbSortable<TItem> peer, int to, string input)
    {
        try
        {
            if (Disabled || peer.Disabled || !await Mover.MoveAsync(() => Items, from, ListId, () => peer.Items, to,
                peer.ListId, peer.CanMove, peer.OnMove, (item, count) => string.Format(CultureInfo.CurrentCulture,
                    Labels.MovedToList ?? Localizer[TbStrings.SortableMovedToList], TextOf(item), to + 1, count,
                    peer.Label)))
            {
                return;
            }

            Telemetry.Used(TelemetryName, "move", input);
            if (input == "keyboard" && to < peer.Items.Count)
            {
                await peer.FocusHandleAsync(peer.Items[to]);
            }
        }
        finally
        {
            _answered++;
            StateHasChanged();
        }
    }

    // A move chosen from the handle's menu names the item, which is found where it is now.
    private Task UserMoveAsync(TItem item, int to, string input) => UserMoveAsync(IndexOf(IdOf(item)), to, input);

    // Blazor may move the item's element to its new place, taking focus off its handle, so the handle gets it back.
    private async Task UserMoveAsync(int from, int to, string input)
    {
        try
        {
            if (Disabled || !await MoveAsync(from, to))
            {
                return;
            }

            Telemetry.Used(TelemetryName, "move", input);
            if (input == "keyboard")
            {
                await FocusHandleAsync(Items[to]);
            }
        }
        finally
        {
            _answered++;
            StateHasChanged();
        }
    }

    // Another list joined or left the group, which the handles' menus offer.
    void ISortableGroupMember.GroupChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task FocusHandleAsync(TItem item)
    {
        if (_handle is null)
        {
            return;
        }

        try
        {
            await _handle.InvokeVoidAsync("focusHandle", IdOf(item));
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the list went with it.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_joined is not null)
        {
            Groups.Leave(_joined, this);
        }

        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
