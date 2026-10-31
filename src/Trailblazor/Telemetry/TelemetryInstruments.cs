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
        DialogsClosed = meter.CreateCounter<long>("trailblazor.dialog.closed", "{dialog}",
            "Dialogs with a TelemetryName that closed, by how they closed.");
        DialogDuration = meter.CreateHistogram<double>("trailblazor.dialog.duration", "s",
            "How long dialogs with a TelemetryName stayed open, by how they closed.");
        ToastsClosed = meter.CreateCounter<long>("trailblazor.toast.closed", "{toast}",
            "Toasts with a TelemetryName that closed, by how they closed.");
        FormsSubmitted = meter.CreateCounter<long>("trailblazor.form.submitted", "{submit}",
            "Submits of forms with a TelemetryName, valid or not.");
        FieldsInvalid = meter.CreateCounter<long>("trailblazor.field.invalid", "{field}",
            "Fields with a TelemetryName that had an error when their form was submitted.");
    }

    public Counter<long> ControlsShown { get; }

    public Counter<long> ControlsUsed { get; }

    public Counter<long> DialogsClosed { get; }

    public Histogram<double> DialogDuration { get; }

    public Counter<long> ToastsClosed { get; }

    public Counter<long> FormsSubmitted { get; }

    public Counter<long> FieldsInvalid { get; }
}
