using Bunit;
using Microsoft.AspNetCore.Components;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace Trailblazor.Tests.Components;

public class TbImageTests : BunitContext
{
    public TbImageTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Render_ShouldRenderTheImageWithItsSizeAndText_WhenGivenThem()
    {
        var cut = Render<TbImage>(parameters => parameters.Add(image => image.Src, "trail.png")
            .Add(image => image.Alt, "A trail").Add(image => image.Width, 640).Add(image => image.Height, 360)
            .Add(image => image.Fluid, true).Add(image => image.Fit, ImageFit.ScaleDown)
            .Add(image => image.Lazy, true));

        var image = cut.Find("img");
        Assert.Equal("trail.png", image.GetAttribute("src"));
        Assert.Equal("A trail", image.GetAttribute("alt"));
        Assert.Equal(("640", "360"), (image.GetAttribute("width"), image.GetAttribute("height")));
        Assert.Equal("lazy", image.GetAttribute("loading"));
        Assert.Equal(["tb-image", "tb-image--fluid", "tb-image--fit-scaledown"], image.ClassList);
    }

    [Fact]
    public void Render_ShouldKeepAnEmptyAlt_WhenTheImageIsDecorative()
    {
        var cut = Render<TbImage>(parameters => parameters.Add(image => image.Src, "trail.png")
            .Add(image => image.Alt, ""));

        Assert.Equal("", cut.Find("img").GetAttribute("alt"));
        Assert.Null(cut.Find("img").GetAttribute("loading"));
    }

    [Fact]
    public void Error_ShouldShowTheFallbackOnceAndSaySo_WhenTheAddressFails()
    {
        var errors = 0;
        var cut = Render<TbImage>(parameters => parameters.Add(image => image.Src, "missing.png")
            .Add(image => image.Alt, "A trail").Add(image => image.FallbackSrc, "fallback.png")
            .Add(image => image.OnError, () => errors++));

        cut.Find("img").TriggerEvent("onerror", new ErrorEventArgs());
        cut.Find("img").TriggerEvent("onerror", new ErrorEventArgs());

        Assert.Equal("fallback.png", cut.Find("img").GetAttribute("src"));
        Assert.Equal(1, errors);
    }

    [Fact]
    public void Render_ShouldTryANewAddress_WhenTheParentChangesSrcAfterAFailure()
    {
        var cut = Render<TbImage>(parameters => parameters.Add(image => image.Src, "missing.png")
            .Add(image => image.Alt, "A trail").Add(image => image.FallbackSrc, "fallback.png"));
        cut.Find("img").TriggerEvent("onerror", new ErrorEventArgs());

        cut.Render(parameters => parameters.Add(image => image.Src, "trail.png"));

        Assert.Equal("trail.png", cut.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void Render_ShouldListenForNoErrors_WhenThereIsNoFallbackOrHandler()
    {
        var cut = Render<TbImage>(parameters => parameters.Add(image => image.Src, "trail.png")
            .Add(image => image.Alt, "A trail"));

        Assert.Throws<MissingEventHandlerException>(
            () => cut.Find("img").TriggerEvent("onerror", new ErrorEventArgs()));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAltIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Render<TbImage>(parameters => parameters.Add(image => image.Src, "trail.png")));

        Assert.Contains(nameof(TbImage.Alt), exception.Message);
    }
}
