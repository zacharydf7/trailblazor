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
            options.Theme.Remember = false;
        });

        var script = Render<TbThemeHead>().Find("script");

        Assert.Equal("_content/Trailblazor/theme-head.js", script.GetAttribute("src"));
        Assert.Equal(["dark", "plum", "false"], [script.GetAttribute("data-mode"), script.GetAttribute("data-palette"),
            script.GetAttribute("data-remember")]);
    }
}
