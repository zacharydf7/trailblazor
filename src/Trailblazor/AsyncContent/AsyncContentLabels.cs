namespace Trailblazor;

/// <summary>The words a <see cref="TbAsyncContent"/> shows and says, as whole sentences, since putting a noun into a
/// sentence doesn't work in every language. Each one left null uses the app's text with the region's
/// <see cref="TbAsyncContent.Label"/> in it.</summary>
public sealed record AsyncContentLabels
{
    /// <summary>Shown and said while a first load is slow, such as "Loading your groups". Leave it null for the app's
    /// text for <see cref="TbStrings.AsyncContentLoading"/>.</summary>
    public string? Loading { get; init; }

    /// <summary>Shown and said when a load fails, such as "Your groups didn't load.". Leave it null for the app's text
    /// for <see cref="TbStrings.AsyncContentLoadFailed"/>.</summary>
    public string? LoadFailed { get; init; }

    /// <summary>The button that loads again after a failure. Leave it null for the app's text for
    /// <see cref="TbStrings.AsyncContentRetry"/>.</summary>
    public string? Retry { get; init; }
}
