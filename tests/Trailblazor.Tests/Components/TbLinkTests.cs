using Bunit;

namespace Trailblazor.Tests.Components;

public class TbLinkTests : BunitContext
{
    public TbLinkTests() => this.AddTrailblazor();

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

    [Theory]
    [InlineData(true, "")]
    [InlineData(false, null)]
    public void Render_ShouldMarkTheAnchorForAWholePageLoadOnlyWithForceLoad_WhenGivenForceLoad(bool forceLoad,
        string? mark)
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "account/sign-in")
            .Add(link => link.ForceLoad, forceLoad).AddChildContent("Sign in"));

        Assert.Equal(mark, cut.Find("a").GetAttribute("data-tb-force-load"));
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

    [Fact]
    public void Render_ShouldSayItOpensANewTab_WhenNewTabIsSetWithoutAHint()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "https://example.com")
            .Add(link => link.NewTab, true).AddChildContent("Example"));

        Assert.Equal("(opens in a new tab)", cut.Find("a .tb-visually-hidden").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldDropTheAddressAndSayItIsDisabled_WhenADisabledLinkHasAnHref()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "guides/theming")
            .Add(link => link.Disabled, true).AddUnmatched("tabindex", "0").AddChildContent("Theming"));

        var link = cut.Find("a");
        Assert.False(link.HasAttribute("href"));
        Assert.False(link.HasAttribute("tabindex"));
        Assert.Equal(("link", "true"), (link.GetAttribute("role"), link.GetAttribute("aria-disabled")));
    }

    [Fact]
    public void Render_ShouldDisableTheButton_WhenADisabledLinkHasNoHref()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Disabled, true).AddChildContent("Run"));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("guides/theming", "a")]
    [InlineData(null, "button")]
    public void Click_ShouldNotRunOnClick_WhenDisabled(string? href, string element)
    {
        var ran = false;
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, href)
            .Add(link => link.Disabled, true).Add(link => link.OnClick, () => ran = true).AddChildContent("Run"));

        cut.Find(element).Click();

        Assert.False(ran);
    }

    [Fact]
    public async Task FocusAsync_ShouldFocusTheLink_WhenCalled()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "guides/theming")
            .AddChildContent("Theming"));

        await cut.InvokeAsync(() => cut.Instance.FocusAsync());

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(cut.Find("a"));
    }
}
