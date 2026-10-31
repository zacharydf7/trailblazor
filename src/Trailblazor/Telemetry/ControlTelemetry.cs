using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor;

// What a control with a TelemetryName reports. The service is looked up only once a control has a name, so an unlabeled
// control renders without AddTrailblazor, as controls always have.
internal sealed class ControlTelemetry(IServiceProvider services, Type component)
{
    private string? _shownAs;
    private Telemetry? _telemetry;
    private bool _lookedUp;

    // A control is shown once for each name it takes, from its first interactive render; a static page never reports.
    public void Rendered(string? telemetryName)
    {
        if (telemetryName is not null && telemetryName != _shownAs)
        {
            _shownAs = telemetryName;
            Service?.ControlShown(telemetryName, component);
        }
    }

    public void Used(string? telemetryName, string? action = null)
    {
        if (telemetryName is not null)
        {
            Service?.ControlUsed(telemetryName, component, action);
        }
    }

    private Telemetry? Service
    {
        get
        {
            if (!_lookedUp)
            {
                _lookedUp = true;
                _telemetry = services.GetService<Telemetry>();
            }

            return _telemetry;
        }
    }
}
