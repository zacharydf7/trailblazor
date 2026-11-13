using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Builds a query from conditions on <see cref="Fields"/>, such as "Region is North ridge", joined in groups
/// that match when all or any of their conditions do. Groups can nest inside other groups. Bind <see cref="Value"/>
/// to read the query as a <see cref="QueryGroup"/>, and filter with its <see cref="QueryGroup.ToExpression{T}"/>.
/// </summary>
public sealed partial class TbQueryBuilder : TbComponentBase
{
    private Group _root = new();
    private QueryGroup? _valueFromParent;
    private QueryGroup? _raised;
    // What takes focus next: the condition or group just added, or the Add condition button of a group that lost one.
    private object? _added;
    private Group? _focusAfterRemove;
    private readonly Dictionary<Group, TbButton> _addButtons = [];
    private ControlTelemetry? _telemetry;

    /// <summary>The query builder's accessible name, such as "Trail search". Required.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The fields conditions can be written on, in the order they're listed, such as
    /// <see cref="QueryField.For{T}"/> gives. Required, with at least one; a new condition starts on the first.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<QueryField> Fields { get; set; } = [];

    /// <summary>The query. Bind it with <c>@bind-Value</c> to start from a saved one and hear each change; a new
    /// query is a group with no conditions, which matches everything.</summary>
    [Parameter]
    public QueryGroup? Value { get; set; }

    /// <summary>Raised with the whole query each time the user changes it.</summary>
    [Parameter]
    public EventCallback<QueryGroup> ValueChanged { get; set; }

    /// <summary>How many levels of groups the query can have, counting the outer one, which is always there. Defaults
    /// to 3; 1 leaves out the Add group button.</summary>
    [Parameter]
    public int MaxDepth { get; set; } = 3;

    /// <summary>The words the lists and buttons show and say to screen readers. Each one left null uses the app's
    /// text.</summary>
    [Parameter]
    public QueryBuilderLabels Labels { get; set; } = new();

    /// <summary>A label the query builder's use is counted under, such as <c>"trail-search"</c>, through the
    /// <c>Trailblazor</c> meter: shown when it first renders, and used with the action <c>add_condition</c>,
    /// <c>add_group</c>, <c>remove</c> or <c>change</c> each time the user does that. The fields and values are never
    /// recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Label) || Fields.Count == 0 || MaxDepth < 1)
        {
            throw new InvalidOperationException($"{nameof(TbQueryBuilder)} needs a {nameof(Label)}, which names it for "
                + $"screen readers, at least one of {nameof(Fields)}, and a {nameof(MaxDepth)} of 1 or more.");
        }

