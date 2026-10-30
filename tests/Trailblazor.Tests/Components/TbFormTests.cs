using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

namespace Trailblazor.Tests.Components;

public class TbFormTests : BunitContext
{
    private string? _name;
    private bool _valid = true;
    private IReadOnlyList<string> _errors = [];

    public TbFormTests()
    {
        this.AddTrailblazor();
        Services.AddSingleton(new TakenNames("Robin"));
        // Only Expedition is marked for it, so Trip is still checked the way an app without it would be.
        Services.AddValidation();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Render_ShouldReportInvalidWithoutShowingMessages_WhenAFieldWouldFail()
    {
        var cut = RenderForm();

        Assert.False(_valid);
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Empty(_errors);
    }

    [Fact]
    public void Change_ShouldReportValid_WhenEveryFieldPasses()
    {
        var cut = RenderForm();

        cut.Find("input").Change("Robin");

        Assert.True(_valid);
    }

    [Fact]
    public void Submit_ShouldShowTheMessagesAndRaiseOnInvalidSubmit_WhenAFieldFails()
    {
        var invalid = 0;
        var cut = RenderForm(onInvalid: () => invalid++);

        cut.Find("form").Submit();

        Assert.Equal(1, invalid);
        Assert.Equal("Enter a name.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal(["Enter a name."], _errors);
    }

    [Fact]
    public void Submit_ShouldRaiseOnValidSubmit_WhenEveryFieldPasses()
    {
        var valid = 0;
        var cut = RenderForm(onValid: () => valid++);
        cut.Find("input").Change("Robin");

        cut.Find("form").Submit();

        Assert.Equal(1, valid);
    }

    [Fact]
    public async Task ResetValidation_ShouldHideTheMessages_WhenTheyWereShown()
    {
        var cut = RenderForm();
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ValidateAsync()));
        Assert.NotEmpty(cut.FindAll("[data-part=error]"));

        await cut.InvokeAsync(() => cut.Instance.ResetValidationAsync());

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Empty(_errors);
    }

    [Fact]
    public async Task Validate_ShouldShowTheModelsValidatorMessages_WhenGivenAModel()
    {
        var cut = RenderModelForm(new Trip());

        Assert.False(await cut.InvokeAsync(() => cut.Instance.ValidateAsync()));

        Assert.Equal(["The Leader field is required."], _errors);
    }

    [Fact]
    public void Render_ShouldCheckTheModelsAnnotationsWithoutShowingMessages_WhenGivenAModel()
    {
        RenderModelForm(new Trip());

        Assert.False(_valid);
        Assert.Empty(_errors);
    }

    [Fact]
    public async Task ResetValidation_ShouldLeaveAValidatorsMessages_WhenTheValidatorAddedThem()
    {
        var cut = RenderModelForm(new Trip());
        await cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        await cut.InvokeAsync(() => cut.Instance.ResetValidationAsync());

        Assert.Equal(["The Leader field is required."], _errors);
    }

    [Fact]
    public void Render_ShouldReportInvalid_WhenTheParentEmptiesAField()
    {
        _name = "Robin";
        var cut = RenderForm();
        Assert.True(_valid);

        _name = null;
        cut.Render(parameters => parameters.Add(form => form.IsValid, _valid));

        Assert.False(_valid);
    }

