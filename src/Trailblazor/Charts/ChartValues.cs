using System.Globalization;

namespace Trailblazor;

// Reads what a chart's functions return: a series' value as a number, and an item's place along the chart as text.
internal static class ChartValues
{
    // Any number type; null, or a value that isn't finite, leaves a gap.
    public static double? Number(object? value, string series)
    {
        if (value is null)
        {
            return null;
        }

        if (!IsNumber(value))
        {
            throw new InvalidOperationException(
                $"The chart series \"{series}\" got a value of type {value.GetType().Name}, which isn't a number.");
        }

        var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return double.IsFinite(number) ? number : null;
    }

    // Whether a value is of a number type. An enum is not, though it has a number under it.
    public static bool IsNumber(object value) =>
        value is not Enum && Type.GetTypeCode(value.GetType()) is >= TypeCode.SByte and <= TypeCode.Decimal;

    // An enum reads as its name in words, and a date without a time of day as a short date, unless a format says
    // otherwise.
    public static string Text(object? value, string? format, CultureInfo culture) => value switch
    {
        null => "",
        string text => text,
        Enum name when format is null => NameWords.Of(name.ToString()),
        DateTime { TimeOfDay.Ticks: 0 } date when format is null => date.ToString("d", culture),
        DateTime moment when format is null => moment.ToString("g", culture),
        DateTimeOffset moment when format is null => moment.ToString("g", culture),
        IFormattable formattable => formattable.ToString(format, culture),
        _ => value.ToString() ?? "",
    };
}
