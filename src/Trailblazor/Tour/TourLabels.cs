namespace Trailblazor;

/// <summary>The words a tour's buttons show. Each one left null uses the app's text.</summary>
public sealed record TourLabels
{
    /// <summary>The button to the next step. Leave it null for the app's text for <see cref="TbStrings.TourNext"/>.
    /// </summary>
    public string? Next { get; init; }

    /// <summary>The button to the step before. Leave it null for the app's text for <see cref="TbStrings.TourBack"/>.
    /// </summary>
    public string? Back { get; init; }

    /// <summary>The last step's button, which ends the tour. Leave it null for the app's text for
    /// <see cref="TbStrings.TourDone"/>.</summary>
    public string? Done { get; init; }

    /// <summary>The button that ends the tour early. Leave it null for the app's text for
    /// <see cref="TbStrings.TourSkip"/>.</summary>
    public string? Skip { get; init; }

    /// <summary>Which step shows, with <c>{0}</c> for its number and <c>{1}</c> for the number of steps. Leave it null
    /// for the app's text for <see cref="TbStrings.TourPosition"/>.</summary>
    public string? Position { get; init; }
}
