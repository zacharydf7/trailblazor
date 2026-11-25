namespace Trailblazor;

// The sortable lists on a page that share a group, so an item can move from one to another. Each has a ListId, which
// TbSortable requires in a group. Lists are offered in their handles' menus in the order a board gives with Order, such
// as a Kanban's columns, or else in the order they first joined, and the others redraw when one joins or leaves, so
// their menus stay current.
internal sealed class SortableGroups
{
    private readonly Dictionary<string, List<ISortableGroupMember>> _groups = [];
    private readonly Dictionary<string, List<string>> _places = [];

    // A list that joins later, such as a Kanban column that opens, then takes its place among the others.
    public void Order(string group, IEnumerable<string> listIds) => _places[group] = [.. listIds];

    public void Join(string group, ISortableGroupMember list)
    {
        if (!_groups.TryGetValue(group, out var lists))
        {
            _groups[group] = lists = [];
        }

        if (lists.Contains(list))
        {
            return;
        }

        if (!_places.TryGetValue(group, out var places))
        {
            _places[group] = places = [];
        }

        if (!places.Contains(list.ListId!))
        {
            places.Add(list.ListId!);
        }

        var place = Place(places, list);
        var before = lists.FindIndex(other => Place(places, other) > place);
        lists.Insert(before < 0 ? lists.Count : before, list);
        Changed(lists, list);
    }

    private static int Place(List<string> places, ISortableGroupMember list) => places.IndexOf(list.ListId!);

    public void Leave(string group, ISortableGroupMember list)
    {
        if (!_groups.TryGetValue(group, out var lists) || !lists.Remove(list))
        {
            return;
        }

        if (lists.Count == 0)
        {
            _groups.Remove(group);
            _places.Remove(group);
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
