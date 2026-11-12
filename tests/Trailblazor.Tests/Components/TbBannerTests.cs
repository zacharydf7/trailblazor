using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Components;

public class TbBannerTests : BunitContext
{
    public TbBannerTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldBeARegionNamedByItsTitle_WhenGivenATitle()
    {
        var cut = Render<TbBanner>(parameters => parameters.Add(banner => banner.Title, "Maintenance tonight")
            .Add(banner => banner.Tone, Tone.Warning).AddChildContent("Trail maps are offline."));

        var region = cut.Find("section");
        Assert.Equal(cut.Find("[data-part=title]").Id, region.GetAttribute("aria-labelledby"));
        Assert.Null(region.GetAttribute("aria-label"));
        Assert.Contains("tb-banner--warning", region.ClassList);
        Assert.Equal("Maintenance tonight Trail maps are offline.",
            string.Join(" ", cut.Find("[data-part=message]").TextContent.Split(' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
    }

    [Theory]
    [InlineData(null, "Notice")]
    [InlineData("Connection", "Connection")]
    public void Render_ShouldNameTheRegion_WhenItHasNoTitle(string? ariaLabel, string name)
    {
        var cut = Render<TbBanner>(parameters => parameters.Add(banner => banner.AriaLabel, ariaLabel)
            .AddChildContent("You're offline."));

        Assert.Equal(name, cut.Find("section").GetAttribute("aria-label"));
        Assert.Null(cut.Find("section").GetAttribute("aria-labelledby"));
    }

    [Theory]
    [InlineData(null, "Dismiss")]
    [InlineData("Hide this notice", "Hide this notice")]
    public async Task Close_ShouldRaiseOnClose_WhenItsButtonIsClicked(string? closeLabel, string name)
    {
        var closed = 0;
        var cut = Render<TbBanner>(parameters => parameters.AddChildContent("You're offline.")
            .Add(banner => banner.CloseLabel, closeLabel)
            .Add(banner => banner.OnClose, () => closed++));

        var close = cut.Find("[data-part=close]");
        Assert.Equal(name, close.GetAttribute("aria-label"));
        await close.ClickAsync(new());

        Assert.Equal(1, closed);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheIconAndCloseButton_WhenAskedTo()
    {
        var cut = Render<TbBanner>(parameters => parameters.Add(banner => banner.ShowIcon, false)
            .AddChildContent("Published."));

        Assert.Empty(cut.FindAll("[data-part=icon], [data-part=close], [data-part=actions]"));
    }
}
