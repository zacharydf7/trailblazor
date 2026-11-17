using Microsoft.JSInterop;

namespace Trailblazor;

// Hears Escape skip the tour, so the public component needn't expose [JSInvokable] methods.
internal sealed class TourEvents(Func<Task> onEscape)
{
    [JSInvokable]
    public Task OnEscape() => onEscape();
}
