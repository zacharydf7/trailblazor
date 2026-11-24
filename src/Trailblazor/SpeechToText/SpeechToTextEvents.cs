using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser's recognition, so the public component needn't expose [JSInvokable] methods. The button's id lets
// the bUnit helper find these events.
internal sealed class SpeechToTextEvents(Func<Exception, Task> onException, string buttonId, Func<string, Task> onText,
    Func<string, Task> onError, Action<int> onStopped, Func<int?> sessionAPhraseEnds)
    : ScriptListener(onException)
{
    public string ButtonId => buttonId;

    // The session the next phrase ends, or null if none does (recognition without Continuous stops after one phrase).
    public int? SessionAPhraseEnds => sessionAPhraseEnds();

    [JSInvokable]
    public Task OnText(string text) => Run(() => onText(text));

    [JSInvokable]
    public Task OnError(string error) => Run(() => onError(error));

    [JSInvokable]
    public Task OnStopped(int session) => Run(() => onStopped(session));
}
