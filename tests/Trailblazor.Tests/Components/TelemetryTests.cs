using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

// The docs examples' markup lists this name too, so the catalog finds it in two assemblies and must report it once.
[assembly: Trailblazor.TelemetryCatalogEntry("edit-name", "AppButton")]

namespace Trailblazor.Tests.Components;

public class TelemetryTests : BunitContext
{
    // A component nested in a generic type is generic too, though its own name has no arity.
    private static class Toolbar<T>
    {
        public sealed class ExportButton : TbButton;
    }

    private sealed class CustomerTags : ITelemetryTags
    {
        public IEnumerable<KeyValuePair<string, object?>> Tags => [new("customer", "acme")];
    }

    [Fact]
    public void TelemetryName_ShouldCountTheButtonShownOnceAndEachClick_WhenSet()
    {
        Start();
        using var shown = Collect("trailblazor.control.shown");
        using var used = Collect("trailblazor.control.used");
        var cut = RenderButton("export-csv");

        cut.Find("button").Click();
        cut.Find("button").Click();

        var measurement = Assert.Single(shown.GetMeasurementSnapshot());
        Assert.Equal("export-csv", measurement.Tags["trailblazor.telemetry_name"]);
        Assert.Equal("TbButton", measurement.Tags["trailblazor.component"]);
        Assert.Equal(2, used.GetMeasurementSnapshot().Count);
    }

    [Fact]
    public void Catalog_ShouldReportEachControlTheAppHasOnce_WhenCollected()
    {
        Start();

        var catalog = ReadCatalog();

        var entry = Assert.Single(catalog,
            measurement => Equals(measurement.Tags["trailblazor.telemetry_name"], "edit-name"));
        Assert.Equal("AppButton", entry.Tags["trailblazor.component"]);
        Assert.Equal(1, entry.Value);
    }

    [Fact]
    public void Catalog_ShouldReportTheNamesInALibrarysMarkup_WhenTheLibraryIsBuilt()
    {
        Start();

        var catalog = ReadCatalog();

        Assert.Contains(catalog, measurement => Equals(measurement.Tags["trailblazor.telemetry_name"], "trail-name")
            && Equals(measurement.Tags["trailblazor.component"], "AppTextField"));
    }

    [Fact]
    public void AppAssemblies_ShouldReachTheLibrariesAnAssemblyReferences_WhenGivenOnlyThatAssembly()
    {
        var found = TelemetryInstruments.AppAssemblies([typeof(TelemetryCatalog).Assembly]);

        // The examples reference Trailblazor, which references its primitives, so the walk goes two levels deep.
        Assert.Contains(typeof(TbButton).Assembly, found);
        Assert.Contains(typeof(Trailblazor.Primitives.JsModuleLoader).Assembly, found);
        Assert.DoesNotContain(typeof(object).Assembly, found);
    }

    [Fact]
    public void Catalog_ShouldReportNothing_WhenTelemetryIsOff()
    {
        Start(options => options.Telemetry.Enabled = false);

        Assert.Empty(ReadCatalog());
    }

