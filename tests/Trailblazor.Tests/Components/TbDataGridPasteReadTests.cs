using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// How a cell pasted into a grid that edits in batches is read, for each kind of column.
public sealed class TbDataGridPasteReadTests : BunitContext
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

    public TbDataGridPasteReadTests()
    {
        this.AddTrailblazor();
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        Services.Configure<TrailblazorOptions>(options =>
            options.Times.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Trail", TimeSpan.FromHours(-7), "Trail", "Trail"));
    }

    public enum Level
    {
        Easy,
        VeryHard,
    }

    public sealed class Visit
    {
        public int Id { get; init; } = 1;

        public string Name { get; set; } = "Pine Hollow";

        public bool Open { get; set; }

        public Level Level { get; set; }

        public DateOnly Day { get; set; } = new(2026, 1, 1);

        public TimeOnly Starts { get; set; } = new(8, 0);

        public DateTimeOffset Booked { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Range(1, 10)]
        public int? Guests { get; set; } = 2;

        public int Groups { get; set; } = 1;
    }

    private readonly List<GridBatchEdit<Visit>> _saved = [];

    private IRenderedComponent<TbDataGrid<Visit>> RenderVisits() =>
        Render<TbDataGrid<Visit>>(parameters => parameters.Add(grid => grid.Items, [new Visit()])
            .Add(grid => grid.AriaLabel, "Visits").Add(grid => grid.KeySelector, visit => visit.Id)
            .Add(grid => grid.EditMode, GridEditMode.Batch)
            .Add(grid => grid.OnBatchSave, (GridBatchEdit<Visit> batch) => _saved.Add(batch))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(name => name.Field, visit => visit.Name)
                .Add(name => name.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(open => open.Field, visit => visit.Open)
                .Add(open => open.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(level => level.Field, visit => visit.Level)
                .Add(level => level.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(day => day.Field, visit => visit.Day)
                .Add(day => day.Format, "dd/MM/yyyy").Add(day => day.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(starts => starts.Field, visit => visit.Starts)
                .Add(starts => starts.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(booked => booked.Field, visit => visit.Booked)
                .Add(booked => booked.Format, "yyyy-MM-dd HH:mm").Add(booked => booked.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(guests => guests.Field, visit => visit.Guests)
                .Add(guests => guests.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(groups => groups.Field, visit => visit.Groups)
                .Add(groups => groups.Editable, true)
                .Add(groups => groups.Validate, (_, value) => value is > 5 ? "Too many groups." : null)));

    private async Task<string?> PastedAsync(string column, string text)
    {
        var cut = RenderVisits();
        await cut.Tb().Grid("Visits").PasteAsync("Pine Hollow", column, text);
        await cut.InvokeAsync(cut.Instance.SaveChangesAsync);
        return _saved.SelectMany(batch => batch.Changes).SingleOrDefault() is { } change
            ? change.Value is null ? "null" : Convert.ToString(change.Value, CultureInfo.InvariantCulture) : null;
    }

    [Theory]
    [InlineData("Open", "Yes", "True")]
    [InlineData("Open", "false", null)]
    [InlineData("Level", "Very hard", "VeryHard")]
    [InlineData("Day", "05/12/2026", "12/05/2026")]
    [InlineData("Starts", "14:30", "14:30")]
    [InlineData("Starts", "2:30 PM", "14:30")]
    [InlineData("Booked", "2026-03-04 09:00", "03/04/2026 09:00:00 -07:00")]
    [InlineData("Guests", "\n", "null")]
    [InlineData("Guests", "4", "4")]
    public async Task PasteAsync_ShouldReadTheCellAsItsColumnsEditorDoes_WhenGivenItsText(string column, string text,
        string? expected)
    {
        var value = await PastedAsync(column, text);

        // A value pasted back to what the item holds keeps no change, as Open's false does.
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("Groups", "\n")]
    [InlineData("Groups", "6")]
    [InlineData("Guests", "40")]
    [InlineData("Day", "2026-12-05")]
    public async Task PasteAsync_ShouldKeepNothing_WhenTheCellFailsItsColumnsChecks(string column, string text)
    {
        Assert.Null(await PastedAsync(column, text));
    }

    [Fact]
    public async Task PasteAsync_ShouldDoNothing_WhenItLandsOnNoCellItCanChange()
    {
        var host = Render<AnnouncerHost>();
        var cut = Render<TbDataGrid<Visit>>(parameters => parameters.Add(grid => grid.Items, [new Visit()])
            .Add(grid => grid.AriaLabel, "Visits").Add(grid => grid.EditMode, GridEditMode.Batch)
            .AddChildContent<TbColumn<Visit>>(column => column.Add(name => name.Field, visit => visit.Name)
                .Add(name => name.Editable, true))
            .AddChildContent<TbColumn<Visit>>(column => column.Add(level => level.Field, visit => visit.Level)));

        await cut.Tb().Grid("Visits").PasteAsync("Pine Hollow", "Level", "Easy");

        Assert.False(cut.Instance.HasChanges);
        Assert.DoesNotContain(host.FindAll("[role=status]"), status => status.TextContent.Contains("pasted"));
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _culture;
        base.Dispose(disposing);
    }
}
