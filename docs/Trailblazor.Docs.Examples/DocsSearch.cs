namespace Trailblazor.Docs.Examples;

// Where a search matched, best first: the page's title, a section's heading, an API member's name, or the text.
public enum SearchMatch
{
    Title,
    Heading,
    Member,
    Text,
}

// One place a search matched. Section names the section or member, or is null for the page itself; Path goes to
// that spot, and Snippet is the text around the match.
public sealed record SearchResult(string Page, string PagePath, string? Section, string Path, string Snippet,
    SearchMatch Match);

// Searches every page's text: titles and summaries, section headings and text, and API members.
public static class DocsSearch
{
    private const int SnippetLength = 140;

    private static readonly Lazy<IReadOnlyList<Passage>> Passages = new(BuildPassages);

    // Every word must appear in the same passage, or in the title of its page, as in "slider step", and at least one
    // in the passage. Results come best match first, then in the order the site lists pages.
    public static IReadOnlyList<SearchResult> Find(string? query)
    {
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            return [];
        }

        return [.. Passages.Value
            .Where(passage => words.All(word => InPassage(passage, word) || Contains(passage.Page, word))
                && words.Any(word => InPassage(passage, word)))
            .Select(passage => (Passage: passage, Match: MatchOf(passage, words)))
            .OrderBy(found => found.Match)
            .Select(found => new SearchResult(found.Passage.Page, found.Passage.PagePath, found.Passage.Section,
                found.Passage.Path, Snippet(found.Passage.Text, words), found.Match))];
    }

    // When every word is in the passage's heading or the page's title, and at least one is in the heading, the passage
    // ranks as its kind; otherwise it ranks as text.
    private static SearchMatch MatchOf(Passage passage, string[] words) =>
        words.All(word => Contains(passage.Heading, word) || Contains(passage.Page, word))
            && words.Any(word => Contains(passage.Heading, word)) ? passage.Kind : SearchMatch.Text;

    private static bool InPassage(Passage passage, string word) =>
        Contains(passage.Heading, word) || Contains(passage.Text, word);

    // A window of the text around the earliest match of any word, cut at spaces.
    private static string Snippet(string text, string[] words)
    {
        if (text.Length <= SnippetLength)
        {
            return text;
        }

        var found = words.Select(word => text.IndexOf(word, StringComparison.OrdinalIgnoreCase))
            .Where(index => index >= 0).DefaultIfEmpty(0).Min();
        var start = Math.Max(0, Math.Min(found - SnippetLength / 3, text.Length - SnippetLength));
        start = start == 0 ? 0 : text.IndexOf(' ', start) + 1;
        var end = Math.Min(text.Length, start + SnippetLength);
        end = end == text.Length ? end : Math.Max(text.LastIndexOf(' ', end), start + SnippetLength / 2);
        return $"{(start > 0 ? "…" : "")}{text[start..end]}{(end < text.Length ? "…" : "")}";
    }

    private static bool Contains(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);

    // A component's section with nothing to index, such as empty notes, would match only by a heading every
    // component's page shares, so it's left out.
    private static IReadOnlyList<Passage> BuildPassages() =>
    [
        .. GuideCatalog.All.SelectMany(GuidePassages),
        .. ExampleCatalog.All.SelectMany(ComponentPassages)
            .Where(passage => passage.Kind == SearchMatch.Title || passage.Text.Length > 0),
    ];

    private static IEnumerable<Passage> GuidePassages(Guide guide)
    {
        var text = GuideText.Of(guide);
        var page = new Page(guide.Title, guide.Path);
        yield return page.Passage(null, guide.Path, $"{guide.Title} Guides", $"{guide.Summary} {text.Intro}",
            SearchMatch.Title);
        foreach (var section in text.Sections)
        {
            yield return page.Passage(section.Title, $"{guide.Path}#{section.Id}", section.Title, section.Text,
                SearchMatch.Heading);
        }

        // The palettes guide builds a section for each palette in a loop, so its headings aren't in the page's source.
        if (guide.Path == "guides/palettes")
        {
            foreach (var palette in PaletteCatalog.All)
            {
                yield return page.Passage(palette.Title, $"{guide.Path}#palette-{palette.Name}", palette.Title,
                    Plain(palette.Summary), SearchMatch.Heading);
            }
        }

        foreach (var passage in MemberPassages(page, guide.Path, guide.Api))
        {
            yield return passage;
        }
    }

    private static IEnumerable<Passage> ComponentPassages(Example example)
    {
        var path = $"components/{example.Name}";
        var page = new Page(example.Title, path);
        yield return page.Passage(null, path, $"{example.Title} {example.Category}", Plain(example.Summary),
            SearchMatch.Title);
        yield return page.Passage("Usage", $"{path}#usage", "Usage",
            Plain([.. example.WhenToUse, .. example.WhenNotToUse]), SearchMatch.Heading);
        foreach (var section in example.Shown)
        {
            yield return page.Passage(section.Title, $"{path}#{section.Id}", section.Title,
                Plain(section.Description ?? ""), SearchMatch.Heading);
        }

        yield return page.Passage("Accessibility", $"{path}#accessibility", "Accessibility", Plain([
            example.Pattern?.Name ?? "",
            .. example.Keyboard.Select(row => $"{string.Join(' ', row.Keys)} {row.Action}"),
            example.RightToLeftKeys ?? "", .. example.LabelingNotes, .. example.Announcements, .. example.Accessibility,
            .. example.AppDuties, .. example.BeforeInteractive]), SearchMatch.Heading);
        yield return page.Passage("Notes and limitations", $"{path}#notes", "Notes and limitations",
            Plain([.. example.NotesShown.Select(note => note.Text)]), SearchMatch.Heading);
        yield return page.Passage("Telemetry", $"{path}#telemetry", "Telemetry", Plain(example.TelemetryNotes),
            SearchMatch.Heading);
        yield return page.Passage("Testing", $"{path}#testing", "Testing", Plain(example.TestingTip ?? ""),
            SearchMatch.Heading);
        foreach (var passage in MemberPassages(page, $"{path}/api", example.Api))
        {
            yield return passage;
        }
    }

    // A member name two of the page's types share is prefixed with its type, as in TbSelect<TValue>.Placeholder, so
    // their results can be told apart; matching still reads only the member's own name.
    private static IEnumerable<Passage> MemberPassages(Page page, string path, IReadOnlyList<Type> types)
    {
        var apis = types.Select(ApiDocs.Describe).ToList();
        var shared = apis.SelectMany(api => api.Members).CountBy(member => member.ShortName)
            .Where(name => name.Value > 1).Select(name => name.Key).ToHashSet();
        return apis.SelectMany(api => api.Members.Select(member =>
        {
            var name = shared.Contains(member.ShortName) ? $"{api.Name}.{member.ShortName}" : member.ShortName;
            return page.Passage(name, $"{path}#{member.Id}", member.ShortName, Plain(member.Description),
                SearchMatch.Member);
        }));
    }

    // Catalog text marks code with backticks and links as [text](path); search reads only the words.
    private static string Plain(IEnumerable<string> notes) => Plain(string.Join(' ', notes));

    private static string Plain(string text) => CatalogMarkup.Pattern().Replace(text, match =>
        match.Groups["link"].Success ? match.Groups["link"].Value : match.Groups["code"].Value);

    private sealed record Page(string Title, string Path)
    {
        public Passage Passage(string? section, string path, string heading, string text, SearchMatch kind) =>
            new(Title, Path, section, path, heading, text, kind);
    }

    // Heading is what a match must be in to rank as Kind.
    private sealed record Passage(string Page, string PagePath, string? Section, string Path, string Heading,
        string Text, SearchMatch Kind);
}
