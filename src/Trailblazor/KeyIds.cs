using System.Globalization;

namespace Trailblazor;

// Gives each row's key a lasting id for the page: the key's own text when it has one, such as a number or a string,
// so tests can find a row by it, and a made-up one for any other key or one whose text another key already has.
internal sealed class KeyIds
{
    private readonly Dictionary<object, string> _ids = [];
    private readonly Dictionary<string, object> _keys = [];
    private int _made;

    public string IdOf(object key)
    {
        if (_ids.TryGetValue(key, out var id))
        {
            return id;
        }

        id = key is string or IFormattable ? Convert.ToString(key, CultureInfo.InvariantCulture) ?? "" : "";
        while (id.Length == 0 || _keys.ContainsKey(id))
        {
            id = $"key-{++_made}";
        }

        _ids[key] = id;
        _keys[id] = key;
        return id;
    }

    // Forgets the keys no longer shown, so a grid paging through a server's rows doesn't hold every key it has seen.
    public void Keep(IReadOnlySet<object> keys)
    {
        foreach (var gone in _ids.Keys.Where(key => !keys.Contains(key)).ToList())
        {
            _keys.Remove(_ids[gone]);
            _ids.Remove(gone);
        }
    }
}
