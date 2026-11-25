using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class TbTimestampTests : BunitContext
{
    private static readonly DateTimeOffset Expiry = new(2026, 11, 25, 15, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Tokyo =
        TimeZoneInfo.CreateCustomTimeZone("Tokyo", TimeSpan.FromHours(9), "Tokyo", "Tokyo");
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

    public TbTimestampTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
    }

    private IRenderedComponent<TbTimestamp> RenderStamp(DateTimeOffset? value,
        Action<ComponentParameterCollectionBuilder<TbTimestamp>>? more = null,
        Action<TrailblazorOptions>? configure = null)
    {
        this.AddTrailblazor(configure);
        return Render<TbTimestamp>(parameters =>
        {
            parameters.Add(timestamp => timestamp.Value, value);
            more?.Invoke(parameters);
        });
    }

    private static string Shown(DateTimeOffset value, TimeZoneInfo zone, string format) =>
        TimeZoneInfo.ConvertTime(value, zone).ToString(format, CultureInfo.CurrentCulture);

    [Fact]
    public void TbTimestamp_ShouldShowValueInTheCascadedTimeZone_WhenOneIsCascaded()
    {
        // The cascade is closer to the timestamp than the app's zone, so it wins.
        this.AddTrailblazor(options => options.Times.TimeZone = NewYork);

        var cut = Render<CascadingValue<TimeZoneInfo>>(parameters => parameters.Add(cascade => cascade.Value, Tokyo)
            .AddChildContent<TbTimestamp>(timestamp => timestamp.Add(stamp => stamp.Value, Expiry)
                .Add(stamp => stamp.Format, TimestampFormat.Time)));

        Assert.Equal(Shown(Expiry, Tokyo, "t"), cut.Find("time").TextContent);
    }

    [Fact]
    public void TbTimestamp_ShouldPreferOwnTimeZone_WhenGivenOne()
    {
        this.AddTrailblazor(options => options.Times.TimeZone = NewYork);

        var cut = Render<CascadingValue<TimeZoneInfo>>(parameters => parameters.Add(cascade => cascade.Value, NewYork)
            .AddChildContent<TbTimestamp>(timestamp => timestamp.Add(stamp => stamp.Value, Expiry)
                .Add(stamp => stamp.TimeZone, Tokyo)));

        Assert.Equal(Shown(Expiry, Tokyo, "g"), cut.Find("time").TextContent);
    }

    [Fact]
    public void TbTimestamp_ShouldShowValueInTheAppsTimeZone_WhenNothingCloserSetsOne()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.Format, TimestampFormat.Date),
            options => options.Times.TimeZone = Tokyo);

        Assert.Equal(Shown(Expiry, Tokyo, "d"), cut.Find("time").TextContent);
    }

    [Fact]
    public void TbTimestamp_ShouldWriteIsoDateTimeAttribute_WhenCalled()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.TimeZone, Tokyo));

        Assert.Equal("2026-11-25T15:00:00.000+00:00", cut.Find("time").GetAttribute("datetime"));
    }

    [Fact]
    public void FormatString_ShouldTakeTheFormatsPlace_WhenGiven()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.TimeZone, Tokyo)
            .Add(stamp => stamp.FormatString, "yyyy-MM-dd HH:mm"));

        Assert.Equal("2026-11-26 00:00", cut.Find("time").TextContent);
    }

    [Fact]
    public void TbTimestamp_ShouldUseFutureText_WhenValueIsAhead()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.TimeZone, Tokyo)
            .Add(stamp => stamp.Format, TimestampFormat.Date)
            .Add(stamp => stamp.FutureText, "Expires {0}").Add(stamp => stamp.PastText, "Expired {0}"));

        Assert.Equal($"Expires {Shown(Expiry, Tokyo, "d")}", cut.Find("time").TextContent);
    }

    [Fact]
    public void TbTimestamp_ShouldSwitchToPastText_WhenTimePasses()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.TimeZone, Tokyo)
            .Add(stamp => stamp.Format, TimestampFormat.Date)
            .Add(stamp => stamp.FutureText, "Expires {0}").Add(stamp => stamp.PastText, "Expired {0}"));

        _time.Advance(TimeSpan.FromDays(6));

        cut.WaitForAssertion(() =>
            Assert.Equal($"Expired {Shown(Expiry, Tokyo, "d")}", cut.Find("time").TextContent));
    }

    [Fact]
    public void TbTimestamp_ShouldShowOffset_WhenGivenShowZone()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.TimeZone, NewYork)
            .Add(stamp => stamp.ShowZone, true));

        Assert.Equal($"{Shown(Expiry, NewYork, "g")} UTC−5", cut.Find("time").TextContent);
    }

    [Theory]
    [InlineData("America/New_York", 11, "EST")]
    [InlineData("America/New_York", 7, "EDT")]
    [InlineData("Eastern Standard Time", 7, "EDT")]
    public void ShowZone_ShouldShowTheAppsAbbreviation_WhenItGivesOneForTheZone(string key, int month, string expected)
    {
        var cut = RenderStamp(Expiry.AddMonths(month - 11), parameters => parameters
                .Add(stamp => stamp.TimeZone, NewYork).Add(stamp => stamp.ShowZone, true),
            options => options.Times.Abbreviations[key] = new("EST", "EDT"));

        Assert.EndsWith($" {expected}", cut.Find("time").TextContent);
    }

    [Fact]
    public void ShowZone_ShouldShowUtcAlone_WhenTheZoneIsUtc()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.TimeZone, TimeZoneInfo.Utc)
            .Add(stamp => stamp.ShowZone, true));

        Assert.EndsWith(" UTC", cut.Find("time").TextContent);
    }

    [Theory]
    [InlineData(10, "just now")]
    [InlineData(-30, "just now")]
    [InlineData(60, "in 1 minute")]
    [InlineData(-25 * 60, "25 minutes ago")]
    [InlineData(-2 * 3600, "2 hours ago")]
    [InlineData(3 * 86400, "in 3 days")]
    [InlineData(-40 * 86400, "1 month ago")]
    [InlineData(400 * 86400, "in 1 year")]
    public void TbTimestamp_ShouldShowRelativeText_WhenGivenRelative(int seconds, string expected)
    {
        var cut = RenderStamp(_time.GetUtcNow().AddSeconds(seconds),
            parameters => parameters.Add(stamp => stamp.Format, TimestampFormat.Relative));

        Assert.Equal(expected, cut.Find("time").TextContent);
    }

    [Fact]
    public void TbTimestamp_ShouldDescribeExactTime_WhenRelative()
    {
        var cut = RenderStamp(Expiry, parameters => parameters.Add(stamp => stamp.Format, TimestampFormat.Relative)
            .Add(stamp => stamp.TimeZone, Tokyo).Add(stamp => stamp.FutureText, "Expires {0}"));

        // The tooltip holds the exact moment, and the time is a tab stop so keyboard users reach it.
        var time = cut.Find("time");
        Assert.Equal(("Expires in 5 days", "0"), (time.TextContent, time.GetAttribute("tabindex")));
        Assert.Equal(Shown(Expiry, Tokyo, "g"), cut.Find("[role=tooltip]").TextContent.Trim());
    }

    [Fact]
    public void Relative_ShouldKeepUp_WhenTimePasses()
    {
        var cut = RenderStamp(_time.GetUtcNow().AddSeconds(50),
            parameters => parameters.Add(stamp => stamp.Format, TimestampFormat.Relative));
        Assert.Equal("in 1 minute", cut.Find("time").TextContent);

        _time.Advance(TimeSpan.FromSeconds(30));

        cut.WaitForAssertion(() => Assert.Equal("just now", cut.Find("time").TextContent));
        _time.Advance(TimeSpan.FromSeconds(75));
        cut.WaitForAssertion(() => Assert.Equal("1 minute ago", cut.Find("time").TextContent));
    }

    [Fact]
    public void Render_ShouldShowADashReadAsNotSet_WhenTheValueIsNull()
    {
        var cut = RenderStamp(null);

        Assert.Empty(cut.FindAll("time"));
        Assert.Equal(("—", "Not set"),
            (cut.Find("[aria-hidden=true]").TextContent, cut.Find(".tb-visually-hidden").TextContent));
    }

    [Fact]
    public void Chat_ShouldShowTimesInTheCascadedTimeZone_WhenOneIsCascaded()
    {
        this.AddTrailblazor();
        var sent = new ChatMessage("1", new ChatAuthor("robin", "Robin"), "Morning.", Expiry);

        var cut = Render<CascadingValue<TimeZoneInfo>>(parameters => parameters.Add(cascade => cascade.Value, Tokyo)
            .AddChildContent<TbChat>(chat => chat.Add(item => item.AriaLabel, "Trip planning")
                .Add(item => item.Messages, [sent]).Add(item => item.CurrentAuthorId, "sam")));

        Assert.Equal(Shown(Expiry, Tokyo, "t"), cut.Find("[data-part=time]").TextContent);
    }

    [Fact]
    public void GridColumn_ShouldShowADateTimeOffsetInTheCascadedTimeZone_WhenOneIsCascaded()
    {
        this.AddTrailblazor();
        Departure[] departures = [new(Expiry)];

        var cut = Render<CascadingValue<TimeZoneInfo>>(parameters => parameters.Add(cascade => cascade.Value, Tokyo)
            .AddChildContent<TbDataGrid<Departure>>(grid => grid.Add(table => table.AriaLabel, "Departures")
                .Add(table => table.Items, departures)
                .AddChildContent<TbColumn<Departure>>(column => column.Add(each => each.Field, trip => trip.Leaves)
                    .Add(each => each.Format, "g"))));

        Assert.Equal(Shown(Expiry, Tokyo, "g"), cut.Find("tbody td").TextContent.Trim());
    }

    [Fact]
    public async Task GridColumn_ShouldFilterAndExportADateTimeOffsetByItsShownDay_WhenAZoneIsCascaded()
    {
        this.AddTrailblazor();
        // 15:00 UTC on the 25th is just past midnight on the 26th in Tokyo, which is the day the cell shows; 01:00 UTC
        // on the 25th is still the 25th there.
        Departure[] departures = [new(Expiry), new(Expiry.AddHours(-14))];

        var cut = Render<CascadingValue<TimeZoneInfo>>(parameters => parameters.Add(cascade => cascade.Value, Tokyo)
            .AddChildContent<TbDataGrid<Departure>>(grid => grid.Add(table => table.AriaLabel, "Departures")
                .Add(table => table.Items, departures)
                .Add(table => table.Filters,
                    [new GridFilter("Leaves", FilterOperator.EqualTo, new DateOnly(2026, 11, 26))])
                .AddChildContent<TbColumn<Departure>>(column => column.Add(each => each.Field, trip => trip.Leaves)
                    .Add(each => each.Filterable, true))));
        var workbook = await cut.InvokeAsync(() =>
            cut.FindComponent<TbDataGrid<Departure>>().Instance.ExportExcelAsync());

        Assert.Equal(TimeZoneInfo.ConvertTime(Expiry, Tokyo).ToString(CultureInfo.CurrentCulture),
            Assert.Single(cut.FindAll("tr[data-part=row] td[data-column=Leaves]")).TextContent.Trim());
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(workbook));
        var sheet = System.Xml.Linq.XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var serial = (new DateTime(2026, 11, 26) - new DateTime(1899, 12, 30)).TotalDays;
        Assert.Contains(sheet.Descendants(), cell => cell.Name.LocalName == "v"
            && cell.Value == serial.ToString("R", CultureInfo.InvariantCulture));
    }

    private sealed record Departure(DateTimeOffset Leaves);

    [Fact]
    public void TimelineItem_ShouldShowItsTimeInTheCascadedTimeZone_WhenOneIsCascaded()
    {
        this.AddTrailblazor();

        var cut = Render<CascadingValue<TimeZoneInfo>>(parameters => parameters.Add(cascade => cascade.Value, Tokyo)
            .AddChildContent<TbTimeline>(timeline => timeline.AddChildContent<TbTimelineItem>(item => item
                .Add(entry => entry.Title, "Trip booked").Add(entry => entry.Time, Expiry))));

        Assert.Equal(Shown(Expiry, Tokyo, "g"), cut.Find("[data-part=time]").TextContent.Trim());
    }
}
