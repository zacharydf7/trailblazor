namespace Trailblazor;

/// <summary>One image in a <see cref="TbLightbox"/>.</summary>
/// <param name="Src">The image's address, which can be a <c>data:</c> URL. Give the full-size image here, and a
/// smaller one to the thumbnail that opens the lightbox.</param>
/// <param name="Alt">What the image shows, for people who can't see it. Required, since a lightbox's images are its
/// content.</param>
public sealed record LightboxImage(string Src, string Alt)
{
    /// <summary>Text shown under the image, such as where the photo was taken.</summary>
    public string? Caption { get; init; }
}
