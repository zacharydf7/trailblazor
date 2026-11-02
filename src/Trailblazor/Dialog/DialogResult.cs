namespace Trailblazor;

/// <summary>How a dialog closed: confirmed with a value, or canceled.</summary>
/// <param name="Confirmed">Whether the dialog closed with a value rather than being canceled.</param>
/// <param name="Value">The dialog's value, or the default when it was canceled.</param>
public sealed record DialogResult<TResult>(bool Confirmed, TResult? Value)
{
    /// <summary>A confirmed result carrying <paramref name="value"/>.</summary>
    public static DialogResult<TResult> Ok(TResult value) => new(true, value);

    /// <summary>A canceled result with no value.</summary>
    public static DialogResult<TResult> Cancel() => new(false, default);
}
