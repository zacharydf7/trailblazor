using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbLightboxTests : BunitContext
{
    private static readonly LightboxImage[] Photos =
    [
        new("/ridge.jpg", "Eagle Ridge at dawn") { Caption = "Eagle Ridge, 6 a.m." },
        new("/falls.jpg", "Cedar Falls in spring"),
        new("/lake.jpg", "Mirror Lake from the north shore"),
    ];

    private readonly List<int> _indexes = [];
    private readonly List<bool> _opens = [];

    public TbLightboxTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldShowTheImageAtIndexInAFullWindowDialog_WhenOpen()
    {
        var cut = RenderLightbox(index: 0);

        var dialog = cut.Find("dialog");
        var options = Assert.Single(Services.GetRequiredService<IDialogService>().OpenDialogs).Options;
        Assert.Contains("tb-lightbox", dialog.ClassList);
        Assert.Equal("Trail photos", dialog.QuerySelector("h2")!.TextContent);
        Assert.Equal((DialogSize.Full, DialogFocus.Dialog, "trail-photos"),
            (options.Size, options.InitialFocus, options.TelemetryName));
        Assert.Equal(("/ridge.jpg", "Eagle Ridge at dawn"),
            (cut.Find("[data-part=image]").GetAttribute("src"), cut.Find("[data-part=image]").GetAttribute("alt")));
        Assert.Equal("Eagle Ridge, 6 a.m.", cut.Tb().Lightbox().Caption);
        Assert.Equal("1 of 3", cut.Find("[data-part=position]").TextContent);
        Assert.Equal("Eagle Ridge at dawn, 1 of 3", cut.Find("[data-part=status]").TextContent);
        Assert.Equal("polite", cut.Find("[data-part=status]").GetAttribute("aria-live"));
    }

    [Fact]
    public async Task NextAsync_ShouldShowTheNextImageAndWrapAround_WhenPressed()
    {
        var cut = RenderLightbox(index: 1);
        var lightbox = cut.Tb().Lightbox("Trail photos");

        await lightbox.NextAsync();
        await lightbox.NextAsync();
        await lightbox.PreviousAsync();

        Assert.Equal((2, "Mirror Lake from the north shore", null), (lightbox.Index, lightbox.Alt, lightbox.Caption));
        Assert.Equal("Mirror Lake from the north shore, 3 of 3", cut.Find("[data-part=status]").TextContent);
        Assert.Equal([2, 0, 2], _indexes);
    }

    [Fact]
    public async Task Render_ShouldShowTheParentsIndexFitted_WhenItChangesWhileOpen()
    {
        var cut = RenderLightbox(index: 0);
        await cut.Tb().Lightbox().ZoomInAsync();

        cut.Render(parameters => parameters.Add(lightbox => lightbox.Index, 2));

        Assert.Equal((2, 1d), (cut.Tb().Lightbox().Index, cut.Tb().Lightbox().Zoom));
        Assert.Empty(_indexes);
    }

    [Fact]
    public async Task NextAsync_ShouldThrow_WhenTheLastImageIsShownWithoutLoop()
    {
        var cut = RenderLightbox(index: 2, loop: false);
        var lightbox = cut.Tb().Lightbox();

        await Assert.ThrowsAsync<InvalidOperationException>(lightbox.NextAsync);

        Assert.Equal("true", cut.Find("[data-part=next]").GetAttribute("aria-disabled"));
        Assert.Null(cut.Find("[data-part=previous]").GetAttribute("aria-disabled"));
        Assert.Empty(_indexes);
    }

    [Fact]
    public async Task ZoomInAsync_ShouldStepUpToFourTimesAndMakeTheStageATabStop_WhenPressed()
    {
        var cut = RenderLightbox(index: 0);
        var lightbox = cut.Tb().Lightbox();
        Assert.Equal("-1", cut.Find("[data-part=stage]").GetAttribute("tabindex"));

        await lightbox.ZoomInAsync();
        var stage = cut.Find("[data-part=stage]");
        Assert.Equal((1.5, "150%"), (lightbox.Zoom, cut.Find("[data-part=zoom]").TextContent));
        Assert.Equal(("0", "group", "Eagle Ridge at dawn"),
            (stage.GetAttribute("tabindex"), stage.GetAttribute("role"), stage.GetAttribute("aria-label")));
        await lightbox.ZoomInAsync();
        await lightbox.ZoomInAsync();
        await lightbox.ZoomInAsync();

        Assert.Equal(4, lightbox.Zoom);
        await Assert.ThrowsAsync<InvalidOperationException>(lightbox.ZoomInAsync);
        await lightbox.ZoomOutAsync();
        Assert.Equal(3, lightbox.Zoom);
    }

    [Fact]
    public async Task RotateRightAsync_ShouldTurnAQuarterAtATimeUntilFit_WhenPressed()
    {
        var cut = RenderLightbox(index: 0);
        var lightbox = cut.Tb().Lightbox();
        await Assert.ThrowsAsync<InvalidOperationException>(lightbox.FitAsync);

        await lightbox.RotateRightAsync();
        Assert.Equal(90, lightbox.Rotation);
        Assert.Contains("--_tb-lightbox-turn: 90deg", cut.Find("[data-part=stage]").GetAttribute("style"));
        await lightbox.RotateLeftAsync();
        await lightbox.RotateLeftAsync();
        Assert.Equal(270, lightbox.Rotation);
        await lightbox.ZoomInAsync();
        await lightbox.FitAsync();

        Assert.Equal((0, 1d), (lightbox.Rotation, lightbox.Zoom));
    }

    [Fact]
    public async Task NextAsync_ShouldShowTheNextImageFitted_WhenTheShownOneWasZoomedAndTurned()
    {
        var cut = RenderLightbox(index: 0);
        var lightbox = cut.Tb().Lightbox();
        await lightbox.ZoomInAsync();
        await lightbox.RotateRightAsync();

        await lightbox.NextAsync();

        Assert.Equal((1d, 0), (lightbox.Zoom, lightbox.Rotation));
    }

    [Fact]
    public async Task Render_ShouldShowTheImageFitted_WhenOpenedAgain()
    {
        var cut = RenderLightbox(index: 0);
        await cut.Tb().Lightbox().ZoomInAsync();

        cut.Render(parameters => parameters.Add(lightbox => lightbox.Open, false));
        cut.Render(parameters => parameters.Add(lightbox => lightbox.Open, true));

        Assert.Equal(1d, cut.Tb().Lightbox().Zoom);
    }

    [Fact]
    public async Task DoubleClick_ShouldZoomToTwiceTheFittedSizeAndBack_WhenOnTheStage()
    {
        var cut = RenderLightbox(index: 0);

        await cut.Find("[data-part=stage]").DoubleClickAsync(new());
        var zoomed = cut.Tb().Lightbox().Zoom;
        await cut.Find("[data-part=stage]").DoubleClickAsync(new());

        Assert.Equal((2d, 1d), (zoomed, cut.Tb().Lightbox().Zoom));
    }

    [Fact]
    public async Task CloseAsync_ShouldReportTheLightboxClosed_WhenCalled()
    {
        var cut = RenderLightbox(index: 0);

        await cut.Tb().Lightbox().CloseAsync();

        Assert.Equal([false], _opens);
    }

    [Fact]
    public void Render_ShouldShowNoPreviousOrNext_WhenThereIsOneImage()
    {
        var cut = Render<TbLightbox>(parameters => parameters.Add(lightbox => lightbox.Open, true)
            .Add(lightbox => lightbox.Title, "Trail photos").Add(lightbox => lightbox.Images, [Photos[0]]));

        Assert.Empty(cut.FindAll("[data-part=previous], [data-part=next]"));
    }

    [Fact]
    public void Labels_ShouldWordTheButtonsAndPosition_WhenGiven()
    {
        var cut = Render<TbLightbox>(parameters => parameters.Add(lightbox => lightbox.Open, true)
            .Add(lightbox => lightbox.Title, "Trail photos").Add(lightbox => lightbox.Images, Photos)
            .Add(lightbox => lightbox.Labels, new LightboxLabels { Next = "Forward", Position = "{0}/{1}" }));

        Assert.Equal("Forward", cut.Find("[data-part=next]").GetAttribute("aria-label"));
        Assert.Equal("Previous image", cut.Find("[data-part=previous]").GetAttribute("aria-label"));
        Assert.Equal("1/3", cut.Find("[data-part=position]").TextContent);
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoTitle()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            Render<TbLightbox>(parameters => parameters.Add(lightbox => lightbox.Images, Photos)));

        Assert.Equal("TbLightbox needs a Title.", thrown.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnImageHasNoAltText()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbLightbox>(parameters => parameters
            .Add(lightbox => lightbox.Title, "Trail photos").Add(lightbox => lightbox.Images, [new("/x.jpg", " ")])));

        Assert.Equal("Each LightboxImage in a TbLightbox needs Alt text.", thrown.Message);
    }

    private IRenderedComponent<TbLightbox> RenderLightbox(int index, bool loop = true) =>
        Render<TbLightbox>(parameters => parameters.Add(lightbox => lightbox.Open, true)
            .Add(lightbox => lightbox.OpenChanged, open => _opens.Add(open))
            .Add(lightbox => lightbox.Index, index).Add(lightbox => lightbox.IndexChanged, i => _indexes.Add(i))
            .Add(lightbox => lightbox.Title, "Trail photos").Add(lightbox => lightbox.Images, Photos)
            .Add(lightbox => lightbox.Loop, loop).Add(lightbox => lightbox.TelemetryName, "trail-photos"));
}
