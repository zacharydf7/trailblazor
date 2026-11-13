namespace Trailblazor;

// The sortable lists on a page that share a group, so an item can move from one to another. Lists join in the order
// they first render, which is the order their handles' menus offer them in, and the others redraw when one joins or
// leaves, so their menus stay current.
internal sealed class SortableGroups
{
    private readonly Dictionary<string, List<ISortableGroupMember>> _groups = [];

    public void Join(string group, ISortableGroupMember list)
    {
        if (!_groups.TryGetValue(group, out var lists))
        {
            _groups[group] = lists = [];
        }

        if (!lists.Contains(list))
        {
            lists.Add(list);
            Changed(lists, list);
        }
    }

    public void Leave(string group, ISortableGroupMember list)
    {
        if (!_groups.TryGetValue(group, out var lists) || !lists.Remove(list))
        {
            return;
        }

        if (lists.Count == 0)
        {
            _groups.Remove(group);
        }

        Changed(lists, list);
    }

    // The lists of the group whose items are of this type; a list of another type can't take them.
    public IEnumerable<T> Lists<T>(string group) => _groups.TryGetValue(group, out var lists) ? lists.OfType<T>() : [];

    private static void Changed(List<ISortableGroupMember> lists, ISortableGroupMember except)
    {
        foreach (var other in lists.Where(other => other != except).ToList())
        {
            other.GroupChanged();
        }
    }
}

internal interface ISortableGroupMember
{
    string? ListId { get; }

    void GroupChanged();
}
