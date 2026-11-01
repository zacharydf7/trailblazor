namespace Trailblazor;

/// <summary>How wide a dialog grows, in rem so it follows the user's text size; the pixels are at the default 16px.
/// A dialog is never wider than the window.</summary>
public enum DialogSize
{
    /// <summary>24rem (384px), for a short question or a single field.</summary>
    Small,

    /// <summary>32rem (512px), for a form of a few fields.</summary>
    Medium,

    /// <summary>48rem (768px), for a wide form or a short table.</summary>
    Large,

    /// <summary>64rem (1024px), for a table or side-by-side panes.</summary>
    ExtraLarge,

    /// <summary>The whole window.</summary>
    Full,
}
