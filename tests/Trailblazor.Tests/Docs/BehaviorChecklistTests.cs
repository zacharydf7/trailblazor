using System.Text.RegularExpressions;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public partial class BehaviorChecklistTests
{
    private static readonly string Folder = Path.Combine(Repository.Root, "docs", "behavior");

    private static IEnumerable<string> Checklists() =>
        Directory.GetFiles(Folder, "*.md").Where(file => Path.GetFileName(file) != "README.md");

    private static IEnumerable<(string File, string Line)> Items(string start) =>
        Checklists().SelectMany(file => File.ReadAllLines(file).Where(line => line.StartsWith(start))
            .Select(line => (Path.GetFileName(file), line)));

    [Fact]
    public void BehaviorChecklists_ShouldMatchTheComponents_WhenCalled()
    {
        var names = Checklists().Select(Path.GetFileNameWithoutExtension);

        Assert.Equal(ExampleCatalog.All.Select(example => example.Name).Order(), names.Order());
    }

    [Fact]
    public void BehaviorChecklists_ShouldLinkAnIssueOrSayWhyForEveryOpenItem_WhenCalled()
    {
        Assert.DoesNotContain(Items("- [ ]"), item => !OpenItemReason().IsMatch(item.Line));
    }

    [Fact]
    public void BehaviorChecklists_ShouldNameEvidenceForEveryTickedItem_WhenCalled()
    {
        Assert.DoesNotContain(Items("- [x]"), item => !NamedEvidenceAtTheEnd().IsMatch(item.Line));
    }

    [Fact]
    public void BehaviorChecklists_ShouldNameTestsThatExistInTheirClass_WhenCalled()
    {
        var tests = Directory.GetFiles(Path.Combine(Repository.Root, "tests"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText).ToList();

        // A checklist test may be declared on a base class, since a component's keyboard tests live in its keys class.
        bool Declares(string type, string method) =>
            tests.Any(text => Regex.IsMatch(text, $@"\bclass {type}\b")
                && Regex.IsMatch(text, $@"\b(?:Task|void) {method}\("))
            || tests.Select(text => Regex.Match(text, $@"\bclass {type}\b(?:\([^)]*\))?\s*:\s*(?<base>\w+)"))
                .FirstOrDefault(found => found.Success) is { } derived
            && Declares(derived.Groups["base"].Value, method);

        var missing = Items("- [x]").SelectMany(item => NamedTest().Matches(item.Line)
            .Where(match => !Declares(match.Groups["class"].Value, match.Groups["method"].Value))
            .Select(match => $"{item.File}: {match.Value}"));

        Assert.Empty(missing);
    }

    [Fact]
    public void BehaviorChecklists_ShouldNameSourceFilesThatExist_WhenCalled()
    {
        var names = Directory.GetFiles(Path.Combine(Repository.Root, "src"), "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName).ToHashSet();

        var missing = Items("- [x]").SelectMany(item => NamedSource().Matches(item.Line)
            .Where(match => !names.Contains(match.Groups["file"].Value))
            .Select(match => $"{item.File}: {match.Value}"));

        Assert.Empty(missing);
    }

    [GeneratedRegex(@"(#\d+|UNCONFIRMED)\s*$")]
    private static partial Regex OpenItemReason();

    [GeneratedRegex(@"`(\w+Tests?\.\w+|[\w.-]+\.(js|razor|cs|css))`$")]
    private static partial Regex NamedEvidenceAtTheEnd();

    [GeneratedRegex(@"`(?<class>\w+Tests?)\.(?<method>\w+)`")]
    private static partial Regex NamedTest();

    [GeneratedRegex(@"`(?<file>[\w.-]+\.(?:js|razor|cs|css))`")]
    private static partial Regex NamedSource();
}
