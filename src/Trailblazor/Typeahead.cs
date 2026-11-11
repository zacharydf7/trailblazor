namespace Trailblazor;

// Type-ahead for a list whose match may not be in the page, finding what typeahead.js finds among the items that are:
// the next item starting with the typed text, with a repeated letter stepping through the matches.
internal static class Typeahead
{
    // The place of the match, counting from 0, or -1. From is the place the search moves on from, or -1.
    public static int Find(int count, Func<int, string> text, Func<int, bool> skip, int from, string typed)
    {
        int FirstFrom(int start, string prefix) => Enumerable.Range(0, count).Select(step => (start + step) % count)
            .FirstOrDefault(index => !skip(index)
                && text(index).Trim().StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase), -1);

        var match = FirstFrom(typed.Length == 1 ? from + 1 : Math.Max(from, 0), typed);
        // A real double letter, as in "Aaron", takes priority over stepping, unless the search is already on it.
        return typed.All(letter => letter == typed[0]) && (match == -1 || match == from)
            ? FirstFrom(from + 1, typed[..1]) : match;
    }
}
