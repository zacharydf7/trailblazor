using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbCodeFieldTests : BunitContext
{
    public TbCodeFieldTests()
    {
        this.AddTrailblazor();
    }

    private List<string?> Values { get; } = [];

    private List<string> Completed { get; } = [];

    private IRenderedComponent<TbCodeField> RenderCode(
        Action<ComponentParameterCollectionBuilder<TbCodeField>>? configure = null) =>
        Render<TbCodeField>(parameters =>
        {
            parameters.Add(field => field.Label, "Verification code")
                .Add(field => field.ValueChanged, (string value) => Values.Add(value))
                .Add(field => field.OnComplete, (string code) => Completed.Add(code));
            configure?.Invoke(parameters);
        });

    [Fact]
    public void Render_ShouldOfferOneInputForAOneTimeCode_WhenShown()
    {
        var cut = RenderCode(parameters => parameters.Add(field => field.Length, 4));

        var input = cut.Find("[data-part=input]");
        Assert.Equal(("one-time-code", "numeric", "4"),
            (input.GetAttribute("autocomplete"), input.GetAttribute("inputmode"), input.GetAttribute("maxlength")));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Single(cut.FindAll("input"));
    }

    [Theory]
    [InlineData(CodeCharacters.Digits, 6, "6 digits")]
    [InlineData(CodeCharacters.Digits, 1, "1 digit")]
    [InlineData(CodeCharacters.LettersAndDigits, 8, "8 characters")]
    public void Render_ShouldDescribeTheInputByItsLength_WhenShown(CodeCharacters characters, int length,
        string description)
    {
        var cut = RenderCode(parameters => parameters.Add(field => field.Characters, characters)
            .Add(field => field.Length, length).Add(field => field.HelperText, "Sent to your phone."));

        var described = cut.Find("[data-part=input]").GetAttribute("aria-describedby")!.Split(' ');
        Assert.Equal(["Sent to your phone.", description],
            described.Select(id => cut.Find($"#{id}").TextContent.Trim()));
    }

    [Fact]
    public void Labels_ShouldWordTheLengthAndTheIncompleteMessage_WhenGiven()
    {
        var cut = RenderCode(parameters => parameters.Add(field => field.Length, 4).Add(field => field.Labels,
            new CodeFieldLabels { Length = "{0} numbers", Incomplete = "{0} wants {1} numbers." }));

        cut.Find("[data-part=input]").Input("12");
        cut.Find("[data-part=input]").Change("12");

        Assert.Equal("4 numbers", cut.Find($"#{cut.Find("[data-part=input]").GetAttribute("aria-describedby")!
            .Split(' ').Last()}").TextContent);
        Assert.Equal("Verification code wants 4 numbers.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Input_ShouldKeepOnlyTheCodesCharactersAndRaiseOnComplete_WhenTyped()
    {
        var cut = RenderCode();

        cut.Find("[data-part=input]").Input("12-34 5");
        cut.Find("[data-part=input]").Input("12345678");

        Assert.Equal(["123456"], Values);
        Assert.Equal(["123456"], Completed);
        Assert.Equal("123456", cut.Find("[data-part=input]").GetAttribute("value"));
    }

    [Theory]
    [InlineData("ar-EG", "١٢٣-٤٥٦")]
    [InlineData("fa-IR", "۱۲۳۴۵۶")]
    [InlineData("ja-JP", "１２３４５６")]
    [InlineData("en-US", "𝟏𝟐𝟑𝟒𝟓𝟔")]
    public void Input_ShouldTakeEachDigitAsItsAsciiDigit_WhenTypedInAnotherScript(string culture, string typed)
    {
        var current = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(culture);
            var cut = RenderCode();

            cut.Find("[data-part=input]").Input(typed);

            Assert.Equal(["123456"], Values);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = current;
        }
    }

    [Fact]
    public void Input_ShouldTakeLettersInCapitals_WhenLettersAreAllowed()
    {
        var cut = RenderCode(parameters => parameters.Add(field => field.Characters, CodeCharacters.LettersAndDigits)
            .Add(field => field.Length, 4));

        cut.Find("[data-part=input]").Input("ab-1c");

        Assert.Equal(["AB1C"], Values);
        Assert.Equal("text", cut.Find("[data-part=input]").GetAttribute("inputmode"));
    }

    [Fact]
    public void Input_ShouldRaiseOnCompleteAgain_WhenAFullCodeChangesToAnother()
    {
        var cut = RenderCode(parameters => parameters.Add(field => field.Value, "123456"));

        cut.Find("[data-part=input]").Input("123456");
        cut.Find("[data-part=input]").Input("123457");

        Assert.Equal(["123457"], Completed);
    }

    [Fact]
    public void Input_ShouldRaiseOnCompleteAgain_WhenTheLastCharacterIsDeletedAndRetyped()
    {
        var cut = RenderCode();

        cut.Find("[data-part=input]").Input("123456");
        cut.Find("[data-part=input]").Input("12345");
        cut.Find("[data-part=input]").Input("123456");

        Assert.Equal(["123456", "123456"], Completed);
    }

    [Fact]
    public void Input_ShouldLeaveTheValue_WhenTheFieldIsReadOnly()
    {
        var cut = RenderCode(parameters => parameters.Add(field => field.Value, "123456")
            .Add(field => field.ReadOnly, true));

        cut.Find("[data-part=input]").Input("654321");

        Assert.Empty(Values);
        Assert.Empty(Completed);
    }

    [Fact]
    public void Validate_ShouldAskTheLocalizerForTheCountedWords_WhenAPartlyTypedCodeCommits()
    {
        Services.AddScoped<ITbLocalizer, PluralRecorder>();
        var cut = RenderCode();

        cut.Find("[data-part=input]").Change("1234");

        Assert.Equal("CodeField.Incomplete for 6", cut.Find("[role=alert], .tb-field__error").TextContent.Trim());
    }

    [Fact]
    public void Validate_ShouldAskForEveryCharacter_WhenAPartlyTypedCodeCommits()
    {
        var cut = RenderCode();

        cut.Find("[data-part=input]").Input("1234");
        Assert.Null(cut.Find("[data-part=input]").GetAttribute("aria-invalid"));
        cut.Find("[data-part=input]").Change("1234");

        Assert.Equal("Verification code needs all 6 characters.",
            cut.Find("[role=alert], .tb-field__error").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-part=input]").GetAttribute("aria-invalid"));
        Assert.Empty(Values);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123-45")]
    public void Form_ShouldBeInvalid_WhenTheCodeArrivesShortOrWithOtherCharacters(string posted)
    {
        bool? valid = null;
        Render<TbForm>(parameters => parameters.Add(form => form.IsValid, true)
            .Add(form => form.IsValidChanged, (bool now) => valid = now)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbCodeField>(0);
                builder.AddAttribute(1, nameof(TbCodeField.Label), "Verification code");
                builder.AddAttribute(2, nameof(TbCodeField.Value), posted);
                builder.CloseComponent();
            })));

        Assert.False(valid);
    }

    [Fact]
    public void Dispose_ShouldLeaveTheFormValid_WhenAFieldWithAnErrorIsRemoved()
    {
        var shown = true;
        bool? valid = null;
        var cut = Render<TbForm>(parameters => parameters
            .Add(form => form.IsValidChanged, (bool now) => valid = now)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                if (shown)
                {
                    builder.OpenComponent<TbCodeField>(0);
                    builder.AddAttribute(1, nameof(TbCodeField.Label), "Verification code");
                    builder.CloseComponent();
                }
            })));
        cut.Find("[data-part=input]").Change("12");
        Assert.False(valid);

        shown = false;
        cut.Render();

        Assert.True(valid);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheLengthIsBelowOne()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RenderCode(parameters => parameters.Add(field => field.Length, 0)));
    }
}
