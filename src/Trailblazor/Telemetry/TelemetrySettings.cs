namespace Trailblazor;

/// <summary>App-wide telemetry settings, set in <c>AddTrailblazor(options => ...)</c>. Trailblazor's components report
/// through a meter named <c>Trailblazor</c>, which records nothing until the app listens to it, such as with
/// <c>AddMeter("Trailblazor")</c> in its OpenTelemetry setup.</summary>
public sealed class TelemetrySettings
{
    /// <summary>Whether the components report at all. Defaults to true; turn it off for an app that listens to every
    /// meter but doesn't want its UI usage recorded.</summary>
    public bool Enabled { get; set; } = true;
}
