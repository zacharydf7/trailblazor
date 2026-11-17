using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Contract;

// Every component that renders alone and takes a TelemetryName, apart from those in UsesOnly, counts itself shown
// under it, checked by reflection; one that renders only inside a parent is covered by its parent's tests.
public class TelemetryContractTests : BunitContext
{
    // Components rendered alone that take a TelemetryName but count only uses, with the reason.
    private static readonly Dictionary<Type, string> UsesOnly = new()
    {
        [typeof(TbMenuItem)] = "it renders hidden until its menu opens, which the menu counts",
        [typeof(TbMenuCheckboxItem)] = "it renders hidden until its menu opens, which the menu counts",
    };

    public TelemetryContractTests() => this.AddTrailblazor();

    public static TheoryData<Type> Named => new(ComponentContractTests.Components.Select(row => row.Data)
        .Where(type => type.GetProperty(nameof(TbButton.TelemetryName)) is not null && !UsesOnly.ContainsKey(type)));

    [Theory, MemberData(nameof(Named))]
    public void TelemetryName_ShouldCountTheComponentShown_WhenItHasATelemetryName(Type component)
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");

        Render(builder =>
        {
            builder.OpenComponent(0, component);
            builder.AddAttribute(1, nameof(TbButton.TelemetryName), "contract-name");
            var sequence = 2;
            if (ComponentContractTests.Setup.TryGetValue(component, out var setup))
            {
                builder.AddAttribute(sequence++, setup.Name, setup.Value);
            }

            foreach (var (name, value) in ComponentContractTests.RequiredParameters(component))
            {
                builder.AddAttribute(sequence++, name, value);
            }

            builder.CloseComponent();
        });

        Assert.Equal(["contract-name"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }
}
