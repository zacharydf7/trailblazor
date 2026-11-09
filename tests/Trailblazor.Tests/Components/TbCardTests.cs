using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbCardTests : BunitContext
{
    public TbCardTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldNameTheArticleByItsHeading_WhenGivenATitle()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.Title, "Ridge loop")
            .Add(card => card.Subtitle, "12 km").Add(card => card.Outlined, true).AddChildContent("A steady climb."));

        var card = cut.Find("[data-part=card]");
        var title = cut.Find("[data-part=title]");

        Assert.Equal("ARTICLE", card.TagName);
        Assert.Equal(["tb-surface", "tb-surface--low", "tb-surface--outlined", "tb-card"], card.ClassList);
        Assert.Equal(("H3", "Ridge loop"), (title.TagName, title.TextContent));
        Assert.Equal(title.Id, card.GetAttribute("aria-labelledby"));
        Assert.Equal("12 km", cut.Find("[data-part=subtitle]").TextContent);
        Assert.Equal("A steady climb.", cut.Find("[data-part=content]").TextContent);
    }

    [Fact]
    public void Render_ShouldUseTheGivenHeadingLevel_WhenOneIsSet()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.Title, "Ridge loop")
            .Add(card => card.HeadingLevel, 2));

        Assert.Equal("H2", cut.Find("[data-part=title]").TagName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Render_ShouldThrow_WhenTheHeadingLevelIsOutOfRange(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbCard>(parameters => parameters
            .Add(card => card.Title, "Ridge loop").Add(card => card.HeadingLevel, level)));
    }

    [Fact]
    public void Render_ShouldLeaveTheCardUnnamed_WhenItIsAListItem()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.Title, "Ridge loop")
            .Add(card => card.As, LayoutElement.Li));

        var card = cut.Find("[data-part=card]");

        Assert.Equal(("LI", null), (card.TagName, card.GetAttribute("aria-labelledby")));
    }

    [Fact]
    public void Render_ShouldPutEachSlotInItsPart_WhenTheyAreGiven()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.Title, "Ridge loop")
            .Add(card => card.Start, "avatar").Add(card => card.End, "menu").Add(card => card.Media, "photo")
            .Add(card => card.Actions, "buttons"));

        Assert.Equal(["avatar", "menu", "photo", "buttons"],
            new[] { "start", "end", "media", "actions" }.Select(part => cut.Find($"[data-part={part}]").TextContent));
    }

    [Fact]
    public void Render_ShouldLeaveOutTheHeaderAndOtherParts_WhenOnlyContentIsGiven()
    {
        var cut = Render<TbCard>(parameters => parameters.AddChildContent("A steady climb."));

        Assert.Equal(["content"], cut.FindAll("[data-part]").Skip(1).Select(part => part.GetAttribute("data-part")));
        Assert.Null(cut.Find("[data-part=card]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldShowTheSubtitle_WhenThereIsNoTitle()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.Subtitle, "12 km"));

        Assert.Equal("12 km", cut.Find("[data-part=header] [data-part=subtitle]").TextContent);
    }

    [Fact]
    public void Render_ShouldKeepTheAppsName_WhenTheCardHasNoTitle()
    {
        var cut = Render<TbCard>(parameters => parameters.AddUnmatched("aria-labelledby", "my-heading"));

        Assert.Equal("my-heading", cut.Find("[data-part=card]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldMakeTheTitleTheCardsLink_WhenGivenAnAddress()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.Title, "Ridge loop")
            .Add(card => card.Href, "trails/ridge-loop"));

        var link = cut.Find("[data-part=title] > a");

        Assert.Equal(("link", "trails/ridge-loop", "Ridge loop"),
            (link.GetAttribute("data-part"), link.GetAttribute("href"), link.TextContent));
        Assert.Contains("tb-card--link", cut.Find("[data-part=card]").ClassList);
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasAnAddressButNoTitle()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbCard>(parameters => parameters
            .Add(card => card.Href, "trails/ridge-loop")));

        Assert.Contains(nameof(TbCard.Title), thrown.Message);
    }
}
