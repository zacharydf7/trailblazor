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
    private string? _nameError;
    private bool _valid = true;
    private IReadOnlyList<string> _errors = [];

    public TbFormTests()
    {
        this.AddTrailblazor();
        Services.AddSingleton(new TakenNames("Robin"));
        // Only Expedition is marked for it, so Trip is still checked the way an app without it would be.
        Services.AddValidation();
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
    public void Reset_ShouldPutTheValueBackAndHideTheMessages_WhenTheFormResets()
    {
        _name = "Robin";
        var resets = 0;
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsValid, _valid)
            .Add(form => form.IsValidChanged, (bool valid) => _valid = valid)
            .Add(form => form.OnReset, () => resets++)
            .Add(form => form.ChildContent, (RenderFragment)AddNameField));
        cut.Find("input").Change("");
        cut.Find("form").Submit();
        Assert.NotEmpty(cut.FindAll("[data-part=error]"));

        cut.Find("form").Reset();

        Assert.Equal("Robin", _name);
        Assert.Equal("Robin", cut.Find("input").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.True(_valid);
        Assert.Equal(1, resets);
    }

    [Fact]
    public async Task Reset_ShouldShowNoMessage_WhenTheFirstValueWouldFail()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Null(_name);
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.False(_valid);
    }

    [Fact]
    public async Task Reset_ShouldGoBackToTheParentsLatestValue_WhenTheParentSetOneAfterTheFirstRender()
    {
        var cut = RenderForm();
        _name = "Loaded";
        cut.Render();
        cut.Find("input").Change("Typed");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal("Loaded", _name);
    }

    [Fact]
    public async Task Reset_ShouldKeepTheParentsError_WhenTheParentSetOne()
    {
        _name = "Robin";
        _nameError = "That name is taken.";
        var cut = RenderForm();

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal("That name is taken.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task Reset_ShouldHideTheAnnotationsMessages_WhenGivenDataAnnotations()
    {
        var cut = RenderModelForm(new Trip());
        await cut.InvokeAsync(() => cut.Instance.ValidateAsync());
        Assert.NotEmpty(_errors);

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());
        Assert.Empty(_errors);

        Assert.False(await cut.InvokeAsync(() => cut.Instance.ValidateAsync()));
        Assert.NotEmpty(_errors);
    }

    [Fact]
    public async Task Reset_ShouldLeaveAValidatorsMessages_WhenTheValidatorAddedThem()
    {
        var model = new Trip();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, model)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> errors) => _errors = errors)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
            })));
        await cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.NotEmpty(_errors);
    }

    [Fact]
    public async Task Reset_ShouldMarkTheFormUnmodified_WhenAFieldHadChanged()
    {
        var context = new EditContext(new Trip());
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.EditContext, context)
            .Add(form => form.ChildContent, (RenderFragment)AddNameField));
        cut.Find("input").Change("Robin");
        Assert.True(context.IsModified());

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.False(context.IsModified());
    }

    [Fact]
    public async Task Reset_ShouldClearUnreadableDates_WhenCalledFromCode()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbDateRangePicker<DateOnly>>(0);
            builder.AddAttribute(1, nameof(TbDateRangePicker<DateOnly>.Label), "Dates");
            builder.AddAttribute(2, nameof(TbDateRangePicker<DateOnly>.Value),
                new DateRange<DateOnly>(new(2026, 6, 1), new(2026, 6, 5)));
            builder.CloseComponent();
        })));
        var first = cut.Find("[data-part=start-input]").GetAttribute("value");
        cut.Find("[data-part=start-input]").Change("not a date");
        Assert.Equal("not a date", cut.Find("[data-part=start-input]").GetAttribute("value"));

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal(first, cut.Find("[data-part=start-input]").GetAttribute("value"));
    }

    [Fact]
    public async Task Reset_ShouldClearAnAutocompletesUnreadableText_WhenItsValueStaysTheSame()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbAutocomplete<double?>>(0);
            builder.AddAttribute(1, nameof(TbAutocomplete<double?>.FreeText), true);
            builder.AddAttribute(2, nameof(TbAutocomplete<double?>.Value), (double?)5);
            builder.AddAttribute(3, nameof(TbAutocomplete<double?>.Items), (IEnumerable<double?>)[5, 10]);
            builder.CloseComponent();
        })));
        cut.Find("input").Input("abc");
        cut.Find("input").Change("abc");
        Assert.NotEmpty(cut.FindAll("[data-part=error]"));

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal("5", cut.Find("input").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task Reset_ShouldLetAValidatorCheckAgain_WhenTheValueChanged()
    {
        var trip = new Trip { Leader = "Sam" };
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, trip)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> errors) => _errors = errors)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbTextField<string>>(1);
                builder.AddAttribute(2, nameof(TbTextField<string>.Value), trip.Leader);
                builder.AddAttribute(3, nameof(TbTextField<string>.ValueChanged),
                    EventCallback.Factory.Create<string>(this, value => trip.Leader = value));
                builder.AddAttribute(4, nameof(TbTextField<string>.ValueExpression),
                    (System.Linq.Expressions.Expression<Func<string>>)(() => trip.Leader!));
                builder.CloseComponent();
            })));
        cut.Find("input").Change("");
        Assert.NotEmpty(_errors);

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal("Sam", trip.Leader);
        Assert.Empty(_errors);
    }

    [Fact]
    public async Task Reset_ShouldNotAddAValidatorsMessage_WhenItWasntShowingOne()
    {
        var trip = new Trip();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, trip)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> errors) => _errors = errors)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbTextField<string>>(1);
                builder.AddAttribute(2, nameof(TbTextField<string>.Value), trip.Leader);
                builder.AddAttribute(3, nameof(TbTextField<string>.ValueChanged),
                    EventCallback.Factory.Create<string>(this, value => trip.Leader = value));
                builder.AddAttribute(4, nameof(TbTextField<string>.ValueExpression),
                    (System.Linq.Expressions.Expression<Func<string>>)(() => trip.Leader!));
                builder.CloseComponent();
            })));
        cut.Find("input").Change("Sam");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Null(trip.Leader);
        Assert.Empty(_errors);
    }

    [Fact]
    public async Task Reset_ShouldNotAddAValidatorsMessage_WhenOnlyTheParentsErrorIsShowing()
    {
        var trip = new Trip();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, trip)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> errors) => _errors = errors)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbTextField<string>>(1);
                builder.AddAttribute(2, nameof(TbTextField<string>.Value), trip.Leader);
                builder.AddAttribute(3, nameof(TbTextField<string>.ValueChanged),
                    EventCallback.Factory.Create<string>(this, value => trip.Leader = value));
                builder.AddAttribute(4, nameof(TbTextField<string>.ValueExpression),
                    (System.Linq.Expressions.Expression<Func<string>>)(() => trip.Leader!));
                builder.AddAttribute(5, nameof(TbTextField<string>.Error), "That name is taken.");
                builder.CloseComponent();
            })));
        cut.Find("input").Change("Sam");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal(["That name is taken."], _errors);
    }

    [Fact]
    public async Task Reset_ShouldNotFail_WhenAnEarlierFieldsResetRemovesAnAutocomplete()
    {
        var cut = Render<KindForm>();
        cut.Find("input").Change("Other");
        cut.FindAll("input")[1].Input("Rid");

        await cut.InvokeAsync(() => cut.FindComponent<TbForm>().Instance.ResetAsync());

        Assert.Single(cut.FindAll("input"));
        Assert.Equal("Trail", cut.Find("input").GetAttribute("value"));
    }

    // A field that shows another for one of its values, so resetting it can remove the other mid-reset.
    private sealed class KindForm : ComponentBase
    {
        private string? _kind = "Trail";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbForm>(0);
            builder.AddComponentParameter(1, nameof(TbForm.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<TbTextField<string>>(0);
                content.AddComponentParameter(1, nameof(TbTextField<string>.Value), _kind);
                content.AddComponentParameter(2, nameof(TbTextField<string>.ValueChanged),
                    EventCallback.Factory.Create<string>(this, value => _kind = value));
                content.CloseComponent();
                if (_kind == "Other")
                {
                    content.OpenComponent<TbAutocomplete<string>>(3);
                    content.AddComponentParameter(4, nameof(TbAutocomplete<string>.Items),
                        (IEnumerable<string>)["Ridge"]);
                    content.CloseComponent();
                }
            }));
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task Reset_ShouldGoBackToAValueTheParentRewrote_WhenItAnsweredAnEdit()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddAttribute(1, nameof(TbTextField<string>.Value), _name);
            builder.AddAttribute(2, nameof(TbTextField<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, value => _name = value?.Trim()));
            builder.CloseComponent();
        })));
        cut.Find("input").Change(" Sam ");
        cut.Render();

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal("Sam", _name);
        Assert.Equal("Sam", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Reset_ShouldReadOutNothing_WhenTheValueItGoesBackToWouldFail()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderForm();
        cut.Find("input").Change("Robin");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Empty(announcer.FindAll("[role=status] p"));
    }

    [Fact]
    public async Task Reset_ShouldEmptyAFileUploadsPickerAndItsRejections_WhenCalled()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbFileUpload>(0);
            builder.AddAttribute(1, nameof(TbFileUpload.Label), "Map");
            builder.AddAttribute(2, nameof(TbFileUpload.Accept), ".pdf");
            builder.CloseComponent();
        })));
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("notes", "notes.txt"));
        Assert.NotEmpty(cut.FindAll("[data-part=rejected]"));

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Empty(cut.FindAll("[data-part=rejected]"));
        Assert.Single(JSInterop.Invocations["clearPicker"]);
    }

    [Fact]
    public void Submit_ShouldRaiseOnInvalidSubmitWithTheParentsError_WhenTheParentSetsOne()
    {
        _name = "Robin";
        _nameError = "That name is taken.";
        var invalid = 0;
        var valid = 0;
        var cut = RenderForm(onValid: () => valid++, onInvalid: () => invalid++);

        cut.Find("form").Submit();

        Assert.Equal((0, 1), (valid, invalid));
        Assert.False(_valid);
        Assert.Equal(["That name is taken."], _errors);
    }

    [Fact]
    public async Task ResetValidation_ShouldKeepTheParentsError_WhenTheParentSetOne()
    {
        _nameError = "That name is taken.";
        var cut = RenderForm();
        await cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        await cut.InvokeAsync(() => cut.Instance.ResetValidationAsync());

        Assert.Equal("That name is taken.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldReportValidAndHideTheError_WhenTheParentClearsItsError()
    {
        _name = "Robin";
        _nameError = "That name is taken.";
        var cut = RenderForm();
        Assert.False(_valid);

        _nameError = null;
        cut.Render();

        Assert.True(_valid);
        Assert.Empty(cut.FindAll("[data-part=error]"));
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

    [Fact]
    public void Submit_ShouldSayWhichRequiredFieldsAreEmptyAndNotSubmit_WhenTheyAreLeftEmpty()
    {
        var valid = 0;
        var cut = Render<TbForm>(parameters => parameters
            .Add(form => form.OnValidSubmit, (EditContext _) => valid++)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
                builder.AddAttribute(2, nameof(TbTextField<string>.Required), true);
                builder.CloseComponent();
                builder.OpenComponent<TbCheckbox<bool>>(3);
                builder.AddAttribute(4, nameof(TbCheckbox<bool>.Label), "Terms");
                builder.AddAttribute(5, nameof(TbCheckbox<bool>.Required), true);
                builder.AddAttribute(6, nameof(TbCheckbox<bool>.RequiredMessage), "Accept the terms to go on.");
                builder.CloseComponent();
            })));
        Assert.Empty(cut.FindAll("[data-part=error]"));

        cut.Find("form").Submit();

        Assert.Equal(0, valid);
        Assert.Equal(["Name is required.", "Accept the terms to go on."],
            cut.FindAll("[data-part=error]").Select(error => error.TextContent.Trim()));
    }

    [Fact]
    public void Submit_ShouldCountAnEmptyListAndAHalfRangeAsMissing_WhenTheirFieldsAreRequired()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbMultiSelect<string>>(0);
            builder.AddAttribute(1, nameof(TbMultiSelect<string>.Label), "Trails");
            builder.AddAttribute(2, nameof(TbMultiSelect<string>.Required), true);
            builder.AddAttribute(3, nameof(TbMultiSelect<string>.Value), (IReadOnlyList<string>)[]);
            builder.CloseComponent();
            builder.OpenComponent<TbDateRangePicker<DateOnly>>(4);
            builder.AddAttribute(5, nameof(TbDateRangePicker<DateOnly>.Label), "Trip");
            builder.AddAttribute(6, nameof(TbDateRangePicker<DateOnly>.Required), true);
            builder.AddAttribute(7, nameof(TbDateRangePicker<DateOnly>.Value),
                new DateRange<DateOnly>(new DateOnly(2026, 7, 2), null));
            builder.CloseComponent();
        })));

        cut.Find("form").Submit();

        Assert.Equal(["Trails is required.", "Trip is required."],
            cut.FindAll("[data-part=error]").Select(error => error.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldReportInvalidUntilARequiredFieldIsFilled_WhenItStartsEmpty()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsValid, _valid)
            .Add(form => form.IsValidChanged, (bool valid) => _valid = valid)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
                builder.AddAttribute(2, nameof(TbTextField<string>.Required), true);
                builder.CloseComponent();
            })));
        Assert.False(_valid);
        Assert.Empty(cut.FindAll("[data-part=error]"));

        cut.Find("input").Change("Robin");

        Assert.True(_valid);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Submit_ShouldSubmit_WhenAnEmptyRequiredFieldIsDisabledOrReadOnly(bool disabled, bool readOnly)
    {
        var valid = 0;
        var cut = Render<TbForm>(parameters => parameters
            .Add(form => form.OnValidSubmit, (EditContext _) => valid++)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
                builder.AddAttribute(2, nameof(TbTextField<string>.Required), true);
                builder.AddAttribute(3, nameof(TbTextField<string>.Disabled), disabled);
                builder.AddAttribute(4, nameof(TbTextField<string>.ReadOnly), readOnly);
                builder.CloseComponent();
            })));

        cut.Find("form").Submit();

        Assert.Equal(1, valid);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Submit_ShouldSubmit_WhenAFieldIsMarkedInvalidWithoutAMessage()
    {
        var valid = 0;
        var cut = Render<TbForm>(parameters => parameters
            .Add(form => form.OnValidSubmit, (EditContext _) => valid++)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
                builder.AddAttribute(2, nameof(TbTextField<string>.Invalid), true);
                builder.CloseComponent();
            })));

        cut.Find("form").Submit();

        Assert.Equal(1, valid);
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

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
        builder.AddAttribute(5, nameof(TbTextField<string>.Error), _nameError);
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
