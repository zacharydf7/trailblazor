namespace Trailblazor;

// A provider groups the rows itself, a level at a time: the grid asks for the top-level groups, then for an open
// group's inner groups or, innermost, its rows. Its groups start closed, since their rows aren't loaded until they
// open.
public partial class TbDataGrid<TItem>
{
    private List<GridGroup> _topGroups = [];
    private readonly Dictionary<GroupKey, GroupContents> _groupContents = [];
    private readonly HashSet<GroupKey> _openGroups = [];
    private readonly Dictionary<GroupKey, CancellationTokenSource> _groupLoads = [];

    private sealed record GroupContents(List<GridGroup> Groups, List<TItem> Rows);

    private bool ProvidesGroups => DataProvider is not null && AppliedGroups.Any();

    private GridRequest GroupRequest(GroupKey? key) => new(0, null, [.. _sorts])
    {
        Filters = [.. _filters],
        Aggregates = Aggregates,
        Groups = [.. AppliedGroups.Select(column => column.Key)],
        Group = PathOf(key),
    };

    private static List<object?> PathOf(GroupKey? key)
    {
        List<object?> path = [];
        for (var at = key; at is not null; at = at.Outer)
        {
            path.Insert(0, at.Value);
        }

        return path;
    }

    private void ShowProviderGroups()
    {
        var groups = AppliedGroups.ToList();
        List<GridLine> lines = [];
        void Add(List<GridGroup> level, int depth, GroupKey? outer)
        {
            var column = groups[depth];
            foreach (var group in level)
            {
                var key = new GroupKey(outer, column.Key, group.Value);
                var open = _openGroups.Contains(key);
                var heading = new GroupHeading(key, depth + 1, column.Heading, column.TextOf(group.Value), group.Count,
                    open, _groupLoads.ContainsKey(key));
                lines.Add(new(default!, lines.Count, heading));
                if (!open || !_groupContents.TryGetValue(key, out var contents))
                {
                    continue;
                }

                if (depth + 1 < groups.Count)
                {
                    Add(contents.Groups, depth + 1, key);
                }
                else
                {
                    lines.AddRange(contents.Rows.Select(row => new GridLine(row, lines.Count)));
                }

                if (GroupTotals && HasTotals)
                {
                    lines.Add(new(default!, lines.Count, Totals: new(heading, new(group.Totals))));
                }
            }
        }

        if (groups.Count > 0)
        {
            Add(_topGroups, 0, null);
        }

        _matched = _topGroups.Sum(group => group.Count);
        _total = lines.Count;
        _page = Math.Min(_page, PageCount);
        ShowLines(_pageSize is { } size ? [.. lines.Skip((_page - 1) * size).Take(size)] : lines);
    }

    // The top-level groups load again as the sorts, filters or groups change, and the groups that were open load again
    // after them, outer ones first, so each inner one finds its outer group's list.
    private async Task LoadGroupsAsync(bool force)
    {
        var request = GroupRequest(null);
        _aggregatesAsked = AggregatesKey;
        var asked = $"groups|{string.Join(",", request.Groups)}|{string.Join(",", _sorts)}|{FiltersKey}|"
            + AggregatesKey;
        if (!force && asked == _loaded)
        {
            ShowProviderGroups();
            return;
        }

        _loaded = asked;
        _load?.Cancel();
        var load = _load = new CancellationTokenSource();
        _loading = true;
        _failed = false;
        GridResult<TItem>? result = null;
        var span = Telemetry.StartSpan(TelemetryName, "load");
        try
        {
            result = await DataProvider!(request, load.Token);
            span.Finish(load.IsCancellationRequested ? "canceled" : "succeeded", result.Groups.Count);
        }
        catch (Exception) when (load.IsCancellationRequested)
        {
            span.Finish("canceled");
            return;
        }
        catch (Exception exception)
        {
            span.Finish("failed", failure: exception);
            ForgetGroupContents();
            _topGroups = [];
            await ShowLoadFailedAsync(exception);
        }
        finally
        {
            if (_load == load)
            {
                _loading = false;
            }
        }

        if (result is null || load.IsCancellationRequested)
        {
            return;
        }

        ForgetGroupContents();
        _topGroups = [.. result.Groups];
        _totals = new(result.Totals);
        // The groups are counted as they show, not by a provider's count of rows.
        _uncounted = false;
        var page = _page;
        ShowProviderGroups();
        if (_page != page)
        {
            await PageChanged.InvokeAsync(_page);
            await StateChangedAsync();
        }

        if (_total == 0)
        {
            Announcer.Polite(EmptyLabel);
        }

        foreach (var key in _openGroups.OrderBy(DepthOf).ToList())
        {
            await LoadGroupAsync(key);
        }
    }

    private static int DepthOf(GroupKey key) => key.Outer is null ? 1 : DepthOf(key.Outer) + 1;

    // A group's contents load when it opens. Closing it cancels them, and a failed load closes it again, so opening it
    // tries again. A group the rows no longer hold, as after a filter, closes.
    private async Task LoadGroupAsync(GroupKey key)
    {
        if (_disposed || _groupContents.ContainsKey(key) || _groupLoads.ContainsKey(key)
            || !_openGroups.Contains(key))
        {
            return;
        }

        var listed = key.Outer is null ? _topGroups
            : _groupContents.TryGetValue(key.Outer, out var outer) ? outer.Groups : null;
        if (listed?.Exists(group => Equals(group.Value, key.Value)) != true)
        {
            _openGroups.Remove(key);
            return;
        }

        using var cancel = new CancellationTokenSource();
        _groupLoads[key] = cancel;
        ShowProviderGroups();
        StateHasChanged();
        var span = Telemetry.StartSpan(TelemetryName, "load");
        try
        {
            var result = await DataProvider!(GroupRequest(key), cancel.Token);
            span.Finish(cancel.IsCancellationRequested ? "canceled" : "succeeded",
                result.Groups.Count + result.Items.Count);
            if (!cancel.IsCancellationRequested)
            {
                _groupContents[key] = new([.. result.Groups], [.. result.Items]);
            }
        }
        catch (Exception) when (cancel.IsCancellationRequested)
        {
            span.Finish("canceled");
        }
        catch (Exception exception)
        {
            span.Finish("failed", failure: exception);
            _openGroups.Remove(key);
            Announcer.Polite(LoadFailedLabel);
            Telemetry.Failed(TelemetryName, AriaLabel, "load", exception);
            await OnLoadFailed.InvokeAsync(exception);
        }
        finally
        {
            // A canceled load was let go when it was canceled, and a new one may have taken its place.
            if (_groupLoads.TryGetValue(key, out var current) && current == cancel)
            {
                _groupLoads.Remove(key);
            }
        }

        // Whatever canceled the load has drawn the groups since.
        if (!_disposed && !cancel.IsCancellationRequested)
        {
            ShowProviderGroups();
            StateHasChanged();
        }
    }

    private async Task ToggleGroupAsync(GroupKey key)
    {
        if (!ProvidesGroups)
        {
            ToggleGroup(key);
            return;
        }

        var open = _openGroups.Add(key);
        Telemetry.Used(TelemetryName, open ? "expand" : "collapse");
        if (!open)
        {
            _openGroups.Remove(key);
            if (_groupLoads.Remove(key, out var loading))
            {
                loading.Cancel();
            }
        }

        ShowProviderGroups();
        if (open)
        {
            await LoadGroupAsync(key);
        }
    }

    private void ForgetGroupContents()
    {
        foreach (var loading in _groupLoads.Values)
        {
            loading.Cancel();
        }

        _groupLoads.Clear();
        _groupContents.Clear();
    }
}
