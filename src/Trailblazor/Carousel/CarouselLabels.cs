namespace Trailblazor;

/// <summary>The words a carousel says to screen readers.</summary>
public sealed record CarouselLabels
{
    /// <summary>Names the previous button. Leave it null for the app's text for
    /// <see cref="TbStrings.CarouselPrevious"/>.</summary>
    public string? Previous { get; init; }

    /// <summary>Names the next button. Leave it null for the app's text for <see cref="TbStrings.CarouselNext"/>.
    /// </summary>
    public string? Next { get; init; }

    /// <summary>Names a button that shows a slide, with <c>{0}</c> for the slide's number. Leave it null for the
    /// app's text for <see cref="TbStrings.CarouselSlide"/>.</summary>
    public string? Slide { get; init; }

    /// <summary>Names a slide, with <c>{0}</c> for its number and <c>{1}</c> for the number of slides. Leave it null
    /// for the app's text for <see cref="TbStrings.CarouselSlideOf"/>.</summary>
    public string? SlideOf { get; init; }

    /// <summary>Names the button that stops the slides turning. Leave it null for the app's text for
    /// <see cref="TbStrings.CarouselStop"/>.</summary>
    public string? Stop { get; init; }

    /// <summary>Names the button that starts the slides turning again. Leave it null for the app's text for
    /// <see cref="TbStrings.CarouselStart"/>.</summary>
    public string? Start { get; init; }
}
