using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Trailblazor.Tests;

public class QueryGroupTests
{
    public enum Difficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public sealed record Trail(string Name, string? Region, double LengthKm, int Sites, DateOnly Opened,
        DateTime LastVisit, bool Loop, Difficulty Difficulty, Difficulty? Rated = null)
    {
        [Display(Name = "Trail code")]
        public string Code { get; init; } = "";

        public Guid Id { get; init; }
    }

    private static readonly Trail[] Trails =
    [
        new("Pine Hollow", "North ridge", 6.4, 12, new(2026, 5, 2), new(2026, 6, 1, 8, 30, 0), true, Difficulty.Easy,
            Difficulty.Easy),
        new("Eagle Ridge", "North ridge", 14.2, 4, new(2026, 6, 14), new(2026, 6, 1, 23, 59, 0), false,
            Difficulty.Hard),
        new("Cedar Flats", null, 3.1, 30, new(2026, 4, 20), new(2026, 6, 2, 0, 0, 0), false, Difficulty.Easy),
        new("Ridge Loop", "", 8.5, 0, new(2026, 8, 23), new(2026, 5, 31, 12, 0, 0), true, Difficulty.Moderate),
    ];

    private static IReadOnlyList<string> Matching(QueryGroup query) =>
        [.. Trails.AsQueryable().Where(query.ToExpression<Trail>()).Select(trail => trail.Name)];

    private static QueryGroup Where(params GridFilter[] conditions) => new() { Conditions = conditions };

    [Fact]
    public void ToExpression_ShouldMatchEveryItem_WhenTheQueryIsEmpty()
    {
        Assert.Equal(4, Matching(new QueryGroup()).Count);
    }

    [Theory]
    [InlineData(FilterOperator.Contains, "RIDGE", "Eagle Ridge,Ridge Loop")]
    [InlineData(FilterOperator.DoesNotContain, "ridge", "Pine Hollow,Cedar Flats")]
    [InlineData(FilterOperator.StartsWith, "ridge", "Ridge Loop")]
    [InlineData(FilterOperator.EndsWith, "FLATS", "Cedar Flats")]
    [InlineData(FilterOperator.EqualTo, "pine hollow", "Pine Hollow")]
    [InlineData(FilterOperator.NotEqualTo, "pine hollow", "Eagle Ridge,Cedar Flats,Ridge Loop")]
    public void ToExpression_ShouldCompareTextIgnoringCase_WhenGivenATextCondition(FilterOperator op, string text,
        string expected)
    {
        Assert.Equal(expected.Split(','), Matching(Where(new GridFilter("Name", op, text))));
    }

