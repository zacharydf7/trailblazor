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

    public void ControlShown(string telemetryName, Type component) =>
        Add(instruments.ControlsShown, telemetryName, component, null);

    // A component with more than one thing to do, such as a grid that sorts and pages, says which it did.
    public void ControlUsed(string telemetryName, Type component, string? action = null) =>
        Add(instruments.ControlsUsed, telemetryName, component, action);

    // Nothing is built when no one listens, so an unwatched control costs a check.
    private void Add(Counter<long> counter, string telemetryName, Type component, string? action)
    {
        if (!counter.Enabled || !options.Value.Telemetry.Enabled)
        {
            return;
        }

        var tags = new TagList { { NameTag, telemetryName }, { ComponentTag, NameOf(component) } };
        if (action is not null)
        {
            tags.Add(ActionTag, action);
        }

        foreach (var source in appTags)
        {
            foreach (var tag in source.Tags)
            {
                tags.Add(tag);
            }
        }

        counter.Add(1, tags);
    }

    // A generic component's type name ends in its arity, as TbSelect`1 does.
    private static string NameOf(Type component) => component.Name.Split('`')[0];
}
