using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trailblazor.Docs.Examples;

// An issue as roadmap.json has it; Phase is its milestone, and a dropped issue was closed without shipping.
public sealed record RoadmapIssue(int Number, string Title, bool Open, string? Phase, IReadOnlyList<string> Labels,
    bool Dropped = false)
{
    public bool IsComponent => Labels.Contains("component");

    public bool InProgress => Open && Labels.Contains("in progress");

    public ComponentStatus Status => !Open ? ComponentStatus.Shipped
        : InProgress ? ComponentStatus.InProgress
        : ComponentStatus.Planned;
}

public enum ComponentStatus
{
    Shipped,
    InProgress,
    Planned,
}

public enum PhaseStage
{
    Done,
    Current,
    Next,
    Later,
}

// A phase's components, and its other open work such as fixes and docs.
public sealed record RoadmapPhase(string Title, PhaseStage Stage, IReadOnlyList<RoadmapIssue> Components,
    IReadOnlyList<RoadmapIssue> OtherOpen)
{
    public int Shipped => Components.Count(component => !component.Open);
}

public sealed record RoadmapSummary(IReadOnlyList<RoadmapIssue> InProgress, IReadOnlyList<RoadmapPhase> Phases,
    IReadOnlyList<RoadmapIssue> Unscheduled);

// The docs site's roadmap, from the issues docs/update-roadmap.sh wrote when the site was built.
public static class Roadmap
{
    public static RoadmapSummary Current { get; } = Summarize(Load());

    // The first phase with open work is current and the one after it is next. Phases sort by title, which keeps
    // them in order while they are numbered below 10.
    public static RoadmapSummary Summarize(IReadOnlyList<RoadmapIssue> issues)
    {
        issues = [.. issues.Where(issue => !issue.Dropped)];
        var phases = issues.Where(issue => issue.Phase is not null).GroupBy(issue => issue.Phase!)
            .OrderBy(phase => phase.Key, StringComparer.Ordinal).ToList();
        var current = phases.FindIndex(phase => phase.Any(issue => issue.Open));
        return new(
            [.. issues.Where(issue => issue.InProgress)],
            [.. phases.Select((phase, index) => new RoadmapPhase(phase.Key, StageOf(index, current),
                [.. phase.Where(issue => issue.IsComponent)],
                [.. phase.Where(issue => issue.Open && !issue.IsComponent)]))],
            [.. issues.Where(issue => issue.Phase is null && issue.IsComponent && issue.Open)]);
    }

    private static PhaseStage StageOf(int index, int current) => current switch
    {
        < 0 => PhaseStage.Done,
        _ when index < current => PhaseStage.Done,
        _ when index == current => PhaseStage.Current,
        _ when index == current + 1 => PhaseStage.Next,
        _ => PhaseStage.Later,
    };

    private static IReadOnlyList<RoadmapIssue> Load()
    {
        using var stream = typeof(Roadmap).Assembly.GetManifestResourceStream("Roadmap/roadmap.json")
            ?? throw new InvalidOperationException("roadmap.json isn't embedded.");
        return JsonSerializer.Deserialize(stream, RoadmapJson.Default.RoadmapIssueArray)!;
    }
}

// Generated at build time, so trimming the published site can't break reading the file.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RoadmapIssue[]))]
internal sealed partial class RoadmapJson : JsonSerializerContext;
