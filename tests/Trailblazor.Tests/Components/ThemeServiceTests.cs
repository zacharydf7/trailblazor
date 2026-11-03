using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class ThemeServiceTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public ThemeServiceTests()
    {
        _module = JSInterop.SetupModule("./_content/Trailblazor/theme.js");
        _module.SetupVoid("apply", _ => true).SetVoidResult();
    }

    private IThemeService Theme => Services.GetRequiredService<IThemeService>();

    [Fact]
    public async Task SetMode_ShouldApplyAndRememberTheModeAndRaiseChanged_WhenCalled()
    {
        this.AddTrailblazor();
        var changes = 0;
        Theme.Changed += () => changes++;

        await Theme.SetModeAsync(ThemeMode.Dark);

        Assert.Equal(["tbTheme", "dark", true], _module.VerifyInvoke("apply").Arguments);
        Assert.Equal((ThemeMode.Dark, 1), (Theme.Mode, changes));
    }

    [Fact]
    public async Task SetPalette_ShouldApplyWithoutRemembering_WhenRememberIsOff()
    {
        this.AddTrailblazor(options => options.Theme.Remember = false);

        await Theme.SetPaletteAsync(Palette.Moss);

        Assert.Equal(["tbPalette", "moss", false], _module.VerifyInvoke("apply").Arguments);
        Assert.Equal(Palette.Moss, Theme.Palette);
    }

    [Fact]
    public async Task SetPalette_ShouldApplyACustomPaletteByItsExactName_WhenGivenOne()
    {
        this.AddTrailblazor();

        await Theme.SetPaletteAsync(new Palette("Forest"));

        Assert.Equal(["tbPalette", "Forest", true], _module.VerifyInvoke("apply").Arguments);
        Assert.Equal("Forest", Theme.Palette.Name);
    }

    [Fact]
    public async Task SetDensity_ShouldApplyAndRememberTheDensityAndRaiseChanged_WhenCalled()
    {
        this.AddTrailblazor();
        var changes = 0;
        Theme.Changed += () => changes++;

        await Theme.SetDensityAsync(Density.Compact);

        Assert.Equal(["tbDensity", "compact", true], _module.VerifyInvoke("apply").Arguments);
        Assert.Equal((Density.Compact, 1), (Theme.Density, changes));
    }

    [Theory]
    [InlineData("dark", "teal", "comfortable", ThemeMode.Dark, "teal", Density.Comfortable)]
    [InlineData(null, "forest", "roomy", ThemeMode.Light, "forest", Density.Compact)]
    [InlineData("light", null, null, ThemeMode.Light, "rose", Density.Compact)]
    public async Task Load_ShouldReadWhatThePageShowsOrFallBackOnTheDefaults_WhenCalled(string? mode, string? palette,
        string? density, ThemeMode expectedMode, string expectedPalette, Density expectedDensity)
    {
        this.AddTrailblazor(options =>
        {
            options.Theme.Mode = ThemeMode.Light;
            options.Theme.Palette = Palette.Rose;
            options.Theme.Density = Density.Compact;
        });
        _module.Setup<string?[]?>("read").SetResult([mode, palette, density]);

        await Theme.LoadAsync();

        Assert.Equal((expectedMode, new Palette(expectedPalette), expectedDensity),
            (Theme.Mode, Theme.Palette, Theme.Density));
    }
}
