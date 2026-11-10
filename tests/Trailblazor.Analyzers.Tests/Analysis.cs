using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Trailblazor.Analyzers.Tests;

// Compiles a page's render code, written the way Razor generates it from markup, and runs the analyzer on it.
internal static class Analysis
{
    internal static readonly MetadataReference[] References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    public static async Task<ImmutableArray<Diagnostic>> RunAsync(string render, string members = "")
    {
        var source = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using Trailblazor;

            public class Page : ComponentBase
            {
                protected override void BuildRenderTree(RenderTreeBuilder __builder)
                {
            {{render}}
                }

            {{members}}
            }
            """;
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);
        var tree = CSharpSyntaxTree.ParseText(source, path: "Page.razor.g.cs");
        var compilation = CSharpCompilation.Create("Page", [tree], References, options);
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers([new ComponentAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    public static async Task<IEnumerable<string>> IdsAsync(string render, string members = "") =>
        (await RunAsync(render, members)).Select(diagnostic => diagnostic.Id);
}
