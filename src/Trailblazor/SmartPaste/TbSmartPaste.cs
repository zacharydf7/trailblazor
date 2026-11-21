using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A button that fills its form's fields from text on the clipboard, such as an email signature, through a
/// model the app provides. Where the clipboard can't be read, it asks for the text in a box instead. It goes inside a
/// <see cref="TbForm"/>.</summary>
public partial class TbSmartPaste
{
    private readonly string _textId = $"tb-smart-paste-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource _gone = new();
    private string _draft = "";
    private string? _status;
    private bool _boxOpen;
    private bool _busy;

    /// <summary>Reads the form's values out of the pasted text, such as by asking a language model. It gets the text
    /// and the fields it can fill, and returns each value it finds as text under its field's
    /// <see cref="SmartPasteField.Name"/>. Write numbers and dates in the invariant format, such as <c>1234.5</c>,
    /// <c>2026-11-17</c> and <c>2026-11-17T14:30</c>, whatever the user's culture; a date-time keeps the time as
    /// written, whatever offset follows it. For a field with <see cref="SmartPasteField.Choices"/>, answer with one
    /// of them. A field that is <see cref="SmartPasteField.Multiple"/> takes one value on each line: several of its
    /// choices, or the tags of a tag input. A field left out of the answer, or given a value it can't take, stays as
    /// it was. Required.</summary>
    [Parameter, EditorRequired]
    public Func<SmartPasteRequest, CancellationToken, Task<IReadOnlyDictionary<string, string>>>? Extract { get; set; }

    /// <summary>The button's text. Defaults to "Smart paste".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Turns the button off. A <see cref="TbForm"/> with <see cref="TbForm.Disabled"/> turns it off too.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words it shows and says, in place of the app's text for the <see cref="TbStrings"/> keys.
    /// </summary>
    [Parameter]
    public SmartPasteLabels Labels { get; set; } = new();

    /// <summary>A label this button's use is counted under, such as <c>"contact-paste"</c>, so the app can see how
    /// often people use it through the <c>Trailblazor</c> meter: counted in <c>trailblazor.control.shown</c> when it
    /// first renders, and in <c>trailblazor.control.used</c> each time it's pressed. Nothing pasted or filled is
    /// recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [CascadingParameter]
    private TbForm? Form { get; set; }

    [CascadingParameter(Name = TbForm.DisabledCascade)]
    private bool FormDisabled { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private string Text(string? given, string key) => given ?? Localizer[key];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Form is null)
        {
            throw new InvalidOperationException($"{nameof(TbSmartPaste)} goes inside a {nameof(TbForm)}.");
        }

        if (Extract is null)
        {
            throw new InvalidOperationException($"{nameof(TbSmartPaste)} needs an {nameof(Extract)} function.");
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Telemetry.Rendered(TelemetryName);
        }
    }

    // Busy from the press on, so a second press while the browser asks about the clipboard starts nothing.
    private async Task PasteAsync()
    {
        Telemetry.Used(TelemetryName, "paste");
        _busy = true;
        var text = await ReadClipboardAsync();
        _busy = false;
        if (string.IsNullOrWhiteSpace(text))
        {
            _boxOpen = true;
            return;
        }

        await FillAsync(text);
    }

    // Null where the browser won't share the clipboard, as when the user refuses, so the box asks for the text. A
    // prompt left unanswered waits until the button goes away, rather than timing out.
    private async Task<string?> ReadClipboardAsync()
    {
        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/smart-paste.js");
            return await module.InvokeAsync<string?>("readClipboard", _gone.Token);
        }
        catch (JSException exception)
        {
            // The script itself failed, such as when it wasn't served; the box still works.
            Services.GetService<ILogger<TbSmartPaste>>()?.LogWarning(exception,
                "Smart paste {Name} couldn't read the clipboard.", TelemetryName);
            return null;
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
            return null;
        }
    }

    private void SetBoxOpen(bool open)
    {
        _boxOpen = open;
        _draft = "";
    }

    private async Task FillFromBoxAsync()
    {
        var text = _draft;
        SetBoxOpen(false);
        await FillAsync(text);
    }

    private async Task FillAsync(string text)
    {
        var fields = Named(Form!.Fields);
        if (string.IsNullOrWhiteSpace(text) || fields.Count == 0)
        {
            Report(Labels.Nothing, TbStrings.SmartPasteNothing);
            return;
        }

        _busy = true;
        Report(Labels.Filling, TbStrings.SmartPasteFilling);
        try
        {
            var values = await Extract!(new(text, [.. fields.Values.Select(named => named.Field)]), _gone.Token);
            var filled = 0;
            foreach (var (name, value) in values)
            {
                if (!string.IsNullOrWhiteSpace(value) && fields.TryGetValue(name, out var named)
                    && await named.Member.PasteAsync(value))
                {
                    filled++;
                }
            }

            Report(filled == 0 ? Text(Labels.Nothing, TbStrings.SmartPasteNothing)
                : string.Format(CultureInfo.CurrentCulture, (filled == 1 ? Labels.FilledOne : Labels.Filled)
                    ?? Localizer.Plural(TbStrings.SmartPasteFilled, filled), filled));
        }
        // Cut short only when the button goes away, with nothing left to report to.
        catch (Exception) when (_gone.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            // Shown beside the button rather than rethrown, which would end a Blazor Server circuit.
            Services.GetService<ILogger<TbSmartPaste>>()?.LogWarning(exception,
                "Smart paste {Name} couldn't fill the form.", TelemetryName);
            Report(Labels.Failed, TbStrings.SmartPasteFailed);
        }
        finally
        {
            _busy = false;
        }
    }

    // The fields it can fill, by the name the model sees; a label two fields share gets a number from the second on.
    private static Dictionary<string, (IFormField Member, SmartPasteField Field)> Named(IEnumerable<IFormField> members)
    {
        var named = new Dictionary<string, (IFormField, SmartPasteField)>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in members)
        {
            if (member.DescribeForPaste() is not { } field)
            {
                continue;
            }

            var name = field.Name;
            for (var number = 2; named.ContainsKey(name); number++)
            {
                name = $"{field.Name} ({number})";
            }

            named[name] = (member, field with { Name = name });
        }

        return named;
    }

    private void Report(string? given, string key) => Report(Text(given, key));

    private void Report(string status)
    {
        _status = status;
        Announcer.Polite(status);
    }

    /// <inheritdoc />
    public void Dispose() => _gone.Cancel();
}
