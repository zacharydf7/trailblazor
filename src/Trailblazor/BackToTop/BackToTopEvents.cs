using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the button first show, so the public component needn't expose [JSInvokable] methods.
internal sealed class BackToTopEvents(Action onShown)
{
    [JSInvokable]
    public void OnShown() => onShown();
}
