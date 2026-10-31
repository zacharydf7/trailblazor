using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Trailblazor;

// SPIKE (#101): what the components report, through System.Diagnostics only. Nothing is recorded until an app listens,
// such as OpenTelemetry with AddMeter("Trailblazor") and AddSource("Trailblazor").
internal static class TrailblazorTelemetry
{
    public const string Name = "Trailblazor";

    public static readonly Meter Meter = new(Name);

    public static readonly ActivitySource Source = new(Name);

    public static readonly Counter<long> DialogsClosed = Meter.CreateCounter<long>("trailblazor.dialog.closed",
        description: "Dialogs closed, by dialog and by how they closed.");

    public static readonly Histogram<double> DialogDuration = Meter.CreateHistogram<double>(
        "trailblazor.dialog.duration", unit: "s", description: "How long dialogs stayed open before they closed.");

    public static readonly Counter<long> FeaturesShown = Meter.CreateCounter<long>("trailblazor.feature.shown",
        description: "Controls with a feature name that rendered, once each.");

    public static readonly Counter<long> FeaturesUsed = Meter.CreateCounter<long>("trailblazor.feature.used",
        description: "Uses of controls with a feature name.");

    public static void FeatureShown(string feature, string component)
    {
        if (FeaturesShown.Enabled)
        {
            FeaturesShown.Add(1, new("tb.feature", feature), new("tb.component", component));
        }
    }

    public static void FeatureUsed(string feature, string component)
    {
        if (FeaturesUsed.Enabled)
        {
            FeaturesUsed.Add(1, new("tb.feature", feature), new("tb.component", component));
        }
    }
}
