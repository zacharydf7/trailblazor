using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Docs.Markdown;

// Turns a rendered docs page into Markdown: its headings, text, lists, tables, callouts and code. What only works in
// the browser is left out: live demos and their buttons, the page's table of contents and its links to other views. A
// demo outside an example's frame is marked docs-demo.
public static partial class HtmlMarkdown
{
    private const string Skipped = ".docs-article__folded, .docs-article__rail, .docs-heading__link, "
        + ".docs-api__anchor, .docs-example__stage, .docs-example__bar, .docs-views, .docs-page-links, .docs-pager, "
        + ".docs-demo, [aria-hidden=true], button, nav";

    private static readonly HashSet<string> Inline =
        ["A", "ABBR", "B", "BR", "CODE", "EM", "I", "IMG", "KBD", "LABEL", "SMALL", "SPAN", "STRONG", "SUB", "SUP"];

    public static string Convert(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        var markdown = new StringBuilder();
        Blocks(document.QuerySelector(".docs-article__body")
            ?? throw new InvalidOperationException(
                "The rendered page has no .docs-article__body, where every docs page puts its text."), markdown);
        return BlankLines().Replace(markdown.ToString(), "\n\n").Trim() + "\n";
    }

    // Runs of text and inline elements between blocks are a paragraph each.
    private static void Blocks(IElement parent, StringBuilder markdown)
    {
        var run = new List<INode>();
        foreach (var node in parent.ChildNodes)
        {
            if (node is IElement element && !Inline.Contains(element.TagName))
            {
                Paragraph(run, markdown);
                Block(element, markdown);
            }
            else
            {
                run.Add(node);
            }
        }

        Paragraph(run, markdown);
    }

    private static void Paragraph(List<INode> run, StringBuilder markdown)
    {
        var text = Text(run);
        if (text.Length > 0)
        {
            markdown.Append(text).Append("\n\n");
        }

        run.Clear();
    }

    private static void Block(IElement element, StringBuilder markdown)
    {
        if (element.Matches(Skipped)
            || (element.ClassList.Contains("docs-example") && element.QuerySelector(".docs-example__code") is null))
        {
            return;
        }

        switch (element.TagName)
        {
            case "H1" or "H2" or "H3" or "H4" or "H5" or "H6":
                markdown.Append('#', element.TagName[1] - '0').Append(' ').Append(Text(element.ChildNodes))
                    .Append("\n\n");
                break;
            case "UL" or "OL":
                List(element, markdown, "");
                markdown.Append('\n');
                break;
            case "TABLE":
                Table(element, markdown);
                break;
            case "PRE":
                Code(element, markdown);
                break;
            default:
                if (element.Matches(".tb-alert"))
                {
                    var callout = new StringBuilder();
                    Blocks(element, callout);
                    markdown.AppendJoin('\n', callout.ToString().Trim().Split('\n')
                        .Select(line => line.Length == 0 ? ">" : $"> {line}")).Append("\n\n");
                }
                else
                {
                    Blocks(element, markdown);
                }

                break;
        }
    }

    private static void List(IElement list, StringBuilder markdown, string indent)
    {
        var number = 1;
        foreach (var item in list.Children.Where(child => child.TagName == "LI" && !child.Matches(Skipped)))
        {
            var marker = list.TagName == "OL" ? $"{number++}." : "-";
            var text = Text(item.ChildNodes.Where(node => node is not IElement { TagName: "UL" or "OL" }));
            markdown.Append(indent).Append(marker).Append(' ').Append(text).Append('\n');
            foreach (var nested in item.Children.Where(child => child.TagName is "UL" or "OL"))
            {
                List(nested, markdown, indent + "  ");
            }
        }
    }

    private static void Table(IElement table, StringBuilder markdown)
    {
        var rows = table.QuerySelectorAll("tr").Select(row => row.Children.Where(cell => cell.TagName is "TH" or "TD")
            .Select(cell => Text(cell.ChildNodes).Replace("|", "\\|")).ToList()).Where(row => row.Count > 0).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        markdown.Append("| ").AppendJoin(" | ", rows[0]).Append(" |\n");
        markdown.Append('|').AppendJoin('|', rows[0].Select(_ => " --- ")).Append("|\n");
        foreach (var row in rows.Skip(1))
        {
            // A row with one cell across the table, such as a keyboard table's "While open", heads the rows under it.
            var cells = row.Count == 1 && rows[0].Count > 1 ? [$"**{row[0]}**"] : row;
            markdown.Append("| ").AppendJoin(" | ", cells.Concat(Enumerable.Repeat("", rows[0].Count - cells.Count)))
                .Append(" |\n");
        }

        markdown.Append('\n');
    }

    private static void Code(IElement pre, StringBuilder markdown)
    {
        var code = pre.TextContent.TrimEnd();
        var language = CodeHighlighter.LanguageOf(pre.GetAttribute("aria-label") ?? "", code) switch
        {
            CodeLanguage.Razor => "razor",
            CodeLanguage.CSharp => "csharp",
            CodeLanguage.Css => "css",
            CodeLanguage.Json => "json",
            CodeLanguage.Shell => "shell",
            _ => "",
        };
        if (pre.GetAttribute("aria-label") is { Length: > 0 } label)
        {
            markdown.Append("**").Append(label).Append("**\n\n");
        }

        markdown.Append("```").Append(language).Append('\n').Append(code).Append("\n```\n\n");
    }

    private static string Text(IEnumerable<INode> nodes) =>
        Spaces().Replace(string.Concat(nodes.Select(InlineText)), " ").Trim();

    private static string InlineText(INode node) => node switch
    {
        IText text => text.Data,
        IElement element when element.Matches(Skipped) => "",
        IElement { TagName: "CODE" } code => $"`{code.TextContent.Trim()}`",
        IElement { TagName: "A" } link when Text(link.ChildNodes) is { Length: > 0 } text => $"[{text}]({Link(link)})",
        IElement { TagName: "STRONG" or "B" } strong => $"**{Text(strong.ChildNodes)}**",
        IElement { TagName: "EM" or "I" } emphasis => $"*{Text(emphasis.ChildNodes)}*",
        IElement { TagName: "IMG" } image => image.GetAttribute("alt") ?? "",
        // A list inside a table cell or a list item reads as one line.
        IElement { TagName: "UL" or "OL" } list =>
            $" {string.Join("; ", list.Children.Select(item => Text(item.ChildNodes)))} ",
        IElement element => $" {string.Concat(element.ChildNodes.Select(InlineText))} ",
        _ => "",
    };

    // A link to another docs page goes to its Markdown, which a reader of this one can follow.
    private static string Link(IElement link)
    {
        var href = link.GetAttribute("href") ?? "";
        if (href.StartsWith('#') || href.Contains("://", StringComparison.Ordinal))
        {
            return href;
        }

        var (path, fragment) = href.IndexOf('#') is var hash and >= 0 ? (href[..hash], href[hash..]) : (href, "");
        return path.Length == 0 ? DocsMarkdown.Site + fragment : $"{DocsMarkdown.Site}{path}.md{fragment}";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();
}
