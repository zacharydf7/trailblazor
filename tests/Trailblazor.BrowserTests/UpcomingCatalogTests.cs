using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class UpcomingCatalogTests
{
    private static RoadmapIssue Issue(int number, string? phase, bool open = true, string? label = null,
        bool dropped = false) =>
        new(number, open, phase, label is null ? [] : [label], dropped);

    private static UpcomingComponent Component(string title, int issue) => new(title, ExampleCategory.Inputs, issue);

    [Fact]
    public void Arrange_ShouldMarkTheNextPhaseComingSoonAndLaterOnesPlanned_WhenAnEarlierPhaseIsUnderWay()
    {
        var entries = UpcomingCatalog.Arrange([Component("Later", 3), Component("Next", 2), Component("Now", 1)],
            [Issue(1, "Phase 2"), Issue(2, "Phase 3"), Issue(3, "Phase 4"), Issue(4, "Phase 1", open: false)]);

        Assert.Equal([("Next", UpcomingStage.ComingSoon), ("Now", UpcomingStage.ComingSoon),
            ("Later", UpcomingStage.Planned)], entries.Select(entry => (entry.Component.Title, entry.Stage)));
    }

    [Fact]
    public void Arrange_ShouldMarkAComponentComingSoon_WhenItsIssueIsInProgress()
    {
        var entries = UpcomingCatalog.Arrange([Component("Started early", 3)],
            [Issue(1, "Phase 2"), Issue(3, "Phase 5", label: "in progress")]);

        Assert.Equal(UpcomingStage.ComingSoon, Assert.Single(entries).Stage);
    }

    [Fact]
    public void Arrange_ShouldLeaveOutAComponent_WhenItsIssueHasShippedOrWasDropped()
    {
        var entries = UpcomingCatalog.Arrange([Component("Shipped", 1), Component("Dropped", 2), Component("Open", 3)],
            [Issue(1, "Phase 3", open: false), Issue(2, "Phase 3", open: false, dropped: true), Issue(3, "Phase 3")]);

        Assert.Equal(["Open"], entries.Select(entry => entry.Component.Title));
    }

    [Fact]
    public void Arrange_ShouldPlanAComponent_WhenItsIssueHasNoPhase()
    {
        var entries = UpcomingCatalog.Arrange([Component("Unscheduled", 2)], [Issue(1, "Phase 2"), Issue(2, null)]);

        Assert.Equal(UpcomingStage.Planned, Assert.Single(entries).Stage);
    }

    [Fact]
    public void Arrange_ShouldLeaveOutAComponent_WhenItsIssueIsMissingFromTheData()
    {
        var entries = UpcomingCatalog.Arrange([Component("Gone", 9), Component("Open", 1)], [Issue(1, "Phase 3")]);

        Assert.Equal(["Open"], entries.Select(entry => entry.Component.Title));
    }

    [Fact]
    public void All_ShouldTrackEachComponentByAnIssueInTheData_WhenCalled()
    {
        var numbers = UpcomingCatalog.Issues.Select(issue => issue.Number).ToHashSet();

        Assert.All(UpcomingCatalog.All, component => Assert.Contains(component.Issue, numbers));
    }

    [Fact]
    public void All_ShouldNameEachComponentOnce_WhenCalled()
    {
        var titles = UpcomingCatalog.All.Select(component => component.Title).ToList();

        Assert.Equal(titles.Count, titles.Distinct().Count());
        Assert.DoesNotContain(titles, title => ExampleCatalog.All.Any(example => example.Title == title));
    }
}
