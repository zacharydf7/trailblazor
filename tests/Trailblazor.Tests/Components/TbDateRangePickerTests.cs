using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class TbDateRangePickerTests : BunitContext
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private readonly List<DateRange<DateOnly>> _changes = [];

    public TbDateRangePickerTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero)));
        this.AddTrailblazor();
    }

    private static DateRange<DateOnly> June3To10 => new(new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 10));

    [Fact]
    public void Render_ShouldShowEachDateInTheFormat_WhenGivenARange()
    {
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.Format, "yyyy-MM-dd"));

        Assert.Equal("2026-06-03", cut.Find("[data-part=start-input]").GetAttribute("value"));
        Assert.Equal("2026-06-10", cut.Find("[data-part=end-input]").GetAttribute("value"));
        Assert.Equal("YYYY-MM-DD", cut.Find("[data-part=start-input]").GetAttribute("placeholder"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMakeBothDateBoxesReadOnly_WhenReadOnly(bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.ReadOnly, true));

        Assert.True(cut.Find("[data-part=start-input]").HasAttribute("readonly"));
        Assert.True(cut.Find("[data-part=end-input]").HasAttribute("readonly"));
    }

    [Fact]
    public void Render_ShouldStartTheWeekOnTheCulturesFirstDay_WhenGivenACulture()
    {
        var cut = Render<TbDateRangePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Value, June3To10)
            .Add(picker => picker.Culture, CultureInfo.GetCultureInfo("de-DE")));

        Assert.Equal("Montag", cut.Find("[role=grid] th").GetAttribute("abbr"));
        Assert.Equal("Juni 2026", cut.Find("[data-part=month] div").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldOpenOnTheRangeAndMarkItAndToday_WhenGivenARange()
    {
        var cut = RenderPicker(June3To10);

        Assert.Equal("0", Day(cut, "2026-06-03").GetAttribute("tabindex"));
        Assert.Equal(8, cut.FindAll("td[aria-selected=true]").Count);
        Assert.Equal("date", Day(cut, "2026-06-15").GetAttribute("aria-current"));
    }

    [Fact]
    public void Render_ShouldSayWhichEndOfTheRangeADayIs_WhenGivenARange()
    {
        var cut = RenderPicker(June3To10);

        Assert.Equal("Wednesday, June 3, 2026, Start date", Day(cut, "2026-06-03").GetAttribute("aria-label"));
        Assert.Equal("Friday, June 5, 2026", Day(cut, "2026-06-05").GetAttribute("aria-label"));
        Assert.Equal("Wednesday, June 10, 2026, End date", Day(cut, "2026-06-10").GetAttribute("aria-label"));
    }

    [Fact]
    public void Pick_ShouldAnnounceTheChosenRange_WhenTheEndIsPicked()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPicker(June3To10);

        Day(cut, "2026-06-12").Click();
        Assert.Equal("Friday, June 12, 2026, Start date", Day(cut, "2026-06-12").GetAttribute("aria-label"));
        Day(cut, "2026-06-15").Click();

        Assert.Contains("Friday, June 12, 2026 to Monday, June 15, 2026",
            announcer.Find("[role=status]").TextContent);
    }

    [Fact]
    public void Pick_ShouldRaiseTheRangeOnce_WhenTheEndIsPicked()
    {
        var cut = RenderPicker(June3To10);

        Day(cut, "2026-06-12").Click();
        Assert.Empty(_changes);
        Day(cut, "2026-06-15").Click();

        Assert.Equal([new(new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 15))], _changes);
    }

    [Fact]
    public void Pick_ShouldStartAgain_WhenTheSecondDayIsEarlier()
    {
        var cut = RenderPicker(June3To10);

        Day(cut, "2026-06-12").Click();
        Day(cut, "2026-06-05").Click();
        Day(cut, "2026-06-08").Click();

        Assert.Equal([new(new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 8))], _changes);
    }

    [Fact]
    public void Pick_ShouldIgnoreTheDay_WhenItCantBeChosen()
    {
        var cut = RenderPicker(June3To10, parameters => parameters
            .Add(picker => picker.Min, new DateOnly(2026, 6, 2))
            .Add(picker => picker.IsDateDisabled, date => date.DayOfWeek == DayOfWeek.Sunday));

        Day(cut, "2026-06-01").Click();
        Day(cut, "2026-06-14").Click();

        Assert.Equal("true", Day(cut, "2026-06-14").GetAttribute("aria-disabled"));
        Assert.Equal(8, cut.FindAll("td[aria-selected=true]").Count);
        Assert.Empty(_changes);
    }

    [Fact]
    public void PreviousMonth_ShouldKeepADayInTheTabOrder_WhenPagingPastTheEarliestDate()
    {
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.Min, new DateOnly(2026, 6, 1)));

        for (var click = 0; click < 3; click++)
        {
            cut.Find("[data-part=previous-month]").Click();
        }

        Assert.Equal("2026-03-03", cut.Find("[data-part=day][tabindex='0']").GetAttribute("data-date"));
    }

    [Fact]
    public void Render_ShouldNotRaiseValueChanged_WhenTheParentSetsTheRange()
    {
        var cut = RenderPicker(June3To10);

        cut.Render(parameters => parameters.Add(picker => picker.Value,
            new DateRange<DateOnly>(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5))));

        Assert.Empty(_changes);
        Assert.Equal("9/1/2026", cut.Find("[data-part=start-input]").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldCommitBothDates_WhenTheyReadInOrder()
    {
        var cut = RenderPicker(June3To10);

        cut.Find("[data-part=start-input]").Change("6/1/2026");

        Assert.Equal([new(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 10))], _changes);
    }

    [Fact]
    public void Change_ShouldExplainWithTheFormatAndKeepTheText_WhenADateCantBeRead()
    {
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.Format, "dd.MM.yyyy"));

        cut.Find("[data-part=end-input]").Change("June");

        Assert.Empty(_changes);
        Assert.Equal("Enter the end date as DD.MM.YYYY.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("June", cut.Find("[data-part=end-input]").GetAttribute("value"));
        Assert.Equal("true", cut.Find("[data-part=end-input]").GetAttribute("aria-invalid"));
        Assert.Null(cut.Find("[data-part=start-input]").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Change_ShouldExplainBothBoxes_WhenNeitherCanBeRead()
    {
        var cut = RenderPicker(June3To10);

        cut.Find("[data-part=start-input]").Change("soon");
        cut.Find("[data-part=end-input]").Change("later");

        Assert.Equal("Enter the start date as MM/DD/YYYY. Enter the end date as MM/DD/YYYY.",
            cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldMarkTheEnd_WhenItComesBeforeTheStart()
    {
        var cut = RenderPicker(June3To10);

        cut.Find("[data-part=start-input]").Change("6/20/2026");

        Assert.Empty(_changes);
        Assert.Equal("The end date is before the start date.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-part=end-input]").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Render_ShouldDropTheTypedError_WhenTheParentSetsANewRange()
    {
        var cut = RenderPicker(June3To10);
        cut.Find("[data-part=start-input]").Change("soon");

        cut.Render(parameters => parameters.Add(picker => picker.Value,
            new DateRange<DateOnly>(new DateOnly(2026, 9, 1), null)));

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Equal("9/1/2026", cut.Find("[data-part=start-input]").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldKeepTheTimeOfTheDateLeftAlone_WhenTheOtherChanges()
    {
        var range = new DateRange<DateTime>(new DateTime(2026, 3, 1, 8, 30, 0), new DateTime(2026, 3, 31, 17, 0, 0));
        var changes = new List<DateRange<DateTime>>();
        var cut = Render<TbDateRangePicker<DateTime>>(parameters => parameters.Add(picker => picker.Value, range)
            .Add(picker => picker.Culture, English)
            .Add(picker => picker.ValueChanged, (DateRange<DateTime> value) => changes.Add(value)));

        cut.Find("[data-part=end-input]").Change("3/30/2026");

        Assert.Equal([new(new DateTime(2026, 3, 1, 8, 30, 0), new DateTime(2026, 3, 30))], changes);
    }

    [Fact]
    public void Preset_ShouldRaiseItsRange_WhenChosen()
    {
        var july = new DateRange<DateOnly>(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 7));
        var cut = RenderPicker(June3To10, parameters => parameters
            .Add(picker => picker.Presets, [new DateRangePreset<DateOnly>("First week of July", july)]));

        cut.Find("[data-part=preset]").Click();

        Assert.Equal([july], _changes);
    }

    [Fact]
    public void Render_ShouldDescribeTheClearButtonByTheFieldsLabel_WhenClearable()
    {
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.Clearable, true));

        var clear = cut.Find("[data-part=clear]");
        Assert.Equal("Trip dates", cut.Find($"[id='{clear.GetAttribute("aria-describedby")}']").TextContent.Trim());
    }

    [Fact]
    public void Clear_ShouldEmptyTheRange_WhenClicked()
    {
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.Clearable, true));

        cut.Find("[data-part=clear]").Click();

        Assert.Equal([default], _changes);
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Change_ShouldMarkBothBoxes_WhenTheFieldsValidationFails()
    {
        var cut = RenderPicker(June3To10, parameters => parameters.Add(picker => picker.Validation,
            range => range.End is null ? ["Choose an end date."] : []));

        cut.Find("[data-part=end-input]").Change("");

        Assert.Equal("Choose an end date.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal(2, cut.FindAll("[aria-invalid=true]").Count);
    }

    [Fact]
    public void Render_ShouldUseTheBrowsersDateBoxesPostingIsoDates_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderPicker(June3To10, parameters => parameters
            .Add(picker => picker.InputAttributes, new Dictionary<string, object> { ["name"] = "Trip" })
            .Add(picker => picker.Min, new DateOnly(2026, 1, 1)));

        var start = cut.Find("[data-part=start-input]");
        Assert.Equal(["date", "2026-06-03", "Trip.Start", "2026-01-01", "", null],
            new[] { "type", "value", "name", "min", "data-static", "data-prerender" }.Select(start.GetAttribute));
        Assert.Equal("Trip.End", cut.Find("[data-part=end-input]").GetAttribute("name"));
        Assert.Empty(cut.FindAll("[data-part=panel]"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTypeIsntADate()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbDateRangePicker<DateTimeOffset>>());

        Assert.Contains("DateTimeOffset", failure.Message);
    }

    private static AngleSharp.Dom.IElement Day(IRenderedComponent<TbDateRangePicker<DateOnly>> cut, string date) =>
        cut.Find($"[data-part=day][data-date='{date}']");

    private IRenderedComponent<TbDateRangePicker<DateOnly>> RenderPicker(DateRange<DateOnly> value,
        Action<ComponentParameterCollectionBuilder<TbDateRangePicker<DateOnly>>>? configure = null) =>
        Render<TbDateRangePicker<DateOnly>>(parameters =>
        {
            parameters.Add(picker => picker.Value, value).Add(picker => picker.Label, "Trip dates")
                .Add(picker => picker.Culture, English)
                .Add(picker => picker.ValueChanged, (DateRange<DateOnly> range) => _changes.Add(range));
            configure?.Invoke(parameters);
        });
}
