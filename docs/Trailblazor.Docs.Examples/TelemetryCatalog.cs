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

    // How a dialog, toast or form submit ended.
    public const string OutcomeTag = "trailblazor.outcome";

    public static IReadOnlyList<TelemetryInstrument> Instruments { get; } =
    [
        new("trailblazor.control.shown", "Counter", "{control}",
            "Controls with a TelemetryName that rendered, counted once each.", CommonTags,
            "A control with a TelemetryName first renders interactively, and again when it takes another name; a "
            + "labeled dialog or toast each time it opens."),
        new("trailblazor.control.used", "Counter", "{use}",
            "Uses of controls with a TelemetryName, such as clicks and choices.", [.. CommonTags, ActionTag, InputTag],
            "A control with a TelemetryName is used: clicked, chosen, changed or selected while it is enabled. A data "
            + "grid, tree view, chip or select also says what it did, as trailblazor.action, and a choice in a select "
            + "or list box, or of an option, whether a pointer or the keyboard made it, as trailblazor.input."),
        new("trailblazor.dialog.closed", "Counter", "{dialog}",
            "Dialogs with a TelemetryName that closed, by how they closed.", [.. CommonTags, OutcomeTag],
            "A dialog with a TelemetryName closes: confirmed, cancelled, or dismissed by escape, backdrop or "
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
    ];
}
