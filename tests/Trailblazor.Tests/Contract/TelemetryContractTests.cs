using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Contract;

// Every component takes a TelemetryName, or DialogOptions that carry one, or is listed in Uncounted with the reason
// it isn't counted. Every component that renders alone and takes a TelemetryName, apart from those in
// NotShownAsRendered, counts itself shown under it; one that renders only inside a parent is covered by its
// parent's tests.
public class TelemetryContractTests : BunitContext
{
    private const string Layout = "it only lays out other components, which count themselves";
    private const string Part = "it is part of another component, which counts it";
    private const string Plumbing = "it renders nothing a person sees or uses";

    // Components that take no TelemetryName, with the reason.
    private static readonly Dictionary<string, string> Uncounted = new()
    {
        ["TbAppBar"] = Layout, ["TbButtonGroup"] = Layout, ["TbContainer"] = Layout,
        ["TbDivider"] = "it is a line between other components, which count themselves",
        ["TbFieldRow"] = Layout, ["TbGrid"] = Layout, ["TbGridItem"] = Layout, ["TbMasonry"] = Layout,
        ["TbSpacer"] = Layout, ["TbStack"] = Layout, ["TbSurface"] = Layout,

        ["TbIcon"] = "it is a glyph inside text or another control",
        ["TbKbd"] = "it is a key name inside text",
        ["TbLabel"] = "it names another control, which counts itself",
        ["TbSkeleton"] = "it stands in for content until it loads, and the content counts itself",
        ["TbText"] = "it is text inside other components",
        ["TbTimestamp"] = "it only shows a date",
        ["TbDescriptionList"] = "it only shows terms and values",
        ["TbDescriptionItem"] = "part of TbDescriptionList, which only shows terms and values",
        ["TbValidationSummary"] = "it lists the form's messages, and the form counts its failed submits",
        ["TbHighlight"] = "it is text inside other components, such as search results, which count themselves",
        ["TbDialogContentHost"] = "it shows content the app wrote, whose own controls count themselves",
        ["TbTooltip"] =
            "it shows on hover and focus in the browser alone, and a count would cost a round trip each time",

        ["TbAccordion"] = "its items count themselves", ["TbChipGroup"] = "its chips count themselves",
        ["TbList"] = "its items count themselves", ["TbMenubar"] = "its menus' items count themselves",
        ["TbMultiToggleGroup"] = "its items count themselves", ["TbTabs"] = "its tabs count themselves",
        ["TbToggleGroup"] = "its items count themselves", ["TbToolbar"] = "its buttons count themselves",
        ["TbRibbon"] = "its tabs and buttons count themselves",

        ["TbBreadcrumb"] = Part, ["TbCarouselSlide"] = Part, ["TbCheckboxItem"] = Part, ["TbColumn"] = Part,
        ["TbColumnGroup"] = Part, ["TbDialogActions"] = Part,
        ["TbDialogDescription"] = Part, ["TbDialogHeader"] = Part, ["TbDialogHeaderActions"] = Part,
        ["TbDockPanel"] = Part,
        ["TbDialogTitle"] = Part, ["TbMenuGroup"] = Part, ["TbMenuRadioGroup"] = Part, ["TbMenuSeparator"] = Part,
        ["TbNavGroup"] = Part, ["TbNavLink"] = Part, ["TbOptionGroup"] = Part, ["TbRadio"] = Part,
        ["TbRibbonGroup"] = Part,
        ["TbSplitterPane"] = Part, ["TbStep"] = Part, ["TbSubMenu"] = Part, ["TbTableOfContentsItem"] = Part,
        ["TbTimelineItem"] = Part, ["TbToolbarSeparator"] = Part, ["TbTreeItem"] = Part, ["TbTreeItemEnd"] = Part,
        ["TbLineSeries"] = Part, ["TbBarSeries"] = Part, ["TbXAxis"] = Part, ["TbYAxis"] = Part,
        ["TbAreaSeries"] = Part, ["TbScatterSeries"] = Part, ["TbGaugeRange"] = Part,
        ["TbReferenceLine"] = Part, ["TbReferenceBand"] = Part,

        ["TbFocusScope"] = Plumbing, ["TbInteractiveMark"] = Plumbing, ["TbModelField"] = Plumbing,
        ["TbRoot"] = Plumbing, ["TbStyles"] = Plumbing, ["TbThemeHead"] = Plumbing,
    };

    // Components rendered alone that take a TelemetryName but don't count shown as they render, with the reason.
    private static readonly Dictionary<Type, string> NotShownAsRendered = new()
    {
        [typeof(TbSpeechToText)] = "it counts shown once its script finds the browser's speech recognition and it "
            + "shows; TbSpeechToTextTests checks it",
        [typeof(TbBackToTop)] = "it counts shown once the page scrolls far enough to show it, which its script "
            + "reports; TbBackToTopTests checks it",
        [typeof(TbMenuItem)] = "it renders hidden until its menu opens, which the menu counts",
        [typeof(TbMenuCheckboxItem)] = "it renders hidden until its menu opens, which the menu counts",
    };

    public TelemetryContractTests() => this.AddTrailblazor();

    public static TheoryData<Type> Named => new(ComponentContractTests.Components.Select(row => row.Data)
        .Where(type => type.GetProperty(nameof(TbButton.TelemetryName)) is not null
            && !NotShownAsRendered.ContainsKey(type)));

    [Fact]
    public void Components_ShouldEachTakeATelemetryNameOrAReason_WhenTheLibraryIsScanned()
    {
        var components = typeof(TbComponentBase).Assembly.GetExportedTypes()
            .Where(type => !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type)).ToList();
        var counted = components.Where(type => type.GetProperty(nameof(TbButton.TelemetryName)) is not null
            || type.GetProperties().Any(property => property.PropertyType == typeof(DialogOptions)))
            .Select(Name).ToHashSet();

        Assert.Empty(components.Select(Name).Where(name => !counted.Contains(name) && !Uncounted.ContainsKey(name))
            .Order());
        Assert.Empty(Uncounted.Keys.Where(counted.Contains).Order());
        Assert.Empty(Uncounted.Keys.Except(components.Select(Name)).Order());
    }

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

    // A generic component's name without its arity, such as TbSelect for TbSelect`1.
    private static string Name(Type type) => type.Name.Split('`')[0];
}
