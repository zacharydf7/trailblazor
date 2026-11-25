using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace Trailblazor;

// What components call to report, with the app's own tags added. Scoped, so tags can depend on the user.
internal sealed class Telemetry(TelemetryInstruments instruments, IOptions<TrailblazorOptions> options,
    IEnumerable<ITelemetryTags> appTags)
{
    public const string NameTag = "trailblazor.telemetry_name";

    public const string ComponentTag = "trailblazor.component";

    public const string ActionTag = "trailblazor.action";

    public const string InputTag = "trailblazor.input";

    public const string OutcomeTag = "trailblazor.outcome";

    public const string ItemsTag = "trailblazor.items";

    // One source for the whole process, as ActivitySource is meant to be used; an app listens with AddSource.
    internal static readonly ActivitySource Spans = new(TelemetryInstruments.MeterName);

    // Toasts come from a service, not a component, so they share one component name.
    private const string ToastComponent = "Toast";

    public void ControlShown(string telemetryName, Type component) =>
        Add(instruments.ControlsShown, 1, telemetryName, NameOf(component));

    public void ToastShown(string telemetryName) =>
        Add(instruments.ControlsShown, 1, telemetryName, ToastComponent);

    // A component with more than one thing to do, such as a grid that sorts and pages, says which it did, and a
    // choice says whether a pointer or the keyboard made it.
    public void ControlUsed(string telemetryName, Type component, string? action = null, string? input = null) =>
        Add(instruments.ControlsUsed, 1, telemetryName, NameOf(component), (ActionTag, action), (InputTag, input));

    // The action says what failed: a load or a search.
    public void ControlFailed(string telemetryName, Type component, string action) =>
        Add(instruments.ControlsFailed, 1, telemetryName, NameOf(component), (ActionTag, action));

    public void LoadFinished(string telemetryName, Type component, string outcome, TimeSpan took) =>
        Add(instruments.LoadDuration, took.TotalSeconds, telemetryName, NameOf(component), (OutcomeTag, outcome));

    public void DialogClosed(string telemetryName, Type component, string outcome, TimeSpan open)
    {
        Add(instruments.DialogsClosed, 1, telemetryName, NameOf(component), (OutcomeTag, outcome));
        Add(instruments.DialogDuration, open.TotalSeconds, telemetryName, NameOf(component), (OutcomeTag, outcome));
    }

    public void ToastClosed(string telemetryName, string outcome) =>
        Add(instruments.ToastsClosed, 1, telemetryName, ToastComponent, (OutcomeTag, outcome));

    public void FormSubmitted(string telemetryName, Type component, bool valid) =>
        Add(instruments.FormsSubmitted, 1, telemetryName, NameOf(component),
            (OutcomeTag, valid ? "valid" : "invalid"));

    public void FieldInvalid(string telemetryName, Type component) =>
        Add(instruments.FieldsInvalid, 1, telemetryName, NameOf(component));

    // A span around a call to the app's data, such as a grid's DataProvider, so the app's own database or HTTP spans
    // nest under it. Nothing is started when no one listens.
    public Activity? StartSpan(string telemetryName, Type component, string action)
    {
        if (!Spans.HasListeners() || !options.Value.Telemetry.Enabled
            || Spans.StartActivity($"{NameOf(component)} {action}") is not { } span)
        {
            return null;
        }

        span.SetTag(NameTag, telemetryName).SetTag(ComponentTag, NameOf(component)).SetTag(ActionTag, action);
        foreach (var source in appTags)
        {
            foreach (var tag in source.Tags)
            {
                span.SetTag(tag.Key, tag.Value);
            }
        }

        return span;
    }

    // Nothing is built when no one listens, so an unwatched control costs a check.
    private void Add<T>(Instrument<T> instrument, T value, string telemetryName, string component,
        params ReadOnlySpan<(string Key, string? Value)> extra) where T : struct
    {
        if (!instrument.Enabled || !options.Value.Telemetry.Enabled)
        {
            return;
        }

        var tags = new TagList { { NameTag, telemetryName }, { ComponentTag, component } };
        foreach (var (key, tagValue) in extra)
        {
            if (tagValue is not null)
            {
                tags.Add(key, tagValue);
            }
        }

        foreach (var source in appTags)
        {
            foreach (var tag in source.Tags)
            {
                tags.Add(tag);
            }
        }

        switch (instrument)
        {
            case Counter<T> counter:
                counter.Add(value, tags);
                break;
            case Histogram<T> histogram:
                histogram.Record(value, tags);
                break;
        }
    }

    // A generic component's type name ends in its arity, as TbSelect`1 does.
    internal static string NameOf(Type component) => component.Name.Split('`')[0];
}
