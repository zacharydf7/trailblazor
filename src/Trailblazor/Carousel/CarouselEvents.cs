using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears focus enter and leave the carousel, and hears when a move between slides ends, so the public carousel
// needn't expose [JSInvokable] methods.
internal sealed class CarouselEvents(Func<Exception, Task> onException, Action<bool> onFocusEnter, Action onFocusLeave,
    Action<int> onMoveEnd)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnFocusEnter(bool fromKeyboard) => Run(() => onFocusEnter(fromKeyboard));

    [JSInvokable]
    public Task OnFocusLeave() => Run(() => onFocusLeave());

    [JSInvokable]
    public Task OnMoveEnd(int move) => Run(() => onMoveEnd(move));
}
