using Microsoft.JSInterop;

namespace Trailblazor;

// Hears focus enter and leave the carousel, so the public carousel needn't expose [JSInvokable] methods.
internal sealed class CarouselEvents(Action<bool> onFocusEnter, Action onFocusLeave)
{
    [JSInvokable]
    public void OnFocusEnter(bool fromKeyboard) => onFocusEnter(fromKeyboard);

    [JSInvokable]
    public void OnFocusLeave() => onFocusLeave();
}
