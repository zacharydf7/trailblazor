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

    public static IReadOnlyList<TelemetryInstrument> Instruments { get; } =
    [
        new("trailblazor.control.shown", "Counter", "{control}",
            "Controls with a TelemetryName that rendered, counted once each.", CommonTags,
            "A control with a TelemetryName first renders interactively, and again when it takes another name."),
        new("trailblazor.control.used", "Counter", "{use}",
            "Uses of controls with a TelemetryName, such as clicks and choices.", [.. CommonTags, ActionTag],
            "A control with a TelemetryName is used: clicked, chosen, changed or selected while it is enabled. A data "
            + "grid, tree view, chip or select also says what it did, as trailblazor.action."),
    ];
}
