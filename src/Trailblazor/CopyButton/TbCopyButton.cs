using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor;

/// <summary>A button that copies <see cref="Text"/> to the clipboard, then shows and says "Copied" for a moment, or
/// "Couldn't copy" when the browser refuses. Give it the text to copy before it's pressed: Safari refuses a copy that
/// waits for anything, such as a download, after the press.</summary>
public partial class TbCopyButton : IDisposable
{
    // How long it shows how the copy went before it offers to copy again.
    private static readonly TimeSpan ShownFor = TimeSpan.FromSeconds(2);

    private ITimer? _reset;
    private bool? _copied;
    // How many copies have finished, so a test waits for its own press rather than an earlier result.
    private int _copies;
    private bool _disposed;

    /// <summary>The text it copies. Required.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    /// <summary>Shows only the icon, named by <see cref="CopyButtonLabels.Copy"/>, with that name in a tooltip, as in a
    /// field or a code block's corner.</summary>
    [Parameter]
    public bool IconOnly { get; set; }

    /// <summary>How the button looks. Defaults to <see cref="Variant.Text"/>.</summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Text;

    /// <summary>The button's size. Defaults to <see cref="Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>Turns the button off.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The words it shows and says, in place of the app's text for the <see cref="TbStrings"/> keys.</summary>
    [Parameter]
    public CopyButtonLabels Labels { get; set; } = new();

    /// <summary>A label this button is counted under, such as <c>"install-command"</c>, through the
    /// <c>Trailblazor</c> meter: in <c>trailblazor.control.shown</c> when it first renders interactively, and in
    /// <c>trailblazor.control.used</c> with the action <c>copied</c> or <c>failed</c>. The text it copies is never
    /// recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IBrowserService Browser { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private TimeProvider Time => Services.GetService<TimeProvider>() ?? TimeProvider.System;

    // Copying runs in the browser, so before the page is interactive the button does nothing and is off.
    private bool Unusable => Disabled || !RendererInfo.IsInteractive;

    private string CopyText => Labels.Copy ?? Localizer[TbStrings.CopyButtonCopy];

    private string CopiedText => Labels.Copied ?? Localizer[TbStrings.CopyButtonCopied];

    private string FailedText => Labels.Failed ?? Localizer[TbStrings.CopyButtonFailed];

    private string ShownLabel => _copied switch { true => CopiedText, false => FailedText, null => CopyText };

    private Icon ShownIcon => _copied switch
    {
        true => BuiltInIcons.Check,
        false => BuiltInIcons.Error,
        null => BuiltInIcons.Copy,
    };

    private string StateName => _copied switch { true => "copied", false => "failed", null => "idle" };

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    private async Task CopyAsync()
    {
        var copied = await Browser.CopyTextAsync(Text);
        if (_disposed)
        {
            return;
        }

        (_copied, _copies) = (copied, _copies + 1);
        Announcer.Polite(copied ? CopiedText : FailedText);
        Telemetry.Used(TelemetryName, copied ? "copied" : "failed");
        _reset?.Dispose();
        _reset = Time.CreateTimer(_ => _ = InvokeAsync(Reset), null, ShownFor, Timeout.InfiniteTimeSpan);
    }

    private void Reset()
    {
        // A reset queued before the button went away can run after it.
        if (!_disposed)
        {
            _copied = null;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _reset?.Dispose();
    }
}
