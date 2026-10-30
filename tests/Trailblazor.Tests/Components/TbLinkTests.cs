using Bunit;

namespace Trailblazor.Tests.Components;

public class TbLinkTests : BunitContext
{
    [Fact]
    public void Render_ShouldRenderAnAnchorToTheAddress_WhenHrefIsSet()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "guides/theming")
            .Add(link => link.Class, "my-link").AddChildContent("Theming"));

        var anchor = cut.Find("a");
        Assert.Equal("guides/theming", anchor.GetAttribute("href"));
        Assert.Equal(["tb-link", "my-link"], anchor.ClassList);
        Assert.Null(anchor.GetAttribute("target"));
        Assert.Equal("Theming", anchor.TextContent.Trim());
    }

    [Fact]
    public void Click_ShouldRunOnClickFromAButton_WhenHrefIsMissing()
    {
        var clicks = 0;
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.OnClick, () => clicks++)
            .AddChildContent("Copy the ID"));

        cut.Find("button").Click();

        Assert.Equal("button", cut.Find("button").GetAttribute("type"));
        Assert.Contains("tb-link--button", cut.Find("button").ClassList);
        Assert.Empty(cut.FindAll("a"));
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Render_ShouldKeepTheConsumersRelAndTarget_WhenNewTabIsOff()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "https://example.com")
            .AddUnmatched("rel", "nofollow").AddUnmatched("target", "_self"));

        Assert.Equal("nofollow", cut.Find("a").GetAttribute("rel"));
        Assert.Equal("_self", cut.Find("a").GetAttribute("target"));
    }

    [Fact]
    public void Render_ShouldAddToTheConsumersRel_WhenNewTabIsSet()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "https://example.com")
            .Add(link => link.NewTab, true).AddUnmatched("rel", "nofollow"));

        Assert.Equal("nofollow noopener noreferrer", cut.Find("a").GetAttribute("rel"));
    }

    [Fact]
    public void Render_ShouldOpenANewTabWithoutTheOpenerAndSaySo_WhenNewTabIsSet()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "https://example.com")
            .Add(link => link.NewTab, true).Add(link => link.NewTabHint, "new tab").AddChildContent("Example"));

        var anchor = cut.Find("a");
        Assert.Equal("_blank", anchor.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", anchor.GetAttribute("rel"));
        Assert.Equal("(new tab)", cut.Find("a .tb-visually-hidden").TextContent.Trim());
    }
}
