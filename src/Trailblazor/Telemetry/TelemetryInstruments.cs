using System.Diagnostics.Metrics;

namespace Trailblazor;

// The Trailblazor meter and its instruments, one set per app. The factory owns the meter, so the app's container
// disposes it, and each test's container gets its own.
internal sealed class TelemetryInstruments
{
    public const string MeterName = "Trailblazor";

    public TelemetryInstruments(IMeterFactory meters)
    {
        var meter = meters.Create(MeterName);
        ControlsShown = meter.CreateCounter<long>("trailblazor.control.shown", "{control}",
            "Controls with a TelemetryName that rendered, counted once each.");
        ControlsUsed = meter.CreateCounter<long>("trailblazor.control.used", "{use}",
            "Uses of controls with a TelemetryName, such as clicks and choices.");
    }

    public Counter<long> ControlsShown { get; }

    public Counter<long> ControlsUsed { get; }
}
