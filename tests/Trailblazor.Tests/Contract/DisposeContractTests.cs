using System.Collections;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

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
        [typeof(TbToggleItem<>)] = typeof(TbToggleGroup<>), [typeof(TbSplitterPane)] = typeof(TbSplitter),
        [typeof(TbTreeItem<>)] = typeof(TbTreeView<,>), [typeof(TbMenuRadioItem<>)] = typeof(TbMenuRadioGroup<>),
        [typeof(TbDialogDescription)] = typeof(TbDialog), [typeof(TbDialogTitle)] = typeof(TbDialog),
        [typeof(TbColumn<>)] = typeof(TbDataGrid<>), [typeof(TbTableOfContentsItem)] = typeof(TbTableOfContents),
        [typeof(TbBreadcrumb)] = typeof(TbBreadcrumbs), [typeof(TbNavLink)] = typeof(TbNavMenu),
        [typeof(TbNavGroup)] = typeof(TbNavMenu), [typeof(TbStep)] = typeof(TbStepper),
        [typeof(TbCarouselSlide)] = typeof(TbCarousel),
    };

    // Callbacks left without a handler: a handled ActiveKeyChanged makes the tabs keyed, so every tab would need a Key.
    private static readonly HashSet<string> Unhandled = ["TbTabs.ActiveKeyChanged"];

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
        var services = WatchedServices();
        var handlers = Handlers(services);
        var cut = Render<DynamicComponent>(parameters => Host(parameters, component, Parameters));
        // Time moves on while the controls are used, so work that waits on it, such as a debounce, finishes.
        var use = ValueContractTests.UseEveryControlAsync(cut);
        for (var second = 0; !use.IsCompleted; second++)
        {
            Assert.True(second < 600, $"Using the controls of {component.Name} never finished.");
            _time.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(1, Xunit.TestContext.Current.CancellationToken);
        }

        await use;
        // Each combobox is typed in once more without moving the time on, so its delayed search is still pending at
        // dispose.
        var typing = cut.FindAll("input[role=combobox]")
            .Select(box => box.InputAsync(new ChangeEventArgs { Value = "Cedar" })).ToList();
        var references = JSInterop.Invocations.SelectMany(call => call.Arguments).Where(IsObjectReference).ToList();

        await DisposeComponentsAsync();
        var callbacks = _callbacks.Count;
        _time.Advance(TimeSpan.FromHours(1));
        // Work that waited on the time is handed to the renderer, so waiting on the renderer lets it run before
        // anything is checked.
        await Renderer.Dispatcher.InvokeAsync(() => { });
        await Task.WhenAll(typing).WaitAsync(TimeSpan.FromSeconds(30), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(handlers, Handlers(services));
        Assert.All(references, reference => Assert.True(IsDisposed(reference), $"{reference!.GetType()} is live."));
        Assert.Equal(callbacks, _callbacks.Count);
        Assert.Null(Renderer.UnhandledException.IsCompleted ? await Renderer.UnhandledException : null);
    }

    // Renders the component, inside its parent when it only renders inside one, with the parameters parametersOf gives
    // each.
    internal static void Host(ComponentParameterCollectionBuilder<DynamicComponent> parameters, Type component,
        Func<Type, Dictionary<string, object>> parametersOf)
    {
        var values = parametersOf(component);
        var shown = component;
        if (component.IsGenericType && Parents.TryGetValue(component.GetGenericTypeDefinition(), out var parent)
            || Parents.TryGetValue(component, out parent))
        {
            var child = values;
            shown = Close(parent);
            values = parametersOf(shown);
            values["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent(0, component);
                foreach (var (name, value) in child)
                {
                    builder.AddAttribute(1, name, value);
                }

                builder.CloseComponent();
            });
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

        foreach (var property in component.GetProperties().Where(property =>
            property.IsDefined(typeof(ParameterAttribute)) && !values.ContainsKey(property.Name)
            && !Unhandled.Contains($"{component.Name}.{property.Name}")))
        {
            if (property.PropertyType == typeof(EventCallback))
            {
                values[property.Name] = EventCallback.Factory.Create(this, () => _callbacks.Add(null));
            }
            else if (property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>))
            {
                values[property.Name] = ValueContractTests.Recorder(property, _callbacks);
            }
        }

        return values;
    }

    // Components whose timers or script callbacks only start with a setting get that setting: a text field set to
    // update on input reports what's typed after a delay, a carousel set to turn and an overlay that closes on a click
    // hand the browser a callback, and an autocomplete given a search waits before calling it, and each call is
    // counted.
    private Dictionary<string, object> Settings(Type component) => component switch
    {
        _ when component == typeof(TbTextField<string>) =>
            new() { ["UpdateOnInput"] = true, ["UpdateDelay"] = TimeSpan.FromSeconds(1) },
        _ when component == typeof(TbCarousel) => new() { ["AutoPlay"] = TimeSpan.FromSeconds(5) },
        _ when component == typeof(TbOverlay) => new() { ["CloseOnClick"] = true },
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

    // The services a component can subscribe to: Trailblazor's own and the navigation manager.
    private List<object> WatchedServices() => Services.ToList()
        .Where(service => service.ServiceType.Assembly == typeof(TbRoot).Assembly
            || service.ServiceType.Assembly == typeof(TrailblazorBunitExtensions).Assembly
            || service.ServiceType == typeof(NavigationManager))
        .Select(service => Services.GetRequiredService(service.ServiceType))
        .Distinct(ReferenceEqualityComparer.Instance).ToList()!;

    // How many handlers each event, and each list of callbacks, on the services holds.
    private static List<string> Handlers(List<object> services) =>
    [
        .. from service in services
           from field in Fields(service.GetType())
           let value = field.GetValue(service)
           let count = value switch
           {
               Delegate handler => handler.GetInvocationList().Length,
               ICollection list when IsCallbackList(field.FieldType) => list.Count,
               _ => -1,
           }
           where count >= 0
           select $"{service.GetType().Name}.{field.Name}: {count}",
    ];

    private static IEnumerable<FieldInfo> Fields(Type? type)
    {
        for (; type is not null; type = type.BaseType)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static bool IsCallbackList(Type type) => type.IsGenericType
        && type.GetGenericArguments() is [var item] && typeof(Delegate).IsAssignableFrom(item);

    // Blazor's own components, such as Virtualize, look after their references themselves.
    private static bool IsObjectReference(object? argument) => argument?.GetType() is { IsGenericType: true } type
        && type.GetGenericTypeDefinition() == typeof(DotNetObjectReference<>)
        && type.GetGenericArguments()[0].Assembly.GetName().Name!.StartsWith("Trailblazor", StringComparison.Ordinal);

    // A disposed reference refuses to give up its object.
    private static bool IsDisposed(object? reference)
    {
        try
        {
            reference!.GetType().GetProperty(nameof(DotNetObjectReference<object>.Value))!.GetValue(reference);
            return false;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ObjectDisposedException)
        {
            return true;
        }
    }
}
