using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Contract;

// What every component promises once it's disposed: it leaves no handler on a service and no reference the browser can
// still call back, and as time passes it neither calls its parent nor raises an error. Every public component is
// checked with a counted handler on each of its callbacks, after each of its controls has been used, so work that only
// starts then is covered too. Not covered: the carousel's turning timer, which needs a set-up script handle, and the
// toast timers, which need a toast shown.
public class DisposeContractTests : BunitContext
{
    // Components that only render inside a parent are rendered inside one.
    private static readonly Dictionary<Type, Type> Parents = new()
    {
        [typeof(TbRadio<>)] = typeof(TbRadioGroup<>), [typeof(TbCheckboxItem<>)] = typeof(TbCheckboxGroup<>),
        [typeof(TbTab)] = typeof(TbTabs), [typeof(TbOption<>)] = typeof(TbSelect<>),
        [typeof(TbRibbonTab)] = typeof(TbRibbon),
        [typeof(TbToggleItem<>)] = typeof(TbToggleGroup<>), [typeof(TbSplitterPane)] = typeof(TbSplitter),
        [typeof(TbTreeItem<>)] = typeof(TbTreeView<,>), [typeof(TbMenuRadioItem<>)] = typeof(TbMenuRadioGroup<>),
        [typeof(TbDialogDescription)] = typeof(TbDialog), [typeof(TbDialogTitle)] = typeof(TbDialog),
        [typeof(TbDialogHeaderActions)] = typeof(TbDialog), [typeof(TbDialogHeader)] = typeof(TbDialog),
        [typeof(TbColumn<>)] = typeof(TbDataGrid<>), [typeof(TbTableOfContentsItem)] = typeof(TbTableOfContents),
        [typeof(TbBreadcrumb)] = typeof(TbBreadcrumbs), [typeof(TbNavLink)] = typeof(TbNavMenu),
        [typeof(TbNavGroup)] = typeof(TbNavMenu), [typeof(TbStep)] = typeof(TbStepper),
        [typeof(TbCarouselSlide)] = typeof(TbCarousel), [typeof(TbColumnGroup<>)] = typeof(TbDataGrid<>),
        [typeof(TbModelField)] = typeof(TbModelFields<>), [typeof(TbSmartPaste)] = typeof(TbForm),
        [typeof(TbValidationSummary)] = typeof(TbForm),
        [typeof(TbDescriptionItem)] = typeof(TbDescriptionList),
        [typeof(TbDockPanel)] = typeof(TbDockManager),
        [typeof(TbLineSeries<>)] = typeof(TbChart<>), [typeof(TbBarSeries<>)] = typeof(TbChart<>),
        [typeof(TbXAxis)] = typeof(TbChart<>), [typeof(TbYAxis)] = typeof(TbChart<>),
    };

    // Callbacks left without a handler: a handled ActiveKeyChanged makes the tabs keyed, so every tab would need a Key,
    // and a form throws for OnSubmit beside OnValidSubmit.
    private static readonly HashSet<string> Unhandled =
        ["TbTabs.ActiveKeyChanged", "TbRibbon.ActiveKeyChanged", "TbForm.OnSubmit"];

    private readonly FakeTimeProvider _time = new();
    private readonly List<object?> _callbacks = [];

    public DisposeContractTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    public static TheoryData<Type> Components => new(ComponentContractTests.Components.Select(row => row.Data)
        .Concat(ComponentContractTests.NotRenderedAlone.Select(Close)));

    [Theory, MemberData(nameof(Components))]
    public async Task Dispose_ShouldLeaveNothingRunning_WhenTheComponentIsDisposed(Type component)
    {
        Assert.Empty(await ContractRules.CheckDisposeAsync(this, _time,
            parameters => Host(parameters, component, Parameters), component.Name, _callbacks));
    }

    // Renders the component, inside its parent when it only renders inside one, with the parameters parametersOf gives
    // each.
    internal static void Host(ComponentParameterCollectionBuilder<DynamicComponent> parameters, Type component,
        Func<Type, Dictionary<string, object>> parametersOf)
    {
        var values = parametersOf(component);
        var shown = component;
        var own = values;
        RenderFragment child = builder =>
        {
            builder.OpenComponent(0, component);
            foreach (var (name, value) in own)
            {
                builder.AddAttribute(1, name, value);
            }

            builder.CloseComponent();
        };
        // A TbTreeItemEnd fills the end slot of the row whose template it's in, so it renders in a tree's
        // ItemTemplate, not its ChildContent, which some tests set and a tree with Items mustn't have.
        if (component == typeof(TbTreeItemEnd))
        {
            shown = typeof(TbTreeView<string, string>);
            values = parametersOf(shown);
            values.Remove(nameof(TbTreeView<string, string>.ChildContent));
            values[nameof(TbTreeView<string, string>.Items)] = new[] { "Pine" };
            values[nameof(TbTreeView<string, string>.ItemTemplate)] =
                (RenderFragment<TreeItemContext<string, string>>)(_ => child);
        }
        else if (component.IsGenericType && Parents.TryGetValue(component.GetGenericTypeDefinition(), out var parent)
            || Parents.TryGetValue(component, out parent))
        {
            shown = Close(parent);
            values = parametersOf(shown);
            values["ChildContent"] = child;
        }

        parameters.Add(host => host.Type, shown).Add(host => host.Parameters, values);
    }

    // The component's required parameters, setup and settings, and a counted handler on each of its callbacks.
    private Dictionary<string, object> Parameters(Type component)
    {
        var values = ValueContractTests.BaseParameters(component);
        foreach (var (name, value) in Settings(component))
        {
            values[name] = value;
        }

        ContractRules.AddCallbacks(component, values, _callbacks, name => Unhandled.Contains($"{component.Name}.{name}"));
        return values;
    }

    // Components whose timers or script callbacks only start with a setting get that setting: a carousel set to turn
    // and an overlay that closes on a click hand the browser a callback, and an autocomplete given a search waits
    // before calling it, and each call is counted. A countdown given OnExtend needs words for its button.
    private Dictionary<string, object> Settings(Type component) => component switch
    {
        _ when component == typeof(TbCarousel) => new() { ["AutoPlayInterval"] = TimeSpan.FromSeconds(5) },
        _ when component == typeof(TbOverlay) => new() { ["CloseOnClick"] = true },
        _ when component == typeof(TbCountdown) => new() { ["ExtendText"] = "Stay signed in" },
        _ when component == typeof(TbAutocomplete<string>) => new()
        {
            ["Search"] = (Func<string, CancellationToken, Task<IEnumerable<string>>>)((_, _) =>
            {
                _callbacks.Add(null);
                return Task.FromResult<IEnumerable<string>>(["Pine"]);
            }),
        },
        _ => [],
    };

    private static Type Close(Type type) => type.IsGenericTypeDefinition
        ? type.MakeGenericType([.. type.GetGenericArguments().Select(_ => typeof(string))])
        : type;
}
