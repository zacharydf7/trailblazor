using System.Globalization;
using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class DateRangePickerExampleTests : DocsTest
{
    [Fact]
    public void DateRangePickerExample_ShouldSummarizeTheRange_WhenItAppears()
    {
        var cut = Render<DateRangePickerExample>();

        Assert.Contains("4 days, from", cut.Markup);
    }

    [Fact]
    public void PresetsExample_ShouldOfferItsRanges_WhenRendered()
    {
        var cut = Render<DateRangePickerPresetsExample>();

        Assert.Equal(["Next 7 days", "Next 30 days", "Next month"], Texts(cut.FindAll("[data-part=preset]")));
    }

    [Fact]
    public void DisabledDaysExample_ShouldMarkOnlyWeekendsUnavailable_WhenRendered()
    {
        var cut = Render<DateRangePickerDisabledDaysExample>();

        var days = cut.FindAll("[data-part=day]");

        Assert.NotEmpty(days);
        Assert.All(days, day => Assert.Equal(DateOf(day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            day.GetAttribute("aria-disabled") == "true"));
    }

    [Fact]
    public void LimitsExample_ShouldMarkDaysOutsideTheLimitsUnavailable_WhenRendered()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cut = Render<DateRangePickerLimitsExample>();

        var days = cut.FindAll("[data-part=day]");

        Assert.NotEmpty(days);
        Assert.All(days, day => Assert.Equal(DateOf(day) < today || DateOf(day) > today.AddDays(30),
            day.GetAttribute("aria-disabled") == "true"));
    }

    [Fact]
    public void DateTimeExample_ShouldShowTheStartWithItsTime_WhenRendered()
    {
        var cut = Render<DateRangePickerDateTimeExample>();

        Assert.Contains("Created between value: 2026-06-01T08:30:00", cut.Markup);
    }

    [Fact]
    public void FormatExample_ShouldShowEachFieldsOwnFormat_WhenRendered()
    {
        var cut = Render<DateRangePickerFormatExample>();
        var starts = cut.FindAll("[data-part=start-input]");

        Assert.Equal("2026-06-12", starts[0].GetAttribute("value"));
        Assert.Equal("01.07.2026", starts[1].GetAttribute("value"));
    }

    [Fact]
    public void ClearExample_ShouldShowNull_WhenTheDatesAreCleared()
    {
        var cut = Render<DateRangePickerClearExample>();
        Assert.Contains("Closure dates value: 2026-06-12 to 2026-06-15", cut.Markup);

        cut.Find("[data-part=clear]").Click();

        Assert.Contains("Closure dates value: null to null", cut.Markup);
    }

    [Fact]
    public void InvalidExample_ShouldSayWhichBoxAndKeepTheValue_WhenTheTextCannotBeRead()
    {
        var cut = Render<DateRangePickerInvalidExample>();

        cut.Find("[data-part=start-input]").Change("soon");

        Assert.Contains("Enter the start date as", cut.Find("[data-part=error]").TextContent);
        Assert.Equal("true", cut.Find("[data-part=start-input]").GetAttribute("aria-invalid"));
        Assert.Contains("Survey dates value: 2026-06-12", cut.Markup);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<DateRangePickerRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Choose both travel dates.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ValidationExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<DateRangePickerValidationExample>();

        cut.WaitForAssertion(() => Assert.Equal("A hut stay can be a week at most.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ErrorExample_ShouldShowTheErrorUntilTheDatesChange_WhenTheCheckFindsAProblem()
    {
        var cut = Render<DateRangePickerErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        Button(cut, "Check the dates").Click();
        Assert.Equal("Those dates are fully booked.", cut.Find("[data-part=error]").TextContent.Trim());

        cut.Find("[data-part=start-input]").Change(new DateOnly(2026, 6, 13).ToShortDateString());

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void StatesExample_ShouldMakeOneReadOnlyAndDisableTheOther_WhenRendered()
    {
        var cut = Render<DateRangePickerStatesExample>();
        var starts = cut.FindAll("[data-part=start-input]");

        Assert.True(starts[0].HasAttribute("readonly"));
        Assert.True(starts[1].HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnWords_WhenRendered()
    {
        var cut = Render<DateRangePickerLabelsExample>();

        Assert.Equal("Clear the rides", cut.Find("[data-part=clear]").GetAttribute("aria-label"));
        Assert.NotNull(Button(cut, "Open the ride calendar"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingThePicker()
    {
        var cut = ComponentPage("date-range-picker");
        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.Equal(["Enter", "Space", "Left", "Right", "Up", "Down", "Home", "End", "Page Up", "Page Down", "Shift",
            "Page Up", "Shift", "Page Down", "Enter", "Space", "Escape", "Tab"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", accessibility);
        Assert.DoesNotContain("Variant", accessibility);
    }

    private static DateOnly DateOf(AngleSharp.Dom.IElement day) =>
        DateOnly.ParseExact(day.GetAttribute("data-date")!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
