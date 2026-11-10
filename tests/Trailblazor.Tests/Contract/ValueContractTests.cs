using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Contract;

// What every component promises about its values. A value the parent sets is never echoed back through its Changed
// callback, so a parent that handles the callback doesn't see its own change as the user's; a component may still
// report a value it corrected or refused, such as the last page when it's set past it. A disabled or read-only field
// never changes its value. Both are checked for every public component.
public class ValueContractTests : BunitContext
{
    // Pairs that only report state, which the parent never sets.
    private static readonly HashSet<string> ReportOnly = ["TbForm.Errors", "TbForm.IsValid"];

    // Pairs of components that only render inside their parent, whose own tests set them there.
    private static readonly HashSet<string> InsideParent =
        ["TbTreeItem`1.Expanded", "TbNavGroup.Expanded", "TbSplitterPane.Size", "TbSplitterPane.Collapsed"];

    // Files come only from the browser, so there is no sample to set.
    private static readonly HashSet<string> NoSample = ["TbFileUpload.Value"];

    // Fields whose controls come from their children, items or presets get one of each, so those controls are used too.
    internal static readonly Dictionary<Type, (string Name, object Value)> Content = new()
    {
        [typeof(TbSelect<string>)] = Child<TbOption<string>>(),
        [typeof(TbMultiSelect<string>)] = Child<TbOption<string>>(),
        [typeof(TbListBox<string>)] = Child<TbOption<string>>(),
        [typeof(TbMultiListBox<string>)] = Child<TbOption<string>>(),
        [typeof(TbRadioGroup<string>)] = Child<TbRadio<string>>(),
        [typeof(TbCheckboxGroup<string>)] = Child<TbCheckboxItem<string>>(),
        [typeof(TbAutocomplete<string>)] = (nameof(TbAutocomplete<string>.Items), new[] { "Pine" }),
        [typeof(TbDateRangePicker<DateOnly>)] = (nameof(TbDateRangePicker<DateOnly>.Presets),
            new[] { new DateRangePreset<DateOnly>("June", new(new(2026, 6, 1), new(2026, 6, 30))) }),
    };

    public ValueContractTests() => this.AddTrailblazor();

    public static TheoryData<Type, string> Pairs => new(Candidates()
        .Where(pair => !Skipped(Name(pair.Component, pair.Parameter)))
        .Select(pair => (pair.Component, pair.Parameter)));

    [Theory, MemberData(nameof(Pairs))]
    public void Parameter_ShouldNotRaiseItsChangedCallback_WhenTheParentSetsIt(Type component, string parameter)
    {
        var property = component.GetProperty(parameter)!;
        var (first, second) = Samples(Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
        var echoes = new List<object?>();
        var values = BaseParameters(component);
        values[parameter + "Changed"] = Recorder(component.GetProperty(parameter + "Changed")!, echoes);

        values[parameter] = first;
        var cut = Render<DynamicComponent>(parameters => parameters.Add(host => host.Type, component)
            .Add(host => host.Parameters, new Dictionary<string, object>(values)));
        Assert.DoesNotContain(first, echoes);
        echoes.Clear();
        values[parameter] = second;
        cut.Render(parameters => parameters.Add(host => host.Type, component)
            .Add(host => host.Parameters, new Dictionary<string, object>(values)));

        Assert.DoesNotContain(second, echoes);
    }

    public static TheoryData<Type, string> FieldsTurnedOff => new(ComponentContractTests.Fields
        .SelectMany(row => new[] { (row.Data, "Disabled"), (row.Data, "ReadOnly") }));

    [Theory, MemberData(nameof(FieldsTurnedOff))]
    public async Task Field_ShouldNotChangeItsValue_WhenTurnedOffAndEveryControlIsUsed(Type field, string state)
    {
        var changes = new List<object?>();
        var values = BaseParameters(field);
        values[nameof(TbTextField<string>.ValueChanged)] =
            Recorder(field.GetProperty(nameof(TbTextField<string>.ValueChanged))!, changes);
        values[state] = true;
        if (Content.TryGetValue(field, out var content))
        {
            values[content.Name] = content.Value;
        }

        var cut = Render<DynamicComponent>(parameters => parameters.Add(host => host.Type, field)
            .Add(host => host.Parameters, values));
        await UseEveryControlAsync(cut);

        Assert.Empty(changes);
    }

    // Clicks, changes, types in and presses Enter on every control the component shows.
    internal static async Task UseEveryControlAsync(IRenderedComponent<DynamicComponent> cut)
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

    [Fact]
    public void Pairs_ShouldEachBeCheckedOrListedWithAReason_WhenFound()
    {
        var found = Candidates().Select(pair => Name(pair.Component, pair.Parameter)).ToHashSet();

        Assert.Empty(ReportOnly.Concat(InsideParent).Concat(NoSample).Except(found));
    }

    private static bool Skipped(string name) =>
        ReportOnly.Contains(name) || InsideParent.Contains(name) || NoSample.Contains(name);

    private static IEnumerable<(Type Component, string Parameter)> Candidates() =>
        ComponentContractTests.Components.Select(row => row.Data).Concat(ComponentContractTests.NotRenderedAlone)
            .SelectMany(component => component.GetProperties()
                .Where(property => property.IsDefined(typeof(ParameterAttribute))
                    && component.GetProperty(property.Name + "Changed") is { } changed
                    && changed.PropertyType.IsGenericType
                    && changed.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>))
                .Select(property => (component, property.Name)));

