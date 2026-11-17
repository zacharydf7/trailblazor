namespace Trailblazor;

/// <summary>How a <see cref="TbCarousel"/> moves from one slide to the next. For someone who asks for reduced motion,
/// and on a static page, every turn is instant.</summary>
public enum CarouselTransition
{
    /// <summary>The shown slide moves out as the next moves in, in the direction of travel.</summary>
    Slide,

    /// <summary>The shown slide fades out as the next fades in.</summary>
    Fade,

    /// <summary>The next slide shows at once.</summary>
    None,
}
