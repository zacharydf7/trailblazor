using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Trailblazor.Internal.Events;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Shows items as rows of a table, with sortable columns, paging and row selection. Give it
/// <see cref="Items"/> held in memory, or a <see cref="DataProvider"/> that loads a page at a time, and its columns as
/// <see cref="TbColumn{TItem}"/>s.</summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TbDataGrid<TItem> : IAsyncDisposable
{
    private readonly List<TbColumn<TItem>> _columns = [];
    private readonly List<object> _members = [];
    private List<TbColumn<TItem>> _arrangedColumns = [];
    private List<TbColumn<TItem>> _shownColumns = [];
    private readonly Dictionary<object, TItem> _selection = [];
    private readonly Dictionary<object, ElementReference> _rowRefs = [];
    private readonly KeyIds _ids = new();
    private List<TItem> _rows = [];
    // The lines the grid draws, its rows and its groups' headings, each with its place among all the lines.
    private List<GridLine> _lines = [];
    // The index, among all rows, of the first row a virtualized grid's provider last loaded.
    private int _rowsStart;
    private Virtualize<GridLine>? _virtualize;
    private List<GridSort> _sorts = [];
    private List<GridFilter> _filters = [];
    private List<string> _groups = [];
    // A group is open until the user closes it.
    private readonly HashSet<GroupKey> _closedGroups = [];
    // A tree grid's open rows, by key. While filters apply, a row with matches under it is open unless the user closed
    // it, and new filters open every such row again.
    private readonly Dictionary<object, TItem> _openParents = [];
    private IReadOnlyCollection<TItem>? _openParentsFromParent;
    private readonly HashSet<object> _closedWhileFiltered = [];
    private object? _closedWhileFilteredFor;
    // Each tree row's parent, by key, so a row can be shown by opening the rows it's nested under.
    private readonly Dictionary<object, TItem> _parentOf = [];
    private int _treeVersion;
    // How many rows the filters leave; unlike _total, it doesn't count group headings.
    private int _matched;
    private int _total;
    private int _page = 1;
    private int? _pageSize;
    private bool _loading;
    private bool _failed;
    private string? _loaded;
    private CancellationTokenSource? _load;
    private IReadOnlyList<GridSort>? _sortsFromParent;
    private IReadOnlyList<GridFilter>? _filtersFromParent;
    private IReadOnlyList<string>? _groupsFromParent;
    private int? _pageFromParent;
    private int? _pageSizeFromParent;
    private GridState? _stateFromParent;
    private Dictionary<string, double> _widths = [];
    private List<string> _order = [];
    private List<string> _hidden = [];
    private Dictionary<string, object?> _totals = [];
    private string _aggregatesAsked = "";
    private string? _focusColumnMenu;
    private bool _focusColumnChooser;
    private string? _measureColumn;
    private bool _measuring;
    // A provider's first load waits for the columns, for their totals and for the types JSON filters need.
    private bool _showForColumns;
    private IReadOnlyCollection<TItem>? _selectedFromParent;
    // A Shift range runs from its anchor to where it has reached, on top of the selection it started with, so it can
    // shrink again.
    private object? _rangeAnchor;
    private List<TItem> _rangeBase = [];
    private TItem? _selectedItemFromParent;
    private (object Key, TaskCompletionSource Done)? _scrollAfterRender;
    private TaskCompletionSource? _nextRender;
    private GridEdit? _edit;
    private int _editVersion;
    private ElementReference _table;
    private ElementReference _body;
    private IJSObjectReference? _handle;
    private IJSObjectReference? _reorder;
    private DotNetObjectReference<SortableEvents>? _moveEvents;
    private ItemMover<TItem>? _mover;
    private bool _reorderable;
    private bool _disposed;
    private bool _createdWaitingForReorder;
    private readonly ItemHandlers<TbColumn<TItem>, Func<MouseEventArgs, Task>> _sort;
    private readonly ItemHandlers<object, Func<RowClickEventArgs, Task>> _rowClick;
    private readonly ItemHandlers<object, Func<RowExtendEventArgs, Task>> _rowExtend;
    private readonly ItemHandlers<object, Func<Task>> _choose;
    private readonly ItemHandlers<object, Func<Task>> _toggleDetail;
    private readonly ItemHandlers<object, Func<Task>> _toggleChildren;
    private readonly Dictionary<object, TItem> _expanded = [];
    // A batch grid's changes by row key and column, in the order first made, then why the app last turned them down,
    // and the save in flight with the cells it sent.
    private readonly OrderedDictionary<(object Key, string Column), GridCellEdit<TItem>> _changes = [];
    private string? _batchError;
    private Task? _batchSave;
    private HashSet<(object Key, string Column)> _sending = [];
    private readonly string _changedId = $"tb-grid-changed-{Guid.NewGuid():N}";
    private readonly string _detailPrefix = $"tb-grid-detail-{Guid.NewGuid():N}";
    private IReadOnlyCollection<TItem>? _expandedFromParent;
    private readonly ItemHandlers<(object, TbColumn<TItem>), Func<Task>> _startEdit;
    private readonly ItemHandlers<GroupKey, Action> _toggleGroup;
    private readonly ItemHandlers<TbColumn<TItem>, Func<KeyboardEventArgs, Task>> _applyOnEnter;
    private bool _columnsChanged;
    private ControlTelemetry? _telemetry;
    private int _columnVersion;
    private int _rowVersion;
    // Counts the row moves and batch saves the user asked for that the app has answered, done or refused, so a test can
    // wait for its own.
    private int _answered;
    private object? _arrangedFor;
    private object? _rowOptions;

    /// <summary>Creates the data grid.</summary>
    public TbDataGrid()
    {
        _sort = new(column => args => SortAsync(column, args));
        _rowClick = new(key => args => OnRowAsync(key, item => OnRowClickedAsync(item, args)), byValue: true);
        _rowExtend = new(key => args => OnRowAsync(key, item => OnRowExtendedAsync(item, args)), byValue: true);
        _choose = new(key => () => OnRowAsync(key, ChooseAsync), byValue: true);
        _toggleDetail = new(key => () => OnRowAsync(key, ToggleDetailAsync), byValue: true);
        _toggleChildren = new(key => () => OnRowAsync(key, ToggleChildrenAsync), byValue: true);
        _startEdit = new(cell => () => OnRowAsync(cell.Item1, item => StartEditAsync(item, cell.Item2)));
        _toggleGroup = new(key => () => ToggleGroup(key), byValue: true);
        _applyOnEnter = new(column => args => ApplyOnEnterAsync(column, args));
    }

    /// <summary>The rows, held in memory. The grid filters, sorts and pages them itself, again when this is a new
    /// collection; call <see cref="RefreshAsync"/> after changing the same collection or its items in place.</summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Loads the rows a page at a time, filtered and sorted as asked, in place of <see cref="Items"/>. A new
    /// request cancels the last one. Call <see cref="RefreshAsync"/> to load again, such as after the page changes a
    /// filter of its own.</summary>
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
    /// read more often than chosen; a tree view defaults to one.</summary>
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

    /// <summary>Raised when a row is clicked anywhere but on a button, link or field in it. With cell navigation, which
    /// <see cref="CellNavigation"/> or an editable column turns on, Enter on a cell with nothing to go into raises it too.
    /// Without it, only a pointer can click a row, so give anything this does a button in a cell too.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowClick { get; set; }

    /// <summary>Names a row's selection box for screen readers, as "Select" and this. Defaults to the first column's
    /// text. A row whose label changes draws again, even when it reads from outside the item.</summary>
    [Parameter]
    public Func<TItem, string>? RowLabel { get; set; }

    /// <summary>Extra CSS classes for a row. A row whose classes change draws again, even when they come from outside
    /// the item, such as which row is current. A class that shades the row should also set
    /// <c>--tb-data-grid-row-shade</c> to that color, so the row's pinned cells, which paint a background of their own,
    /// take the shade too.</summary>
    [Parameter]
    public Func<TItem, string?>? RowClass { get; set; }

    /// <summary>Extra CSS classes for the grid's inner parts.</summary>
    [Parameter]
    public DataGridClasses? Classes { get; set; }

    /// <summary>The columns the rows sort by, in order. Bind it with <c>@bind-Sorts</c> to sort from code or to save
    /// it.</summary>
    [Parameter]
    public IReadOnlyList<GridSort>? Sorts { get; set; }

    /// <summary>Raised when the user sorts the rows.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<GridSort>> SortsChanged { get; set; }

    /// <summary>The conditions rows must all meet to show, which the user sets in the filter row of each
    /// <see cref="TbColumn{TItem}.Filterable"/> column. Bind it with <c>@bind-Filters</c> to filter from code or to
    /// save it. A <see cref="DataProvider"/> gets them in its <see cref="GridRequest"/>.</summary>
    [Parameter]
    public IReadOnlyList<GridFilter>? Filters { get; set; }

    /// <summary>Raised when the user changes a filter.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<GridFilter>> FiltersChanged { get; set; }

    /// <summary>Where the <see cref="TbColumn{TItem}.Filterable"/> columns' filters are: a row under the headers, the
    /// default, or a menu on each header.</summary>
    [Parameter]
    public GridFilterMode FilterMode { get; set; }

    /// <summary>The columns the rows are grouped by, outermost first, named by their <see cref="TbColumn{TItem}.Id"/>s.
    /// The user picks them, in order, from the "Group by" list of <see cref="TbColumn{TItem}.Groupable"/> columns; bind
    /// it with <c>@bind-Groups</c> to group from code or to save it. Groups sort by their values, in the column's own
    /// direction when it is sorted, and each starts with a heading that shows its row count and opens or closes the
    /// group. Only rows held in <see cref="Items"/> can be grouped, and with a <see cref="PageSize"/> the
    /// headings count among a page's rows.</summary>
    [Parameter]
    public IReadOnlyList<string>? Groups { get; set; }

    /// <summary>Raised when the user changes which columns group the rows.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> GroupsChanged { get; set; }

    /// <summary>Adds a row under each open group with that group's totals, on the columns with an
    /// <see cref="TbColumn{TItem}.Aggregate"/>, <see cref="TbColumn{TItem}.AggregateBy"/> or
    /// <see cref="TbColumn{TItem}.FooterTemplate"/>, as the totals row under the grid does for every row. Like
    /// grouping, it works only on rows held in <see cref="Items"/>, and with a <see cref="PageSize"/> these rows count
    /// among a page's rows, as the headings do.</summary>
    [Parameter]
    public bool GroupTotals { get; set; }

    /// <summary>Content shown in a row of its own under a row whose details are open, such as a grid of that row's
    /// orders. Each row then starts with a button that opens and closes its details. A grid that virtualizes its rows
    /// can't show details.</summary>
    [Parameter]
    public RenderFragment<TItem>? DetailTemplate { get; set; }

    /// <summary>Keeps one row's details open at a time, so opening another row's closes the first.</summary>
    [Parameter]
    public bool SingleDetail { get; set; }

    /// <summary>The items whose details are open. Bind it with <c>@bind-ExpandedItems</c> to open or close them from
    /// code and hear when the user does.</summary>
    [Parameter]
    public IReadOnlyCollection<TItem>? ExpandedItems { get; set; }

    /// <summary>Raised when the user opens or closes a row's details.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TItem>> ExpandedItemsChanged { get; set; }

    /// <summary>Gives a row's child rows, which makes the grid a tree grid: <see cref="Items"/> are the top rows, and a
    /// row with children starts its first column with a button that shows or hides them. Rows sort among their
    /// siblings, and filters show the rows that match along with the rows they're nested under, opened to show them.
    /// A tree grid takes its rows from <see cref="Items"/>, and can't group, move or page them.</summary>
    [Parameter]
    public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

    /// <summary>The rows of a tree grid whose children show; every row starts closed. Bind it with
    /// <c>@bind-ExpandedParents</c> to open or close rows from code and hear when the user does.</summary>
    [Parameter]
    public IReadOnlyCollection<TItem>? ExpandedParents { get; set; }

    /// <summary>Raised when the user opens or closes a tree grid's row, or <see cref="ScrollToAsync"/> opens rows to
    /// show one. Rows opened or closed while filters apply stay that way only until the filters change, so they don't
    /// raise it.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TItem>> ExpandedParentsChanged { get; set; }

    /// <summary>How many rows a page holds. Null, the default, shows every row with no pages.</summary>
    [Parameter]
    public int? PageSize { get; set; }

    /// <summary>Raised when the user picks another page size.</summary>
    [Parameter]
    public EventCallback<int?> PageSizeChanged { get; set; }

    /// <summary>The page sizes the user can pick from, beside the pages.</summary>
    [Parameter]
    public IReadOnlyList<int>? PageSizes { get; set; }

    /// <summary>Adds buttons for the first and last page to the pager, for grids with many pages.</summary>
    [Parameter]
    public bool ShowFirstLast { get; set; }

    /// <summary>The current page, from 1. Bind it with <c>@bind-Page</c>.</summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>Raised when the page changes.</summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>The rows' sorts, filters, groups, page and page size, and the column widths, in one value. Bind it
    /// with <c>@bind-State</c> to save how the user has arranged the grid, such as to local storage as JSON, and to
    /// restore it later; a state set here replaces all of them. A virtualized grid has no pages, so it ignores the
    /// state's page and page size.</summary>
    [Parameter]
    public GridState? State { get; set; }

    /// <summary>Builds the address for a grid state, such as the page's address with <see
    /// cref="GridState.ToQueryParameters"/> in its query, and turns the sort headers and the pager into links to the
    /// state each one leads to; picking a page size goes to its address too. Sorting and paging then work in static
    /// SSR, and the grid's place can be shared and survives a reload; read it back with <see
    /// cref="GridState.FromQuery"/> and pass its sorts, page and page size to the grid, such as
    /// <c>PageSize="state.PageSize ?? 20"</c>, since a null page size turns paging off. A sort link sorts by its column
    /// alone, since a Shift- or Ctrl-click opens a link in a new window or tab rather than adding a sort.</summary>
    [Parameter]
    public Func<GridState, string>? StateHref { get; set; }

    /// <summary>Raised when the user sorts, filters, groups or pages the rows, picks another page size, or resizes or
    /// moves a column.</summary>
    [Parameter]
    public EventCallback<GridState> StateChanged { get; set; }

    /// <summary>Lets the user resize the columns: by dragging a header's edge, or from its column menu, which the keys
    /// and a single click or tap reach. The first resize fixes every column at the width it has, so the others don't
    /// move; the widths are kept in <see cref="State"/>. A column opts out with
    /// <see cref="TbColumn{TItem}.Resizable"/>.</summary>
    [Parameter]
    public bool ResizableColumns { get; set; }

    /// <summary>Lets the user move the columns: by dragging a header past another, or from its column menu, which the
    /// keys and a single click or tap reach. A pinned column moves only among the columns pinned to its side. The order
    /// is kept in <see cref="State"/>.</summary>
    [Parameter]
    public bool ReorderableColumns { get; set; }

    /// <summary>Lets the user hide and show columns from a Columns menu above the grid, which lists every column, and
    /// hide one from its own menu. At least one column always shows, and a column with
    /// <see cref="TbColumn{TItem}.Hideable"/> off can't be hidden. The hidden columns are kept in <see cref="State"/>.
    /// </summary>
    [Parameter]
    public bool HideableColumns { get; set; }

    /// <summary>Shades every other row.</summary>
    [Parameter]
    public bool Striped { get; set; }

    /// <summary>How tall the rows are. <see cref="Size.Small"/> fits more rows. Defaults to <see cref="Size.Medium"/>.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>The tallest the rows grow before they scroll, as CSS, such as <c>"24rem"</c>; the header stays in view
    /// above them. Fewer rows take less. To fill a container instead, use <see cref="Fill"/>.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Makes the grid as tall as its container, which needs a height of its own, as a full or maximized
    /// dialog's body has. The rows scroll in the room the toolbar and pager leave, under a header that stays in view.
    /// </summary>
    [Parameter]
    public bool Fill { get; set; }

    /// <summary>Draws only the rows scrolled into view, and a few around them, instead of pages, so the grid can hold
    /// thousands of rows. It needs a <see cref="Height"/> or <see cref="Fill"/> for the rows to scroll in, and can't
    /// have a <see cref="PageSize"/>. With a <see cref="DataProvider"/>, rows load as they scroll into view, and the
    /// header has no box that selects every row, since most of them aren't loaded.</summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>Makes the grid an ARIA grid that Tab reaches once: the arrow keys move between its cells, Home and End
    /// to a row's ends, Ctrl+Home and Ctrl+End to its first and last cells, and Page Up and Page Down by a screenful. A
    /// cell holding just a button, link, checkbox or radio button hands focus to it, unless it is a menu's button or a
    /// row's move handle, which use the arrow keys themselves. Enter or F2 moves into any other cell's fields, Tab
    /// cycles through them and Escape comes back out. Enter on a cell with nothing to go into clicks its row. Space
    /// selects or unselects the row of the cell in focus.
    /// </summary>
    [Parameter]
    public bool CellNavigation { get; set; }

    /// <summary>Raised with each edit the user saves in a <see cref="TbColumn{TItem}.Editable"/> column, once its
    /// checks pass; apply it to the item, or save it, and the grid shows the rows again. Without it, an edit is set on
    /// the item when the column's field is a property or field the grid can set.</summary>
    [Parameter]
    public EventCallback<GridCellEdit<TItem>> OnCellEdit { get; set; }

    /// <summary>What one edit covers: a cell, the default, or in <see cref="GridEditMode.Row"/> every
    /// <see cref="TbColumn{TItem}.Editable"/> cell of a row, where Enter saves the row, Escape cancels it, Tab moves
    /// between its fields and Save and Cancel buttons, and leaving the row saves it. In
    /// <see cref="GridEditMode.Batch"/>, cells are edited as in the default, and each edit is kept as a change until
    /// the user saves or discards them all.</summary>
    [Parameter]
    public GridEditMode EditMode { get; set; }

    /// <summary>Raised with each row the user saves when <see cref="EditMode"/> is <see cref="GridEditMode.Row"/>,
    /// once every cell's checks pass; apply it to the item, or save it, and the grid shows the rows again. Without
    /// it, each value is set on the item when its column's field is a property or field the grid can set.</summary>
    [Parameter]
    public EventCallback<GridRowEdit<TItem>> OnRowEdit { get; set; }

    /// <summary>Raised with every change when the user saves the changes of a grid whose <see cref="EditMode"/> is
    /// <see cref="GridEditMode.Batch"/>; apply them to the items, or save them, and the grid shows the rows again.
    /// Without it, each value is set on its item when its column's field is a property or field the grid can set.
    /// A change holds the item it was made on, so if the rows can reload before a save, set <see cref="KeySelector"/>
    /// and look the item up by its key.</summary>
    [Parameter]
    public EventCallback<GridBatchEdit<TItem>> OnBatchSave { get; set; }

    /// <summary>Whether a <see cref="GridEditMode.Batch"/> grid holds changes not yet saved, such as to ask before the
    /// user leaves the page.</summary>
    public bool HasChanges => _changes.Count > 0;

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
    /// keys on it, while the grid isn't sorted or filtered. Raised with the move; apply it to the items, as
    /// <see cref="SortableMove{TItem}.ApplyTo"/> does, and the grid shows the new order. Each row's handle is a menu
    /// with a script of its own, so give a grid of many rows a <see cref="PageSize"/>.</summary>
    [Parameter]
    public EventCallback<SortableMove<TItem>> OnRowMove { get; set; }

    /// <summary>Asked while a row is dragged over a place and before a move is made. A refusal's reason shows beside
    /// the drop and is read out.</summary>
    [Parameter]
    public Func<SortableMove<TItem>, Task<MoveCheck>>? CanMoveRow { get; set; }

    /// <summary>A label the grid's use is counted under, such as <c>"jobs"</c>, so the app can see how people use it
    /// through the <c>Trailblazor</c> meter: shown when it renders, and used with the action <c>sort</c>,
    /// <c>filter</c>, <c>page</c>, <c>page_size</c>, <c>select</c>, <c>move</c>, <c>group</c>, <c>expand</c>,
    /// <c>collapse</c>, <c>edit</c>, <c>open_detail</c>, <c>close_detail</c>, <c>export_csv</c>,
    /// <c>export_excel</c> or <c>export_pdf</c> each time the user does one. Which
    /// column, filter value, page, group, rows or value is never recorded. It names the grid, never the user.</summary>
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
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private int PageCount => _pageSize is { } size ? Math.Max(1, (_total + size - 1) / size) : 1;

    // A provider's rows aren't all loaded, so a box can't select them all.
    private bool CanSelectAll => !(Virtualize && DataProvider is not null);

    // A virtualized table's aria-rowcount and aria-rowindex count its header rows too.
    private int HeaderRowCount => BandDepth + (HasFilterRow ? 2 : 1);

    // How many rows of group headings sit above the column headers.
    private int BandDepth => _shownColumns.Count == 0 ? 0 : _shownColumns.Max(column => column.Bands.Count);

    // A row of group headings: each run of shown columns in the same group, and pinned to the same side, shares one
    // heading, and a column in no group at that level gets a blank cell of its own.
    private List<(TbColumnGroup<TItem>? Group, List<TbColumn<TItem>> Columns)> BandsAt(int level)
    {
        List<(TbColumnGroup<TItem>? Group, List<TbColumn<TItem>> Columns)> bands = [];
        foreach (var column in _shownColumns)
        {
            var group = level < column.Bands.Count ? column.Bands[level] : null;
            if (group is not null && bands is [.., var last] && last.Group == group
                && last.Columns[^1].Pinned == column.Pinned)
            {
                last.Columns.Add(column);
            }
            else
            {
                bands.Add((group, [column]));
            }
        }

        return bands;
    }

    // A heading pinned to the end sticks where its last column does; one pinned to the start, where its first does.
    private string? BandPinStyle(List<TbColumn<TItem>> columns) =>
        PinStyle(columns[0].Pinned == ColumnPin.End ? columns[^1] : columns[0]);

    // Editing a cell or opening a tree's rows, as much as moving between cells, makes the table an ARIA grid.
    private bool IsGrid => CellNavigation || CellSelection || ChildrenSelector is not null || _columns.Any(CanEdit);

    private bool CanEdit(TbColumn<TItem> column) => column.Editor != ColumnEditor.None
        && (HasEditHandler || column.CanSet);

    private bool HasEditHandler => EditMode switch
    {
        GridEditMode.Row => OnRowEdit.HasDelegate,
        GridEditMode.Batch => OnBatchSave.HasDelegate,
        _ => OnCellEdit.HasDelegate,
    };

    // A failed, loading or empty grid shows one row saying so, which a virtualized table counts and numbers too, so
    // the keyboard can reach it.
    private bool ShowsStatus => _failed || HasNoRows;

    // A virtualized grid's provider loads a few rows at a time, so only its count says whether there are any.
    private bool HasNoRows => Virtualize && DataProvider is not null ? _total == 0 : _lines.Count == 0;

    // A grouped grid's lines count headings too, so the rows the filters leave are counted apart.
    private int RowCount => DataProvider is null ? _matched : _total;

    private int ColumnCount => _shownColumns.Count + LeadCount;

    // The cells a row draws, counting the spacers of a grid that draws only the columns in view.
    private int DrawnCount => _drawnColumns.Count + LeadCount;

    private bool HasTotals => _shownColumns.Any(column => column.HasTotal);

    private Dictionary<string, object?> TotalsOf(IReadOnlyList<TItem> rows) =>
        _columns.Where(column => column.HasTotal).ToDictionary(column => column.Key, column => column.TotalOf(rows));

    private IReadOnlyList<GridAggregate> Aggregates =>
    [
        .. _columns.Where(column => column.Aggregate != ColumnAggregate.None)
            .Select(column => new GridAggregate(column.Key, column.Aggregate)),
    ];

    private string AggregatesKey => string.Join(",", Aggregates.Select(asked => $"{asked.Column}:{asked.Aggregate}"));

    private string VirtualLoadKey => $"{string.Join(",", _sorts)}|{FiltersKey}|{AggregatesKey}";

    private string AggregateLabel(ColumnAggregate aggregate) => Localizer[aggregate switch
    {
        ColumnAggregate.Sum => TbStrings.DataGridSum,
        ColumnAggregate.Average => TbStrings.DataGridAverage,
        ColumnAggregate.Count => TbStrings.DataGridCount,
        ColumnAggregate.Min => TbStrings.DataGridMin,
        _ => TbStrings.DataGridMax,
    }];

    // Rows move among all the items, so only rows held in memory, all shown in their own order, can be moved.
    private bool CanReorder => OnRowMove.HasDelegate && Items is not null && _sorts.Count == 0
        && !AppliedFilters.Any() && !AppliedGroups.Any();

    // A group on a column that isn't there, or can't group, is kept but doesn't group the rows.
    private IEnumerable<TbColumn<TItem>> AppliedGroups =>
        from key in _groups
        from column in _columns
        where column.Key == key && column.CanGroup
        select column;

    private bool HasGroupBy => Items is not null && ChildrenSelector is null && _columns.Any(column => column.CanGroup);

    // A filter on a column that isn't there, or can't filter, is kept but hides no rows.
    private IEnumerable<(GridFilter Filter, TbColumn<TItem> Column)> AppliedFilters =>
        from filter in _filters
        from column in _columns
        where column.Key == filter.Column && column.CanFilter
        select (filter, column);

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

        if (DataProvider is not null && Groups is { Count: > 0 })
        {
            throw new InvalidOperationException($"{nameof(TbDataGrid<TItem>)} groups only rows held in " +
                $"{nameof(Items)}, not rows from a {nameof(DataProvider)}.");
        }

        // A tree's rows follow their parents, which a provider's pages, groups or moved rows would break apart, and a
        // page could start with a row whose parent is on the page before.
        if (ChildrenSelector is not null && (DataProvider is not null || Groups is { Count: > 0 } || OnRowMove.HasDelegate
            || PageSize is not null))
        {
            throw new InvalidOperationException($"A {nameof(TbDataGrid<TItem>)} with a {nameof(ChildrenSelector)} " +
                $"takes its rows from {nameof(Items)}, and can't group them, move them or split them into pages; set " +
                $"{nameof(Virtualize)} to show many rows.");
        }

        // A virtualized grid numbers its rows for screen readers, and a detail row has no number of its own.
        if (Virtualize && DetailTemplate is not null)
        {
            throw new InvalidOperationException($"A {nameof(TbDataGrid<TItem>)} that virtualizes its rows can't " +
                $"have a {nameof(DetailTemplate)}.");
        }

        // Without a height the rows' box grows with them, so every row would count as in view.
        if (Virtualize && (!Scrolls || PageSize is not null))
        {
            throw new InvalidOperationException($"A {nameof(TbDataGrid<TItem>)} that virtualizes its rows needs a " +
                $"{nameof(Height)} or {nameof(Fill)}, and no {nameof(PageSize)}.");
        }

        if (!ReferenceEquals(Sorts, _sortsFromParent))
        {
            _sortsFromParent = Sorts;
            TakeSorts(Sorts ?? []);
        }

        if (!ReferenceEquals(Filters, _filtersFromParent))
        {
            _filtersFromParent = Filters;
            TakeFilters(Filters ?? []);
        }

        if (!ReferenceEquals(Groups, _groupsFromParent))
        {
            _groupsFromParent = Groups;
            TakeGroups(Groups ?? []);
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

        if (!ReferenceEquals(State, _stateFromParent))
        {
            _stateFromParent = State;
            if (State is { } state)
            {
                TakeState(state);
            }
        }

        // Rows draw again for a new collection, whose items may have changed in place, or when the selection mode,
        // cell navigation, cell selection or part classes change a row's markup. Each row compares its own class and
        // label, which may read from outside the item.
        object rowOptions = (Items, SelectionMode, CellNavigation, CellSelection, Classes);
        if (!rowOptions.Equals(_rowOptions))
        {
            _rowOptions = rowOptions;
            _rowVersion++;
        }

        TakeSelectionFromParent();
        TakeExpandedFromParent();
        TakeOpenParentsFromParent();
        await ShowAsync(force: false);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _columnsChanged = false;
        if (_nextRender is { } rendered)
        {
            _nextRender = null;
            rendered.TrySetResult();
        }
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

            // The headers trade places as a column moves, so its menu button takes focus back in its new place.
            if (_focusColumnMenu is { } moved && _handle is not null)
            {
                _focusColumnMenu = null;
                await _handle.InvokeVoidAsync("focusColumnMenu", moved);
            }

            // A column hidden from its own menu takes that menu's button with it.
            if (_focusColumnChooser && _handle is not null)
            {
                _focusColumnChooser = false;
                await _handle.InvokeVoidAsync("focusColumnChooser");
            }

            // A column shown without a width is measured, so the user's widths hold fixed again.
            if (_measureColumn is { } shown && _handle is not null)
            {
                _measureColumn = null;
                _measuring = true;
                await _handle.InvokeVoidAsync("resizeColumn", shown, 0);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the grid went with it.
        }

        if (_showForColumns)
        {
            _showForColumns = false;
            await ShowAsync(force: false);
            // With still no columns, ShowAsync waits again for the app's next render; otherwise the loaded rows need
            // drawing.
            if (!_showForColumns)
            {
                StateHasChanged();
            }
        }
        // Ask the provider again when a column with an Aggregate joins after the first load, such as one shown later.
        else if (DataProvider is not null && AggregatesKey != _aggregatesAsked)
        {
            await ShowAsync(force: false);
            StateHasChanged();
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

    /// <summary>Draws every row again. A row draws again by itself only when its item, its selection, its
    /// <see cref="RowClass"/>, its <see cref="RowLabel"/> or the columns change, so call this when a
    /// <see cref="TbColumn{TItem}.CellTemplate"/> shows something else that changed, such as which row is being edited.
    /// </summary>
    public void RenderRows()
    {
        _rowVersion++;
        StateHasChanged();
    }

    /// <summary>Goes to the page that holds this item's row, found by its <see cref="KeySelector"/> key, and scrolls
    /// the row into view. With a <see cref="DataProvider"/>, only a row on the current page can be found or, when
    /// <see cref="Virtualize"/> is set, only a loaded row.</summary>
    public async Task ScrollToAsync(TItem item)
    {
        var key = KeyOf(item);
        var opened = Items is not null && (ChildrenSelector is null ? OpenGroupsOf(item) : await OpenParentsOfAsync(item));
        if (Virtualize)
        {
            // Virtualize draws the opened groups' lines only once the grid renders, so the scroll waits for it.
            if (opened)
            {
                await NextRenderAsync();
            }

            var at = DataProvider is null ? PlaceOf(_lines, key)
                : _rows.FindIndex(row => Equals(KeyOf(row), key)) is var loaded and >= 0 ? _rowsStart + loaded : -1;
            if (_handle is not null && at >= 0)
            {
                await _handle.InvokeVoidAsync("scrollToRow", IdOf(item), at);
            }

            return;
        }

        if (_pageSize is { } size && Items is not null && PlaceOf(Lines(Arranged().ToList()), key) is var index
            and >= 0)
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
        foreach (var line in _lines)
        {
            AddLine(builder, line);
        }
    }

    // Virtualize draws each line it shows in one region, so the lines keep their keys among themselves.
    private RenderFragment<GridLine> LineContent => line => builder => AddLine(builder, line);

    private void AddLine(RenderTreeBuilder builder, GridLine line)
    {
        if (line.Group is { } group)
        {
            AddGroupRow(builder, group, line.Index);
        }
        else if (line.Totals is { } totals)
        {
            builder.OpenComponent<GridRow<GroupTotalsLine>>(0);
            builder.SetKey((totals.Group.Key, "totals"));
            builder.AddComponentParameter(1, nameof(GridRow<GroupTotalsLine>.Item), totals);
            builder.AddComponentParameter(2, nameof(GridRow<GroupTotalsLine>.Version), _rowVersion);
            builder.AddComponentParameter(3, nameof(GridRow<GroupTotalsLine>.ChildContent),
                GroupTotalsRow(totals, line.Index));
            builder.CloseComponent();
        }
        else
        {
            AddRow(builder, line.Item, line.Index, line.Tree);
        }
    }

    // A heading keeps its key as it opens and closes, so focus stays on its button.
    private void AddGroupRow(RenderTreeBuilder builder, GroupHeading group, int at)
    {
        builder.OpenComponent<GridRow<GroupHeading>>(0);
        builder.SetKey(group.Key);
        builder.AddComponentParameter(1, nameof(GridRow<GroupHeading>.Item), group);
        builder.AddComponentParameter(2, nameof(GridRow<GroupHeading>.Version), _rowVersion);
        builder.AddComponentParameter(3, nameof(GridRow<GroupHeading>.ChildContent), GroupRow(group, at));
        builder.CloseComponent();
    }

    private void AddRow(RenderTreeBuilder builder, TItem item, int at, TreePlace? tree = null)
    {
        var key = KeyOf(item);
        var selected = IsSelected(item);
        var rowClass = RowClass?.Invoke(item);
        var label = RowLabel?.Invoke(item);
        builder.OpenComponent<GridRow<TItem>>(0);
        builder.SetKey(key);
        builder.AddComponentParameter(1, nameof(GridRow<TItem>.Item), item);
        builder.AddComponentParameter(2, nameof(GridRow<TItem>.Selected), selected);
        builder.AddComponentParameter(3, nameof(GridRow<TItem>.Class), rowClass);
        builder.AddComponentParameter(4, nameof(GridRow<TItem>.Label), label);
        builder.AddComponentParameter(5, nameof(GridRow<TItem>.Version), _rowVersion);
        builder.AddComponentParameter(6, nameof(GridRow<TItem>.Editing),
            _edit is { } edit && Equals(edit.Key, key) ? (edit, _editVersion) : null);
        var expanded = HasDetails && IsExpanded(item);
        builder.AddComponentParameter(7, nameof(GridRow<TItem>.Expanded), expanded);
        builder.AddComponentParameter(8, nameof(GridRow<TItem>.Tree), tree);
        builder.AddComponentParameter(9, nameof(GridRow<TItem>.ChildContent),
            Row(item, key, selected, at, rowClass, label, expanded, tree));
        builder.CloseComponent();
        if (expanded)
        {
            AddDetailRow(builder, item, key);
        }
    }

    // Drawn with the grid rather than with its row, so details that show the page's own state stay up to date, and
    // keyed beside the row, so they move with it when the rows reorder rather than being drawn anew.
    private void AddDetailRow(RenderTreeBuilder builder, TItem item, object key)
    {
        builder.OpenElement(9, "tr");
        builder.SetKey((key, "detail"));
        builder.AddAttribute(10, "id", DetailId(key));
        builder.AddAttribute(11, "data-part", "detail");
        builder.AddAttribute(12, "class", "tb-data-grid__detail");
        builder.OpenElement(13, "td");
        builder.AddAttribute(14, "colspan", DrawnCount);
        builder.AddAttribute(15, "data-part", "detail-cell");
        builder.AddAttribute(16, "class", "tb-data-grid__detail-cell");
        builder.AddContent(17, DetailTemplate!(item));
        builder.CloseElement();
        builder.CloseElement();
    }

    private ItemsProviderDelegate<GridLine>? VirtualRowsProvider =>
        DataProvider is null ? null : LoadVirtualRowsAsync;

    // A guess at a row's height, which Virtualize corrects from the rows it draws.
    private float RowSize => Size switch
    {
        Size.Small => 28,
        Size.Large => 52,
        _ => 44,
    };

    // A virtualized grid's provider loads the rows Virtualize asks for, sorted and filtered as the grid is.
    private async ValueTask<ItemsProviderResult<GridLine>> LoadVirtualRowsAsync(ItemsProviderRequest request)
    {
        TypeFilters();
        var asked = new GridRequest(request.StartIndex, request.Count, [.. _sorts])
        {
            Filters = [.. _filters],
            Aggregates = Aggregates,
        };
        _aggregatesAsked = AggregatesKey;
        _loaded = VirtualLoadKey;
        _loading = true;
        _failed = false;
        GridResult<TItem> result;
        try
        {
            result = await DataProvider!(asked, request.CancellationToken);
        }
        // Virtualize drops a load it has moved past and expects that load to throw OperationCanceledException.
        catch (Exception) when (request.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(request.CancellationToken);
        }
        catch (Exception exception)
        {
            _loading = false;
            _total = 0;
            await ShowLoadFailedAsync(exception);
            StateHasChanged();
            return new([], 0);
        }

        // A load the provider finished after Virtualize moved past it mustn't replace the rows of a newer one.
        if (request.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(request.CancellationToken);
        }

        _loading = false;
        List<TItem> rows = [.. result.Items];
        ShowRows(rows);
        _totals = new(result.Totals);
        _rowsStart = request.StartIndex;
        _total = result.TotalCount;
        if (_total == 0)
        {
            Announcer.Polite(EmptyLabel);
        }

        StateHasChanged();
        return new(rows.Select((row, index) => new GridLine(row, request.StartIndex + index)), result.TotalCount);
    }

    // The render is captured before it is asked for, since it can finish while StateHasChanged runs.
    private Task NextRenderAsync()
    {
        var rendered = _nextRender ??= new();
        StateHasChanged();
        return rendered.Task;
    }

    /// <summary>Starts an edit in this item's row from code, such as an app's own Edit button, as a double-click on one
    /// of its cells does. <paramref name="columnId"/> names the cell to start in by its column's <c>Id</c> or field
    /// path, and without one the first editable column is used. An edit already open is saved first, and stays open
    /// when it can't be. An item the grid isn't showing, such as one filtered out, isn't edited.</summary>
    /// <exception cref="ArgumentException">No shown, editable column has this id.</exception>
    /// <exception cref="InvalidOperationException">No shown column is editable.</exception>
    public async Task StartEditAsync(TItem item, string? columnId = null)
    {
        var editable = _shownColumns.Where(CanEdit).ToList();
        if (editable.Count == 0)
        {
            throw new InvalidOperationException("The grid shows no editable column.");
        }

        var column = columnId is null ? editable[0] : editable.FirstOrDefault(shown => shown.Key == columnId)
            ?? throw new ArgumentException($"No shown, editable column has the id \"{columnId}\".", nameof(columnId));
        await StartEditAsync(item, column);
        StateHasChanged();
    }

    // An edit already open is saved before another starts, and stays open when it can't be. In row mode an edit covers
    // every editable cell of the row, starting in the one asked for.
    private async Task StartEditAsync(TItem item, TbColumn<TItem> column)
    {
        var key = KeyOf(item);
        if (_edit is { } open && (Equals(open.Key, key) && (EditMode == GridEditMode.Row || open.Start == column)
                || !await SaveEditAsync(refocus: false)))
        {
            return;
        }

        // The save just made may have replaced the row's item, or filtered or sorted the row out of view.
        var at = _rows.FindIndex(row => Equals(KeyOf(row), key));
        if (at < 0)
        {
            return;
        }

        item = _rows[at];
        var columns = EditMode == GridEditMode.Row ? _shownColumns.Where(CanEdit) : [column];
        _edit = new(item, key, column, [.. columns.Select(editable => new CellEdit(editable,
            ChangeOf(key, editable) is { } change ? change.Value : editable.ValueOf(item)))]);
        _editVersion++;
    }

    // A select takes Enter to open its list, so choosing from it saves a cell's edit; a row's edit is saved whole.
    private Task ChooseAsync(CellEdit cell, string? choice)
    {
        cell.Value = choice;
        return EditMode != GridEditMode.Row ? SaveEditAsync(refocus: true) : Task.CompletedTask;
    }

    // A key the field used, such as Enter opening a select's list or Escape closing it, leaves the edit open.
    private async Task OnEditKeyAsync(GridEditKeyEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (args is { Key: "Enter", IsComposing: false })
        {
            await SaveEditAsync(refocus: true);
        }
        else if (args is { Key: "Escape", IsComposing: false })
        {
            await CancelEditAsync();
        }
    }

    // Enter on a row's Save or Cancel button clicks it, so only Escape is handled here.
    private Task OnRowActionKeyAsync(GridEditKeyEventArgs args) =>
        args is { Key: "Escape", Handled: false } ? CancelEditAsync() : Task.CompletedTask;

    // A field removed as the next edit opens reports focus leaving too, and that mustn't save the new edit.
    private Func<FieldFocusEventArgs, Task> LeftHandler(GridEdit edit) => args => OnEditLeftAsync(edit, args);

    private async Task OnEditLeftAsync(GridEdit edit, FieldFocusEventArgs args)
    {
        if (args.CrossesField && _edit == edit)
        {
            await SaveEditAsync(refocus: false);
        }
    }

    // A value that fails a check keeps the edit open, showing why; one that passes goes to the app, or onto the item,
    // and the rows are shown again, sorted and filtered with it. The edit stays open until the app has taken it, so a
    // handler that sets an error loses nothing typed. A save already on its way, such as to a server, is waited for
    // rather than started again.
    private Task<bool> SaveEditAsync(bool refocus)
    {
        if (_edit is not { } edit)
        {
            return Task.FromResult(true);
        }

        if (edit.Saving is { IsCompleted: false } saving)
        {
            return saving;
        }

        return edit.Saving = SaveAsync(edit, refocus);
    }

    // A row is saved only when every one of its cells passes its checks.
    private async Task<bool> SaveAsync(GridEdit edit, bool refocus)
    {
        edit.Error = null;
        Dictionary<CellEdit, object?> values = [];
        foreach (var cell in edit.Cells)
        {
            cell.Error = CheckEdit(edit.Item, cell, out var value);
            values[cell] = value;
        }

        if (edit.Cells.Any(cell => cell.Error is not null))
        {
            ShowEditErrors();
            return false;
        }

        if (EditMode == GridEditMode.Batch)
        {
            KeepChanges(edit, values);
            Telemetry.Used(TelemetryName, "edit");
            if (refocus)
            {
                await FocusCellAsync(edit);
            }

            return true;
        }

        if (await RejectionOfAsync(edit, values) is { } rejected)
        {
            if (EditMode == GridEditMode.Row)
            {
                edit.Error = rejected;
                Announcer.Assertive(rejected);
            }
            else
            {
                edit.StartCell.Error = rejected;
            }

            ShowEditErrors();
            return false;
        }

        if (!HasEditHandler)
        {
            foreach (var (cell, value) in values)
            {
                cell.Column.SetValue(edit.Item, value);
            }
        }

        _edit = null;
        _editVersion++;
        Telemetry.Used(TelemetryName, "edit");
        await RefreshAsync();
        if (refocus)
        {
            await FocusCellAsync(edit);
        }

        return true;
    }

    // A batch grid keeps each checked value as a change, and a value edited back to the item's own drops the change.
    private void KeepChanges(GridEdit edit, Dictionary<CellEdit, object?> values)
    {
        foreach (var (cell, value) in values)
        {
            var key = (edit.Key, cell.Column.Key);
            // A cell still being saved keeps a value edited back to the item's own, since the save is about to change the
            // item.
            if (Equals(value, cell.Column.ValueOf(edit.Item)) && !_sending.Contains(key))
            {
                _changes.Remove(key);
            }
            else
            {
                _changes[key] = new(edit.Item, cell.Column.Key, value);
            }
        }

        _edit = null;
        _editVersion++;
        _rowVersion++;
    }

    private GridCellEdit<TItem>? ChangeOf(object key, TbColumn<TItem> column) =>
        _changes.GetValueOrDefault((key, column.Key));

    /// <summary>Saves the changes of a grid whose <see cref="EditMode"/> is <see cref="GridEditMode.Batch"/>, as its
    /// Save changes button does: an edit still open is saved into them first, then <see cref="OnBatchSave"/> gets them
    /// all (without a handler, each value is set on its item), and the rows are shown again. If that edit fails its
    /// check, nothing is saved. A handler that sets <see cref="GridBatchEdit{TItem}.Error"/> keeps the changes and
    /// shows why. A save already on its way is waited for rather than started again.</summary>
    public Task SaveChangesAsync() => _batchSave is { IsCompleted: false } saving ? saving : _batchSave = SaveBatchAsync();

    private async Task SaveBatchAsync()
    {
        try
        {
            await SaveBatchCoreAsync();
        }
        finally
        {
            _answered++;
            StateHasChanged();
        }
    }

    private async Task SaveBatchCoreAsync()
    {
        if (!await SaveEditAsync(refocus: false) || _changes.Count == 0)
        {
            return;
        }

        var sent = _changes.ToList();
        var batch = new GridBatchEdit<TItem>([.. sent.Select(pair => pair.Value)]);
        _batchError = null;
        _sending = [.. sent.Select(pair => pair.Key)];
        try
        {
            if (OnBatchSave.HasDelegate)
            {
                await OnBatchSave.InvokeAsync(batch);
            }
            else
            {
                foreach (var change in batch.Changes)
                {
                    _columns.First(column => column.Key == change.Column).SetValue(change.Item, change.Value);
                }
            }
        }
        finally
        {
            _sending = [];
        }

        if (batch.Error is { } error)
        {
            _batchError = error;
            Announcer.Assertive(error);
            return;
        }

        // Changes made while the batch was on its way stay for the next save.
        foreach (var (key, change) in sent)
        {
            if (ReferenceEquals(_changes.GetValueOrDefault(key), change))
            {
                _changes.Remove(key);
            }
        }

        Announcer.Polite(Localizer[TbStrings.DataGridChangesSaved]);
        await RefreshAsync();
    }

    /// <summary>Drops the changes of a grid whose <see cref="EditMode"/> is <see cref="GridEditMode.Batch"/>, and any
    /// edit still open, as its Discard changes button does, so every cell shows its item's value again.</summary>
    public Task DiscardChangesAsync()
    {
        _edit = null;
        _editVersion++;
        _changes.Clear();
        _batchError = null;
        RenderRows();
        return Task.CompletedTask;
    }

    private async Task SaveChangesFromBarAsync()
    {
        Telemetry.Used(TelemetryName, "save_changes");
        await SaveChangesAsync();
    }

    private Task DiscardChangesFromBarAsync()
    {
        Telemetry.Used(TelemetryName, "discard_changes");
        return DiscardChangesAsync();
    }

    private void ShowEditErrors()
    {
        _editVersion++;
        StateHasChanged();
    }

    // The app's handler takes the edit, or turns it down with an error.
    private async Task<string?> RejectionOfAsync(GridEdit edit, Dictionary<CellEdit, object?> values)
    {
        if (!HasEditHandler)
        {
            return null;
        }

        if (EditMode == GridEditMode.Row)
        {
            var row = new GridRowEdit<TItem>(edit.Item,
                values.ToDictionary(pair => pair.Key.Column.Key, pair => pair.Value));
            await OnRowEdit.InvokeAsync(row);
            return row.Error;
        }

        var change = new GridCellEdit<TItem>(edit.Item, edit.Start.Key, values[edit.StartCell]);
        await OnCellEdit.InvokeAsync(change);
        return change.Error;
    }

    /// <summary>Cancels the open edit, as Escape does: the values stay as they were, and focus goes back to the cell.
    /// In <see cref="GridEditMode.Cell"/>, focus leaving the cell saves the edit, so a control outside the cell that
    /// takes focus saves the edit before this method can cancel it; call this from code that leaves focus where it is,
    /// or use <see cref="GridEditMode.Row"/>, whose rows have their own Cancel button.</summary>
    public async Task CancelEditAsync()
    {
        if (_edit is not { } edit)
        {
            return;
        }

        _edit = null;
        _editVersion++;
        await FocusCellAsync(edit);
    }

    // Focus goes back to the cell once its field is gone, unless another edit has opened since.
    private async Task FocusCellAsync(GridEdit edit)
    {
        await NextRenderAsync();
        if (_handle is not null && _edit is null)
        {
            await _handle.InvokeVoidAsync("focusCell", IdOf(edit.Item), edit.Start.Key);
        }
    }

    // A value the field can't hold, such as a fraction for a whole number, or no value for a field that can't be
    // null, fails before the column's own checks.
    private string? CheckEdit(TItem item, CellEdit cell, out object? value)
    {
        value = null;
        if (cell.Column.HoldsWholeNumbers && cell.Value is decimal number && number != decimal.Truncate(number))
        {
            return Format(TbStrings.DataGridEditInvalid, cell.Column.Heading);
        }

        try
        {
            value = cell.Column.ConvertForField(cell.Value);
        }
        catch (OverflowException)
        {
            return Format(TbStrings.DataGridEditInvalid, cell.Column.Heading);
        }

        if (value is null && !cell.Column.AcceptsNull)
        {
            return Format(TbStrings.FieldRequired, cell.Column.Heading);
        }

        return cell.Column.AnnotationErrorOf(item, value) ?? cell.Column.Validate?.Invoke(item, value);
    }

    // A template that shows its value draws again as the value changes.
    private GridCellEditContext<TItem> ContextOf(GridEdit edit, CellEdit cell) => new(edit.Item, cell.Value,
        cell.Error, value =>
        {
            cell.Value = value;
            _editVersion++;
            StateHasChanged();
        }, () => SaveEditAsync(refocus: true), CancelEditAsync);

    private static decimal? NumberOf(object? value) => NumberTypes.TryToDecimal(value, out var number) ? number : null;

    // What is being edited: one cell, or in row mode every editable cell of a row, starting in one.
    private sealed class GridEdit(TItem item, object key, TbColumn<TItem> start, IReadOnlyList<CellEdit> cells)
    {
        public TItem Item { get; } = item;

        public object Key { get; } = key;

        public TbColumn<TItem> Start { get; } = start;

        public IReadOnlyList<CellEdit> Cells { get; } = cells;

        public CellEdit StartCell => Cells.First(cell => cell.Column == Start);

        public Task<bool>? Saving { get; set; }

        public string? Error { get; set; }

        public CellEdit? CellOf(TbColumn<TItem> column) => Cells.FirstOrDefault(cell => cell.Column == column);
    }

    // A cell being edited, and the value its field holds so far.
    private sealed class CellEdit(TbColumn<TItem> column, object? value)
    {
        public TbColumn<TItem> Column { get; } = column;

        public object? Value { get; set; } = value;

        public string? Error { get; set; }
    }

    // A line the grid draws: an item's row, a group's heading or a group's totals, with its place among all the lines,
    // and a tree row's place in the tree.
    private readonly record struct GridLine(TItem Item, int Index, GroupHeading? Group = null,
        GroupTotalsLine? Totals = null, TreePlace? Tree = null)
    {
        public bool IsRow => Group is null && Totals is null;
    }

    private sealed record GroupTotalsLine(GroupHeading Group, Dictionary<string, object?> Values);

    private sealed record GroupHeading(GroupKey Key, int Level, string Column, string? Value, int Count, bool Open);

    // A group's column and value within its outer group. As its own type, it also keys the heading apart from every
    // row.
    private sealed record GroupKey(GroupKey? Outer, string Column, object? Value);

    // What the first render guesses, since only data-grid.js can measure whether the box overflows.
    private bool ScrollIsTabStop => Scrolls && !IsGrid;

    private bool Scrolls => Height is not null || Fill;

    private bool _sortKeyHasShift;

    // Columns and groups, each added to the group it's written in, or to the grid, in the order they're written.
    internal void AddMember(object member, TbColumnGroup<TItem>? group)
    {
        (group?.Members ?? _members).Add(member);
        TakeColumnsInOrder();
        StateHasChanged();
    }

    private void TakeColumnsInOrder()
    {
        _columns.Clear();
        _columns.AddRange(ColumnsOf(_members));
    }

    private static IEnumerable<TbColumn<TItem>> ColumnsOf(List<object> members) => members.SelectMany(member =>
        member is TbColumnGroup<TItem> group ? ColumnsOf(group.Members) : [(TbColumn<TItem>)member]);

    // Pinned columns show at their side, those pinned at the start first and those pinned at the end last.
    // Within its side, a column named in the saved order takes its place there, ahead of any it doesn't name.
    // A saved state that hides every column shows them all, so the grid is never empty.
    private void ArrangeColumns()
    {
        _arrangedColumns = [.. _columns.OrderBy(column => column.Pinned switch
        {
            ColumnPin.Start => 0,
            ColumnPin.End => 2,
            _ => 1,
        }).ThenBy(column => _order.IndexOf(column.Key) is >= 0 and var at ? at : int.MaxValue)];
        var shown = _arrangedColumns.Where(column => !(HideableColumns && column.Hideable
            && _hidden.Contains(column.Key))).ToList();
        _shownColumns = shown.Count > 0 ? shown : _arrangedColumns;
    }

    // The selection and order columns lead each row, so they stay with the columns pinned at the start.
    private bool PinsLead => _shownColumns is [{ Pinned: ColumnPin.Start }, ..];

    private int LeadCount =>
        (CanReorder ? 1 : 0) + (HasDetails ? 1 : 0) + (SelectionMode == SelectionMode.None ? 0 : 1);

    private bool HasDetails => DetailTemplate is not null;

    // The details button comes after a row's move handle and before its selection box.
    private int DetailSlot => CanReorder ? 1 : 0;

    private string? LeadPin => PinsLead ? "start" : null;

    private string? LeadStyle(int slot) => PinsLead ? StartPinStyle(slot) : null;

    private static string? PinSide(TbColumn<TItem> column) =>
        column.Pinned == ColumnPin.None ? null : column.Pinned.ToString().ToLowerInvariant();

    // A pinned cell sticks just after the cells pinned before it on its side; the grid's script measures their widths
    // into variables numbered from that side.
    private string? PinStyle(TbColumn<TItem> column)
    {
        var at = _shownColumns.IndexOf(column);
        return column.Pinned switch
        {
            ColumnPin.Start => StartPinStyle(LeadCount + at),
            ColumnPin.End => $"inset-inline-end: var(--tb-data-grid-pin-end-{_shownColumns.Count - 1 - at}, 0px)",
            _ => null,
        };
    }

    private string? HeaderStyle(TbColumn<TItem> column) =>
        Styles.Join(WidthOf(column) is { } width ? $"width: {width}" : null,
            CanResize(column) ? $"min-inline-size: {column.MinWidth ?? "8rem"}" : null, PinStyle(column));

    private string? WidthOf(TbColumn<TItem> column) => VirtualizeColumns
        ? $"{PixelsOf(column).ToString(CultureInfo.InvariantCulture)}px"
        : _widths.TryGetValue(column.Key, out var width) ? $"{width.ToString(CultureInfo.InvariantCulture)}px" : column.Width;

    private bool CanResize(TbColumn<TItem> column) => ResizableColumns && column.Resizable;

    private string? KeptWidth(TbColumn<TItem> column) =>
        _widths.TryGetValue(column.Key, out var width) ? width.ToString(CultureInfo.InvariantCulture) : null;

    // How far a column menu's Make wider and Make narrower move the column's edge, in pixels.
    private const double WidthStep = 32;

    // Widths the user set hold; the browser's own layout would squeeze them when the rows are wider than the grid. A
    // fixed layout gives a column without a width nothing, so until every column has one, the widths only guide the
    // browser's layout.
    private bool HasFixedWidths => ResizableColumns && _shownColumns.Count > 0
        && _shownColumns.All(column => _widths.ContainsKey(column.Key));

    private async Task OnColumnWidthsAsync(ColumnWidthsEventArgs e)
    {
        if (!_measuring)
        {
            Telemetry.Used(TelemetryName, "resize");
        }

        _measuring = false;
        // The script measures only the columns it draws, so a column it doesn't report, hidden or out of view, keeps
        // the width it had.
        var widths = _widths.Where(width => !e.Widths.ContainsKey(width.Key)
                && _arrangedColumns.Any(column => column.Key == width.Key))
            .ToDictionary(width => width.Key, width => width.Value);
        foreach (var width in e.Widths.Where(width => _columns.Any(column => column.Key == width.Key)))
        {
            widths[width.Key] = Math.Round(width.Value);
        }

        _widths = widths;
        await StateChangedAsync();
    }

    private async Task StepWidthAsync(TbColumn<TItem> column, double delta)
    {
        if (_handle is not null)
        {
            await _handle.InvokeVoidAsync("resizeColumn", column.Key, delta);
        }
    }

    // A menu with nothing to do would be an empty tab stop, as for a column that can't be resized and is alone on its
    // pinned side.
    private bool HasColumnMenu(TbColumn<TItem> column) => CanResize(column) || CanHide(column)
        || ReorderableColumns && (NeighborOf(column, -1) ?? NeighborOf(column, 1)) is not null;

    // The last column shown stays, so the grid is never empty.
    private bool CanHide(TbColumn<TItem> column) =>
        HideableColumns && column.Hideable && _shownColumns.Contains(column) && _shownColumns.Count > 1;

    private bool CanShowOrHide(TbColumn<TItem> column) => !_shownColumns.Contains(column) || CanHide(column);

    private async Task SetColumnShownAsync(TbColumn<TItem> column, bool shown)
    {
        Telemetry.Used(TelemetryName, shown ? "show" : "hide");
        // A saved state that hid every column shows them all, so drop any hidden key whose column is now in view.
        _hidden = [.. _hidden.Where(key => key != column.Key && !_shownColumns.Any(other => other.Key == key))];
        if (!shown)
        {
            _hidden.Add(column.Key);
        }
        else if (ResizableColumns && _widths.Count > 0 && !_widths.ContainsKey(column.Key))
        {
            _measureColumn = column.Key;
        }

        // Rows draw again only when told, and their cells follow the shown columns.
        _rowVersion++;
        await StateChangedAsync();
    }

    private async Task HideFromMenuAsync(TbColumn<TItem> column)
    {
        _focusColumnChooser = true;
        Announcer.Polite(Format(TbStrings.DataGridColumnHidden, column.Heading));
        await SetColumnShownAsync(column, false);
    }

    // The neighbor a column trades places with, among the columns pinned to its side.
    private TbColumn<TItem>? NeighborOf(TbColumn<TItem> column, int by)
    {
        var side = _shownColumns.Where(other => other.Pinned == column.Pinned).ToList();
        var at = side.IndexOf(column) + by;
        return at >= 0 && at < side.Count ? side[at] : null;
    }

    private async Task OnColumnOrderAsync(ColumnOrderEventArgs e)
    {
        Telemetry.Used(TelemetryName, "reorder");
        // The script reports only the columns it draws, so a column it doesn't report, hidden or out of view, keeps its
        // place, and the reported ones take the places they held, in their new order.
        var moved = e.Order.Where(key => _arrangedColumns.Any(column => column.Key == key)).Distinct().ToList();
        var next = new Queue<string>(moved);
        _order = [.. _arrangedColumns.Select(column => moved.Contains(column.Key) ? next.Dequeue() : column.Key)];

        // Rows draw again only when told, and their cells follow the columns' order.
        _rowVersion++;
        await StateChangedAsync();
    }

    private async Task MoveColumnAsync(TbColumn<TItem> column, int by)
    {
        if (NeighborOf(column, by) is not { } neighbor)
        {
            return;
        }

        var keys = _shownColumns.Select(other => other.Key).ToList();
        var (from, to) = (keys.IndexOf(column.Key), keys.IndexOf(neighbor.Key));
        (keys[from], keys[to]) = (keys[to], keys[from]);
        _focusColumnMenu = column.Key;
        // The announced place counts the selection and order columns, as screen readers do.
        Announcer.Polite(Format(TbStrings.DataGridColumnMoved, column.Heading, LeadCount + to + 1,
            LeadCount + keys.Count));
        await OnColumnOrderAsync(new() { Order = keys });
    }

    private async Task ResetWidthsAsync()
    {
        Telemetry.Used(TelemetryName, "resize");
        _widths = [];
        await StateChangedAsync();
    }

    private static string StartPinStyle(int slot) => $"inset-inline-start: var(--tb-data-grid-pin-{slot}, 0px)";

    // A sort names its column by key, so two columns with one key would sort and mark each other. Checked as the grid
    // renders, once every column has its parameters, since columns reordered in a loop change one at a time.
    private void CheckColumnKeys()
    {
        if (VirtualizeColumns && _members.Any(member => member is TbColumnGroup<TItem>))
        {
            throw new InvalidOperationException($"A {nameof(TbDataGrid<TItem>)} that virtualizes its columns can't have "
                + $"column groups; leave out {nameof(VirtualizeColumns)} or the {nameof(TbColumnGroup<TItem>)}s.");
        }

        if (_columns.GroupBy(column => column.Key).FirstOrDefault(keys => keys.Count() > 1) is { } clash)
        {
            throw new InvalidOperationException($"{nameof(TbDataGrid<TItem>)} has two columns named \"{clash.Key}\"; " +
                $"give one of them an {nameof(TbColumn<TItem>.Id)}.");
        }
    }

    internal void RemoveMember(object member, TbColumnGroup<TItem>? group)
    {
        if (!_disposed && (group?.Members ?? _members).Remove(member))
        {
            TakeColumnsInOrder();
            _sort.KeepOnly(_columns.Contains);
            _applyOnEnter.KeepOnly(_columns.Contains);
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

        // Tests wait for data-ready, so with rows that move it waits for their reordering too.
        var waitForReorder = _createdWaitingForReorder = OnRowMove.HasDelegate && _reorder is null;
        var handle = await module.InvokeAsync<IJSObjectReference?>("createDataGrid", _table, waitForReorder);
        if (handle is not null && _disposed)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
        if (handle is not null && waitForReorder && _reorder is not null)
        {
            await handle.InvokeVoidAsync("setReady", true);
        }
        else if (handle is not null && !waitForReorder && _moveEvents is not null && _reorder is null)
        {
            // Rows became movable while createDataGrid ran, and their sortable is still attaching.
            await handle.InvokeVoidAsync("setReady", false);
        }
    }

    // Rows can move once the app handles OnRowMove, which it may start doing after the first render.
    private async Task AttachReorderAsync()
    {
        _moveEvents = DotNetObjectReference.Create(new SortableEvents(OnRowDragOverAsync, OnRowDropAsync,
            OnRowKeyAsync) { Name = () => AriaLabel });
        // A grid marked ready before its rows could move isn't ready again until they can.
        if (_handle is not null && !_createdWaitingForReorder)
        {
            await _handle.InvokeVoidAsync("setReady", false);
        }

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
        if (_handle is not null && !_disposed)
        {
            await _handle.InvokeVoidAsync("setReady", true);
        }
    }

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
            ? Mover.CheckAsync(AllItems, AllIndexOf(id), to, CanMoveRow, list: null) : Task.FromResult(new MoveCheck(false));

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
        try
        {
            await MoveRowCoreAsync(from, to, input);
        }
        finally
        {
            _answered++;
            StateHasChanged();
        }
    }

    private async Task MoveRowCoreAsync(int from, int to, string input)
    {
        if (!CanReorder || from < 0
            || !await Mover.MoveAsync(() => AllItems, from, to, CanMoveRow, OnRowMove, LabelOf, moved: null, list: null))
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
            await StateChangedAsync();
        }

        await RefreshAsync();
        if (input == "keyboard" && Virtualize && _handle is not null)
        {
            await _handle.InvokeVoidAsync("scrollToRow", IdOf(moved), to);
        }

        if (input == "keyboard" && _reorder is not null)
        {
            await _reorder.InvokeVoidAsync("focusHandle", IdOf(moved));
        }
    }

    // In memory the rows are sorted and paged here; a provider is asked only when the request differs from the last.
    private async Task ShowAsync(bool force)
    {
        TypeFilters();
        if (DataProvider is not null && Virtualize)
        {
            await ReloadVirtualRowsAsync(force);
            return;
        }

        if (DataProvider is null)
        {
            var page = _page;
            Arrange(force);
            if (_page != page)
            {
                await PageChanged.InvokeAsync(_page);
                await StateChangedAsync();
            }

            return;
        }

        // Wait for the columns' JSON filter types and totals; with no next render, static renders load without totals.
        if (_columns.Count == 0 && (RendererInfo.IsInteractive || _filters.Any(FilterJson.IsUntyped)))
        {
            _showForColumns = true;
            _loading = true;
            return;
        }

        var request = new GridRequest((_page - 1) * (_pageSize ?? 0), _pageSize, [.. _sorts])
        {
            Filters = [.. _filters],
            Aggregates = Aggregates,
        };
        _aggregatesAsked = AggregatesKey;
        var asked = $"{request.StartIndex}|{request.Count}|{string.Join(",", _sorts)}|{FiltersKey}|{AggregatesKey}";
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
            await ShowLoadFailedAsync(exception);
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
        _totals = new(result.Totals);
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

    private async Task ShowLoadFailedAsync(Exception exception)
    {
        _failed = true;
        ShowRows([]);
        _totals = [];
        Announcer.Polite(LoadFailedLabel);
        await OnLoadFailed.InvokeAsync(exception);
    }

    // Virtualize asks a provider for the rows it draws, so a new sort or filter only tells it to ask again. Before it
    // first renders, it asks of its own accord.
    private async Task ReloadVirtualRowsAsync(bool force)
    {
        _aggregatesAsked = AggregatesKey;
        var asked = VirtualLoadKey;
        if (!force && asked == _loaded)
        {
            return;
        }

        _loaded = asked;
        _loading = true;
        if (_virtualize is not null)
        {
            await _virtualize.RefreshDataAsync();
        }
    }

    // In-memory rows sort and page again only when something that orders them changed, not on every parent render.
    private void Arrange(bool force)
    {
        if (!force && ArrangedFor.Equals(_arrangedFor))
        {
            return;
        }

        List<TItem> arranged;
        List<GridLine> lines;
        if (ChildrenSelector is null)
        {
            arranged = Arranged().ToList();
            lines = Lines(arranged);
        }
        else
        {
            (arranged, lines) = TreeLines();
        }

        _totals = TotalsOf(arranged);
        _matched = arranged.Count;
        _total = lines.Count;
        // A virtualized row shows its place among the lines, which may have moved, so the rows draw again.
        if (Virtualize)
        {
            _rowVersion++;
        }

        _page = Math.Min(_page, PageCount);
        ShowLines(_pageSize is { } size ? [.. lines.Skip((_page - 1) * size).Take(size)] : lines);
        _arrangedFor = ArrangedFor;
    }

    private object ArrangedFor =>
        (Items, _sorts, _filters, _groups, _page, _pageSize, _columnVersion, GroupTotals && HasTotals, _treeVersion);

    // A tree grid's lines, where each open row's children follow it. The rows that match the filters are counted and
    // totaled at every level.
    private (List<TItem> Matched, List<GridLine> Lines) TreeLines()
    {
        var filtering = AppliedFilters.Any();
        if (!ReferenceEquals(_closedWhileFilteredFor, _filters))
        {
            _closedWhileFilteredFor = _filters;
            _closedWhileFiltered.Clear();
        }

        List<TItem> matched = [];
        List<GridLine> lines = [];
        void Add(List<TreeNode> nodes, int level, int parent)
        {
            for (var at = 0; at < nodes.Count; at++)
            {
                var node = nodes[at];
                var key = KeyOf(node.Item);
                bool? open = node.Children.Count == 0 ? null
                    : filtering ? !_closedWhileFiltered.Contains(key) : _openParents.ContainsKey(key);
                var index = lines.Count;
                lines.Add(new(node.Item, index, Tree: new(level, at + 1, nodes.Count, open, Virtualize ? parent : -1)));
                if (open == true)
                {
                    Add(node.Children, level + 1, index);
                }
            }
        }

        Add(TreeOf(Matcher(filtered: true), matched), 1, -1);
        return (matched, lines);
    }

    // A tree row with the rows under it that show: all of them, or with filters, those that match or have matches
    // under them.
    private sealed record TreeNode(TItem Item, List<TreeNode> Children);

    // Walks the whole tree, so every row's parent is known and a key seen twice, as in a row under itself, is caught.
    private List<TreeNode> TreeOf(Func<TItem, bool> matches, List<TItem> matched)
    {
        _parentOf.Clear();
        HashSet<object> seen = [];
        List<TreeNode> Level(IEnumerable<TItem> items, bool top, TItem parent)
        {
            List<TreeNode> nodes = [];
            foreach (var item in Sorted(items))
            {
                var key = KeyOf(item);
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException($"{nameof(TbDataGrid<TItem>)} has two rows with the same key, " +
                        $"or a row under itself; give it a {nameof(KeySelector)} that gives each item a different key, " +
                        $"and make sure no row's {nameof(ChildrenSelector)} leads back to that row.");
                }

                if (!top)
                {
                    _parentOf[key] = parent;
                }

                var match = matches(item);
                if (match)
                {
                    matched.Add(item);
                }

                var children = Level(ChildrenSelector!(item) ?? [], top: false, item);
                if (match || children.Count > 0)
                {
                    nodes.Add(new(item, children));
                }
            }

            return nodes;
        }

        return Level(Items ?? [], top: true, default!);
    }

    private static IEnumerable<TItem> Flattened(List<TreeNode> nodes) =>
        nodes.SelectMany(node => Flattened(node.Children).Prepend(node.Item));

    private void TakeOpenParentsFromParent()
    {
        if (!ReferenceEquals(ExpandedParents, _openParentsFromParent))
        {
            _openParentsFromParent = ExpandedParents;
            _openParents.Clear();
            foreach (var item in ExpandedParents ?? [])
            {
                _openParents[KeyOf(item)] = item;
            }

            _treeVersion++;
        }
    }

    private async Task ToggleChildrenAsync(TItem item)
    {
        var key = KeyOf(item);
        var filtering = AppliedFilters.Any();
        bool open;
        if (filtering)
        {
            open = _closedWhileFiltered.Remove(key);
            if (!open)
            {
                _closedWhileFiltered.Add(key);
            }
        }
        else
        {
            open = !_openParents.Remove(key);
            if (open)
            {
                _openParents[key] = item;
            }
        }

        Telemetry.Used(TelemetryName, open ? "expand" : "collapse");
        Arrange(force: true);
        if (!filtering)
        {
            await ExpandedParentsChanged.InvokeAsync([.. _openParents.Values]);
        }
    }

    // A tree row under closed rows is shown before the grid goes to it.
    private async Task<bool> OpenParentsOfAsync(TItem item)
    {
        var filtering = AppliedFilters.Any();
        var opened = false;
        var key = KeyOf(item);
        while (_parentOf.TryGetValue(key, out var parent))
        {
            key = KeyOf(parent);
            opened |= filtering ? _closedWhileFiltered.Remove(key) : _openParents.TryAdd(key, parent);
        }

        if (!opened)
        {
            return false;
        }

        Arrange(force: true);
        if (!filtering)
        {
            await ExpandedParentsChanged.InvokeAsync([.. _openParents.Values]);
        }

        return true;
    }

    // Each group starts with its heading; an open group then lists its inner groups, or its rows in their sorted
    // order. Groups sort by their first row's value, not their key, since a formatted column's key is text.
    private List<GridLine> Lines(List<TItem> arranged)
    {
        var groups = AppliedGroups.ToList();
        List<GridLine> lines = [];
        void Add(IEnumerable<TItem> items, int level, GroupKey? outer)
        {
            if (level == groups.Count)
            {
                lines.AddRange(items.Select(item => new GridLine(item, lines.Count)));
                return;
            }

            var column = groups[level];
            foreach (var group in GroupsOf(items, column))
            {
                var key = new GroupKey(outer, column.Key, group.Key);
                var open = !_closedGroups.Contains(key);
                var heading = new GroupHeading(key, level + 1, column.Heading,
                    column.TextOf(column.GroupValueOf(group.First())), group.Count(), open);
                lines.Add(new(default!, lines.Count, heading));
                if (open)
                {
                    Add(group, level + 1, key);
                    if (GroupTotals && HasTotals)
                    {
                        lines.Add(new(default!, lines.Count, Totals: new(heading, TotalsOf([.. group]))));
                    }
                }
            }
        }

        Add(arranged, 0, null);
        return lines;
    }

    // A column's groups, in the order they show: by their first row's value, turned around when the column is
    // sorted descending.
    private IEnumerable<IGrouping<object?, TItem>> GroupsOf(IEnumerable<TItem> items, TbColumn<TItem> column)
    {
        var grouped = items.GroupBy(column.GroupKeyOf);
        var descending = _sorts.Any(sort => sort.Column == column.Key && sort.Direction == SortDirection.Descending);
        Func<IGrouping<object?, TItem>, object?> value = group => column.GroupValueOf(group.First());
        return descending ? grouped.OrderByDescending(value, Comparer<object?>.Default)
            : grouped.OrderBy(value, Comparer<object?>.Default);
    }

    // The rows in the order a grouped grid shows them, with the rows of closed groups too.
    private IEnumerable<TItem> GroupedOrder(IEnumerable<TItem> items, List<TbColumn<TItem>> groups, int level) =>
        level == groups.Count ? items
            : GroupsOf(items, groups[level]).SelectMany(group => GroupedOrder(group, groups, level + 1));

    private int PlaceOf(List<GridLine> lines, object key) =>
        lines.FindIndex(line => line.IsRow && Equals(KeyOf(line.Item), key)) is var at and >= 0
            ? lines[at].Index : -1;

    // A row in a closed group is shown before the grid goes to it.
    private bool OpenGroupsOf(TItem item)
    {
        GroupKey? key = null;
        var opened = false;
        foreach (var column in AppliedGroups)
        {
            key = new(key, column.Key, column.GroupKeyOf(item));
            opened |= _closedGroups.Remove(key);
        }

        if (opened)
        {
            Arrange(force: true);
        }

        return opened;
    }

    private void ToggleGroup(GroupKey key)
    {
        Telemetry.Used(TelemetryName, _closedGroups.Contains(key) ? "expand" : "collapse");
        if (!_closedGroups.Remove(key))
        {
            _closedGroups.Add(key);
        }

        Arrange(force: true);
    }

    // Groups nest in the order the user picks them, and a new grouping starts with every group open.
    private async Task GroupByAsync(IReadOnlyList<string> keys)
    {
        Telemetry.Used(TelemetryName, "group");
        _groups = [.. keys];
        _closedGroups.Clear();
        var pageMoved = _page != 1;
        _page = 1;
        var show = ShowAsync(force: false);
        await GroupsChanged.InvokeAsync(_groups);
        if (pageMoved)
        {
            await PageChanged.InvokeAsync(1);
        }

        await StateChangedAsync();

        await show;
    }

    // Columns report their changes after the grid renders, so their rows sort again as it next renders, once for all.
    private void ArrangeForColumns()
    {
        if (DataProvider is null)
        {
            TypeFilters();
            Arrange(force: false);
        }
    }

    // Selection follows a row by its key, so no two rows may share one. A row that isn't shown has no element, so its
    // reference is dropped before anything can scroll to it.
    private void ShowRows(List<TItem> rows) => ShowLines([.. rows.Select((row, at) => new GridLine(row, Offset + at))]);

    private void ShowLines(List<GridLine> lines)
    {
        List<TItem> rows = [.. lines.Where(line => line.IsRow).Select(line => line.Item)];
        var shown = rows.Select(KeyOf).ToHashSet();
        if (shown.Count != rows.Count)
        {
            throw new InvalidOperationException($"{nameof(TbDataGrid<TItem>)} has two rows with the same key; " +
                $"give it a {nameof(KeySelector)} that gives each item a different key, such as its id.");
        }

        _rows = rows;
        _lines = lines;
        _rowClick.KeepOnly(shown.Contains);
        _rowExtend.KeepOnly(shown.Contains);
        _choose.KeepOnly(shown.Contains);
        _toggleDetail.KeepOnly(shown.Contains);
        _toggleChildren.KeepOnly(shown.Contains);
        _startEdit.KeepOnly(cell => shown.Contains(cell.Item1) && _columns.Contains(cell.Item2));
        var groups = lines.Where(line => line.Group is not null).Select(line => line.Group!.Key).ToHashSet();
        _toggleGroup.KeepOnly(groups.Contains);
        _ids.Keep(shown);
        foreach (var gone in _rowRefs.Keys.Where(key => !shown.Contains(key)).ToList())
        {
            _rowRefs.Remove(gone);
        }
    }

    private IEnumerable<TItem> Arranged(bool filtered = true) => Sorted((Items ?? []).Where(Matcher(filtered)));

    private Func<TItem, bool> Matcher(bool filtered)
    {
        var filters = filtered ? AppliedFilters.ToList() : [];
        return item => filters.All(applied =>
        {
            var value = applied.Column.FilterValueOf(item);
            return FilterMatch.Matches(value, () => applied.Column.TextOf(value), applied.Filter);
        });
    }

    private IEnumerable<TItem> Sorted(IEnumerable<TItem> items)
    {
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

    // Firefox leaves Shift off the click that Enter or Space makes, so the key says whether Shift is held.
    private void OnSortKeyDown(KeyboardEventArgs args) => _sortKeyHasShift = args.ShiftKey && args.Key is "Enter" or " ";

    private async Task SortAsync(TbColumn<TItem> column, MouseEventArgs args)
    {
        // A pointer's click has a detail, so a key let go elsewhere doesn't carry over to it.
        var shifted = args.ShiftKey || (_sortKeyHasShift && args.Detail == 0);
        _sortKeyHasShift = false;
        // A modified click on a sort link opens it in a new tab or window, so this tab's grid stays as it is.
        if (StateHref is not null && (shifted || args.CtrlKey || args.MetaKey || args.AltKey))
        {
            return;
        }

        Telemetry.Used(TelemetryName, "sort");
        _sorts = NextSorts(column, adding: shifted);
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

        await StateChangedAsync();

        Announcer.Polite(_sorts.FirstOrDefault(sort => sort.Column == column.Key) is { } now
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
        await StateChangedAsync();
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
        await StateChangedAsync();
        await show;
        // The picker isn't a link, so the grid navigates to the new size's address itself, as a link would.
        if (StateHref is not null)
        {
            Navigation.NavigateTo(StateHref(CurrentState));
        }
    }

    // A plain click sorts by the column alone, then turns it around, then stops; adding keeps the other sorts.
    private List<GridSort> NextSorts(TbColumn<TItem> column, bool adding)
    {
        var at = _sorts.FindIndex(sort => sort.Column == column.Key);
        List<GridSort> sorts = [.. _sorts];
        if (!adding && !(at == 0 && _sorts.Count == 1))
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

        return sorts;
    }

    private string SortLink(TbColumn<TItem> column) =>
        StateHref!(CurrentState with { Sorts = NextSorts(column, adding: false), Page = 1 });

    private string PageLink(int page) => StateHref!(CurrentState with { Page = page });

    private GridState CurrentState => new()
    {
        Sorts = [.. _sorts],
        Filters = [.. _filters],
        Groups = [.. _groups],
        Page = _page,
        PageSize = _pageSize,
        ColumnWidths = new Dictionary<string, double>(_widths),
        ColumnOrder = [.. _order],
        HiddenColumns = [.. _hidden],
    };

    private Task StateChangedAsync() => StateChanged.InvokeAsync(CurrentState);

    // A virtualized grid has no pages, so a state's page and page size don't apply to it. A tree grid can't page
    // either, so it ignores the page size, which would turn paging on.
    private void TakeState(GridState state)
    {
        TakeSorts(state.Sorts);
        TakeFilters(state.Filters);
        TakeGroups(state.Groups);
        _page = Virtualize ? 1 : Math.Max(1, state.Page);
        _pageSize = !Virtualize && ChildrenSelector is null && state.PageSize is > 0 ? state.PageSize : null;
        if (!GridState.SameWidths(state.ColumnWidths, _widths))
        {
            _widths = new(state.ColumnWidths);
        }

        if (!state.ColumnOrder.SequenceEqual(_order))
        {
            _order = [.. state.ColumnOrder];
            _rowVersion++;
        }

        if (!state.HiddenColumns.SequenceEqual(_hidden))
        {
            var shownAgain = _hidden.Except(state.HiddenColumns).ToList();
            _hidden = [.. state.HiddenColumns];
            _rowVersion++;
            var unmeasured = shownAgain.FirstOrDefault(key => !_widths.ContainsKey(key)
                && _columns.Any(column => column.Key == key));
            if (ResizableColumns && _widths.Count > 0 && unmeasured is not null)
            {
                _measureColumn = unmeasured;
            }
        }
    }

    // When a binding passes the grid's own sorts, filters or groups back, the grid keeps its own, so the rows aren't
    // arranged again for nothing and groups the user closed stay closed.
    private void TakeSorts(IReadOnlyList<GridSort> sorts)
    {
        if (!sorts.SequenceEqual(_sorts))
        {
            _sorts = [.. sorts];
        }
    }

    private void TakeFilters(IReadOnlyList<GridFilter> filters)
    {
        if (!filters.SequenceEqual(_filters))
        {
            _filters = [.. filters];
            ForgetClosedDrafts();
        }
    }

    private void TakeGroups(IReadOnlyList<string> groups)
    {
        if (!groups.SequenceEqual(_groups))
        {
            _groups = [.. groups];
            _closedGroups.Clear();
        }
    }

    // Filters read back from JSON take their columns' types once the columns are known. Until then, or when a filter's
    // column is gone, they stay as they are and the list isn't replaced, so the rows aren't arranged again.
    private void TypeFilters()
    {
        if (!_filters.Any(FilterJson.IsUntyped))
        {
            return;
        }

        List<GridFilter> typed = [.. _filters.Select(filter => FilterJson.IsUntyped(filter)
            && _columns.FirstOrDefault(column => column.Key == filter.Column) is { } column
                ? FilterJson.Typed(filter, column.FilterType) : filter)];
        if (!typed.SequenceEqual(_filters))
        {
            _filters = typed;
        }
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
            // A selection the parent set itself ends the range; its echo of this grid's own report doesn't.
            if (!_selection.Keys.ToHashSet().SetEquals((SelectedItems ?? []).Select(KeyOf)))
            {
                _rangeAnchor = null;
            }

            _selection.Clear();
            foreach (var item in SelectedItems ?? [])
            {
                _selection[KeyOf(item)] = item;
            }
        }
    }

    private bool IsSelected(TItem item) => _selection.ContainsKey(KeyOf(item));

    private void TakeExpandedFromParent()
    {
        if (!ReferenceEquals(ExpandedItems, _expandedFromParent))
        {
            _expandedFromParent = ExpandedItems;
            _expanded.Clear();
            foreach (var item in ExpandedItems ?? [])
            {
                _expanded[KeyOf(item)] = item;
            }
        }
    }

    private bool IsExpanded(TItem item) => _expanded.ContainsKey(KeyOf(item));

    // A key's text can hold spaces, which an id can't, so it's escaped.
    private string DetailId(object key) => $"{_detailPrefix}-{Uri.EscapeDataString(_ids.IdOf(key))}";

    private async Task ToggleDetailAsync(TItem item)
    {
        var key = KeyOf(item);
        var open = !_expanded.Remove(key);
        if (open)
        {
            if (SingleDetail)
            {
                _expanded.Clear();
            }

            _expanded[key] = item;
        }

        Telemetry.Used(TelemetryName, open ? "open_detail" : "close_detail");
        await ExpandedItemsChanged.InvokeAsync([.. _expanded.Values]);
    }

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
    private Task ToggleAllAsync()
    {
        _rangeAnchor = null;
        return AllSelected ? SetSelectionAsync(_selection.Values.Where(item => !_rows.Any(row => Equals(KeyOf(row), KeyOf(item)))))
            : SelectAllAsync();
    }

    private Task SelectAllAsync()
    {
        _rangeAnchor = null;
        return SetSelectionAsync([.. _selection.Values, .. _rows.Where(row => !IsSelected(row))]);
    }

    private void StartRange(object key)
    {
        _rangeAnchor = key;
        _rangeBase = [.. _selection.Values];
    }

    // A row's handlers find its item by key as they run, since the parent can pass a new instance with the same key.
    private Task OnRowAsync(object key, Func<TItem, Task> handle) =>
        _rows.FindIndex(row => Equals(KeyOf(row), key)) is var at and >= 0 ? handle(_rows[at]) : Task.CompletedTask;

    private async Task ChooseAsync(TItem item)
    {
        await ToggleAsync(item);
        StartRange(KeyOf(item));
    }

    // Selects from the anchor to this row, on top of what the range started with. With no anchor among the rows, as
    // when none was chosen or it has been filtered away, the range starts at this row, which it adds and never toggles.
    private Task ExtendRangeAsync(TItem item)
    {
        var to = _rows.FindIndex(row => Equals(KeyOf(row), KeyOf(item)));
        // The rows can change while a row's event is on its way here, so the row may be gone.
        if (to < 0)
        {
            return Task.CompletedTask;
        }

        var from = _rows.FindIndex(row => Equals(KeyOf(row), _rangeAnchor));
        if (from < 0)
        {
            StartRange(KeyOf(item));
            from = to;
        }

        return SetSelectionAsync([.. _rangeBase, .. _rows.GetRange(Math.Min(from, to), Math.Abs(to - from) + 1)]);
    }

    // With no chosen row among the rows, the row that Shift and an arrow moved from anchors the range.
    private Task OnRowExtendedAsync(TItem item, RowExtendEventArgs args)
    {
        if (!_rows.Exists(row => Equals(KeyOf(row), _rangeAnchor))
            && _rows.FindIndex(row => IdOf(row) == args.From) is >= 0 and var start)
        {
            StartRange(KeyOf(_rows[start]));
        }

        return ExtendRangeAsync(item);
    }

    private bool AllSelected => _rows.Count > 0 && _rows.All(IsSelected);

    private bool SomeSelected => !AllSelected && _rows.Any(IsSelected);

    private async Task OnRowClickedAsync(TItem item, RowClickEventArgs args)
    {
        if (args.Ignored)
        {
            return;
        }

        await OnRowClick.InvokeAsync(item);
        if (RowClickSelects && SelectionMode != SelectionMode.None)
        {
            await (args.ShiftKey && SelectionMode == SelectionMode.Multiple ? ExtendRangeAsync(item) : ChooseAsync(item));
        }
    }

    private object KeyOf(TItem item) => KeySelector?.Invoke(item) ?? item!;

    private string IdOf(TItem item) => _ids.IdOf(KeyOf(item));

    private string LabelOf(TItem item) => RowLabel?.Invoke(item)
        ?? _columns.Select(column => column.TextOf(item)).FirstOrDefault(text => text is not null) ?? IdOf(item);

    // The row handles use the app's text, since DataGridLabels doesn't word them.
    private static readonly SortableLabels RowHandleLabels = new();

    private string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Localizer[key], args);

    private string RangeText(int first, int last) => string.Format(CultureInfo.CurrentCulture,
        Labels.Range ?? Localizer[TbStrings.DataGridRange], first, last, _total);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _load?.Cancel();
        _exporting.Cancel();
        _scrollAfterRender?.Done.TrySetResult();
        _nextRender?.TrySetResult();
        _edit = null;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        if (_reorder is not null)
        {
            await _reorder.DisposeHandleAsync();
        }

        _moveEvents?.Dispose();
    }
}
