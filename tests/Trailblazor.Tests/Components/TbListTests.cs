using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbListTests : BunitContext
{
    [Fact]
    public void Render_ShouldRenderAnOrderedList_WhenOrdered()
    {
        var cut = Render<TbList>(parameters => parameters.Add(list => list.Ordered, true));

        Assert.Equal("list", cut.Find("ol").GetAttribute("role"));
    }

    [Fact]
    public void Render_ShouldPutTheTextAndDescriptionInTheButton_WhenGivenBoth()
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Pine Hollow")
            .Add(item => item.Description, "4 sites").Add(item => item.OnClick, () => { }));

        Assert.Equal("Pine Hollow", cut.Find("button > * > [data-part=text]").TextContent);
        Assert.Equal("4 sites", cut.Find("button > * > [data-part=description]").TextContent);
    }

    [Fact]
    public void Render_ShouldKeepTheEndOutsideTheAction_WhenTheItemIsALink()
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Guides")
            .Add(item => item.Href, "guides").Add(item => item.End, (RenderFragment)(b => b.AddContent(0, "3"))));

        Assert.Equal("guides", cut.Find("a[data-part=action]").GetAttribute("href"));
        Assert.Empty(cut.FindAll("a [data-part=end]"));
        Assert.Equal("3", cut.Find("li > [data-part=end]").TextContent);
    }

    [Fact]
    public void Click_ShouldRaiseOnClick_WhenTheItemIsChosen()
    {
        var clicks = 0;
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Pine Hollow")
            .Add(item => item.OnClick, () => clicks++));

        cut.Find("button[data-part=action]").Click();

        Assert.Equal(1, clicks);
    }

    [Theory]
    [InlineData("guides", "page")]
    [InlineData(null, "true")]
    public void Render_ShouldMarkTheActionCurrent_WhenSelected(string? href, string current)
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Pine Hollow")
            .Add(item => item.Href, href).Add(item => item.OnClick, () => { }).Add(item => item.Selected, true));

        Assert.Equal(current, cut.Find("[data-part=action]").GetAttribute("aria-current"));
        Assert.False(cut.Find("li").HasAttribute("aria-current"));
    }

    [Fact]
    public void Render_ShouldMarkTheItemCurrent_WhenSelectedWithoutAnAction()
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Ridge loop")
            .Add(item => item.Selected, true));

        Assert.Equal("true", cut.Find("li").GetAttribute("aria-current"));
    }

    [Fact]
    public void Render_ShouldDropTheLink_WhenDisabled()
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Guides")
            .Add(item => item.Href, "guides").Add(item => item.Disabled, true));

        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("true", cut.Find("[data-part=content]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldTurnTheButtonOff_WhenDisabled()
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.ChildContent, "Pine Hollow")
            .Add(item => item.OnClick, () => { }).Add(item => item.Disabled, true));

        Assert.True(cut.Find("button[data-part=action]").HasAttribute("disabled"));
    }
}
