using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A box to write a prompt for a model, with suggestions, and the responses it streams back, newest first,
/// each with Copy, Retry and Insert. Enter generates, Shift+Enter starts a new line, and Escape stops a response while
/// it streams. Screen readers hear when a response starts, is ready, stops or fails.</summary>
public partial class TbAiPrompt
{
    private readonly string _id = $"tb-ai-prompt-{Guid.NewGuid():N}";
    // Newest first, each with a key that stays with it as newer ones are added above.
    private readonly List<(int Key, AiPromptOutput Output)> _outputs = [];
    private CancellationTokenSource? _generating;
    private ElementReference _root;
    // Set once the script has attached and handles Enter (generate) and Escape (stop); data-ready says so.
    private IJSObjectReference? _handle;
    private string _draft = "";
    private int _keys;
    private int _commits;
    private bool _disposed;

    /// <summary>The prompt box's visible label, such as "Ask about this trail". It also names the whole prompt for
    /// screen readers. Required.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Generates a response to a prompt: the prompt in, the text out in chunks as the model streams them.
    /// Pass the token on to the model, so Stop ends the response at once. Required.</summary>
    [Parameter, EditorRequired]
    public Func<string, CancellationToken, IAsyncEnumerable<string>>? Generate { get; set; }

    /// <summary>Prompts to start from, shown as buttons that fill the box, so the user can change one before
    /// generating.</summary>
    [Parameter]
    public IReadOnlyList<string> Suggestions { get; set; } = [];

    /// <summary>Hint text in the empty prompt box.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Raised when the user inserts a response, such as into the document they're writing. Each response gets
    /// an Insert button only when this is set.</summary>
    [Parameter]
    public EventCallback<AiPromptOutput> OnInsert { get; set; }

    /// <summary>Shows a response's text, such as Markdown rendered to HTML. Without it, the text shows as it is.
    /// </summary>
    [Parameter]
    public RenderFragment<AiPromptOutput>? OutputTemplate { get; set; }

    /// <summary>Turns off the prompt box, the suggestions and generating. A response already generating can still be
    /// stopped.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words it shows and reads out, in place of the app's text for the <see cref="TbStrings"/> keys.
    /// </summary>
    [Parameter]
    public AiPromptLabels Labels { get; set; } = new();

    /// <summary>A label this prompt's use is counted under, such as <c>"trail-help"</c>, so the app can see how people
    /// use it through the <c>Trailblazor</c> meter: counted in <c>trailblazor.control.shown</c> when it first renders,
    /// and in <c>trailblazor.control.used</c> for each generate, retry, stop, copy, insert and suggestion. Nothing the
    /// user writes or the model returns is recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private IBrowserService Browser { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private bool Generating => _generating is not null;

