using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser's recognition, so the public component needn't expose [JSInvokable] methods. The button's id lets
// the bUnit helper find these events.
internal sealed class SpeechToTextEvents(string buttonId, Func<string, Task> onText, Func<string, Task> onError,
    Action<int> onStopped)
{
    public string ButtonId => buttonId;

    [JSInvokable]
    public Task OnText(string text) => onText(text);

    [JSInvokable]
    public Task OnError(string error) => onError(error);

    [JSInvokable]
    public void OnStopped(int session) => onStopped(session);
}
