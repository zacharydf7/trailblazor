using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Shows items as a tree that expands and collapses, with selection, checkboxes, a filter, children that
/// load when opened, and the full keyboard of a WAI-ARIA tree. Give it your own items, usually a
/// <see cref="KeySelector"/>, and either a <see cref="ChildrenSelector"/> or a <see cref="ParentKeySelector"/>; or
/// write its rows as nested <see cref="TbTreeItem{TItem}"/>s. <typeparamref name="TKey"/> is the type of its keys,
/// which <see cref="KeySelector"/> gives; a tree whose items are their own keys must name it, as with
/// <c>TKey="string"</c>.</summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TbTreeView<TItem, TKey> : IAsyncDisposable, ITreeItemHost<TItem> where TKey : notnull
{
    // The chevron is the same on every row, so it's markup rather than a component each row draws.
    private static readonly MarkupString Chevron = new($"<svg viewBox=\"{BuiltInIcons.ChevronEnd.ViewBox}\" "
        + "fill=\"currentColor\" aria-hidden=\"true\" class=\"" + ClassNames.Join("tb-icon",
            ClassNames.Modifier("tb-icon", Size.Small), BuiltInIcons.ChevronEnd.MirrorsInRightToLeft
                ? "tb-icon--mirrored" : null, "tb-tree__chevron") + $"\">{BuiltInIcons.ChevronEnd.Svg}</svg>");

    private readonly List<TbTreeItem<TItem>> _declaredRoots = [];
    private readonly Dictionary<object, TbTreeItem<TItem>> _declared = [];
    private readonly Dictionary<object, TreeNode<TItem>> _nodes = [];
    private readonly Dictionary<string, TreeNode<TItem>> _byId = [];
    private readonly Dictionary<object, List<TItem>> _loaded = [];
    private readonly Dictionary<object, CancellationTokenSource> _loads = [];
    private readonly HashSet<object> _failed = [];
    private readonly Dictionary<object, bool?> _checkStates = [];
    private readonly Dictionary<object, ElementReference> _rowRefs = [];
    private readonly Dictionary<object, int> _rowIndex = [];
    // Kept across rebuilds, so a row keeps its id and the focus held by that id.
    private readonly KeyIds _ids = new();
    // Rows the filter opened that the user closed; they open again when the filter finds other rows.
    private readonly HashSet<object> _closedWhileFiltering = [];
    private List<TreeNode<TItem>> _roots = [];
    private List<TreeRow<TItem>> _rows = [];
    private int[] _inside = [];
    private int _rowVersion;
    private HashSet<object> _keep = [];
    private HashSet<object> _matches = [];
    private HashSet<object> _matchAncestors = [];
    private HashSet<object> _expanded = [];
    private HashSet<object> _selected = [];
    private HashSet<object> _checked = [];
    private IReadOnlyCollection<TKey>? _expandedFromParent;
    private IReadOnlyCollection<TKey>? _selectedFromParent;
    private IReadOnlyCollection<TKey>? _checkedFromParent;
    private object? _anchor;
    private string? _focusId;
    // The row the browser last said has focus, which a virtualized tree keeps rendered until focus has moved on.
    private string? _browserFocusId;
    // Set while the tree moves focus itself, so the browser's word about a row it passed on the way doesn't undo it.
    private bool _focusing;
    private bool _initialExpandDone;
    private bool _itemsChanged;
    private bool _itemsRegistered;
    private (object Key, bool Focus, TaskCompletionSource Done)? _scrollAfterRender;
    // The scroll the last render drew its row for; one asked for after that render waits for the next.
    private TaskCompletionSource? _scrollRendered;
    // The rows being scrolled to while the browser does it, which a virtualized tree keeps rendered till then.
    private readonly List<object> _scrollingKeys = [];
    // With Virtualize, the rows the browser last said were in view; until it says, the first ones render.
    private int _viewStart;
    private int _viewCount = 40;
    private ElementReference _tree;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<TreeViewEvents>? _events;
    private bool _disposed;
    private readonly ItemHandlers<object, Func<MouseEventArgs, Task>> _rowClick;
    private readonly ItemHandlers<object, Func<Task>> _rowDoubleClick;
    private readonly ItemHandlers<object, Func<Task>> _toggle;
    private readonly ItemHandlers<object, Func<Task>> _toggleCheck;
    private ControlTelemetry? _telemetry;
    private object? _builtFor;
    private object? _flattenedFor;
    private Func<TItem, bool>? _flattenedFilter;
    private TreeScope<TItem>? _scope;
    private int _version;
    // Set by ExpandAllAsync, so declared rows that appear as their parents open are opened too, until a row is closed.
    private int? _expandAllDepth;
    // The open rows before the parent gave new ExpandedKeys, so the declared rows that changed can raise
    // ExpandedChanged.
    private HashSet<object>? _expandedByParent;

    /// <summary>Creates the tree view.</summary>
    public TbTreeView()
    {
        _rowClick = new(key => args => OnNodeAsync(key, node => OnRowClickAsync(node, args)), byValue: true);
        _rowDoubleClick = new(key => () => OnNodeAsync(key, OnRowDoubleClickAsync), byValue: true);
        _toggle = new(key => () => OnNodeAsync(key, ToggleAsync), byValue: true);
        _toggleCheck = new(key => () => OnNodeAsync(key, ToggleCheckAsync), byValue: true);
    }

    /// <summary>The roots, or with a <see cref="ParentKeySelector"/>, every item. The rows are built again when this
    /// is a new collection; call <see cref="RefreshAsync"/> after changing it, or the children it holds, in place.
    /// </summary>
    [Parameter]
    public IEnumerable<TItem> Items { get; set; } = [];

    [CascadingParameter]
    private ITreeChoice? Choice { get; set; }

    /// <summary>The rows as nested <see cref="TbTreeItem{TItem}"/>s, in place of <see cref="Items"/>. Other content
    /// written among them shows where it is written.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gives each item a key that stays the same across renders, such as its id. Expansion, selection,
    /// checks and focus follow keys, so they survive when <see cref="Items"/> is replaced with new instances.
    /// Defaults to the item itself, which suits items compared by value, such as strings and records, when
    /// <typeparamref name="TKey"/> is the item's type. A row's
    /// <c>data-key</c> is the key's text when it is a string, number, date or other formattable value, and a made-up id
    /// such as <c>key-3</c> otherwise, or when that text is empty or another key already has it.</summary>
    [Parameter]
    public Func<TItem, TKey>? KeySelector { get; set; }

    /// <summary>Gives an item's children, for items that hold their own. Call <see cref="RefreshAsync"/> when what it
    /// reads from outside the item changes.</summary>
    [Parameter]
    public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

    /// <summary>Gives an item's parent key, or null for a root, for a flat list of every item. Children keep the
    /// list's order.</summary>
    [Parameter]
    public Func<TItem, object?>? ParentKeySelector { get; set; }

    /// <summary>Loads an item's children the first time it is opened. The token is canceled if the item is closed
    /// first.</summary>
    [Parameter]
    public Func<TItem, CancellationToken, Task<IEnumerable<TItem>>>? LoadChildren { get; set; }

    /// <summary>Whether an item has children to load, so its toggle shows before they do. Defaults to true for every
    /// item when <see cref="LoadChildren"/> is set.</summary>
    [Parameter]
    public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>The text of an item's row, and what typing a letter finds. Defaults to the item's own text.</summary>
    [Parameter]
    public Func<TItem, string>? ItemText { get; set; }

    /// <summary>An icon before an item's text.</summary>
    [Parameter]
    public Func<TItem, Icon?>? ItemIcon { get; set; }

    /// <summary>CSS classes for an item's row, such as one that marks rows needing attention.</summary>
    [Parameter]
    public Func<TItem, string?>? ItemClass { get; set; }

    /// <summary>Extra CSS classes for the tree's inner parts.</summary>
    [Parameter]
    public TreeViewClasses? Classes { get; set; }

    /// <summary>Draws a row's content instead of its icon and text.</summary>
    [Parameter]
    public RenderFragment<TreeItemContext<TItem, TKey>>? ItemTemplate { get; set; }

    /// <summary>Content at the end of a row, such as badges or action buttons. Clicks and keys in it never select,
    /// expand or activate the row.</summary>
    [Parameter]
    public RenderFragment<TreeItemContext<TItem, TKey>>? ItemEnd { get; set; }

    /// <summary>The keys of the open rows. Bind it with <c>@bind-ExpandedKeys</c> to open rows from code or to save
    /// them. A <see cref="TbTreeItem{TItem}"/> with no value has no key, so it opens and closes only by the user,
    /// <see cref="ExpandAllAsync"/> and <see cref="CollapseAllAsync"/>, or as a row inside it is opened.</summary>
    [Parameter]
    public IReadOnlyCollection<TKey>? ExpandedKeys { get; set; }

    /// <summary>Raised when rows open or close.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TKey>> ExpandedKeysChanged { get; set; }

    /// <summary>How many levels open on the first render, when <see cref="ExpandedKeys"/> isn't given. Defaults to
    /// 0.</summary>
    [Parameter]
    public int InitialExpandDepth { get; set; }

    /// <summary>How many rows can be selected. Defaults to <see cref="SelectionMode.Single"/>, since a tree usually
    /// selects one row, as in the WAI-ARIA tree pattern; a data grid defaults to none.</summary>
    [Parameter]
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>Selects each row the keys move focus to, with the arrows, Home, End or typing, so the selection is
    /// always the focused row, as in a list of files with a preview beside it. Ctrl or Cmd with a key moves focus
    /// alone. Applies to <see cref="SelectionMode.Single"/>.</summary>
    [Parameter]
    public bool SelectionFollowsFocus { get; set; }

    /// <summary>Whether typing a row's first letters moves focus to it. Defaults to true; turn it off when the page has
    /// single-letter shortcuts that should still work while the tree has focus.</summary>
    [Parameter]
    public bool TypeAhead { get; set; } = true;

    /// <summary>The keys of the selected rows. Bind it with <c>@bind-SelectedKeys</c>.</summary>
    [Parameter]
    public IReadOnlyCollection<TKey>? SelectedKeys { get; set; }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TKey>> SelectedKeysChanged { get; set; }

    /// <summary>Raised when a row is activated with Enter or a double-click. Either one also opens or closes a row with
    /// children.</summary>
    [Parameter]
    public EventCallback<TItem> OnActivate { get; set; }

    /// <summary>Whether a row can be selected, so group and heading rows can't. Defaults to every row.</summary>
    [Parameter]
    public Func<TItem, bool>? IsSelectable { get; set; }

    /// <summary>Whether a row is turned off: it can be focused but not selected, checked or activated.</summary>
    [Parameter]
    public Func<TItem, bool>? IsDisabled { get; set; }

    /// <summary>Shows a checkbox on each row, apart from selection.</summary>
    [Parameter]
    public bool Checkable { get; set; }

    /// <summary>The keys of the checked rows. Bind it with <c>@bind-CheckedKeys</c>.</summary>
    [Parameter]
    public IReadOnlyCollection<TKey>? CheckedKeys { get; set; }

    /// <summary>Raised when checks change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TKey>> CheckedKeysChanged { get; set; }

    /// <summary>Checking a row checks the rows inside it, and a parent shows whether all, some or none of them are
    /// checked. Defaults to true.</summary>
    [Parameter]
    public bool CascadeChecks { get; set; } = true;

    /// <summary>Shows only the rows that match and the rows they sit in, which open to show them. It runs again
    /// whenever a new delegate is passed, as markup does on every render; for one kept in a field, call
    /// <see cref="RefreshAsync"/> when what it reads changes.</summary>
    [Parameter]
    public Func<TItem, bool>? Filter { get; set; }

    /// <summary>Also shows every row inside a match.</summary>
    [Parameter]
    public bool FilterKeepsDescendants { get; set; }

    /// <summary>Shown instead of the rows when there are none.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Raised when <see cref="LoadChildren"/> fails; the row offers to try again.</summary>
    [Parameter]
    public EventCallback<Exception> OnLoadFailed { get; set; }

    /// <summary>Names the tree for screen readers.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Turns the whole tree off.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words the tree shows and says to screen readers.</summary>
    [Parameter]
    public TreeViewLabels Labels { get; set; } = new();

    /// <summary>Raised when the user moves rows, by dragging them or with Alt+Shift and the arrow keys, with what
    /// moved and where to. The tree doesn't move them: apply the move to your items, so it can be undone like any
    /// other change, and the tree builds its rows again from them once this returns. Setting it lets rows be
    /// dragged.</summary>
    [Parameter]
    public EventCallback<TreeDrop<TItem>> OnDrop { get; set; }

    /// <summary>Decides which rows can be moved. When null, every row that isn't disabled can be moved.</summary>
    [Parameter]
    public Func<TItem, bool>? CanDrag { get; set; }

    /// <summary>Asked whether a move is allowed, as a drag passes over a place and before a keyboard move. Refuse
    /// with <see cref="MoveCheck.Refuse"/> and a reason, which shows beside the place and is read out. An item can
    /// never move into itself, whatever this says.</summary>
    [Parameter]
    public Func<TreeDrop<TItem>, Task<MoveCheck>>? CanDrop { get; set; }

    /// <summary>A slot below each row, such as an "insert here" button, which shows while the pointer is on the row's
    /// lower edge or the slot has keyboard focus. Until it shows, it takes the pointer only on that edge, clear of the
    /// rows' content, so a click on a row's text selects the row. Its context says where an item inserted there would
    /// go; the first row also has one above it.</summary>
    [Parameter]
    public RenderFragment<TreeInsertContext<TItem>>? InsertTemplate { get; set; }

    /// <summary>Renders only the rows in view, and a few either side, so a tree of tens of thousands of rows stays
    /// quick. Every row is then <see cref="ItemSize"/> tall. Give the tree a height to scroll in, or let the page
    /// scroll it. It needs <see cref="Items"/>, since <see cref="TbTreeItem{TItem}"/>s draw their rows where they are
    /// written.</summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>The height of every row, in pixels, when <see cref="Virtualize"/> is on. Defaults to 32, the
    /// comfortable row height; use 28 with <see cref="Density.Compact"/>.</summary>
    [Parameter]
    public float ItemSize { get; set; } = 32;

    /// <summary>A label the tree's use is counted under, such as <c>"folders"</c>, so the app can see how people use it
    /// through the <c>Trailblazor</c> meter: shown when it renders, and used with the action <c>expand</c>,
    /// <c>collapse</c>, <c>select</c>, <c>check</c>, <c>activate</c> or <c>move</c> each time the user does one.
    /// Opening rows from code isn't counted, and which rows is never recorded. It names the tree, never the
    /// user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IBrowserService Browser { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    private string TreeId { get; } = $"tb-tree-{Guid.NewGuid():N}";

    private string MoveHintId => $"{TreeId}-move-hint";

    // The row that takes the tree's one tab stop: the last focused, else the first selected, else the first.
    private string? FocusId()
    {
        if (KeyOfId(_focusId) is { } focused && _rowIndex.ContainsKey(focused))
        {
            return _focusId;
        }

        var selected = _selected.Select(key => _rowIndex.GetValueOrDefault(key, _rows.Count)).Append(_rows.Count).Min();
        return selected < _rows.Count ? _rows[selected].Node.Id : _rows.Count > 0 ? _rows[0].Node.Id : null;
    }

    private object? KeyOfId(string? id) => id is not null && _byId.TryGetValue(id, out var node) ? node.Key : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _rowVersion++;
        if (ChildContent is not null && (Items.Any() || ChildrenSelector is not null || ParentKeySelector is not null
            || LoadChildren is not null))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)} takes {nameof(Items)} or " +
                $"{nameof(TbTreeItem<TItem>)}s, not both.");
        }

        if (ParentKeySelector is not null && (ChildrenSelector is not null || LoadChildren is not null))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)} takes a " +
                $"{nameof(ParentKeySelector)} or a {nameof(ChildrenSelector)} and {nameof(LoadChildren)}, not both.");
        }

        if (ChildContent is not null && Virtualize)
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)} virtualizes only " +
                $"{nameof(Items)}, since {nameof(TbTreeItem<TItem>)}s draw their rows where they are written.");
        }

        if (KeySelector is null && !typeof(TKey).IsAssignableFrom(typeof(TItem)))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)} needs a {nameof(KeySelector)} " +
                "when its items aren't their own keys.");
        }

        var expanded = _expanded;
        _expanded = TakeFromParent(ExpandedKeys, ref _expandedFromParent, _expanded);
        _expandedByParent ??= ReferenceEquals(expanded, _expanded) ? null : expanded;
        // A parent closing rows stops ExpandAllAsync opening new ones, as the user closing a row does.
        if (!expanded.IsSubsetOf(_expanded))
        {
            _expandAllDepth = null;
        }
        _selected = TakeFromParent(SelectedKeys, ref _selectedFromParent, _selected);
        _checked = TakeFromParent(CheckedKeys, ref _checkedFromParent, _checked);
        // A parent render builds or lays out the rows again only when what they come from changed.
        if (!BuiltFor.Equals(_builtFor))
        {
            Rebuild();
        }
        // A filter compares by instance, since one from the same method can read new search text.
        else if (!ReferenceEquals(Filter, _flattenedFilter) || !FlattenedFor.Equals(_flattenedFor))
        {
            Flatten();
        }
    }

    // Markup makes a new delegate on every render, so selectors are compared by method.
    private object BuiltFor => (Items, ChildContent, KeySelector?.Method, ChildrenSelector?.Method,
        ParentKeySelector?.Method, HasChildren?.Method, LoadChildren is null);

    // A tree must hold rows, so one with none is a group around its message, which can hold a button of the app's own.
    private bool ShowsEmpty => _rows.Count == 0 && (ChildContent is null || _itemsRegistered);

    private object FlattenedFor => (FilterKeepsDescendants, Checkable, CascadeChecks, _expanded, _checked);

    /// <summary>Builds the rows again from <see cref="Items"/> after they, or the children they hold, changed in
    /// place, or after what <see cref="KeySelector"/>, <see cref="ChildrenSelector"/>,
    /// <see cref="ParentKeySelector"/> or <see cref="HasChildren"/> read from outside the item changed; a parent render
    /// reads them again only for a new collection. Children already loaded with <see cref="LoadChildren"/> stay.
    /// </summary>
    public Task RefreshAsync()
    {
        _rowVersion++;
        Rebuild();
        StateHasChanged();
        return LoadOpenRowsAsync();
    }

    private void Rebuild()
    {
        Build();
        // Items that arrive after the first render, as loaded data does, still open to the initial depth; a declared
        // row given Expanded stays as its parent says.
        if (!_initialExpandDone && _nodes.Count > 0)
        {
            _initialExpandDone = true;
            if (ExpandedKeys is null)
            {
                _expanded.UnionWith(_nodes.Values.Where(node => node.Level <= InitialExpandDepth)
                    .Where(node => node.Children.Count > 0
                        && _declared.GetValueOrDefault(node.Key)?.ExpandedGiven != true)
                    .Select(node => node.Key));
            }
        }

        Flatten();
    }

    private TreeScope<TItem> Scope => _scope ??= new(this, null);

    // The name of the cascading render count that makes declared items draw their rows again.
    internal const string TreeVersion = "TbTreeVersion";

    // Set for a tree inside another tree's row, whose clicks mustn't also select that row.
    [CascadingParameter]
    private ITreeScope? Enclosing { get; set; }

    private bool Nested => Enclosing is not null;

    // A declared row with no value is keyed by its TbTreeItem, which stays inside the tree and out of the bound keys.
    private static bool HasValue(TreeNode<TItem> node) => node.Key is not TbTreeItem<TItem>;

    // A row with no value is checked only through the rows inside it, as its own check would mean nothing. A row a
    // tree select can't choose gets a checkbox only while checks cascade and it may hold rows, as otherwise its check
    // would reach nothing.
    private bool CanCheck(TreeNode<TItem> node) => Checkable
        && ((HasValue(node)
                && (Choice is null || (CascadeChecks && node.HasChildren) || IsSelectable?.Invoke(node.Item) != false))
            || (CascadeChecks && node.Children.Count > 0));

    private static List<TKey> Reported(IEnumerable<object> keys) =>
        [.. keys.Where(key => key is not TbTreeItem<TItem>).Cast<TKey>()];

    // Tree items register as the tree renders them, so the tree builds its rows from them after that render.
    void ITreeItemHost<TItem>.AddItem(TbTreeItem<TItem> item, TbTreeItem<TItem>? parent)
    {
        (parent?.Children ?? _declaredRoots).Add(item);
        ItemChanged();
    }

    void ITreeItemHost<TItem>.RemoveItem(TbTreeItem<TItem> item, TbTreeItem<TItem>? parent)
    {
        (parent?.Children ?? _declaredRoots).Remove(item);
        ItemChanged();
    }

    private void ItemChanged()
    {
        _itemsChanged = true;
        StateHasChanged();
    }

    // Only a new set from the parent replaces the tree's own, so a parent rendering again doesn't undo the user.
    private static HashSet<object> TakeFromParent(IReadOnlyCollection<TKey>? fromParent,
        ref IReadOnlyCollection<TKey>? last, HashSet<object> current)
    {
        if (ReferenceEquals(fromParent, last))
        {
            return current;
        }

        last = fromParent;
        if (fromParent is null)
        {
            return current;
        }

        // The tree's own set passed back, as a binding does, keeps it, so the rows aren't laid out again for nothing.
        // Rows with no value can't be in the parent's set, so they keep their state.
        HashSet<object> taken =
            [.. fromParent.Select(key => (object)key), .. current.Where(key => key is TbTreeItem<TItem>)];
        return taken.SetEquals(current) ? current : taken;
    }

    // Rows opened by a set from the parent, such as one saved earlier, load like rows the user opens.
    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (_expandedByParent is { } before)
        {
            _expandedByParent = null;
            await ReportExpansionAsync(before, null);
        }

        await LoadOpenRowsAsync();
    }

    private Task LoadOpenRowsAsync() => Task.WhenAll(_expanded.Select(key => _nodes.GetValueOrDefault(key))
        .OfType<TreeNode<TItem>>().Where(node => node.MayHaveChildren && !_failed.Contains(node.Key))
        .ToList().Select(LoadIfNeededAsync));

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        try
        {
            if (firstRender && RendererInfo.IsInteractive)
            {
                await AttachAsync();
            }

            if (RendererInfo.IsInteractive && (_itemsChanged || (firstRender && ChildContent is not null)))
            {
                // Declared rows the initial depth opens raise ExpandedChanged, as rows opened any other way do.
                var before = _expanded.ToHashSet();
                BuildFromItems();
                await ReportExpansionAsync(before, null);
                // Rows that appeared inside rows ExpandAllAsync opened are opened too.
                if (_expandAllDepth is { } depth)
                {
                    await SetExpandedAsync(_expanded.Union(Expandable(depth)));
                }
            }

            if (_scrollAfterRender is { } scroll && scroll.Done == _scrollRendered)
            {
                _scrollAfterRender = null;
                _scrollingKeys.Add(scroll.Key);
                try
                {
                    if (_rowIndex.ContainsKey(scroll.Key) && _rowRefs.TryGetValue(scroll.Key, out var row))
                    {
                        await Browser.ScrollIntoViewAsync(row);
                        if (scroll.Focus)
                        {
                            await row.FocusAsync();
                        }
                    }
                }
                finally
                {
                    _scrollingKeys.Remove(scroll.Key);
                    _focusing &= _scrollAfterRender is not null || _scrollingKeys.Count > 0;
                    scroll.Done.TrySetResult();
                }
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the tree went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/tree-view.js");
        if (_disposed)
        {
            return;
        }

        _events = DotNetObjectReference.Create(new TreeViewEvents(OnFocusAsync, OnKeyAsync, OnExtendAsync,
            OnItemOrderAsync, OnDragOverAsync, OnViewAsync, OnFindAsync));
        var handle = await module.InvokeAsync<IJSObjectReference?>("createTreeView", _tree, _events);
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

    // Declared items that have appeared become rows only after the render they appear in, such as those of a row
    // that renders its rows once open, so until then the tree is busy.
    private bool Building =>
        ChildContent is not null && RendererInfo.IsInteractive && (_itemsChanged || !_itemsRegistered);

    private void BuildFromItems()
    {
        _itemsChanged = false;
        _itemsRegistered = true;
        Rebuild();
        StateHasChanged();
    }

    // Items register in the order they first render, and a keyed item can move without telling the tree, so the
    // browser reports the order of their markup.
    private Task OnItemOrderAsync(string[] ids)
    {
        var place = ids.Select((id, index) => (id, index)).ToDictionary(pair => pair.id, pair => pair.index);
        SortByPlace(_declaredRoots);
        Rebuild();
        StateHasChanged();
        return Task.CompletedTask;

        void SortByPlace(List<TbTreeItem<TItem>> items)
        {
            items.Sort((first, second) =>
                place.GetValueOrDefault(first.Id).CompareTo(place.GetValueOrDefault(second.Id)));
            items.ForEach(item => SortByPlace(item.Children));
        }
    }

    /// <summary>Opens every row that has children, or only the rows down to <paramref name="depth"/>. Rows whose
    /// children haven't loaded stay closed, so this never starts a load; a <see cref="TbTreeItem{TItem}"/> with
    /// <c>HasChildren</c> opens, and so do the rows that appear inside it.</summary>
    public async Task ExpandAllAsync(int? depth = null)
    {
        _expandAllDepth = depth ?? int.MaxValue;
        await SetExpandedAsync(_expanded.Union(Expandable(_expandAllDepth.Value)));
    }

    private IEnumerable<object> Expandable(int depth) => _nodes.Values
        .Where(node => (node.Children.Count > 0 || (node.MayHaveChildren && LoadChildren is null))
            && node.Level <= depth)
        .Select(node => node.Key);

    /// <summary>Closes every row.</summary>
    public Task CollapseAllAsync() => SetExpandedAsync([]);

    /// <summary>Opens the row with this key, and the rows it sits in, loading its children if they haven't
    /// loaded.</summary>
    public Task ExpandAsync(TKey key) => OpenAsync(key);

    private async Task OpenAsync(object key)
    {
        if (!_nodes.TryGetValue(key, out var node))
        {
            return;
        }

        if (Reveal(node.Ancestors().Prepend(node)))
        {
            Flatten();
            StateHasChanged();
        }

        await SetExpandedAsync(_expanded.Union(node.Ancestors().Select(ancestor => ancestor.Key)).Append(node.Key));
        await LoadIfNeededAsync(node);
    }

    /// <summary>Closes the row with this key, even one the filter opened.</summary>
    public Task CollapseAsync(TKey key) => CloseAsync(key);

    private async Task CloseAsync(object key, TbTreeItem<TItem>? asking = null)
    {
        if (_matchAncestors.Contains(key) && _closedWhileFiltering.Add(key))
        {
            Flatten();
            StateHasChanged();
        }

        await SetExpandedAsync(_expanded.Where(other => !Equals(other, key)), asking);
    }

    /// <summary>Makes the row with this key the only selected row, and raises <see cref="SelectedKeysChanged"/> if
    /// that changes the selection. Focus stays where it is. A key no row has, or a row that can't be selected,
    /// changes nothing.</summary>
    public async Task SelectAsync(TKey key)
    {
        if (_nodes.TryGetValue(key, out var node) && CanSelect(node))
        {
            _anchor = node.Key;
            await SetSelectedAsync([node.Key], byUser: false);
            StateHasChanged();
        }
    }

    /// <summary>Opens the rows the row with this key sits in, and scrolls it into view, focusing it if asked.
    /// </summary>
    public async Task ScrollToAsync(TKey key, bool focus = false)
    {
        if (!_nodes.TryGetValue(key, out var node))
        {
            return;
        }

        if (Reveal(node.Ancestors()))
        {
            Flatten();
        }

        await SetExpandedAsync(_expanded.Union(node.Ancestors().Select(ancestor => ancestor.Key)));
        await ScrollAfterRenderAsync(node, focus);
    }

    // Focus from outside, such as a tree select's search, goes to the first shown match or else the tab-stop row.
    internal async Task FocusRowAsync(Func<TItem, bool>? match)
    {
        var node = (match is null ? null : _rows.Where(row => row.IsItem).Select(row => row.Node)
            .FirstOrDefault(node => match(node.Item))) ?? (FocusId() is { } id ? _byId[id] : null);
        if (node is not null)
        {
            await ScrollAfterRenderAsync(node, focus: true);
        }
    }

    private async Task ScrollAfterRenderAsync(TreeNode<TItem> node, bool focus)
    {
        if (focus)
        {
            _focusId = node.Id;
        }

        _focusing = focus;
        // A newer scroll replaces one still waiting for its render, which then ends without scrolling.
        _scrollAfterRender?.Done.TrySetResult();
        var done = new TaskCompletionSource();
        _scrollAfterRender = (node.Key, focus, done);
        StateHasChanged();
        await done.Task;
    }

    // The rows to render: every one, or with Virtualize the rows in view, with the focused row, the row the browser
    // still has focus on, the row being scrolled to and their neighbors, so focus and the arrow keys always have a row
    // to land on. A static page renders all.
    private IEnumerable<int> ShownIndexes(string? focusId)
    {
        if (!Virtualize || !RendererInfo.IsInteractive)
        {
            return Enumerable.Range(0, _rows.Count);
        }

        var start = Math.Clamp(_viewStart, 0, Math.Max(0, _rows.Count - _viewCount));
        var shown = new SortedSet<int>(Enumerable.Range(start, Math.Min(_viewCount, _rows.Count - start)));
        object?[] pinned = [KeyOfId(focusId), KeyOfId(_browserFocusId), _scrollAfterRender?.Key, .. _scrollingKeys];
        foreach (var key in pinned)
        {
            if (key is not null && _rowIndex.TryGetValue(key, out var index))
            {
                shown.UnionWith(Enumerable.Range(index - 1, 3).Where(near => near >= 0 && near < _rows.Count));
            }
        }

        return shown;
    }

    private Task OnViewAsync(int start, int count)
    {
        if (start != _viewStart || count != _viewCount)
        {
            (_viewStart, _viewCount) = (start, count);
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    // A virtualized tree finds typed text here, since the row it finds may not be rendered.
    private async Task OnFindAsync(string id, string typed)
    {
        var shown = _rows.Where(row => row.IsItem).Select(row => row.Node).ToList();
        var from = Moving(id);
        var index = shown.FindIndex(node => node.Id == from);
        var found = Typeahead.Find(shown.Count, place => TextOf(shown[place]), _ => false, index, typed);
        if (found >= 0)
        {
            var match = shown[found];
            if (FollowsFocus)
            {
                await SelectAsync(match, shift: false, toggle: false);
            }

            await ScrollAfterRenderAsync(match, focus: true);
        }
    }

    private async Task MoveFocusAsync(string id, string key, bool extend, bool follow)
    {
        if (!_byId.TryGetValue(id, out var node) || FocusTarget(node, key) is not { } target)
        {
            return;
        }

        if (extend && SelectionMode == SelectionMode.Multiple && key is "Next" or "Previous" or "First" or "Last")
        {
            await ExtendAsync(target, node);
        }
        else if (follow)
        {
            await SelectAsync(target, shift: false, toggle: false);
        }

        await ScrollAfterRenderAsync(target, focus: true);
    }

    // The row a key moves focus to in a virtualized tree, when the browser hasn't rendered it.
    private TreeNode<TItem>? FocusTarget(TreeNode<TItem> node, string key)
    {
        var shown = _rows.Where(row => row.IsItem).Select(row => row.Node).ToList();
        var index = shown.IndexOf(node);
        return key switch
        {
            "Next" => shown.ElementAtOrDefault(index + 1),
            "Previous" => index > 0 ? shown[index - 1] : null,
            "First" => shown.FirstOrDefault(),
            "Last" => shown.LastOrDefault(),
            _ => node.Parent,
        };
    }

    // Opening a row from code opens the rows above it too, even ones the user closed while the filter was on.
    private bool Reveal(IEnumerable<TreeNode<TItem>> nodes)
    {
        var count = _closedWhileFiltering.Count;
        _closedWhileFiltering.ExceptWith(nodes.Select(node => node.Key));
        return _closedWhileFiltering.Count != count;
    }

    // Rebuilds the nodes, then forgets the ids of keys no longer in the tree; those still in it keep theirs.
    private void Build()
    {
        BuildNodes();
        _ids.Keep(_nodes.Keys.ToHashSet());
        _rowClick.KeepOnly(_nodes.ContainsKey);
        _rowDoubleClick.KeepOnly(_nodes.ContainsKey);
        _toggle.KeepOnly(_nodes.ContainsKey);
        _toggleCheck.KeepOnly(_nodes.ContainsKey);
        _builtFor = BuiltFor;
    }

    private void BuildNodes()
    {
        _nodes.Clear();
        _byId.Clear();
        _declared.Clear();
        _roots = [];
        // Rendered statically, each tree item shows its own row instead.
        if (ChildContent is not null)
        {
            if (RendererInfo.IsInteractive)
            {
                _roots.AddRange(_declaredRoots.Select(item => AddDeclared(item, null)));
            }

            return;
        }

        if (ParentKeySelector is not { } parentOf)
        {
            _roots.AddRange(Items.Select(item => AddWithChildren(item, null)));
            return;
        }

        var items = Items.ToList();
        if (items.Select(parentOf).FirstOrDefault(parent => parent is not null and not TKey) is { } wrong)
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)}'s {nameof(ParentKeySelector)} " +
                $"must give a key of type {typeof(TKey).Name} or null for a root, but gave a {wrong.GetType().Name}.");
        }

        // A parent that isn't in the list leaves the item at the root, rather than losing it.
        var keys = items.Select(KeyOf).ToHashSet();
        var childrenOf = items.Where(item => parentOf(item) is { } parent && keys.Contains(parent))
            .ToLookup(item => parentOf(item)!);
        _roots.AddRange(items.Where(item => parentOf(item) is not { } parent || !keys.Contains(parent))
            .Select(item => Place(item, null)));
        if (_nodes.Count < items.Count)
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)} has items whose parents lead " +
                $"back to themselves, such as {items.Select(KeyOf).First(key => !_nodes.ContainsKey(key))}.");
        }

        TreeNode<TItem> Place(TItem item, TreeNode<TItem>? parent)
        {
            var node = Add(item, parent);
            node.Children.AddRange(childrenOf[node.Key].Select(child => Place(child, node)));
            return node;
        }
    }

    private TreeNode<TItem> AddDeclared(TbTreeItem<TItem> item, TreeNode<TItem>? parent)
    {
        var node = Add(item.Value, parent, item.Key);
        _declared[node.Key] = item;
        node.Children.AddRange(item.Children.Select(child => AddDeclared(child, node)));
        node.MayHaveChildren = item.HasChildren && node.Children.Count == 0;
        return node;
    }

    private TreeNode<TItem> AddWithChildren(TItem item, TreeNode<TItem>? parent)
    {
        var node = Add(item, parent);
        var children = ChildrenSelector?.Invoke(item)
            ?? (_loaded.TryGetValue(node.Key, out var loaded) ? loaded : null);
        if (children is not null)
        {
            node.Children.AddRange(children.Select(child => AddWithChildren(child, node)));
        }
        else if (LoadChildren is not null)
        {
            node.MayHaveChildren = HasChildren?.Invoke(item) ?? true;
        }

        return node;
    }

    private TreeNode<TItem> Add(TItem item, TreeNode<TItem>? parent, object? key = null)
    {
        key ??= KeyOf(item);
        var id = _ids.IdOf(key);
        if (_nodes.ContainsKey(key))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem, TKey>)} has two items with the key " +
                $"{key}; each item's {nameof(KeySelector)} must give a different key.");
        }

        var node = new TreeNode<TItem>(item, key, id, parent);
        _nodes[key] = node;
        _byId[id] = node;
        return node;
    }

    // The visible rows, in order, with the place each holds among its siblings.
    private void Flatten()
    {
        ComputeFilter();
        ComputeChecks();
        _rows = [];
        _rowIndex.Clear();
        Walk(_roots);
        CountInside();
        _flattenedFor = FlattenedFor;
        _flattenedFilter = Filter;
        // A row that isn't shown has no element, so its reference is dropped before anything can scroll to it.
        foreach (var hidden in _rowRefs.Keys.Where(key => !_rowIndex.ContainsKey(key)).ToList())
        {
            _rowRefs.Remove(hidden);
        }

        void Walk(List<TreeNode<TItem>> siblings)
        {
            var shown = Filter is null ? siblings : siblings.Where(node => _keep.Contains(node.Key)).ToList();
            for (var index = 0; index < shown.Count; index++)
            {
                var node = shown[index];
                _rowIndex[node.Key] = _rows.Count;
                _rows.Add(new TreeRow<TItem>(node, shown.Count, index + 1, TreeRowKind.Item));
                if (!IsOpen(node))
                {
                    continue;
                }

                if (_loads.ContainsKey(node.Key))
                {
                    _rows.Add(new TreeRow<TItem>(node, 1, 1, TreeRowKind.Loading));
                }
                else if (_failed.Contains(node.Key))
                {
                    _rows.Add(new TreeRow<TItem>(node, 1, 1, TreeRowKind.Failed));
                }
                else
                {
                    Walk(node.Children);
                }
            }
        }
    }

    // How many rows each row holds below it, so the testing helpers can step over a branch a virtualized tree hasn't
    // rendered.
    private void CountInside()
    {
        _inside = new int[_rows.Count];
        var open = new Stack<int>();
        for (var index = 0; index <= _rows.Count; index++)
        {
            var level = index < _rows.Count ? LevelOf(_rows[index]) : 0;
            while (open.Count > 0 && LevelOf(_rows[open.Peek()]) >= level)
            {
                var done = open.Pop();
                _inside[done] = index - done - 1;
            }

            open.Push(index);
        }

        static int LevelOf(TreeRow<TItem> row) => row.IsItem ? row.Node.Level : row.Node.Level + 1;
    }

    private bool IsOpen(TreeNode<TItem> node) => node.HasChildren && !_closedWhileFiltering.Contains(node.Key)
        && (_expanded.Contains(node.Key) || _matchAncestors.Contains(node.Key));

    private void ComputeFilter()
    {
        var before = _matches;
        _keep = [];
        _matches = [];
        _matchAncestors = [];
        if (Filter is not { } filter)
        {
            _closedWhileFiltering.Clear();
            return;
        }

        foreach (var node in _nodes.Values.Where(node => HasValue(node) && filter(node.Item)))
        {
            _matches.Add(node.Key);
            _keep.Add(node.Key);
            foreach (var ancestor in node.Ancestors())
            {
                _keep.Add(ancestor.Key);
                _matchAncestors.Add(ancestor.Key);
            }

            if (FilterKeepsDescendants)
            {
                _keep.UnionWith(node.Descendants().Select(descendant => descendant.Key));
            }
        }

        if (!_matches.SetEquals(before))
        {
            _closedWhileFiltering.Clear();
        }
    }

    // A parent is checked when every row inside it is, unchecked when none is, and mixed in between.
    private void ComputeChecks()
    {
        _checkStates.Clear();
        if (!Checkable)
        {
            return;
        }

        foreach (var root in _roots)
        {
            StateOf(root);
        }

        bool? StateOf(TreeNode<TItem> node)
        {
            bool? state = _checked.Contains(node.Key);
            var states = node.Children.Select(StateOf).Distinct().ToList();
            if (CascadeChecks && states.Count > 0)
            {
                state = states.Count == 1 ? states[0] : null;
            }

            _checkStates[node.Key] = state;
            return state;
        }
    }

    private Task OnFocusAsync(string id)
    {
        _browserFocusId = id;
        if (!_focusing)
        {
            _focusId = id;
        }

        return Task.CompletedTask;
    }

    // Keys pressed while the tree is still moving focus count from where it is moving to, as they would once there.
    private string Moving(string id) => _focusing && _focusId is { } target ? target : id;

    private async Task OnKeyAsync(string id, string key, bool shift, bool ctrl)
    {
        // Moving focus works in a disabled tree too, as it does among rendered rows.
        if (key is "Next" or "Previous" or "First" or "Last" or "Parent")
        {
            await MoveFocusAsync(Moving(id), key, shift && !Disabled, FollowsFocus && !ctrl);
            return;
        }

        if (!_byId.TryGetValue(id, out var node) || Disabled)
        {
            return;
        }

        // A drag resting on a closed row opens it, and the tab stop stays with the row being dragged.
        if (key == "Open")
        {
            CountOpening(node, open: true);
            await OpenAsync(node.Key);
            StateHasChanged();
            return;
        }

        _focusId = id;
        switch (key)
        {
            case "ArrowRight":
                CountOpening(node, open: true);
                await OpenAsync(node.Key);
                break;
            case "ArrowLeft":
                CountOpening(node, open: false);
                await CloseAsync(node.Key);
                break;
            case "*":
                if (Siblings(node).Any(sibling => sibling.HasChildren && !IsOpen(sibling)))
                {
                    Telemetry.Used(TelemetryName, "expand");
                }

                await SetExpandedAsync(_expanded.Union(Siblings(node).Where(sibling => sibling.HasChildren)
                    .Select(sibling => sibling.Key)));
                await Task.WhenAll(Siblings(node).Select(LoadIfNeededAsync));
                break;
            case "Enter" when Checkable && Choice is not null:
                await ToggleCheckAsync(node);
                break;
            case "Enter":
                await SelectAsync(node, shift: false, toggle: false);
                await ActivateAsync(node);
                await ToggleIfParentAsync(node);
                break;
            case " " when Checkable:
                await ToggleCheckAsync(node);
                break;
            case " ":
                await SelectAsync(node, shift, toggle: SelectionMode == SelectionMode.Multiple);
                break;
            case "Follow" when FollowsFocus:
                await SelectAsync(node, shift: false, toggle: false);
                break;
            case "MoveUp" or "MoveDown" or "Indent" or "Outdent" when CanDragRow(node):
                await MoveByKeyAsync(node, key);
                break;
            case "SelectAll" when SelectionMode == SelectionMode.Multiple:
                await SetSelectedAsync(_rows.Where(row => row.IsItem && CanSelect(row.Node))
                    .Select(row => row.Node.Key));
                break;
        }

        StateHasChanged();
    }

    private IEnumerable<TreeNode<TItem>> Siblings(TreeNode<TItem> node) => node.Parent?.Children ?? _roots;

    private bool CanDragRow(TreeNode<TItem> node) => OnDrop.HasDelegate && HasValue(node) && !Disabled
        && !IsItemDisabled(node) && CanDrag?.Invoke(node.Item) != false;

    // A slot below a row inserts as its first child when its children show under it, and otherwise just after it. A
    // row with no value can't be named as the parent, so no slot goes inside one.
    private TreeInsertContext<TItem>? InsertBelow(TreeNode<TItem> node, TreeItemContext<TItem, TKey> context)
    {
        var inside = context.Expanded && node.Children.Count > 0;
        if ((inside ? node : node.Parent) is { } parent && !HasValue(parent))
        {
            return null;
        }

        return inside ? new(node.Item, 0)
            : new(node.Parent is { } above ? above.Item : default, Siblings(node).ToList().IndexOf(node) + 1);
    }

    // A row inside another row that moves goes with it, so only the outermost rows are moved.
    private List<TreeNode<TItem>> NodesOf(IEnumerable<string> ids)
    {
        var nodes = ids.Select(id => _byId.GetValueOrDefault(id)).OfType<TreeNode<TItem>>().ToList();
        return [.. nodes.Where(node => !node.Ancestors().Any(nodes.Contains))];
    }

    private async Task<MoveCheck> CheckDropAsync(IReadOnlyList<TreeNode<TItem>> sources, TreeNode<TItem> target,
        TreeDropPosition position)
    {
        // A row with no value can't be named to the app as the place a move goes.
        if (sources.Count == 0 || !sources.All(CanDragRow) || !HasValue(target))
        {
            return new(false);
        }

        // Beside or inside one of its own descendants, an item would move into itself.
        if (sources.Any(source => source == target || target.Ancestors().Contains(source)))
        {
            return MoveCheck.Refuse(Localizer[TbStrings.TreeViewCantMoveIntoItself]);
        }

        return CanDrop is null ? MoveCheck.Allow
            : await CanDrop(new([.. sources.Select(source => source.Item)], target.Item, position));
    }

    private Task<MoveCheck> OnDragOverAsync(string[] ids, string targetId, TreeDropPosition position) =>
        _byId.TryGetValue(targetId, out var target) ? CheckDropAsync(NodesOf(ids), target, position)
            : Task.FromResult(new MoveCheck(false));

    // A drop arrives as an event on the tree rather than a script call, so a bUnit test can raise it too.
    private async Task OnDropAsync(TreeDropEventArgs args)
    {
        if (_byId.TryGetValue(args.Target, out var target))
        {
            await DropAsync(NodesOf(args.Rows), target, args.Position, keepFocus: false);
        }
    }

    // A keyboard move goes before or after the row's neighbor, into the row above as its last child, or after its
    // parent.
    private async Task MoveByKeyAsync(TreeNode<TItem> node, string key)
    {
        var siblings = Siblings(node).Where(sibling => Filter is null || _keep.Contains(sibling.Key)).ToList();
        var index = siblings.IndexOf(node);
        var (target, position) = key switch
        {
            "MoveUp" => (index > 0 ? siblings[index - 1] : null, TreeDropPosition.Before),
            "MoveDown" => (index < siblings.Count - 1 ? siblings[index + 1] : null, TreeDropPosition.After),
            "Indent" => (index > 0 ? siblings[index - 1] : null, TreeDropPosition.Inside),
            _ => (node.Parent, TreeDropPosition.After),
        };
        if (target is not null)
        {
            await DropAsync([node], target, position, keepFocus: true);
        }
    }

    // The app moves the items; the tree opens a target they went into and says what moved, or why it didn't.
    private async Task DropAsync(IReadOnlyList<TreeNode<TItem>> sources, TreeNode<TItem> target,
        TreeDropPosition position, bool keepFocus)
    {
        var check = await CheckDropAsync(sources, target, position);
        if (!check.Allowed)
        {
            if (check.Reason is { } reason)
            {
                Announcer.Polite(reason);
            }

            return;
        }

        var moved = sources.Count == 1 ? TextOf(sources[0])
            : string.Format(CultureInfo.CurrentCulture, Localizer.Plural(TbStrings.TreeViewItemCount, sources.Count),
                sources.Count);
        var said = position switch
        {
            TreeDropPosition.Before => TbStrings.TreeViewMovedBefore,
            TreeDropPosition.After => TbStrings.TreeViewMovedAfter,
            _ => TbStrings.TreeViewMovedInside,
        };
        Telemetry.Used(TelemetryName, "move");
        await OnDrop.InvokeAsync(new([.. sources.Select(source => source.Item)], target.Item, position));
        // The app applies a move to its items, often in place, so the rows are built again from them.
        _rowVersion++;
        Rebuild();
        StateHasChanged();
        Announcer.Polite(string.Format(CultureInfo.CurrentCulture, Localizer[said], moved, TextOf(target)));
        if (position == TreeDropPosition.Inside)
        {
            await ExpandAsync((TKey)target.Key);
        }

        // Blazor moves the row's element to its new place, which takes focus off it, so it gets focus back.
        if (keepFocus)
        {
            await ScrollToAsync((TKey)sources[0].Key, focus: true);
        }
    }

    // A parent row that can't be selected opens or closes on a click, as a sidebar's heading does. A double-click's
    // second click and its dblclick leave it alone, so the row toggles once.
    private bool TogglesOnClick(TreeNode<TItem> node) => node.HasChildren && !CanSelect(node) && !IsItemDisabled(node);

    // A row's handlers find its node as they run, since a rebuild, such as after a lazy load, makes new nodes.
    private Task OnNodeAsync(object key, Func<TreeNode<TItem>, Task> handle) =>
        _nodes.TryGetValue(key, out var node) ? handle(node) : Task.CompletedTask;

    private async Task OnRowClickAsync(TreeNode<TItem> node, MouseEventArgs args)
    {
        _focusId = node.Id;
        if (Disabled)
        {
            return;
        }

        if (TogglesOnClick(node))
        {
            if (args.Detail < 2)
            {
                await ToggleAsync(node);
            }
        }
        else if (Checkable && Choice is not null)
        {
            await ToggleCheckAsync(node);
        }
        else
        {
            await SelectAsync(node, args.ShiftKey, toggle: args.CtrlKey || args.MetaKey);
        }
    }

    private async Task OnRowDoubleClickAsync(TreeNode<TItem> node)
    {
        if (!Disabled)
        {
            await ActivateAsync(node);
            if (!TogglesOnClick(node))
            {
                await ToggleIfParentAsync(node);
            }
        }
    }

    private Task ToggleIfParentAsync(TreeNode<TItem> node) => node.HasChildren ? ToggleAsync(node) : Task.CompletedTask;

    private async Task ToggleAsync(TreeNode<TItem> node)
    {
        _focusId = node.Id;
        if (Disabled)
        {
            return;
        }

        CountOpening(node, open: !IsOpen(node));
        if (IsOpen(node))
        {
            await CloseAsync(node.Key);
        }
        else
        {
            await OpenAsync(node.Key);
        }
    }

    // ExpandAsync and CollapseAsync serve code too, so only the user's clicks and keys count, when they change a row.
    private void CountOpening(TreeNode<TItem> node, bool open)
    {
        if (node.HasChildren && open != IsOpen(node))
        {
            Telemetry.Used(TelemetryName, open ? "expand" : "collapse");
        }
    }

    private Task ActivateAsync(TreeNode<TItem> node)
    {
        if (!HasValue(node) || IsItemDisabled(node))
        {
            return Task.CompletedTask;
        }

        Telemetry.Used(TelemetryName, "activate");
        return OnActivate.InvokeAsync(node.Item);
    }

    private bool FollowsFocus => SelectionFollowsFocus && SelectionMode == SelectionMode.Single && !Disabled;

    private bool CanSelect(TreeNode<TItem> node) => SelectionMode != SelectionMode.None && HasValue(node)
        && IsSelectable?.Invoke(node.Item) != false && !IsItemDisabled(node);

    private bool IsItemDisabled(TreeNode<TItem> node) => _declared.GetValueOrDefault(node.Key)?.Disabled == true
        || (HasValue(node) && IsDisabled?.Invoke(node.Item) == true);

    // Without a KeySelector, OnParametersSet has checked that the items are keys.
    private object KeyOf(TItem item) => KeySelector is null ? item! : KeySelector(item);

    object ITreeItemHost<TItem>.KeyOf(TItem item) => KeyOf(item);

    void ITreeItemHost<TItem>.ItemChanged() => ItemChanged();

    // A declared item's row, or nothing while it is hidden in a closed row, filtered out or not yet built.
    RenderFragment? ITreeItemHost<TItem>.RowOf(TbTreeItem<TItem> item) => _rowIndex.TryGetValue(item.Key, out var index)
        ? builder => RenderRow(builder, _rows[index], index, null, FocusId())
        : null;

    // Opening leaves the rows around it as they are, as their own Expanded may say, but undoes a close made while
    // filtering.
    Task ITreeItemHost<TItem>.SetExpandedAsync(TbTreeItem<TItem> item, bool open)
    {
        if (!open)
        {
            return CloseAsync(item.Key, item);
        }

        if (_nodes.TryGetValue(item.Key, out var node) && Reveal([node]))
        {
            Flatten();
        }

        return SetExpandedAsync(_expanded.Append(item.Key), item);
    }

    bool ITreeItemHost<TItem>.IsOpen(TbTreeItem<TItem> item) =>
        _rowIndex.TryGetValue(item.Key, out var index) && IsOpen(_rows[index].Node);

    private async Task OnExtendAsync(string id, string from)
    {
        if (Disabled || !_byId.TryGetValue(id, out var node))
        {
            return;
        }

        _focusId = id;
        await ExtendAsync(node, _byId.GetValueOrDefault(from) ?? node);
        StateHasChanged();
    }

    // When the row last chosen is missing or hidden, the range starts at the row the keys moved from, as in a file
    // list.
    private Task ExtendAsync(TreeNode<TItem> target, TreeNode<TItem> from)
    {
        if ((_anchor is null || !_rowIndex.ContainsKey(_anchor)) && CanSelect(from))
        {
            _anchor = from.Key;
        }

        return SelectAsync(target, shift: true, toggle: false);
    }

    // Shift selects from the last row chosen without it, while that row is shown; Ctrl or Cmd adds or removes one row.
    private Task SelectAsync(TreeNode<TItem> node, bool shift, bool toggle)
    {
        if (!CanSelect(node))
        {
            return Task.CompletedTask;
        }

        if (SelectionMode == SelectionMode.Single)
        {
            _anchor = node.Key;
            return ChooseAsync(node.Key);
        }

        if (shift && _anchor is not null && _rowIndex.ContainsKey(_anchor))
        {
            var keys = _rows.Where(row => row.IsItem).Select(row => row.Node).ToList();
            var from = keys.FindIndex(other => Equals(other.Key, _anchor));
            var to = keys.FindIndex(other => Equals(other.Key, node.Key));
            var range = keys.Skip(Math.Min(from, to)).Take(Math.Abs(to - from) + 1).Where(CanSelect)
                .Select(other => other.Key);
            return SetSelectedAsync(toggle ? _selected.Union(range) : range);
        }

        _anchor = node.Key;
        return SetSelectedAsync(!toggle ? [node.Key]
            : _selected.Contains(node.Key) ? _selected.Where(other => !Equals(other, node.Key))
            : _selected.Append(node.Key));
    }

    private async Task ToggleCheckAsync(TreeNode<TItem> node)
    {
        if (Disabled || !CanCheck(node) || IsItemDisabled(node))
        {
            return;
        }

        Telemetry.Used(TelemetryName, "check");
        // A cascade sets the rows it can reach, unchecking them once all the rows with nothing inside are checked, as a
        // tree select's value from outside carries no heading's key.
        var reached = CascadeChecks ? EnabledDescendants(node).ToList() : [];
        var check = reached.Count == 0 ? !_checked.Contains(node.Key)
            : !reached.Where(each => each.Children.Count == 0).All(each => _checked.Contains(each.Key));
        var keys = new HashSet<object>(_checked);
        foreach (var each in reached.Prepend(node))
        {
            if (check)
            {
                keys.Add(each.Key);
            }
            else
            {
                keys.Remove(each.Key);
            }
        }

        if (CascadeChecks)
        {
            KeepParentsInStep(keys);
        }

        _checked = keys;
        Flatten();
        await CheckedKeysChanged.InvokeAsync(Reported(_checked));
    }

    // A parent's key follows its rows, from the deepest up, so the set lists exactly the rows that show as checked.
    private void KeepParentsInStep(HashSet<object> keys)
    {
        foreach (var root in _roots)
        {
            Visit(root);
        }

        bool Visit(TreeNode<TItem> node)
        {
            if (node.Children.Count == 0)
            {
                return keys.Contains(node.Key);
            }

            // Every child is visited, so parents deeper down are put in step too.
            var all = node.Children.Select(Visit).ToList().All(isChecked => isChecked);
            if (all)
            {
                keys.Add(node.Key);
            }
            else
            {
                keys.Remove(node.Key);
            }

            return all;
        }
    }

    // A cascade passes over turned-off rows, which can't be checked.
    private IEnumerable<TreeNode<TItem>> EnabledDescendants(TreeNode<TItem> node) =>
        node.Descendants().Where(descendant => !IsItemDisabled(descendant));

    private async Task SetExpandedAsync(IEnumerable<object> keys, TbTreeItem<TItem>? asking = null)
    {
        var expanded = keys.ToHashSet();
        if (expanded.SetEquals(_expanded))
        {
            return;
        }

        // Closing a row that is loading stops the load.
        foreach (var closed in _expanded.Except(expanded).Where(_loads.ContainsKey).ToList())
        {
            await _loads[closed].CancelAsync();
        }

        if (!_expanded.IsSubsetOf(expanded))
        {
            _expandAllDepth = null;
        }

        var before = _expanded;
        _expanded = expanded;
        Flatten();
        // Called from code as well as from events, so the tree renders the change itself.
        StateHasChanged();
        await ExpandedKeysChanged.InvokeAsync(Reported(_expanded));
        await ReportExpansionAsync(before, asking);
    }

    // Declared rows that opened or closed raise ExpandedChanged, all but the one whose parent asked for it.
    private async Task ReportExpansionAsync(HashSet<object> before, TbTreeItem<TItem>? asking)
    {
        foreach (var item in _declared.Values.Where(item => item != asking).ToList())
        {
            if (before.Contains(item.Key) != _expanded.Contains(item.Key))
            {
                await item.ReportExpandedAsync(_expanded.Contains(item.Key));
            }
        }
    }

    private async Task ChooseAsync(object key)
    {
        await SetSelectedAsync([key]);
        if (Choice is not null)
        {
            await Choice.ChosenAsync(key);
        }
    }

    private async Task SetSelectedAsync(IEnumerable<object> keys, bool byUser = true)
    {
        var selected = keys.ToHashSet();
        if (selected.SetEquals(_selected))
        {
            return;
        }

        // Only a selection the user changed counts, not one SelectAsync makes.
        if (byUser)
        {
            Telemetry.Used(TelemetryName, "select");
        }

        _selected = selected;
        await SelectedKeysChanged.InvokeAsync(Reported(_selected));
    }

    private async Task LoadIfNeededAsync(TreeNode<TItem> node)
    {
        if (LoadChildren is not { } load || !node.MayHaveChildren || _loads.ContainsKey(node.Key)
            || !_expanded.Contains(node.Key))
        {
            return;
        }

        using var cancel = new CancellationTokenSource();
        _loads[node.Key] = cancel;
        _failed.Remove(node.Key);
        Flatten();
        StateHasChanged();
        try
        {
            _loaded[node.Key] = [.. await load(node.Item, cancel.Token)];
        }
        catch (Exception) when (cancel.IsCancellationRequested)
        {
            // The row was closed while its children loaded, so they load again when it opens; a loader may report
            // the cancel with its own exception.
        }
        catch (Exception exception)
        {
            _failed.Add(node.Key);
            Announcer.Polite(LoadFailedLabel);
            await OnLoadFailed.InvokeAsync(exception);
        }
        finally
        {
            _loads.Remove(node.Key);
        }

        if (_disposed)
        {
            return;
        }

        // A row opened again before its canceled load ended starts a new one now.
        if (cancel.IsCancellationRequested && _expanded.Contains(node.Key) && !_loaded.ContainsKey(node.Key))
        {
            await LoadIfNeededAsync(node);
            return;
        }

        Build();
        // A row checked before its children loaded checks them too, as a cascade would have.
        if (Checkable && CascadeChecks && _checked.Contains(node.Key) && _nodes.TryGetValue(node.Key, out var loaded))
        {
            _checked.UnionWith(EnabledDescendants(loaded).Select(descendant => descendant.Key));
            KeepParentsInStep(_checked);
            await CheckedKeysChanged.InvokeAsync(Reported(_checked));
        }

        Flatten();
        StateHasChanged();
    }

    private Task RetryAsync(TreeNode<TItem> node)
    {
        _failed.Remove(node.Key);
        return LoadIfNeededAsync(node);
    }

    private void RenderItemRow(RenderTreeBuilder builder, TreeRow<TItem> row, int index, int? place, string? focusId)
    {
        var node = row.Node;
        builder.OpenComponent<TreeItemRow>(0);
        builder.SetKey(node.Key);
        // Only the insert slots show a row's index, so without them a row that moves down needn't draw again.
        builder.AddComponentParameter(1, nameof(TreeItemRow.State), new TreeRowState(_rowVersion,
            InsertTemplate is null ? 0 : index, place,
            Virtualize ? _inside[index] : null, node.Id == focusId, row.SetSize, row.Position, ContextOf(node)));
        builder.AddComponentParameter(2, nameof(TreeItemRow.Content),
            (RenderFragment)(rowBuilder => RenderRow(rowBuilder, row, index, place, focusId)));
        builder.CloseComponent();
    }

    private TreeItemContext<TItem, TKey> ContextOf(TreeNode<TItem> node) => new(node.Item,
        HasValue(node) ? (TKey)node.Key : default!, node.Level,
        IsOpen(node), _selected.Contains(node.Key), Checkable ? _checkStates.GetValueOrDefault(node.Key) : false,
        node.HasChildren, _loads.ContainsKey(node.Key), _matches.Contains(node.Key));

    private string TextOf(TreeNode<TItem> node) =>
        (HasValue(node) ? ItemText?.Invoke(node.Item) : null) ?? _declared.GetValueOrDefault(node.Key)?.Text
        ?? (HasValue(node) ? node.Item?.ToString() : null) ?? "";

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _scrollAfterRender?.Done.TrySetResult();
        foreach (var load in _loads.Values)
        {
            await load.CancelAsync();
        }

        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }

    private string LoadingLabel => Labels.Loading ?? Localizer[TbStrings.TreeViewLoading];
    private string LoadFailedLabel => Labels.LoadFailed ?? Localizer[TbStrings.TreeViewLoadFailed];
    private string RetryLabel => Labels.Retry ?? Localizer[TbStrings.TreeViewRetry];
    private string EmptyLabel => Labels.Empty ?? Localizer[TbStrings.TreeViewEmpty];
}

internal enum TreeRowKind
{
    Item,
    Loading,
    Failed,
}

// A row the tree shows: an item, or in place of an item's children, a loading or failed row.
internal readonly record struct TreeRow<TItem>(TreeNode<TItem> Node, int SetSize, int Position, TreeRowKind Kind)
{
    public bool IsItem => Kind == TreeRowKind.Item;
}
