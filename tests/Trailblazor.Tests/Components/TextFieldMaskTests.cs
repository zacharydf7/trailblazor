using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TextFieldMaskTests : BunitContext
{
    private const string Phone = "(000) 000-0000";

    public TextFieldMaskTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldShowTheValueInTheMaskAndHintItsShape_WhenGivenAMask()
    {
        var filled = RenderPhone("5551234567");
        var empty = RenderPhone(null);

        Assert.Equal("(555) 123-4567", filled.Find("input").GetAttribute("value"));
        Assert.Equal("(___) ___-____", empty.Find("input").GetAttribute("placeholder"));
        Assert.Equal("numeric", empty.Find("input").GetAttribute("inputmode"));
        Assert.Equal(Phone, JSInterop.Invocations["createMask"][0].Arguments[1]);
    }

    [Theory]
    [InlineData("(555) 123-4567")]
    [InlineData("555.123.4567 x")]
    public void Change_ShouldSetWhatWasTypedIntoTheSlots_WhenTheMaskIsFilled(string text)
    {
        string? value = null;
        var cut = RenderPhone(null, changed => value = changed);

        cut.Find("input").Change(text);

        Assert.Equal("5551234567", value);
        Assert.Equal("(555) 123-4567", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldKeepTheLiteralsInTheValue_WhenIncludeMaskLiteralsIsSet()
    {
        string? value = null;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Expires").Add(field => field.Mask, "00/00")
            .Add(field => field.IncludeMaskLiterals, true)
            .Add(field => field.ValueChanged, (string changed) => value = changed));

        cut.Find("input").Change("1234");

        Assert.Equal("12/34", value);
    }

    [Fact]
    public void Change_ShouldSayTheFieldIsntCompleteAndKeepTheText_WhenSlotsAreEmpty()
    {
        var changed = false;
        var cut = RenderPhone("5551234567", _ => changed = true);

        cut.Find("input").Change("(555) 12");

        Assert.False(changed);
        Assert.Equal("(555) 12", cut.Find("input").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("Phone isn't complete.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldSayTheGivenWordsWithTheLabel_WhenLabelsWordTheIncompleteMessage()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Phone")
            .Add(field => field.Mask, Phone).Add(field => field.Labels, new() { MaskIncomplete = "Finish {0}." }));

        cut.Find("input").Change("(555) 12");

        Assert.Equal("Finish Phone.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Theory]
    [InlineData(@"aa-\0-**", "AB0z", "AB0z", "AB-0-0z")]
    [InlineData("+44 0000", "4123", "4123", "+44 4123")]
    [InlineData("+44 0000", "+44 4123", "4123", "+44 4123")]
    [InlineData("+44 0000 000000", "+447700900123", "7700900123", "+44 7700 900123")]
    [InlineData("+44 0000", "+4 7700", "7700", "+44 7700")]
    [InlineData("+1 (000) 000-0000", "+15551234567", "5551234567", "+1 (555) 123-4567")]
    public void Change_ShouldPassOverOnlyTheLiteralsTheTextHolds_WhenAMaskLiteralCouldFillASlot(string mask,
        string text, string expected, string shown)
    {
        string? value = null;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Code").Add(field => field.Mask, mask)
            .Add(field => field.ValueChanged, (string changed) => value = changed));

        cut.Find("input").Change(text);

        Assert.Equal(expected, value);
        Assert.Equal(shown, cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldEndWithTheMasksTrailingLiterals_WhenEverySlotIsFilled()
    {
        string? value = null;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Area").Add(field => field.Mask, "(000)")
            .Add(field => field.IncludeMaskLiterals, true)
            .Add(field => field.ValueChanged, (string changed) => value = changed));

        cut.Find("input").Change("123");

        Assert.Equal("(123)", value);
        Assert.Equal("(123)", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldFitLettersAndEscapedLiterals_WhenTheMaskHasThem()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Code").Add(field => field.Mask, @"aa-\0-**").Add(field => field.Value, "AB7z"));

        Assert.Equal("AB-0-7z", cut.Find("input").GetAttribute("value"));
        Assert.Null(cut.Find("input").GetAttribute("inputmode"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenATextAreaIsGivenAMask()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Notes").Add(field => field.Lines, 3).Add(field => field.Mask, Phone)));

        Assert.Contains("text area", error.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheMaskHasNoSlots()
    {
        Assert.Throws<ArgumentException>(() => Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Code").Add(field => field.Mask, "--")));
    }

    private IRenderedComponent<TbTextField<string>> RenderPhone(string? value, Action<string?>? changed = null) =>
        Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Phone").Add(field => field.Mask, Phone).Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (string text) => changed?.Invoke(text)));
}