        // A query the parent passes back after hearing it is the one already shown.
        if (!ReferenceEquals(Value, _valueFromParent))
        {
            _valueFromParent = Value;
            if (!ReferenceEquals(Value, _raised))
            {
                _root = GroupOf(Value ?? new());
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        _added = null;
        if (_focusAfterRemove is { } group && _addButtons.TryGetValue(group, out var button))
        {
            _focusAfterRemove = null;
            await button.FocusAsync();
        }
    }

    private Group GroupOf(QueryGroup query) => new()
    {
        Join = query.Join,
        Conditions = [.. query.Conditions.Select(ConditionOf)],
        Groups = [.. query.Groups.Select(GroupOf)],
    };

    // A query read back from JSON holds JSON values, which take the types the field's editor works with.
    private Condition ConditionOf(GridFilter filter)
    {
        var field = Fields.FirstOrDefault(field => field.Key == filter.Column) ?? throw new InvalidOperationException(
            $"The query has a condition on \"{filter.Column}\", which none of the {nameof(TbQueryBuilder)}'s "
            + $"{nameof(Fields)} has as its key.");
        var typed = FilterJson.Typed(filter, field.Kind switch
        {
            QueryFieldKind.Number => typeof(decimal),
            QueryFieldKind.Date => typeof(DateOnly),
            QueryFieldKind.YesNo => typeof(bool),
            QueryFieldKind.Choice => field.Choices.FirstOrDefault()?.Value.GetType(),
            _ => typeof(string),
        });
        return new() { Field = field, Operator = filter.Operator, Value = EditableValueOf(field, typed.Value) };
    }

    // A query built in code can hold any number type or a date and time, which the editors take as a decimal or a day.
    private static object? EditableValueOf(QueryField field, object? value) => (field.Kind, value) switch
    {
        (QueryFieldKind.Number, _) when NumberTypes.TryToDecimal(value, out var number) => number,
        (QueryFieldKind.Date, DateTime time) => DateOnly.FromDateTime(time),
        _ => value,
    };

    private static QueryGroup QueryOf(Group group) => new()
    {
        Join = group.Join,
        Conditions = [.. group.Conditions.Select(condition =>
            new GridFilter(condition.Field.Key, condition.Operator, condition.Value))],
        Groups = [.. group.Groups.Select(QueryOf)],
    };

    private async Task ChangedAsync(string action)
    {
        Telemetry.Used(TelemetryName, action);
        _raised = QueryOf(_root);
        await ValueChanged.InvokeAsync(_raised);
    }

    private Condition NewCondition() => new() { Field = Fields[0], Operator = OperatorsOf(Fields[0])[0] };

    private Task AddConditionAsync(Group group)
    {
        var condition = NewCondition();
        group.Conditions.Add(condition);
        _added = condition;
        return ChangedAsync("add_condition");
    }

    // A new group starts with a condition, since an empty one matches everything.
    private Task AddGroupAsync(Group group)
    {
        var inner = new Group { Conditions = [NewCondition()] };
        group.Groups.Add(inner);
        _added = inner;
        return ChangedAsync("add_group");
    }

    private Task RemoveConditionAsync(Group group, Condition condition)
    {
        group.Conditions.Remove(condition);
        _focusAfterRemove = group;
        return ChangedAsync("remove");
    }

    private Task RemoveGroupAsync(Group parent, Group group)
    {
        parent.Groups.Remove(group);
        Forget(group);
        _focusAfterRemove = parent;
        return ChangedAsync("remove");
    }

    private void Forget(Group group)
    {
        _addButtons.Remove(group);
        group.Groups.ForEach(Forget);
    }

    private Task JoinAsync(Group group, QueryJoin join)
    {
        group.Join = join;
        return ChangedAsync("change");
    }

    // Another field takes its first comparison and an empty value, since the old value may not suit it.
    private Task FieldAsync(Condition condition, string? key)
    {
        condition.Field = Fields.First(field => field.Key == key);
        condition.Operator = OperatorsOf(condition.Field)[0];
        condition.Value = null;
        return ChangedAsync("change");
    }

    // The value stays unless the new comparison takes none, or switches between one value and a list.
    private Task OperatorAsync(Condition condition, FilterOperator op)
    {
        if (!Comparisons.NeedsValue(op) || (op == FilterOperator.IsAnyOf) != (condition.Operator == FilterOperator.IsAnyOf))
        {
            condition.Value = null;
        }

        condition.Operator = op;
        return ChangedAsync("change");
    }

    private Task ValueAsync(Condition condition, object? value)
    {
        condition.Value = value;
        return ChangedAsync("change");
    }

    private Task ChoicesAsync(Condition condition, IReadOnlyList<string>? keys) =>
        ValueAsync(condition, (keys ?? []).Select(key => ChoiceOf(condition.Field, key)).ToList());

    private static IReadOnlyList<FilterOperator> OperatorsOf(QueryField field) => field.Kind switch
    {
        QueryFieldKind.Text => Comparisons.ForText,
        QueryFieldKind.Number or QueryFieldKind.Date => Comparisons.ForNumbers,
        QueryFieldKind.YesNo => [FilterOperator.EqualTo],
        _ => [FilterOperator.EqualTo, FilterOperator.NotEqualTo, FilterOperator.IsAnyOf],
    };

    // A date's comparisons read as before and after.
    private string OperatorLabel(QueryField field, FilterOperator op) => Localizer[(field.Kind, op) switch
    {
        (QueryFieldKind.Date, FilterOperator.LessThan) => TbStrings.QueryBuilderBefore,
        (QueryFieldKind.Date, FilterOperator.LessThanOrEqualTo) => TbStrings.QueryBuilderOnOrBefore,
        (QueryFieldKind.Date, FilterOperator.GreaterThan) => TbStrings.QueryBuilderAfter,
        (QueryFieldKind.Date, FilterOperator.GreaterThanOrEqualTo) => TbStrings.QueryBuilderOnOrAfter,
        _ => Comparisons.TextKeyOf(op),
    }];

    // A choice's option is its place in the field's list, since its value can be any type.
    private static string? ChoiceKeyOf(QueryField field, object? value) =>
        field.Choices.ToList().FindIndex(choice => SameChoice(choice.Value, value)) is var at and >= 0
            ? at.ToString(CultureInfo.InvariantCulture) : null;

    // A number read back from JSON is a decimal, so a choice whose value is another number type matches it by value.
    private static bool SameChoice(object choice, object? value) => Equals(choice, value)
        || NumberTypes.TryToDecimal(choice, out var number) && NumberTypes.TryToDecimal(value, out var other)
        && number == other;

    private static object? ChoiceOf(QueryField field, string? key) =>
        int.TryParse(key, CultureInfo.InvariantCulture, out var at) && at >= 0 && at < field.Choices.Count
            ? field.Choices[at].Value : null;

    private static IReadOnlyList<string> ChoiceKeysOf(Condition condition) =>
        condition.Value is IEnumerable values and not string
            ? [.. values.Cast<object?>().Select(value => ChoiceKeyOf(condition.Field, value)).OfType<string>()]
            : [];

    private string Text(string? label, string key) => label ?? Localizer[key];

    private string Format(string? label, string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Text(label, key), args);

    // The query as the user edits it, kept as one object per condition and group so each keeps its place and focus.
    private sealed class Group
    {
        public QueryJoin Join { get; set; }

        public List<Condition> Conditions { get; init; } = [];

        public List<Group> Groups { get; init; } = [];
    }

    private sealed class Condition
    {
        public required QueryField Field { get; set; }

        public FilterOperator Operator { get; set; }

        public object? Value { get; set; }
    }
}
