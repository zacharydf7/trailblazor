using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A board of columns whose cards move within a column and between columns, by dragging a card's handle or
/// with the keys on it: Up and Down within the column, Left and Right to the column before or after. The board
/// doesn't move cards itself: <see cref="OnMove"/> asks the app to, and <see cref="CanMove"/> or a full column can
/// refuse.</summary>
/// <typeparam name="TItem">The type of the cards.</typeparam>
public partial class TbKanban<TItem>
{
    private readonly string _id = $"tb-kanban-{Guid.NewGuid():N}";
    // The columns' sortable lists share this group, so cards move only between this board's columns.
    private readonly string _group = $"tb-kanban-{Guid.NewGuid():N}";
    private ElementReference _board;
    private IJSObjectReference? _edges;
    private bool _disposed;
    private HashSet<string> _collapsed = [];
    // The collapsed columns last given, so a parent's render that passes the same collection leaves the user's
    // toggles alone.
    private IReadOnlyCollection<string>? _givenCollapsed;

    /// <summary>Names the board for screen readers, such as "Trail work". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The columns, left to right, each with its cards. Apply each <see cref="OnMove"/> to them. With
    /// <see cref="Lanes"/>, they give only their headings and limits, and the lanes hold the cards.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<KanbanColumn<TItem>> Columns { get; set; } = [];

    /// <summary>Swimlanes: rows across the columns, such as a team or a priority, each with its cards in each column.
    /// With them, each column's cards come from the lanes rather than its own <see cref="KanbanColumn{TItem}.Cards"/>,
    /// each lane's cell of a column is a list of its own, and a move's lists are the cells'
    /// <see cref="KanbanLane{TItem}.CellId"/>s. A card's Left and Right stay in its lane. An empty list shows no lanes.
    /// </summary>
    [Parameter]
    public IReadOnlyList<KanbanLane<TItem>>? Lanes { get; set; }

    /// <summary>Draws a card. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> CardTemplate { get; set; } = default!;

    /// <summary>A card's name, which its move handle and the announcements use, such as a task's title. Without it, the
    /// card's <see cref="object.ToString"/> is used.</summary>
    [Parameter]
    public Func<TItem, string>? CardLabel { get; set; }

    /// <summary>A card's identity, so a card keeps its place in the page as cards move. Without it, the card itself is
    /// the key.</summary>
    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Raised when the user moves a card. Within a column, apply it with
    /// <see cref="SortableMove{TItem}.ApplyTo"/>; between columns, its <see cref="SortableMove{TItem}.FromList"/> and
    /// <see cref="SortableMove{TItem}.ToList"/> are the columns' <see cref="KanbanColumn{TItem}.Id"/>s, and
    /// <see cref="SortableMove{TItem}.ApplyBetween"/> applies it. With <see cref="Lanes"/>, they are the cells'
    /// <see cref="KanbanLane{TItem}.CellId"/>s, and <see cref="SortableMove{TItem}.ApplyBetween"/> applies any move,
    /// such as to another lane of the same column.</summary>
    [Parameter]
    public EventCallback<SortableMove<TItem>> OnMove { get; set; }

    /// <summary>Asked while a card is dragged over a place and before a move is made, after the board's own check that
    /// the column isn't full. A refusal's reason shows beside the drop and is read out.</summary>
    [Parameter]
    public Func<SortableMove<TItem>, Task<MoveCheck>>? CanMove { get; set; }

    /// <summary>Drawn at the foot of each column, such as a button that adds a card to it.</summary>
    [Parameter]
    public RenderFragment<KanbanColumn<TItem>>? ColumnFooterTemplate { get; set; }

    /// <summary>Shows a button in each column's header that collapses the column to its title and count, and opens it
    /// again. A collapsed column shows no cards and no <see cref="ColumnFooterTemplate"/>, and no card can move into it
    /// until it's open.</summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>The <see cref="KanbanColumn{TItem}.Id"/>s of the collapsed columns. Bind it with
    /// <c>@bind-CollapsedColumns</c> to keep them, such as in the user's settings, or pass the same collection on each
    /// render. Set <see cref="Collapsible"/> too, so people can open them.</summary>
    [Parameter]
    public IReadOnlyCollection<string>? CollapsedColumns { get; set; }

    /// <summary>Raised with the collapsed columns' ids each time a column collapses or opens.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> CollapsedColumnsChanged { get; set; }

    /// <summary>Turns off the cards' move handles, so no card can move.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words it shows and reads out, in place of the app's text for the <see cref="TbStrings"/> keys.
    /// </summary>
    [Parameter]
    public KanbanLabels Labels { get; set; } = new();

    /// <summary>A label this board's use is counted under, such as <c>"trail-work"</c>, so the app can see how people
    /// use it through the <c>Trailblazor</c> meter: counted in <c>trailblazor.control.shown</c> when it first renders,
    /// and in <c>trailblazor.control.used</c> each time a card moves, with the action <c>move</c>, or a column
    /// collapses or opens, with <c>collapse</c> or <c>expand</c>. Which card or column is never recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private SortableGroups Groups { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private bool IsFull(KanbanColumn<TItem> column) => CountOf(column) >= column.Limit;

