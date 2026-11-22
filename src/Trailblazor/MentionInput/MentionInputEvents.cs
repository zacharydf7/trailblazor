using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser find a trigger and the text after it, and the user choose a suggestion, so the public field
// needn't expose [JSInvokable] methods. The input's id lets the bUnit helper find the events for its field.
internal sealed class MentionInputEvents(Func<Exception, Task> onException, string inputId,
    Func<string?, string, int, string?, Task> onQuery, Func<int, int, Task> onChosen,
    Func<string, Task>? onCommand = null, Func<RichTextSchema>? schema = null)
    : ScriptListener(onException)
{
    public string InputId => inputId;

    // What a rich text editor keeps, so the bUnit helper reads the HTML the editor hands its script as the script does.
    public RichTextSchema? Schema => schema?.Invoke();

    // kind is the kind of block the trigger is in, which only a rich text editor's script sends.
    [JSInvokable]
    public Task OnQuery(string? trigger, string text, int number, string? kind = null) =>
        Run(() => onQuery(trigger, text, number, kind));

    [JSInvokable]
    public Task OnChosen(int index, int resultsFor) => Run(() => onChosen(index, resultsFor));

    // A rich text editor's script names one of the app's slash commands, once the slash is out of the text.
    [JSInvokable]
    public Task OnCommand(string id) => Run(() => onCommand?.Invoke(id) ?? Task.CompletedTask);
}
