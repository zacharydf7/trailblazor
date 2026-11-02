namespace Trailblazor;

/// <summary>App-wide settings, passed to <c>AddTrailblazor(options => ...)</c>.</summary>
public sealed class TrailblazorOptions
{
    /// <summary>The theme and palette a page starts with, and whether the user's choice is remembered.</summary>
    public ThemeSettings Theme { get; } = new();

    /// <summary>Where toasts appear, how many show at once, and how long they stay.</summary>
    public ToastSettings Toasts { get; } = new();

    /// <summary>Defaults for dialogs whose own options leave a setting unset.</summary>
    public DialogSettings Dialogs { get; } = new();

    /// <summary>How long tooltips wait before they appear and disappear.</summary>
    public TooltipSettings Tooltips { get; } = new();

    /// <summary>Whether the components report what people see and use through the <c>Trailblazor</c> meter.</summary>
    public TelemetrySettings Telemetry { get; } = new();

    /// <summary>Text for built-in strings in place of the English, by the keys in <see cref="TbStrings"/>, for the
    /// default <see cref="TbLocalizer"/>. An app that registers its own <see cref="ITbLocalizer"/> applies them itself.
    /// </summary>
    public IDictionary<string, string> Strings { get; } = new Dictionary<string, string>();
}
