using System.Globalization;
using Bunit;

namespace Trailblazor.Tests.Components;

public class TbNumberFieldTests : BunitContext
{
    [Fact]
    public void Render_ShouldBeANumberInputWithItsLimits_WhenMinMaxAndStepAreSet()
    {
        var cut = Render<TbNumberField<double>>(parameters => parameters
            .Add(field => field.Label, "Weight")
            .Add(field => field.Value, 2.5)
            .Add(field => field.Min, 0m)
            .Add(field => field.Max, 10m)
            .Add(field => field.Step, 0.25m));

        var input = cut.Find("[data-part=input]");
        Assert.Equal("number", input.GetAttribute("type"));
        Assert.Equal("2.5", input.GetAttribute("value"));
        Assert.Equal("0", input.GetAttribute("min"));
        Assert.Equal("10", input.GetAttribute("max"));
        Assert.Equal("0.25", input.GetAttribute("step"));
    }

    [Fact]
    public void Render_ShouldWriteTheValueWithAPeriod_WhenTheCultureUsesACommaForDecimals()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = Render<TbNumberField<decimal>>(parameters => parameters.Add(field => field.Value, 1.5m));

            Assert.Equal("1.5", cut.Find("input").GetAttribute("value"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData("increment", 6)]
    [InlineData("decrement", -4)]
    public void Click_ShouldStepTheValue_WhenAStepButtonIsClicked(string button, int expected)
    {
        var value = 1;
        var cut = Render<TbNumberField<int>>(parameters => parameters
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (int number) => value = number)
            .Add(field => field.Step, 5m));

        cut.Find($"[data-part={button}]").Click();

        Assert.Equal(expected, value);
    }

    [Fact]
    public void Render_ShouldDisableDecrease_WhenAnUnsignedValueIsAtZero()
    {
        var cut = Render<TbNumberField<byte>>(parameters => parameters.Add(field => field.Value, (byte)0));

        Assert.True(cut.Find("[data-part=decrement]").HasAttribute("disabled"));
    }

    [Fact]
    public void Click_ShouldStopAtTheTypesLimit_WhenAStepWouldPassIt()
    {
        var value = int.MaxValue - 1;
        var cut = Render<TbNumberField<int>>(parameters => parameters
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (int number) => value = number)
            .Add(field => field.Step, 5m));

        cut.Find("[data-part=increment]").Click();

        Assert.Equal(int.MaxValue, value);
        Assert.True(cut.Find("[data-part=increment]").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(1e30)]
    public void Render_ShouldDisableTheStepButtons_WhenADoubleCannotBeStepped(double number)
    {
        var cut = Render<TbNumberField<double>>(parameters => parameters
            .Add(field => field.Value, number)
            .Add(field => field.Min, 0m));

        Assert.True(cut.Find("[data-part=increment]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-part=decrement]").HasAttribute("disabled"));
    }

    [Fact]
    public void Change_ShouldAcceptAHugeDouble_WhenItIsWithinTheLimits()
    {
        var value = 0.0;
        var cut = Render<TbNumberField<double>>(parameters => parameters
            .Add(field => field.Value, value)
            .Add(field => field.Min, 0m)
            .Add(field => field.ValueChanged, (double number) => value = number));

        Commit(cut, "1e29");

        Assert.Equal(1e29, value);
    }

    [Fact]
    public void Click_ShouldStopAtTheLimitAndDisableTheButton_WhenAStepWouldPassIt()
    {
        var cut = Render<TbNumberField<int>>(parameters => parameters
            .Add(field => field.Value, 9)
            .Add(field => field.Max, 10m)
            .Add(field => field.Step, 5m));

        cut.Find("[data-part=increment]").Click();

        Assert.Equal("10", cut.Find("input").GetAttribute("value"));
        Assert.True(cut.Find("[data-part=increment]").HasAttribute("disabled"));
        Assert.False(cut.Find("[data-part=decrement]").HasAttribute("disabled"));
    }

