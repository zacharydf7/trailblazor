namespace Trailblazor;

/// <summary>How a dialog closed: confirmed with a value, or cancelled.</summary>
/// <param name="Confirmed">Whether the dialog closed with a value rather than being cancelled.</param>
/// <param name="Value">The dialog's value, or the default when it was cancelled.</param>
public sealed record DialogResult<TResult>(bool Confirmed, TResult? Value)
{
    /// <summary>A confirmed result carrying <paramref name="value"/>.</summary>
    public static DialogResult<TResult> Ok(TResult value) => new(true, value);

    /// <summary>A cancelled result with no value.</summary>
    public static DialogResult<TResult> Cancel() => new(false, default);
}