    [Fact]
    public void Change_ShouldPassAFailingCallbackToTheRenderer_WhenAFieldChanges()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsValid, false)
            .Add(form => form.IsValidChanged, (bool _) => throw new InvalidOperationException("Callback failed."))
            .Add(form => form.ChildContent, (RenderFragment)AddNameField));

        var failure = Assert.Throws<InvalidOperationException>(() => cut.Find("input").Change("Robin"));

        Assert.Equal("Callback failed.", failure.Message);
    }

    [Fact]
    public void Submit_ShouldLeaveValidatingToTheHandler_WhenGivenOnSubmit()
    {
        var submitted = 0;
        var cut = RenderForm(onSubmit: () => submitted++);

        cut.Find("form").Submit();

        Assert.Equal(1, submitted);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Render_ShouldCheckTheModelWithTheAppsServices_WhenAnAnnotationNeedsOne()
    {
        RenderModelForm(new Trip { Leader = "Robin" });

        Assert.False(_valid);
    }

    [Fact]
    public async Task Validate_ShouldAgreeWithIsValid_WhenTheModelsAnnotationsArentValidated()
    {
        var cut = RenderModelForm(new Trip(), annotations: false);

        Assert.True(_valid);
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ValidateAsync()));
    }

    [Fact]
    public void Validate_ShouldCountAFieldWithNoBinding_WhenItIsInAPlainEditForm()
    {
        var context = new EditContext(new object());
        Render<EditForm>(parameters => parameters.Add(form => form.EditContext, context)
            .Add(form => form.ChildContent, (RenderFragment<EditContext>)(_ => AddNameField)));

        Assert.False(context.Validate());

        Assert.Equal(["Enter a name."], context.GetValidationMessages());
    }

    [Fact]
    public async Task Render_ShouldCheckNestedObjectsAsTheValidatorDoes_WhenTheModelIsRegisteredForValidation()
    {
        var cut = RenderModelForm(new Expedition());

        Assert.False(_valid);
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ValidateAsync()));
        Assert.Equal(["The Site field is required."], _errors);
    }

    [Fact]
    public void Render_ShouldReportValid_WhenARegisteredModelsNestedObjectsPass()
    {
        RenderModelForm(new Expedition { Camp = { Site = "Lakeshore" } });

        Assert.True(_valid);
    }

    private IRenderedComponent<TbForm> RenderModelForm(object model, bool annotations = true) =>
        Render<TbForm>(parameters => parameters.Add(form => form.Model, model).Add(form => form.IsValid, _valid)
            .Add(form => form.DataAnnotations, annotations)
            .Add(form => form.IsValidChanged, (bool valid) => _valid = valid)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> errors) => _errors = errors));

    private IRenderedComponent<TbForm> RenderForm(Action? onValid = null, Action? onInvalid = null,
        Action? onSubmit = null) =>
        Render<TbForm>(parameters =>
        {
            parameters.Add(form => form.IsValid, _valid)
                .Add(form => form.IsValidChanged, (bool valid) => _valid = valid)
                .Add(form => form.ErrorsChanged, (IReadOnlyList<string> errors) => _errors = errors)
                .Add(form => form.ChildContent, (RenderFragment)AddNameField);
            if (onValid is not null)
            {
                parameters.Add(form => form.OnValidSubmit, (EditContext _) => onValid());
            }

            if (onInvalid is not null)
            {
                parameters.Add(form => form.OnInvalidSubmit, (EditContext _) => onInvalid());
            }

            if (onSubmit is not null)
            {
                parameters.Add(form => form.OnSubmit, (EditContext _) => onSubmit());
            }
        });

    // A field with no binding, which the form still counts.
    private void AddNameField(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
        builder.AddAttribute(2, nameof(TbTextField<string>.Value), _name);
        builder.AddAttribute(3, nameof(TbTextField<string>.ValueChanged),
            EventCallback.Factory.Create<string>(this, value => _name = value));
        builder.AddAttribute(4, nameof(TbTextField<string>.Validation),
            (Func<string?, IEnumerable<string>>)(name => string.IsNullOrWhiteSpace(name) ? ["Enter a name."] : []));
        builder.CloseComponent();
    }

    private sealed class Trip
    {
        [Required]
        [NotTaken]
        public string? Leader { get; set; }
    }

    private sealed record TakenNames(params string[] Names);

    // Checks a name against a service, as a uniqueness rule would.
    private sealed class NotTakenAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
            validationContext.GetService(typeof(TakenNames)) is TakenNames taken && taken.Names.Contains(value)
                ? new ValidationResult("That name is taken.") : ValidationResult.Success;
    }
}

// AddValidation reaches nested objects only in public, top-level types marked for its source generator.
#pragma warning disable ASP0029 // The validation types are experimental.
[ValidatableType]
#pragma warning restore ASP0029
public sealed class Expedition
{
    [Required]
    public string? Name { get; set; } = "Ridge loop";

    public Camp Camp { get; set; } = new();
}

public sealed class Camp
{
    [Required]
    public string? Site { get; set; }
}
