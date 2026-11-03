using System.Globalization;

namespace Trailblazor;

// Whether a row's value meets a filter, for rows a grid filters in memory. The text operators read the column's text,
// or its FilterBy value's text, which is only formatted when one of them asks; the rest compare the value itself.
internal static class FilterMatch
{
    public static bool Matches(object? value, Func<string?> text, GridFilter filter)
    {
        var compare = CultureInfo.CurrentCulture.CompareInfo;
        var wanted = filter.Value?.ToString() ?? "";
        return filter.Operator switch
        {
            FilterOperator.IsEmpty => IsEmpty(value),
            FilterOperator.IsNotEmpty => !IsEmpty(value),
            FilterOperator.Contains => compare.IndexOf(text() ?? "", wanted, CompareOptions.IgnoreCase) >= 0,
            FilterOperator.DoesNotContain => compare.IndexOf(text() ?? "", wanted, CompareOptions.IgnoreCase) < 0,
            FilterOperator.StartsWith => compare.IsPrefix(text() ?? "", wanted, CompareOptions.IgnoreCase),
            FilterOperator.EndsWith => compare.IsSuffix(text() ?? "", wanted, CompareOptions.IgnoreCase),
            FilterOperator.NotEqualTo => Compare(value, filter.Value) != 0,
            _ => Compare(value, filter.Value) is { } order && filter.Operator switch
            {
                FilterOperator.EqualTo => order == 0,
                FilterOperator.LessThan => order < 0,
                FilterOperator.LessThanOrEqualTo => order <= 0,
                FilterOperator.GreaterThan => order > 0,
                FilterOperator.GreaterThanOrEqualTo => order >= 0,
                _ => false,
            },
        };
    }

    private static bool IsEmpty(object? value) => value is null or string { Length: 0 };

    // Values that can't be compared, such as a missing one, meet no comparison but NotEqualTo. A float compares at
    // its own precision, since a typed 8.1 isn't the double nearest the float 8.1.
    private static int? Compare(object? value, object? wanted) => (value, wanted) switch
    {
        (null, _) or (_, null) => null,
        _ when NumberTypes.IsNumber(value.GetType()) && NumberTypes.IsNumber(wanted.GetType()) => (value, wanted) switch
        {
            (double, _) or (_, double) => Convert.ToDouble(value, CultureInfo.InvariantCulture)
                .CompareTo(Convert.ToDouble(wanted, CultureInfo.InvariantCulture)),
            (float, _) or (_, float) => Convert.ToSingle(value, CultureInfo.InvariantCulture)
                .CompareTo(Convert.ToSingle(wanted, CultureInfo.InvariantCulture)),
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
                .CompareTo(Convert.ToDecimal(wanted, CultureInfo.InvariantCulture)),
        },
        (_, DateOnly day) when DayOf(value) is { } valueDay => valueDay.CompareTo(day),
        (string text, string other) => CultureInfo.CurrentCulture.CompareInfo.Compare(text, other,
            CompareOptions.IgnoreCase),
        _ when value.GetType() == wanted.GetType() && value is IComparable comparable => comparable.CompareTo(wanted),
        _ when value.Equals(wanted) => 0,
        _ => null,
    };

    private static DateOnly? DayOf(object value) => value switch
    {
        DateOnly day => day,
        DateTime time => DateOnly.FromDateTime(time),
        DateTimeOffset time => DateOnly.FromDateTime(time.DateTime),
        _ => null,
    };
}
