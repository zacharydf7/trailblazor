using System.Globalization;
using Bunit;
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

    private TbFieldDriver Drive(Type field, string? error, bool invalid = false)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Trail");
            builder.AddAttribute(2, nameof(TbTextField<string>.Error), error);
            builder.AddAttribute(3, nameof(TbTextField<string>.Invalid), invalid);
            var sequence = 4;
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