    private string Text(string? given, string key) => given ?? Localizer[key];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException($"{nameof(TbAiPrompt)} needs a {nameof(Label)}.");
        }

        if (Generate is null)
        {
            throw new InvalidOperationException($"{nameof(TbAiPrompt)} needs a {nameof(Generate)} function.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        Telemetry.Rendered(TelemetryName);
        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/ai-prompt.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("createAiPrompt", _root);
            if (_disposed && handle is not null)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
            StateHasChanged();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the prompt went with it.
        }
    }

    // Built in C# for SetUpdatesAttributeName("value"), so a render for an earlier keystroke can't drop fast-typed
    // characters, and so the handler stays the same across renders.
    private RenderFragment Box => builder =>
    {
        builder.OpenElement(0, "textarea");
        builder.AddAttribute(1, "id", $"{_id}-input");
        builder.AddAttribute(2, "data-part", "input");
        builder.AddAttribute(3, "rows", 2);
        builder.AddAttribute(4, "placeholder", Placeholder);
        builder.AddAttribute(5, "disabled", Disabled);
        builder.AddAttribute(6, "class", "tb-ai-prompt__input");
        builder.AddAttribute(7, "value", _draft);
        builder.AddAttribute(8, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, Type));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    };

    private void Type(ChangeEventArgs args) => _draft = args.Value as string ?? "";

    private void Suggest(string suggestion)
    {
        _draft = suggestion;
        Telemetry.Used(TelemetryName, "suggestion");
    }

    // Counts every submit, refused or not, once it's done, so a test can wait for the response.
    private async Task SubmitAsync()
    {
        var prompt = _draft.Trim();
        try
        {
            if (Generating || Disabled || prompt.Length == 0)
            {
                return;
            }

            _draft = "";
            _outputs.Insert(0, (_keys++, new(prompt, "", AiPromptStatus.Generating)));
            Telemetry.Used(TelemetryName, "generate");
            await RunAsync(0);
        }
        finally
        {
            _commits++;
        }
    }

    private async Task RetryAsync(int index)
    {
        try
        {
            if (Generating || Disabled)
            {
                return;
            }

            _outputs[index] = (_outputs[index].Key, new(_outputs[index].Output.Prompt, "", AiPromptStatus.Generating));
            Telemetry.Used(TelemetryName, "retry");
            await RunAsync(index);
        }
        finally
        {
            _commits++;
        }
    }

    // Streams one response into its place; nothing else can generate meanwhile, so the index stays put.
    private async Task RunAsync(int index)
    {
        using var generating = _generating = new CancellationTokenSource();
        var (key, output) = _outputs[index];
        var text = new StringBuilder();
        Announcer.Polite(Text(Labels.Generating, TbStrings.AiPromptGenerating));
        try
        {
            await foreach (var chunk in Generate!(output.Prompt, generating.Token).WithCancellation(generating.Token))
            {
                // A model that doesn't watch the token still stops at its next chunk.
                generating.Token.ThrowIfCancellationRequested();
                text.Append(chunk);
                _outputs[index] = (key, output with { Text = text.ToString() });
                StateHasChanged();
            }

            _outputs[index] = (key, output with { Text = text.ToString(), Status = AiPromptStatus.Done });
            Announcer.Polite(Text(Labels.Done, TbStrings.AiPromptDone));
        }
        // A response the user stopped is marked Stopped, not Failed, even when the model reports the cancel its own
        // way; one cut short by the prompt going away isn't read out at all.
        catch (Exception) when (generating.IsCancellationRequested)
        {
            if (_disposed)
            {
                return;
            }

            _outputs[index] = (key, output with { Text = text.ToString(), Status = AiPromptStatus.Stopped });
            Announcer.Polite(Text(Labels.Stopped, TbStrings.AiPromptStopped));
        }
        catch (Exception exception)
        {
            // Kept on the response rather than rethrown, which would end a Blazor Server circuit.
            Services.GetService<ILogger<TbAiPrompt>>()?.LogWarning(exception,
                "The AI prompt {Name} couldn't generate a response.", TelemetryName ?? Label);
            _outputs[index] = (key, output with
            {
                Text = text.ToString(), Status = AiPromptStatus.Failed, Error = exception,
            });
            Announcer.Polite(Text(Labels.Failed, TbStrings.AiPromptFailed));
        }
        finally
        {
            _generating = null;
        }
    }

    private void StopIfGenerating()
    {
        if (_generating is null)
        {
            return;
        }

        Telemetry.Used(TelemetryName, "stop");
        _generating.Cancel();
    }

    private async Task CopyAsync(AiPromptOutput output)
    {
        Telemetry.Used(TelemetryName, "copy");
        if (await Browser.CopyTextAsync(output.Text))
        {
            Announcer.Polite(Text(Labels.Copied, TbStrings.AiPromptCopied));
        }
    }

    private async Task InsertAsync(AiPromptOutput output)
    {
        Telemetry.Used(TelemetryName, "insert");
        await OnInsert.InvokeAsync(output);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _generating?.Cancel();
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
