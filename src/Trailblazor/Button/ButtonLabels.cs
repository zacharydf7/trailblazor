namespace Trailblazor;

/// <summary>The words a button shows and says while its <c>Cooldown</c> runs. Each one left null uses the app's text.
/// </summary>
public sealed record ButtonLabels
{
    /// <summary>The seconds left, shown after the button's text, with <c>{0}</c> for how many. Leave it null for the
    /// app's text for <see cref="TbStrings.ButtonCooldownLeft"/>.</summary>
    public string? CooldownLeft { get; init; }

    /// <summary>The button's description while its cooldown runs, read out once as it starts, with <c>{0}</c> for the
    /// seconds. It's used for every count, including one. Leave it null for the app's text for
    /// <see cref="TbStrings.ButtonCooldown"/>, which has a one-form.</summary>
    public string? Cooldown { get; init; }
}
