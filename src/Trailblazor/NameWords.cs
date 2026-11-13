using System.Globalization;
using System.Text.RegularExpressions;

namespace Trailblazor;

// Splits a member's name into words for a data grid heading or a property grid label: "TrailName" reads "Trail name",
// "maxResults" reads "Max results", and an acronym stays whole, so "HTMLPage" reads "HTML page", while a one-letter
// word doesn't count as one, so "IsAValue" reads "Is a value".
internal static partial class NameWords
{
    public static string Of(string name) => string.Join(' ', WordStart().Split(name)
        .Where(word => word.Length > 0)
        .Select((word, index) => index == 0 ? char.ToUpper(word[0], CultureInfo.CurrentCulture) + word[1..]
            : word.Length > 1 && word.All(char.IsUpper) ? word : word.ToLower(CultureInfo.CurrentCulture)));

    // Where a new word starts: before a capital after a lowercase letter or digit, and before the last capital of a run
    // that a lowercase letter follows.
    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordStart();
}
