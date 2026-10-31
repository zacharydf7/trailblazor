namespace Trailblazor;

/// <summary>Why a toast closed, as <see cref="ToastReference.Closed"/> reports it.</summary>
public enum ToastCloseReason
{
    /// <summary>Its time ran out.</summary>
    TimedOut,

    /// <summary>The user pressed its close button.</summary>
    CloseButton,

    /// <summary>The user pressed its action, such as "Undo".</summary>
    Action,

    /// <summary>The app closed it, with <see cref="ToastReference.Dismiss"/> or <see cref="IToastService.DismissAll"/>.
    /// </summary>
    App,
}
