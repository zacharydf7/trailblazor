using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class CodeHighlighterTests
{
    [Theory]
    [InlineData("app.css", ":root { }", CodeLanguage.Css)]
    [InlineData("data.json", "{ }", CodeLanguage.Json)]
    [InlineData("build.sh", "dotnet build", CodeLanguage.Shell)]
    [InlineData("Page.razor", "<p>Hi</p>", CodeLanguage.Razor)]
    [InlineData("MyApp.csproj", "<PropertyGroup />", CodeLanguage.Razor)]
    [InlineData("index.html, standalone WebAssembly", "<link />", CodeLanguage.Razor)]
    [InlineData("App.razor and Program.cs", "<head>", CodeLanguage.Razor)]
    [InlineData("Program.cs", "var trail = 1;", CodeLanguage.CSharp)]
    [InlineData("A first component", "@using Trailblazor", CodeLanguage.Razor)]
    [InlineData("A bUnit test", "var page = Render<TrailPage>();", CodeLanguage.CSharp)]
    public void LanguageOf_ShouldPickTheLanguage_WhenGivenALabelAndItsCode(string label, string code,
        CodeLanguage expected)
    {
        Assert.Equal(expected, CodeHighlighter.LanguageOf(label, code));
    }

    public static TheoryData<string, CodeLanguage, (string, string)[]> Snippets => new()
    {
        {
            """<TbButton OnClick="Save">Go for it</TbButton>""", CodeLanguage.Razor,
            [("<TbButton", "tag"), ("OnClick", "attribute"), ("\"Save\"", "string"), (">", "tag"),
                ("</TbButton", "tag"), (">", "tag")]
        },
        {
            "<TbButton />\n@code {\n    private List<int> _ids = [];\n    Task GoAsync() => Task.CompletedTask;\n}",
            CodeLanguage.Razor,
            [("<TbButton", "tag"), ("/>", "tag"), ("@code", "keyword"), ("private", "keyword"), ("int", "keyword")]
        },
        {
            "@foreach (var tone in Enum.GetValues<Tone>())\n{\n    <TbAlert Tone=\"tone\" />\n}", CodeLanguage.Razor,
            [("@foreach", "keyword"), ("<TbAlert", "tag"), ("Tone", "attribute"), ("\"tone\"", "string"), ("/>", "tag")]
        },
        {
            "<TbRoot />\nbuilder.Services.AddTrailblazor(options => { });", CodeLanguage.Razor,
            [("<TbRoot", "tag"), ("/>", "tag")]
        },
        {
            "var name = \"Ada\"; // greet\nint count = 2;", CodeLanguage.CSharp,
            [("var", "keyword"), ("\"Ada\"", "string"), ("// greet", "comment"), ("int", "keyword"), ("2", "number")]
        },
        {
            "/* note */\n.button:hover {\n    font-weight: 600;\n    color: #fff;\n}", CodeLanguage.Css,
            [("/* note */", "comment"), ("font-weight", "attribute"), ("600", "number"), ("color", "attribute"),
                ("#fff", "number")]
        },
        {
            """{ "name": "Ada", "age": 3, "ok": true }""", CodeLanguage.Json,
            [("\"name\"", "attribute"), ("\"Ada\"", "string"), ("\"age\"", "attribute"), ("3", "number"),
                ("\"ok\"", "attribute"), ("true", "keyword")]
        },
        {
            "dotnet add package Trailblazor --version 1 # pinned", CodeLanguage.Shell,
            [("dotnet", "keyword"), ("--version", "attribute"), ("# pinned", "comment")]
        },
        { "sum by (name) (increase(used[30d]))", CodeLanguage.Plain, [] },
    };

    [Theory, MemberData(nameof(Snippets))]
    public void Tokens_ShouldColorEachTokenByItsKindAndKeepTheText_WhenGivenASnippet(string code,
        CodeLanguage language, (string, string)[] expected)
    {
        var tokens = CodeHighlighter.Tokens(code, language).ToList();

        Assert.Equal(code, string.Concat(tokens.Select(token => token.Text)));
        Assert.Equal(expected, tokens.Where(token => token.Kind is not null)
            .Select(token => (token.Text, token.Kind!)).ToArray());
    }
}
