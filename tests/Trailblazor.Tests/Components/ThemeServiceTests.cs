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

    [Theory]
    [InlineData("dark", "teal", ThemeMode.Dark, Palette.Teal)]
    [InlineData(null, "unknown", ThemeMode.Light, Palette.Rose)]
    public async Task Load_ShouldReadWhatThePageShowsOrFallBackOnTheDefaults_WhenCalled(string? mode, string? palette,
        ThemeMode expectedMode, Palette expectedPalette)
    {
        this.AddTrailblazor(options =>
        {
            options.Theme.Mode = ThemeMode.Light;
            options.Theme.Palette = Palette.Rose;
        });
        _module.Setup<string?[]?>("read").SetResult([mode, palette]);

        await Theme.LoadAsync();

        Assert.Equal((expectedMode, expectedPalette), (Theme.Mode, Theme.Palette));
    }
}
