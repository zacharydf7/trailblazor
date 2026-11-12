namespace Trailblazor;

/// <summary>The words a <see cref="TbNumberField{TValue}"/> shows and says to screen readers.</summary>
public sealed record NumberFieldLabels
{
    /// <summary>Names the button that steps the number down. Leave it null for the app's text for
    /// <see cref="TbStrings.NumberFieldDecrease"/>.</summary>
    public string? Decrease { get; init; }

    /// <summary>Names the button that steps the number up. Leave it null for the app's text for
    /// <see cref="TbStrings.NumberFieldIncrease"/>.</summary>
    public string? Increase { get; init; }

    /// <summary>The message for text that isn't a number, with <c>{0}</c> for the field's label. Leave it null for the
    /// app's text for <see cref="TbStrings.NumberFieldNotANumber"/>.</summary>
    public string? NotANumber { get; init; }

    /// <summary>The message for a number outside <c>Min</c> and <c>Max</c>, with <c>{0}</c> for the field's label and
    /// <c>{1}</c> and <c>{2}</c> for the lowest and highest numbers. Leave it null for the app's text for
    /// <see cref="TbStrings.NumberFieldRange"/>.</summary>
    public string? Range { get; init; }

    /// <summary>The message for a number below <c>Min</c> when there is no <c>Max</c>, with <c>{0}</c> for the field's
    /// label and <c>{1}</c> for the lowest number. Leave it null for the app's text for
    /// <see cref="TbStrings.NumberFieldAtLeast"/>.</summary>
    public string? AtLeast { get; init; }

    /// <summary>The message for a number above <c>Max</c> when there is no <c>Min</c>, with <c>{0}</c> for the field's
    /// label and <c>{1}</c> for the highest number. Leave it null for the app's text for
    /// <see cref="TbStrings.NumberFieldAtMost"/>.</summary>
    public string? AtMost { get; init; }
}
