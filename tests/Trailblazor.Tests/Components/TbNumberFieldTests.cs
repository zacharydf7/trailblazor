using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbNumberFieldTests : BunitContext
{
    public TbNumberFieldTests() => this.AddTrailblazor();

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
    public void Render_ShouldTurnOffTheStepButtonsAndMakeTheInputReadOnly_WhenReadOnly()
    {
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Value, 5)
            .Add(field => field.ReadOnly, true));

        Assert.True(cut.Find("input").HasAttribute("readonly"));
        Assert.True(cut.Find("[data-part=decrement]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-part=increment]").HasAttribute("disabled"));
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

    [Theory]
    [InlineData("P0", "en-US", 0.0, 1.0, "150%", "Rate must be from 0% to 100%.")]
    [InlineData("N1", "fr-FR", 0.0, 1.5, "2,5", "Rate must be from 0,0 to 1,5.")]
    public void Change_ShouldGiveTheLimitsAsTheFieldShowsNumbers_WhenAFormattedFieldGetsANumberOutsideThem(
        string format, string culture, double min, double max, string text, string message)
    {
        var cut = RenderFormatted(format, culture, 0.5m, parameters => parameters.Add(field => field.Label, "Rate")
            .Add(field => field.Min, (decimal)min).Add(field => field.Max, (decimal)max));

        cut.Find("input").Change(text);

        Assert.Equal(message, cut.Find("[data-part=error]").TextContent.Trim());
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

    [Fact]
    public void Render_ShouldBeASpinButtonShowingTheFormattedValue_WhenGivenAFormat()
    {
        var cut = RenderFormatted<decimal>("N2", "en-US", 1234.5m, parameters => parameters.Add(field => field.Min, 0m)
            .Add(field => field.Max, 5000m));

        var input = cut.Find("input");
        Assert.Equal(["text", "decimal", "spinbutton", "1,234.50", "1234.5", "0", "5000", "1,234.50"],
            new[] { "type", "inputmode", "role", "value", "aria-valuenow", "aria-valuemin", "aria-valuemax",
                "aria-valuetext" }.Select(input.GetAttribute));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(29)]
    public void Render_ShouldThrow_WhenDecimalsIsMoreThanADecimalCanHold(int decimals)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbNumberField<decimal>>(parameters => parameters
            .Add(field => field.Decimals, decimals)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkTheInputReadyOrStatic_WhenTheRendererIsOrIsntInteractive(bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        var input = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Label, "Copies"))
            .Find("input");

        Assert.Equal([interactive, !interactive],
            [input.HasAttribute("data-ready"), input.HasAttribute("data-static")]);
    }

    [Fact]
    public void Render_ShouldStayTheBrowsersNumberBox_WhenAFormattedFieldRendersWithoutACircuit()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderFormatted<decimal>("C2", "en-US", 1234.5m);

        var input = cut.Find("input");
        Assert.Equal(["number", "1234.5"], [input.GetAttribute("type"), input.GetAttribute("value")]);
    }

    [Theory]
    [InlineData("N2", "en-US", "2,500.75", 2500.75)]
    [InlineData("C2", "en-US", "$1,200.00", 1200)]
    [InlineData("C2", "en-US", "($5.00)", -5)]
    [InlineData("P0", "en-US", "15%", 0.15)]
    [InlineData("P0", "en-US", "15", 0.15)]
    [InlineData("E2", "en-US", "1.25E+003", 1250)]
    [InlineData("N1", "de-DE", "1.234,5", 1234.5)]
    public void Change_ShouldReadTheTextInTheFieldsCulture_WhenGivenAFormat(string format, string culture, string text,
        double expected)
    {
        decimal? value = null;
        var cut = RenderFormatted<decimal?>(format, culture, null, parameters => parameters
            .Add(field => field.ValueChanged, (decimal? number) => value = number));

        cut.Find("input").Change(text);

        Assert.Equal((decimal)expected, value);
    }

    [Fact]
    public void Change_ShouldExplainAndKeepTheText_WhenAWholeNumberFieldGetsAFraction()
    {
        var cut = RenderFormatted<int>("N0", "en-US", 3);

        cut.Find("input").Change("1.5");

        Assert.Equal("1.5", cut.Find("input").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("N3")]
    public void Change_ShouldRoundToTheDecimalPlaces_WhenDecimalsIsSet(string? format)
    {
        decimal value = 0;
        var cut = Render<TbNumberField<decimal>>(parameters => parameters.Add(field => field.Format, format)
            .Add(field => field.Culture, CultureInfo.InvariantCulture).Add(field => field.Decimals, 2)
            .Add(field => field.ValueChanged, (decimal number) => value = number));

        if (format is null)
        {
            Commit(cut, "3.14159");
        }
        else
        {
            cut.Find("input").Change("3.145");
        }

        Assert.Equal(format is null ? 3.14m : 3.15m, value);
    }

    [Theory]
    [InlineData("25", 10)]
    [InlineData("-3", 1)]
    public void Change_ShouldMoveAValueOutsideTheRangeToTheNearerLimit_WhenClampToRangeIsSet(string text, int expected)
    {
        var value = 5;
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Value, value)
            .Add(field => field.Min, 1m).Add(field => field.Max, 10m).Add(field => field.ClampToRange, true)
            .Add(field => field.ValueChanged, (int number) => value = number));

        Commit(cut, text);

        Assert.Equal(expected, value);
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Theory]
    [InlineData(null, "99999999999", int.MaxValue)]
    [InlineData(null, "-99999999999", int.MinValue)]
    [InlineData(null, "1e400", int.MaxValue)]
    [InlineData("N0", "99,999,999,999", int.MaxValue)]
    [InlineData("P0", "-1e20", int.MinValue)]
    public void Change_ShouldMoveANumberBeyondTheTypeToItsLimit_WhenClampToRangeIsSet(string? format, string text,
        int expected)
    {
        var value = 5;
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Value, value)
            .Add(field => field.Format, format).Add(field => field.Culture, new CultureInfo("en-US"))
            .Add(field => field.ClampToRange, true).Add(field => field.ValueChanged, (int number) => value = number));

        if (format is null)
        {
            Commit(cut, text);
        }
        else
        {
            cut.Find("input").Change(text);
        }

        Assert.Equal(expected, value);
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Theory]
    [InlineData(true, "3.5")]
    [InlineData(true, "NaN")]
    [InlineData(false, "99999999999")]
    public void Change_ShouldSayItNeedsANumber_WhenTheTextIsAFractionOrBeyondTheTypeWithoutClampToRange(bool clamp,
        string text)
    {
        var changed = false;
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Label, "Copies")
            .Add(field => field.ClampToRange, clamp).Add(field => field.ValueChanged, (int _) => changed = true));

        Commit(cut, text);

        Assert.False(changed);
        Assert.Equal("Copies must be a number.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Input_ShouldUpdateTheValueAndKeepTheTextAsTyped_WhenUpdateOnInputIsSetOnAPlainField()
    {
        double? value = null;
        var cut = Render<TbNumberField<double?>>(parameters => parameters.Add(field => field.UpdateOnInput, true)
            .Add(field => field.ValueChanged, (double? number) => value = number));
        var input = cut.Find("input");

        input.TriggerEvent("ontbnumberinput", new NumberChangeEventArgs { Value = "1.50" });
        Assert.Equal(1.5, value);
        Assert.Equal("1.50", cut.Find("input").GetAttribute("value"));

        input.TriggerEvent("ontbnumberinput", new NumberChangeEventArgs { Value = "", BadInput = true });
        Assert.Equal(1.5, value);
        Assert.Equal("", cut.Find("input").GetAttribute("value"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Input_ShouldUpdateTheValueAndFormatItOnlyOnChange_WhenUpdateOnInputIsSetOnAFormattedField()
    {
        var value = 0m;
        var cut = RenderFormatted("C2", "en-US", value, parameters => parameters.Add(field => field.UpdateOnInput, true)
            .Add(field => field.ValueChanged, (decimal number) => value = number));

        cut.Find("input").Input("12");
        Assert.Equal(12m, value);
        Assert.Equal("12", cut.Find("input").GetAttribute("value"));
        cut.Find("input").Input("12x");
        Assert.Equal(12m, value);
        Assert.Equal("12x", cut.Find("input").GetAttribute("value"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));

        cut.Find("input").Change("12");
        Assert.Equal("$12.00", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Clamp_ShouldMoveANumberPastTheTypesEndToIt_WhenTheTypeIsLongOrDouble()
    {
        var whole = 0L;
        var wide = 0d;
        var capped = 0d;
        var longField = Render<TbNumberField<long>>(parameters => parameters.Add(field => field.ClampToRange, true)
            .Add(field => field.ValueChanged, (long number) => whole = number));
        var doubleField = Render<TbNumberField<double>>(parameters => parameters.Add(field => field.ClampToRange, true)
            .Add(field => field.ValueChanged, (double number) => wide = number));
        var cappedField = Render<TbNumberField<double>>(parameters => parameters.Add(field => field.ClampToRange, true)
            .Add(field => field.Max, 100m).Add(field => field.ValueChanged, (double number) => capped = number));

        Commit(longField, "9223372036854775808");
        Commit(doubleField, "1e400");
        Commit(cappedField, "1e300");

        Assert.Equal((long.MaxValue, double.MaxValue, 100d), (whole, wide, capped));
    }

    [Theory]
    [InlineData("N0", "1e30", 1e30)]
    [InlineData("P0", "1e32", 1e30)]
    public void Clamp_ShouldKeepANumberAFormattedDoubleHolds_WhenItIsPastDecimalsRange(string format, string text,
        double expected)
    {
        var value = 0d;
        var cut = RenderFormatted(format, "en-US", value, parameters => parameters
            .Add(field => field.ClampToRange, true)
            .Add(field => field.ValueChanged, (double number) => value = number));

        cut.Find("input").Change(text);

        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null, "15", "150")]
    [InlineData("C2", "abc", "ab")]
    public void Input_ShouldKeepTheTypedTextAndTheError_WhenTypingFollowsACommitThatShowedOne(string? format,
        string committed, string typed)
    {
        var cut = Render<TbNumberField<decimal>>(parameters => parameters.Add(field => field.Label, "Budget")
            .Add(field => field.UpdateOnInput, true).Add(field => field.Max, 10m).Add(field => field.Format, format)
            .Add(field => field.Culture, new CultureInfo("en-US")));

        if (format is null)
        {
            Commit(cut, committed);
            cut.Find("input").TriggerEvent("ontbnumberinput", new NumberChangeEventArgs { Value = typed });
        }
        else
        {
            cut.Find("input").Change(committed);
            cut.Find("input").Input(typed);
        }

        Assert.Equal(typed, cut.Find("input").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Input_ShouldShowTheFormattedValue_WhenTheArrowsStepAwayAndBack()
    {
        var cut = RenderFormatted("C2", "en-US", 0m, parameters => parameters.Add(field => field.UpdateOnInput, true));

        cut.Find("input").Input("12");
        cut.Find("input").KeyDown("ArrowUp");
        cut.Find("input").KeyDown("ArrowDown");

        Assert.Equal("$12.00", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Input_ShouldShowTheFormattedValue_WhenTheParentChangesWhatWasTypedAfterAnAwait()
    {
        var cut = Render<CappingHost>();

        cut.Find("input").Input("12");
        await cut.InvokeAsync(() => cut.Instance.Checked.SetResult());

        Assert.Equal("$10.00", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Input_ShouldShowTheValue_WhenTheParentSetsAnotherAfterTyping()
    {
        var cut = RenderFormatted("C2", "en-US", 0m, parameters => parameters.Add(field => field.UpdateOnInput, true));

        cut.Find("input").Input("12");
        cut.Render(parameters => parameters.Add(field => field.Value, 30m));

        Assert.Equal("$30.00", cut.Find("input").GetAttribute("value"));
    }

    [Theory]
    [InlineData("ArrowUp", false, 6)]
    [InlineData("ArrowDown", false, 4)]
    [InlineData("ArrowUp", true, 5)]
    public void KeyDown_ShouldStepAFormattedField_WhenAnArrowIsPressedAndItIsntReadOnly(string key, bool readOnly,
        int expected)
    {
        var value = 5;
        var cut = RenderFormatted<int>("N0", "en-US", value, parameters => parameters
            .Add(field => field.ReadOnly, readOnly).Add(field => field.ValueChanged, (int number) => value = number));

        cut.Find("input").KeyDown(key);

        Assert.Equal(expected, value);
    }

    private IRenderedComponent<TbNumberField<TValue>> RenderFormatted<TValue>(string format, string culture,
        TValue value, Action<ComponentParameterCollectionBuilder<TbNumberField<TValue>>>? configure = null) =>
        Render<TbNumberField<TValue>>(parameters =>
        {
            parameters.Add(field => field.Format, format).Add(field => field.Culture, new CultureInfo(culture))
                .Add(field => field.Value, value);
            configure?.Invoke(parameters);
        });

    // Caps what is typed at 10 once the test lets its await pass, as a handler that checks with a service would.
    private sealed class CappingHost : ComponentBase
    {
        private decimal _budget;

        public TaskCompletionSource Checked { get; } = new();

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbNumberField<decimal>>(0);
            builder.AddAttribute(1, nameof(TbNumberField<decimal>.Format), "C2");
            builder.AddAttribute(2, nameof(TbNumberField<decimal>.Culture), new CultureInfo("en-US"));
            builder.AddAttribute(3, nameof(TbNumberField<decimal>.UpdateOnInput), true);
            builder.AddAttribute(4, nameof(TbNumberField<decimal>.Value), _budget);
            builder.AddAttribute(5, nameof(TbNumberField<decimal>.ValueChanged),
                EventCallback.Factory.Create<decimal>(this, async budget =>
                {
                    await Checked.Task;
                    _budget = Math.Min(budget, 10);
                }));
            builder.CloseComponent();
        }
    }

    private static void Commit<TValue>(IRenderedComponent<TbNumberField<TValue>> cut, string text) =>
        cut.Find("input").TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = text });
}
