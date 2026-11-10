using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Trailblazor.Analyzers.Tests;

public class TelemetryCatalogGeneratorTests
{
    private sealed class RazorFile(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }

    [Fact]
    public void Find_ShouldListEachLiteralNameWithItsComponent_WhenGivenMarkup()
    {
        const string markup = """"
            <TbButton OnClick="() => count++" TelemetryName="export-csv">Export</TbButton>
            <TbSelect TValue="string"
                      TelemetryName='trail-size' Label="Size" />
            <TbSwitch TelemetryName="@name" />
            <TbChip TelemetryName="" />
            <button TelemetryName="plain-button" />
            <TbCheckbox Disabled=@busy @bind-Value=agreed TelemetryName="terms" />
            <AppButton TelemetryName="edit-name">Edit</AppButton>
            <TbChip TelemetryName=unquoted-chip/>
            @* <TbButton TelemetryName="razor-comment" /> *@
            <!-- <TbButton TelemetryName="html-comment" /> -->
            <TbButton OnClick=@(() => Save("a)b")) TelemetryName="after-expression" />
            <TbChip TelemetryName=" " />
            <Trailblazor.TbChip TelemetryName="qualified" />
            @code {
                // Don't count a sample's name.
                /* It's a block comment { */
                private void Check(bool open)
                {
                    if (open) { }
                }

                private RenderFragment Template => @<TbButton TelemetryName="in-template" />;
                private const string Raw = """<TbButton TelemetryName="raw-string" />""";
                private const string Sample = "@code { {";
                private string _regular = "<TbChip TelemetryName=\"regular-string\" />";
                private string _verbatim = @"<TbChip TelemetryName=""verbatim-string"" />";
                private char _quote = '"';
            }
            <TbSwitch TelemetryName="after-code" />
            """";

        var found = TelemetryCatalogGenerator.Find(markup);

        Assert.Equal([("export-csv", "TbButton"), ("trail-size", "TbSelect"), ("terms", "TbCheckbox"),
            ("edit-name", "AppButton"), ("unquoted-chip", "TbChip"), ("after-expression", "TbButton"),
            ("qualified", "TbChip"), ("after-code", "TbSwitch")], found);
    }

    [Fact]
    public void Generator_ShouldWriteOneCatalogEntryPerName_WhenTheAppReferencesTrailblazor()
    {
        var compilation = CSharpCompilation.Create("App", [], Analysis.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var files = ImmutableArray.Create<AdditionalText>(
            new RazorFile("Pages/Trails.razor", """<TbButton TelemetryName="export-csv" />"""),
            new RazorFile("Pages/Again.razor", """<TbButton TelemetryName="export-csv" />"""),
            new RazorFile("notes.txt", """<TbButton TelemetryName="not-markup" />"""));

        var driver = CSharpGeneratorDriver.Create([new TelemetryCatalogGenerator().AsSourceGenerator()],
            files).RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics,
            TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        var source = Assert.Single(driver.GetRunResult().GeneratedTrees).ToString();
        Assert.Single(source.Split('\n'), line =>
            line == """[assembly: global::Trailblazor.TelemetryCatalogEntryAttribute("export-csv", "TbButton")]""");
        Assert.DoesNotContain("not-markup", source);
        Assert.Empty(updated.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