    private static string Name(Type component, string parameter) =>
        $"{(component.IsGenericType ? component.GetGenericTypeDefinition().Name : component.Name)}.{parameter}";

    internal static Dictionary<string, object> BaseParameters(Type component)
    {
        var values = ComponentContractTests.RequiredParameters(component)
            .ToDictionary(pair => pair.Name, pair => pair.Value);
        if (ComponentContractTests.Setup.TryGetValue(component, out var setup))
        {
            values[setup.Name] = setup.Value;
        }

        return values;
    }

    // Two different values of a parameter's type.
    private static (object First, object Second) Samples(Type type) => type switch
    {
        _ when type == typeof(string) => ("Pine", "Cedar"),
        _ when type == typeof(int) => (1, 2),
        _ when type == typeof(double) => (30d, 40d),
        _ when type == typeof(bool) => (false, true),
        _ when type == typeof(DateOnly) => (new DateOnly(2026, 6, 3), new DateOnly(2026, 7, 10)),
        _ when type == typeof(TimeOnly) => (new TimeOnly(8, 30), new TimeOnly(14, 45)),
        _ when type == typeof(DateTime) => (new DateTime(2026, 6, 3, 8, 30, 0), new DateTime(2026, 7, 10, 14, 45, 0)),
        _ when type == typeof(DateRange<DateOnly>) => (new DateRange<DateOnly>(new(2026, 6, 3), new(2026, 6, 5)),
            new DateRange<DateOnly>(new(2026, 7, 1), new(2026, 7, 9))),
        _ when type == typeof(SliderRange<int>) => (new SliderRange<int>(1, 3), new SliderRange<int>(2, 4)),
        _ when type == typeof(GridState) => (new GridState { Sorts = [new("Name", SortDirection.Ascending)] },
            new GridState { Sorts = [new("Name", SortDirection.Descending)] }),
        _ when type.IsAssignableFrom(typeof(string[])) => (new[] { "Pine" }, new[] { "Cedar" }),
        _ when type.IsAssignableFrom(typeof(GridSort[])) => (new[] { new GridSort("Name", SortDirection.Ascending) },
            new[] { new GridSort("Name", SortDirection.Descending) }),
        _ when type.IsAssignableFrom(typeof(GridFilter[])) => (
            new[] { new GridFilter("Name", FilterOperator.Contains, "Pine") },
            new[] { new GridFilter("Name", FilterOperator.Contains, "Cedar") }),
        _ => throw new NotSupportedException(
            $"Add a sample {type.Name} to the contract kit, or list the parameter with a reason."),
    };

    // An EventCallback of the callback property's type that records each value it's given.
    internal static object Recorder(PropertyInfo callback, List<object?> values) => typeof(ValueContractTests)
        .GetMethod(nameof(Record), BindingFlags.NonPublic | BindingFlags.Static)!
        .MakeGenericMethod(callback.PropertyType.GetGenericArguments()[0]).Invoke(null, [values])!;

    private static EventCallback<T> Record<T>(List<object?> values) =>
        EventCallback.Factory.Create<T>(new object(), value => values.Add(value));

    private static (string Name, object Value) Child<TChild>() where TChild : IComponent =>
        ("ChildContent", (RenderFragment)(builder =>
        {
            builder.OpenComponent<TChild>(0);
            builder.AddAttribute(1, "Value", "Pine");
            builder.CloseComponent();
        }));
}
