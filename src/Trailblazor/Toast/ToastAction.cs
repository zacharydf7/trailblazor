using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A button on a toast, such as "Undo". Clicking it closes the toast and runs <paramref name="OnClick"/>.
/// Create the callback with <c>EventCallback.Factory.Create(this, ...)</c> so the component that showed the toast
/// re-renders afterwards.</summary>
/// <param name="Text">The button's text.</param>
/// <param name="OnClick">What clicking the button does.</param>
public sealed record ToastAction(string Text, EventCallback OnClick);
