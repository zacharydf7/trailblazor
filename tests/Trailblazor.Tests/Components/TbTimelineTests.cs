using System.Globalization;
using Bunit;

namespace Trailblazor.Tests.Components;

public class TbTimelineTests : BunitContext
{
    private static readonly DateTimeOffset Booked = new(2026, 3, 3, 9, 30, 0, TimeSpan.FromHours(-5));

    public TbTimelineTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldListTheEventsInOrder_WhenGivenEvents()
    {
        var cut = Render<TbTimeline>(parameters => parameters.Add(timeline => timeline.AriaLabel, "Trip history")
            .AddChildContent<TbTimelineItem>(item => item.Add(i => i.Title, "Trip booked").Add(i => i.Time, Booked)
                .AddChildContent("Two seats on the ridge loop."))
            .AddChildContent<TbTimelineItem>(item => item.Add(i => i.Title, "Gear packed")));

        var list = cut.Find("ol[data-part=timeline]");

        Assert.Equal(("list", "Trip history"), (list.GetAttribute("role"), list.GetAttribute("aria-label")));
        Assert.Equal(["Trip booked", "Gear packed"],
            cut.FindAll("[data-part=title]").Select(title => title.TextContent));
        Assert.Equal("Two seats on the ridge loop.", cut.Find("[data-part=content]").TextContent);
        Assert.All(cut.FindAll("[data-part=dot]"),
            dot => Assert.Equal("true", dot.ParentElement!.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Render_ShouldShowTheTimeInTheCurrentCultureForPeopleAndInIsoForMachines_WhenGivenATime()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = Render<TbTimeline>(parameters => parameters
                .AddChildContent<TbTimelineItem>(item => item.Add(i => i.Time, Booked))
                .AddChildContent<TbTimelineItem>(item => item.Add(i => i.Time, Booked)
                    .Add(i => i.TimeFormat, "d. MMMM")));

            var times = cut.FindAll("time[data-part=time]");

            Assert.Equal("2026-03-03T09:30:00.000-05:00", times[0].GetAttribute("datetime"));
            Assert.Equal(["03.03.2026 09:30", "3. März"], times.Select(time => time.TextContent));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Render_ShouldColorTheDotAndShowItsIcon_WhenGivenAToneAndAnIcon()
    {
        var cut = Render<TbTimeline>(parameters => parameters.AddChildContent<TbTimelineItem>(item => item
            .Add(i => i.Title, "Trip booked").Add(i => i.Tone, Tone.Success).Add(i => i.Icon, BuiltInIcons.Check)
            .Add(i => i.Class, "booked").Add(i => i.Style, "color: teal").AddUnmatched("data-kind", "booking")));

        var item = cut.Find("li[data-part=timeline-item]");

        Assert.Equal(["tb-timeline__item", "tb-timeline__item--success", "tb-timeline__item--icon", "booked"],
            item.ClassList);
        Assert.Equal(("color: teal", "booking"), (item.GetAttribute("style"), item.GetAttribute("data-kind")));
        Assert.NotNull(cut.Find("[data-part=dot] svg"));
    }

    [Fact]
    public void Render_ShouldLeaveOutTheTimeAndTitle_WhenTheyAreNotGiven()
    {
        var cut = Render<TbTimeline>(parameters => parameters.AddChildContent<TbTimelineItem>(item => item
            .AddChildContent("Weather checked.")));

        Assert.Empty(cut.FindAll("[data-part=time], [data-part=title]"));
        Assert.Equal("Weather checked.", cut.Find("[data-part=content]").TextContent);
    }
}
