using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser find a trigger and the text after it, and the user choose a suggestion, so the public field
// needn't expose [JSInvokable] methods. The input's id lets the bUnit helper find the events for its field.
internal sealed class MentionInputEvents(string inputId, Func<string?, string, int, Task> onQuery,
    Func<int, int, Task> onChosen)
{
    public string InputId => inputId;

    [JSInvokable]
    public Task OnQuery(string? trigger, string text, int number) => onQuery(trigger, text, number);

    [JSInvokable]
    public Task OnChosen(int index, int resultsFor) => onChosen(index, resultsFor);
}
