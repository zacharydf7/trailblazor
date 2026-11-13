using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Trailblazor;

/// <summary>A group of conditions, and of groups inside it, that a <see cref="TbQueryBuilder"/> builds: an item
/// matches when it meets all of them or any of them, as <see cref="Join"/> says. It reads and writes as JSON, so a
/// query can be saved, and <see cref="ToExpression{T}"/> turns it into a filter for a list or a database query.
/// </summary>
public sealed class QueryGroup
{
    /// <summary>Whether an item has to meet all of the conditions and groups, or any of them.</summary>
    public QueryJoin Join { get; init; }

    /// <summary>The conditions, each on a field named by its <see cref="GridFilter.Column"/>, which is a
    /// <see cref="QueryField.Key"/>.</summary>
    public IReadOnlyList<GridFilter> Conditions { get; init; } = [];

    /// <summary>The groups inside this one, each with a join of its own.</summary>
    public IReadOnlyList<QueryGroup> Groups { get; init; } = [];

    /// <summary>A filter that's true for the items that match, for <c>Where</c> on a list or on an EF Core query. Each
    /// condition's column names one of <typeparamref name="T"/>'s public properties. Text compares in lower case, so
    /// it ignores case in memory and wherever the database lowers text, and a date matches a
    /// <see cref="DateTime"/> anywhere on that day. A condition still waiting for its value is left out, and a query
    /// with nothing to check matches every item.</summary>
    public Expression<Func<T, bool>> ToExpression<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        var item = Expression.Parameter(typeof(T), "item");
        return Expression.Lambda<Func<T, bool>>(BodyOf(this, item, typeof(T)) ?? Expression.Constant(true), item);
    }

    private static Expression? BodyOf(QueryGroup group, ParameterExpression item,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        List<Expression> parts =
        [
            .. group.Conditions.Select(condition => ConditionOf(condition, item, type)).OfType<Expression>(),
            .. group.Groups.Select(inner => BodyOf(inner, item, type)).OfType<Expression>(),
        ];
        return parts.Count == 0 ? null
            : parts.Aggregate(group.Join == QueryJoin.And ? Expression.AndAlso : Expression.OrElse);
    }

    private static Expression? ConditionOf(GridFilter condition, ParameterExpression item,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var property = type.GetProperty(condition.Column, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"{type.Name} has no public property \"{condition.Column}\" for a query's condition.");
        var member = Expression.Property(item, property);
        var plain = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
        var canBeNull = !member.Type.IsValueType || plain != member.Type;
        // A query read back from JSON holds JSON values, which take the property's type.
        condition = FilterJson.Typed(condition, plain);
        Expression? isNull = canBeNull ? Expression.Equal(member, Expression.Constant(null, member.Type)) : null;
        switch (condition.Operator)
        {
            case FilterOperator.IsEmpty:
                return member.Type == typeof(string) ? Expression.OrElse(isNull!, Expression.Equal(member,
                    Expression.Constant(""))) : isNull ?? Expression.Constant(false);
            case FilterOperator.IsNotEmpty:
                return Expression.Not(ConditionOf(condition with { Operator = FilterOperator.IsEmpty }, item, type)!);
        }

        var values = condition.Values.Where(value => value is not (null or "")).Select(value => value!).ToList();
        if (values.Count == 0)
        {
            return null;
        }

        if (member.Type == typeof(string))
        {
            return condition.Operator == FilterOperator.IsAnyOf
                ? values.Select(value => TextOf(FilterOperator.EqualTo, member, isNull!, Lowered(value)))
                    .Aggregate(Expression.OrElse)
                : TextOf(condition.Operator, member, isNull!, Lowered(values[0]));
        }

        if (plain == typeof(DateTime) && values[0] is DateOnly day)
        {
            return DayOf(condition.Operator, member, day);
        }

        // A whole-number property compares as a decimal, so a decimal value against it neither rounds nor overflows.
        if (NumberTypes.IsNumber(plain) && NumberTypes.IsWhole(plain))
        {
            var number = Expression.Convert(member, canBeNull ? typeof(decimal?) : typeof(decimal));
            return Matching(condition.Operator, values, number, value =>
                Expression.Constant(Convert.ToDecimal(value, CultureInfo.InvariantCulture), number.Type));
        }

        // Other numbers compare in their own type, since a double such as NaN can't convert to a decimal.
        if (NumberTypes.IsNumber(plain))
        {
            return Matching(condition.Operator, values, member, value =>
                Expression.Constant(Convert.ChangeType(value, plain, CultureInfo.InvariantCulture), member.Type));
        }

        return Matching(condition.Operator, values, member, value =>
            Expression.Constant(plain.IsEnum ? Enum.ToObject(plain, value) : value, member.Type));
    }

    private static Expression Matching(FilterOperator op, List<object> values, Expression target,
        Func<object, Expression> constantOf) => op switch
    {
        FilterOperator.IsAnyOf => values.Select(value => (Expression)Expression.Equal(target, constantOf(value)))
            .Aggregate(Expression.OrElse),
        FilterOperator.EqualTo => Expression.Equal(target, constantOf(values[0])),
        FilterOperator.NotEqualTo => Expression.Not(Expression.Equal(target, constantOf(values[0]))),
        _ => Compare(op, target, constantOf(values[0])),
    };

    private static string Lowered(object value) => value.ToString()!.ToLower(CultureInfo.CurrentCulture);

    // Both sides go through ToLower, which a database can translate, and null text meets only the negative comparisons.
    private static Expression TextOf(FilterOperator op, Expression member, Expression isNull, string wanted)
    {
        var lowered = Expression.Call(member, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
        Expression Call(string method) => Expression.Call(lowered,
            typeof(string).GetMethod(method, [typeof(string)])!, Expression.Constant(wanted));
        Expression Present(Expression test) => Expression.AndAlso(Expression.Not(isNull), test);
        return op switch
        {
            FilterOperator.Contains => Present(Call(nameof(string.Contains))),
            FilterOperator.DoesNotContain => Expression.OrElse(isNull, Expression.Not(Call(nameof(string.Contains)))),
            FilterOperator.StartsWith => Present(Call(nameof(string.StartsWith))),
            FilterOperator.EndsWith => Present(Call(nameof(string.EndsWith))),
            FilterOperator.NotEqualTo => Expression.OrElse(isNull,
                Expression.NotEqual(lowered, Expression.Constant(wanted))),
            _ => Present(Expression.Equal(lowered, Expression.Constant(wanted))),
        };
    }

    // A date and time is on a day when it's from that day's start up to the next day's.
    private static Expression DayOf(FilterOperator op, Expression member, DateOnly day)
    {
        Expression Start(DateOnly date) => Expression.Constant(date.ToDateTime(TimeOnly.MinValue), member.Type);
        var next = day.AddDays(1);
        return op switch
        {
            FilterOperator.EqualTo => Expression.AndAlso(Expression.GreaterThanOrEqual(member, Start(day)),
                Expression.LessThan(member, Start(next))),
            FilterOperator.NotEqualTo => Expression.Not(DayOf(FilterOperator.EqualTo, member, day)),
            FilterOperator.LessThan => Expression.LessThan(member, Start(day)),
            FilterOperator.LessThanOrEqualTo => Expression.LessThan(member, Start(next)),
            FilterOperator.GreaterThan => Expression.GreaterThanOrEqual(member, Start(next)),
            _ => Expression.GreaterThanOrEqual(member, Start(day)),
        };
    }

    private static Expression Compare(FilterOperator op, Expression member, Expression value) => op switch
    {
        FilterOperator.LessThan => Expression.LessThan(member, value),
        FilterOperator.LessThanOrEqualTo => Expression.LessThanOrEqual(member, value),
        FilterOperator.GreaterThan => Expression.GreaterThan(member, value),
        FilterOperator.GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(member, value),
        _ => throw new InvalidOperationException($"A query can't compare a value with {op}."),
    };
}
