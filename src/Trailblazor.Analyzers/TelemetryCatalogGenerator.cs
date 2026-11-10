using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Trailblazor.Analyzers;

/// <summary>Lists each literal <c>TelemetryName</c> on a component in the app's markup as a
/// <c>TelemetryCatalogEntry</c> attribute, so the app can report controls it has but never showed.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class TelemetryCatalogGenerator : IIncrementalGenerator
{
    private const string AttributeType = "Trailblazor.TelemetryCatalogEntryAttribute";

    // Commented-out markup isn't in the app.
    private static readonly Regex Comments = new(@"@\*.*?\*@|<!--.*?-->", RegexOptions.Singleline);

    private static readonly Regex CodeBlock = new(@"@(code|functions)\s*\{");

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entries = context.AdditionalTextsProvider
            .Where(file => Path.GetExtension(file.Path) == ".razor")
            .SelectMany((file, cancel) => Find(file.GetText(cancel)?.ToString() ?? ""))
            .Collect();
        var hasAttribute = context.CompilationProvider
            .Select((compilation, _) => compilation.GetTypeByMetadataName(AttributeType) is not null);
        context.RegisterSourceOutput(entries.Combine(hasAttribute), (output, found) =>
        {
            var (list, available) = found;
            if (available && list.Length > 0)
            {
                output.AddSource("TelemetryCatalog.g.cs", Write(list));
            }
        });
    }

    // A quoted value can hold a '>', as a lambda's arrow does, so the scan skips quoted text.
    internal static ImmutableArray<(string Name, string Component)> Find(string markup)
    {
        markup = WithoutCodeBlocks(Comments.Replace(markup, " "));
        var found = ImmutableArray.CreateBuilder<(string, string)>();
        for (var at = markup.IndexOf('<'); at >= 0; at = markup.IndexOf('<', at + 1))
        {
            if (at + 1 >= markup.Length || !char.IsUpper(markup[at + 1]))
            {
                continue;
            }

            var nameEnd = at + 1;
            while (nameEnd < markup.Length && (char.IsLetterOrDigit(markup[nameEnd]) || markup[nameEnd] is '_' or '.'))
            {
                nameEnd++;
            }

            // A tag can name its namespace, as in Trailblazor.TbButton; the component is the type's own name.
            var tag = markup.Substring(at + 1, nameEnd - at - 1);
            var component = tag.Substring(tag.LastIndexOf('.') + 1);
            foreach (var (attribute, value) in Attributes(markup, nameEnd))
            {
                if (attribute == "TelemetryName" && !string.IsNullOrWhiteSpace(value) && value.IndexOf('@') < 0)
                {
                    found.Add((value, component));
                }
            }
        }

        return found.ToImmutable();
    }

    private static IEnumerable<(string Attribute, string Value)> Attributes(string markup, int at)
    {
        while (at < markup.Length && markup[at] != '>')
        {
            if (char.IsWhiteSpace(markup[at]) || markup[at] == '/')
            {
                at++;
                continue;
            }

            var start = at;
            while (at < markup.Length && !char.IsWhiteSpace(markup[at]) && markup[at] != '=' && markup[at] != '>')
            {
                at++;
            }

            var attribute = markup.Substring(start, at - start);
            if (at < markup.Length && markup[at] == '=' && at + 1 < markup.Length
                && markup[at + 1] is '"' or '\'')
            {
                var quote = markup[at + 1];
                var end = markup.IndexOf(quote, at + 2);
                if (end < 0)
                {
                    yield break;
                }

                yield return (attribute, markup.Substring(at + 2, end - at - 2));
                at = end + 1;
            }
            else if (at < markup.Length && markup[at] == '=')
            {
                // An unquoted value ends at a space or the tag's end, but @(() => Save()) runs to its parenthesis.
                var valueStart = ++at;
                if (at + 1 < markup.Length && markup[at] == '@' && markup[at + 1] == '(')
                {
                    at = ClosingBracket(markup, at + 1);
                }

                while (at < markup.Length && !char.IsWhiteSpace(markup[at]) && markup[at] != '>'
                       && !(markup[at] == '/' && at + 1 < markup.Length && markup[at + 1] == '>'))
                {
                    at++;
                }

                yield return (attribute, markup.Substring(valueStart, at - valueStart));
            }
        }
    }

    // A code block is C#, not markup, though its strings and templates can look like markup.
    private static string WithoutCodeBlocks(string markup)
    {
        var text = new StringBuilder(markup);
        var after = 0;
        foreach (Match block in CodeBlock.Matches(markup))
        {
            if (block.Index < after)
            {
                continue;
            }

            after = ClosingBracket(markup, block.Index + block.Length - 1);
            for (var at = block.Index; at < after; at++)
            {
                text[at] = text[at] == '\n' ? '\n' : ' ';
            }
        }

        return text.ToString();
    }

    // Finds where the bracket at open closes, reading C# tokens so one inside a string or comment doesn't count.
    private static int ClosingBracket(string markup, int open)
    {
        var (opening, closing) = markup[open] == '('
            ? (SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken)
            : (SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken);
        var depth = 0;
        foreach (var token in SyntaxFactory.ParseTokens(markup, open, open))
        {
            if (token.IsKind(opening))
            {
                depth++;
            }
            else if (token.IsKind(closing) && --depth == 0)
            {
                return token.Span.End;
            }
        }

        return markup.Length;
    }

    private static string Write(ImmutableArray<(string Name, string Component)> entries)
    {
        var source = new StringBuilder("// <auto-generated/>\n");
        var sorted = entries.Distinct().OrderBy(entry => entry.Name, System.StringComparer.Ordinal);
        foreach (var (name, component) in sorted)
        {
            source.Append("[assembly: global::").Append(AttributeType).Append('(')
                .Append(SymbolDisplay.FormatLiteral(name, quote: true)).Append(", ")
                .Append(SymbolDisplay.FormatLiteral(component, quote: true)).Append(")]\n");
        }

        return source.ToString();
    }
}
