using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Shows items as rows of a table, with columns that sort, pages, and rows that can be selected. Give it
/// <see cref="Items"/> held in memory, or a <see cref="DataProvider"/> that loads a page at a time, and its columns as
/// <see cref="TbColumn{TItem}"/>s.</summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TbDataGrid<TItem> : IAsyncDisposable
{
    private readonly List<TbColumn<TItem>> _columns = [];
    private readonly Dictionary<object, TItem> _selection = [];
    private readonly Dictionary<object, ElementReference> _rowRefs = [];
    private readonly KeyIds _ids = new();
    private List<TItem> _rows = [];
    private List<GridSort> _sorts = [];
    private int _total;
    private int _page = 1;
    private int? _pageSize;
    private bool _loading;
    private bool _failed;
    private string? _loaded;
    private CancellationTokenSource? _load;
    private IReadOnlyList<GridSort>? _sortsFromParent;
    private int? _pageFromParent;
    private int? _pageSizeFromParent;
    private IReadOnlyCollection<TItem>? _selectedFromParent;
    private TItem? _selectedItemFromParent;
    private (object Key, TaskCompletionSource Done)? _scrollAfterRender;
    private ElementReference _table;
    private ElementReference _body;
    private IJSObjectReference? _handle;
    private IJSObjectReference? _reorder;
    private DotNetObjectReference<DataGridEvents>? _events;
    private DotNetObjectReference<SortableEvents>? _moveEvents;
    private ItemMover<TItem>? _mover;
    private bool _reorderable;
    private bool _disposed;
    private bool _columnsChanged;
    private ControlTelemetry? _telemetry;
    private int _columnVersion;
    private int _rowVersion;
    private object? _arrangedFor;
    private object? _rowOptions;

    /// <summary>The rows, held in memory. The grid sorts and pages them itself, again when this is a new collection;
    /// call <see cref="RefreshAsync"/> after changing the same collection or its items in place.</summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Loads the rows a page at a time, sorted as asked, in place of <see cref="Items"/>. A new request
    /// cancels the last one. Call <see cref="RefreshAsync"/> to load again, such as when a filter changes.</summary>
    [Parameter]
    public Func<GridRequest, CancellationToken, Task<GridResult<TItem>>>? DataProvider { get; set; }

    /// <summary>The columns, as <see cref="TbColumn{TItem}"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gives each item a key that stays the same across loads, such as its id, so selection follows the item.
    /// Defaults to the item itself, which suits items compared by value, such as records; each row's key must differ.
    /// A row's <c>data-key</c> is the key's text when it is a string, number, date or other formattable value, and a
    /// made-up id such as <c>key-3</c> otherwise, or when that text is empty or another key already has it.</summary>
    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Names the table for screen readers.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>How many rows can be selected. Defaults to <see cref="SelectionMode.None"/>, since a grid's rows are
    /// read more often than chosen; a tree view, whose focus selects, defaults to one.</summary>
    [Parameter]
    public SelectionMode SelectionMode { get; set; }

    /// <summary>The selected items. Bind it with <c>@bind-SelectedItems</c>.</summary>
    [Parameter]
    public IReadOnlyCollection<TItem>? SelectedItems { get; set; }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>The selected item of a <see cref="SelectionMode.Single"/> grid. Bind it with
    /// <c>@bind-SelectedItem</c>.</summary>
    [Parameter]
    public TItem? SelectedItem { get; set; }

    /// <summary>Raised when the selected item changes.</summary>
    [Parameter]
    public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Lets a click on a row select it, or in a <see cref="SelectionMode.Multiple"/> grid select or
    /// unselect it. Defaults to true.</summary>
    [Parameter]
    public bool RowClickSelects { get; set; } = true;

    /// <summary>Raised when a row is clicked anywhere but on a button, link or field in it. Only a pointer can click a
    /// row, so give anything this does a button in a cell too.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowClick { get; set; }

    /// <summary>Names a row's selection box for screen readers, as "Select" and this. Defaults to the first column's
    /// text. Call <see cref="RenderRows"/> when what it reads from outside the item changes.</summary>
    [Parameter]
    public Func<TItem, string>? RowLabel { get; set; }

    /// <summary>Extra CSS classes for a row. Call <see cref="RenderRows"/> when what it reads from outside the item
    /// changes, such as which row is current.</summary>
    [Parameter]
    public Func<TItem, string?>? RowClass { get; set; }

    /// <summary>The columns the rows sort by, in order. Bind it with <c>@bind-Sorts</c> to sort from code or to save
    /// it.</summary>
    [Parameter]
    public IReadOnlyList<GridSort>? Sorts { get; set; }

    /// <summary>Raised when the user sorts the rows.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<GridSort>> SortsChanged { get; set; }

    /// <summary>How many rows a page holds. Null, the default, shows every row with no pages.</summary>
    [Parameter]
    public int? PageSize { get; set; }

    /// <summary>Raised when the user picks another page size.</summary>
    [Parameter]
    public EventCallback<int?> PageSizeChanged { get; set; }

    /// <summary>The page sizes the user can pick from, beside the pages.</summary>
    [Parameter]
    public IReadOnlyList<int>? PageSizes { get; set; }

    /// <summary>The current page, from 1. Bind it with <c>@bind-Page</c>.</summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>Raised when the page changes.</summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>Shades every other row.</summary>
    [Parameter]
    public bool Striped { get; set; }

    /// <summary>How tall the rows are. <see cref="Size.Small"/> fits more rows. Defaults to <see cref="Size.Medium"/>.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>The height the rows scroll in, as CSS, such as <c>"24rem"</c>; the header stays in view above them.
    /// </summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Shows the rows as loading, such as while the page fetches <see cref="Items"/>.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Shown instead of the rows when there are none.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Raised when the <see cref="DataProvider"/> fails; the grid offers to try again.</summary>
    [Parameter]
    public EventCallback<Exception> OnLoadFailed { get; set; }

    /// <summary>The words the grid shows and says to screen readers.</summary>
    [Parameter]
    public DataGridLabels Labels { get; set; } = new();

    /// <summary>Lets the user reorder rows held in <see cref="Items"/>, by dragging a row's handle or with the arrow
    /// keys on it, while the grid isn't sorted. Raised with the move; apply it to the items, as
    /// <see cref="SortableMove{TItem}.ApplyTo"/> does, and the grid shows the new order. Each row's handle is a menu
    /// with a script of its own, so give a grid of many rows a <see cref="PageSize"/>.</summary>
    [Parameter]
    public EventCallback<SortableMove<TItem>> OnRowMove { get; set; }

    /// <summary>Asked while a row is dragged over a place and before a move is made. A refusal's reason shows beside
    /// the drop and is read out.</summary>
    [Parameter]
    public Func<SortableMove<TItem>, Task<MoveCheck>>? CanMoveRow { get; set; }

    /// <summary>A label the grid's use is counted under, such as <c>"jobs"</c>, so the app can see how people use it
    /// through the <c>Trailblazor</c> meter: shown when it renders, and used with the action <c>sort</c>, <c>page</c>,
    /// <c>page_size</c>, <c>select</c> or <c>move</c> each time the user does one. Which column, page or rows is never
    /// recorded. It names the grid, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private IBrowserService Browser { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private int PageCount => _pageSize is { } size ? Math.Max(1, (_total + size - 1) / size) : 1;

    private int ColumnCount => _columns.Count + (SelectionMode == SelectionMode.None ? 0 : 1) + (CanReorder ? 1 : 0);

    // Rows move among all the items, so only rows held in memory, in their own order, can be moved.
    private bool CanReorder => OnRowMove.HasDelegate && Items is not null && _sorts.Count == 0;

    private ItemMover<TItem> Mover => _mover ??= new(Announcer, Localizer);

    private bool IsLoading => Loading || _loading;

    private string LoadFailedLabel => Labels.LoadFailed ?? Localizer[TbStrings.DataGridLoadFailed];

    private string EmptyLabel => Labels.Empty ?? Localizer[TbStrings.DataGridEmpty];

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Items is not null && DataProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(TbDataGrid<TItem>)} takes {nameof(Items)} or a {nameof(DataProvider)}, not both.");
        }

        // The grid's own sorts passed back, as a binding does, keep them, so the rows aren't sorted again for nothing.
        if (!ReferenceEquals(Sorts, _sortsFromParent))
        {
            _sortsFromParent = Sorts;
            if (!(Sorts ?? []).SequenceEqual(_sorts))
            {
                _sorts = [.. Sorts ?? []];
            }
        }

        if (Page != _pageFromParent)
        {
            _pageFromParent = Page;
            _page = Math.Max(1, Page);
        }

        if (PageSize != _pageSizeFromParent)
        {
            _pageSizeFromParent = PageSize;
            _pageSize = PageSize is > 0 ? PageSize : null;
        }

        // Rows draw again for a new collection, whose items may have changed in place, and for options read through
        // delegates only when the method changes, since markup makes a new delegate on every render.
        object rowOptions = (Items, SelectionMode, RowClass?.Method, RowLabel?.Method);
        if (!rowOptions.Equals(_rowOptions))
        {
            _rowOptions = rowOptions;
            _rowVersion++;
        }

        TakeSelectionFromParent();
        await ShowAsync(force: false);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _columnsChanged = false;
        Telemetry.Rendered(TelemetryName);
        try
        {
            if (firstRender && RendererInfo.IsInteractive)
            {
                await AttachAsync();
            }

            if (_moveEvents is null && OnRowMove.HasDelegate && RendererInfo.IsInteractive)
            {
                await AttachReorderAsync();
            }

            if (_scrollAfterRender is { } scroll)
            {
                _scrollAfterRender = null;
                try
                {
                    if (_rows.Any(shown => Equals(KeyOf(shown), scroll.Key))
                        && _rowRefs.TryGetValue(scroll.Key, out var row))
                    {
                        await Browser.ScrollIntoViewAsync(row);
                    }
                }
                finally
                {
                    scroll.Done.TrySetResult();
                }
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the grid went with it.
        }
    }

    /// <summary>Loads the rows again from the <see cref="DataProvider"/>, or sorts and pages <see cref="Items"/> again
    /// after they changed in place.</summary>
    public async Task RefreshAsync()
    {
        _rowVersion++;
        await ShowAsync(force: true);
        StateHasChanged();
    }

    /// <summary>Draws every row again. A row draws again by itself only when its item, its selection or the columns
    /// change, so call this when a <see cref="TbColumn{TItem}.Cell"/> template, <see cref="RowClass"/> or
    /// <see cref="RowLabel"/> shows something else that changed, such as which row is being edited.</summary>
    public void RenderRows()
    {
        _rowVersion++;
        StateHasChanged();
    }

    /// <summary>Goes to the page that holds this item's row, found by its <see cref="KeySelector"/> key, and scrolls
    /// the row into view. With a <see cref="DataProvider"/>, only a row on the current page can be found.</summary>
    public async Task ScrollToAsync(TItem item)
    {
        var key = KeyOf(item);
        if (_pageSize is { } size && Items is not null
            && Arranged().ToList().FindIndex(other => Equals(KeyOf(other), key)) is var index and >= 0)
        {
            await GoToPageAsync(index / size + 1);
        }

        _scrollAfterRender?.Done.TrySetResult();
        var done = new TaskCompletionSource();
        _scrollAfterRender = (key, done);
        StateHasChanged();
        await done.Task;
    }

    // Each row is a component of its own, so one row can draw again without the rest; it's added here because markup
    // can't name an internal component.
    private void Rows(RenderTreeBuilder builder)
    {
        for (var index = 0; index < _rows.Count; index++)
        {
            var item = _rows[index];
            var key = KeyOf(item);
            var selected = IsSelected(item);
            builder.OpenComponent<GridRow<TItem>>(0);
            builder.SetKey(key);
            builder.AddComponentParameter(1, nameof(GridRow<TItem>.Item), item);
            builder.AddComponentParameter(2, nameof(GridRow<TItem>.Selected), selected);
            builder.AddComponentParameter(3, nameof(GridRow<TItem>.Version), _rowVersion);
            builder.AddComponentParameter(4, nameof(GridRow<TItem>.ChildContent),
                Row(item, key, selected, Offset + index));
            builder.CloseComponent();
        }
    }

    internal void AddColumn(TbColumn<TItem> column)
    {
        _columns.Add(column);
        StateHasChanged();
    }

    // A sort names its column by key, so two columns with one key would sort and mark each other. Checked as the grid
    // renders, once every column has its parameters, since columns reordered in a loop change one at a time.
    private void CheckColumnKeys()
    {
        if (_columns.GroupBy(column => column.Key).FirstOrDefault(keys => keys.Count() > 1) is { } clash)
        {
            throw new InvalidOperationException($"{nameof(TbDataGrid<TItem>)} has two columns named \"{clash.Key}\"; " +
                $"give one of them an {nameof(TbColumn<TItem>.Id)}.");
        }
    }

    internal void RemoveColumn(TbColumn<TItem> column)
    {
        if (!_disposed && _columns.Remove(column))
        {
            _columnVersion++;
            _rowVersion++;
            StateHasChanged();
        }
    }

    // A column that reads a value made anew on every render reports a change on each one, so the grid renders again
    // for its columns at most once until that render has finished, and sorts its rows again as it does.
    internal void ColumnChanged()
    {
        _columnVersion++;
        _rowVersion++;
        if (!_columnsChanged)
        {
            _columnsChanged = true;
            StateHasChanged();
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/data-grid.js");
        if (_disposed)
        {
            return;
        }

        _events = DotNetObjectReference.Create(new DataGridEvents(OnRowClickedAsync));
        var handle = await module.InvokeAsync<IJSObjectReference?>("createDataGrid", _table, _events);
        if (handle is not null && _disposed)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
        StateHasChanged();
    }

    // Rows can move once the app handles OnRowMove, which it may start doing after the first render.
    private async Task AttachReorderAsync()
    {
        _moveEvents = DotNetObjectReference.Create(new SortableEvents(OnRowDragOverAsync, OnRowDropAsync,
            OnRowKeyAsync));
        var sortable = await Modules.ImportAsync("./_content/Trailblazor/sortable.js");
        var reorder = _disposed ? null
            : await sortable.InvokeAsync<IJSObjectReference?>("attachSortable", _body, _moveEvents,
                "tr[data-part=\"row\"]");
        if (reorder is not null && _disposed)
        {
            await reorder.DisposeHandleAsync();
            return;
        }

        _reorder = reorder;
        StateHasChanged();
    }

    // Tests wait for data-ready, so it waits for whatever hears their clicks and keys.
    private bool IsReady => _handle is not null && (!OnRowMove.HasDelegate || _reorder is not null);

    // Rows that can move count among all the items, which they show in order, so a row's place follows from its page.
    private int Offset => _pageSize is { } size ? (_page - 1) * size : 0;

    // The handles come and go as the grid is sorted, and rows draw again only when told to, so they are told to.
    private void DrawRowsForReorder()
    {
        if (CanReorder != _reorderable)
        {
            _reorderable = CanReorder;
            _rowVersion++;
        }
    }

    private IReadOnlyList<TItem> AllItems => Items as IReadOnlyList<TItem> ?? [.. Items ?? []];

    // Rows are named by their key's id, since a key pressed before the last move shows would name a stale place.
    private int AllIndexOf(string id) => _rows.FindIndex(row => IdOf(row) == id) is >= 0 and var at
        ? AllIndexOf(_rows[at]) : -1;

    private int AllIndexOf(TItem item)
    {
        var key = KeyOf(item);
        var items = AllItems;
        for (var index = 0; index < items.Count; index++)
        {
            if (Equals(KeyOf(items[index]), key))
            {
                return index;
            }
        }

        return -1;
    }

    private Task<MoveCheck> OnRowDragOverAsync(string id, string targetId, bool before) =>
        CanReorder && ItemMover<TItem>.DropIndex(AllIndexOf(id), AllIndexOf(targetId), before) is { } to
            ? Mover.CheckAsync(AllItems, AllIndexOf(id), to, CanMoveRow) : Task.FromResult(new MoveCheck(false));

    private Task OnRowDropAsync(string id, string targetId, bool before) =>
        ItemMover<TItem>.DropIndex(AllIndexOf(id), AllIndexOf(targetId), before) is { } to
            ? MoveRowAsync(AllIndexOf(id), to, "pointer") : Task.CompletedTask;

    private Task OnRowKeyAsync(string id, string key) =>
        AllIndexOf(id) is >= 0 and var from && ItemMover<TItem>.TargetOf(key, from, AllItems.Count) is { } to
            ? MoveRowAsync(from, to, "keyboard") : Task.CompletedTask;

    // A move chosen from a row's handle menu names the row's item, which is found where it is now.
    private Task MoveRowAsync(TItem item, int to, string input) => MoveRowAsync(AllIndexOf(item), to, input);

    // The app moves the item in its own list, often in place, so the rows are arranged again, on the page the row
    // went to. A key press keeps focus on the moved row's handle.
    private async Task MoveRowAsync(int from, int to, string input)
    {
        var items = AllItems;
        if (!CanReorder || from < 0 || !await Mover.MoveAsync(items, from, to, CanMoveRow, OnRowMove, LabelOf))
        {
            return;
        }

        var moved = AllItems[to];

        Telemetry.Used(TelemetryName, "move", input);
        var page = _pageSize is { } size ? to / size + 1 : 1;
        if (page != _page)
        {
            _page = page;
            await PageChanged.InvokeAsync(_page);
        }

        await RefreshAsync();
        if (input == "keyboard" && _reorder is not null)
        {
            await _reorder.InvokeVoidAsync("focusHandle", IdOf(moved));
        }
    }

    // In memory the rows are sorted and paged here; a provider is asked only when the request differs from the last.
    private async Task ShowAsync(bool force)
    {
        if (DataProvider is null)
        {
            var page = _page;
            Arrange(force);
            if (_page != page)
            {
                await PageChanged.InvokeAsync(_page);
            }

            return;
        }

        var request = new GridRequest((_page - 1) * (_pageSize ?? 0), _pageSize, [.. _sorts]);
        var asked = $"{request.StartIndex}|{request.Count}|{string.Join(",", _sorts)}";
        if (!force && asked == _loaded)
        {
            return;
        }

        _loaded = asked;
        _load?.Cancel();
        var load = _load = new CancellationTokenSource();
        _loading = true;
        _failed = false;
        GridResult<TItem>? result = null;
        try
        {
            result = await DataProvider(request, load.Token);
        }
        // A load a newer one replaced ends quietly, even when its provider reports the cancel its own way.
        catch (Exception) when (load.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            _failed = true;
            ShowRows([]);
            Announcer.Polite(LoadFailedLabel);
            await OnLoadFailed.InvokeAsync(exception);
        }
        finally
        {
            if (_load == load)
            {
                _loading = false;
            }
        }

        // Rows that can't be told apart are the app's mistake, not a failed load, so they throw outside the catch.
        if (result is null || load.IsCancellationRequested)
        {
            return;
        }

        ShowRows([.. result.Items]);
        _total = result.TotalCount;
        if (_total == 0)
        {
            Announcer.Polite(EmptyLabel);
        }

        // Fewer rows than before can leave the page past the last one, so the last page shows instead.
        if (_page > PageCount)
        {
            await GoToPageAsync(PageCount);
        }
    }

    // In-memory rows sort and page again only when something that orders them changed, not on every parent render.
    private void Arrange(bool force)
    {
        if (!force && ArrangedFor.Equals(_arrangedFor))
        {
            return;
        }

        var arranged = Arranged().ToList();
        _total = arranged.Count;
        _page = Math.Min(_page, PageCount);
        ShowRows(_pageSize is { } size ? [.. arranged.Skip((_page - 1) * size).Take(size)] : arranged);
        _arrangedFor = ArrangedFor;
    }

    private object ArrangedFor => (Items, _sorts, _page, _pageSize, _columnVersion);

    // Columns report their changes after the grid renders, so their rows sort again as it next renders, once for all.
    private void ArrangeForColumns()
    {
        if (DataProvider is null)
        {
            Arrange(force: false);
        }
    }

    // Selection follows a row by its key, so no two rows may share one. A row that isn't shown has no element, so its
    // reference goes before anything can scroll to it.
    private void ShowRows(List<TItem> rows)
    {
        var shown = rows.Select(KeyOf).ToHashSet();
        if (shown.Count != rows.Count)
        {
            throw new InvalidOperationException($"{nameof(TbDataGrid<TItem>)} has two rows with the same key; " +
                $"give it a {nameof(KeySelector)} that gives each item a different key, such as its id.");
        }

        _rows = rows;
        _ids.Keep(shown);
        foreach (var gone in _rowRefs.Keys.Where(key => !shown.Contains(key)).ToList())
        {
            _rowRefs.Remove(gone);
        }
    }

    private IEnumerable<TItem> Arranged()
    {
        IEnumerable<TItem> items = Items ?? [];
        IOrderedEnumerable<TItem>? sorted = null;
        foreach (var sort in _sorts)
        {
            if (_columns.FirstOrDefault(column => column.Key == sort.Column) is not { CanSort: true } column)
            {
                continue;
            }

            Func<TItem, object?> key = column.SortValueOf;
            sorted = (sorted, sort.Direction) switch
            {
                (null, SortDirection.Ascending) => items.OrderBy(key, Comparer<object?>.Default),
                (null, _) => items.OrderByDescending(key, Comparer<object?>.Default),
                (_, SortDirection.Ascending) => sorted.ThenBy(key, Comparer<object?>.Default),
                _ => sorted.ThenByDescending(key, Comparer<object?>.Default),
            };
        }

        return sorted ?? items;
    }

    // A plain click sorts by the column alone, then turns it around, then stops; Shift adds it to the sorts instead.
    private async Task SortAsync(TbColumn<TItem> column, MouseEventArgs args)
    {
        Telemetry.Used(TelemetryName, "sort");
        var at = _sorts.FindIndex(sort => sort.Column == column.Key);
        List<GridSort> sorts = [.. _sorts];
        if (!args.ShiftKey && !(at == 0 && _sorts.Count == 1))
        {
            sorts.Clear();
            at = -1;
        }

        if (at < 0)
        {
            sorts.Add(new(column.Key, SortDirection.Ascending));
        }
        else if (sorts[at].Direction == SortDirection.Ascending)
        {
            sorts[at] = sorts[at] with { Direction = SortDirection.Descending };
        }
        else
        {
            sorts.RemoveAt(at);
        }

        _sorts = sorts;
        var pageMoved = _page != 1;
        _page = 1;
        // The load starts before the app hears of the change, so a handler that waits can't leave the table looking
        // loaded while the new rows are still on their way.
        var show = ShowAsync(force: false);
        await SortsChanged.InvokeAsync(_sorts);
        if (pageMoved)
        {
            await PageChanged.InvokeAsync(1);
        }

        Announcer.Polite(sorts.FirstOrDefault(sort => sort.Column == column.Key) is { } now
            ? Format(now.Direction == SortDirection.Ascending ? TbStrings.DataGridSortedAscending
                : TbStrings.DataGridSortedDescending, column.Heading)
            : Format(TbStrings.DataGridUnsorted, column.Heading));
        await show;
    }

    private async Task GoToPageAsync(int page)
    {
        _page = Math.Clamp(page, 1, PageCount);
        var show = ShowAsync(force: false);
        await PageChanged.InvokeAsync(_page);
        await show;
    }

    // Code moves the page too, as ScrollToAsync does, so only the pager's moves count as the user's.
    private Task PageFromPagerAsync(int page)
    {
        Telemetry.Used(TelemetryName, "page");
        return GoToPageAsync(page);
    }

    private async Task ChangePageSizeAsync(int size)
    {
        Telemetry.Used(TelemetryName, "page_size");
        _pageSize = size;
        _page = 1;
        var show = ShowAsync(force: false);
        await PageSizeChanged.InvokeAsync(size);
        await PageChanged.InvokeAsync(1);
        await show;
    }

    private void TakeSelectionFromParent()
    {
        if (SelectionMode == SelectionMode.Single && !EqualityComparer<TItem>.Default.Equals(SelectedItem,
                _selectedItemFromParent))
        {
            _selectedItemFromParent = SelectedItem;
            _selection.Clear();
            if (SelectedItem is { } item)
            {
                _selection[KeyOf(item)] = item;
            }
        }

        if (!ReferenceEquals(SelectedItems, _selectedFromParent))
        {
            _selectedFromParent = SelectedItems;
            _selection.Clear();
            foreach (var item in SelectedItems ?? [])
            {
                _selection[KeyOf(item)] = item;
            }
        }
    }

    private bool IsSelected(TItem item) => _selection.ContainsKey(KeyOf(item));

    private async Task SetSelectionAsync(IEnumerable<TItem> items)
    {
        // The items may be a query over the selection, so they are read before it is cleared.
        List<TItem> chosen = [.. items];
        var before = _selection.Keys.ToHashSet();
        _selection.Clear();
        foreach (var item in chosen)
        {
            _selection[KeyOf(item)] = item;
        }

        // Only the user's clicks get here, and only a selection that changed counts.
        if (!before.SetEquals(_selection.Keys))
        {
            Telemetry.Used(TelemetryName, "select");
        }

        IReadOnlyCollection<TItem> selected = [.. _selection.Values];
        await SelectedItemsChanged.InvokeAsync(selected);
        await SelectedItemChanged.InvokeAsync(selected.FirstOrDefault());
    }

    private Task ToggleAsync(TItem item) => SelectionMode == SelectionMode.Single
        ? SetSelectionAsync([item])
        : SetSelectionAsync(IsSelected(item) ? _selection.Values.Where(other => !Equals(KeyOf(other), KeyOf(item)))
            : [.. _selection.Values, item]);

    // The header box selects every row on the page, or unselects them when all already are.
    private Task ToggleAllAsync() => AllSelected
        ? SetSelectionAsync(_selection.Values.Where(item => !_rows.Any(row => Equals(KeyOf(row), KeyOf(item)))))
        : SetSelectionAsync([.. _selection.Values, .. _rows.Where(row => !IsSelected(row))]);

    private bool AllSelected => _rows.Count > 0 && _rows.All(IsSelected);

    private bool SomeSelected => !AllSelected && _rows.Any(IsSelected);

    private async Task OnRowClickedAsync(string id)
    {
        if (_rows.FirstOrDefault(row => IdOf(row) == id) is not { } item)
        {
            return;
        }

        await OnRowClick.InvokeAsync(item);
        if (RowClickSelects && SelectionMode != SelectionMode.None)
        {
            await ToggleAsync(item);
        }

        StateHasChanged();
    }

    private object KeyOf(TItem item) => KeySelector?.Invoke(item) ?? item!;

    private string IdOf(TItem item) => _ids.IdOf(KeyOf(item));

    private string LabelOf(TItem item) => RowLabel?.Invoke(item)
        ?? _columns.Select(column => column.TextOf(item)).FirstOrDefault(text => text is not null) ?? IdOf(item);

    private string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Localizer[key], args);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _load?.Cancel();
        _scrollAfterRender?.Done.TrySetResult();
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        if (_reorder is not null)
        {
            await _reorder.DisposeHandleAsync();
        }

        _events?.Dispose();
        _moveEvents?.Dispose();
    }
}
