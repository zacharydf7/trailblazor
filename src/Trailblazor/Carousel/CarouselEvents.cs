using Microsoft.JSInterop;

namespace Trailblazor;

// Hears focus enter and leave the carousel, and hears when a move between slides ends, so the public carousel
// needn't expose [JSInvokable] methods.
internal sealed class CarouselEvents(Action<bool> onFocusEnter, Action onFocusLeave, Action<int> onMoveEnd)
{
    [JSInvokable]
    public void OnFocusEnter(bool fromKeyboard) => onFocusEnter(fromKeyboard);

    [JSInvokable]
    public void OnFocusLeave() => onFocusLeave();

    [JSInvokable]
    public void OnMoveEnd(int move) => onMoveEnd(move);
}
