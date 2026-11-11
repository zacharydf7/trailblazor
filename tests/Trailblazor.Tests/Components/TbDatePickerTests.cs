using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class TbDatePickerTests : BunitContext
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly DateOnly June3 = new(2026, 6, 3);
    private readonly List<DateOnly?> _changes = [];

    public TbDatePickerTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero)));
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldShowTheDateInTheFormatWithItAsThePlaceholder_WhenGivenADate()
    {
        var cut = RenderPicker(June3);

        var input = cut.Find("input[data-part=input]");
        Assert.Equal("6/3/2026", input.GetAttribute("value"));
        Assert.Equal("MM/DD/YYYY", input.GetAttribute("placeholder"));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal("2026-06-03", input.GetAttribute("data-value"));
    }

    [Fact]
    public async Task Change_ShouldReadAndShowAGregorianYear_WhenTheCulturesCalendarIsAnother()
    {
        var cut = Render<TbDatePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Start date")
            .Add(picker => picker.Value, June3).Add(picker => picker.Culture, CultureInfo.GetCultureInfo("th-TH"))
            .Add(picker => picker.ValueChanged, (DateOnly? date) => _changes.Add(date)));

        Assert.Equal("3/6/2026", cut.Find("input[data-part=input]").GetAttribute("value"));
        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "12/6/2026" });

        Assert.Equal([new DateOnly(2026, 6, 12)], _changes);
    }

    [Theory]
    [InlineData("1.5.2026")]
    [InlineData("01.05.26")]
    [InlineData("1.5.26")]
    [InlineData("1.5.99", 1999)]
    public async Task Change_ShouldReadTheDate_WhenItLeavesOutLeadingZerosOrTwoYearDigits(string text, int year = 2026)
    {
        var cut = Render<TbDatePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Start date")
            .Add(picker => picker.Value, June3).Add(picker => picker.Culture, CultureInfo.GetCultureInfo("de-DE"))
            .Add(picker => picker.ValueChanged, (DateOnly? date) => _changes.Add(date)));

        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = text });

        Assert.Equal([new DateOnly(year, 5, 1)], _changes);
    }

    [Fact]
    public async Task Change_ShouldSetTheDate_WhenTheTextIsInTheFormat()
    {
        var cut = RenderPicker(June3);

        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "6/12/2026" });

        Assert.Equal([new DateOnly(2026, 6, 12)], _changes);
    }

    [Fact]
    public async Task Change_ShouldShowAMessageAndKeepTheDate_WhenTheTextCantBeRead()
    {
        var cut = RenderPicker(June3);

        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "June 12" });

        Assert.Empty(_changes);
        Assert.Equal("Enter the date as MM/DD/YYYY.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("June 12", cut.Find("input[data-part=input]").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input[data-part=input]").GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task Change_ShouldRefuseTheDate_WhenItCantBeChosen()
    {
        var cut = RenderPicker(June3, parameters => parameters.Add(picker => picker.Max, new DateOnly(2026, 6, 30)));

        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "7/1/2026" });

        Assert.Empty(_changes);
        Assert.Equal("7/1/2026 can't be chosen.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task Pick_ShouldSetTheDate_WhenADayIsClicked()
    {
        var cut = RenderPicker(June3);

        await cut.Find("[role=grid] [data-date='2026-06-09']").ClickAsync(new MouseEventArgs());

        Assert.Equal([new DateOnly(2026, 6, 9)], _changes);
        Assert.Equal("6/9/2026", cut.Find("input[data-part=input]").GetAttribute("value"));
    }

    [Fact]
    public async Task Pick_ShouldTakeBackTheMessageAboutTypedText_WhenTheChosenDayIsPickedAgain()
    {
        var cut = RenderPicker(June3);
        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "6/31/2026" });
        Assert.Single(cut.FindAll("[data-part=error]"));

        await cut.Find("[role=grid] [data-date='2026-06-03']").ClickAsync(new MouseEventArgs());

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Equal("6/3/2026", cut.Find("input[data-part=input]").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldMarkTheGridForThePopoverToFocus_WhenInteractive()
    {
        var cut = RenderPicker(June3);

        var grid = cut.Find("[role=grid]");
        Assert.True(grid.HasAttribute("data-tb-popover-focus"));
        Assert.False(grid.HasAttribute("autofocus"));
        Assert.False(grid.HasAttribute("data-tb-autofocus"));
    }

    [Fact]
    public void Render_ShouldOpenTheCalendarOnTheChosenDayWithItsLimits_WhenGivenADate()
    {
        var cut = RenderPicker(June3, parameters => parameters.Add(picker => picker.IsDateDisabled,
            date => date.Day == 25));

        Assert.Equal("0", cut.Find("[role=grid] [data-date='2026-06-03']").GetAttribute("tabindex"));
        Assert.Equal("true", cut.Find("[role=grid] [data-date='2026-06-25']").GetAttribute("aria-disabled"));
        Assert.Equal("Choose a date", cut.Find("[data-part=popover] [aria-haspopup]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Clear_ShouldClearTheDateAndFocusTheBox_WhenPressed()
    {
        var cut = RenderPicker(June3, parameters => parameters.Add(picker => picker.Clearable, true));

        await cut.Find("[data-part=clear]").ClickAsync(new MouseEventArgs());

        Assert.Equal([(DateOnly?)null], _changes);
        Assert.Empty(cut.FindAll("[data-part=clear]"));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Render_ShouldTurnOffTheBoxAndButton_WhenReadOnly()
    {
        var cut = RenderPicker(June3, parameters => parameters.Add(picker => picker.ReadOnly, true)
            .Add(picker => picker.Clearable, true));

        Assert.True(cut.Find("input[data-part=input]").HasAttribute("readonly"));
        Assert.True(cut.Find("[data-part=popover] [aria-haspopup]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Render_ShouldShowTheBrowsersDateBox_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderPicker(June3);

        var input = cut.Find("input[type=date]");
        Assert.Equal("2026-06-03", input.GetAttribute("value"));
        Assert.Empty(cut.FindAll("[role=grid]"));
    }

    [Fact]
    public async Task Change_ShouldKeepTheTimeOfDay_WhenTheSameDayIsTypedForADateTime()
    {
        var changes = new List<DateTime?>();
        var cut = Render<TbDatePicker<DateTime>>(parameters => parameters.Add(picker => picker.Label, "Start")
            .Add(picker => picker.Value, new DateTime(2026, 6, 3, 14, 30, 0)).Add(picker => picker.Culture, English)
            .Add(picker => picker.ValueChanged, (DateTime? date) => changes.Add(date)));

        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "6/3/2026" });
        await cut.Find("input[data-part=input]").ChangeAsync(new ChangeEventArgs { Value = "6/4/2026" });

        Assert.Equal([new DateTime(2026, 6, 4)], changes);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTypeIsntADate()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbDatePicker<DateTimeOffset>>());

        Assert.Contains("DateTimeOffset", failure.Message);
    }

    private IRenderedComponent<TbDatePicker<DateOnly>> RenderPicker(DateOnly? value,
        Action<ComponentParameterCollectionBuilder<TbDatePicker<DateOnly>>>? configure = null) =>
        Render<TbDatePicker<DateOnly>>(parameters =>
        {
            parameters.Add(picker => picker.Value, value).Add(picker => picker.Label, "Start date")
                .Add(picker => picker.Culture, English)
                .Add(picker => picker.ValueChanged, (DateOnly? date) => _changes.Add(date));
            configure?.Invoke(parameters);
        });
}
