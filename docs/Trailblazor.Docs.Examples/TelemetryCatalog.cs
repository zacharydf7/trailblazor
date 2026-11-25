namespace Trailblazor.Docs.Examples;

// An instrument of the Trailblazor meter, as the guide tables it; a test checks it, description too, against the meter.
public sealed record TelemetryInstrument(string Name, string Kind, string Unit, string Description,
    IReadOnlyList<string> Tags, string When);

// Every instrument the Trailblazor meter publishes, for the telemetry guide.
public static class TelemetryCatalog
{
    public const string MeterName = "Trailblazor";

    // The tags every instrument carries, besides any the app adds.
    public static IReadOnlyList<string> CommonTags { get; } = ["trailblazor.telemetry_name", "trailblazor.component"];

    // What a use was, for a component that does more than one thing, such as a grid's sort or page.
    public const string ActionTag = "trailblazor.action";

    // Whether a pointer or the keyboard made a choice in a select or list box, or of an option.
    public const string InputTag = "trailblazor.input";

    // How a dialog, toast, form submit or load ended.
    public const string OutcomeTag = "trailblazor.outcome";

    public static IReadOnlyList<TelemetryInstrument> Instruments { get; } =
    [
        new("trailblazor.control.shown", "Counter", "{control}",
            "Controls with a TelemetryName that rendered, counted once each.", CommonTags,
            "A control with a TelemetryName first renders interactively, and again when it takes another name; a "
            + "labeled dialog or toast each time it opens."),
        new("trailblazor.control.used", "Counter", "{use}",
            "Uses of controls with a TelemetryName, such as clicks and choices.", [.. CommonTags, ActionTag, InputTag],
            "A control with a TelemetryName is used: clicked, chosen, changed, opened, closed or selected while it "
            + "is enabled. Many also say what they did, as trailblazor.action, such as next on a carousel or close "
            + "on an alert, and a choice in a select or list box, or of an option, whether a pointer or the keyboard "
            + "made it, as trailblazor.input."),
        new("trailblazor.control.failed", "Counter", "{failure}",
            "Loads, searches and exports that failed in controls with a TelemetryName.", [.. CommonTags, ActionTag],
            "A data grid's DataProvider throws for its rows, or a tree view's LoadChildren throws, with the action "
            + "load. A data grid's export fails, in its DataProvider or while the file is built, such as when a "
            + "column's field throws, with the action export. An autocomplete's or a mention input's Search, or "
            + "a rich text editor's MentionSearch, throws, with the action search. An async content region's Load "
            + "throws, with the action load. A load or a search that was "
            + "canceled or replaced isn't counted. Each failure is also logged as a warning, whether or not the "
            + "control has a TelemetryName."),
        new("trailblazor.load.duration", "Histogram", "s",
            "How long loads of controls with a TelemetryName took, by whether they loaded or failed.",
            [.. CommonTags, OutcomeTag],
            "A TbAsyncContent with a TelemetryName finishes a load, with the seconds it took and the outcome loaded "
            + "or failed; the time a spinner is held to its minimum isn't counted. A load that was canceled or "
            + "replaced isn't recorded."),
        new("trailblazor.dialog.closed", "Counter", "{dialog}",
            "Dialogs with a TelemetryName that closed, by how they closed.", [.. CommonTags, OutcomeTag],
            "A dialog with a TelemetryName closes: confirmed, canceled, or dismissed by escape, backdrop or "
            + "close_button."),
        new("trailblazor.dialog.duration", "Histogram", "s",
            "How long dialogs with a TelemetryName stayed open, by how they closed.", [.. CommonTags, OutcomeTag],
            "A dialog with a TelemetryName closes, with the seconds it was open."),
        new("trailblazor.toast.closed", "Counter", "{toast}",
            "Toasts with a TelemetryName that closed, by how they closed.", [.. CommonTags, OutcomeTag],
            "A toast with a TelemetryName closes: by its action, dismissed, timed_out, or closed by the app."),
        new("trailblazor.form.submitted", "Counter", "{submit}",
            "Submits of forms with a TelemetryName, valid or not.", [.. CommonTags, OutcomeTag],
            "A TbForm with a TelemetryName validates a submit, valid or invalid."),
        new("trailblazor.field.invalid", "Counter", "{field}",
            "Fields with a TelemetryName that had an error when their form was submitted.", CommonTags,
            "A TbForm's submit is invalid, once for each field with a TelemetryName that has an error."),
        new("trailblazor.control.catalog", "ObservableGauge", "{control}",
            "Controls with a TelemetryName that the app has, 1 each, whether or not they have been shown.",
            CommonTags,
            "Whenever the app's metrics are read, after a control with a TelemetryName has rendered: 1 for each "
            + "literal TelemetryName in the app's markup and each TelemetryCatalogEntry the app adds by hand, shown or not. "
            + "Its component is the one the markup names, such as an app's own wrapper, and it carries none of the "
            + "app's own tags."),
    ];
}
