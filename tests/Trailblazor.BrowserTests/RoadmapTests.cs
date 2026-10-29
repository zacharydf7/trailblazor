using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class RoadmapTests
{
    private static RoadmapIssue Component(int number, string? phase, bool open = true, string? label = null) =>
        new(number, $"Component {number}", open, phase, label is null ? ["component"] : ["component", label]);

    private static RoadmapIssue Other(int number, string? phase, bool open = true) =>
        new(number, $"Fix {number}", open, phase, []);

    [Fact]
    public void Summarize_ShouldMakeTheFirstPhaseWithOpenWorkCurrent_WhenEarlierPhasesAreDone()
    {
        var roadmap = Roadmap.Summarize([Component(1, "Phase 3"), Component(2, "Phase 1", open: false),
            Other(3, "Phase 2"), Component(4, "Phase 4"), Component(5, "Phase 2", open: false)]);

        Assert.Equal([("Phase 1", PhaseStage.Done), ("Phase 2", PhaseStage.Current), ("Phase 3", PhaseStage.Next),
            ("Phase 4", PhaseStage.Later)], roadmap.Phases.Select(phase => (phase.Title, phase.Stage)));
    }

    [Fact]
    public void Summarize_ShouldMarkEveryPhaseDone_WhenNothingIsOpen()
    {
        var roadmap = Roadmap.Summarize([Component(1, "Phase 1", open: false), Component(2, "Phase 2", open: false)]);

        Assert.All(roadmap.Phases, phase => Assert.Equal(PhaseStage.Done, phase.Stage));
    }

    [Fact]
    public void Summarize_ShouldGiveEachComponentItsStatus_WhenShippedInProgressOrPlanned()
    {
        var roadmap = Roadmap.Summarize([Component(1, "Phase 1", open: false),
            Component(2, "Phase 1", label: "in progress"), Component(3, "Phase 1")]);

        var phase = Assert.Single(roadmap.Phases);
        Assert.Equal([ComponentStatus.Shipped, ComponentStatus.InProgress, ComponentStatus.Planned],
            phase.Components.Select(component => component.Status));
        Assert.Equal(1, phase.Shipped);
    }

    [Fact]
    public void Summarize_ShouldListOnlyOpenWorkBesideTheComponents_WhenAPhaseHasOtherIssues()
    {
        var roadmap = Roadmap.Summarize([Component(1, "Phase 1"), Other(2, "Phase 1"), Other(3, "Phase 1", false)]);

        var phase = Assert.Single(roadmap.Phases);
        Assert.Equal([1], phase.Components.Select(component => component.Number));
        Assert.Equal([2], phase.OtherOpen.Select(issue => issue.Number));
    }

    [Fact]
    public void Summarize_ShouldListEverythingInProgressAndTheOpenComponentsWithoutAPhase_WhenGivenThem()
    {
        var roadmap = Roadmap.Summarize([Component(1, "Phase 1", label: "in progress"),
            new(2, "Docs", true, null, ["in progress"]), Component(3, null), Component(4, null, open: false)]);

        Assert.Equal([1, 2], roadmap.InProgress.Select(issue => issue.Number));
        Assert.Equal([3], roadmap.Unscheduled.Select(issue => issue.Number));
    }

    [Fact]
    public void Summarize_ShouldLeaveOutDroppedIssues_WhenClosedWithoutShipping()
    {
        var dropped = Component(2, "Phase 1", open: false) with { Dropped = true };

        var roadmap = Roadmap.Summarize([Component(1, "Phase 1", open: false), dropped]);

        var phase = Assert.Single(roadmap.Phases);
        Assert.Equal([1], phase.Components.Select(component => component.Number));
        Assert.Equal(1, phase.Shipped);
    }

    [Fact]
    public void Current_ShouldReadTheIssuesEmbeddedInTheSite_WhenCalled()
    {
        var roadmap = Roadmap.Current;

        Assert.NotEmpty(roadmap.Phases);
        Assert.All(roadmap.Phases, phase => Assert.StartsWith("Phase ", phase.Title));
    }
}
