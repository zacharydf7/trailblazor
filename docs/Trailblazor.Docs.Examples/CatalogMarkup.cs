using System.Text.RegularExpressions;

namespace Trailblazor.Docs.Examples;

// The catalog's inline markup: text between backticks is code, and [text](path) links to another page, such as
// [text field](components/text-field).
public static partial class CatalogMarkup
{
    [GeneratedRegex(@"`(?<code>[^`]*)`|\[(?<link>[^\]]+)\]\((?<path>[^)\s]+)\)")]
    public static partial Regex Pattern();
}
