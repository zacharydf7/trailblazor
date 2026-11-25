namespace Trailblazor;

/// <summary>The words a <see cref="TbCountdown"/> shows. Each one left null uses the app's text.</summary>
public sealed record CountdownLabels
{
    /// <summary>The button that runs the action at once. Leave it null for the app's text for
    /// <see cref="TbStrings.CountdownGoNow"/>.</summary>
    public string? GoNow { get; init; }

    /// <summary>The button that stops the countdown. Leave it null for the app's text for
    /// <see cref="TbStrings.CountdownStay"/>.</summary>
    public string? Stay { get; init; }

    /// <summary>Shown in place of a countdown that was stopped. Leave it null for the app's text for
    /// <see cref="TbStrings.CountdownStayed"/>.</summary>
    public string? Stayed { get; init; }
}
