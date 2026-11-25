using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trailblazor;

// What a control with a TelemetryName reports. The service is looked up only once a control has a name, so an unnamed
// control renders without AddTrailblazor, as controls always have.
internal sealed class ControlTelemetry(IServiceProvider services, Type component)
{
    private string? _shownAs;
    private Telemetry? _telemetry;
    private bool _lookedUp;

    // A control counts as shown once per name it takes, from its first interactive render; a static page never reports.
    public void Rendered(string? telemetryName)
    {
        if (telemetryName is not null && telemetryName != _shownAs)
        {
            _shownAs = telemetryName;
            Service?.ControlShown(telemetryName, component);
        }
    }

    public void Used(string? telemetryName, string? action = null, string? input = null)
    {
        if (telemetryName is not null)
        {
            Service?.ControlUsed(telemetryName, component, action, input);
        }
    }

    // A failed load or search is logged for every control, since an app that binds no failure callback would never
    // hear of it otherwise, and counted for a control with a TelemetryName. The label names an unnamed control.
    public void Failed(string? telemetryName, string? label, string action, Exception exception)
    {
        services.GetService<ILoggerFactory>()?.CreateLogger(component).LogWarning(exception,
            "{Component} {Name} failed to {Action}.", Telemetry.NameOf(component),
            telemetryName ?? label ?? "(unnamed)", action);
        if (telemetryName is not null)
        {
            Service?.ControlFailed(telemetryName, component, action);
        }
    }

    public void Loaded(string? telemetryName, string outcome, TimeSpan took)
    {
        if (telemetryName is not null)
        {
            Service?.LoadFinished(telemetryName, component, outcome, took);
        }
    }

    public void Invalid(string? telemetryName)
    {
        if (telemetryName is not null)
        {
            Service?.FieldInvalid(telemetryName, component);
        }
    }

    public void Submitted(string? telemetryName, bool valid)
    {
        if (telemetryName is not null)
        {
            Service?.FormSubmitted(telemetryName, component, valid);
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
