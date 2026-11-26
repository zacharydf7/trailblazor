using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbTimePickerTests : BunitContext
{
    private readonly List<TimeOnly?> _changes = [];

    public TbTimePickerTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldShowTheTimeAndListATimeForEachStepWithinTheLimits_WhenGivenThem()
    {
        var cut = RenderPicker(new TimeOnly(8, 30), parameters => parameters
            .Add(picker => picker.Min, new TimeOnly(8, 0)).Add(picker => picker.Max, new TimeOnly(10, 0))
            .Add(picker => picker.Step, TimeSpan.FromMinutes(30)));

        var input = cut.Find("input[role=combobox]");
        Assert.Equal(("8:30 AM", "08:30"), (Plain(input.GetAttribute("value")), input.GetAttribute("data-value")));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal(cut.Find("[role=listbox]").Id, input.GetAttribute("aria-controls"));
        Assert.Equal(["8:00 AM", "8:30 AM", "9:00 AM", "9:30 AM", "10:00 AM"],
            cut.FindAll("[data-part=option]").Select(option => Plain(option.TextContent)));
        Assert.Equal("true", cut.Find("[data-time='08:30']").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Render_ShouldShowSeconds_WhenTheStepIsUnderAMinute()
    {
        var cut = RenderPicker(new TimeOnly(8, 30, 15),
            parameters => parameters.Add(picker => picker.Step, TimeSpan.FromSeconds(5)));

        Assert.Equal("8:30:15 AM", Plain(cut.Find("input").GetAttribute("value")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(4)]
    public void Render_ShouldListEveryHalfHour_WhenTheStepIsUnderFiveMinutes(int? minutes)
    {
        var cut = RenderPicker(null, parameters => parameters.Add(picker => picker.Step,
            minutes is { } step ? TimeSpan.FromMinutes(step) : null));

        var times = cut.FindAll("[data-part=option]").Select(option => option.GetAttribute("data-time")).ToList();
        Assert.Equal(48, times.Count);
        Assert.Equal(("00:00", "00:30", "23:30"), (times[0], times[1], times[^1]));
    }

    [Fact]
    public void Render_ShouldRunTheListPastMidnight_WhenMinIsAfterMax()
    {
        var cut = RenderPicker(null, parameters => parameters.Add(picker => picker.Min, new TimeOnly(22, 0))
            .Add(picker => picker.Max, new TimeOnly(2, 0)).Add(picker => picker.Step, TimeSpan.FromHours(1)));

        Assert.Equal(["22:00", "23:00", "00:00", "01:00", "02:00"],
            cut.FindAll("[data-part=option]").Select(option => option.GetAttribute("data-time")));
    }

    [Theory]
    [InlineData(8, 30, "08:30")]
    [InlineData(8, 40, "09:00")]
    [InlineData(23, 50, "00:00")]
    public void Render_ShouldStartTheKeysAtTheChosenTimeOrTheNextListedOne_WhenATimeIsChosen(int hour, int minute,
        string start)
    {
        var cut = RenderPicker(new TimeOnly(hour, minute));

        Assert.Equal(start, Assert.Single(cut.FindAll("[data-start]")).GetAttribute("data-time"));
    }

    [Theory]
    [InlineData(1, "01:00")]
    [InlineData(23, "23:00")]
    [InlineData(3, "22:00")]
    public void Render_ShouldStartTheKeysAtTheChosenTime_WhenTheListRunsPastMidnight(int hour, string start)
    {
        var cut = RenderPicker(new TimeOnly(hour, 0), parameters => parameters
            .Add(picker => picker.Min, new TimeOnly(22, 0)).Add(picker => picker.Max, new TimeOnly(2, 0))
            .Add(picker => picker.Step, TimeSpan.FromHours(1)));

        Assert.Equal(start, Assert.Single(cut.FindAll("[data-start]")).GetAttribute("data-time"));
    }

    [Fact]
    public void Render_ShouldStartTheKeysAtTheFirstTime_WhenNoTimeIsChosen()
    {
        var cut = RenderPicker(null);

        Assert.Empty(cut.FindAll("[data-start]"));
    }

    [Fact]
    public async Task Click_ShouldChooseTheTime_WhenATimeInTheListIsClicked()
    {
        var cut = RenderPicker(null);

        await cut.Find("[data-time='09:30']").ClickAsync();

        Assert.Equal([new TimeOnly(9, 30)], _changes);
        Assert.Equal("9:30 AM", Plain(cut.Find("input").GetAttribute("value")));
    }

    [Theory]
    [InlineData("9:15 PM")]
    [InlineData("9:15 pm")]
    [InlineData("21:15")]
    public async Task Change_ShouldReadTheTime_WhenTypedInTheCulturesFormOrIn24HourForm(string text)
    {
        var cut = RenderPicker(null);

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = text });

        Assert.Equal([new TimeOnly(21, 15)], _changes);
    }

    [Fact]
    public async Task Render_ShouldShowAndReadTwentyFourHourTimes_WhenTheCultureWritesThem()
    {
        var cut = RenderPicker(new TimeOnly(21, 15),
            parameters => parameters.Add(picker => picker.Culture, CultureInfo.GetCultureInfo("de-DE")));
        Assert.Equal("21:15", cut.Find("input").GetAttribute("value"));
        Assert.Equal("21:30", cut.Find("[data-time='21:30']").TextContent);

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "7:05" });

        Assert.Equal([new TimeOnly(7, 5)], _changes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldLeaveOutTheClockButton_WhenTheTimeCantChange(bool readOnly)
    {
        var cut = RenderPicker(null, parameters => parameters.Add(picker => picker.ReadOnly, readOnly)
            .Add(picker => picker.Disabled, !readOnly));

        Assert.Empty(cut.FindAll("[data-part=toggle]"));
    }

    [Fact]
    public void Render_ShouldNameTheClockButtonAndKeepItOutOfTheTabOrder_WhenInteractive()
    {
        var cut = RenderPicker(null);

        var toggle = cut.Find("[data-part=toggle]");
        Assert.Equal(("Choose a time", "-1"), (toggle.GetAttribute("aria-label"), toggle.GetAttribute("tabindex")));
    }

    [Fact]
    public void Render_ShouldBeTheBrowsersTimeBoxWithItsLimitsAndStep_WhenTheFormIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", false));

        var cut = RenderPicker(new TimeOnly(8, 30), parameters => parameters
            .Add(picker => picker.Min, new TimeOnly(6, 0)).Add(picker => picker.Max, new TimeOnly(18, 0))
            .Add(picker => picker.Step, TimeSpan.FromMinutes(15)));

        var input = cut.Find("input[type=time]");
        Assert.Equal(("08:30", "06:00", "18:00", "900"), (input.GetAttribute("value"), input.GetAttribute("min"),
            input.GetAttribute("max"), input.GetAttribute("step")));
        Assert.Empty(cut.FindAll("[role=listbox], [data-part=toggle]"));
    }

    [Fact]
    public async Task Change_ShouldSetTheTime_WhenTheBoxGivesOne()
    {
        var cut = RenderPicker(null);

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "14:45" });
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "" });

        Assert.Equal([new TimeOnly(14, 45), null], _changes);
    }

    [Theory]
    [InlineData("08:00")]
    [InlineData("18:00")]
    public async Task Change_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits(string text)
    {
        var cut = RenderPicker(null, parameters => NineToFive(parameters));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = text });

        Assert.Equal("Choose a time from 9:00 AM to 5:00 PM.", ErrorText(cut));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Theory]
    [InlineData("09:00")]
    [InlineData("12:00")]
    [InlineData("17:00")]
    public async Task Change_ShouldShowNoMessage_WhenTheTimeIsWithinTheLimits(string text)
    {
        var cut = RenderPicker(null, parameters => NineToFive(parameters));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = text });

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Theory]
    [InlineData("23:00", false)]
    [InlineData("05:00", false)]
    [InlineData("12:00", true)]
    public async Task Change_ShouldTakeTimesAcrossMidnight_WhenMinIsAfterMax(string text, bool refused)
    {
        var cut = RenderPicker(null, parameters => parameters.Add(picker => picker.Min, new TimeOnly(22, 0))
            .Add(picker => picker.Max, new TimeOnly(6, 0)));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = text });

        Assert.Equal(refused, cut.FindAll("[data-part=error]").Count == 1);
    }

    [Theory]
    [InlineData(true, "08:00", "Choose 9:00 AM or later.")]
    [InlineData(false, "18:00", "Choose 5:00 PM or earlier.")]
    public async Task Change_ShouldSayTheOneLimit_WhenOnlyMinOrMaxIsSet(bool onlyMin, string text, string message)
    {
        var cut = RenderPicker(null, parameters =>
        {
            if (onlyMin)
            {
                parameters.Add(picker => picker.Min, new TimeOnly(9, 0));
            }
            else
            {
                parameters.Add(picker => picker.Max, new TimeOnly(17, 0));
            }
        });

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = text });

        Assert.Equal(message, ErrorText(cut));
    }

    [Fact]
    public async Task Change_ShouldUseTheAppsMessage_WhenLabelsGiveOne()
    {
        var cut = RenderPicker(null, parameters => NineToFive(parameters)
            .Add(picker => picker.Labels, new TimePickerLabels { OutOfRange = "Open {0} to {1} only." }));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "18:00" });

        Assert.Equal("Open 9:00 AM to 5:00 PM only.", ErrorText(cut));
    }

    [Fact]
    public async Task Change_ShouldShowAMessage_WhenTheTextIsntATime()
    {
        var cut = RenderPicker(null);

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "25:00" });

        Assert.Empty(_changes);
        Assert.Single(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task Change_ShouldReadSeconds_WhenTheStepHasThem()
    {
        var cut = RenderPicker(null, parameters => parameters.Add(picker => picker.Step, TimeSpan.FromSeconds(5)));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "08:30:15" });

        Assert.Equal([new TimeOnly(8, 30, 15)], _changes);
    }

    [Fact]
    public void Render_ShouldListTimesWithinTimeSpanLimits_WhenBoundToATimeSpan()
    {
        var cut = Render<TbTimePicker<TimeSpan>>(parameters => parameters.Add(picker => picker.Label, "Start")
            .Add(picker => picker.Min, new TimeSpan(9, 0, 0)).Add(picker => picker.Max, new TimeSpan(17, 30, 0)));

        var times = cut.FindAll("[data-part=option]").Select(option => option.GetAttribute("data-time")).ToList();
        Assert.Equal(("09:00", "17:30"), (times[0], times[^1]));
    }

    [Fact]
    public void Render_ShouldThrow_WhenATimeSpanIsntATimeOfDay()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbTimePicker<TimeSpan>>(parameters =>
            parameters.Add(picker => picker.Value, TimeSpan.FromHours(25))));

        Assert.Contains("1.01:00:00", failure.Message);
    }

    [Fact]
    public async Task Change_ShouldGiveATimeSpanFromMidnight_WhenBoundToATimeSpan()
    {
        var changes = new List<TimeSpan?>();
        var cut = Render<TbTimePicker<TimeSpan>>(parameters => parameters.Add(picker => picker.Label, "Start")
            .Add(picker => picker.ValueChanged, (TimeSpan? time) => changes.Add(time)));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "07:05" });

        Assert.Equal([new TimeSpan(7, 5, 0)], changes);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTypeIsntATime()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbTimePicker<DateTime>>());

        Assert.Contains("DateTime", failure.Message);
    }

    // Newer ICU versions write a narrow no-break space before AM and PM.
    private static string ErrorText(IRenderedComponent<IComponent> cut) =>
        Plain(cut.Find("[data-part=error]").TextContent.Trim());

    private static string Plain(string? text) => text?.Replace('\u202F', ' ') ?? "";

    private static ComponentParameterCollectionBuilder<TbTimePicker<TimeOnly>> NineToFive(
        ComponentParameterCollectionBuilder<TbTimePicker<TimeOnly>> parameters) =>
        parameters.Add(picker => picker.Min, new TimeOnly(9, 0)).Add(picker => picker.Max, new TimeOnly(17, 0));

    private IRenderedComponent<TbTimePicker<TimeOnly>> RenderPicker(TimeOnly? value,
        Action<ComponentParameterCollectionBuilder<TbTimePicker<TimeOnly>>>? configure = null) =>
        Render<TbTimePicker<TimeOnly>>(parameters =>
        {
            parameters.Add(picker => picker.Value, value).Add(picker => picker.Label, "Start time")
                .Add(picker => picker.ValueChanged, (TimeOnly? time) => _changes.Add(time));
            configure?.Invoke(parameters);
        });
}
