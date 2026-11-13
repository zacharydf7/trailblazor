using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trailblazor.Docs.Examples;

// An issue as roadmap.json has it; Phase is its milestone, and a dropped issue was closed without shipping.
public sealed record RoadmapIssue(int Number, bool Open, string? Phase, IReadOnlyList<string> Labels,
    bool Dropped = false);

public enum UpcomingStage
{
    ComingSoon,
    Planned,
}

// A component the navigation lists before it ships, with the category it will join and the issue that tracks it.
public sealed record UpcomingComponent(string Title, ExampleCategory Category, int Issue);

public sealed record UpcomingEntry(UpcomingComponent Component, UpcomingStage Stage)
{
    public string Label => Stage == UpcomingStage.ComingSoon ? "Coming soon" : "Planned";
}

public static class UpcomingCatalog
{
    public static IReadOnlyList<UpcomingComponent> All { get; } =
    [
        new("Dashboard tile layout", ExampleCategory.Layout, 204),
        new("Dock manager", ExampleCategory.Layout, 75),
        new("Masonry layout", ExampleCategory.Layout, 193),
        new("Viewport", ExampleCategory.Layout, 82),
        new("Charts", ExampleCategory.Content, 199),
        new("Chat", ExampleCategory.Content, 181),
        new("Diagram", ExampleCategory.Content, 207),
        new("Gantt chart", ExampleCategory.Content, 203),
        new("Gauges", ExampleCategory.Content, 200),
        new("Image editor", ExampleCategory.Content, 210),
        new("Kanban board", ExampleCategory.Content, 184),
        new("Maps", ExampleCategory.Content, 208),
        new("PDF viewer", ExampleCategory.Content, 205),
        new("Pivot grid", ExampleCategory.Content, 202),
        new("QR code and barcode", ExampleCategory.Content, 183),
        new("Scheduler", ExampleCategory.Content, 201),
        new("Spreadsheet", ExampleCategory.Content, 206),
        new("Text highlighter", ExampleCategory.Content, 195),
        new("Tree grid", ExampleCategory.Content, 81),
        new("Watermark", ExampleCategory.Content, 192),
        new("Back to top button", ExampleCategory.Actions, 194),
        new("Ribbon", ExampleCategory.Actions, 83),
        new("Smart paste", ExampleCategory.Actions, 198),
        new("Speech-to-text button", ExampleCategory.Actions, 197),
        new("AI prompt", ExampleCategory.Inputs, 182),
        new("Cascader", ExampleCategory.Inputs, 186),
        new("Form from a model", ExampleCategory.Inputs, 180),
        new("In-place editor", ExampleCategory.Inputs, 196),
        new("Markdown editor", ExampleCategory.Inputs, 189),
        new("Mention input", ExampleCategory.Inputs, 190),
        new("Multi-column combobox", ExampleCategory.Inputs, 185),
        new("Query builder", ExampleCategory.Inputs, 174),
        new("Signature pad", ExampleCategory.Inputs, 179),
        new("Image preview", ExampleCategory.Overlays, 188),
        new("Product tour", ExampleCategory.Overlays, 191),
        new("Sheet", ExampleCategory.Overlays, 59),
        new("File manager", ExampleCategory.Navigation, 209),
        new("Menubar", ExampleCategory.Navigation, 187),
    ];

    public static IReadOnlyList<RoadmapIssue> Issues { get; } = Load();

    public static IReadOnlyList<UpcomingEntry> Current { get; } = Arrange(All, Issues);

    public static IEnumerable<UpcomingEntry> In(ExampleCategory category) =>
        Current.Where(entry => entry.Component.Category == category);

    // A closed (shipped or dropped) or missing issue skips its component instead of throwing; a test catches typos.
    // Coming soon means in progress or in the current or next phase; phases sort by title while numbered below 10.
    public static IReadOnlyList<UpcomingEntry> Arrange(IReadOnlyList<UpcomingComponent> components,
        IReadOnlyList<RoadmapIssue> issues)
    {
        var phases = issues.Where(issue => issue.Phase is not null && !issue.Dropped).Select(issue => issue.Phase!)
            .Distinct().Order(StringComparer.Ordinal).ToList();
        var current = phases.FindIndex(phase => issues.Any(issue => issue.Phase == phase && issue.Open));
        var byNumber = issues.ToDictionary(issue => issue.Number);
        return [.. components
            .Where(component => byNumber.ContainsKey(component.Issue))
            .Select(component => (Component: component, Issue: byNumber[component.Issue]))
            .Where(pair => pair.Issue.Open)
            .Select(pair => new UpcomingEntry(pair.Component, StageOf(pair.Issue, phases, current)))
            .OrderBy(entry => entry.Stage).ThenBy(entry => entry.Component.Title, StringComparer.CurrentCulture)];
    }

    private static UpcomingStage StageOf(RoadmapIssue issue, List<string> phases, int current) =>
        issue.Labels.Contains("in progress") || (issue.Phase is not null && phases.IndexOf(issue.Phase) <= current + 1)
            ? UpcomingStage.ComingSoon : UpcomingStage.Planned;

    private static IReadOnlyList<RoadmapIssue> Load()
    {
        using var stream = typeof(UpcomingCatalog).Assembly.GetManifestResourceStream("Roadmap/roadmap.json")
            ?? throw new InvalidOperationException("roadmap.json isn't embedded.");
        return JsonSerializer.Deserialize(stream, RoadmapJson.Default.RoadmapIssueArray)!;
    }
}

// Generated at build time, so trimming the published site can't break reading the file.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RoadmapIssue[]))]
internal sealed partial class RoadmapJson : JsonSerializerContext;
