namespace Trailblazor.Docs.Examples;

// Parameters lists the API members whose names match, so a search for a parameter says where it was found.
public sealed record SearchResult(string Title, string Path, string Summary, IReadOnlyList<string> Parameters);

// Searches the guides and components by title, summary, category and API member names.
public static class DocsSearch
{
    private static readonly Lazy<IReadOnlyList<Entry>> Entries = new(BuildEntries);

    // Every word must appear somewhere in an entry; a match in the title ranks it higher.
    public static IReadOnlyList<SearchResult> Find(string? query)
    {
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            return [];
        }

        return [.. Entries.Value
            .Where(entry => words.All(word => entry.Text.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => words.Count(word =>
                entry.Title.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => new SearchResult(entry.Title, entry.Path, entry.Summary, [.. entry.Members
                .Where(member => words.Any(word => member.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .Distinct()]))];
    }

    private static IReadOnlyList<Entry> BuildEntries() =>
    [
        .. GuideCatalog.All.Select(guide => new Entry(guide.Title, guide.Path, guide.Summary, "Guides", [])),
        .. ExampleCatalog.All.Select(example => new Entry(example.Title, $"components/{example.Name}",
            example.Summary, example.Category.ToString(), [.. example.Api
                .SelectMany(type => ApiDocs.Describe(type).Members).Select(member => member.Name)])),
    ];

    private sealed record Entry(string Title, string Path, string Summary, string Category,
        IReadOnlyList<string> Members)
    {
        public string Text { get; } = string.Join(' ', [Title, Summary, Category, .. Members]);
    }
}
