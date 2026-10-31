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

    public static IReadOnlyList<TelemetryInstrument> Instruments { get; } =
    [
        new("trailblazor.control.shown", "Counter", "{control}",
            "Controls with a TelemetryName that rendered, counted once each.", CommonTags,
            "A control with a TelemetryName first renders interactively, and again when it takes another name."),
        new("trailblazor.control.used", "Counter", "{use}",
            "Uses of controls with a TelemetryName, such as clicks and choices.", CommonTags,
            "A control with a TelemetryName is used: a button is clicked while it is enabled and not loading."),
    ];
}
