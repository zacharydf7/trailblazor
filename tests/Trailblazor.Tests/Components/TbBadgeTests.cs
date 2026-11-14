using Bunit;

namespace Trailblazor.Tests.Components;

public class TbBadgeTests : BunitContext
{
    [Fact]
    public void Render_ShouldShowTheCountInTheText_WhenItHasNoChildContent()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 2)
            .Add(badge => badge.Tone, Tone.Danger).Add(badge => badge.Class, "my-badge"));

        var host = cut.Find(".tb-badge-host");
        Assert.Equal(["tb-badge-host", "my-badge"], host.ClassList);
        var badge = cut.Find("[data-part=badge]");
        Assert.Equal(["tb-badge", "tb-badge--danger"], badge.ClassList);
        Assert.Equal("2", badge.TextContent.Trim());
    }

    [Theory]
    [InlineData(99, "99")]
    [InlineData(100, "99+")]
    public void Render_ShouldCapTheCountAtMax_WhenGivenACountAroundIt(int count, string shown)
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, count)
            .Add(badge => badge.Max, 99));

        Assert.Equal(shown, cut.Find("[data-part=badge]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldMarkTheShownNumberApartFromTheLabel_WhenBothAreGiven()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 120)
            .Add(badge => badge.Label, "120 unread"));

        Assert.Equal("99+", cut.Find("[data-part=count]").TextContent.Trim());
        Assert.Equal("120", cut.Find("[data-part=badge]").GetAttribute("data-count"));
    }

    [Fact]
    public void Render_ShouldSitOnTheChildAndSpeakTheLabelInsteadOfTheNumber_WhenBothAreGiven()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 3)
            .Add(badge => badge.Label, "3 unsaved changes")
            .AddChildContent("<button>Review changes</button>"));

        Assert.Contains("tb-badge-host--overlap", cut.Find(".tb-badge-host").ClassList);
        Assert.Null(cut.Find("[data-part=badge]").GetAttribute("aria-hidden"));
        Assert.Equal("true", cut.Find("[data-part=badge] > span:not(.tb-visually-hidden)").GetAttribute("aria-hidden"));
        Assert.Equal("3 unsaved changes", cut.Find("[data-part=badge] .tb-visually-hidden").TextContent);
    }

    [Fact]
    public void Render_ShouldHideTheBadgeFromScreenReaders_WhenItSitsOnAChildWithoutALabel()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 120)
            .AddChildContent("<button aria-label=\"Messages, 120 unread\">Messages</button>"));

        Assert.Equal("true", cut.Find("[data-part=badge]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Render_ShouldLetScreenReadersReadTheNumber_WhenTheBadgeSitsInTheText()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 2));

        Assert.Null(cut.Find("[data-part=badge]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Render_ShouldShowAnEmptyDot_WhenThereIsNoCount()
    {
        var cut = Render<TbBadge>(parameters => parameters.AddChildContent("<button>Filters</button>"));

        var badge = cut.Find("[data-part=badge]");
        Assert.Contains("tb-badge--dot", badge.ClassList);
        Assert.Empty(badge.TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldHideOnlyTheBadge_WhenVisibleIsOff()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 5)
            .Add(badge => badge.Visible, false).AddChildContent("<button>Messages</button>"));

        Assert.Empty(cut.FindAll("[data-part=badge]"));
        Assert.Equal("Messages", cut.Find("button").TextContent);
    }

    [Fact]
    public void Render_ShouldHideTheBadge_WhenTheCountIsZero()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 0));

        Assert.Empty(cut.FindAll("[data-part=badge]"));
    }

    [Fact]
    public void Render_ShouldShowAZero_WhenAskedToShowZero()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Count, 0)
            .Add(badge => badge.ShowZero, true));

        Assert.Equal("0", cut.Find("[data-part=badge]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldPlaceTheBadgeAndSetItsOffset_WhenItSitsOnContent()
    {
        var cut = Render<TbBadge>(parameters => parameters.Add(badge => badge.Placement, BadgePlacement.BottomStart)
            .Add(badge => badge.Overlap, BadgeOverlap.Circular).Add(badge => badge.Offset, "4px")
            .AddChildContent("<img alt=\"Ana\">"));

        var badge = cut.Find("[data-part=badge]");
        Assert.Contains("tb-badge--placement-bottomstart", badge.ClassList);
        Assert.Contains("tb-badge--circular", badge.ClassList);
        Assert.Equal("--tb-badge-offset: 4px", cut.Find(".tb-badge-host").GetAttribute("style"));
    }
}
