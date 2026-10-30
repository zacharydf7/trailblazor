using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Shows items as a tree that expands and collapses, with selection, checkboxes, a filter, children that
/// load when opened, and the full keyboard of a WAI-ARIA tree. Give it your own items, usually a
/// <see cref="KeySelector"/>, and either a <see cref="ChildrenSelector"/> or a <see cref="ParentKeySelector"/>; or
/// write its rows as nested <see cref="TbTreeItem{TItem}"/>s.</summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TbTreeView<TItem> : IAsyncDisposable
{
    private readonly List<TbTreeItem<TItem>> _declaredRoots = [];
    private readonly Dictionary<object, TbTreeItem<TItem>> _declared = [];
    private readonly Dictionary<object, TreeNode<TItem>> _nodes = [];
    private readonly Dictionary<string, TreeNode<TItem>> _byId = [];
    private readonly Dictionary<object, List<TItem>> _loaded = [];
    private readonly Dictionary<object, CancellationTokenSource> _loads = [];
    private readonly HashSet<object> _failed = [];
    private readonly Dictionary<object, bool?> _checkStates = [];
    private readonly Dictionary<object, ElementReference> _rowRefs = [];
    // Rows the filter opened that the user closed; they open again when the filter finds other rows.
    private readonly HashSet<object> _closedWhileFiltering = [];
    private List<TreeNode<TItem>> _roots = [];
    private List<TreeRow<TItem>> _rows = [];
    private HashSet<object> _keep = [];
    private HashSet<object> _matches = [];
    private HashSet<object> _matchAncestors = [];
    private HashSet<object> _expanded = [];
    private HashSet<object> _selected = [];
    private HashSet<object> _checked = [];
    private IReadOnlyCollection<object>? _expandedFromParent;
    private IReadOnlyCollection<object>? _selectedFromParent;
    private IReadOnlyCollection<object>? _checkedFromParent;
    private object? _anchor;
    private string? _focusId;
    private bool _initialExpandDone;
    private bool _itemsChanged;
    private bool _itemsRegistered;
    private (object Key, bool Focus, TaskCompletionSource Done)? _scrollAfterRender;
    private ElementReference _tree;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<TreeViewEvents>? _events;
    private bool _disposed;

    /// <summary>The roots, or with a <see cref="ParentKeySelector"/>, every item.</summary>
    [Parameter]
    public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>The rows as nested <see cref="TbTreeItem{TItem}"/>s, in place of <see cref="Items"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gives each item a key that stays the same across renders, such as its id. Expansion, selection,
    /// checks and focus follow keys, so they survive when <see cref="Items"/> is replaced with new instances.
    /// Defaults to the item itself, which suits items compared by value, such as strings and records.</summary>
    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Gives an item's children, for items that hold their own.</summary>
    [Parameter]
    public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

    /// <summary>Gives an item's parent key, or null for a root, for a flat list of every item. Children keep the
    /// list's order.</summary>
    [Parameter]
    public Func<TItem, object?>? ParentKeySelector { get; set; }

    /// <summary>Loads an item's children the first time it is opened. The token is canceled if the item is closed
    /// first.</summary>
    [Parameter]
    public Func<TItem, CancellationToken, Task<IEnumerable<TItem>>>? LoadChildrenAsync { get; set; }

    /// <summary>Whether an item has children to load, so its toggle shows before they do. Defaults to true for every
    /// item when <see cref="LoadChildrenAsync"/> is set.</summary>
    [Parameter]
    public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>The text of an item's row, and what typing a letter finds. Defaults to the item's own text.</summary>
    [Parameter]
    public Func<TItem, string>? ItemText { get; set; }

    /// <summary>An icon before an item's text.</summary>
    [Parameter]
    public Func<TItem, Icon?>? ItemIcon { get; set; }

    /// <summary>Draws a row's content instead of its icon and text.</summary>
    [Parameter]
    public RenderFragment<TreeItemContext<TItem>>? ItemTemplate { get; set; }

    /// <summary>Content at the end of a row, such as badges or action buttons. Clicks and keys in it never select,
    /// expand or activate the row.</summary>
    [Parameter]
    public RenderFragment<TreeItemContext<TItem>>? ItemEnd { get; set; }

    /// <summary>The keys of the open rows. Bind it with <c>@bind-ExpandedKeys</c> to open rows from code or to save
    /// them.</summary>
    [Parameter]
    public IReadOnlyCollection<object>? ExpandedKeys { get; set; }

    /// <summary>Raised when rows open or close.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<object>> ExpandedKeysChanged { get; set; }

    /// <summary>How many levels open on the first render, when <see cref="ExpandedKeys"/> isn't given. Defaults to
    /// 0.</summary>
    [Parameter]
    public int InitialExpandDepth { get; set; }

    /// <summary>How many rows can be selected. Defaults to <see cref="TreeSelectionMode.Single"/>.</summary>
    [Parameter]
    public TreeSelectionMode SelectionMode { get; set; } = TreeSelectionMode.Single;

    /// <summary>The keys of the selected rows. Bind it with <c>@bind-SelectedKeys</c>.</summary>
    [Parameter]
    public IReadOnlyCollection<object>? SelectedKeys { get; set; }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<object>> SelectedKeysChanged { get; set; }

    /// <summary>Raised when a row is opened with Enter or a double-click.</summary>
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
    public IReadOnlyCollection<object>? CheckedKeys { get; set; }

    /// <summary>Raised when checks change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<object>> CheckedKeysChanged { get; set; }

    /// <summary>Checking a row checks the rows inside it, and a parent shows whether all, some or none of them are
    /// checked. Defaults to true.</summary>
    [Parameter]
    public bool CascadeChecks { get; set; } = true;

    /// <summary>Shows only the rows that match and the rows they sit in, which open to show them.</summary>
    [Parameter]
    public Func<TItem, bool>? Filter { get; set; }

    /// <summary>Also shows every row inside a match.</summary>
    [Parameter]
    public bool FilterKeepsDescendants { get; set; }

    /// <summary>Shown instead of the rows when there are none.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Raised when <see cref="LoadChildrenAsync"/> fails; the row offers to try again.</summary>
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

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IBrowserService Browser { get; set; } = default!;

    // The row that takes the tree's one tab stop: the last focused, else the first selected, else the first.
    private string? FocusId()
    {
        var shown = _rows.Where(row => row.IsItem).Select(row => row.Node).ToList();
        return shown.Any(node => node.Id == _focusId) ? _focusId
            : (shown.FirstOrDefault(node => _selected.Contains(node.Key)) ?? shown.FirstOrDefault())?.Id;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ChildContent is not null && (Items.Any() || ChildrenSelector is not null || ParentKeySelector is not null
            || LoadChildrenAsync is not null))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem>)} takes {nameof(Items)} or " +
                $"{nameof(TbTreeItem<TItem>)}s, not both.");
        }

        if (ParentKeySelector is not null && (ChildrenSelector is not null || LoadChildrenAsync is not null))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem>)} takes a {nameof(ParentKeySelector)} " +
                $"or a {nameof(ChildrenSelector)} and {nameof(LoadChildrenAsync)}, not both.");
        }

        _expanded = TakeFromParent(ExpandedKeys, ref _expandedFromParent, _expanded);
        _selected = TakeFromParent(SelectedKeys, ref _selectedFromParent, _selected);
        _checked = TakeFromParent(CheckedKeys, ref _checkedFromParent, _checked);
        Refresh();
    }

    private void Refresh()
    {
        Build();
        // Items that arrive after the first render, as loaded data does, still open to the initial depth.
        if (!_initialExpandDone && _nodes.Count > 0)
        {
            _initialExpandDone = true;
            if (ExpandedKeys is null)
            {
                _expanded.UnionWith(_nodes.Values.Where(node => node.Level <= InitialExpandDepth)
                    .Where(node => node.Children.Count > 0).Select(node => node.Key));
            }
        }

        Flatten();
    }

    // Tree items register as the tree renders them, so the tree builds its rows from them after that render.
    internal void AddItem(TbTreeItem<TItem> item, TbTreeItem<TItem>? parent)
    {
        (parent?.Children ?? _declaredRoots).Add(item);
        ItemChanged();
    }

    internal void RemoveItem(TbTreeItem<TItem> item, TbTreeItem<TItem>? parent)
    {
        (parent?.Children ?? _declaredRoots).Remove(item);
        ItemChanged();
    }

    internal void ItemChanged()
    {
        _itemsChanged = true;
        StateHasChanged();
    }

    // Only a new set from the parent replaces the tree's own, so a parent rendering again doesn't undo the user.
    private static HashSet<object> TakeFromParent(IReadOnlyCollection<object>? fromParent,
        ref IReadOnlyCollection<object>? last, HashSet<object> current)
    {
        if (ReferenceEquals(fromParent, last))
        {
            return current;
        }

        last = fromParent;
        return fromParent is null ? current : [.. fromParent];
    }

    // Rows opened by a set from the parent, such as one saved earlier, load like rows the user opens.
    /// <inheritdoc />
    protected override Task OnParametersSetAsync() => Task.WhenAll(_nodes.Values
        .Where(node => node.MayHaveChildren && _expanded.Contains(node.Key) && !_failed.Contains(node.Key))
        .ToList().Select(LoadIfNeededAsync));

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender && RendererInfo.IsInteractive)
            {
                await AttachAsync();
            }

            if (RendererInfo.IsInteractive && (_itemsChanged || (firstRender && ChildContent is not null)))
            {
                BuildFromItems();
            }

            if (_scrollAfterRender is { } scroll)
            {
                _scrollAfterRender = null;
                try
                {
                    if (_rows.Any(shown => shown.IsItem && Equals(shown.Node.Key, scroll.Key))
                        && _rowRefs.TryGetValue(scroll.Key, out var row))
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

        _events = DotNetObjectReference.Create(new TreeViewEvents(OnFocusAsync, OnKeyAsync, OnItemOrderAsync));
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
        StateHasChanged();
    }

    private void BuildFromItems()
    {
        _itemsChanged = false;
        _itemsRegistered = true;
        Refresh();
        StateHasChanged();
    }

    // Items register in the order they first render, and a keyed item can move without telling the tree, so the
    // browser reports the order of their markup.
    private Task OnItemOrderAsync(string[] ids)
    {
        var place = ids.Select((id, index) => (id, index)).ToDictionary(pair => pair.id, pair => pair.index);
        SortByPlace(_declaredRoots);
        Refresh();
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
    /// children haven't loaded stay closed, so this never starts a load.</summary>
    public Task ExpandAllAsync(int? depth = null) => SetExpandedAsync(_expanded.Union(_nodes.Values
        .Where(node => node.Children.Count > 0 && node.Level <= (depth ?? int.MaxValue)).Select(node => node.Key)));

    /// <summary>Closes every row.</summary>
    public Task CollapseAllAsync() => SetExpandedAsync([]);

    /// <summary>Opens the row with this key, and the rows it sits in, loading its children if they haven't
    /// loaded.</summary>
    public async Task ExpandAsync(object key)
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
    public async Task CollapseAsync(object key)
    {
        if (_matchAncestors.Contains(key) && _closedWhileFiltering.Add(key))
        {
            Flatten();
            StateHasChanged();
        }

        await SetExpandedAsync(_expanded.Where(other => !Equals(other, key)));
    }

    /// <summary>Opens the rows the row with this key sits in, and scrolls it into view, focusing it if asked.
    /// </summary>
    public async Task ScrollToAsync(object key, bool focus = false)
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
        if (focus)
        {
            _focusId = node.Id;
        }

        // A newer scroll replaces one still waiting for its render, which then ends without scrolling.
        _scrollAfterRender?.Done.TrySetResult();
        var done = new TaskCompletionSource();
        _scrollAfterRender = (node.Key, focus, done);
        StateHasChanged();
        await done.Task;
    }

    // Opening a row from code opens the rows above it too, even ones the user closed while the filter was on.
    private bool Reveal(IEnumerable<TreeNode<TItem>> nodes)
    {
        var count = _closedWhileFiltering.Count;
        _closedWhileFiltering.ExceptWith(nodes.Select(node => node.Key));
        return _closedWhileFiltering.Count != count;
    }

    private void Build()
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

        // A parent that isn't in the list leaves the item at the root, rather than losing it.
        var items = Items.ToList();
        var keys = items.Select(KeyOf).ToHashSet();
        var childrenOf = items.Where(item => parentOf(item) is { } parent && keys.Contains(parent))
            .ToLookup(item => parentOf(item)!);
        _roots.AddRange(items.Where(item => parentOf(item) is not { } parent || !keys.Contains(parent))
            .Select(item => Place(item, null)));
        if (_nodes.Count < items.Count)
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem>)} has items whose parents lead back to " +
                $"themselves, such as {items.Select(KeyOf).First(key => !_nodes.ContainsKey(key))}.");
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
        var node = Add(item.Value, parent);
        _declared[node.Key] = item;
        node.Children.AddRange(item.Children.Select(child => AddDeclared(child, node)));
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
        else if (LoadChildrenAsync is not null)
        {
            node.MayHaveChildren = HasChildren?.Invoke(item) ?? true;
        }

        return node;
    }

    private TreeNode<TItem> Add(TItem item, TreeNode<TItem>? parent)
    {
        var key = KeyOf(item);
        var id = Convert.ToString(key, CultureInfo.InvariantCulture) ?? "";
        if (_nodes.ContainsKey(key) || _byId.ContainsKey(id))
        {
            throw new InvalidOperationException($"{nameof(TbTreeView<TItem>)} has two items with the key {id}; " +
                $"each item's {nameof(KeySelector)} must give a different key.");
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
        Walk(_roots);
        // A row that isn't shown has no element, so its reference is dropped before anything can scroll to it.
        var shownKeys = _rows.Where(row => row.IsItem).Select(row => row.Node.Key).ToHashSet();
        foreach (var hidden in _rowRefs.Keys.Where(key => !shownKeys.Contains(key)).ToList())
        {
            _rowRefs.Remove(hidden);
        }

        void Walk(List<TreeNode<TItem>> siblings)
        {
            var shown = Filter is null ? siblings : siblings.Where(node => _keep.Contains(node.Key)).ToList();
            for (var index = 0; index < shown.Count; index++)
            {
                var node = shown[index];
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

        foreach (var node in _nodes.Values.Where(node => filter(node.Item)))
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
            if (CascadeChecks && node.Children.Count > 0)
            {
                var states = node.Children.Select(StateOf).Distinct().ToList();
                state = states.Count == 1 ? states[0] : null;
            }

            _checkStates[node.Key] = state;
            return state;
        }
    }

    private Task OnFocusAsync(string id)
    {
        _focusId = id;
        return Task.CompletedTask;
    }

    private async Task OnKeyAsync(string id, string key, bool shift, bool ctrl)
    {
        if (!_byId.TryGetValue(id, out var node) || Disabled)
        {
            return;
        }

        _focusId = id;
        switch (key)
        {
            case "ArrowRight":
                await ExpandAsync(node.Key);
                break;
            case "ArrowLeft":
                await CollapseAsync(node.Key);
                break;
            case "*":
                await SetExpandedAsync(_expanded.Union(Siblings(node).Where(sibling => sibling.HasChildren)
                    .Select(sibling => sibling.Key)));
                await Task.WhenAll(Siblings(node).Select(LoadIfNeededAsync));
                break;
            case "Enter":
                await SelectAsync(node, shift: false, toggle: false);
                await ActivateAsync(node);
                break;
            case " " when Checkable:
                await ToggleCheckAsync(node);
                break;
            case " ":
                await SelectAsync(node, shift, toggle: SelectionMode == TreeSelectionMode.Multiple);
                break;
            case "Extend":
                await SelectAsync(node, shift: true, toggle: false);
                break;
            case "SelectAll" when SelectionMode == TreeSelectionMode.Multiple:
                await SetSelectedAsync(_rows.Where(row => row.IsItem && CanSelect(row.Node))
                    .Select(row => row.Node.Key));
                break;
        }

        StateHasChanged();
    }

    private IEnumerable<TreeNode<TItem>> Siblings(TreeNode<TItem> node) => node.Parent?.Children ?? _roots;

    private async Task OnRowClickAsync(TreeNode<TItem> node, MouseEventArgs args)
    {
        _focusId = node.Id;
        if (!Disabled)
        {
            await SelectAsync(node, args.ShiftKey, toggle: args.CtrlKey || args.MetaKey);
        }
    }

    private async Task OnRowDoubleClickAsync(TreeNode<TItem> node)
    {
        if (!Disabled)
        {
            await ActivateAsync(node);
        }
    }

    private async Task ToggleAsync(TreeNode<TItem> node)
    {
        _focusId = node.Id;
        if (Disabled)
        {
            return;
        }

        if (IsOpen(node))
        {
            await CollapseAsync(node.Key);
        }
        else
        {
            await ExpandAsync(node.Key);
        }
    }

    private Task ActivateAsync(TreeNode<TItem> node) =>
        IsItemDisabled(node) ? Task.CompletedTask : OnActivate.InvokeAsync(node.Item);

    private bool CanSelect(TreeNode<TItem> node) => SelectionMode != TreeSelectionMode.None
        && IsSelectable?.Invoke(node.Item) != false && !IsItemDisabled(node);

    private bool IsItemDisabled(TreeNode<TItem> node) =>
        IsDisabled?.Invoke(node.Item) == true || _declared.GetValueOrDefault(node.Key)?.Disabled == true;

    internal object KeyOf(TItem item) => KeySelector is null ? item! : KeySelector(item);

    // Shift selects from the last row chosen without it; Ctrl or Cmd adds or removes one row.
    private Task SelectAsync(TreeNode<TItem> node, bool shift, bool toggle)
    {
        if (!CanSelect(node))
        {
            return Task.CompletedTask;
        }

        if (SelectionMode == TreeSelectionMode.Single)
        {
            _anchor = node.Key;
            return SetSelectedAsync([node.Key]);
        }

        if (shift && _anchor is not null && _nodes.ContainsKey(_anchor))
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
        if (Disabled || IsItemDisabled(node))
        {
            return;
        }

        // A cascade sets the rows it can reach, unchecking them once all of them are checked.
        var reached = CascadeChecks ? EnabledDescendants(node).ToList() : [];
        var check = reached.Count == 0 ? !_checked.Contains(node.Key)
            : !reached.All(each => _checked.Contains(each.Key));
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
        await CheckedKeysChanged.InvokeAsync(_checked.ToList());
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

    private async Task SetExpandedAsync(IEnumerable<object> keys)
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

        _expanded = expanded;
        Flatten();
        // Called from code as well as from events, so the tree renders the change itself.
        StateHasChanged();
        await ExpandedKeysChanged.InvokeAsync(_expanded.ToList());
    }

    private async Task SetSelectedAsync(IEnumerable<object> keys)
    {
        var selected = keys.ToHashSet();
        if (selected.SetEquals(_selected))
        {
            return;
        }

        _selected = selected;
        await SelectedKeysChanged.InvokeAsync(_selected.ToList());
    }

    private async Task LoadIfNeededAsync(TreeNode<TItem> node)
    {
        if (LoadChildrenAsync is not { } load || !node.MayHaveChildren || _loads.ContainsKey(node.Key)
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
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            // The row was closed while its children loaded, so they load again when it opens.
        }
        catch (Exception exception)
        {
            _failed.Add(node.Key);
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

        Build();
        // A row checked before its children loaded checks them too, as a cascade would have.
        if (Checkable && CascadeChecks && _checked.Contains(node.Key) && _nodes.TryGetValue(node.Key, out var loaded))
        {
            _checked.UnionWith(EnabledDescendants(loaded).Select(descendant => descendant.Key));
            KeepParentsInStep(_checked);
            await CheckedKeysChanged.InvokeAsync(_checked.ToList());
        }

        Flatten();
        StateHasChanged();
    }

    private Task RetryAsync(TreeNode<TItem> node)
    {
        _failed.Remove(node.Key);
        return LoadIfNeededAsync(node);
    }

    private TreeItemContext<TItem> ContextOf(TreeNode<TItem> node) => new(node.Item, node.Key, node.Level,
        IsOpen(node), _selected.Contains(node.Key), Checkable ? _checkStates.GetValueOrDefault(node.Key) : false,
        node.HasChildren, _loads.ContainsKey(node.Key), _matches.Contains(node.Key));

    private string TextOf(TreeNode<TItem> node) =>
        ItemText?.Invoke(node.Item) ?? _declared.GetValueOrDefault(node.Key)?.Text ?? node.Item?.ToString() ?? "";

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