    [Fact]
    public void TelemetryName_ShouldCountTheButtonShownAgain_WhenItTakesAnotherName()
    {
        Start();
        using var shown = Collect("trailblazor.control.shown");
        var cut = RenderButton("export-csv");

        cut.Render(parameters => parameters.Add(button => button.TelemetryName, "export-pdf"));
        cut.Render(parameters => parameters.Add(button => button.Disabled, true));

        Assert.Equal(["export-csv", "export-pdf"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }

    [Fact]
    public void TelemetryName_ShouldNameTheComponentByItsType_WhenItIsNestedInAGenericType()
    {
        Start();
        using var used = Collect("trailblazor.control.used");
        var cut = Render<Toolbar<int>.ExportButton>(parameters => parameters
            .Add(button => button.TelemetryName, "export-csv"));

        cut.Find("button").Click();

        Assert.Equal("ExportButton", Assert.Single(used.GetMeasurementSnapshot()).Tags["trailblazor.component"]);
    }

    [Fact]
    public void TelemetryName_ShouldCountNothing_WhenTheButtonHasNone()
    {
        Start();
        using var shown = Collect("trailblazor.control.shown");
        using var used = Collect("trailblazor.control.used");
        var cut = RenderButton(null);

        cut.Find("button").Click();

        Assert.Empty(shown.GetMeasurementSnapshot());
        Assert.Empty(used.GetMeasurementSnapshot());
    }

    [Fact]
    public void TelemetryName_ShouldNotCountAUse_WhenTheButtonIsDisabled()
    {
        Start();
        using var used = Collect("trailblazor.control.used");
        var cut = RenderButton("export-csv", disabled: true);

        cut.Find("button").Click();

        Assert.Empty(used.GetMeasurementSnapshot());
    }

    [Fact]
    public void Telemetry_ShouldCountNothing_WhenTheAppTurnsItOff()
    {
        Start(options => options.Telemetry.Enabled = false);
        using var shown = Collect("trailblazor.control.shown");
        using var used = Collect("trailblazor.control.used");
        var cut = RenderButton("export-csv");

        cut.Find("button").Click();

        Assert.Empty(shown.GetMeasurementSnapshot());
        Assert.Empty(used.GetMeasurementSnapshot());
    }

    [Fact]
    public void Telemetry_ShouldAddTheAppsOwnTags_WhenTheAppRegistersThem()
    {
        Services.AddScoped<ITelemetryTags, CustomerTags>();
        Start();
        using var used = Collect("trailblazor.control.used");
        var cut = RenderButton("export-csv");

        cut.Find("button").Click();

        Assert.Equal("acme", Assert.Single(used.GetMeasurementSnapshot()).Tags["customer"]);
    }

    [Fact]
    public void TelemetryName_ShouldLeaveTheButtonWorking_WhenTrailblazorIsNotRegistered()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.TelemetryName, "export-csv")
            .Add(button => button.OnClick, () => clicks++)
            .Add(button => button.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Export"))));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void TelemetryCatalog_ShouldListEveryInstrumentOfTheMeter_WhenTheMeterIsCreated()
    {
        using var provider = new ServiceCollection().AddTrailblazor().BuildServiceProvider();
        var factory = provider.GetRequiredService<IMeterFactory>();
        var published = new List<Instrument>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, _) =>
        {
            if (instrument.Meter.Scope == factory)
            {
                published.Add(instrument);
            }
        };
        listener.Start();

        provider.GetRequiredService<TelemetryInstruments>();

        Assert.Equal([Telemetry.NameTag, Telemetry.ComponentTag], TelemetryCatalog.CommonTags);
        Assert.Equal(Telemetry.ActionTag, TelemetryCatalog.ActionTag);
        Assert.Equal(Telemetry.InputTag, TelemetryCatalog.InputTag);
        Assert.Equal(Telemetry.OutcomeTag, TelemetryCatalog.OutcomeTag);
        Assert.Equal(TelemetryCatalog.Instruments.Select(instrument => (TelemetryCatalog.MeterName, instrument.Name,
                instrument.Kind, (string?)instrument.Unit, (string?)instrument.Description)),
            published.Select(instrument => (instrument.Meter.Name, instrument.Name,
                instrument.GetType().Name.Split('`')[0], instrument.Unit, instrument.Description)));
    }

    private void Start(Action<TrailblazorOptions>? configure = null)
    {
        this.AddTrailblazor(configure);
    }

    private IReadOnlyList<CollectedMeasurement<int>> ReadCatalog()
    {
        using var catalog = new MetricCollector<int>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.catalog");
        Services.GetRequiredService<TelemetryInstruments>();
        catalog.RecordObservableInstruments();
        return catalog.GetMeasurementSnapshot();
    }

    // Scoped to this test's meter factory, so tests running alongside it don't add to its counts.
    private MetricCollector<long> Collect(string instrument) =>
        new(Services.GetRequiredService<IMeterFactory>(), "Trailblazor", instrument);

    private IRenderedComponent<TbButton> RenderButton(string? telemetryName, bool disabled = false) =>
        Render<TbButton>(parameters => parameters.Add(button => button.TelemetryName, telemetryName)
            .Add(button => button.Disabled, disabled)
            .Add(button => button.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Export"))));
}
