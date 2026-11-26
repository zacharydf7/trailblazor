using System.Text.RegularExpressions;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests.Contract;

// Every custom property an app may rely on is listed in TbTokens, and every other one is marked private with --_tb-
// (#626), so a release can't change one an app sets without the list showing it.
public partial class TokensContractTests
{
    private static readonly string[] Sources =
    [
        Path.Combine(Repository.Root, "src", "Trailblazor"),
        Path.Combine(Repository.Root, "src", "Trailblazor.Primitives"),
    ];

    private static readonly string[] Kinds = [".css", ".razor", ".cs", ".js"];

    private static readonly string[] Listed = [.. TbTokens.Theme, .. TbTokens.Component, .. TbTokens.SetByComponents];

    // The starts of names built in code, such as --tb-color- with a tone after it, each a family of theme tokens.
    private static readonly string[] BuiltInCode = ["--tb-color-", "--tb-space-", "--tb-breakpoint-"];

    private static readonly string Stylesheet =
        File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "trailblazor.css"));

    [Fact]
    public void Sources_ShouldUseOnlyListedPublicTokens_WhenTheyNameATbCustomProperty()
    {
        var unlisted = Sources
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
            .Where(file => Kinds.Contains(Path.GetExtension(file))
                && !Path.GetRelativePath(Repository.Root, file).Split(Path.DirectorySeparatorChar)
                    .Any(part => part is "bin" or "obj"))
            .SelectMany(file => Name().Matches(File.ReadAllText(file)).Select(match => (file, match.Value)))
            .Where(use => use.Value.EndsWith('-') ? !BuiltInCode.Contains(use.Value) : !Listed.Contains(use.Value))
            .Select(use => $"{use.Value} in {Path.GetFileName(use.file)}").Distinct().Order().ToList();

        Assert.Empty(unlisted);
    }

    [Fact]
    public void Theme_ShouldBeTheTokensTheStylesheetDeclaresOutsideAnyComponent_WhenRead()
    {
        // Outside a component's rules means on :root, a palette or a density.
        var css = Regex.Replace(Stylesheet, @"/\*.*?\*/", "", RegexOptions.Singleline);
        var declared = Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}")
            .Where(rule => !rule.Groups[1].Value.Contains(".tb-"))
            .SelectMany(rule => Regex.Matches(rule.Groups[2].Value, @"(--tb-[\w-]+)\s*:"))
            .Select(match => match.Groups[1].Value).ToHashSet();

        Assert.Equal(declared.Order(), TbTokens.Theme.Order());
    }

    [Fact]
    public void TbTokens_ShouldEachBeUsedByTheStylesheet_WhenListed()
    {
        var used = Name().Matches(Stylesheet).Select(match => match.Value).ToHashSet();

        Assert.Empty(Listed.Except(used));
    }

    [Fact]
    public void Classes_ShouldEachBeStyledByTheStylesheet_WhenListed()
    {
        Assert.All(TbTokens.Classes, name => Assert.Matches($@"\.{name}(?![\w-])", Stylesheet));
    }

    [Fact]
    public void TbTokens_ShouldListEachTokenOnce_WhenCalled()
    {
        Assert.Empty(Listed.GroupBy(token => token).Where(group => group.Count() > 1).Select(group => group.Key));
    }

    [GeneratedRegex(@"--tb-[a-z0-9-]*")]
    private static partial Regex Name();
}
