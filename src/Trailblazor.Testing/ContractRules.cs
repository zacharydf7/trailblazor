using System.Collections;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using Trailblazor.Internal.Events;

namespace Trailblazor.Testing;

// The rules every Trailblazor component keeps. TbContract checks a consumer's component against them, and Trailblazor's
// own contract tests check each of its components. Each check returns the ways the component breaks its rule, or none.
internal static class ContractRules
{
    // The events a test raises itself, which a part found once must take again after the component renders.
    private static readonly Dictionary<string, Func<IElement, Task>> Events = new()
    {
        ["onclick"] = part => part.ClickAsync(new MouseEventArgs { Detail = 1 }),
        ["ondblclick"] = part => part.DoubleClickAsync(new MouseEventArgs { Detail = 2 }),
        ["onchange"] = part => part.ChangeAsync(new ChangeEventArgs
            { Value = part.GetAttribute("type") == "checkbox" ? true : part.GetAttribute("value") }),
        ["onkeydown"] = part => part.KeyDownAsync(new KeyboardEventArgs { Key = "Shift" }),
        ["onkeyup"] = part => part.KeyUpAsync(new KeyboardEventArgs { Key = "Shift" }),
        ["ontbrowclick"] = part => part.TriggerEventAsync("ontbrowclick", new RowClickEventArgs()),
        ["ontbrowextend"] = part => part.TriggerEventAsync("ontbrowextend", new RowExtendEventArgs()),
        ["ontbeditstart"] = part => part.TriggerEventAsync("ontbeditstart", EventArgs.Empty),
    };

    private static readonly string Handled = string.Join(", ", Events.Keys.Select(name => $"[blazor\\:{name}]"));

    // Setting a value from the parent never raises its Changed callback, so a parent that handles the callback doesn't
    // take its own change for the user's. A component may still report a value it corrected or refused.
    public static List<string> CheckEcho(BunitContext context, Type component, string parameter,
        Dictionary<string, object> values, object first, object second)
    {
        var echoes = new List<object?>();
        values = new(values)
        {
            [parameter + "Changed"] = Recorder(component.GetProperty(parameter + "Changed")!, echoes),
            [parameter] = first,
        };
        var cut = context.Render<DynamicComponent>(parameters => parameters.Add(host => host.Type, component)
            .Add(host => host.Parameters, new Dictionary<string, object>(values)));
        var failures = new List<string>();
        if (echoes.Contains(first))
        {
            failures.Add($"{component.Name} raised {parameter}Changed with the {parameter} it first rendered with.");
        }

        echoes.Clear();
        values[parameter] = second;
        cut.Render(parameters => parameters.Add(host => host.Type, component)
            .Add(host => host.Parameters, new Dictionary<string, object>(values)));
        if (echoes.Contains(second))
        {
            failures.Add($"{component.Name} raised {parameter}Changed with a new {parameter} its parent set.");
        }

        return failures;
    }

    // A disabled or read-only field never changes its value, however its controls are used.
    public static async Task<List<string>> CheckTurnedOffAsync(BunitContext context, Type field, string state,
        Dictionary<string, object> values)
    {
        var changes = new List<object?>();
        values = new(values)
        {
            ["ValueChanged"] = Recorder(field.GetProperty("ValueChanged")!, changes),
            [state] = true,
        };
        var cut = context.Render<DynamicComponent>(parameters => parameters.Add(host => host.Type, field)
            .Add(host => host.Parameters, values));
        await UseEveryControlAsync(cut);
        return changes.Count == 0 ? [] : [$"{field.Name} changed its value while {state}."];
    }

