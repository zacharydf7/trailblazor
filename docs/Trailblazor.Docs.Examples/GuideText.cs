using System.Net;
using System.Text.RegularExpressions;

namespace Trailblazor.Docs.Examples;

// A section of a guide: the id and title of the heading that starts it, and the words under it.
public sealed record GuideSection(string Id, string Title, string Text);

// The words of a guide for search, read from its embedded page: the text before its first heading, and each section
// a DocsHeading starts. Markup, the @code block and Razor expressions are left out.
public sealed partial record GuideText(string Intro, IReadOnlyList<GuideSection> Sections)
{
    private static readonly Lazy<IReadOnlyDictionary<string, GuideText>> ByPath = new(Load);

    public static GuideText Of(Guide guide) => ByPath.Value[guide.Path];

    private static IReadOnlyDictionary<string, GuideText> Load()
    {
        var assembly = typeof(GuideText).Assembly;
        return assembly.GetManifestResourceNames().Where(name => name.StartsWith("Guides/", StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                return new StreamReader(stream).ReadToEnd();
            })
            .ToDictionary(source => Route().Match(source).Groups["path"].Value, Parse);
    }

    private static GuideText Parse(string source)
    {
        // A page's @code block comes last, so everything from it on is dropped; PageTitle goes too, since it repeats
        // the guide's title.
        var page = PageTitle().Replace(Comment().Replace(source.Split("\n@code", 2)[0], ""), "");
        var headings = Heading().Matches(page).Where(heading => !heading.Groups["id"].Value.StartsWith('@')).ToList();
        var ends = headings.Skip(1).Select(heading => heading.Index).Append(page.Length).ToList();
        return new(Words(page[..(headings.Count > 0 ? headings[0].Index : page.Length)]), [.. headings.Select(
            (heading, index) => new GuideSection(heading.Groups["id"].Value,
                WebUtility.HtmlDecode(heading.Groups["title"].Value),
                Words(page[(heading.Index + heading.Length)..ends[index]])))]);
    }

    // A tag becomes a space so cells and items stay apart, but the space is dropped before punctuation that follows
    // it. Razor writes a literal @ as @@.
    private static string Words(string markup)
    {
        var text = Razor().Replace(Tag().Replace(markup, " "), " ").Replace("@@", "@");
        return SpaceBeforePunctuation().Replace(Space().Replace(WebUtility.HtmlDecode(text), " ").Trim(), "$1");
    }

    [GeneratedRegex("@page \"/(?<path>[^\"]+)\"")]
    private static partial Regex Route();

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex("<DocsHeading\\s[^>]*?Id=\"(?<id>[^\"]+)\"\\s+Text=\"(?<title>[^\"]+)\"[^>]*>")]
    private static partial Regex Heading();

    // Quoted attribute values may hold > or =>, so they're skipped whole. The pages quote with ", and an apostrophe
    // can sit in an attribute's C#, as in Assertive("Couldn't reach the server").
    [GeneratedRegex("<(?:\"[^\"]*\"|[^>\"])*>")]
    private static partial Regex Tag();

    // A Razor line such as @foreach (...) or a brace on its own, or an inline @expression such as @palette.Title, but
    // not an escaped @@.
    [GeneratedRegex(@"^\s*(?:@\w.*|[{}])\s*$|(?<!@)@(?!@)\(?[\w.]+(?:\([^)]*\))?\)?", RegexOptions.Multiline)]
    private static partial Regex Razor();

    [GeneratedRegex("<PageTitle>.*?</PageTitle>", RegexOptions.Singleline)]
    private static partial Regex PageTitle();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Space();

    [GeneratedRegex(@" ([.,;:!?)])")]
    private static partial Regex SpaceBeforePunctuation();
}
