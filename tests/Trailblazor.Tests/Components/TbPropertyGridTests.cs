using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbPropertyGridTests : BunitContext
{
    private readonly Trail _trail = new();
    private readonly List<string> _changed = [];

    public TbPropertyGridTests() => this.AddTrailblazor();

    public enum Difficulty
    {
        Easy,
        Moderate,
        VeryHard,
    }

    [Flags]
    public enum Access
    {
        Read = 1,
        Write = 2,
    }

    public sealed class Trail
    {
        public int Id { get; } = 7;

        public string Name { get; set; } = "Ridge loop";

        [Display(Name = "Length (km)", GroupName = "Route", Description = "Out and back.", Order = 1)]
        public double LengthKm { get; set; } = 12.5;

        [Category("Route")]
        public Difficulty Difficulty { get; set; } = Difficulty.Moderate;

        [Category("Status")]
        public bool Open { get; set; } = true;

        public DateOnly? Opened { get; set; }

        public string TrailURL { get; set; } = "trails/ridge";

        public bool IsAMember { get; set; }

        [ReadOnly(true)]
        public DateTime Created { get; set; } = new(2026, 1, 2, 3, 4, 0);

        public string Code { get; init; } = "R1";

        public Access Access { get; set; } = Access.Read | Access.Write;

        [Category("Status")]
        public Difficulty? Rating { get; set; } = Difficulty.Easy;

        public List<string> Tags { get; set; } = ["views"];

        [Browsable(false)]
        public string Secret { get; set; } = "hidden";
    }

    [Fact]
    public void Render_ShouldListEachVisiblePropertyWithAnEditorForItsTypeUnderItsGroup_WhenGivenAModel()
    {
        var cut = RenderGrid();

        Assert.Equal("Trail", cut.Find("[role=group]").GetAttribute("aria-label"));
        Assert.Equal(["Id", "Name", "Opened", "TrailURL", "IsAMember", "Created", "Code", "Access", "Tags"],
            Names(cut.Find(".tb-property-grid"), direct: true));
        Assert.Equal(["Route", "Status"], cut.FindAll("[data-part=group-label]").Select(label => label.TextContent));
        // LengthKm has an Order, which puts it before a property without one.
        Assert.Equal(["LengthKm", "Difficulty"], Names(cut.FindAll("[data-part=group]")[0], direct: false));
        Assert.Equal(["Easy", "Moderate", "Very hard"], cut.Tb().PropertyGrid("Trail").Select("Difficulty").Options);
        Assert.Equal("Trail URL", Label(cut, "TrailURL"));
        Assert.Equal("Is a member", Label(cut, "IsAMember"));
        Assert.Equal("Length (km)", Label(cut, "LengthKm"));
        Assert.Contains("Out and back.", Property(cut, "LengthKm").TextContent);
        Assert.NotNull(Property(cut, "LengthKm").QuerySelector("input[type=number], input[role=spinbutton]"));
        Assert.NotNull(Property(cut, "Open").QuerySelector("[role=switch]"));
        Assert.NotNull(Property(cut, "Difficulty").QuerySelector("[role=combobox], select"));
        Assert.Empty(cut.FindAll("[data-name=Secret]"));
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Created")]
    [InlineData("Tags")]
    [InlineData("Code")]
    [InlineData("Access")]
    public void Render_ShouldMakeThePropertyReadOnly_WhenItHasNoSetterIsMarkedReadOnlyOrHasNoEditor(string name)
    {
        var cut = RenderGrid();

        Assert.True(Property(cut, name).QuerySelector("input")!.HasAttribute("readonly"));
    }

    [Fact]
    public async Task Change_ShouldSetThePropertyAndRaiseOnChange_WhenTheUserEditsIt()
    {
        var cut = RenderGrid();

        await cut.Tb().PropertyGrid("Trail").TextField("Name").EnterAsync("Lake path");
        await cut.Tb().PropertyGrid("Trail").NumberField("Length (km)").EnterAsync("8");
        await cut.Tb().PropertyGrid("Trail").Select("Difficulty").ChooseAsync("Very hard");
        await cut.Find("[data-name=Open] input").ChangeAsync(new ChangeEventArgs { Value = false });
        await cut.Tb().PropertyGrid("Trail").Select("Rating").ClearAsync();

        Assert.Equal(("Lake path", 8d, Difficulty.VeryHard, false, (Difficulty?)null),
            (_trail.Name, _trail.LengthKm, _trail.Difficulty, _trail.Open, _trail.Rating));
        Assert.Equal(["Name", "LengthKm", "Difficulty", "Open", "Rating"], _changed);
    }

    [Fact]
    public void Render_ShouldMakeEveryPropertyReadOnly_WhenReadOnlyIsSet()
    {
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.ReadOnly, true));

        // A switch can't take readonly, so it says so with aria-readonly.
        Assert.All(cut.FindAll("[data-part=property] input:not([type=hidden])"),
            input => Assert.True(input.HasAttribute("readonly") || input.GetAttribute("aria-readonly") == "true"));
    }

    [Fact]
    public async Task Fields_ShouldListAndSetTheGivenProperties_WhenGivenInPlaceOfReflection()
    {
        var settings = new Dictionary<string, object?> { ["units"] = "km", ["maxResults"] = 20 };
        var cut = Render<TbPropertyGrid<object>>(parameters => parameters.Add(grid => grid.Label, "Settings")
            .Add(grid => grid.Fields, settings.Select(setting => new PropertyGridField
            {
                Name = setting.Key, Type = setting.Value!.GetType(), Get = () => settings[setting.Key],
                Set = value => settings[setting.Key] = value, Group = setting.Key == "units" ? "Display" : null,
            }).ToList()));

        await cut.Tb().NumberField("Max results").EnterAsync("50");

        // The field without a group comes first, though it was listed second.
        Assert.Equal(["maxResults"], Names(cut.Find(".tb-property-grid"), direct: true));
        Assert.Equal(["maxResults", "units"], Names(cut.Find(".tb-property-grid"), direct: false));
        Assert.Equal(50, settings["maxResults"]);
    }

    [Fact]
    public async Task Change_ShouldSetADateButLeaveIt_WhenAnEditorForAValueThatCantBeEmptyIsCleared()
    {
        var day = new DateOnly(2026, 5, 1);
        var cut = Render<TbPropertyGrid<object>>(parameters => parameters.Add(grid => grid.Label, "Dates")
            .Add(grid => grid.Fields, [new PropertyGridField
            {
                Name = "Start", Type = typeof(DateOnly), Get = () => day, Set = value => day = (DateOnly)value!,
            }]));

        var picker = cut.FindComponent<TbDatePicker<DateOnly>>().Instance;
        await cut.InvokeAsync(() => picker.ValueChanged.InvokeAsync(null));
        Assert.Equal(new DateOnly(2026, 5, 1), day);
        await cut.InvokeAsync(() => picker.ValueChanged.InvokeAsync(new DateOnly(2026, 6, 1)));

        Assert.Equal(new DateOnly(2026, 6, 1), day);
    }

    [Theory]
    [InlineData(null, true, "needs a Model or Fields")]
    [InlineData("", false, "needs a Label")]
    public void Render_ShouldNameWhatIsMissing_WhenTheModelOrLabelIsLeftOut(string? label, bool noModel, string message)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbPropertyGrid<Trail>>(parameters =>
            parameters.Add(grid => grid.Label, label ?? "Trail").Add(grid => grid.Model, noModel ? null : _trail)));

        Assert.Contains(message, error.Message);
    }

    private IRenderedComponent<TbPropertyGrid<Trail>> RenderGrid(
        Action<ComponentParameterCollectionBuilder<TbPropertyGrid<Trail>>>? configure = null) =>
        Render<TbPropertyGrid<Trail>>(parameters =>
        {
            parameters.Add(grid => grid.Model, _trail).Add(grid => grid.Label, "Trail")
                .Add(grid => grid.OnChange, (PropertyGridField field) => _changed.Add(field.Name));
            configure?.Invoke(parameters);
        });

    private static AngleSharp.Dom.IElement Property(IRenderedComponent<TbPropertyGrid<Trail>> cut, string name) =>
        cut.Find($"[data-part=property][data-name={name}]");

    private static string Label(IRenderedComponent<TbPropertyGrid<Trail>> cut, string name) =>
        Property(cut, name).QuerySelector("label")!.TextContent.Trim();

    private static IEnumerable<string?> Names(AngleSharp.Dom.IElement scope, bool direct) =>
        scope.QuerySelectorAll(direct ? ":scope > [data-part=property]" : "[data-part=property]")
            .Select(property => property.GetAttribute("data-name"));
}
