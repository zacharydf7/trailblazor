using Trailblazor.Docs.Examples;
using Trailblazor.Docs.Markdown;

namespace Trailblazor.Tests.Docs;

public class MarkdownTests
{
    // Rendering every page takes a few seconds, so the tests share one run.
    private static readonly Lazy<Task<IReadOnlyList<MarkdownPage>>> Pages = new(DocsMarkdown.RenderAsync);

    public static TheoryData<string> Names => new(ExampleCatalog.All.Select(example => example.Name));

    public static TheoryData<string> Guides => new(GuideCatalog.All.Select(guide => guide.Path));

    private static async Task<string> MarkdownOf(string path) =>
        (await Pages.Value).Single(page => page.Path == path).Markdown;

    [Theory, MemberData(nameof(Names))]
    public async Task ComponentMarkdown_ShouldIncludeEveryApiMember_WhenGenerated(string name)
    {
        var markdown = await MarkdownOf($"components/{name}/api");

        Assert.All(ExampleCatalog.Find(name)!.Api.Select(ApiDocs.Describe), api =>
        {
            Assert.Contains($"## `{api.Name}`", markdown);
            // A required member's name is marked as such in the same cell.
            Assert.All(api.Members, member => Assert.Contains($"| `{member.Name}`", markdown));
        });
    }

    [Theory, MemberData(nameof(Names))]
    public async Task ComponentMarkdown_ShouldIncludeEachSectionWithItsCodeButNoControls_WhenGenerated(string name)
    {
        var example = ExampleCatalog.Find(name)!;

        var markdown = await MarkdownOf($"components/{name}");

        Assert.StartsWith($"# {example.Title}\n\n{example.Summary}\n", markdown);
        Assert.All(example.Shown, section =>
        {
            Assert.Contains($"\n## {section.Title}\n", markdown);
            Assert.All(section.Sources,
                source => Assert.Contains(source.Text.ReplaceLineEndings("\n").Trim(), markdown));
        });
        // Each note's longest run of plain words, which the catalog's markup leaves as they are.
        Assert.All(example.NotesShown, note => Assert.Contains(note.Text.Split('`', '[', ']', '(', ')')
            .MaxBy(run => run.Length)!.Trim(), markdown));
        Assert.All(new[] { "## Accessibility", "## Telemetry", "## Design tokens", "## Testing" },
            heading => Assert.Contains($"\n{heading}\n", markdown));
        Assert.All(new[] { "Show code", "Copy code", "Open on its own", "View as Markdown", "Edit this page" },
            control => Assert.DoesNotContain(control, markdown));
    }

    [Fact]
    public async Task ComponentMarkdown_ShouldWriteNotesAndKeyboardTablesAsMarkdown_WhenGenerated()
    {
        var markdown = await MarkdownOf("components/select");

        Assert.Contains("\n- With `Filterable`, matching ignores case and accents, and groups with no matches hide.\n",
            markdown);
        Assert.Contains("\n| Key | Action |\n| --- | --- |\n| **While closed** |  |\n"
            + "| Down Up Enter Space | Opens the list. |", markdown);
        Assert.Contains("[radio group](https://zacharydf7.github.io/trailblazor/components/radio-group.md)", markdown);
        Assert.Contains("\n**SelectExample.razor**\n\n```razor\n<TbStack Gap=\"4\"", markdown);
    }

    [Theory, MemberData(nameof(Guides))]
    public async Task GuideMarkdown_ShouldIncludeEachSectionsHeading_WhenGenerated(string path)
    {
        var guide = GuideCatalog.All.Single(guide => guide.Path == path);

        var markdown = await MarkdownOf(path);

        Assert.StartsWith($"# {guide.Title}\n", markdown);
        Assert.All(GuideText.Of(guide).Sections, section => Assert.Contains($"\n## {section.Title}\n", markdown));
    }

    [Theory]
    [InlineData("guides/palettes", "Owner email")]
    [InlineData("guides/theming", "Brand color")]
    [InlineData("guides/telemetry", "| Shown | Used |")]
    public async Task GuideMarkdown_ShouldLeaveOutLiveDemos_WhenGenerated(string path, string demo)
    {
        var markdown = await MarkdownOf(path);

        Assert.DoesNotContain(demo, markdown);
    }

    [Fact]
    public async Task Index_ShouldLinkEveryPagesMarkdownWithWhatItCovers_WhenGenerated()
    {
        var pages = await Pages.Value;

        var index = DocsMarkdown.Index(pages);

        Assert.StartsWith("# Trailblazor\n\n> ", index);
        Assert.Equal(GuideCatalog.All.Count + 2 * ExampleCatalog.All.Count, pages.Count);
        Assert.All(pages, page =>
            Assert.Contains($"\n- [{page.Title}]({DocsMarkdown.Site}{page.Path}.md): {page.Summary}\n", index));
    }

    [Fact]
    public void Convert_ShouldKeepWhatReadsAndLeaveOutWhatOnlyWorksInTheBrowser_WhenGivenAPage()
    {
        const string html = """
            <div class="docs-article__body">
                <h1>Trail</h1>
                <details class="docs-article__folded">On this page</details>
                <div class="docs-heading"><h2 id="a">Parts</h2><a class="docs-heading__link" href="#a">#</a></div>
                <p>Read the <a href="guides/theming#tokens">theming guide</a>, or <code>TbTrail</code>.</p>
                <div class="docs-example"><button>Try it</button></div>
                <div class="tb-alert" role="note"><p>Watch out</p><p>It's steep.</p></div>
                <ol><li>One<ul><li>Inside</li></ul></li><li>Two</li></ol>
            </div>
            """;

        var markdown = HtmlMarkdown.Convert(html);

        Assert.Equal("""
            # Trail

            ## Parts

            Read the [theming guide](https://zacharydf7.github.io/trailblazor/guides/theming.md#tokens), or `TbTrail`.

            > Watch out
            >
            > It's steep.

            1. One
              - Inside
            2. Two

            """.ReplaceLineEndings("\n"), markdown);
    }
}