    // Once disposed, a component leaves no handler on a service and no reference the browser can still call back, and
    // as time passes it neither calls its parent nor raises an error. The context must use the given fake clock, and
    // calls must record every callback the component can make, so a call after dispose shows up.
    public static async Task<List<string>> CheckDisposeAsync(BunitContext context, FakeTimeProvider time,
        Action<ComponentParameterCollectionBuilder<DynamicComponent>> render, string name, List<object?> calls)
    {
        var services = WatchedServices(context);
        var handlers = Handlers(services);
        var cut = context.Render(render);
        // Work a component starts after it first renders, such as a script call or a timer, runs before the controls.
        await context.Renderer.Dispatcher.InvokeAsync(() => { });
        // Time moves on while the controls are used, so work that waits on it, such as a debounce, finishes.
        var use = UseEveryControlAsync(cut);
        for (var second = 0; !use.IsCompleted; second++)
        {
            if (second == 600)
            {
                return [$"Using the controls of {name} never finished."];
            }

            time.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(1);
        }

        await use;
        // Each combobox is typed in once more without moving the time on, so its delayed search is still pending at
        // dispose.
        var typing = cut.FindAll("input[role=combobox]")
            .Select(box => box.InputAsync(new ChangeEventArgs { Value = "Cedar" })).ToList();
        var references = context.JSInterop.Invocations.SelectMany(call => call.Arguments).Where(IsObjectReference)
            .ToList();

        await context.DisposeComponentsAsync();
        var called = calls.Count;
        time.Advance(TimeSpan.FromHours(1));
        // Work that waited on the time is handed to the renderer, so waiting on the renderer lets it run before
        // anything is checked.
        await context.Renderer.Dispatcher.InvokeAsync(() => { });
        await Task.WhenAll(typing).WaitAsync(TimeSpan.FromSeconds(30));

        var failures = Handlers(services).Except(handlers)
            .Select(left => $"{name} left a handler behind on {left}.").ToList();
        failures.AddRange(references.Where(reference => !IsDisposed(reference))
            .Select(reference => $"{name} left a reference to {reference!.GetType().GetGenericArguments()[0].Name} "
                + "that the browser can still call."));
        if (calls.Count != called)
        {
            failures.Add($"{name} called its parent after it was disposed.");
        }

        if (context.Renderer.UnhandledException.IsCompleted)
        {
            failures.Add($"{name} raised an error after it was disposed: {await context.Renderer.UnhandledException}");
        }

        return failures;
    }

    // Each part keeps its event handlers from render to render, so a test can use a part, let the component render
    // again and use the same found part again. A text box's input and change events are left out: Blazor's @bind gives
    // them a new handler on every render, so a test has to find the box again anyway. Other custom events, such as
    // ontbfocusleave, aren't raised.
    public static async Task<List<string>> CheckHandlersAsync(IRenderedComponent<DynamicComponent> cut)
    {
        var failures = new List<string>();
        for (var at = 0; at < cut.FindAll(Handled).Count; at++)
        {
            var part = cut.FindAll(Handled)[at];
            // The parts after this one, one of which moves into its place if using this part removes it, such as the
            // next tag's button.
            var later = cut.FindAll(Handled).Skip(at + 1).Select(HandlerIdsOf).ToHashSet();
            await UsePartAsync(cut, part, at, later, failures);
            await UsePartAsync(cut, part, at, later, failures);
        }

        return failures.Distinct().ToList();
    }

    // Clicks, changes, types in and presses Enter on every control the component shows.
    public static async Task UseEveryControlAsync(IRenderedComponent<DynamicComponent> cut)
    {
        // A browser sends no events to a control turned off by the disabled attribute, but bUnit would, so those
        // controls are skipped. A read-only box can be clicked but not typed in, and read-only.js holds the native
        // controls that ignore readonly still, so neither gets change or input events here.
        const string usable = ":is(button, input, textarea, select, [role=option], [contenteditable]):not([disabled], "
            + "fieldset[disabled] *)";
        const string readOnlyBox = ":is(input:not([type=checkbox], [type=radio]), textarea)[readonly], "
            + ":is(input[type=checkbox], input[type=range], select)[aria-readonly=true]";
        for (var at = 0; at < cut.FindAll(usable).Count; at++)
        {
            // Each event can render again, so the control is found afresh for each one, and may be gone.
            var index = at;
            IElement? Control() => cut.FindAll(usable).ElementAtOrDefault(index);
            await TryAsync(() => Control()?.ClickAsync(new MouseEventArgs { Detail = 1 }));
            if (Control()?.Matches(readOnlyBox) == false)
            {
                await TryAsync(() => Control()?.ChangeAsync(new ChangeEventArgs
                    { Value = Control()!.GetAttribute("type") == "checkbox" ? true : "Cedar" }));
                await TryAsync(() => Control()?.InputAsync(new ChangeEventArgs { Value = "Cedar" }));
            }

            await TryAsync(() => Control()?.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" }));
        }
    }

    // A handler on each of the component's callbacks that records what it's given, except those leaveOut names.
    public static void AddCallbacks(Type component, Dictionary<string, object> values, List<object?> calls,
        Func<string, bool> leaveOut)
    {
        foreach (var property in component.GetProperties().Where(property =>
            property.IsDefined(typeof(ParameterAttribute)) && !values.ContainsKey(property.Name)
            && !leaveOut(property.Name)))
        {
            if (property.PropertyType == typeof(EventCallback))
            {
                values[property.Name] = EventCallback.Factory.Create(calls, () => calls.Add(null));
            }
            else if (property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>))
            {
                values[property.Name] = Recorder(property, calls);
            }
        }
    }

