using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class CalendarExampleTests : DocsTest
{
    [Fact]
    public void PickExample_ShouldHoldTheMonthsFirstDay_WhenAMonthIsPicked()
    {
        var cut = Render<CalendarPickExample>();

        cut.FindAll("[data-part=month]")[2].Click();

        Assert.Matches(@"Report month value: \d{4}-03-01", cut.Find("output").TextContent);
    }

    [Fact]
    public void WeekNumbersExample_ShouldNumberTheWeeksByIso8601_WhenRendered()
    {
        var cut = Render<CalendarWeekNumbersExample>();

        Assert.Equal(["53", "1", "2", "3", "4"],
            cut.FindAll("[data-part=week]").Select(week => week.TextContent.Trim()));
    }
}
