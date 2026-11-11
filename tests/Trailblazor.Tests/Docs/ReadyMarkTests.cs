using System.Text.RegularExpressions;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public partial class ReadyMarkTests
{
    // Classes no docs page lists in its API: ToastHost is checked through the toast page; DialogFrame needs no mark,
    // since its script is what opens the dialog, so a test waits for the dialog itself; BrowserService is a service.
    private static readonly Dictionary<string, string?> Hosts = new()
    {
        ["ToastHost"] = "toast", ["DialogFrame"] = null, ["BrowserService"] = null,
    };

    [GeneratedRegex(@"InvokeAsync<(IJSObjectReference|TabsAttachment)\??>\(")]
    private static partial Regex CreatesScriptObject();

    private static IEnumerable<string> ScriptedComponents() =>
        Directory.GetFiles(Path.Combine(Repository.Root, "src", "Trailblazor"), "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs") || file.EndsWith(".razor"))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(file => CreatesScriptObject().IsMatch(File.ReadAllText(file)))
            .Select(Path.GetFileNameWithoutExtension).OfType<string>().Distinct();

    // A type's name and its base types' names, without the generic arity, so a page listing TbSelect covers
    // TbSelectBase.
    private static IEnumerable<string> TypeNames(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current.Name.Split('`')[0];
        }
    }

    [Fact]
    public void ScriptedComponents_ShouldEachHaveAReadyMark_WhenTheyCreateAScriptObject()
    {
        var marked = ExampleCatalog.All.Where(example => example.Ready is not null)
            .SelectMany(example => example.Api).SelectMany(TypeNames).ToHashSet();

        var unmarked = ScriptedComponents().Where(component => Hosts.TryGetValue(component, out var page)
            ? page is not null && ExampleCatalog.Find(page)?.Ready is null
            : !marked.Contains(component));

        Assert.Empty(unmarked);
    }

    [Fact]
    public void ScriptedComponents_ShouldBeFound_WhenTheSourceIsScanned()
    {
        Assert.Contains("TbTreeView", ScriptedComponents());
        Assert.Contains("TbSelectBase", ScriptedComponents());
    }
}