    // An EventCallback of the callback property's type that records each value it's given.
    public static object Recorder(PropertyInfo callback, List<object?> values) => typeof(ContractRules)
        .GetMethod(nameof(Record), BindingFlags.NonPublic | BindingFlags.Static)!
        .MakeGenericMethod(callback.PropertyType.GetGenericArguments()[0]).Invoke(null, [values])!;

    // The parameters with a Changed callback, which the parent can set and the component reports.
    public static IEnumerable<string> BoundParameters(Type component) => component.GetProperties()
        .Where(property => property.IsDefined(typeof(ParameterAttribute))
            && component.GetProperty(property.Name + "Changed") is { } changed
            && changed.PropertyType.IsGenericType
            && changed.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>))
        .Select(property => property.Name);

    // Two different values of a type, for the types Trailblazor's own components bind.
    public static (object First, object Second)? Samples(Type type) => type switch
    {
        _ when type == typeof(string) => ("Pine", "Cedar"),
        _ when type == typeof(sbyte) => ((sbyte)1, (sbyte)2),
        _ when type == typeof(byte) => ((byte)1, (byte)2),
        _ when type == typeof(short) => ((short)1, (short)2),
        _ when type == typeof(ushort) => ((ushort)1, (ushort)2),
        _ when type == typeof(int) => (1, 2),
        _ when type == typeof(uint) => (1u, 2u),
        _ when type == typeof(long) => (1L, 2L),
        _ when type == typeof(ulong) => (1ul, 2ul),
        _ when type == typeof(float) => (30f, 40f),
        _ when type == typeof(double) => (30d, 40d),
        _ when type == typeof(decimal) => (30m, 40m),
        _ when type == typeof(bool) => (false, true),
        { IsEnum: true } when Enum.GetNames(type) is { Length: >= 2 } names =>
            (Enum.Parse(type, names[0]), Enum.Parse(type, names[1])),
        _ when type == typeof(DateOnly) => (new DateOnly(2026, 6, 3), new DateOnly(2026, 7, 10)),
        _ when type == typeof(TimeOnly) => (new TimeOnly(8, 30), new TimeOnly(14, 45)),
        _ when type == typeof(DateTime) => (new DateTime(2026, 6, 3, 8, 30, 0), new DateTime(2026, 7, 10, 14, 45, 0)),
        _ when type == typeof(DateTimeOffset) => (new DateTimeOffset(2026, 6, 3, 8, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 10, 14, 45, 0, TimeSpan.Zero)),
        _ when type == typeof(TimeSpan) => (TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45)),
        // A range of any type with samples runs from its first sample, to the first and then to the second.
        { IsGenericType: true } when (type.GetGenericTypeDefinition() == typeof(DateRange<>)
                || type.GetGenericTypeDefinition() == typeof(SliderRange<>))
            && Samples(type.GetGenericArguments()[0]) is { } ends =>
            (Activator.CreateInstance(type, ends.First, ends.First)!,
                Activator.CreateInstance(type, ends.First, ends.Second)!),
        _ when type == typeof(QueryGroup) => (new QueryGroup(), new QueryGroup { Join = QueryJoin.Or }),
        _ when type == typeof(Signature) => (new Signature([], "Ada"), new Signature([], "Grace")),
        _ when type == typeof(GridState) => (new GridState { Sorts = [new("Name", SortDirection.Ascending)] },
            new GridState { Sorts = [new("Name", SortDirection.Descending)] }),
        _ when type.IsAssignableFrom(typeof(string[])) => (new[] { "Pine" }, new[] { "Cedar" }),
        _ when type.IsAssignableFrom(typeof(GridSort[])) => (new[] { new GridSort("Name", SortDirection.Ascending) },
            new[] { new GridSort("Name", SortDirection.Descending) }),
        _ when type.IsAssignableFrom(typeof(GridFilter[])) => (
            new[] { new GridFilter("Name", FilterOperator.Contains, "Pine") },
            new[] { new GridFilter("Name", FilterOperator.Contains, "Cedar") }),
        _ => null,
    };

    // A type's name as C# writes it, such as IReadOnlyList<Trail> or Trail[].
    public static string NameOf(Type type) => type switch
    {
        { IsArray: true } => $"{NameOf(type.GetElementType()!)}[]",
        { IsGenericType: true } when type.Name.IndexOf('`') is var tick and > 0 =>
            $"{type.Name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(NameOf))}>",
        _ => type.Name,
    };

    private static EventCallback<T> Record<T>(List<object?> values) =>
        EventCallback.Factory.Create<T>(values, value => values.Add(value));

    // A control the field turned off usually has no handler at all, which bUnit reports by throwing.
    private static async Task TryAsync(Func<Task?> use)
    {
        try
        {
            await (use() ?? Task.CompletedTask);
        }
        catch (MissingEventHandlerException)
        {
        }
    }

    // Raises each event the part handles, unless an earlier event removed the part. Change is raised only on checkboxes
    // and radio buttons, which a click changes; a text box's change comes from @bind.
    private static async Task UsePartAsync(IRenderedComponent<DynamicComponent> cut, IElement part, int at,
        HashSet<string> later, List<string> failures)
    {
        foreach (var (name, raise) in Events.Where(pair => part.HasAttribute($"blazor:{pair.Key}")
            && (pair.Key != "onchange" || part.Matches("[type=checkbox], [type=radio]"))))
        {
            if (cut.FindAll(Handled).ElementAtOrDefault(at) is not { } shown || !IsSamePart(part, shown)
                || later.Contains(HandlerIdsOf(shown)))
            {
                return;
            }

            var error = await CatchAsync(() => raise(part));
            if (error is UnknownEventHandlerIdException)
            {
                failures.Add($"{Describe(part)} lost its {name} handler when the component rendered again.");
            }
            else if (error is not null and not MissingEventHandlerException)
            {
                failures.Add($"{Describe(part)} failed on {name}: {error}");
            }
        }
    }

    private static async Task<Exception?> CatchAsync(Func<Task> raise)
    {
        try
        {
            await raise();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    // Label and text aren't compared, since they can change to show the part's new state, as a picked day's does.
    private static bool IsSamePart(IElement before, IElement after) => before.TagName == after.TagName
        && before.Id == after.Id && before.GetAttribute("data-part") == after.GetAttribute("data-part");

    private static string HandlerIdsOf(IElement part) => string.Join(" ",
        part.Attributes.Where(attribute => attribute.Name.StartsWith("blazor:on", StringComparison.Ordinal))
            .Select(attribute => $"{attribute.Name}={attribute.Value}"));

    private static string Describe(IElement part) => $"<{part.TagName.ToLowerInvariant()} "
        + $"data-part=\"{part.GetAttribute("data-part")}\" aria-label=\"{part.GetAttribute("aria-label")}\">";

    // The services a component can subscribe to: Trailblazor's own and the navigation manager.
    private static List<object> WatchedServices(BunitContext context) => context.Services.ToList()
        .Where(service => service.ServiceType.Assembly == typeof(TbRoot).Assembly
            || service.ServiceType.Assembly == typeof(TrailblazorBunitExtensions).Assembly
            || service.ServiceType == typeof(NavigationManager))
        .Select(service => context.Services.GetRequiredService(service.ServiceType))
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
           select $"{service.GetType().Name}.{field.Name} ({count})",
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
        && !type.GetGenericArguments()[0].Assembly.GetName().Name!.StartsWith("Microsoft.", StringComparison.Ordinal);

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
