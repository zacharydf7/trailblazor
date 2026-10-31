namespace Trailblazor;

/// <summary>Shows toasts: short messages that appear for a while without taking focus. Register it with
/// <c>AddTrailblazor()</c> and put one <see cref="TbRoot"/> in the layout. F8 moves focus to the newest toast, and
/// Escape there moves it back.</summary>
public interface IToastService
{
    /// <summary>The toasts not yet closed, oldest first, including those waiting for room.</summary>
    IReadOnlyList<ToastReference> Toasts { get; }

    /// <summary>Shows a toast and announces it to screen readers.</summary>
    /// <returns>The toast, to update or dismiss it later.</returns>
    ToastReference Show(string message, Tone tone = Tone.Neutral, ToastOptions? options = null);

    /// <summary>Closes every toast, the waiting ones too, as <see cref="ToastCloseReason.App"/>.</summary>
    void DismissAll();
}
