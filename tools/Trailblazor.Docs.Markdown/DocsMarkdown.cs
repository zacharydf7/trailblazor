using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using Trailblazor.Docs.Examples;
using Trailblazor.Docs.Pages;

namespace Trailblazor.Docs.Markdown;

// A docs page as Markdown. Its file sits at the page's address with .md added, such as components/select.md.
public sealed record MarkdownPage(string Path, string Title, string Summary, string Markdown);

// Every guide, component page and API page as Markdown, and the llms.txt that lists them, for readers that don't run
// the site's WebAssembly, such as search engines and coding assistants.
public static class DocsMarkdown
{
    public const string Site = "https://zacharydf7.github.io/trailblazor/";

    public static async Task<IReadOnlyList<MarkdownPage>> RenderAsync()
    {
        await using var renderer = new PageRenderer();
        var pages = new List<MarkdownPage>();
        foreach (var guide in GuideCatalog.All)
        {
            var html = await renderer.RenderAsync(guide.Path, GuidePage(guide));
            pages.Add(new(guide.Path, guide.Title, guide.Summary, HtmlMarkdown.Convert(html)));
        }

        foreach (var example in ExampleCatalog.All)
        {
            var name = new Dictionary<string, object?> { [nameof(ComponentPage.Name)] = example.Name };
            var path = $"components/{example.Name}";
            var page = await renderer.RenderAsync(path, typeof(ComponentPage), name);
            pages.Add(new(path, example.Title, example.Summary, HtmlMarkdown.Convert(page)));
            var api = await renderer.RenderAsync($"{path}/api", typeof(ComponentApiPage), name);
            pages.Add(new($"{path}/api", $"{example.Title} API",
                $"{example.Title} parameters, events and methods.",
                HtmlMarkdown.Convert(api)));
        }

        return pages;
    }

    public static async Task WriteAsync(string root)
    {
        var pages = await RenderAsync();
        foreach (var page in pages)
        {
            var file = Path.Combine(root, $"{page.Path}.md");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await File.WriteAllTextAsync(file, page.Markdown);
        }

        await File.WriteAllTextAsync(Path.Combine(root, "llms.txt"), Index(pages));
        await File.WriteAllTextAsync(Path.Combine(root, "llms-full.txt"),
            string.Join("\n", pages.Select(page => page.Markdown)));
    }

    // The site in brief, then a link to each page's Markdown with what it covers, as llmstxt.org lays it out.
    public static string Index(IReadOnlyList<MarkdownPage> pages)
    {
        var index = new StringBuilder("""
            # Trailblazor

            > Accessible Blazor components for .NET 10, built on the browser's own dialogs and popovers, themed with
            > design tokens, easy to test with bUnit and Playwright, with built-in telemetry that counts which features
            > people use.

            ## Guides


            """.ReplaceLineEndings("\n"));
        foreach (var page in pages.Where(page => page.Path.StartsWith("guides/", StringComparison.Ordinal)))
        {
            index.Append($"- [{page.Title}]({Site}{page.Path}.md): {page.Summary}\n");
        }

        index.Append("\n## Components\n\n");
        foreach (var page in pages.Where(page => page.Path.StartsWith("components/", StringComparison.Ordinal)))
        {
            index.Append($"- [{page.Title}]({Site}{page.Path}.md): {page.Summary}\n");
        }

        return index.ToString();
    }

    private static Type GuidePage(Guide guide) => typeof(ComponentPage).Assembly.GetTypes().Single(type =>
        type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template == $"/{guide.Path}"));
}
