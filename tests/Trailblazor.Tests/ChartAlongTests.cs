using System.Globalization;

namespace Trailblazor.Tests;

public class ChartAlongTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void Even_ShouldGiveEachItemOnePlaceOfTheSameWidth_WhenCalled()
    {
        var along = ChartAlong.Even(4);

        Assert.Equal([0.125, 0.375, 0.625, 0.875], along.At);
        Assert.Equal((0.25, 4d, false), (along.Slot, along.Steps, along.ByValue));
        Assert.Empty(along.Marks);
    }

    [Fact]
    public void Continuous_ShouldPlaceEachItemByItsValueAndMarkRoundValues_WhenGivenNumbers()
    {
        var along = Continuous([0, 10m, 40.0]);

        // The plot runs from the first value to the last, and a place is as wide as the smallest gap.
        Assert.Equal([0, 0.25, 1], along.At);
        Assert.Equal((0.25, 4d, true), (along.Slot, along.Steps, along.ByValue));
        Assert.Equal([(0, "0"), (0.25, "10"), (0.5, "20"), (0.75, "30"), (1, "40")], along.Marks);
    }

    [Fact]
    public void Continuous_ShouldLeaveHalfTheSmallestGapAtEachEnd_WhenTheChartHasBars()
    {
        var along = Continuous([0, 10, 40], bars: true);

        Assert.Equal([0.1, 0.3, 0.9], along.At.Select(at => Math.Round(at, 9)));
        Assert.Equal(0.2, along.Slot, 9);
        Assert.Equal(["0", "10", "20", "30", "40"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldNotMarkWholeNumbersByTheHalf_WhenTheItemsAreOneApart()
    {
        // A fifth of the span would be a step of a half, and years read without digit grouping.
        var along = Continuous([2020, 2021, 2022]);

        Assert.Equal([(0, "2020"), (0.5, "2021"), (1, "2022")], along.Marks);
        Assert.Equal(2, along.Steps);
    }

    [Theory]
    [InlineData(1, 4, "2 4")]
    [InlineData(2021, 2024, "2022 2024")]
    [InlineData(1.2, 1.9, "1.2 1.4 1.6 1.8")]
    public void Continuous_ShouldTakeTheLongestStepThatGivesTwoMarks_WhenNoneOfTheItemsSpacingDoes(object first,
        object last, string marks)
    {
        // The items' spacing rounds up to a step of 5, or of 1 for the last pair, which has no mark between them.
        // The axis is not then marked by the half, or by the tenth.
        var along = Continuous([first, last]);

        Assert.Equal(marks, string.Join(' ', along.Marks.Select(mark => mark.Text)));
    }

    [Fact]
    public void Continuous_ShouldWriteAWholeMarkAsTheItemsAreWritten_WhenTheirFormatSuitsOnlyWholeNumbers()
    {
        var along = ChartAlong.Continuous([2020, 2021, 2022], bars: false, "D5", markFormat: null, English);

        Assert.Equal(["02020", "02021", "02022"], along.Marks.Select(mark => mark.Text));
    }

    [Theory]
    [InlineData(2021, 2024, "2022 2024")]
    [InlineData(2019, 2024, "2020 2022 2024")]
    public void Continuous_ShouldMarkEverySecondYear_WhenALongerStepHasFewerThanTwoMarksBetweenTwoDates(int from,
        int to, string marks)
    {
        // No step of five years has a mark between the first pair, and only 2020 lies between the second.
        var along = Continuous([new DateOnly(from, 6, 1), new DateOnly(to, 6, 1)]);

        Assert.Equal(marks, string.Join(' ', along.Marks.Select(mark => mark.Text)));
    }

    [Fact]
    public void Continuous_ShouldWriteAMarkBeyondTheItemsOwnType_WhenBarsWidenTheAxisPastIt()
    {
        // Half the gap past 250 is beyond what a byte holds, and the mark at 300 is written all the same.
        var along = ChartAlong.Continuous([(byte)100, (byte)250], bars: true, "D3", markFormat: null, English);

        Assert.Equal(["100", "200", "300"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldPlaceEachItemByItsDateAndMarkDays_WhenGivenDates()
    {
        var along = Continuous([new DateTime(2026, 7, 1), new DateOnly(2026, 7, 2),
            new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.FromHours(-5))]);

        // A date with an offset is placed by its own clock time.
        Assert.Equal([0, 0.125, 1], along.At);
        Assert.Equal((0.125, 8d), (along.Slot, along.Steps));
        Assert.Equal(["Jul 1", "Jul 2", "Jul 3", "Jul 4", "Jul 5", "Jul 6", "Jul 7", "Jul 8", "Jul 9"],
            along.Marks.Select(mark => mark.Text));
        Assert.Equal([0, 0.5, 1], new[] { along.Marks[0].X, along.Marks[4].X, along.Marks[8].X });
    }

    [Fact]
    public void Continuous_ShouldMarkTheMonths_WhenTheItemsAreAMonthOf31DaysApart()
    {
        // Two months have room for a mark every two weeks, and 31 days are a little more than a mean month.
        var along = Continuous([new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1)]);

        Assert.Equal(["Jul 2026", "Aug 2026", "Sep 2026"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldStillMarkTheAxis_WhenNoRoundTimeOfTheItemsSpacingIsBetweenThem()
    {
        // The two are nearly a month apart, and no first of a month lies between them: the longest step that puts
        // two marks there is two weeks.
        var along = Continuous([new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 30)]);

        Assert.True(along.ByValue);
        Assert.Equal([(0, "Jan 2"), (0.5, "Jan 16"), (1, "Jan 30")], along.Marks);
    }

    [Fact]
    public void Continuous_ShouldWriteItsMarksAsTheItemsAreWritten_WhenGivenAFormatForNumbers()
    {
        var along = ChartAlong.Continuous([0, 0.5, 2.5], bars: false, "0.0 km", markFormat: null, English);

        Assert.Equal(["0.0 km", "0.5 km", "1.0 km", "1.5 km", "2.0 km", "2.5 km"],
            along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldWriteItsMarksAsTheirOwnFormatSays_WhenGivenOne()
    {
        var days = new object?[] { new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 3) };

        // The items' format is for the tooltip and the table; a date axis's marks don't fall back to it, since it
        // may not suit their step.
        var dates = ChartAlong.Continuous(days, bars: false, "MMMM d, yyyy", markFormat: "d MMM", English);
        var unmarked = ChartAlong.Continuous(days, bars: false, "MMMM d, yyyy", markFormat: null, English);
        var numbers = ChartAlong.Continuous([0, 0.5, 1], bars: false, "0.0 km", markFormat: "0.#", English);

        Assert.Equal(["1 Jul", "2 Jul", "3 Jul"], dates.Marks.Select(mark => mark.Text));
        Assert.Equal(["Jul 1", "Jul 2", "Jul 3"], unmarked.Marks.Select(mark => mark.Text));
        Assert.Equal(["0", "0.5", "1"], numbers.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldWriteADayAsItsCultureDoes_WhenTheCultureIsNotEnglish()
    {
        var along = ChartAlong.Continuous([new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2)], bars: false,
            format: null, markFormat: null, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal(["1. Juli", "2. Juli"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldPutTheItemsInTheMiddleUnderOneMark_WhenTheyShareOneValue()
    {
        var along = Continuous([5, 5], bars: true);

        Assert.Equal([0.5, 0.5], along.At);
        Assert.Equal(1, along.Slot);
        Assert.Equal([(0.5, "5")], along.Marks);
    }

    [Fact]
    public void Continuous_ShouldBeEmpty_WhenThereAreNoItems()
    {
        var along = Continuous([]);

        Assert.Empty(along.At);
        Assert.Empty(along.Marks);
        Assert.True(along.ByValue);
    }

    [Theory]
    [InlineData("Jul", "Jul (String)")]
    [InlineData(null, "null")]
    [InlineData(double.NaN, "NaN (Double)")]
    [InlineData(DayOfWeek.Monday, "Monday (DayOfWeek)")]
    public void Continuous_ShouldSayWhatItGot_WhenAnXIsNotANumberOrADate(object? value, string got)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Continuous([1, value]));

        Assert.Equal("A chart with a continuous scale needs every item's X to be a number or a date, and got "
            + $"{got}.", thrown.Message);
    }

    [Fact]
    public void Continuous_ShouldRunBetweenTheAppsEnds_WhenMinAndMaxAreGiven()
    {
        var along = Continuous([10, 20, 30], ends: new(0, 40, null, false));

        Assert.Equal([0.25, 0.5, 0.75], along.At);
        Assert.Equal([(0, "0"), (0.25, "10"), (0.5, "20"), (0.75, "30"), (1, "40")], along.Marks);
    }

    [Fact]
    public void Continuous_ShouldMarkTheMultiplesOfTheAppsStep_WhenGivenOne()
    {
        var along = Continuous([0, 10, 40], ends: new(null, null, 20, false));

        Assert.Equal([(0, "0"), (0.5, "20"), (1, "40")], along.Marks);
        Assert.Equal(2, along.Steps);
    }

    [Fact]
    public void Continuous_ShouldPassOverTheAppsStep_WhenItWouldGiveMoreThanFiftyMarks()
    {
        var along = Continuous([0, 100], ends: new(null, null, 2, false));

        Assert.Equal(["0", "100"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldPassOverTheAppsStep_WhenItWouldLeaveFewerThanTwoMarks()
    {
        var along = Continuous([1, 2, 4], ends: new(null, null, 10, false));

        // No multiple of ten lies between 1 and 4, so the axis keeps its own marks.
        Assert.Equal(["1", "2", "3", "4"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldMoveAnEndOutToTheNearestMark_WhenRounded()
    {
        var along = Continuous([2.4, 7.9, 14.2], ends: new(null, null, null, true));

        // The marks are five apart, so the plot runs from 0 to 15 and holds every item inside its edges.
        Assert.Equal([0.16, 0.527, 0.947], along.At.Select(at => Math.Round(at, 3)));
        Assert.Equal(["0", "5", "10", "15"], along.Marks.Select(mark => mark.Text));
        Assert.Equal(3, along.Steps);
    }

    [Fact]
    public void Continuous_ShouldLeaveAnItemOnAMarkWhereItIs_WhenRounded()
    {
        var along = Continuous([2020, 2021, 2022], ends: new(null, null, null, true));

        // The first and the last year are marks themselves, so the plot still runs from one to the other.
        Assert.Equal([0, 0.5, 1], along.At);
    }

    [Theory]
    [InlineData(30, null, 30d, 40d)]
    [InlineData(null, 5, -5d, 5d)]
    public void Continuous_ShouldMoveTheOtherEnd_WhenAnEndIsSetBeyondEveryItem(int? min, int? max, double first,
        double last)
    {
        var along = Continuous([10, 20], ends: new(min, max, null, false));

        // The plot is as wide as the items span, from the end the app gave, and the items lie outside it.
        Assert.Equal([(10 - first) / (last - first), (20 - first) / (last - first)], along.At);
        Assert.All(along.At, at => Assert.False(at is > 0 and < 1));
    }

    [Fact]
    public void Continuous_ShouldPlaceOneItemBetweenTheAppsEnds_WhenItIsTheOnlyValue()
    {
        var along = Continuous([5, 5], bars: true, ends: new(0, 20, null, false));

        // Its place is as wide as a step of the app's range, a quarter of the plot here, not the whole range.
        Assert.Equal([0.25, 0.25], along.At);
        Assert.Equal(0.25, along.Slot);
        Assert.Equal(["0", "5", "10", "15", "20"], along.Marks.Select(mark => mark.Text));
    }

    [Fact]
    public void Continuous_ShouldKeepABarsPlaceInsideThePlot_WhenOnlyOneEndIsSetAndItIsTheOnlyValue()
    {
        var along = Continuous([5, 5], bars: true, ends: new(null, 20, null, false));

        // The end left to the chart moves out by half a place, so the place starts at the plot's edge.
        Assert.Equal(along.Slot / 2, along.At[0], 9);
        Assert.Equal("20", along.Marks[^1].Text);
    }

    [Fact]
    public void Continuous_ShouldRunBetweenTheAppsDates_WhenGivenDatesAsEnds()
    {
        var along = Continuous([new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 10)],
            ends: new(new DateOnly(2026, 1, 1), new DateTime(2026, 1, 31), null, false));

        Assert.Equal([0.066667, 0.3], along.At.Select(at => Math.Round(at, 6)));
    }

    [Fact]
    public void Continuous_ShouldSayWhatKindOfEndItNeeds_WhenAnEndIsNotOfTheItemsKind()
    {
        var onNumbers = Assert.Throws<InvalidOperationException>(() =>
            Continuous([1, 2], ends: new(new DateOnly(2026, 1, 1), null, null, false)));
        var onDates = Assert.Throws<InvalidOperationException>(() =>
            Continuous([new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 10)], ends: new(null, 31, null, false)));

        const string message = "TbXAxis needs a Min and a Max of the kind its items' X values are: numbers for "
            + "numbers, dates for dates.";
        Assert.Equal((message, message), (onNumbers.Message, onDates.Message));
    }

    private static ChartAlong Continuous(object?[] values, bool bars = false, ChartAlongEnds ends = default) =>
        ChartAlong.Continuous(values, bars, format: null, markFormat: null, English, ends);
}
