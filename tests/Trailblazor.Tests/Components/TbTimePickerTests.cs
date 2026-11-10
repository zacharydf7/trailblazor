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
    public void Render_ShouldShowTheTimeInTheTimeBoxWithItsLimitsAndStep_WhenGivenThem()
    {
        var cut = RenderPicker(new TimeOnly(8, 30), parameters => parameters
            .Add(picker => picker.Min, new TimeOnly(6, 0)).Add(picker => picker.Max, new TimeOnly(18, 0))
            .Add(picker => picker.Step, TimeSpan.FromMinutes(15)));

        var input = cut.Find("input[type=time]");
        Assert.Equal(("08:30", "06:00", "18:00", "900"), (input.GetAttribute("value"), input.GetAttribute("min"),
            input.GetAttribute("max"), input.GetAttribute("step")));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
    }

    [Fact]
    public void Render_ShouldShowSeconds_WhenTheStepIsUnderAMinute()
    {
        var cut = RenderPicker(new TimeOnly(8, 30, 15),
            parameters => parameters.Add(picker => picker.Step, TimeSpan.FromSeconds(5)));

        Assert.Equal("08:30:15", cut.Find("input").GetAttribute("value"));
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

        Assert.Equal("Choose a time from 9:00 AM to 5:00 PM.", cut.Find("[data-part=error]").TextContent.Trim());
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

        Assert.Equal(message, cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task Change_ShouldUseTheAppsMessage_WhenLabelsGiveOne()
    {
        var cut = RenderPicker(null, parameters => NineToFive(parameters)
            .Add(picker => picker.Labels, new TimePickerLabels { OutOfRange = "Open {0} to {1} only." }));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "18:00" });

        Assert.Equal("Open 9:00 AM to 5:00 PM only.", cut.Find("[data-part=error]").TextContent.Trim());
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
    public void Render_ShouldShowTimeSpanLimitsAsTimes_WhenBoundToATimeSpan()
    {
        var cut = Render<TbTimePicker<TimeSpan>>(parameters => parameters.Add(picker => picker.Label, "Start")
            .Add(picker => picker.Min, new TimeSpan(9, 0, 0)).Add(picker => picker.Max, new TimeSpan(17, 30, 0)));

        var input = cut.Find("input");
        Assert.Equal(("09:00", "17:30"), (input.GetAttribute("min"), input.GetAttribute("max")));
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
