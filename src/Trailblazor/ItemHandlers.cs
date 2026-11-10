using System.Runtime.CompilerServices;

namespace Trailblazor;

// Keeps one event handler per item across renders. Blazor gives an element a new handler id whenever its handler is a
// new delegate, and a lambda in a loop is a new delegate on every render, so an element a test found earlier stops
// working. A class is matched by reference, since an item whose Equals reads only its id can still change and its
// handler must use the new instance. Strings are matched by Equals, and so is a class passed with byValue, for a key
// built again on each render.
internal sealed class ItemHandlers<TItem, THandler>(Func<TItem, THandler> create, bool byValue = false)
{
    private readonly Dictionary<Key, THandler> _handlers = [];

    public THandler For(TItem item)
    {
        var key = new Key(item, byValue);
        if (!_handlers.TryGetValue(key, out var handler))
        {
            handler = create(item);
            _handlers[key] = handler;
        }

        return handler;
    }

    // Drops the handlers of items not among those the component holds, matching them as For does.
    public void KeepOnly(IEnumerable<TItem> held)
    {
        var kept = held.Select(item => new Key(item, byValue)).ToHashSet();
        KeepOnly(item => kept.Contains(new Key(item, byValue)));
    }

    // Drops the handlers of items the component no longer holds, checking only the items it has handlers for.
    public void KeepOnly(Func<TItem, bool> held)
    {
        foreach (var gone in _handlers.Keys.Where(key => !held(key.Item)).ToList())
        {
            _handlers.Remove(gone);
        }
    }

    private readonly struct Key(TItem item, bool byValue) : IEquatable<Key>
    {
        public TItem Item => item;

        private bool ByReference => !byValue && !typeof(TItem).IsValueType && typeof(TItem) != typeof(string);

        public bool Equals(Key other) => ByReference
            ? ReferenceEquals(item, other.Item) : EqualityComparer<TItem>.Default.Equals(item, other.Item);

        public override bool Equals(object? other) => other is Key key && Equals(key);

        public override int GetHashCode() => item is null ? 0
            : ByReference ? RuntimeHelpers.GetHashCode(item) : EqualityComparer<TItem>.Default.GetHashCode(item);
    }
}
