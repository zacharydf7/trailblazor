using System.Globalization;
using System.Text.Json;

namespace Trailblazor;

// A filter read back from JSON holds JSON elements for its values. Once the grid knows the filter's column, they're
// converted to the column's type, like the grid's own filters, so they match rows, show in the filter fields and reach
// a provider the same way filters the app sets do. A column with FilterBy has no known type, so its values follow
// their JSON.
internal static class FilterJson
{
    public static bool IsUntyped(GridFilter filter) => filter.Values.Any(value => value is JsonElement);

    public static GridFilter Typed(GridFilter filter, Type? type)
    {
        // The text operators match the column's text, whatever its type.
        var target = filter.Operator is FilterOperator.Contains or FilterOperator.DoesNotContain
            or FilterOperator.StartsWith or FilterOperator.EndsWith ? typeof(string) : type;
        return filter.Value is JsonElement { ValueKind: JsonValueKind.Array } values
            ? filter with { Value = values.EnumerateArray().Select(value => ValueOf(value, target)).ToList() }
            : filter.Value is JsonElement value ? filter with { Value = ValueOf(value, target) } : filter;
    }

    private static object? ValueOf(JsonElement value, Type? type) => value.ValueKind switch
    {
        JsonValueKind.True or JsonValueKind.False => value.GetBoolean(),
        JsonValueKind.Number when type is { IsEnum: true } && value.TryGetInt64(out var whole) =>
            Enum.ToObject(type, whole),
        // A number filter holds a decimal, which compares with any numeric type.
        JsonValueKind.Number => value.TryGetDecimal(out var number) ? number : value.GetDouble(),
        JsonValueKind.String => TextValueOf(value.GetString()!, type),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.GetRawText(),
    };

    // A date filter holds a day, whatever the column's type, as its range picker gives one.
    private static object TextValueOf(string text, Type? type) => type switch
    {
        { IsEnum: true } when Enum.TryParse(type, text, ignoreCase: true, out var choice) => choice!,
        _ when type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset) =>
            DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                ? day
                : type == typeof(DateTimeOffset) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var moment) ? moment
                : DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
                    ? time : text,
        _ when type == typeof(TimeOnly) && TimeOnly.TryParse(text, CultureInfo.InvariantCulture, out var clock) =>
            clock,
        _ when type == typeof(Guid) && Guid.TryParse(text, out var id) => id,
        _ => text,
    };
}
