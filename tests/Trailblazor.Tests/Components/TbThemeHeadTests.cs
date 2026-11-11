using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

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

    [Fact]
    public void Render_ShouldWriteNoStyle_WhenThePaletteHasNoSeeds()
    {
        this.AddTrailblazor(options => options.Theme.Palette = new Palette("forest"));

        Assert.Empty(Render<TbThemeHead>().FindAll("style"));
    }

    [Fact]
    public void Render_ShouldWriteThePalettesTokens_WhenThePaletteHasSeeds()
    {
        this.AddTrailblazor(options => options.Theme.Palette = new Palette("brand")
        {
            Seeds = new() { Primary = "#1F5FAD", Neutral = "#5B6472" },
        });

        var css = Render<TbThemeHead>().Find("style").TextContent;

        Assert.Contains("[data-tb-palette=\"brand\"] {", css);
        Assert.Contains("--tb-color-primary: light-dark(oklch(", css);
        Assert.Contains("--tb-color-neutral-subtle: light-dark(oklch(", css);
    }

    [Fact]
    public void Render_ShouldWarnOnceAboutEachHardToReadPair_WhenRenderedAgain()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<SeededPalettes>>(new FakeLogger<SeededPalettes>(logs));
        this.AddTrailblazor(options => options.Theme.Palette = new Palette("lemon")
        {
            Seeds = new() { Primary = "#FFD84D" },
        });

        Render<TbThemeHead>();
        Render<TbThemeHead>();

        var warnings = logs.GetSnapshot().Where(log => log.Level == LogLevel.Warning).Select(log => log.Message)
            .ToList();
        Assert.Contains(warnings, warning =>
            warning.StartsWith("Palette lemon in light mode: --tb-color-primary on --tb-color-background has a contrast"));
        Assert.Equal(warnings.Count, warnings.Distinct().Count());
    }

    [Fact]
    public void Render_ShouldNotWarn_WhenTheSeedsReadWellInBothModes()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<SeededPalettes>>(new FakeLogger<SeededPalettes>(logs));
        this.AddTrailblazor(options => options.Theme.Palette = new Palette("brand")
        {
            Seeds = new() { Primary = "#1F5FAD", Neutral = "#5B6472" },
        });

        Render<TbThemeHead>();

        Assert.Empty(logs.GetSnapshot());
    }

    [Fact]
    public void Render_ShouldNotWarn_WhenContrastWarningsAreOff()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<SeededPalettes>>(new FakeLogger<SeededPalettes>(logs));
        this.AddTrailblazor(options =>
        {
            options.Theme.Palette = new Palette("lemon") { Seeds = new() { Primary = "#FFD84D" } };
            options.WarnAboutContrast = false;
        });

        var cut = Render<TbThemeHead>();

        Assert.Contains("[data-tb-palette=\"lemon\"]", cut.Find("style").TextContent);
        Assert.Empty(logs.GetSnapshot());
    }
}
