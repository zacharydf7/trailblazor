using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class ColorPickerExampleTests : DocsTest
{
    [Fact]
    public void ColorPickerExample_ShouldShowTheBoundValue_WhenASwatchIsChosen()
    {
        var cut = Render<ColorPickerExample>();
        Assert.Contains("Trail marker value: #1f6feb", cut.Markup);

        cut.Find("[data-part=swatch][aria-label='Red blaze']").Change(true);

        Assert.Contains("Trail marker value: #d1242f", cut.Markup);
    }

    [Fact]
    public void AlphaExample_ShouldWriteTheColorWithItsOpacity_WhenTheOpacityChanges()
    {
        var cut = Render<ColorPickerAlphaExample>();
        Assert.Contains("Map overlay value: #2da44e66", cut.Markup);

        cut.Find("[data-part=alpha]").Input("25");

        Assert.Contains("Map overlay value: #2da44e40", cut.Markup);
    }

    [Fact]
    public void EyedropperExample_ShouldLeaveOutTheEyedropper_WhenItIsTurnedOff()
    {
        var cut = Render<ColorPickerEyedropperExample>();

        Assert.Empty(cut.FindAll("[data-part=eyedropper]"));
    }

    [Fact]
    public void PaletteOnlyExample_ShouldLeaveOutTheCustomPicker_WhenPaletteOnlyIsSet()
    {
        var cut = Render<ColorPickerPaletteOnlyExample>();

        Assert.NotEmpty(cut.FindAll("[data-part=swatch]"));
        Assert.Empty(cut.FindAll("[data-part=pad]"));
    }

    [Fact]
    public void PaletteOnlyExample_ShouldStillTakeAnotherColor_WhenOneIsTyped()
    {
        var cut = Render<ColorPickerPaletteOnlyExample>();

        cut.Find("[data-part=input]").Change("#123456");

        Assert.Contains("Tent color value: #123456", cut.Markup);
    }

    [Fact]
    public void ClearExample_ShouldShowNull_WhenTheColorIsCleared()
    {
        var cut = Render<ColorPickerClearExample>();
        Assert.Contains("Highlight value: #f2c037", cut.Markup);

        cut.Find("[data-part=clear]").Click();

        Assert.Contains("Highlight value: null", cut.Markup);
    }

    [Fact]
    public void InvalidExample_ShouldSayTheFormatAndKeepTheValue_WhenTheTextCannotBeRead()
    {
        var cut = Render<ColorPickerInvalidExample>();

        cut.Find("[data-part=input]").Change("blue");

        Assert.Equal("Enter a color as #RRGGBB.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-part=input]").GetAttribute("aria-invalid"));
        Assert.Contains("Boundary color value: #1f6feb", cut.Markup);
    }

    [Fact]
    public void InvalidExample_ShouldMakeTheValueNull_WhenTheBoxIsEmptied()
    {
        var cut = Render<ColorPickerInvalidExample>();

        cut.Find("[data-part=input]").Change("");

        Assert.Contains("Boundary color value: null", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<ColorPickerRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Choose your group's color.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ValidationExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<ColorPickerValidationExample>();

        cut.WaitForAssertion(() => Assert.Equal("White won't show on the trail map.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ErrorExample_ShouldShowTheErrorUntilTheColorChanges_WhenTheCheckFindsAProblem()
    {
        var cut = Render<ColorPickerErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        Button(cut, "Check the color").Click();
        Assert.Equal("Another club already uses that color.", cut.Find("[data-part=error]").TextContent.Trim());

        cut.Find("[data-part=input]").Change("#2da44e");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void StatesExample_ShouldMakeOneReadOnlyAndDisableTheOther_WhenRendered()
    {
        var cut = Render<ColorPickerStatesExample>();
        var fields = cut.FindAll("[data-part=input]");

        Assert.True(fields[0].HasAttribute("readonly"));
        Assert.True(fields[1].HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnWords_WhenRendered()
    {
        var cut = Render<ColorPickerLabelsExample>();

        Assert.Equal("Clear the shelter color", cut.Find("[data-part=clear]").GetAttribute("aria-label"));
        Assert.NotNull(Button(cut, "Open the shelter colors"));
        cut.Find("[data-part=input]").Change("blue");
        Assert.Equal("Enter the shelter's color as #RRGGBB.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheColorPicker()
    {
        var cut = ComponentPage("color-picker");
        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.Equal(["Enter", "Space", "Left", "Right", "Up", "Down", "Space", "Home", "End", "Page Up", "Page Down", "Up",
            "Down", "Escape"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", accessibility);
        Assert.DoesNotContain("Variant", accessibility);
    }
}
