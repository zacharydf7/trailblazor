using System.Text.RegularExpressions;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests;

// While one of Firefox's pages holds a mouse button down, clicks and moves meant for its other pages go astray. A
// browser test class that holds one gives each of its tests a Firefox of its own, with HoldsTheMouse, or is in the
// PointerDrags collection, which runs with no other test.
public partial class MouseHoldTests
{
    [Fact]
    public void BrowserTests_ShouldKeepFirefoxsOtherPagesOutOfReach_WhenTheirClassHoldsAMouseButton()
    {
        var holds = WhatHoldsTheMouse();

        var sharing = SourcesIn("tests", "Trailblazor.BrowserTests")
            .SelectMany(file => ClassStarts().Split(File.ReadAllText(file)))
            .Where(source => holds.IsMatch(source) && !source.Contains("HoldsTheMouse => true")
                && !source.Contains("[Collection(nameof(PointerDrags))]"))
            .Select(source => ClassName().Match(source).Groups[1].Value);

        Assert.Empty(sharing);
    }

    [Fact]
    public void WhatHoldsTheMouse_ShouldNameTheTestingLibrarysDrags_WhenCalled()
    {
        var holds = WhatHoldsTheMouse();

        Assert.All(["await page.Mouse.DownAsync();", "await pad.DrawAsync((10, 10), (90, 60));",
            "await grid.ResizeColumnAsync(\"Name\", 40);", "await list.DragAfterAsync(\"Tent\", \"Stove\");"],
            line => Assert.Matches(holds, line));
        Assert.DoesNotMatch(holds, "await page.Keyboard.DownAsync(\"Shift\"); await button.ClickAsync();");
    }

    // A project's own source files, in its folder and the folders under it.
    private static IEnumerable<string> SourcesIn(params string[] project) =>
        Directory.GetFiles(Path.Combine([Repository.Root, .. project]), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(folder => folder is "bin" or "obj"));

    // A press of the mouse, or a call, by name, of any method in the Playwright testing library that leads to one: the
    // library's drags hold the button just as a test's own press does.
    private static Regex WhatHoldsTheMouse()
    {
        var methods = new List<(string Name, string Body)>();
        foreach (var file in SourcesIn("src", "Trailblazor.Testing.Playwright"))
        {
            var text = File.ReadAllText(file);
            var starts = MethodStarts().Matches(text);
            for (var at = 0; at < starts.Count; at++)
            {
                var from = starts[at].Index + starts[at].Length;
                var to = at + 1 < starts.Count ? starts[at + 1].Index : text.Length;
                methods.Add((starts[at].Groups[1].Value, text[from..to]));
            }
        }

        var names = new HashSet<string>();
        while (true)
        {
            var holds = new Regex(names.Count == 0 ? MousePress : $@"{MousePress}|\b(?:{string.Join('|', names)})\(");
            var more = methods.Where(method => !names.Contains(method.Name) && holds.IsMatch(method.Body))
                .Select(method => method.Name).ToList();
            if (more.Count == 0)
            {
                return holds;
            }

            names.UnionWith(more);
        }
    }

    private const string MousePress = @"[Mm]ouse\.DownAsync\(";

    // Where a class starts, counting the attributes above it.
    [GeneratedRegex(@"(?m)^(?=(?:\[.*\]\r?\n)+public .*\bclass |(?<!\]\r?\n)public .*\bclass )")]
    private static partial Regex ClassStarts();

    [GeneratedRegex(@"\bclass (\w+)")]
    private static partial Regex ClassName();

    // Where a method starts: its name, up to the bracket that opens its parameters.
    [GeneratedRegex(@"(?m)^ +(?:public|internal|private|protected)[^\n=;(]*?\b(\w+)(?:<[^>\n]*>)?\(")]
    private static partial Regex MethodStarts();
}
