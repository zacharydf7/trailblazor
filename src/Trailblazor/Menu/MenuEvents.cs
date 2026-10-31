using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report that a labeled menu opened, by pointer or key, so the public menu needn't expose
// [JSInvokable] methods.
internal sealed class MenuEvents(Action onOpened)
{
    [JSInvokable]
    public void OnOpened() => onOpened();
}