    private int CountOf(KanbanColumn<TItem> column) =>
        ShownLanes?.Sum(lane => CardsIn(lane, column).Count) ?? column.Cards.Count;

    private static IReadOnlyList<TItem> CardsIn(KanbanLane<TItem> lane, KanbanColumn<TItem> column) =>
        lane.Cards.GetValueOrDefault(column.Id) ?? [];

    private static string CellLabel(KanbanColumn<TItem> column, KanbanLane<TItem> lane) =>
        $"{column.Title}, {lane.Title}";

    // An empty list of lanes shows the columns as a board without lanes does.
    private IReadOnlyList<KanbanLane<TItem>>? ShownLanes => Lanes is { Count: > 0 } ? Lanes : null;

    // The lanes' count sets the grid's rows: the headers, one per lane, then the footers.
    private string? BoardStyle => Styles.Join(ShownLanes is { } lanes
        ? $"--_tb-kanban-lanes: {lanes.Count.ToString(CultureInfo.InvariantCulture)}" : null, Style);

    private string CountText(KanbanColumn<TItem> column) => column.Limit is { } limit
        ? string.Format(CultureInfo.CurrentCulture, Labels.Count ?? Localizer[TbStrings.KanbanCount],
            CountOf(column), limit)
        : CountOf(column).ToString(CultureInfo.CurrentCulture);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbKanban<TItem>)} needs an {nameof(AriaLabel)}.");
        }

        // A cell's id joins its lane's and column's with a slash, so an id with one could name two cells.
        if (ShownLanes is { } lanes && lanes.Select(lane => lane.Id).Concat(Columns.Select(column => column.Id))
            .FirstOrDefault(id => id.Contains('/')) is { } slashed)
        {
            throw new InvalidOperationException(
                $"{nameof(TbKanban<TItem>)} with {nameof(Lanes)} can't take the id \"{slashed}\": lane and column ids "
                + "can't hold a slash.");
        }

        if (!ReferenceEquals(CollapsedColumns, _givenCollapsed))
        {
            _givenCollapsed = CollapsedColumns;
            _collapsed = [.. CollapsedColumns ?? []];
        }

        OrderColumns();
    }

    // The cards' menus offer the columns in the board's order, even one that opens after the others rendered.
    private void OrderColumns() => Groups.Order(_group, ShownLanes is not { } lanes
        ? Columns.Select(column => column.Id)
        : lanes.SelectMany(lane => Columns.Select(column => lane.CellId(column.Id))));

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        Telemetry.Rendered(TelemetryName);
        try
        {
            // The script marks the sides of the board that have more columns past them, which the board fades.
            var module = await Modules.ImportAsync("./_content/Trailblazor.Primitives/scroll-edges.js");
            if (_disposed)
            {
                return;
            }

            var edges = await module.InvokeAsync<IJSObjectReference?>("markScrollEdges", _board);
            if (edges is not null && _disposed)
            {
                await edges.DisposeHandleAsync();
                return;
            }

            _edges = edges;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the board went with it.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_edges is not null)
        {
            await _edges.DisposeHandleAsync();
        }
    }

    // A move within a column never fills it, even to another lane; one from another column does once the column holds
    // its limit.
    private async Task<MoveCheck> CheckAsync(KanbanColumn<TItem> column, SortableMove<TItem> move)
    {
        if (ColumnOf(move.FromList) != ColumnOf(move.ToList) && IsFull(column))
        {
            return MoveCheck.Refuse(string.Format(CultureInfo.CurrentCulture,
                Labels.Full ?? Localizer[TbStrings.KanbanFull], column.Title, column.Limit));
        }

        return CanMove is null ? MoveCheck.Allow : await CanMove(move);
    }

    private string? ColumnOf(string? list) => ShownLanes is not { } lanes ? list : Columns.FirstOrDefault(column =>
        lanes.Any(lane => lane.CellId(column.Id) == list))?.Id;

    private string ToggleLabel(KanbanColumn<TItem> column) => string.Format(CultureInfo.CurrentCulture,
        Labels.Cards ?? Localizer[TbStrings.KanbanCards], column.Title);

    private async Task ToggleAsync(KanbanColumn<TItem> column)
    {
        var collapsing = _collapsed.Add(column.Id);
        if (!collapsing)
        {
            _collapsed.Remove(column.Id);
        }

        Telemetry.Used(TelemetryName, collapsing ? "collapse" : "expand");
        OrderColumns();
        await CollapsedColumnsChanged.InvokeAsync([.. _collapsed]);
    }

    private async Task MoveAsync(SortableMove<TItem> move)
    {
        Telemetry.Used(TelemetryName, "move");
        await OnMove.InvokeAsync(move);
    }
}
