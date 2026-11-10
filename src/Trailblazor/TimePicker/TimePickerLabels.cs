namespace Trailblazor;

/// <summary>The words a <see cref="TbTimePicker{TTime}"/> shows.</summary>
public sealed record TimePickerLabels
{
    /// <summary>The message for a time outside <c>Min</c> and <c>Max</c>, with <c>{0}</c> and <c>{1}</c> for the
    /// earliest and latest times. Leave it null for the app's text for <see cref="TbStrings.TimePickerOutOfRange"/>.
    /// </summary>
    public string? OutOfRange { get; init; }

    /// <summary>The message for a time before <c>Min</c> when there is no <c>Max</c>, with <c>{0}</c> for the earliest
    /// time. Leave it null for the app's text for <see cref="TbStrings.TimePickerAtLeast"/>.</summary>
    public string? AtLeast { get; init; }

    /// <summary>The message for a time after <c>Max</c> when there is no <c>Min</c>, with <c>{0}</c> for the latest
    /// time. Leave it null for the app's text for <see cref="TbStrings.TimePickerAtMost"/>.</summary>
    public string? AtMost { get; init; }
}