    [Fact]
    public void ToExpression_ShouldTreatMissingTextAsNoMatchButAsADifference_WhenAValueIsNull()
    {
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Matching(Where(new GridFilter("Region", FilterOperator.Contains, "north"))));
        Assert.Equal(["Cedar Flats", "Ridge Loop"],
            Matching(Where(new GridFilter("Region", FilterOperator.DoesNotContain, "north"))));
    }

    [Fact]
    public void ToExpression_ShouldMatchNullAndEmptyText_WhenTheConditionIsIsEmpty()
    {
        Assert.Equal(["Cedar Flats", "Ridge Loop"], Matching(Where(new GridFilter("Region", FilterOperator.IsEmpty))));
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Matching(Where(new GridFilter("Region", FilterOperator.IsNotEmpty))));
    }

    [Theory]
    [InlineData("LengthKm", FilterOperator.GreaterThan, 8.5, "Eagle Ridge")]
    [InlineData("LengthKm", FilterOperator.GreaterThanOrEqualTo, 8.5, "Eagle Ridge,Ridge Loop")]
    [InlineData("Sites", FilterOperator.LessThan, 12, "Eagle Ridge,Ridge Loop")]
    [InlineData("Sites", FilterOperator.LessThanOrEqualTo, 12, "Pine Hollow,Eagle Ridge,Ridge Loop")]
    [InlineData("Sites", FilterOperator.EqualTo, 30, "Cedar Flats")]
    [InlineData("Sites", FilterOperator.NotEqualTo, 30, "Pine Hollow,Eagle Ridge,Ridge Loop")]
    public void ToExpression_ShouldCompareNumbersOfAnyType_WhenGivenADecimal(string field, FilterOperator op,
        double value, string expected)
    {
        Assert.Equal(expected.Split(','), Matching(Where(new GridFilter(field, op, (decimal)value))));
    }

    [Theory]
    [InlineData(FilterOperator.GreaterThanOrEqualTo, 4.5, "Pine Hollow,Cedar Flats")]
    [InlineData(FilterOperator.LessThan, 4.5, "Eagle Ridge,Ridge Loop")]
    [InlineData(FilterOperator.EqualTo, 4.5, "")]
    [InlineData(FilterOperator.LessThan, 3000000000.0, "Pine Hollow,Eagle Ridge,Cedar Flats,Ridge Loop")]
    public void ToExpression_ShouldCompareAWholeNumberWithoutRounding_WhenGivenAFractionOrALargeNumber(FilterOperator op,
        double value, string expected)
    {
        Assert.Equal(expected.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Matching(Where(new GridFilter("Sites", op, (decimal)value))));
    }

    [Fact]
    public void ToExpression_ShouldLeaveOutARowWithoutANumber_WhenADoubleIsNotANumber()
    {
        var trails = new[] { Trails[0] with { LengthKm = double.NaN }, Trails[1] };

        var matching = trails.AsQueryable().Where(Where(new GridFilter("LengthKm", FilterOperator.GreaterThan, 5m))
            .ToExpression<Trail>());

        Assert.Equal(["Eagle Ridge"], matching.Select(trail => trail.Name));
    }

    [Fact]
    public void ToExpression_ShouldMatchAnyOfSeveralTexts_WhenAChoiceHoldsText()
    {
        Assert.Equal(["Cedar Flats", "Ridge Loop"],
            Matching(Where(new GridFilter("Name", FilterOperator.IsAnyOf, new[] { "cedar flats", "RIDGE LOOP" }))));
    }

    [Fact]
    public void ToExpression_ShouldLowerBothSidesInTheSameCulture_WhenTextIsCompared()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
        try
        {
            var trails = new[] { Trails[0] with { Name = "İzmir loop" } };

            var query = Where(new GridFilter("Name", FilterOperator.StartsWith, "İZMİR"));

            Assert.Single(trails.AsQueryable().Where(query.ToExpression<Trail>()));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void ToExpression_ShouldCompareDays_WhenGivenADateForADateOnly()
    {
        Assert.Equal(["Pine Hollow", "Cedar Flats"],
            Matching(Where(new GridFilter("Opened", FilterOperator.LessThan, new DateOnly(2026, 6, 1)))));
    }

    [Theory]
    [InlineData(FilterOperator.EqualTo, "Pine Hollow,Eagle Ridge")]
    [InlineData(FilterOperator.NotEqualTo, "Cedar Flats,Ridge Loop")]
    [InlineData(FilterOperator.LessThan, "Ridge Loop")]
    [InlineData(FilterOperator.LessThanOrEqualTo, "Pine Hollow,Eagle Ridge,Ridge Loop")]
    [InlineData(FilterOperator.GreaterThan, "Cedar Flats")]
    [InlineData(FilterOperator.GreaterThanOrEqualTo, "Pine Hollow,Eagle Ridge,Cedar Flats")]
    public void ToExpression_ShouldMatchADateAndTimeAnywhereOnTheDay_WhenGivenADate(FilterOperator op,
        string expected)
    {
        Assert.Equal(expected.Split(','), Matching(Where(new GridFilter("LastVisit", op, new DateOnly(2026, 6, 1)))));
    }

    [Fact]
    public void ToExpression_ShouldCompareChoicesAndYesOrNo_WhenGivenThem()
    {
        Assert.Equal(["Pine Hollow", "Ridge Loop"], Matching(Where(new GridFilter("Loop", FilterOperator.EqualTo, true))));
        Assert.Equal(["Eagle Ridge", "Ridge Loop"], Matching(Where(new GridFilter("Difficulty", FilterOperator.IsAnyOf,
            new[] { Difficulty.Hard, Difficulty.Moderate }))));
        Assert.Equal(["Pine Hollow"], Matching(Where(new GridFilter("Rated", FilterOperator.EqualTo, Difficulty.Easy))));
        Assert.Equal(["Eagle Ridge", "Cedar Flats", "Ridge Loop"], Matching(Where(new GridFilter("Rated", FilterOperator.IsEmpty))));
    }

    [Fact]
    public void ToExpression_ShouldJoinConditionsAndInnerGroups_WhenTheyHaveTheirOwnJoins()
    {
        var query = new QueryGroup
        {
            Join = QueryJoin.Or,
            Conditions = [new("Name", FilterOperator.StartsWith, "pine")],
            Groups =
            [
                new()
                {
                    Conditions = [new("Loop", FilterOperator.EqualTo, false), new("Sites", FilterOperator.LessThan, 10m)],
                },
            ],
        };

        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Matching(query));
    }

    [Fact]
    public void ToExpression_ShouldLeaveOutACondition_WhenItIsWaitingForItsValue()
    {
        Assert.Equal(["Eagle Ridge"], Matching(Where(new GridFilter("Name", FilterOperator.Contains), new GridFilter("Name",
            FilterOperator.EqualTo, ""), new GridFilter("Difficulty", FilterOperator.IsAnyOf, Array.Empty<Difficulty>()),
            new GridFilter("Sites", FilterOperator.EqualTo, 4m))));
    }

    [Fact]
    public void ToExpression_ShouldMatchAsBefore_WhenTheQueryIsReadBackFromJson()
    {
        var query = new QueryGroup
        {
            Join = QueryJoin.Or,
            Conditions =
            [
                new("Difficulty", FilterOperator.IsAnyOf, new[] { Difficulty.Moderate }),
                new("LastVisit", FilterOperator.EqualTo, new DateOnly(2026, 6, 2)),
                new("Sites", FilterOperator.EqualTo, 4m),
            ],
        };

        var json = JsonSerializer.Serialize(query);
        var read = JsonSerializer.Deserialize<QueryGroup>(json)!;

        Assert.Contains("\"Join\":\"Or\"", json);
        Assert.Equal(["Eagle Ridge", "Cedar Flats", "Ridge Loop"], Matching(read));
    }

    [Fact]
    public void ToExpression_ShouldThrow_WhenAConditionNamesNoPropertyOfTheType()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Where(new GridFilter("Shipping", FilterOperator.IsEmpty))
            .ToExpression<Trail>());

        Assert.Contains("Shipping", error.Message);
    }

    [Fact]
    public void For_ShouldListAFieldForEachPropertyAQueryCanCompare_WhenGivenAType()
    {
        var fields = QueryField.For<Trail>();

        Assert.Equal(["Name", "Region", "LengthKm", "Sites", "Opened", "LastVisit", "Loop", "Difficulty", "Rated", "Code"],
            fields.Select(field => field.Key));
        Assert.Equal([QueryFieldKind.Text, QueryFieldKind.Text, QueryFieldKind.Number, QueryFieldKind.Number,
            QueryFieldKind.Date, QueryFieldKind.Date, QueryFieldKind.YesNo, QueryFieldKind.Choice, QueryFieldKind.Choice,
            QueryFieldKind.Text], fields.Select(field => field.Kind));
        Assert.Equal(["Length km", "Last visit", "Trail code"],
            fields.Where(field => field.Key is "LengthKm" or "LastVisit" or "Code").Select(field => field.Label));
        Assert.Equal([(Difficulty.Easy, "Easy"), (Difficulty.Moderate, "Moderate"), (Difficulty.Hard, "Hard")],
            fields.Single(field => field.Key == "Rated").Choices.Select(choice => ((Difficulty)choice.Value, choice.Label)));
    }
}
