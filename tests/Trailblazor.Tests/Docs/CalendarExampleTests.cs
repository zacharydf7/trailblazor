using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class CalendarExampleTests : DocsTest
{
    [Fact]
    public void WeekNumbersExample_ShouldNumberTheWeeksByIso8601_WhenRendered()
    {
        var cut = Render<CalendarWeekNumbersExample>();

        Assert.Equal(["53", "1", "2", "3", "4"],
            cut.FindAll("[data-part=week]").Select(week => week.TextContent.Trim()));
    }
}
