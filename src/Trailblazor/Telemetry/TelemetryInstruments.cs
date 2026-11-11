using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Trailblazor;

// The Trailblazor meter and its instruments, one set per app. The factory owns the meter, so the app's container
// disposes it, and each test's container gets its own.
internal sealed class TelemetryInstruments
{
    public const string MeterName = "Trailblazor";

    public TelemetryInstruments(IMeterFactory meters, IOptions<TrailblazorOptions> options)
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
        meter.CreateObservableGauge("trailblazor.control.catalog",
            () => options.Value.Telemetry.Enabled ? Entries.Value : [], "{control}",
            "Controls with a TelemetryName that the app has, 1 each, whether or not they have been shown.");
    }

    private static readonly Lazy<Measurement<int>[]> Entries = new(() =>
    [
        .. AppAssemblies(AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic))
            .SelectMany(EntriesOf).DistinctBy(entry => (entry.Name, entry.Component))
            .Select(entry => new Measurement<int>(1, new(Telemetry.NameTag, entry.Name),
                new(Telemetry.ComponentTag, entry.Component))),
    ]);

    // An assembly loads only once its code runs, so a library nobody has reached yet is found through the references.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The catalog reports the controls it can find; "
        + "an assembly trimmed away has none to report.")]
    internal static List<Assembly> AppAssemblies(IEnumerable<Assembly> loaded)
    {
        var found = new List<Assembly>();
        var seen = new HashSet<string>();
        var waiting = new Queue<Assembly>(loaded);
        while (waiting.TryDequeue(out var assembly))
        {
            if (!seen.Add(assembly.FullName!) || IsFramework(assembly.GetName()))
            {
                continue;
            }

            found.Add(assembly);
            foreach (var name in assembly.GetReferencedAssemblies())
            {
                if (!seen.Contains(name.FullName) && !IsFramework(name) && Load(name) is { } referenced)
                {
                    waiting.Enqueue(referenced);
                }
            }
        }

        return found;
    }

    private static bool IsFramework(AssemblyName name) => name.Name is "mscorlib" or "netstandard"
        || name.Name?.StartsWith("System.", StringComparison.Ordinal) == true || name.Name == "System"
        || name.Name?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;

    // A reference that isn't there yet, such as a WebAssembly assembly loaded lazily, stays out of the catalog.
    private static Assembly? Load(AssemblyName name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
    }

    // An assembly whose attributes can't be read, such as one naming a type that is missing, lists nothing.
    private static IEnumerable<TelemetryCatalogEntryAttribute> EntriesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetCustomAttributes<TelemetryCatalogEntryAttribute>();
        }
        catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            return [];
        }
    }

    public Counter<long> ControlsShown { get; }

    public Counter<long> ControlsUsed { get; }

    public Counter<long> DialogsClosed { get; }

    public Histogram<double> DialogDuration { get; }

    public Counter<long> ToastsClosed { get; }

    public Counter<long> FormsSubmitted { get; }

    public Counter<long> FieldsInvalid { get; }
}
