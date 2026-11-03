using Bunit;

namespace Trailblazor.Tests.Components;

public class TbThemeHeadTests : BunitContext
{
    [Fact]
    public void Render_ShouldGiveItsScriptTheAppsDefaults_WhenCalled()
    {
        this.AddTrailblazor(options =>
        {
            options.Theme.Mode = ThemeMode.Dark;
            options.Theme.Palette = Palette.Plum;
            options.Theme.Density = Density.Compact;
            options.Theme.Remember = false;
        });

        var script = Render<TbThemeHead>().Find("script");

        Assert.Equal("_content/Trailblazor/theme-head.js", script.GetAttribute("src"));
        Assert.Equal(["dark", "plum", "compact", "false"], [script.GetAttribute("data-mode"),
            script.GetAttribute("data-palette"), script.GetAttribute("data-density"),
            script.GetAttribute("data-remember")]);
    }

    [Fact]
    public void Render_ShouldGiveItsScriptACustomPaletteByItsExactName_WhenOneIsSet()
    {
        this.AddTrailblazor(options => options.Theme.Palette = new Palette("Forest"));

        var script = Render<TbThemeHead>().Find("script");

        Assert.Equal("Forest", script.GetAttribute("data-palette"));
    }
}