    [Fact]
    public void Click_ShouldStartFromTheMinimum_WhenTheFieldIsEmpty()
    {
        int? value = null;
        var cut = Render<TbNumberField<int?>>(parameters => parameters
            .Add(field => field.Min, 3m)
            .Add(field => field.ValueChanged, (int? number) => value = number));

        cut.Find("[data-part=increment]").Click();

        Assert.Equal(4, value);
    }

    [Theory]
    [InlineData("12", 0, 10, "Quantity must be from 0 to 10.")]
    [InlineData("-1", 0, 10, "Quantity must be from 0 to 10.")]
    [InlineData("-1", 0, null, "Quantity must be at least 0.")]
    [InlineData("12", null, 10, "Quantity must be at most 10.")]
    public void Change_ShouldShowAnErrorAndKeepTheValue_WhenGivenANumberOutsideTheLimits(string text, int? min,
        int? max, string message)
    {
        var raised = false;
        var cut = Render<TbNumberField<int>>(parameters => parameters
            .Add(field => field.Label, "Quantity")
            .Add(field => field.Value, 5)
            .Add(field => field.Min, min)
            .Add(field => field.Max, max)
            .Add(field => field.ValueChanged, (int _) => raised = true));

        Commit(cut, text);

        Assert.Equal(message, cut.Find("[data-part=error]").TextContent.Trim());
        Assert.False(raised);
    }

    [Fact]
    public void Change_ShouldShowAnError_WhenANonNullableFieldIsEmptied()
    {
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Label, "Quantity"));

        Commit(cut, "");

        Assert.Equal("Quantity must be a number.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldShowAnErrorAndKeepTheValue_WhenTheBrowserCannotReadTheText()
    {
        int? value = 4;
        var cut = Render<TbNumberField<int?>>(parameters => parameters
            .Add(field => field.Label, "Copies")
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (int? number) => value = number));

        cut.Find("input").TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = "", BadInput = true });

        Assert.Equal(4, value);
        Assert.Equal("Copies must be a number.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldClearTheValue_WhenANullableFieldIsEmptied()
    {
        int? value = 4;
        var cut = Render<TbNumberField<int?>>(parameters => parameters
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (int? number) => value = number));

        Commit(cut, "");

        Assert.Null(value);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Leave_ShouldShowAnError_WhenTheBrowserCannotReadTheText()
    {
        var cut = Render<TbNumberField<int?>>(parameters => parameters.Add(field => field.Label, "Copies"));

        cut.Find("input").TriggerEvent("ontbnumberleave", new NumberChangeEventArgs { Value = "", BadInput = true });

        Assert.Equal("Copies must be a number.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Leave_ShouldClearTheErrorAndTheValue_WhenUnreadableTextIsDeleted()
    {
        int? value = 4;
        var cut = Render<TbNumberField<int?>>(parameters => parameters
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (int? number) => value = number));
        cut.Find("input").TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = "", BadInput = true });

        cut.Find("input").TriggerEvent("ontbnumberleave", new NumberChangeEventArgs { Value = "" });

        Assert.Null(value);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Change_ShouldShowAnError_WhenAFloatIsTooLargeForItsType()
    {
        var cut = Render<TbNumberField<float>>(parameters => parameters.Add(field => field.Label, "Ratio"));

        Commit(cut, "1e39");

        Assert.Equal("Ratio must be a number.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldHideTheStepButtons_WhenHideStepButtonsIsSet()
    {
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.HideStepButtons, true));

        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenGivenANonNumberType()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbNumberField<string>>());
    }

    [Fact]
    public void Render_ShouldThrow_WhenGivenAnEnumType()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbNumberField<DayOfWeek>>());
    }

    private static void Commit<TValue>(IRenderedComponent<TbNumberField<TValue>> cut, string text) =>
        cut.Find("input").TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = text });
}
