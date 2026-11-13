using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Trailblazor.Analyzers;

// Razor maps a component's attributes to the .razor file but not its start tag. A page's code builds its components in
// the order its markup writes them, so a component is found by its place among the page's tags of the same name.
internal static class RazorTag
{
    public static Location? Find(InvocationExpressionSyntax open, string component,
        ImmutableDictionary<string, AdditionalText> razorFiles, CancellationToken cancel)
    {
        // Only the markup's own components are counted: a type-inference helper or a render fragment in @code builds
        // its components outside BuildRenderTree.
        var method = open.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is not { Identifier.Text: "BuildRenderTree" } || RazorPath(open.SyntaxTree, cancel) is not { } path
            || !razorFiles.TryGetValue(path, out var file) || file.GetText(cancel) is not { } text)
        {
            return null;
        }

        // A tag in a comment builds nothing, so comments are blanked, keeping every offset. Any other tag the markup's
        // code doesn't build, such as one in @code, makes the counts differ, and then no tag is chosen.
        var builds = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => Builds(call, component)).ToList();
        var markup = Comments.Replace(text.ToString(), comment => new string(' ', comment.Length));
        var tags = Regex.Matches(markup, $@"<{Regex.Escape(component)}(?=[\s/>])");
        var place = builds.IndexOf(open);
        if (place < 0 || tags.Count != builds.Count)
        {
            return null;
        }

        var span = new TextSpan(tags[place].Index + 1, component.Length);
        return Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
    }

    private static readonly Regex Comments = new(@"@\*.*?\*@|<!--.*?-->", RegexOptions.Singleline);

    // The page's code opens the component itself, or calls the type-inference helper Razor writes for a generic one.
    private static bool Builds(InvocationExpressionSyntax call, string component) => call.Expression switch
    {
        MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.Text: "OpenComponent" } open } =>
            NameOf(open.TypeArgumentList.Arguments.FirstOrDefault()) == component,
        MemberAccessExpressionSyntax { Name: IdentifierNameSyntax helper } =>
            Regex.IsMatch(helper.Identifier.Text, $@"^Create{Regex.Escape(component)}_\d+$"),
        _ => false,
    };

    private static string? NameOf(TypeSyntax? type) => type switch
    {
        QualifiedNameSyntax qualified => NameOf(qualified.Right),
        AliasQualifiedNameSyntax alias => NameOf(alias.Name),
        SimpleNameSyntax simple => simple.Identifier.Text,
        _ => null,
    };

    // The .razor file the page was generated from, named in the first checksum Razor writes; the generated #line
    // directives name _Imports.razor too, so they aren't used.
    private static string? RazorPath(SyntaxTree tree, CancellationToken cancel) => tree.GetRoot(cancel)
        .DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<PragmaChecksumDirectiveTriviaSyntax>()
        .Select(checksum => checksum.File.ValueText).FirstOrDefault();
}
