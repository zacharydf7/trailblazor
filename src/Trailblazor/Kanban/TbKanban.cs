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

    /// <summary>Names the board for screen readers, such as "Trail work". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The columns, left to right, each with its cards. Apply each <see cref="OnMove"/> to them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<KanbanColumn<TItem>> Columns { get; set; } = [];

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
    /// <see cref="SortableMove{TItem}.ApplyBetween"/> applies it.</summary>
    [Parameter]
    public EventCallback<SortableMove<TItem>> OnMove { get; set; }

    /// <summary>Asked while a card is dragged over a place and before a move is made, after the board's own check that
    /// the column isn't full. A refusal's reason shows beside the drop and is read out.</summary>
    [Parameter]
    public Func<SortableMove<TItem>, Task<MoveCheck>>? CanMove { get; set; }

    /// <summary>Drawn at the foot of each column, such as a button that adds a card to it.</summary>
    [Parameter]
    public RenderFragment<KanbanColumn<TItem>>? ColumnFooterTemplate { get; set; }

    /// <summary>Turns off the cards' move handles, so no card can move.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words it shows and reads out, in place of the app's text for the <see cref="TbStrings"/> keys.
    /// </summary>
    [Parameter]
    public KanbanLabels Labels { get; set; } = new();

    /// <summary>A label this board's use is counted under, such as <c>"trail-work"</c>, so the app can see how people
    /// use it through the <c>Trailblazor</c> meter: counted in <c>trailblazor.control.shown</c> when it first renders,
    /// and in <c>trailblazor.control.used</c> each time a card moves. Which card is never recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private static bool IsFull(KanbanColumn<TItem> column) => column.Cards.Count >= column.Limit;

    private string CountText(KanbanColumn<TItem> column) => column.Limit is { } limit
        ? string.Format(CultureInfo.CurrentCulture, Labels.Count ?? Localizer[TbStrings.KanbanCount],
            column.Cards.Count, limit)
        : column.Cards.Count.ToString(CultureInfo.CurrentCulture);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbKanban<TItem>)} needs an {nameof(AriaLabel)}.");
        }
    }

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

    // A move within a column never fills it; one from another column does once the column holds its limit.
    private async Task<MoveCheck> CheckAsync(KanbanColumn<TItem> column, SortableMove<TItem> move)
    {
        if (move.FromList != move.ToList && IsFull(column))
        {
            return MoveCheck.Refuse(string.Format(CultureInfo.CurrentCulture,
                Labels.Full ?? Localizer[TbStrings.KanbanFull], column.Title, column.Limit));
        }

        return CanMove is null ? MoveCheck.Allow : await CanMove(move);
    }

    private async Task MoveAsync(SortableMove<TItem> move)
    {
        Telemetry.Used(TelemetryName, "move");
        await OnMove.InvokeAsync(move);
    }
}
