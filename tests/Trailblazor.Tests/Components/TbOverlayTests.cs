using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbOverlayTests : BunitContext
{
    public TbOverlayTests()
    {
        this.AddTrailblazor();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_ShouldBlockWhatItCoversUntilHidden_WhenBlocking(bool contained)
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/overlay.js").SetupModule("blockBehind", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        var cut = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.Blocking, true).Add(overlay => overlay.Contained, contained));
        Assert.Equal(contained, JSInterop.Invocations["blockBehind"].Single().Arguments[1]);
        Assert.Empty(handle.Invocations["dispose"]);

        cut.Render(parameters => parameters.Add(overlay => overlay.Visible, false));

        Assert.Single(handle.Invocations["dispose"]);
    }

    [Fact]
    public async Task Dispose_ShouldUnblockThePage_WhenRemovedWhileBlocking()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/overlay.js").SetupModule("blockBehind", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.Blocking, true));

        await DisposeComponentsAsync();

        Assert.Single(handle.Invocations["dispose"]);
    }

    [Fact]
    public void Render_ShouldLeaveThePageAlone_WhenNotBlocking()
    {
        Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true));

        Assert.Empty(JSInterop.Invocations["blockBehind"]);
    }

    [Fact]
    public void Render_ShouldRenderNothing_WhenHidden()
    {
        var cut = Render<TbOverlay>(parameters => parameters.AddChildContent("Loading"));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_ShouldCoverThePageAndCenterItsContent_WhenVisible()
    {
        var cut = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.LockScroll, true).Add(overlay => overlay.Blur, true).AddChildContent("Loading"));

        var overlay = cut.Find("[data-part=overlay]");
        Assert.Equal(["tb-overlay", "tb-overlay--fixed", "tb-overlay--blur", "tb-overlay--lock-scroll"],
            overlay.ClassList);
        Assert.Equal("true", cut.Find("[data-part=scrim]").GetAttribute("aria-hidden"));
        Assert.Equal("Loading", cut.Find("[data-part=content]").TextContent);
    }

    [Fact]
    public void Render_ShouldCoverItsRegionWithoutLockingThePage_WhenContained()
    {
        var cut = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.Contained, true).Add(overlay => overlay.LockScroll, true));

        Assert.Equal(["tb-overlay", "tb-overlay--contained"], cut.Find("[data-part=overlay]").ClassList);
        Assert.Empty(cut.FindAll("[data-part=content]"));
    }

    [Fact]
    public void Click_ShouldCloseAndSayWhy_WhenCloseOnClickIsSet()
    {
        bool? visible = null;
        var clicks = 0;
        var cut = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.CloseOnClick, true).Add(overlay => overlay.OnClick, () => clicks++)
            .Add(overlay => overlay.VisibleChanged, (bool value) => visible = value));

        cut.Find("[data-part=scrim]").Click();

        Assert.Equal((false, 1), (visible, clicks));
        Assert.Empty(cut.FindAll("[data-part=overlay]"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldGiveTheOverlayALaterOpenOrder_WhenItIsClosedAndShownAgain(bool byClick)
    {
        var cut = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.CloseOnClick, true));
        var other = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true));
        if (byClick)
        {
            cut.Find("[data-part=scrim]").Click();
        }
        else
        {
            cut.Render(parameters => parameters.Add(overlay => overlay.Visible, false));
        }

        cut.Render(parameters => parameters.Add(overlay => overlay.Visible, true));

        Assert.True(long.Parse(cut.Find("[data-part=overlay]").GetAttribute("data-open-order")!)
            > long.Parse(other.Find("[data-part=overlay]").GetAttribute("data-open-order")!));
    }

    [Fact]
    public void Click_ShouldOnlyReportTheClick_WhenCloseOnClickIsOff()
    {
        bool? visible = null;
        var clicks = 0;
        var cut = Render<TbOverlay>(parameters => parameters.Add(overlay => overlay.Visible, true)
            .Add(overlay => overlay.OnClick, () => clicks++)
            .Add(overlay => overlay.VisibleChanged, (bool value) => visible = value));

        cut.Find("[data-part=scrim]").Click();

        Assert.Equal((null, 1), (visible, clicks));
        Assert.Single(cut.FindAll("[data-part=overlay]"));
    }
}
