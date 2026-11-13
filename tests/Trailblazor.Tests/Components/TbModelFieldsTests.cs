using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbModelFieldsTests : BunitContext
{
    private readonly Hike _hike = new();

    public TbModelFieldsTests() => this.AddTrailblazor();

    public enum Pace
    {
        Easy,
        Brisk,
    }

    public sealed class Hike
    {
        [Required, Display(Name = "Trail name", Order = 1, Prompt = "Ridge loop")]
        public string? Name { get; set; }

        [Range(1, 50), Display(Name = "Length (km)", Order = 2)]
        public int Length { get; set; } = 5;

        [EmailAddress, DataType(DataType.EmailAddress), Display(GroupName = "Contact", Order = 0)]
        public string? Email { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Description("Shown to everyone.")]
        public bool Public { get; set; }

        [Browsable(false)]
        public int Id { get; set; }

        public string Code { get; init; } = "H1";

        [Required(ErrorMessage = "Pick a pace")]
        public Pace? Speed { get; set; }

        [Required]
        public DateOnly Start { get; set; } = new(2026, 5, 1);

        [Required]
        public Pace Mode { get; set; }
    }

    private readonly List<string> _changed = [];
    private IReadOnlyList<string> _errors = [];

    private IRenderedComponent<TbForm> RenderForm(RenderFragment? overrides = null, int columns = 1) =>
        Render<TbForm>(parameters => parameters.Add(form => form.Model, _hike).Add(form => form.DataAnnotations, true)
            .Add(form => form.ChildContent, Fields(overrides, columns))
            .Add(form => form.ErrorsChanged, errors => _errors = errors));

    private RenderFragment Fields(RenderFragment? overrides, int columns = 1) => builder =>
    {
        builder.OpenComponent<TbModelFields<Hike>>(0);
        builder.AddComponentParameter(1, nameof(TbModelFields<Hike>.Model), _hike);
        builder.AddComponentParameter(2, nameof(TbModelFields<Hike>.Columns), columns);
        builder.AddComponentParameter(3, nameof(TbModelFields<Hike>.ChildContent), overrides);
        builder.AddComponentParameter(4, nameof(TbModelFields<Hike>.OnChange),
            EventCallback.Factory.Create<string>(this, _changed.Add));
        builder.CloseComponent();
    };

    private static RenderFragment Override(string name, Action<Action<string, object?>> set) => builder =>
    {
        builder.OpenComponent<TbModelField>(0);
        builder.AddComponentParameter(1, nameof(TbModelField.Name), name);
        var sequence = 2;
        set((parameter, value) => builder.AddComponentParameter(sequence++, parameter, value));
        builder.CloseComponent();
    };

    private static string[] Names(IRenderedComponent<TbForm> cut) =>
        [.. cut.FindAll("[data-part=field]").Select(field => field.GetAttribute("data-name")!)];

    [Fact]
    public void Fields_ShouldShowTheShownPropertiesInTheirOrder_WhenGivenAModel()
    {
        var cut = RenderForm();

        Assert.Equal(["Name", "Length", "Notes", "Public", "Code", "Speed", "Start", "Mode", "Email"], Names(cut));
        Assert.Equal("Contact", cut.Find("[data-part=group] > [data-part=group-label]").TextContent);
        Assert.Equal("Email", cut.Find("[data-part=group] [data-part=field]").GetAttribute("data-name"));
    }

    [Fact]
    public void Fields_ShouldFollowTheAnnotations_WhenTheyNameDescribeAndLimitAProperty()
    {
        var cut = RenderForm();

        Assert.Equal("Ridge loop", cut.Find("[data-name=Name] input").GetAttribute("placeholder"));
        Assert.Equal("1", cut.Find("[data-name=Length] input").GetAttribute("min"));
        Assert.Equal("50", cut.Find("[data-name=Length] input").GetAttribute("max"));
        Assert.Equal("email", cut.Find("[data-name=Email] input").GetAttribute("type"));
        Assert.Equal("Notes", cut.Find("[data-name=Notes] label").TextContent.Trim());
        Assert.Single(cut.FindAll("[data-name=Notes] textarea"));
        Assert.Contains("Shown to everyone.", cut.Find("[data-name=Public]").TextContent);
        Assert.True(cut.Find("[data-name=Code] input").HasAttribute("readonly"));
    }

    [Fact]
    public async Task Fields_ShouldSetTheModel_WhenTheUserChangesAField()
    {
        var cut = RenderForm();

        await cut.Tb().TextField("Trail name").EnterAsync("Ridge loop");
        await cut.Tb().NumberField("Length (km)").EnterAsync("12");

        Assert.Equal("Ridge loop", _hike.Name);
        Assert.Equal(12, _hike.Length);
        Assert.Equal(["Name", "Length"], _changed);
    }

    [Fact]
    public async Task Fields_ShouldShowTheAnnotationsMessages_WhenTheFormIsSubmitted()
    {
        _hike.Length = 80;
        var cut = RenderForm();

        await cut.Find("form").SubmitAsync();

        Assert.Contains("Trail name is required", cut.Find("[data-name=Name]").TextContent);
        Assert.Contains("must be between 1 and 50", cut.Find("[data-name=Length]").TextContent);
        Assert.Contains("Pick a pace", cut.Find("[data-name=Speed]").TextContent);
        Assert.Equal("true", cut.Find("[data-name=Start] input").GetAttribute("aria-required"));
        Assert.Empty(cut.FindAll("[data-name=Mode] [aria-required=true]"));
        Assert.Equal("true", cut.Find("[data-name=Name] input").GetAttribute("aria-required"));
    }

    [Fact]
    public async Task Fields_ShouldClearAPropertysMessage_WhenAFieldThatCantBindToItIsFixed()
    {
        _hike.Name = "Ridge loop";
        var cut = RenderForm();
        await cut.Find("form").SubmitAsync();
        Assert.Contains(_errors, error => error.Contains("Pick a pace"));

        await cut.Tb().Select("Speed").ChooseAsync("Brisk");

        Assert.Equal(Pace.Brisk, _hike.Speed);
        Assert.Empty(_errors);
    }

    [Fact]
    public void Fields_ShouldApplyAnOverride_WhenAModelFieldChangesALabelPlaceOrWidth()
    {
        var cut = RenderForm(builder =>
        {
            builder.AddContent(0, Override("Notes", set =>
            {
                set(nameof(TbModelField.Label), "Trip notes");
                set(nameof(TbModelField.HelperText), "Anything worth remembering.");
                set(nameof(TbModelField.Order), 0);
                set(nameof(TbModelField.Span), 5);
            }));
            builder.AddContent(1, Override("Public", set => set(nameof(TbModelField.Hidden), true)));
            builder.AddContent(2, Override("Name", set => set(nameof(TbModelField.ReadOnly), true)));
        }, columns: 2);

        Assert.Equal(["Notes", "Name", "Length", "Code", "Speed", "Start", "Mode", "Email"], Names(cut));
        Assert.Equal("Trip notes", cut.Find("[data-name=Notes] label").TextContent.Trim());
        Assert.Contains("Anything worth remembering.", cut.Find("[data-name=Notes]").TextContent);
        Assert.Contains("--tb-model-fields-span: 2;", cut.Find("[data-name=Notes]").GetAttribute("style"));
        Assert.Contains("--tb-model-fields-columns: 2;", cut.Find(".tb-model-fields").GetAttribute("style"));
        Assert.True(cut.Find("[data-name=Name] input").HasAttribute("readonly"));
    }

    [Fact]
    public void Fields_ShouldFollowAnOverride_WhenItsLabelOrNameChangesAfterTheFirstRender()
    {
        var cut = RenderForm(Override("Notes", set => set(nameof(TbModelField.Label), "Trip notes")));

        cut.Render(parameters => parameters.Add(form => form.ChildContent,
            Fields(Override("Notes", set => set(nameof(TbModelField.Label), "Trail notes")))));
        Assert.Equal("Trail notes", cut.Find("[data-name=Notes] label").TextContent.Trim());
        cut.Render(parameters => parameters.Add(form => form.ChildContent,
            Fields(Override("Code", set => set(nameof(TbModelField.Label), "Trail notes")))));

        Assert.Equal("Notes", cut.Find("[data-name=Notes] label").TextContent.Trim());
        Assert.Equal("Trail notes", cut.Find("[data-name=Code] label").TextContent.Trim());
    }

    [Fact]
    public void Fields_ShouldDrawTheOverridesEditor_WhenAModelFieldGivesOne()
    {
        RenderFragment<ModelFieldContext> editor = context => builder =>
        {
            builder.OpenElement(0, "output");
            builder.AddAttribute(1, "data-custom", context.Name);
            builder.AddContent(2, $"{context.Label} | {context.HelperText} | {context.ReadOnly}");
            builder.CloseElement();
        };

        var cut = RenderForm(Override("Public", set => set(nameof(TbModelField.Editor), editor)));

        Assert.Equal("Public | Shown to everyone. | False",
            cut.Find("[data-name=Public] output[data-custom=Public]").TextContent);
        Assert.Empty(cut.FindAll("[data-name=Public] [role=switch]"));
    }

    [Fact]
    public void Fields_ShouldThrow_WhenAModelFieldNamesNoShownProperty()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderForm(Override("Id", _ => { })));

        Assert.Contains("names Id", error.Message);
    }

    [Fact]
    public void ModelField_ShouldThrow_WhenItIsOutsideModelFields()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            Render<TbModelField>(parameters => parameters.Add(field => field.Name, "Name")));

        Assert.Contains("must be inside a TbModelFields", error.Message);
    }
}
