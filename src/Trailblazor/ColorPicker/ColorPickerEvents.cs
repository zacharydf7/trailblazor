using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the pointer move over the saturation and brightness pad, so the public picker needn't expose [JSInvokable]
// methods.
internal sealed class ColorPickerEvents(Func<double, double, Task> onPad)
{
    [JSInvokable]
    public Task OnPad(double saturation, double brightness) => onPad(saturation, brightness);
}
