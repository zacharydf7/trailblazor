using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbColorPickerTests : BunitContext
{
    private readonly List<string?> _changes = [];

    public TbColorPickerTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private FakeBrowserService Browser => Services.GetRequiredService<FakeBrowserService>();

    [Fact]
    public void Render_ShouldShowTheColorNormalizedAndCheckItsSwatch_WhenGivenAColor()
    {
        var cut = RenderPicker("#CC3333", parameters => parameters
            .Add(picker => picker.Palette, ["#336699", "#cc3333"]));

        Assert.Equal("#cc3333", cut.Find("[data-part=input]").GetAttribute("value"));
        Assert.False(cut.FindAll("[data-part=swatch]")[0].HasAttribute("checked"));
        Assert.True(cut.FindAll("[data-part=swatch]")[1].HasAttribute("checked"));
        Assert.Empty(_changes);
    }

    [Fact]
    public void Render_ShouldTurnOffEveryControlInThePanel_WhenReadOnly()
    {
        var cut = RenderPicker("#CC3333", parameters => parameters.Add(picker => picker.ReadOnly, true)
            .Add(picker => picker.Palette, ["#336699", "#cc3333"]));

        var controls = cut.FindAll("[data-part=swatch], .tb-color-picker__body input[type=range], "
            + ".tb-color-picker__body input[type=number]");
        Assert.NotEmpty(controls);
        Assert.All(controls, control => Assert.True(control.HasAttribute("disabled"), control.OuterHtml));
    }

    [Fact]
    public void Swatch_ShouldChooseItsColor_WhenChanged()
    {
        var cut = RenderPicker("#336699", parameters => parameters
            .Add(picker => picker.Palette, ["#336699", "#cc3333"])
            .Add(picker => picker.SwatchName, swatch => swatch == "#cc3333" ? "Brick" : "Ocean"));

        cut.Find("[data-part=swatch][aria-label=Brick]").Change(true);

        Assert.Equal(["#cc3333"], _changes);
    }

    [Fact]
    public void Change_ShouldTakeAnRgbColor_WhenTyped()
    {
        var cut = RenderPicker("#336699");

        cut.Find("[data-part=input]").Change("rgb(255, 0, 0)");

        Assert.Equal(["#ff0000"], _changes);
        Assert.Equal("0", cut.Find("[data-part=hue]").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldExplainAndKeepTheValue_WhenTheTextIsntAColor()
    {
        var cut = RenderPicker("#336699");

        cut.Find("[data-part=input]").Change("#33669980");

        Assert.Empty(_changes);
        Assert.Equal("Enter a color as #RRGGBB.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Brightness_ShouldKeepTheHueAndSaturation_WhenItGoesToBlackAndBack()
    {
        var cut = RenderPicker("#336699");

        cut.Find("[data-part=brightness]").Input("0");
        cut.Find("[data-part=brightness]").Input("100");

        Assert.Equal(["#000000", "#55aaff"], _changes);
        Assert.Equal("210", cut.Find("[data-part=hue]").GetAttribute("value"));
    }

    [Fact]
    public void Channel_ShouldChangeOnlyItsPart_WhenANumberIsEntered()
    {
        var cut = RenderPicker("#336699");

        cut.Find("[data-part=green]").Change("0");

        Assert.Equal(["#330099"], _changes);
    }

    [Fact]
    public void Alpha_ShouldShowTheOpacityAndWriteItIntoTheHex_WhenOn()
    {
        var cut = RenderPicker("#ff000080", parameters => parameters.Add(picker => picker.Alpha, true));

        Assert.Equal("50", cut.Find("[data-part=alpha]").GetAttribute("value"));
        cut.Find("[data-part=alpha]").Input("25");

        Assert.Equal(["#ff000040"], _changes);
    }

    [Fact]
    public void Eyedropper_ShouldTakeTheColorThePickerReturns_WhenClicked()
    {
        Browser.PickedColor = "#123456";
        var cut = RenderPicker("#336699");

        cut.Find("[data-part=eyedropper]").Click();

        Assert.Equal(["#123456"], _changes);
    }

    [Fact]
    public void Eyedropper_ShouldBeLeftOut_WhenTheBrowserHasNone()
    {
        Browser.CanPickColor = false;

        var cut = RenderPicker("#336699");

        Assert.Empty(cut.FindAll("[data-part=eyedropper]"));
    }

    [Fact]
    public void Eyedropper_ShouldBeLeftOut_WhenTurnedOff()
    {
        var cut = RenderPicker("#336699", parameters => parameters.Add(picker => picker.Eyedropper, false));

        Assert.Empty(cut.FindAll("[data-part=eyedropper]"));
    }

    [Fact]
    public void PaletteOnly_ShouldLeaveOutTheCustomPicker_WhenSet()
    {
        var cut = RenderPicker("#336699", parameters => parameters.Add(picker => picker.Palette, ["#336699"])
            .Add(picker => picker.PaletteOnly, true));

        Assert.Empty(cut.FindAll("[data-part=pad], [data-part=hue], [data-part=red]"));
        Assert.Single(cut.FindAll("[data-part=swatch]"));
    }

    [Fact]
    public void Clear_ShouldEmptyTheColor_WhenClicked()
    {
        var cut = RenderPicker("#336699", parameters => parameters.Add(picker => picker.Clearable, true));

        cut.Find("[data-part=clear]").Click();

        Assert.Equal([null], _changes);
    }

    [Fact]
    public void Render_ShouldNotRaiseValueChanged_WhenTheParentSetsTheColor()
    {
        var cut = RenderPicker("#336699");

        cut.Render(parameters => parameters.Add(picker => picker.Value, "#00FF00"));

        Assert.Empty(_changes);
        Assert.Equal("#00ff00", cut.Find("[data-part=input]").GetAttribute("value"));
        Assert.Equal("120", cut.Find("[data-part=hue]").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldKeepTheHueAndSaturation_WhenTheParentSetsBlack()
    {
        var cut = RenderPicker("#336699");

        cut.Render(parameters => parameters.Add(picker => picker.Value, "#000000"));

        Assert.Equal("210", cut.Find("[data-part=hue]").GetAttribute("value"));
        Assert.Equal("67", cut.Find("[data-part=saturation]").GetAttribute("value"));
        Assert.Equal("0", cut.Find("[data-part=brightness]").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAPaletteColorCantBeRead()
    {
        var failure = Assert.Throws<InvalidOperationException>(() =>
            RenderPicker("#336699", parameters => parameters.Add(picker => picker.Palette, ["#336699", "red"])));

        Assert.Contains("red", failure.Message);
    }

    [Fact]
    public void Render_ShouldPostTheColorAsTyped_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderPicker("#ff000080", parameters => parameters.Add(picker => picker.Alpha, true));

        var input = cut.Find("[data-part=input]");
        Assert.Equal(["text", "#ff000080", ""], new[] { "type", "value", "data-static" }.Select(input.GetAttribute));
        Assert.Empty(cut.FindAll("[data-part=panel]"));
    }

    private IRenderedComponent<TbColorPicker> RenderPicker(string? value,
        Action<ComponentParameterCollectionBuilder<TbColorPicker>>? configure = null) =>
        Render<TbColorPicker>(parameters =>
        {
            parameters.Add(picker => picker.Value, value).Add(picker => picker.Label, "Accent")
                .Add(picker => picker.ValueChanged, (string? color) => _changes.Add(color));
            configure?.Invoke(parameters);
        });
}
