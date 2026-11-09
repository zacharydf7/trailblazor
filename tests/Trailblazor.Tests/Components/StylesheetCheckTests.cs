using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class StylesheetCheckTests : BunitContext
{
    [Fact]
    public void Root_ShouldCheckThePagesStylesheets_WhenItRendersInteractively()
    {
        this.AddTrailblazor();

        Render<TbRoot>();

        Assert.Single(JSInterop.Invocations["checkStylesheets"]);
    }

    [Fact]
    public void Root_ShouldLeaveTheStylesheetsAlone_WhenTheAppTurnsTheWarningOff()
    {
        this.AddTrailblazor(options => options.WarnAboutUnlayeredStyles = false);

        Render<TbRoot>();

        Assert.Empty(JSInterop.Invocations["checkStylesheets"]);
    }
}
