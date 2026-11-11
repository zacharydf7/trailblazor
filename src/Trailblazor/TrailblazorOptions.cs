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

    /// <summary>Whether a page, once interactive, warns in the browser's console about another stylesheet from the
    /// same origin, such as a CSS reset, whose rules outside a cascade layer override Trailblazor's on every element or
    /// on buttons, labels, legends or headings. Defaults to true; turn it off once the warning is dealt with, or when a
    /// stylesheet overrides them on purpose.</summary>
    public bool WarnAboutUnlayeredStyles { get; set; } = true;

    /// <summary>Whether a palette built from <see cref="Palette.Seeds"/> logs a warning, once per app, for each pair of
    /// its colors under the WCAG contrast minimum. Defaults to true; set it to
    /// <c>builder.Environment.IsDevelopment()</c> to warn only in Development.</summary>
    public bool WarnAboutContrast { get; set; } = true;

    /// <summary>Text for built-in strings in place of the English, by the keys in <see cref="TbStrings"/>, for the
    /// default <see cref="TbLocalizer"/>. An app that registers its own <see cref="ITbLocalizer"/> applies them itself.
    /// </summary>
    public IDictionary<string, string> Strings { get; } = new Dictionary<string, string>();
}
