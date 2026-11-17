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
    // The milestone title of the phase under way, whose components show as coming soon; update it when the next
    // phase starts.
    public const string CurrentPhase = "Phase 4 · Advanced tier";

    public static IReadOnlyList<UpcomingComponent> All { get; } =
    [
        new("Dashboard tile layout", ExampleCategory.Layout, 204),
        new("Dock manager", ExampleCategory.Layout, 75),
        new("Masonry layout", ExampleCategory.Layout, 193),
        new("Charts", ExampleCategory.Content, 199),
        new("Diagram", ExampleCategory.Content, 207),
        new("Gantt chart", ExampleCategory.Content, 203),
        new("Gauges", ExampleCategory.Content, 200),
        new("Image editor", ExampleCategory.Content, 210),
        new("Maps", ExampleCategory.Content, 208),
        new("PDF viewer", ExampleCategory.Content, 205),
        new("Pivot grid", ExampleCategory.Content, 202),
        new("Scheduler", ExampleCategory.Content, 201),
        new("Spreadsheet", ExampleCategory.Content, 206),
        new("Text highlighter", ExampleCategory.Content, 195),
        new("Back to top button", ExampleCategory.Actions, 194),
        new("Ribbon", ExampleCategory.Actions, 83),
        new("Smart paste", ExampleCategory.Actions, 198),
        new("Speech-to-text button", ExampleCategory.Actions, 197),
        new("In-place editor", ExampleCategory.Inputs, 196),
        new("File manager", ExampleCategory.Navigation, 209),
    ];

    public static IReadOnlyList<RoadmapIssue> Issues { get; } = Load();

    public static IReadOnlyList<UpcomingEntry> Current { get; } = Arrange(All, Issues, CurrentPhase);

    public static IEnumerable<UpcomingEntry> In(ExampleCategory category) =>
        Current.Where(entry => entry.Component.Category == category);

    // A closed (shipped or dropped) or missing issue skips its component instead of throwing; a test catches typos.
    // Coming soon means in progress or in the current phase.
    public static IReadOnlyList<UpcomingEntry> Arrange(IReadOnlyList<UpcomingComponent> components,
        IReadOnlyList<RoadmapIssue> issues, string currentPhase)
    {
        var byNumber = issues.ToDictionary(issue => issue.Number);
        return [.. components
            .Where(component => byNumber.ContainsKey(component.Issue))
            .Select(component => (Component: component, Issue: byNumber[component.Issue]))
            .Where(pair => pair.Issue.Open)
            .Select(pair => new UpcomingEntry(pair.Component, StageOf(pair.Issue, currentPhase)))
            .OrderBy(entry => entry.Stage).ThenBy(entry => entry.Component.Title, StringComparer.CurrentCulture)];
    }

    private static UpcomingStage StageOf(RoadmapIssue issue, string currentPhase) =>
        issue.Labels.Contains("in progress") || issue.Phase == currentPhase
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
