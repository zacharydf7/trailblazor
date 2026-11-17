namespace Trailblazor;

/// <summary>How a <see cref="TbTour"/> ended.</summary>
public enum TourEnd
{
    /// <summary>The user went through to the last step and pressed Done.</summary>
    Completed,

    /// <summary>The user pressed Skip or Escape before the end.</summary>
    Skipped,
}
