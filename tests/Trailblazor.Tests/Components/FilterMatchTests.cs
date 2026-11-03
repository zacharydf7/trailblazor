namespace Trailblazor.Tests.Components;

public class FilterMatchTests
{
    public static TheoryData<object?, string?, FilterOperator, object?, bool> Cases => new()
    {
        { "Pine Hollow", "Pine Hollow", FilterOperator.Contains, "hOLL", true },
        { "Pine Hollow", "Pine Hollow", FilterOperator.DoesNotContain, "holl", false },
        { "Pine Hollow", "Pine Hollow", FilterOperator.StartsWith, "pine", true },
        { "Pine Hollow", "Pine Hollow", FilterOperator.EndsWith, "pine", false },
        // The text operators read the shown text, such as a formatted date.
        { new DateOnly(2026, 5, 2), "5/2/2026", FilterOperator.Contains, "5/2", true },
        { null, null, FilterOperator.Contains, "a", false },
        { 8.5, "8.5", FilterOperator.EqualTo, 8.5m, true },
        { 8, "8", FilterOperator.EqualTo, 8.0, true },
        // A float compares at its own precision, as the number it shows.
        { 8.1f, "8.1", FilterOperator.EqualTo, 8.1m, true },
        { 8.1f, "8.1", FilterOperator.GreaterThan, 8.1m, false },
        { 8L, "8", FilterOperator.GreaterThan, 7, true },
        { 8, "8", FilterOperator.LessThanOrEqualTo, 8m, true },
        { 8, "8", FilterOperator.LessThan, 8m, false },
        { 8, "8", FilterOperator.GreaterThanOrEqualTo, 9, false },
        { new DateTime(2026, 5, 2, 23, 30, 0), "", FilterOperator.EqualTo, new DateOnly(2026, 5, 2), true },
        { new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.FromHours(-7)), "", FilterOperator.GreaterThan,
            new DateOnly(2026, 5, 1), true },
        { DayOfWeek.Monday, "Monday", FilterOperator.EqualTo, DayOfWeek.Monday, true },
        { true, "True", FilterOperator.EqualTo, false, false },
        { "ridge", "ridge", FilterOperator.EqualTo, "RIDGE", true },
        // A value that can't be compared, missing or of another kind, meets nothing but NotEqualTo.
        { null, null, FilterOperator.EqualTo, 3, false },
        { null, null, FilterOperator.NotEqualTo, 3, true },
        { "eight", "eight", FilterOperator.GreaterThan, 3, false },
        { "eight", "eight", FilterOperator.NotEqualTo, 8, true },
        { null, null, FilterOperator.IsEmpty, null, true },
        { "", "", FilterOperator.IsEmpty, null, true },
        { 0, "0", FilterOperator.IsEmpty, null, false },
        { 0, "0", FilterOperator.IsNotEmpty, null, true },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_ShouldCompareTheValueOrItsTextAsTheOperatorSays_WhenGivenAFilter(object? value, string? text,
        FilterOperator op, object? wanted, bool expected)
    {
        var matches = FilterMatch.Matches(value, () => text, new GridFilter("Column", op, wanted));

        Assert.Equal(expected, matches);
    }
}
