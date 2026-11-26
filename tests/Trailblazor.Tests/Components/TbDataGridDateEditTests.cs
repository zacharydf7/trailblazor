using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// An editable column of dates or times edits with the picker for its type.
public class TbDataGridDateEditTests : BunitContext
{
    public TbDataGridDateEditTests()
    {
        this.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public sealed class Trip
    {
        public int Id { get; init; }

        public DateOnly Starts { get; set; }

        public TimeOnly Leaves { get; set; }

        public DateTime Booked { get; set; }

        public DateTimeOffset Logged { get; set; }

        public DateOnly? Returns { get; set; }
    }

    private readonly Trip _trip = new()
    {
        Id = 1,
        Starts = new(2026, 6, 3),
        Leaves = new(7, 30),
        Booked = new(2026, 5, 1, 9, 0, 0),
        Logged = new(2026, 5, 2, 10, 0, 0, TimeSpan.Zero),
        Returns = new(2026, 6, 9),
    };

    [Theory]
    [InlineData("Starts", typeof(TbDatePicker<DateOnly>))]
    [InlineData("Leaves", typeof(TbTimePicker<TimeOnly>))]
    [InlineData("Booked", typeof(TbDateTimePicker<DateTime>))]
    [InlineData("Logged", typeof(TbDateTimePicker<DateTimeOffset>))]
    public async Task StartEditAsync_ShouldOpenThePickerForTheFieldsType_WhenTheFieldHoldsADateOrATime(string column,
        Type picker)
    {
        var cut = RenderTrips();

        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, column));

