namespace Trailblazor.Tests;

public class ChartTimeMarksTests
{
    [Fact]
    public void Between_ShouldMarkEverySecondDay_WhenNineDaysAreTooManyForOneEach()
    {
        var (marks, format, steps) = ChartTimeMarks.Between(new(2026, 7, 1), new(2026, 7, 10), most: 8);

        Assert.Equal([new(2026, 7, 1), new(2026, 7, 3), new(2026, 7, 5), new(2026, 7, 7), new(2026, 7, 9)], marks);
        Assert.Equal(("M", 4.5), (format, steps));
    }

    [Fact]
    public void Between_ShouldNotMarkFinerThanTheItemsAre_WhenGivenTheirSmallestGap()
    {
        // Two days have room for a mark every six hours, which says nothing of values a day apart.
        var hourly = ChartTimeMarks.Between(new(2026, 7, 1), new(2026, 7, 3), most: 8);
        var daily = ChartTimeMarks.Between(new(2026, 7, 1), new(2026, 7, 3), most: 8, TimeSpan.FromDays(1));
        // Four months have room for a mark every two weeks, which says nothing of values a month apart.
        var monthly = ChartTimeMarks.Between(new(2026, 1, 1), new(2026, 5, 1), most: 8, TimeSpan.FromDays(28));

        Assert.Equal((9, "t"), (hourly.Marks.Count, hourly.Format));
        Assert.Equal([new(2026, 7, 1), new(2026, 7, 2), new(2026, 7, 3)], daily.Marks);
        Assert.Equal([1, 2, 3, 4, 5], monthly.Marks.Select(mark => mark.Month));
        Assert.Equal("Y", monthly.Format);
    }

    [Fact]
    public void Between_ShouldTakeTheLongestStepWithTwoMarks_WhenNoneAsLongAsTheItemsSpacingHasAMarkBetweenTheTimes()
    {
        // No new year lies between March and November. Half years would put one mark there, and quarters three.
        var (marks, format, _) = ChartTimeMarks.Between(new(2001, 3, 1), new(2001, 11, 1), most: 8,
            TimeSpan.FromDays(245));

        Assert.Equal([new(2001, 4, 1), new(2001, 7, 1), new(2001, 10, 1)], marks);
        Assert.Equal("Y", format);
    }

    [Fact]
    public void Between_ShouldMarkTheTwoTimes_WhenNoRoundTimeIsBetweenThem()
    {
        var from = new DateTime(2026, 7, 1, 8, 0, 0, 200);

        var (marks, _, steps) = ChartTimeMarks.Between(from, from.AddMilliseconds(300), most: 8);

        Assert.Equal([from, from.AddMilliseconds(300)], marks);
        Assert.Equal(1, steps);
    }

    [Fact]
    public void Between_ShouldMarkTheQuartersOfTheYear_WhenTheTimesAreMonthsApart()
    {
        var (marks, format, _) = ChartTimeMarks.Between(new(2026, 1, 15), new(2026, 12, 20), most: 8);

        // The first of each month a whole number of quarters after January.
        Assert.Equal([new(2026, 4, 1), new(2026, 7, 1), new(2026, 10, 1)], marks);
        Assert.Equal("Y", format);
    }

    [Fact]
    public void Between_ShouldMarkWholeStepsIntoTheDay_WhenTheTimesAreHoursApart()
    {
        var (marks, format, _) = ChartTimeMarks.Between(new(2026, 7, 1, 8, 20, 0), new(2026, 7, 1, 17, 40, 0), most: 8);

        Assert.Equal([new(2026, 7, 1, 9, 0, 0), new(2026, 7, 1, 12, 0, 0), new(2026, 7, 1, 15, 0, 0)], marks);
        Assert.Equal("t", format);
    }

    [Fact]
    public void Between_ShouldMarkYearsThatDivideByTheStep_WhenTheTimesAreDecadesApart()
    {
        var (marks, format, _) = ChartTimeMarks.Between(new(1995, 6, 1), new(2026, 6, 1), most: 8);

        Assert.Equal([2000, 2005, 2010, 2015, 2020, 2025], marks.Select(mark => mark.Year));
        Assert.All(marks, mark => Assert.Equal((1, 1), (mark.Month, mark.Day)));
        Assert.Equal("yyyy", format);
    }

    [Fact]
    public void Between_ShouldMarkTheSecond_WhenTheTimesAreSecondsApart()
    {
        var from = new DateTime(2026, 7, 1, 8, 0, 0, 500);

        var (marks, format, _) = ChartTimeMarks.Between(from, from.AddSeconds(3), most: 8);

        Assert.Equal([new(2026, 7, 1, 8, 0, 1), new(2026, 7, 1, 8, 0, 2), new(2026, 7, 1, 8, 0, 3)], marks);
        Assert.Equal("T", format);
    }

    [Fact]
    public void Between_ShouldTakeItsLongestStep_WhenTheTimesAreMillenniaApart()
    {
        var (marks, _, _) = ChartTimeMarks.Between(new(100, 1, 1), new(3000, 1, 1), most: 8);

        Assert.Equal(30, marks.Count);
        Assert.Equal((100, 3000), (marks[0].Year, marks[^1].Year));
    }
}
