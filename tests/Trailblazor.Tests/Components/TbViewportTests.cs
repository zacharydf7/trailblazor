using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbViewportTests : BunitContext
{
    private readonly BunitJSModuleInterop _handle;

    public TbViewportTests()
    {
        this.AddTrailblazor();
        _handle = SetUpScript();
    }

    private List<double> Zooms { get; } = [];

    private List<ViewportFit> Fits { get; } = [];

    private IRenderedComponent<TbViewport> RenderMap(
        Action<ComponentParameterCollectionBuilder<TbViewport>>? configure = null) =>
        Render<TbViewport>(parameters =>
        {
            parameters.Add(viewport => viewport.Label, "Trail map")
                .Add(viewport => viewport.ZoomChanged, (double zoom) => Zooms.Add(zoom))
                .Add(viewport => viewport.FitChanged, (ViewportFit fit) => Fits.Add(fit))
                .AddChildContent("<img src=\"map.png\" alt=\"Map of the ridge trails\">");
            configure?.Invoke(parameters);
        });

    // The script's handle, which answers each update with the zoom it's given, or a fit's zoom of 1.5.
    private BunitJSModuleInterop SetUpScript()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/viewport.js").SetupModule("attachViewport", _ => true);
        handle.Setup<double>("update", call => (string)call.Arguments[1]! != "None").SetResult(1.5);
        foreach (var zoom in new[] { 0.5, 1, 1.2, 1.25, 2, 8 })
        {
            handle.Setup<double>("update", call => (string)call.Arguments[1]! == "None" && (double)call.Arguments[0]! == zoom)
                .SetResult(zoom);
        }

        return handle;
    }

    private static ViewportReport ReportOf(BunitJSInterop interop) =>
        ((DotNetObjectReference<ViewportReport>)interop.Invocations["attachViewport"].Single().Arguments[1]!).Value;

    [Fact]
    public void Render_ShouldShowTheContentInANamedBoxUnderItsControls_WhenGivenALabel()
    {
        var cut = RenderMap();

        var surface = cut.Find("[data-part=surface]");
        Assert.Equal(("region", "Trail map", "0"), (surface.GetAttribute("role"), surface.GetAttribute("aria-label"),
            surface.GetAttribute("tabindex")));
        Assert.NotNull(surface.QuerySelector("[data-part=content] img"));
        Assert.Equal(["Zoom out", "Zoom 100%", "Zoom in"],
            cut.FindAll("[data-part=controls] [aria-label]").Select(control => control.GetAttribute("aria-label")));
        Assert.Equal(("1", "None"), (cut.Find("[data-part=viewport]").GetAttribute("data-zoom"),
            cut.Find("[data-part=viewport]").GetAttribute("data-fit")));
        Assert.Contains("tb-viewport--drag", cut.Find("[data-part=viewport]").ClassList);
    }

    [Fact]
    public void Render_ShouldSetTheZoomAndHeightOnTheContentsStyle_WhenGivenThem()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Zoom, 2)
            .Add(viewport => viewport.Height, "30rem"));

        Assert.Equal("--tb-viewport-zoom: 2; --tb-viewport-height: 30rem;",
            cut.Find("[data-part=viewport]").GetAttribute("style"));
        Assert.Equal("Zoom 200%", cut.Find("[data-part=zoom-menu] [aria-haspopup=menu]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldKeepTheZoomWithinItsLimits_WhenGivenOneOutside()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Zoom, 20));

        Assert.Equal(8, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal("true", cut.Find("[data-part=zoom-in]").GetAttribute("aria-disabled"));
        Assert.Null(cut.Find("[data-part=zoom-out]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Zoom_ShouldStayWithinItsLimits_WhenTheParentChangesItToOneOutside()
    {
        var cut = RenderMap();

        cut.Render(parameters => parameters.Add(viewport => viewport.Zoom, 20));

        Assert.Equal(8, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal(8d, _handle.Invocations["update"].Last().Arguments[0]);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheControlsAndTheDrag_WhenTurnedOff()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.ShowControls, false)
            .Add(viewport => viewport.DragToPan, false));

        Assert.Empty(cut.FindAll("[data-part=controls]"));
        Assert.DoesNotContain("tb-viewport--drag", cut.Find("[data-part=viewport]").ClassList);
    }

    [Fact]
    public async Task ZoomInAsync_ShouldZoomByTheStepAndSaySo_WhenItsButtonIsClicked()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderMap();

        await cut.Tb().Viewport("Trail map").ZoomInAsync();

        Assert.Equal(1.25, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal([1.25], Zooms);
        announcer.WaitForAssertion(() => Assert.Equal("Zoom 125%", announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task ZoomInAsync_ShouldStepFromTheLastClick_WhenClickedAgainBeforeTheScriptAnswers()
    {
        var first = _handle.Setup<double>("update", call => (double)call.Arguments[0]! == 1.25);
        _handle.Setup<double>("update", call => (double)call.Arguments[0]! == 1.5625).SetResult(1.5625);
        var cut = RenderMap();

        var clicks = new[] { cut.Tb().Viewport("Trail map").ZoomInAsync(), cut.Tb().Viewport("Trail map").ZoomInAsync() };
        first.SetResult(1.25);
        await Task.WhenAll(clicks);

        Assert.Equal(1.5625, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal([1.25, 1.5625], Zooms);
    }

    [Fact]
    public async Task FitAsync_ShouldLeaveALaterZoomAlone_WhenTheScriptAnswersTheFitAfterIt()
    {
        var fit = _handle.Setup<double>("update", call => (string)call.Arguments[1]! == "Width");
        var cut = RenderMap();

        var picked = cut.Tb().Viewport("Trail map").FitAsync(ViewportFit.Width);
        await cut.Tb().Viewport("Trail map").ZoomInAsync();
        fit.SetResult(1.5);
        await picked;

        Assert.Equal((1.25, ViewportFit.None), (cut.Tb().Viewport("Trail map").Zoom, cut.Tb().Viewport("Trail map").Fit));
        Assert.Equal([1.25], Zooms);
    }

    [Fact]
    public async Task Fit_ShouldLeaveALaterZoomAlone_WhenTheScriptAnswersTheParentsFitAfterIt()
    {
        var cut = RenderMap();
        var fit = _handle.Setup<double>("update", call => (string)call.Arguments[1]! == "Width");

        cut.Render(parameters => parameters.Add(viewport => viewport.Fit, ViewportFit.Width));
        await cut.Tb().Viewport("Trail map").ZoomInAsync();
        await cut.InvokeAsync(() => fit.SetResult(1.5));

        Assert.Equal((1.25, ViewportFit.None), (cut.Tb().Viewport("Trail map").Zoom, cut.Tb().Viewport("Trail map").Fit));
        Assert.Equal([1.25], Zooms);
    }

    [Fact]
    public async Task ZoomInAsync_ShouldThrow_WhenTheZoomIsAtItsLargest()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Zoom, 8));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Viewport("Trail map").ZoomInAsync());
    }

    [Fact]
    public async Task ZoomOutAsync_ShouldDivideTheZoomByTheStepAndLeaveTheFit_WhenItsButtonIsClicked()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Fit, ViewportFit.Page));

        await cut.Tb().Viewport("Trail map").ZoomOutAsync();

        Assert.Equal(1.2, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal([1.5, 1.2], Zooms);
        Assert.Equal([ViewportFit.None], Fits);
    }

    [Fact]
    public async Task FitAsync_ShouldTakeTheZoomTheScriptWorksOut_WhenAFitIsPicked()
    {
        var cut = RenderMap();

        await cut.Tb().Viewport("Trail map").FitAsync(ViewportFit.Width);

        Assert.Equal((1.5, ViewportFit.Width), (cut.Tb().Viewport("Trail map").Zoom, cut.Tb().Viewport("Trail map").Fit));
        Assert.Equal([1.5], Zooms);
        Assert.Equal([ViewportFit.Width], Fits);
        Assert.Equal([1d, "Width", 0.1, 8d, 1.25, true], _handle.Invocations["update"].Last().Arguments);
    }

    [Fact]
    public async Task ActualSizeAsync_ShouldShowTheContentAt100Percent_WhenPickedFromTheMenu()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Zoom, 2));

        await cut.Tb().Viewport("Trail map").ActualSizeAsync();

        Assert.Equal(1, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal([1d], Zooms);
    }

    [Fact]
    public void Attach_ShouldSendTheFitAndRaiseItsZoom_WhenTheViewportStartsFitted()
    {

        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Fit, ViewportFit.Page)
            .Add(viewport => viewport.MaxZoom, 4).Add(viewport => viewport.DragToPan, false));

        Assert.Equal([1d, "Page", 0.1, 4d, 1.25, false], _handle.Invocations["update"].Single().Arguments);
        Assert.Equal(1.5, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Equal([1.5], Zooms);
        Assert.Empty(Fits);
    }

    [Fact]
    public void Zoom_ShouldBeSentToTheScript_WhenTheParentChangesIt()
    {
        var cut = RenderMap();

        cut.Render(parameters => parameters.Add(viewport => viewport.Zoom, 2));

        Assert.Equal(2d, _handle.Invocations["update"].Last().Arguments[0]);
        Assert.Equal(2, cut.Tb().Viewport("Trail map").Zoom);
        Assert.Empty(Zooms);
    }

    [Fact]
    public async Task Zoom_ShouldStayAsTheUserLeftIt_WhenTheParentRendersAgainWithoutChangingIt()
    {
        var cut = RenderMap();
        await cut.Tb().Viewport("Trail map").ZoomInAsync();

        cut.Render(parameters => parameters.Add(viewport => viewport.Label, "Trail map"));

        Assert.Equal(1.25, cut.Tb().Viewport("Trail map").Zoom);
    }

    [Fact]
    public async Task Report_ShouldTakeTheZoomAndSaySo_WhenTheKeysZoom()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Fit, ViewportFit.Width));
        Zooms.Clear();

        await cut.InvokeAsync(() => ReportOf(JSInterop).OnZoom(2, "None", "keyboard"));

        Assert.Equal((2, ViewportFit.None), (cut.Tb().Viewport("Trail map").Zoom, cut.Tb().Viewport("Trail map").Fit));
        Assert.Equal([2d], Zooms);
        Assert.Equal([ViewportFit.None], Fits);
        announcer.WaitForAssertion(() => Assert.Equal("Zoom 200%", announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task Report_ShouldTakeTheZoomQuietly_WhenAPointerOrAFitZooms()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Fit, ViewportFit.Page));

        await cut.InvokeAsync(() => ReportOf(JSInterop).OnZoom(0.5, "Page", "resize"));
        Assert.Equal(ViewportFit.Page, cut.Tb().Viewport("Trail map").Fit);
        await cut.InvokeAsync(() => ReportOf(JSInterop).OnZoom(2, "None", "pointer"));

        Assert.Equal([1.5, 0.5, 2], Zooms);
        Assert.Equal([ViewportFit.None], Fits);
        Assert.Empty(announcer.FindAll("[role=status] p"));
    }

    [Fact]
    public void Labels_ShouldReplaceTheControlsText_WhenGiven()
    {
        var cut = RenderMap(parameters => parameters.Add(viewport => viewport.Labels, new ViewportLabels
        {
            ZoomIn = "Closer", ZoomOut = "Farther", Zoom = "Scale {0}", FitWidth = "Across", FitPage = "Whole",
            ActualSize = "Real size",
        }));

        Assert.Equal(["Farther", "Scale 100%", "Closer"],
            cut.FindAll("[data-part=controls] [aria-label]").Select(control => control.GetAttribute("aria-label")));
        Assert.Equal(["Across", "Whole", "Real size"],
            cut.FindAll("[role=menuitemradio], [role=menuitem]").Select(item => item.TextContent.Trim()));
    }

    [Theory]
    [InlineData("", 0.1, 8, 1.25)]
    [InlineData("Map", 0, 8, 1.25)]
    [InlineData("Map", 2, 1, 1.25)]
    [InlineData("Map", 0.1, 8, 1)]
    public void Render_ShouldThrow_WhenTheLabelOrLimitsAreMissingOrWrong(string label, double min, double max,
        double step)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbViewport>(parameters => parameters
            .Add(viewport => viewport.Label, label).Add(viewport => viewport.MinZoom, min)
            .Add(viewport => viewport.MaxZoom, max).Add(viewport => viewport.ZoomStep, step)));
    }
}
