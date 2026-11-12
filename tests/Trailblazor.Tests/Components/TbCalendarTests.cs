using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbCalendarTests : BunitContext
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly DateOnly June3 = new(2026, 6, 3);
    private readonly List<DateOnly?> _changes = [];
    private readonly List<DateOnly?> _months = [];
    private readonly BunitJSModuleInterop _module;

    public TbCalendarTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero)));
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/calendar.js");
        _module.SetupModule("createCalendar", _ => true).Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_ShouldShowTheValuesMonthAndMarkItsDayAndToday_WhenGivenAValue()
    {
        var cut = RenderCalendar(June3);

        Assert.Equal("June 2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal("0", Cell(cut, "2026-06-03").GetAttribute("tabindex"));
        Assert.Single(cut.FindAll("[data-date][tabindex='0']"));
        Assert.Equal("true", Cell(cut, "2026-06-03").ParentElement!.GetAttribute("aria-selected"));
        Assert.Single(cut.FindAll("td[aria-selected=true]"));
        Assert.Equal("date", Cell(cut, "2026-06-15").GetAttribute("aria-current"));
        Assert.Equal("Wednesday, June 3, 2026", Cell(cut, "2026-06-03").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldNameTheGridByTheLabelAndTitle_WhenLabeled()
    {
        var cut = RenderCalendar(June3);

        var grid = cut.Find("[role=grid]");
        var ids = grid.GetAttribute("aria-labelledby")!.Split(' ');
        Assert.Equal(["Hike date", "June 2026"], ids.Select(id => cut.Find($"[id='{id}']").TextContent.Trim()));
        var group = cut.Find("[role=group]");
        Assert.Equal(ids[0], group.GetAttribute("aria-labelledby"));
        Assert.Equal("June 2026, choose a month", cut.Find("[data-part=title]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldOpenOnToday_WhenGivenNoValue()
    {
        var cut = RenderCalendar(null);

        Assert.Equal("0", Cell(cut, "2026-06-15").GetAttribute("tabindex"));
        Assert.Empty(cut.FindAll("td[aria-selected=true]"));
    }

    [Fact]
    public void Render_ShouldOpenOnTheNearestAllowedDay_WhenTodayIsBeforeMin()
    {
        var cut = RenderCalendar(null, parameters => parameters.Add(calendar => calendar.Min, new DateOnly(2026, 8, 20)));

        Assert.Equal("0", Cell(cut, "2026-08-20").GetAttribute("tabindex"));
    }

    [Fact]
    public void Render_ShouldStartTheWeekOnTheCulturesFirstDay_WhenGivenACulture()
    {
        var cut = Render<TbCalendar<DateOnly>>(parameters => parameters.Add(calendar => calendar.Value, June3)
            .Add(calendar => calendar.Culture, CultureInfo.GetCultureInfo("de-DE")));

        Assert.Equal("Montag", cut.Find("[role=grid] th").GetAttribute("abbr"));
        Assert.Equal("Juni 2026", cut.Find("[data-part=title]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldNameTheGridsGregorianYear_WhenTheCulturesCalendarIsAnother()
    {
        var cut = Render<TbCalendar<DateOnly>>(parameters => parameters.Add(calendar => calendar.Value, June3)
            .Add(calendar => calendar.Culture, CultureInfo.GetCultureInfo("th-TH")));

        Assert.EndsWith("2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.EndsWith("2026", Cell(cut, "2026-06-03").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Click_ShouldChooseTheDay_WhenADayIsClicked()
    {
        var cut = RenderCalendar(June3);

        await Cell(cut, "2026-06-09").ClickAsync(new MouseEventArgs());

        Assert.Equal([new DateOnly(2026, 6, 9)], _changes);
        Assert.Equal("true", Cell(cut, "2026-06-09").ParentElement!.GetAttribute("aria-selected"));
        Assert.Equal("0", Cell(cut, "2026-06-09").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task OnDayPicked_ShouldBeRaised_WhenTheChosenDayIsPickedAgain()
    {
        var picked = new List<DateOnly>();
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.OnDayPicked,
            (DateOnly date) => picked.Add(date)));

        await Cell(cut, "2026-06-03").ClickAsync(new MouseEventArgs());
        await Cell(cut, "2026-06-25").ClickAsync(new MouseEventArgs());

        Assert.Equal([June3, new DateOnly(2026, 6, 25)], picked);
        Assert.Equal([new DateOnly(2026, 6, 25)], _changes);
    }

    [Fact]
    public async Task Click_ShouldLeaveTheDateAlone_WhenTheDayCantBeChosen()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.Max, new DateOnly(2026, 6, 20))
            .Add(calendar => calendar.IsDateDisabled, date => date.DayOfWeek == DayOfWeek.Sunday));

        await Cell(cut, "2026-06-14").ClickAsync(new MouseEventArgs());
        await Cell(cut, "2026-06-25").ClickAsync(new MouseEventArgs());

        Assert.Empty(_changes);
        Assert.Equal("true", Cell(cut, "2026-06-14").GetAttribute("aria-disabled"));
        Assert.Equal("true", Cell(cut, "2026-06-25").GetAttribute("aria-disabled"));
        Assert.Null(Cell(cut, "2026-06-13").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task Click_ShouldMoveFocusButKeepTheDate_WhenReadOnly()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.ReadOnly, true));

        await Cell(cut, "2026-06-09").ClickAsync(new MouseEventArgs());

        Assert.Empty(_changes);
        Assert.Equal("0", Cell(cut, "2026-06-09").GetAttribute("tabindex"));
        Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-readonly"));
    }

    [Fact]
    public void Render_ShouldTurnOffEveryButton_WhenDisabled()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.Disabled, true));

        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled"), button.OuterHtml));
    }

    [Fact]
    public async Task Click_ShouldKeepTheTimeOfDayOnlyOnTheSameDay_WhenTheValueIsADateTime()
    {
        var changes = new List<DateTime?>();
        var cut = Render<TbCalendar<DateTime>>(parameters => parameters
            .Add(calendar => calendar.Value, new DateTime(2026, 6, 3, 14, 30, 0))
            .Add(calendar => calendar.Culture, English)
            .Add(calendar => calendar.ValueChanged, (DateTime? date) => changes.Add(date)));

        await cut.Find("[data-date='2026-06-03']").ClickAsync(new MouseEventArgs());
        Assert.Equal(new DateTime(2026, 6, 3, 14, 30, 0), cut.Instance.Value);
        await cut.Find("[data-date='2026-06-05']").ClickAsync(new MouseEventArgs());

        Assert.Equal([new DateTime(2026, 6, 5)], changes);
    }

    [Fact]
    public async Task Title_ShouldShowTheYearsMonthsAndThenTheDecadesYears_WhenClicked()
    {
        var cut = RenderCalendar(June3);

        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());

        Assert.Equal("months", cut.Find("[role=grid]").GetAttribute("data-view"));
        Assert.Equal("2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal(12, cut.FindAll("[data-part=month]").Count);
        Assert.Equal("0", Cell(cut, "2026-06-01").GetAttribute("tabindex"));
        Assert.Equal("June 2026", Cell(cut, "2026-06-01").GetAttribute("aria-label"));
        Assert.Equal("true", Cell(cut, "2026-06-01").ParentElement!.GetAttribute("aria-selected"));

        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());

        Assert.Equal("years", cut.Find("[role=grid]").GetAttribute("data-view"));
        Assert.Equal("2020–2029", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal(10, cut.FindAll("[data-part=year]").Count);
        Assert.Equal("0", Cell(cut, "2026-01-01").GetAttribute("tabindex"));
        Assert.Equal("true", cut.Find("[data-part=title]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task Click_ShouldOpenTheYearAndThenTheMonthOnTheSameDay_WhenAYearAndAMonthArePicked()
    {
        var cut = RenderCalendar(June3);
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());

        await Cell(cut, "2028-01-01").ClickAsync(new MouseEventArgs());

        Assert.Equal("months", cut.Find("[role=grid]").GetAttribute("data-view"));
        Assert.Equal("0", Cell(cut, "2028-06-01").GetAttribute("tabindex"));

        await Cell(cut, "2028-02-01").ClickAsync(new MouseEventArgs());

        Assert.Equal("days", cut.Find("[role=grid]").GetAttribute("data-view"));
        Assert.Equal("February 2028", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal("0", Cell(cut, "2028-02-03").GetAttribute("tabindex"));
        Assert.Empty(_changes);
    }

    [Fact]
    public async Task Click_ShouldRefuseAMonthWithNoDayThatCanBeChosen_WhenMinAndMaxAreSet()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.Min, new DateOnly(2026, 5, 20))
            .Add(calendar => calendar.Max, new DateOnly(2026, 8, 10)));
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());

        await Cell(cut, "2026-04-01").ClickAsync(new MouseEventArgs());

        Assert.Equal("true", Cell(cut, "2026-04-01").GetAttribute("aria-disabled"));
        Assert.Null(Cell(cut, "2026-05-01").GetAttribute("aria-disabled"));
        Assert.Null(Cell(cut, "2026-08-01").GetAttribute("aria-disabled"));
        Assert.Equal("true", Cell(cut, "2026-09-01").GetAttribute("aria-disabled"));
        Assert.Equal("months", cut.Find("[role=grid]").GetAttribute("data-view"));
    }

    [Theory]
    [InlineData(0, "previous", "May 2026")]
    [InlineData(0, "next", "July 2026")]
    [InlineData(1, "previous", "2025")]
    [InlineData(2, "next", "2030–2039")]
    public async Task Paging_ShouldMoveByAMonthYearOrDecade_WhenTheButtonsAreClicked(int zoomOuts, string button,
        string title)
    {
        var cut = RenderCalendar(June3);
        for (var i = 0; i < zoomOuts; i++)
        {
            await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());
        }

        await cut.Find($"[data-part={button}]").ClickAsync(new MouseEventArgs());

        Assert.Equal(title, cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Single(cut.FindAll("[data-date][tabindex='0']"));
    }

    [Fact]
    public async Task Paging_ShouldNameTheButtonsForTheView_WhenTheViewChanges()
    {
        var cut = RenderCalendar(June3);
        var names = new List<string?>();
        for (var i = 0; i < 3; i++)
        {
            names.Add(cut.Find("[data-part=previous]").GetAttribute("aria-label"));
            names.Add(cut.Find("[data-part=next]").GetAttribute("aria-label"));
            await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());
        }

        Assert.Equal(["Previous month", "Next month", "Previous year", "Next year", "Previous decade", "Next decade"],
            names);
    }

    [Fact]
    public async Task MonthChanged_ShouldReportEachMonthShown_WhenTheUserMovesThroughTheViews()
    {
        var cut = RenderCalendar(June3);

        await cut.Find("[data-part=next]").ClickAsync(new MouseEventArgs());
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());
        await cut.Find("[data-part=next]").ClickAsync(new MouseEventArgs());
        await Cell(cut, "2027-03-01").ClickAsync(new MouseEventArgs());

        Assert.Equal([new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1), new DateOnly(2027, 3, 1)], _months);
    }

    [Fact]
    public void Month_ShouldShowTheGivenMonth_WhenTheParentSetsIt()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.Month, new DateOnly(2026, 9, 20)));

        Assert.Equal("September 2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal("0", Cell(cut, "2026-09-03").GetAttribute("tabindex"));

        cut.Render(parameters => parameters.Add(calendar => calendar.Month, new DateOnly(2027, 1, 1)));

        Assert.Equal("January 2027", cut.Find("[data-part=title]").TextContent.Trim());
    }

    [Fact]
    public void Value_ShouldBringItsMonthIntoView_WhenTheParentChangesIt()
    {
        var cut = RenderCalendar(June3);

        cut.Render(parameters => parameters.Add(calendar => calendar.Value, new DateOnly(2026, 11, 20)));

        Assert.Equal("November 2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal("0", Cell(cut, "2026-11-20").GetAttribute("tabindex"));
        Assert.Equal(new DateOnly(2026, 11, 1), _months[^1]);
    }

    [Fact]
    public async Task ShowDate_ShouldShowTheMonthTheKeysMovedTo_WhenTheBrowserAsks()
    {
        var cut = RenderCalendar(June3);

        await cut.InvokeAsync(() => Events().Value.OnShowDate(new DateOnly(2026, 7, 2)));

        Assert.Equal("July 2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal("0", Cell(cut, "2026-07-02").GetAttribute("tabindex"));
        Assert.Equal(new DateOnly(2026, 7, 1), _months[^1]);
    }

    [Fact]
    public async Task ShowDate_ShouldKeepTheMonthAndDay_WhenTheKeysMoveAmongTheYears()
    {
        var cut = RenderCalendar(June3);
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());

        await cut.InvokeAsync(() => Events().Value.OnShowDate(new DateOnly(2031, 1, 1)));
        await cut.InvokeAsync(() => Events().Value.OnPick(new DateOnly(2031, 1, 1)));
        await cut.InvokeAsync(() => Events().Value.OnPick(new DateOnly(2031, 6, 1)));

        Assert.Equal("June 2031", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal("0", Cell(cut, "2031-06-03").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task Pick_ShouldChooseTheDayTheKeysMovedTo_WhenTheBrowserAsks()
    {
        var cut = RenderCalendar(June3);

        await cut.InvokeAsync(() => Events().Value.OnPick(new DateOnly(2026, 6, 12)));

        Assert.Equal([new DateOnly(2026, 6, 12)], _changes);
        Assert.Equal("true", Cell(cut, "2026-06-12").ParentElement!.GetAttribute("aria-selected"));
    }

    [Fact]
    public void DayTemplate_ShouldShowUnderTheDayAndDescribeIt_WhenGiven()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.DayTemplate,
            date => builder => builder.AddContent(0, date.Day == 9 ? "2 hikes" : null)));

        var day = Cell(cut, "2026-06-09");
        var content = cut.Find($"[id='{day.GetAttribute("aria-describedby")}']");
        Assert.Equal("2 hikes", content.TextContent.Trim());
        Assert.Equal("day-content", content.GetAttribute("data-part"));
        Assert.Equal("Tuesday, June 9, 2026", day.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldShowTheBrowsersDateBox_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.Min, new DateOnly(2026, 1, 1)));

        var input = cut.Find("input[type=date]");
        Assert.Equal("2026-06-03", input.GetAttribute("value"));
        Assert.Equal("2026-01-01", input.GetAttribute("min"));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Empty(cut.FindAll("[role=grid]"));
    }

    [Fact]
    public async Task Required_ShouldMarkTheGridRequiredAndValid_WhenADateIsChosen()
    {
        var cut = RenderCalendar(null, parameters => parameters.Add(calendar => calendar.Required, true));

        await Cell(cut, "2026-06-09").ClickAsync(new MouseEventArgs());

        Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-required"));
        Assert.Null(cut.Find("[role=grid]").GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task FocusAsync_ShouldFocusTheGridsTabStopWithoutTheScriptsHandle_WhenCalled()
    {
        _module.SetupVoid("focusGrid", _ => true).SetVoidResult();
        var cut = RenderCalendar(June3);

        await cut.InvokeAsync(() => cut.Instance.FocusAsync());

        Assert.Single(_module.Invocations["focusGrid"]);
    }

    [Fact]
    public async Task Month_ShouldStayWhereTheUserTookIt_WhenTheParentRendersAgainWithTheMonthItGave()
    {
        var cut = Render<TbCalendar<DateOnly>>(parameters => parameters.Add(calendar => calendar.Culture, English)
            .Add(calendar => calendar.Month, new DateOnly(2026, 9, 1))
            .Add(calendar => calendar.MonthChanged, (DateOnly? month) => _months.Add(month)));

        await cut.Find("[data-part=next]").ClickAsync(new MouseEventArgs());
        cut.Render(parameters => parameters.Add(calendar => calendar.Month, new DateOnly(2026, 9, 1)));

        Assert.Equal("October 2026", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Equal(new DateOnly(2026, 10, 1), _months[^1]);
    }

    [Fact]
    public async Task Paging_ShouldStopAtTheLastYearADateCanHave_WhenTheValueIsInDecember9999()
    {
        var cut = RenderCalendar(new DateOnly(9999, 12, 15));

        Assert.Equal("true", cut.Find("[data-part=next]").GetAttribute("aria-disabled"));
        await cut.Find("[data-part=next]").ClickAsync(new MouseEventArgs());
        await cut.Find("[data-part=title]").ClickAsync(new MouseEventArgs());

        Assert.Equal("9999", cut.Find("[data-part=title]").TextContent.Trim());
        Assert.Null(Cell(cut, "9999-12-01").GetAttribute("aria-disabled"));
        Assert.Equal("true", cut.Find("[data-part=next]").GetAttribute("aria-disabled"));
        Assert.Null(cut.Find("[data-part=previous]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task ChooseAsync_ShouldPickADateInAnotherDecade_WhenUsedFromTheTestingDriver()
    {
        var cut = RenderCalendar(June3);

        await cut.Tb().Calendar("Hike date").ChooseAsync(new DateOnly(2041, 3, 9));

        Assert.Equal([new DateOnly(2041, 3, 9)], _changes);
        Assert.Equal("March 2041", cut.Find("[data-part=title]").TextContent.Trim());
    }

    [Fact]
    public async Task ChooseAsync_ShouldThrow_WhenTheDateCantBeChosen()
    {
        var cut = RenderCalendar(June3, parameters => parameters.Add(calendar => calendar.Max, new DateOnly(2026, 12, 31)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().Calendar("Hike date").ChooseAsync(new DateOnly(2027, 1, 5)));
        Assert.Empty(_changes);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTypeIsntADate()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbCalendar<DateTimeOffset>>());

        Assert.Contains("DateTimeOffset", failure.Message);
    }

    private DotNetObjectReference<CalendarEvents> Events() =>
        (DotNetObjectReference<CalendarEvents>)_module.Invocations["createCalendar"].Single().Arguments[1]!;

    private static IElement Cell(IRenderedComponent<TbCalendar<DateOnly>> cut, string date) =>
        cut.Find($"[role=grid] [data-date='{date}']");

    private IRenderedComponent<TbCalendar<DateOnly>> RenderCalendar(DateOnly? value,
        Action<ComponentParameterCollectionBuilder<TbCalendar<DateOnly>>>? configure = null) =>
        Render<TbCalendar<DateOnly>>(parameters =>
        {
            parameters.Add(calendar => calendar.Value, value).Add(calendar => calendar.Label, "Hike date")
                .Add(calendar => calendar.Culture, English)
                .Add(calendar => calendar.ValueChanged, (DateOnly? date) => _changes.Add(date))
                .Add(calendar => calendar.MonthChanged, (DateOnly? month) => _months.Add(month));
            configure?.Invoke(parameters);
        });
}
