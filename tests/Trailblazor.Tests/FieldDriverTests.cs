using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;
using Trailblazor.Tests.Contract;

namespace Trailblazor.Tests;

// Every field's driver reads the field's messages and whether it's marked invalid, the same way.
public class FieldDriverTests : BunitContext
{
    public FieldDriverTests() => this.AddTrailblazor();

    public static TheoryData<Type> FieldsWithDrivers => new(ComponentContractTests.Fields.Select(row => row.Data)
        .Where(type => DriverParityTests.Helpers.ContainsKey(type.Name.Split('`')[0])));

    [Theory, MemberData(nameof(FieldsWithDrivers))]
    public void Driver_ShouldReadTheMessageAndInvalidMark_WhenTheFieldShowsTheParentsError(Type field)
    {
        var driver = Drive(field, error: "That name is taken.");

        Assert.Equal(("That name is taken.", true), (driver.Error, driver.Invalid));
        Assert.Equal(["That name is taken."], driver.Errors);
    }

    [Theory, MemberData(nameof(FieldsWithDrivers))]
    public void Driver_ShouldReadNoMessage_WhenTheFieldIsValid(Type field)
    {
        var driver = Drive(field, error: null);

        Assert.Equal((null, false), (driver.Error, driver.Invalid));
        Assert.Empty(driver.Errors);
    }

    [Theory, MemberData(nameof(FieldsWithDrivers))]
    public void Driver_ShouldReadTheInvalidMarkWithoutAMessage_WhenTheParentSetsInvalid(Type field)
    {
        var driver = Drive(field, error: null, invalid: true);

        Assert.Equal((null, true), (driver.Error, driver.Invalid));
    }

    [Theory, MemberData(nameof(FieldsWithDrivers))]
    public async Task LeaveAsync_ShouldRaiseOnBlur_WhenTheFieldListensForIt(Type field)
    {
        var left = 0;
        var driver = Drive(field, error: null, onBlur: () => left++);

        await driver.LeaveAsync();

        Assert.Equal(1, left);
    }

    [Theory, MemberData(nameof(FieldsWithDrivers))]
    public async Task LeaveAsync_ShouldNotThrow_WhenTheFieldDoesntListenForIt(Type field)
    {
        var driver = Drive(field, error: null);

        var failure = await Record.ExceptionAsync(driver.LeaveAsync);

        Assert.Null(failure);
    }

    [Fact]
    public async Task EnterAsync_ShouldLeaveTheFieldOnceTheTextIsCommitted_WhenTypingIntoATextOrNumberField()
    {
        var heard = new List<string>();
        string? trail = null;
        var text = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.ValueChanged, (string value) => trail = value)
            .Add(field => field.OnBlur, () => heard.Add($"trail {trail}")));
        var number = Render<TbNumberField<int?>>(parameters => parameters.Add(field => field.Label, "Nights")
            .Add(field => field.OnBlur, () => heard.Add("nights")));

        await text.Tb().TextField("Trail").EnterAsync("Pine Hollow");
        await number.Tb().NumberField("Nights").EnterAsync("3");

        Assert.Equal(["trail Pine Hollow", "nights"], heard);
    }

    [Fact]
    public void Invalid_ShouldBeTrue_WhenOnlyADateRangesEndIsMarkedInvalid()
    {
        var cut = Render<TbDateRangePicker<DateOnly>>(parameters => parameters.Add(field => field.Label, "Trip")
            .Add(field => field.Culture, CultureInfo.GetCultureInfo("en-US")));
        cut.Find("[data-part=start-input]").Change("6/10/2026");
        cut.Find("[data-part=end-input]").Change("6/1/2026");

        var driver = cut.Tb().DateRangePicker("Trip");

        // Only the end box is marked, which the first box the error describes doesn't show.
        Assert.Null(cut.Find("[data-part=start-input]").GetAttribute("aria-invalid"));
        Assert.Equal(("The end date is before the start date.", true), (driver.Error, driver.Invalid));
    }

    [Fact]
    public void Errors_ShouldListEachMessageInOrder_WhenTheFieldShowsSeveral()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Value, "x").Add(field => field.Error, "That name is taken.")
            .Add(field => field.Validation, _ => ["Use at least 3 letters."]));
        cut.Find("input").Change("y");

        Assert.Equal(["That name is taken.", "Use at least 3 letters."], cut.Tb().TextField("Trail").Errors);
    }

    private TbFieldDriver Drive(Type field, string? error, bool invalid = false, Action? onBlur = null)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Trail");
            builder.AddAttribute(2, nameof(TbTextField<string>.Error), error);
            builder.AddAttribute(3, nameof(TbTextField<string>.Invalid), invalid);
            if (onBlur is not null)
            {
                builder.AddAttribute(4, nameof(TbTextField<string>.OnBlur),
                    EventCallback.Factory.Create<FocusEventArgs>(this, onBlur));
            }

            var sequence = 5;
            foreach (var (name, value) in ComponentContractTests.RequiredParameters(field))
            {
                builder.AddAttribute(sequence++, name, value);
            }

            builder.CloseComponent();
        });
        var helper = DriverParityTests.Helpers[field.Name.Split('`')[0]];
        return (TbFieldDriver)typeof(TrailblazorMarkup).GetMethod(helper, [typeof(string)])!
            .Invoke(cut.Tb(), ["Trail"])!;
    }
}
