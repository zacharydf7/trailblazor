using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Two lists side by side for moving items between those available and those chosen, such as the hikers on
/// a trip. Buttons between the lists move the items marked in one list to the other, or every item it shows. Bind it
/// with <c>@bind-Value</c> to an <see cref="IReadOnlyList{T}"/> of the chosen items, which keep the order of
/// <see cref="Items"/>.</summary>
public partial class TbTransferList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TItem>
{
    private TbMultiListBox<TItem>? _available;
    private IReadOnlyList<TItem> _markedAvailable = [];
    private IReadOnlyList<TItem> _markedChosen = [];
    private string _searchAvailable = "";
    private string _searchChosen = "";
    private readonly ItemHandlers<TItem, Func<Task>> _moveToChosen;
    private readonly ItemHandlers<TItem, Func<Task>> _moveToAvailable;

    /// <summary>Creates the transfer list.</summary>
    public TbTransferList()
    {
        _moveToChosen = new(item => () => MoveToChosenAsync([item]));
        _moveToAvailable = new(item => () => MoveToAvailableAsync([item]));
    }

    /// <summary>Every item the lists can hold, each one distinct. The chosen items, in <c>Value</c>, are taken from
    /// these; a chosen value that matches no item stays chosen, at the end, though neither list shows it.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>The text each item shows and is searched by. Defaults to the item's own text.</summary>
    [Parameter]
    public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Decides whether two items are the same, such as entities matched by id, so a chosen item loaded again
    /// still matches its item. Defaults to the items' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TItem>? Comparer { get; set; }

    private protected override bool SameValue(IReadOnlyList<TItem>? value, IReadOnlyList<TItem>? other) =>
        SameItems(value, other, Comparer);

    /// <summary>Adds a search box above each list; the list then shows only the items whose text contains what's
    /// typed. Moving every item moves only the ones shown.</summary>
    [Parameter]
    public bool Searchable { get; set; }

    /// <summary>The size of the lists, their search boxes and the buttons. Defaults to
    /// <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>How many items each list shows before it scrolls.</summary>
    [Parameter]
    public int Rows { get; set; } = 8;

    /// <summary>Replaces the lists' titles.</summary>
    [Parameter]
    public TransferListLabels? Labels { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) =>
        _available?.FocusAsync(preventScroll) ?? ValueTask.CompletedTask;

    private bool Editable => !Disabled && !ReadOnly;

    private string AvailableTitle => Labels?.Available ?? Localizer[TbStrings.TransferListAvailable];

    private string ChosenTitle => Labels?.Chosen ?? Localizer[TbStrings.TransferListChosen];

    private IEnumerable<TItem> AvailableShown => Matching(Items.Where(item => !IsChosen(item)), _searchAvailable);

    private IEnumerable<TItem> ChosenShown => Matching(Items.Where(IsChosen), _searchChosen);

    private bool IsChosen(TItem item) => Value?.Any(chosen => Same(chosen, item)) ?? false;

    private bool Same(TItem one, TItem other) => (Comparer ?? EqualityComparer<TItem>.Default).Equals(one, other);

    private string TextOf(TItem item) => ItemText?.Invoke(item) ?? Convert.ToString(item, CultureInfo.CurrentCulture)
        ?? "";

    private IEnumerable<TItem> Matching(IEnumerable<TItem> items, string search) => string.IsNullOrWhiteSpace(search)
        ? items : items.Where(item => TextOf(item).Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    // Marked items that a search hides or a move takes away aren't moved again.
    private IReadOnlyList<TItem> Shown(IReadOnlyList<TItem> marked, IEnumerable<TItem> shown) =>
        [.. marked.Where(item => shown.Any(other => Same(other, item)))];

    private Task MoveToChosenAsync(IEnumerable<TItem> moving) => MoveAsync(moving, toChosen: true);

    private Task MoveToAvailableAsync(IEnumerable<TItem> moving) => MoveAsync(moving, toChosen: false);

    private async Task MoveAsync(IEnumerable<TItem> moving, bool toChosen)
    {
        var moved = moving.ToList();
        if (!Editable || moved.Count == 0)
        {
            return;
        }

        bool Moved(TItem item) => moved.Any(other => Same(other, item));
        var chosen = Items.Where(item => toChosen ? IsChosen(item) || Moved(item) : IsChosen(item) && !Moved(item))
            .Concat((Value ?? []).Where(value => !Items.Any(item => Same(item, value))));
        Telemetry.Used(TelemetryName, "move");
        _markedAvailable = [];
        _markedChosen = [];
        await SetValueAsync([.. chosen]);
        Announcer.Polite(Format(Localizer.Plural(TbStrings.TransferListMoved, moved.Count), moved.Count,
            toChosen ? ChosenTitle : AvailableTitle));
    }
}
