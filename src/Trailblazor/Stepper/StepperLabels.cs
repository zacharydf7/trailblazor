namespace Trailblazor;

/// <summary>The words a <see cref="TbStepper"/> shows and says to screen readers.</summary>
public sealed record StepperLabels
{
    /// <summary>Shown under an optional step's title. Leave it null for the app's text for
    /// <see cref="TbStrings.StepperOptional"/>.</summary>
    public string? Optional { get; init; }

    /// <summary>Said after a completed step's title. Leave it null for the app's text for
    /// <see cref="TbStrings.StepperCompleted"/>.</summary>
    public string? Completed { get; init; }

    /// <summary>Read out when a step becomes the current one, with <c>{0}</c> for its title. Leave it null for the app's
    /// text for <see cref="TbStrings.StepperCurrent"/>.</summary>
    public string? Current { get; init; }
}
