namespace Trailblazor;

/// <summary>How a dialog closed: confirmed with a value, or cancelled.</summary>
public sealed record DialogResult<TResult>(bool Confirmed, TResult? Value)
{
    /// <summary>A confirmed result carrying <paramref name="value"/>.</summary>
    public static DialogResult<TResult> Ok(TResult value) => new(true, value);

    /// <summary>A cancelled result with no value.</summary>
    public static DialogResult<TResult> Cancel() => new(false, default);
}
