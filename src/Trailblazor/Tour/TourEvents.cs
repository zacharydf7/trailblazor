using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears Escape skip the tour, so the public component needn't expose [JSInvokable] methods.
internal sealed class TourEvents(Func<Exception, Task> onException, Func<Task> onEscape)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnEscape() => Run(() => onEscape());
}
