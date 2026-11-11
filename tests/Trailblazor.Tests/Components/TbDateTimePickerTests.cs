using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class TbDateTimePickerTests : BunitContext
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly DateTime June3At2 = new(2026, 6, 3, 14, 30, 0);
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero));
    private readonly List<DateTime?> _changes = [];

    public TbDateTimePickerTests()
    {
        CultureInfo.CurrentCulture = English;
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldShowTheDateAndTimeInTheirBoxes_WhenGivenAValue()
    {
        var cut = RenderPicker(June3At2);

        Assert.Equal("6/3/2026", DateBox(cut).GetAttribute("value"));
        Assert.Equal("14:30", TimeBox(cut).GetAttribute("value"));
        Assert.Equal("MM/DD/YYYY", DateBox(cut).GetAttribute("placeholder"));
        Assert.Equal("2026-06-03T14:30", cut.Find("[role=group]").GetAttribute("data-value"));
    }

    [Fact]
    public void Render_ShouldNameEachBoxAfterTheLabel_WhenInteractive()
    {
        var cut = RenderPicker(June3At2);

        var label = cut.Find("label");
        Assert.Null(label.GetAttribute("for"));
        Assert.Equal(label.Id, cut.Find("[role=group]").GetAttribute("aria-labelledby"));
        Assert.Equal(["Date", "Time"], new[] { DateBox(cut), TimeBox(cut) }.Select(box =>
            cut.Find($"#{box.GetAttribute("aria-labelledby")!.Split(' ')[1]}").TextContent));
        Assert.StartsWith(label.Id + " ", TimeBox(cut).GetAttribute("aria-labelledby"));
    }

    [Fact]
    public async Task Change_ShouldReadAndShowAGregorianYear_WhenTheCulturesCalendarIsAnother()
    {
        var cut = RenderPicker(June3At2, culture: CultureInfo.GetCultureInfo("th-TH"));

        Assert.Equal("3/6/2026", DateBox(cut).GetAttribute("value"));
        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "12/6/2026" });

        Assert.Equal([new DateTime(2026, 6, 12, 14, 30, 0)], _changes);
    }

    [Fact]
    public async Task Change_ShouldKeepTheTime_WhenADateIsTyped()
    {
        var cut = RenderPicker(June3At2);

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/12/2026" });

        Assert.Equal([new DateTime(2026, 6, 12, 14, 30, 0)], _changes);
    }

    [Fact]
    public async Task Change_ShouldKeepTheDate_WhenATimeIsGiven()
    {
        var cut = RenderPicker(June3At2);

        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "09:15" });

        Assert.Equal([new DateTime(2026, 6, 3, 9, 15, 0)], _changes);
    }

    [Fact]
    public async Task Change_ShouldBeMidnight_WhenADateIsTypedWithNoTime()
    {
        var cut = RenderPicker(null);

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/12/2026" });

        Assert.Equal([new DateTime(2026, 6, 12)], _changes);
        Assert.Equal("00:00", TimeBox(cut).GetAttribute("value"));
    }

    [Fact]
    public async Task Change_ShouldWaitForADate_WhenATimeIsGivenFirst()
    {
        var cut = RenderPicker(null);

        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "09:15" });
        Assert.Empty(_changes);
        Assert.Equal("09:15", TimeBox(cut).GetAttribute("value"));
        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/12/2026" });

        Assert.Equal([new DateTime(2026, 6, 12, 9, 15, 0)], _changes);
    }

    [Fact]
    public async Task Change_ShouldClearTheValueAndKeepTheTime_WhenTheDateIsCleared()
    {
        var cut = RenderPicker(June3At2);

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "" });

        Assert.Equal([(DateTime?)null], _changes);
        Assert.Equal("14:30", TimeBox(cut).GetAttribute("value"));
    }

    [Fact]
    public async Task Change_ShouldBeMidnight_WhenTheTimeIsCleared()
    {
        var cut = RenderPicker(June3At2);

        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "" });

        Assert.Equal([new DateTime(2026, 6, 3)], _changes);
    }

    [Fact]
    public async Task Change_ShouldShowAMessageAndKeepTheValue_WhenTheDateCantBeRead()
    {
        var cut = RenderPicker(June3At2);

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "June 12" });

        Assert.Empty(_changes);
        Assert.Equal("Enter the date as MM/DD/YYYY.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("June 12", DateBox(cut).GetAttribute("value"));
        Assert.Equal(("true", null), (DateBox(cut).GetAttribute("aria-invalid"),
            TimeBox(cut).GetAttribute("aria-invalid")));
    }

    [Fact]
    public async Task Change_ShouldTakeBackTheMessage_WhenTheDateIsFixed()
    {
        var cut = RenderPicker(June3At2);
        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "June 3" });

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/3/2026" });

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Empty(_changes);
    }

    [Fact]
    public async Task Change_ShouldRefuseTheDate_WhenIsDateDisabledRulesItOut()
    {
        var cut = RenderPicker(June3At2, parameters => parameters.Add(picker => picker.IsDateDisabled,
            date => date.DayOfWeek == DayOfWeek.Sunday));

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/7/2026" });

        Assert.Empty(_changes);
        Assert.Equal("6/7/2026 can't be chosen.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Theory]
    [InlineData(true, true, "6/3/2026", "Choose a date and time from 6/3/2026 3:00 PM to 6/5/2026 9:00 AM.")]
    [InlineData(true, false, "6/3/2026", "Choose 6/3/2026 3:00 PM or later.")]
    [InlineData(false, true, "6/6/2026", "Choose 6/5/2026 9:00 AM or earlier.")]
    public async Task Change_ShouldShowAMessage_WhenTheValueIsOutsideTheLimits(bool min, bool max, string date,
        string message)
    {
        var cut = RenderPicker(null, parameters =>
        {
            if (min)
            {
                parameters.Add(picker => picker.Min, new DateTime(2026, 6, 3, 15, 0, 0));
            }

            if (max)
            {
                parameters.Add(picker => picker.Max, new DateTime(2026, 6, 5, 9, 0, 0));
            }
        });

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = date });

        Assert.Equal(message, ErrorText(cut));
    }

    [Fact]
    public async Task Change_ShouldShowNoMessage_WhenTheValueIsOnTheLimit()
    {
        var cut = RenderPicker(null, parameters => parameters.Add(picker => picker.Min, June3At2));

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/3/2026" });
        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "14:30" });

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Render_ShouldLimitTheCalendarToTheDaysOfMinAndMax_WhenGivenThem()
    {
        var cut = RenderPicker(June3At2, parameters => parameters.Add(picker => picker.Min, June3At2)
            .Add(picker => picker.Max, new DateTime(2026, 6, 5, 9, 0, 0)));

        Assert.Null(cut.Find("[role=grid] [data-date='2026-06-03']").GetAttribute("aria-disabled"));
        Assert.Null(cut.Find("[role=grid] [data-date='2026-06-05']").GetAttribute("aria-disabled"));
        Assert.Equal("true", cut.Find("[role=grid] [data-date='2026-06-06']").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task Pick_ShouldSetTheDateAndKeepTheTime_WhenADayIsClicked()
    {
        var cut = RenderPicker(June3At2);

        await cut.Find("[role=grid] [data-date='2026-06-09']").ClickAsync(new MouseEventArgs());

        Assert.Equal([new DateTime(2026, 6, 9, 14, 30, 0)], _changes);
        Assert.Equal("6/9/2026", DateBox(cut).GetAttribute("value"));
    }

    [Theory]
    [InlineData(nameof(TbDateTimePicker<DateTime>.Disabled))]
    [InlineData(nameof(TbDateTimePicker<DateTime>.ReadOnly))]
    public void Render_ShouldTurnOffTheCalendarButton_WhenTheFieldIsTurnedOff(string state)
    {
        var cut = RenderPicker(June3At2, parameters => parameters.Add(
            state == "Disabled" ? picker => picker.Disabled : picker => picker.ReadOnly, true));

        Assert.True(cut.Find("[data-part=popover] [aria-haspopup]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Pick_ShouldTakeTheDayAsPicked_WhenTheFormatHasATwoDigitYear()
    {
        var cut = RenderPicker(new DateTime(2060, 6, 3, 14, 30, 0),
            parameters => parameters.Add(picker => picker.Format, "M/d/yy"));

        await cut.Find("[role=grid] [data-date='2060-06-09']").ClickAsync(new MouseEventArgs());

        Assert.Equal([new DateTime(2060, 6, 9, 14, 30, 0)], _changes);
    }

    [Fact]
    public async Task Render_ShouldDropTypedText_WhenTheParentSetsANewValue()
    {
        var cut = RenderPicker(null);
        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "09:15" });

        cut.Render(parameters => parameters.Add(picker => picker.Value, June3At2));

        Assert.Equal("14:30", TimeBox(cut).GetAttribute("value"));
    }

    [Fact]
    public async Task Reset_ShouldDropTypedText_WhenTheFormResets()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbDateTimePicker<DateTime>>(0);
            builder.AddAttribute(1, nameof(TbDateTimePicker<DateTime>.Label), "Starts");
            builder.AddAttribute(2, nameof(TbDateTimePicker<DateTime>.Culture), English);
            builder.AddAttribute(3, nameof(TbDateTimePicker<DateTime>.Value), (DateTime?)June3At2);
            builder.CloseComponent();
        })));
        await cut.Find("input[data-part=date-input]").ChangeAsync(new ChangeEventArgs { Value = "June 9" });

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal("6/3/2026", cut.Find("input[data-part=date-input]").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task Clear_ShouldClearBothBoxesAndFocusTheDateBox_WhenPressed()
    {
        var cut = RenderPicker(June3At2, parameters => parameters.Add(picker => picker.Clearable, true));

        await cut.Find("[data-part=clear]").ClickAsync(new MouseEventArgs());

        Assert.Equal([(DateTime?)null], _changes);
        Assert.Equal((null, null), (DateBox(cut).GetAttribute("value"), TimeBox(cut).GetAttribute("value")));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task Change_ShouldKeepTheKind_WhenTheDateTimeIsUtc()
    {
        var cut = RenderPicker(DateTime.SpecifyKind(June3At2, DateTimeKind.Utc));

        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "09:15" });

        Assert.Equal(DateTimeKind.Utc, Assert.Single(_changes)!.Value.Kind);
    }

    [Fact]
    public void Render_ShouldShowSeconds_WhenTheStepIsUnderAMinute()
    {
        var cut = RenderPicker(new DateTime(2026, 6, 3, 14, 30, 15),
            parameters => parameters.Add(picker => picker.Step, TimeSpan.FromSeconds(5)));

        Assert.Equal(("14:30:15", "5"), (TimeBox(cut).GetAttribute("value"), TimeBox(cut).GetAttribute("step")));
    }

    [Fact]
    public async Task Change_ShouldKeepTheOffset_WhenBoundToADateTimeOffset()
    {
        var changes = new List<DateTimeOffset?>();
        var cut = RenderOffsetPicker(new DateTimeOffset(June3At2, TimeSpan.FromHours(5)), changes);

        await TimeBox(cut).ChangeAsync(new ChangeEventArgs { Value = "09:15" });

        Assert.Equal([new DateTimeOffset(2026, 6, 3, 9, 15, 0, TimeSpan.FromHours(5))], changes);
        Assert.Equal("6/3/2026", DateBox(cut).GetAttribute("value"));
    }

    [Fact]
    public async Task Change_ShouldTakeTheOffsetOfTheAppsTimeZone_WhenTheFirstValueIsChosen()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("Trail", TimeSpan.FromHours(-7), "Trail", "Trail"));
        var changes = new List<DateTimeOffset?>();
        var cut = RenderOffsetPicker(null, changes);

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "6/12/2026" });

        Assert.Equal([new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.FromHours(-7))], changes);
    }

    [Fact]
    public async Task Change_ShouldTakeTheZonesOffsetForThatDate_WhenGivenATimeZone()
    {
        var summer = TimeZoneInfo.CreateCustomTimeZone("Trail", TimeSpan.FromHours(-8), "Trail", "Trail", "Trail",
        [
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(DateTime.MinValue.Date, DateTime.MaxValue.Date,
                TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1)),
        ]);
        var changes = new List<DateTimeOffset?>();
        var cut = RenderOffsetPicker(new DateTimeOffset(June3At2, TimeSpan.FromHours(-7)), changes,
            parameters => parameters.Add(picker => picker.TimeZone, summer));

        await DateBox(cut).ChangeAsync(new ChangeEventArgs { Value = "12/3/2026" });

        Assert.Equal([new DateTimeOffset(2026, 12, 3, 14, 30, 0, TimeSpan.FromHours(-8))], changes);
    }

    [Fact]
    public void Render_ShouldShowTheBrowsersDateAndTimeBox_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderPicker(June3At2, parameters => parameters.Add(picker => picker.Min, June3At2));

        var input = cut.Find("input[type=datetime-local]");
        Assert.Equal(("2026-06-03T14:30", "2026-06-03T14:30"), (input.GetAttribute("value"),
            input.GetAttribute("min")));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Empty(cut.FindAll("[role=grid]"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTypeIsntADateTime()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbDateTimePicker<DateOnly>>());

        Assert.Contains("DateOnly", failure.Message);
    }

    // Newer ICU versions write a narrow no-break space before AM and PM.
    private static string ErrorText(IRenderedComponent<IComponent> cut) =>
        cut.Find("[data-part=error]").TextContent.Trim().Replace('\u202F', ' ');

    private static AngleSharp.Dom.IElement DateBox(IRenderedComponent<IComponent> cut) =>
        cut.Find("input[data-part=date-input]");

    private static AngleSharp.Dom.IElement TimeBox(IRenderedComponent<IComponent> cut) =>
        cut.Find("input[data-part=time-input]");

    private IRenderedComponent<TbDateTimePicker<DateTime>> RenderPicker(DateTime? value,
        Action<ComponentParameterCollectionBuilder<TbDateTimePicker<DateTime>>>? configure = null,
        CultureInfo? culture = null) =>
        Render<TbDateTimePicker<DateTime>>(parameters =>
        {
            parameters.Add(picker => picker.Value, value).Add(picker => picker.Label, "Starts")
                .Add(picker => picker.Culture, culture ?? English)
                .Add(picker => picker.ValueChanged, (DateTime? time) => _changes.Add(time));
            configure?.Invoke(parameters);
        });

    private IRenderedComponent<TbDateTimePicker<DateTimeOffset>> RenderOffsetPicker(DateTimeOffset? value,
        List<DateTimeOffset?> changes,
        Action<ComponentParameterCollectionBuilder<TbDateTimePicker<DateTimeOffset>>>? configure = null) =>
        Render<TbDateTimePicker<DateTimeOffset>>(parameters =>
        {
            parameters.Add(picker => picker.Value, value).Add(picker => picker.Label, "Starts")
                .Add(picker => picker.Culture, English)
                .Add(picker => picker.ValueChanged, (DateTimeOffset? time) => changes.Add(time));
            configure?.Invoke(parameters);
        });
}
