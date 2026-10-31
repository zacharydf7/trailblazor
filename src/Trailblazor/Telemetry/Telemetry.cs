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

    // A control is shown once for each name it takes, from its first interactive render; a static page never reports.
    public void ShownOnce(ref string? shownAs, string? telemetryName, Type component)
    {
        if (telemetryName is not null && telemetryName != shownAs)
        {
            shownAs = telemetryName;
            Add(instruments.ControlsShown, telemetryName, component);
        }
    }

    public void ControlUsed(string telemetryName, Type component) =>
        Add(instruments.ControlsUsed, telemetryName, component);

    // Nothing is built when no one listens, so an unwatched control costs a check.
    private void Add(Counter<long> counter, string telemetryName, Type component)
    {
        if (!counter.Enabled || !options.Value.Telemetry.Enabled)
        {
            return;
        }

        var tags = new TagList { { NameTag, telemetryName }, { ComponentTag, NameOf(component) } };
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
