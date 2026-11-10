using Bunit;
using Microsoft.AspNetCore.Components;

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
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var (first, second) = ContractRules.Samples(type) ?? throw new NotSupportedException(
            $"Add a sample {type.Name} to the contract kit, or list the parameter with a reason.");

        Assert.Empty(ContractRules.CheckEcho(this, component, parameter, BaseParameters(component), first, second));
    }

    public static TheoryData<Type, string> FieldsTurnedOff => new(ComponentContractTests.Fields
        .SelectMany(row => new[] { (row.Data, "Disabled"), (row.Data, "ReadOnly") }));

    [Theory, MemberData(nameof(FieldsTurnedOff))]
    public async Task Field_ShouldNotChangeItsValue_WhenTurnedOffAndEveryControlIsUsed(Type field, string state)
    {
        var values = BaseParameters(field);
        if (Content.TryGetValue(field, out var content))
        {
            values[content.Name] = content.Value;
        }

        Assert.Empty(await ContractRules.CheckTurnedOffAsync(this, field, state, values));
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
            .SelectMany(component => ContractRules.BoundParameters(component).Select(name => (component, name)));

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

    private static (string Name, object Value) Child<TChild>() where TChild : IComponent =>
        ("ChildContent", (RenderFragment)(builder =>
        {
            builder.OpenComponent<TChild>(0);
            builder.AddAttribute(1, "Value", "Pine");
            builder.CloseComponent();
        }));
}