        Assert.Single(cut.FindComponents<IComponent>(), component => component.Instance.GetType() == picker);
    }

    [Fact]
    public async Task Enter_ShouldSetTheDateOnTheItem_WhenADateIsChosenInTheCell()
    {
        var cut = RenderTrips();
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Starts"));

        await cut.Tb().DatePicker("Starts").ChooseAsync(new DateOnly(2026, 6, 20));
        await PressEnterAsync(cut);

        Assert.Equal(new DateOnly(2026, 6, 20), _trip.Starts);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task Enter_ShouldSetTheTimeOnTheItem_WhenATimeIsChosenInTheCell()
    {
        var cut = RenderTrips();
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Leaves"));

        await cut.Tb().TimePicker("Leaves").ChooseAsync(new TimeOnly(8, 15));
        await PressEnterAsync(cut);

        Assert.Equal(new TimeOnly(8, 15), _trip.Leaves);
    }

    [Fact]
    public async Task Enter_ShouldSetTheDateAndTimeOnTheItem_WhenADateAndTimeIsChosenInTheCell()
    {
        var cut = RenderTrips();
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Booked"));

        await cut.Tb().DateTimePicker("Booked").ChooseAsync(new DateTime(2026, 7, 4, 16, 45, 0));
        await PressEnterAsync(cut);

        Assert.Equal(new DateTime(2026, 7, 4, 16, 45, 0), _trip.Booked);
    }

    [Fact]
    public async Task Enter_ShouldSetTheMomentInTheShownZoneOnTheItem_WhenADateAndTimeIsChosenInAnOffsetCell()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Trail", TimeSpan.FromHours(-7), "Trail", "Trail");
        Services.Configure<TrailblazorOptions>(options => options.Times.TimeZone = zone);
        var cut = RenderTrips();
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Logged"));

        await cut.Tb().DateTimePicker("Logged").ChooseAsync(new DateTime(2026, 7, 4, 16, 45, 0));
        await PressEnterAsync(cut);

        Assert.Equal(new DateTimeOffset(2026, 7, 4, 16, 45, 0, TimeSpan.FromHours(-7)), _trip.Logged);
        Assert.Equal(TimeSpan.FromHours(-7), _trip.Logged.Offset);
    }

    [Fact]
    public async Task Enter_ShouldSetEveryPickedValueOnTheItem_WhenARowIsEditedTogether()
    {
        var cut = RenderTrips(mode: GridEditMode.Row);
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Starts"));

        await cut.Tb().DatePicker("Starts").ChooseAsync(new DateOnly(2026, 6, 20));
        await cut.Tb().TimePicker("Leaves").ChooseAsync(new TimeOnly(8, 15));
        await PressEnterAsync(cut);

        Assert.Equal(new DateOnly(2026, 6, 20), _trip.Starts);
        Assert.Equal(new TimeOnly(8, 15), _trip.Leaves);
    }

    [Fact]
    public async Task Enter_ShouldKeepTheEditOpen_WhenTheDateBoxHoldsTextItCouldNotRead()
    {
        var cut = RenderTrips();
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Starts"));

        await cut.Tb().DatePicker("Starts").EnterAsync("someday");
        await PressEnterAsync(cut);

        Assert.Equal(new DateOnly(2026, 6, 3), _trip.Starts);
        Assert.Single(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task Enter_ShouldKeepTheEditOpenAndSayTheDateIsRequired_WhenAPlainDateIsCleared()
    {
        var cut = RenderTrips();
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Starts"));

        await cut.Tb().DatePicker("Starts").ChooseAsync(null);
        await PressEnterAsync(cut);

        Assert.Equal(new DateOnly(2026, 6, 3), _trip.Starts);
        Assert.Contains("Starts is required", cut.Find("[data-part=editor]").TextContent);
    }

    [Fact]
    public async Task Enter_ShouldClearTheDateOnTheItem_WhenAnOptionalDateIsCleared()
    {
        var cut = RenderTrips([trip => trip.Returns]);
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Returns"));

        await cut.Tb().DatePicker("Returns").ChooseAsync(null);
        await PressEnterAsync(cut);

        Assert.Null(_trip.Returns);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task Enter_ShouldSaveTheDate_WhenItIsTypedInTheColumnsFormat()
    {
        var cut = RenderTrips([trip => trip.Starts], format: "yyyy-MM-dd");
        await cut.InvokeAsync(() => cut.Instance.StartEditAsync(_trip, "Starts"));

        await cut.Tb().DatePicker("Starts").EnterAsync("2026-06-20");
        await PressEnterAsync(cut);

        Assert.Equal(new DateOnly(2026, 6, 20), _trip.Starts);
    }

    [Fact]
    public async Task StartEditAsync_ShouldKeepTheCulturesDateForm_WhenTheColumnsFormatIsAStandardOne()
    {
        var plain = RenderTrips([trip => trip.Starts]);
        await plain.InvokeAsync(() => plain.Instance.StartEditAsync(_trip, "Starts"));
        var standard = RenderTrips([trip => trip.Starts], format: "d");

        await standard.InvokeAsync(() => standard.Instance.StartEditAsync(_trip, "Starts"));

        Assert.Equal(plain.Find("[data-part=editor] input").GetAttribute("placeholder"),
            standard.Find("[data-part=editor] input").GetAttribute("placeholder"));
    }

    private static Task PressEnterAsync(IRenderedComponent<TbDataGrid<Trip>> cut) =>
        cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey", new GridEditKeyEventArgs { Key = "Enter" });

    private IRenderedComponent<TbDataGrid<Trip>> RenderTrips(Expression<Func<Trip, object?>>[]? fields = null,
        GridEditMode mode = GridEditMode.Cell, string? format = null) =>
        Render<TbDataGrid<Trip>>(parameters => parameters
        .Add(grid => grid.Items, [_trip]).Add(grid => grid.AriaLabel, "Trips").Add(grid => grid.EditMode, mode)
        .Add(grid => grid.KeySelector, trip => trip.Id)
        .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
        {
            var sequence = 0;
            Expression<Func<Trip, object?>>[] plain =
                [trip => trip.Starts, trip => trip.Leaves, trip => trip.Booked, trip => trip.Logged];
            foreach (var field in fields ?? plain)
            {
                builder.OpenComponent<TbColumn<Trip>>(sequence++);
                builder.AddAttribute(sequence++, nameof(TbColumn<Trip>.Field), field);
                builder.AddAttribute(sequence++, nameof(TbColumn<Trip>.Editable), true);
                builder.AddAttribute(sequence++, nameof(TbColumn<Trip>.Format), format);
                builder.CloseComponent();
            }
        })));
}
